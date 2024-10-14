using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace IhGitWpf.ViewModel;

public partial class BranchVersion : ObservableObject
{
    [ObservableProperty]
    private bool isSelected = true;

    [ObservableProperty]
    private bool isCherryPicked;

    [ObservableProperty]
    private BranchType branchType;

    [ObservableProperty]
    private int major;

    [ObservableProperty]
    private int minor;

    public static BranchVersion? TryParse(string branchVersion)
    {
        var matches = BranchRegex().Matches(branchVersion);
        var groups = matches.FirstOrDefault()?.Groups;

        if (groups is null || groups == null || groups.Count != 3)
            return null;

        var branch = groups[0].Value;
        var branchName = groups[1].Value;

        var branchType = branchName switch
        {
            "support" => BranchType.support,
            "deploy" => BranchType.deploy,
            "stable" => BranchType.stable,
            _ => BranchType.unknown,
        };

        int major = 0;
        int minor = 0;

        if (branchType == BranchType.support || branchType == BranchType.deploy)
        {
            var versionNumber = groups[2].Value.Trim('v');
            var split = versionNumber.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (split.Length > 0 && int.TryParse(split[0], out major) && int.TryParse(split[1], out minor))
            { }
        }

        return new BranchVersion(major, minor, branchType);
    }

    public BranchVersion(string branchVersion)
    {
        var parsed = TryParse(branchVersion);

        if (parsed is null)
        {
            MessageBox.Show(branchVersion, "Base branch regex doesn't match");
            return;
        }

        BranchType = parsed.BranchType;
        Major = parsed.Major;
        Minor = parsed.Minor;
    }

    public BranchVersion(int major, int minor, BranchType branchType = BranchType.support)
    {
        BranchType = branchType;
        Major = major;
        Minor = minor;
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

    public override string ToString()
        => GetRemoteBranchName();

    [GeneratedRegex("^([A-Za-z]*)\\/*(v{0,1}[\\d\\.]*)$")]
    private static partial Regex BranchRegex();
}
