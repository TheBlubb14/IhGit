using System.Text.Json;

namespace IhGit.Core.Scenarios;

public sealed record LocalRequestResult(
    int Number,
    string Title,
    string SourceBranch,
    string BaseBranch,
    bool IsUpMerge,
    IReadOnlyList<string> Commits,
    DateTimeOffset CreatedAt,
    string Note);

/// <summary>Records what a real GitHub pull request would have been, without contacting GitHub.</summary>
public static class LocalRequestStore
{
    public const string FileName = "local-results.json";
    public const string Note = "Local-only result: no GitHub pull request, reviewers, labels or merge queue entry were created.";

    private static readonly JsonSerializerOptions options = new() { WriteIndented = true };

    public static IReadOnlyList<LocalRequestResult> Read(ScenarioManifest manifest)
    {
        var path = Path.Combine(manifest.DirectoryPath, FileName);

        return File.Exists(path)
            ? JsonSerializer.Deserialize<List<LocalRequestResult>>(File.ReadAllText(path), options) ?? []
            : [];
    }

    public static LocalRequestResult Append(ScenarioManifest manifest, string title, string sourceBranch, string baseBranch, bool isUpMerge, IEnumerable<string> commits)
    {
        var existing = Read(manifest).ToList();
        var result = new LocalRequestResult(existing.Count + 1, title, sourceBranch, baseBranch, isUpMerge, [.. commits], DateTimeOffset.Now, Note);
        existing.Add(result);

        var path = Path.Combine(manifest.DirectoryPath, FileName);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(existing, options));
        File.Move(temp, path, overwrite: true);

        return result;
    }
}
