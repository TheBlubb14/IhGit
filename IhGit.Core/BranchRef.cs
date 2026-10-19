using System.Text.RegularExpressions;

namespace IhGit.Core;

public enum BranchType
{
    unknown,
    support,
    stable,
    deploy,
}

public sealed partial record BranchRef(BranchType BranchType, int Major, int Minor)
{
    public static BranchRef? TryParse(string branchVersion)
    {
        var match = BranchRegex().Match(branchVersion);

        if (!match.Success)
            return null;

        var branchType = match.Groups[1].Value switch
        {
            "support" => BranchType.support,
            "deploy" => BranchType.deploy,
            "stable" => BranchType.stable,
            _ => BranchType.unknown,
        };

        int major = 0;
        int minor = 0;

        if (branchType is BranchType.support or BranchType.deploy)
        {
            var split = match.Groups[2].Value.Trim('v').Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (split.Length < 2 || !int.TryParse(split[0], out major) || !int.TryParse(split[1], out minor))
            {
                major = 0;
                minor = 0;
            }
        }

        return new BranchRef(branchType, major, minor);
    }

    public string GetBranchNameForChanges(string featureName) => BranchType switch
    {
        BranchType.support => $"patch/v{Major}.{Minor}/{featureName}",
        BranchType.deploy => $"patch/v{Major}.{Minor}.0/{featureName}",
        BranchType.stable => $"work/{featureName}",
        _ => "?"
    };

    public string GetRemoteBranchName() => BranchType switch
    {
        BranchType.support => $"support/v{Major}.{Minor}",
        BranchType.deploy => $"deploy/v{Major}.{Minor}.0",
        BranchType.stable => "stable",
        _ => "?"
    };

    public override string ToString() => GetRemoteBranchName();

    [GeneratedRegex("^([A-Za-z]*)\\/*(v{0,1}[\\d\\.]*)$")]
    private static partial Regex BranchRegex();
}
