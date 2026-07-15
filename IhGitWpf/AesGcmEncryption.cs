using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace IhGitWpf;

/// <summary>
/// AES-GCM encryption backed by a TPM key.
///
/// Architecture
/// ────────────
/// • A named ECC (P-256) key lives permanently in the TPM via the
///   "Microsoft Platform Crypto Provider".  Its private key never leaves
///   the chip.
/// • At runtime we load that key, perform ECDH against an *ephemeral*
///   peer key-pair, and SHA-256 hash the resulting shared secret.
///   That 32-byte hash becomes the `rawKey` for your existing PBKDF2
///   pipeline.
/// • For large plaintexts the payload is split into 64 KiB chunks so
///   that each chunk gets a fresh nonce — a hard requirement for GCM
///   safety (never reuse a (key, nonce) pair).
///
/// Wire format (Base-64 of)
/// ────────────────────────
///   [4 bytes LE  – chunk count         ]
///   [65 bytes    – ephemeral public key ] (X9.62 uncompressed)
///   Per chunk:
///     [4 bytes LE – plaintext chunk length]
///     [16 bytes   – salt                  ]
///     [12 bytes   – nonce                 ]
///     [16 bytes   – GCM tag               ]
///     [N  bytes   – ciphertext            ]
/// </summary>
public static class AesGcmEncryption
{
    public class AesGcmEncryptionException(string message, Exception? innerException = null) : Exception(message, innerException)
    {
    }

    // ── crypto constants ────────────────────────────────────────────────
    private const int SaltSize = 16;   // 128 bits
    private const int DerivedKeyOutputLength = 32; // 256 bits – AES-256
    private const int DerivedKeyIterations = 600_000;
    private const int NonceSize = 12;  // 96 bits  – GCM canonical
    private const int TagSize = 16;  // 128 bits – GCM max
    private const int ChunkSize = 64 * 1024; // 64 KiB per chunk

    // ── TPM / CNG constants ─────────────────────────────────────────────
    //private const string TpmProviderName = "Microsoft Platform Crypto Provider";
    private const int EphemeralPublicKeySize = 65; // X9.62 uncompressed P-256

    // ====================================================================
    //  TPM key management
    // ====================================================================

    /// <summary>
    /// Creates a named P-256 ECDH key inside the TPM (one-time setup).
    /// If a key with <paramref name="keyName"/> already exists, this is a no-op.
    /// </summary>
    /// <param name="keyName">Persistent key name stored in the TPM key-store.</param>
    /// <returns>The TPM-resident key. Needs to be disposed by the caller when no longer needed.</returns>
    private static CngKey EnsureTpmKeyExists(string keyName)
    {
        if (GetKey(keyName, out var key, out var providerName) && key is not null)
            return key;

        var creationParams = new CngKeyCreationParameters
        {
            Provider = new CngProvider(providerName),
            KeyUsage = CngKeyUsages.KeyAgreement,
            ExportPolicy = CngExportPolicies.None, // private key never exported
            KeyCreationOptions = CngKeyCreationOptions.None // user-level by default
        };

        return CngKey.Create(CngAlgorithm.ECDiffieHellmanP256, keyName, creationParams);
    }

    /// <summary>
    /// Deletes a named TPM key (e.g. during key rotation or de-provisioning).
    /// </summary>
    public static void DeleteTpmKey(string keyName)
    {
        if (GetKey(keyName, out var key, out _) && key is not null)
        {
            using (key)
                key.Delete();
        }
    }

