using System.Text.Json;
using System.Text.Json.Serialization;

namespace IhGit.Core.Scenarios;

public sealed record ScenarioCommit(string Sha, string Title);

public sealed record ScenarioManifest
{
    public const int CurrentVersion = 1;
    public const string FileName = "manifest.json";
    public const string MarkerFileName = ".ihgit-scenario";

    public int Version { get; init; } = CurrentVersion;
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";

    /// <summary>Random token also stored in the marker file; proves this directory was created by the generator.</summary>
    public string OwnerToken { get; init; } = "";

    /// <summary>Working clone, relative to the scenario directory.</summary>
    public string RepoDirectory { get; init; } = "work";

    /// <summary>Local bare origin, relative to the scenario directory.</summary>
    public string OriginDirectory { get; init; } = "origin.git";

    /// <summary>Base branch of the synthetic request, e.g. support/v4.17.</summary>
    public string TargetBranch { get; init; } = "";

    public string SourceBranch { get; init; } = "";
    public string FeatureName { get; init; } = "";
    public string Title { get; init; } = "";
    public string Body { get; init; } = "";

    /// <summary>Branch checked out in the working clone after generation.</summary>
    public string StartBranch { get; init; } = "";

    public IReadOnlyList<ScenarioCommit> Commits { get; init; } = [];

    /// <summary>Expected result of merging the first selected target; see <see cref="ScenarioOutcomes"/>.</summary>
    public string ExpectedFirstOutcome { get; init; } = "";

    public int MinSupportMajor { get; init; } = 4;
    public int MinSupportMinor { get; init; } = 15;

    [JsonIgnore]
    public string DirectoryPath { get; init; } = "";

    [JsonIgnore]
    public string RepoPath => Path.Combine(DirectoryPath, RepoDirectory);

    [JsonIgnore]
    public string OriginPath => Path.Combine(DirectoryPath, OriginDirectory);

    internal static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static ScenarioManifest Read(string manifestPath)
    {
        var full = Path.GetFullPath(manifestPath);
        var manifest = JsonSerializer.Deserialize<ScenarioManifest>(File.ReadAllText(full), JsonOptions)
            ?? throw new ScenarioValidationException("Scenario manifest is empty");

        return manifest with { DirectoryPath = Path.GetDirectoryName(full)! };
    }

    public void Write()
        => File.WriteAllText(Path.Combine(DirectoryPath, FileName), JsonSerializer.Serialize(this, JsonOptions));
}

public static class ScenarioOutcomes
{
    public const string Applied = "Applied";
    public const string Conflicts = "Conflicts";
    public const string AlreadyApplied = "AlreadyApplied";
    public const string CheckoutBlocked = "CheckoutBlocked";
    public const string ConflictInProgress = "ConflictInProgress";
}

public sealed class ScenarioValidationException(string message) : Exception(message);
