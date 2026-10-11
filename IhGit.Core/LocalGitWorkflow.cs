using LibGit2Sharp;

namespace IhGit.Core;

public enum CherryPickOutcome
{
    Applied,
    Conflicts,
    CommitNotFound,
    Failed,
}

public sealed record CherryPickResult(CherryPickOutcome Outcome, string? Message = null);

/// <summary>Local git operations of an up/downmerge, independent of any UI or GitHub access.</summary>
public static class LocalGitWorkflow
{
    public static string CurrentBranch(string repoPath)
    {
        using var repo = new Repository(repoPath);
        return repo.Head.FriendlyName;
    }

    public static bool HasConflicts(string repoPath)
    {
        using var repo = new Repository(repoPath);
        return !repo.Index.IsFullyMerged;
    }

    public static bool BranchExists(string repoPath, string branchName)
    {
        using var repo = new Repository(repoPath);
        return repo.Branches[branchName] is not null;
    }

    public static bool CommitExists(string repoPath, string sha)
    {
        using var repo = new Repository(repoPath);
        return repo.Lookup<Commit>(sha) is not null;
    }

    public static IReadOnlyList<string> RemoteBranchNames(string repoPath, string remoteName = "origin")
    {
        using var repo = new Repository(repoPath);
        var prefix = remoteName + "/";

        return [.. repo.Branches
            .Where(x => x.IsRemote && x.FriendlyName.StartsWith(prefix, StringComparison.Ordinal))
            .Select(x => x.FriendlyName[prefix.Length..])
            .Where(x => x != "HEAD")];
    }

    public static GitResult Fetch(string repoPath)
        => GitProcess.Run(repoPath, ["fetch", "origin", "--prune"]);

    public static GitResult CreateAndSwitchBranch(string repoPath, string newBranch, string remoteBranch)
        => BranchExists(repoPath, newBranch)
            ? GitProcess.Run(repoPath, GitCommands.Checkout(newBranch))
            : GitProcess.Run(repoPath, GitCommands.CheckoutNewBranchFromOrigin(newBranch, remoteBranch));

    public static CherryPickResult CherryPick(string repoPath, string sha)
    {
        try
        {
            using var repo = new Repository(repoPath);
            var commit = repo.Lookup<Commit>(sha);

            if (commit is null)
                return new(CherryPickOutcome.CommitNotFound);

            var result = repo.CherryPick(commit, commit.Author, new CherryPickOptions { CommitOnSuccess = true });

            return result.Status == CherryPickStatus.Conflicts
                ? new(CherryPickOutcome.Conflicts)
                : new(CherryPickOutcome.Applied);
        }
        catch (LibGit2SharpException ex)
        {
            return new(CherryPickOutcome.Failed, ex.Message);
        }
    }

    public static GitResult StageResolvedFile(string repoPath, string path)
        => GitProcess.Run(repoPath, ["add", path]);

    public static GitResult RemoveFile(string repoPath, string path)
        => GitProcess.Run(repoPath, ["rm", path]);

    public static GitResult ContinueCherryPick(string repoPath)
    {
        var add = GitProcess.Run(repoPath, GitCommands.AddAll);
        if (!add.Success)
            return add;

        return GitProcess.Run(repoPath, GitCommands.CherryPickContinue, new Dictionary<string, string> { ["GIT_EDITOR"] = "true" });
    }

    public static GitResult AbortCherryPick(string repoPath)
        => GitProcess.Run(repoPath, GitCommands.CherryPickAbort);

    public static GitResult Push(string repoPath)
        => GitProcess.Run(repoPath, GitCommands.Push(CurrentBranch(repoPath)));
}
