using IhGit.Core.Scenarios;

namespace IhGitWpf;

/// <summary>A validated offline scenario the app was started with (--offline-scenario).</summary>
public sealed record OfflineSession(ScenarioManifest Manifest)
{
    public const string Argument = "--offline-scenario";
}
