using System.Text.Json;

namespace IhGit.Core.Scenarios;

public static class ScenarioGuard
{
    /// <summary>
    /// Validates that a manifest belongs to a generator-owned scenario directory and that
    /// the working clone only talks to the scenario's local bare origin.
    /// </summary>
    public static ScenarioManifest Validate(string manifestPath)
    {
        if (!File.Exists(manifestPath))
            throw new ScenarioValidationException($"Scenario manifest '{manifestPath}' does not exist");

        ScenarioManifest manifest;
        try
        {
            manifest = ScenarioManifest.Read(manifestPath);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            throw new ScenarioValidationException($"Scenario manifest is malformed: {ex.Message}");
        }

        if (manifest.Version != ScenarioManifest.CurrentVersion)
            throw new ScenarioValidationException($"Unsupported scenario manifest version {manifest.Version}");

        var marker = Path.Combine(manifest.DirectoryPath, ScenarioManifest.MarkerFileName);
        if (string.IsNullOrWhiteSpace(manifest.OwnerToken) || !File.Exists(marker) || File.ReadAllText(marker).Trim() != manifest.OwnerToken)
            throw new ScenarioValidationException("Directory is not owned by the IhGit scenario generator");

        foreach (var directory in new[] { manifest.RepoDirectory, manifest.OriginDirectory })
        {
            if (string.IsNullOrWhiteSpace(directory) || directory is "." or ".." || directory.IndexOfAny(['/', '\\', ':']) >= 0)
                throw new ScenarioValidationException($"Invalid scenario directory name '{directory}'");
        }

        if (!Directory.Exists(Path.Combine(manifest.RepoPath, ".git")))
            throw new ScenarioValidationException("Scenario working clone is missing");

        if (!Directory.Exists(manifest.OriginPath) ||
            GitProcess.Run(manifest.OriginPath, ["--git-dir", manifest.OriginPath, "rev-parse", "--is-bare-repository"]) is not { Success: true, Output: "true" })
            throw new ScenarioValidationException("Scenario local origin is missing or not a bare repository");

        if (manifest.Commits.Count == 0)
            throw new ScenarioValidationException("Scenario has no commits");

        RevalidateRemote(manifest);

        foreach (var commit in manifest.Commits)
        {
            if (!LocalGitWorkflow.CommitExists(manifest.RepoPath, commit.Sha))
                throw new ScenarioValidationException($"Scenario commit {commit.Sha} is missing from the working clone");
        }

        return manifest;
    }

    /// <summary>Call before every fetch/push: the remote must still be the scenario's own local origin.</summary>
    public static void RevalidateRemote(ScenarioManifest manifest)
    {
        var remotes = GitProcess.Run(manifest.RepoPath, ["remote"]);
        if (!remotes.Success || remotes.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) is not ["origin"])
            throw new ScenarioValidationException("Scenario working clone must have exactly one remote named 'origin'");

        var expected = NormalizePath(manifest.OriginPath);

        foreach (var args in new[] { new[] { "remote", "get-url", "origin" }, ["remote", "get-url", "--push", "origin"] })
        {
            var result = GitProcess.Run(manifest.RepoPath, args);
            var url = result.Output.Trim();

            if (!result.Success || url.Contains("://") || !Path.IsPathRooted(url) || NormalizePath(url) != expected)
                throw new ScenarioValidationException($"Remote '{url}' is not the scenario's local origin");
        }
    }

    public static void DeleteOwnedDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;

        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);

        Directory.Delete(path, recursive: true);
    }

    /// <summary>Deletes a generated scenario, refusing any directory that is not generator-owned.</summary>
    public static void Delete(string scenarioDirectory)
    {
        var manifest = Validate(Path.Combine(scenarioDirectory, ScenarioManifest.FileName));
        DeleteOwnedDirectory(manifest.DirectoryPath);
    }

    private static string NormalizePath(string path)
        => Path.GetFullPath(path).TrimEnd('\\', '/').ToUpperInvariant();
}
