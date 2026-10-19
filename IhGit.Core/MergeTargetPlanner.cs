namespace IhGit.Core;

public sealed record MergeTargets(
    IReadOnlyList<BranchRef> UpMerge,
    IReadOnlyList<BranchRef> DownMerge,
    int MaxMajor,
    int MaxMinor,
    int MinMajor,
    int MinMinor,
    bool HasDeploy);

public static class MergeTargetPlanner
{
    public static MergeTargets Plan(BranchRef baseBranch, IEnumerable<string> remoteBranchNames, int minSupportMajor, int minSupportMinor)
    {
        var all = remoteBranchNames.Select(BranchRef.TryParse).OfType<BranchRef>().ToList();

        var support = all.Where(x => x.BranchType == BranchType.support).ToList();
        var deployBranch = all.Where(x => x.BranchType == BranchType.deploy).MaxBy(x => x.Minor);

        var maxMajor = support.MaxBy(x => x.Major)?.Major ?? 0;
        var maxMinor = support.Where(x => x.Major == maxMajor).MaxBy(x => x.Minor)?.Minor ?? 0;

        // TODO: major jumps, 4.00 -> 5.00 // 5.00 -> 4.00
        var minMajor = support.MinBy(x => x.Major)?.Major ?? 0;
        var minMinor = support.Where(x => x.Major == minMajor).MinBy(x => x.Minor)?.Minor ?? 0;

        minMajor = Math.Max(minMajor, minSupportMajor);
        minMinor = Math.Max(minMinor, minSupportMinor);

        List<BranchRef> up = [];
        List<BranchRef> down = [];

        switch (baseBranch.BranchType)
        {
            case BranchType.support:
                for (int i = baseBranch.Minor + 1; i <= maxMinor; i++)
                    up.Add(new(BranchType.support, baseBranch.Major, i));

                if (deployBranch is not null)
                    up.Add(deployBranch);

                up.Add(new(BranchType.stable, 0, 0));
                break;

            case BranchType.deploy:
                up.Add(new(BranchType.stable, 0, 0));
                break;
        }

        if (baseBranch.BranchType is BranchType.support or BranchType.deploy or BranchType.stable)
        {
            var downMaxMinor = baseBranch.Minor;
            var downMajor = baseBranch.Major;

            if (baseBranch.BranchType == BranchType.stable)
            {
                // On stable, the max support version is also a target
                downMaxMinor = maxMinor + 1;
                downMajor = maxMajor;

                if (deployBranch is not null)
                    down.Add(deployBranch);
            }

            if (downMaxMinor > minMinor)
            {
                for (int i = downMaxMinor - 1; i >= minMinor; i--)
                    down.Add(new(BranchType.support, downMajor, i));
            }
        }

        return new MergeTargets(up, down, maxMajor, maxMinor, minMajor, minMinor, deployBranch is not null);
    }
}