    public static bool GetKey(string keyName, [NotNullWhen(true)] out CngKey? key, [NotNullWhen(true)] out string providerName)
    {
        key = null;
        providerName = null!;

        // Strongest -> weakest open preference
        CngKeyOpenOptions[] openOptionsOrder =
        [
            CngKeyOpenOptions.MachineKey, // strongest isolation, usually requires elevated rights
            CngKeyOpenOptions.UserKey,    // per-user persisted key (no admin needed)
            CngKeyOpenOptions.None        // fallback/default
        ];

        // Prefer TPM provider first, then software provider
        CngProvider[] providers =
        [
            CngProvider.MicrosoftPlatformCryptoProvider,      // TPM-backed when available
            CngProvider.MicrosoftSoftwareKeyStorageProvider   // software fallback
        ];

        foreach (var provider in providers)
        {
            foreach (var openOptions in openOptionsOrder)
            {
                try
                {
                    var exists = CngKey.Exists(keyName, provider, openOptions);
                    providerName = provider.Provider;
                    if (exists)
                    {
                        key = CngKey.Open(keyName, provider);
                        return true;
                    }
                }
                catch (CryptographicException)
                {
                    // Try next option/provider
                }
                catch (UnauthorizedAccessException)
                {
                    // MachineKey commonly fails without admin; continue
                }
            }
        }

        if (string.IsNullOrWhiteSpace(providerName))
        {
            // This should never happen if the TPM is present and the Microsoft Platform Crypto Provider is available.
            throw new AesGcmEncryptionException("No suitable CNG provider found for TPM key creation.");
        }

        return false;
    }

    // ====================================================================
    //  Raw-key derivation from TPM  (shared-secret → PBKDF2 input)
    // ====================================================================

    /// <summary>
    /// Uses ECDH with the named TPM key to derive a 32-byte raw key.
    ///
    /// Returns both the derived key material AND the ephemeral public key
    /// so the recipient can reproduce the same shared secret later.
    /// </summary>
    /// <param name="tpmCngKey">The P-256 key in the TPM.</param>
    /// <param name="ephemeralPublicKeyBytes">
    ///   Output: 65-byte X9.62 uncompressed public key of the ephemeral pair.
    /// </param>
    private static byte[] DeriveRawKeyFromTpm(
        CngKey tpmCngKey,
        out byte[] ephemeralPublicKeyBytes)
    {
        using var tpmEcdh = new ECDiffieHellmanCng(tpmCngKey);

        // Ephemeral pair – used once per encryption, then discarded.
        using var ephemeralEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

        // Import the ephemeral *public* key into CNG so tpmEcdh can consume it.
        var ephemeralPubKeyParams = ephemeralEcdh.ExportParameters(includePrivateParameters: false);
        using var ephemeralCngKey = ECDiffieHellmanCng.Create(ephemeralPubKeyParams).PublicKey;

        // Configure: raw ECDH → SHA-256 hash of the shared secret.
        tpmEcdh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
        tpmEcdh.HashAlgorithm = CngAlgorithm.Sha256;

        // TPM performs the private-key operation; result is the 32-byte KDF output.
        byte[] rawKey = tpmEcdh.DeriveKeyMaterial(ephemeralCngKey);

        // Serialize the ephemeral public key for storage alongside the ciphertext.
        ephemeralPublicKeyBytes = ephemeralEcdh
            .ExportSubjectPublicKeyInfo()
            .ExportUncompressedPoint(); // helper below; gives 65-byte X9.62 blob

        return rawKey; // 32 bytes – fed into PBKDF2 as the "password"
    }

    /// <summary>
    /// Re-derives the same raw key using the TPM private key + the ephemeral
    /// public key stored with the ciphertext.
    /// </summary>
    private static byte[] ReconstructRawKeyFromTpm(
        CngKey tpmCngKey,
        ReadOnlySpan<byte> ephemeralPublicKeyBytes)
    {
        using var tpmEcdh = new ECDiffieHellmanCng(tpmCngKey);

        // Re-import the ephemeral public key from the stored bytes.
        var ephemeralEcdh = ECDiffieHellman.Create();
        ephemeralEcdh.ImportSubjectPublicKeyInfo(
            ImportUncompressedPoint(ephemeralPublicKeyBytes), out _);

        using var ephemeralCngKey = ephemeralEcdh.PublicKey;

        tpmEcdh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
        tpmEcdh.HashAlgorithm = CngAlgorithm.Sha256;

        return tpmEcdh.DeriveKeyMaterial(ephemeralCngKey);
    }

    // ====================================================================
    //  Public Encrypt / Decrypt  (chunked, TPM-keyed)
    // ====================================================================

    /// <summary>
    /// Encrypts <paramref name="plainText"/> using AES-256-GCM, chunked into
    /// 64 KiB blocks, with the encryption key derived from a TPM-resident key.
    /// </summary>
    /// <param name="plainText">Plaintext of arbitrary length.</param>
    /// <param name="tpmKeyName">Persistent name of the P-256 key in the TPM.</param>
    /// <returns>Base-64 ciphertext payload.</returns>
    public static string EncryptWithTpm(string plainText, string tpmKeyName)
    {
        using var tpmKey = EnsureTpmKeyExists(tpmKeyName);
        byte[] rawKey = DeriveRawKeyFromTpm(tpmKey, out byte[] ephemeralPubKey);
        return EncryptChunked(plainText, rawKey, ephemeralPubKey);
    }

    /// <summary>
    /// Decrypts a payload produced by <see cref="EncryptWithTpm"/>.
    /// </summary>
    /// <param name="cipherText">Base-64 ciphertext payload.</param>
    /// <param name="tpmKeyName">Persistent name of the P-256 key in the TPM.</param>
    /// <returns>Decrypted plaintext.</returns>
    public static string DecryptWithTpm(string cipherText, string tpmKeyName)
    {
        using var tpmKey = EnsureTpmKeyExists(tpmKeyName);

        // Peek at the ephemeral public key embedded in the payload header.
        byte[] payload = Convert.FromBase64String(cipherText);
        byte[] ephemeralPubKey = payload[4..(4 + EphemeralPublicKeySize)];

        byte[] rawKey = ReconstructRawKeyFromTpm(tpmKey, ephemeralPubKey);
        return DecryptChunked(cipherText, rawKey);
    }

    // ====================================================================
    //  Chunked Encrypt / Decrypt  (reusable with any raw key)
    // ====================================================================

    /// <summary>
    /// Encrypts arbitrary-length <paramref name="plainText"/> in 64 KiB chunks.
    /// Each chunk receives its own random salt + nonce.
    /// </summary>
    private static string EncryptChunked(
        string plainText,
        byte[] rawKey,
        byte[]? ephemeralPubKey = null)
    {
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        int chunkCount = (plainBytes.Length + ChunkSize - 1) / ChunkSize;
        if (chunkCount == 0) chunkCount = 1; // handle empty string

        // Pre-calculate total output size for a single allocation.
        int chunkPayloadPerByte = SaltSize + NonceSize + TagSize;
        int headerSize = 4 + (ephemeralPubKey?.Length ?? 0);
        long totalSize = headerSize
                                   + (long)chunkCount * (4 + chunkPayloadPerByte)
                                   + plainBytes.Length;

        byte[] output = new byte[totalSize];
        int offset = 0;

        // ── header: chunk count ──────────────────────────────────────────
        BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(offset, 4), chunkCount);
        offset += 4;

        // ── header: ephemeral public key (optional) ──────────────────────
        if (ephemeralPubKey is { Length: > 0 })
        {
            ephemeralPubKey.CopyTo(output.AsSpan(offset));
            offset += ephemeralPubKey.Length;
        }

        // ── chunks ───────────────────────────────────────────────────────
        for (int i = 0; i < chunkCount; i++)
        {
            int chunkStart = i * ChunkSize;
            int chunkLength = Math.Min(ChunkSize, plainBytes.Length - chunkStart);
            // Edge-case: plainBytes is empty → chunkLength is 0.
            if (chunkLength < 0) chunkLength = 0;

            ReadOnlySpan<byte> chunkPlain = plainBytes.AsSpan(chunkStart, chunkLength);

            ReadOnlySpan<byte> salt = RandomNumberGenerator.GetBytes(SaltSize);
            ReadOnlySpan<byte> nonce = RandomNumberGenerator.GetBytes(NonceSize);
            ReadOnlySpan<byte> derivedKey = Rfc2898DeriveBytes.Pbkdf2(
                rawKey, salt, DerivedKeyIterations,
                HashAlgorithmName.SHA256, DerivedKeyOutputLength);

            var tag = new byte[TagSize];
            var ciphertext = new byte[chunkLength];

            using (var aes = new AesGcm(derivedKey, TagSize))
                aes.Encrypt(nonce, chunkPlain, ciphertext, tag);

            // chunk header: plaintext length (so decryptor knows how much to allocate)
            BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(offset, 4), chunkLength);
            offset += 4;

            salt.CopyTo(output.AsSpan(offset)); offset += SaltSize;
            nonce.CopyTo(output.AsSpan(offset)); offset += NonceSize;
            tag.CopyTo(output.AsSpan(offset)); offset += TagSize;
            ciphertext.CopyTo(output.AsSpan(offset)); offset += chunkLength;
        }

        return Convert.ToBase64String(output, 0, offset);
    }

    /// <summary>
    /// Decrypts a payload produced by <see cref="EncryptChunked"/>.
    /// </summary>
    private static string DecryptChunked(string cipherText, byte[] rawKey)
    {
        byte[] payload = Convert.FromBase64String(cipherText);
        ReadOnlySpan<byte> span = payload;
        int offset = 0;

        int chunkCount = BinaryPrimitives.ReadInt32LittleEndian(span[offset..]);
        offset += 4;

        // Skip ephemeral public key if present (already consumed by the caller).
        offset += EphemeralPublicKeySize;

        // Accumulate decrypted chunks.
        using var ms = new MemoryStream(payload.Length);

        for (int i = 0; i < chunkCount; i++)
        {
            int chunkLength = BinaryPrimitives.ReadInt32LittleEndian(span[offset..]);
            offset += 4;

            ReadOnlySpan<byte> salt = span.Slice(offset, SaltSize); offset += SaltSize;
            ReadOnlySpan<byte> nonce = span.Slice(offset, NonceSize); offset += NonceSize;
            ReadOnlySpan<byte> tag = span.Slice(offset, TagSize); offset += TagSize;
            ReadOnlySpan<byte> ciphertext = span.Slice(offset, chunkLength); offset += chunkLength;

            ReadOnlySpan<byte> derivedKey = Rfc2898DeriveBytes.Pbkdf2(
                rawKey, salt, DerivedKeyIterations,
                HashAlgorithmName.SHA256, DerivedKeyOutputLength);

            var plainChunk = new byte[chunkLength];

            using (var aes = new AesGcm(derivedKey, TagSize))
                aes.Decrypt(nonce, ciphertext, tag, plainChunk);

            ms.Write(plainChunk);
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    // ====================================================================
    //  Low-level helpers – X9.62 point serialization
    // ====================================================================

    private static byte[] ExportUncompressedPoint(this byte[] subjectPublicKeyInfo)
    {
        // SubjectPublicKeyInfo wraps the uncompressed point; the last 65 bytes are it.
        // (For P-256: 1 byte 0x04 + 32 bytes X + 32 bytes Y = 65 bytes)
        if (subjectPublicKeyInfo.Length < EphemeralPublicKeySize)
            throw new CryptographicException("Unexpected SubjectPublicKeyInfo length.");

        return subjectPublicKeyInfo[^EphemeralPublicKeySize..];
    }

    private static byte[] ImportUncompressedPoint(ReadOnlySpan<byte> point)
    {
        // Wrap the raw 65-byte uncompressed point back into SubjectPublicKeyInfo.
        // Minimal DER wrapper for id-ecPublicKey + namedCurve P-256.
        // OID 1.2.840.10045.2.1 (ecPublicKey) + OID 1.2.840.10045.3.1.7 (prime256v1)
        byte[] oidWrapper =
        [
            0x30, 0x59,               // SEQUENCE (89 bytes)
              0x30, 0x13,             //   SEQUENCE (19 bytes)
                0x06, 0x07,           //     OID (7 bytes)
                  0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x02, 0x01,   // ecPublicKey
                0x06, 0x08,           //     OID (8 bytes)
                  0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x03, 0x01, 0x07, // prime256v1
              0x03, 0x42,             //   BIT STRING (66 bytes)
                0x00                  //     no unused bits
        ];

        byte[] spki = new byte[oidWrapper.Length + point.Length];
        oidWrapper.CopyTo(spki, 0);
        point.CopyTo(spki.AsSpan(oidWrapper.Length));
        return spki;
    }
}