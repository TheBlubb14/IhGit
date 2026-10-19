using IhGit.Core;
using IhGit.Core.Scenarios;

namespace IhGit.Tests;

public class ScenarioTests
{
    private string root = "";

    [SetUp]
    public void SetUp() => root = Path.Combine(Path.GetTempPath(), "ihgit-tests-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void TearDown()
    {
        if (!Directory.Exists(root)) return;
        foreach (var d in Directory.GetDirectories(root))
            if (File.Exists(Path.Combine(d, ScenarioManifest.MarkerFileName)))
                ScenarioGuard.DeleteOwnedDirectory(d);
        try { Directory.Delete(root); } catch (IOException) { }
    }

    private (ScenarioManifest Manifest, string Dir, string Repo) Create(string name)
    {
        var dir = Path.Combine(root, name + "-" + Guid.NewGuid().ToString("N")[..6]);
        var manifest = ScenarioBuilder.Create(name, dir);
        return (manifest, dir, Path.Combine(dir, manifest.RepoDirectory));
    }

    private static string G(string dir, params string[] args) => GitProcess.RunOrThrow(dir, args);

    [Test]
    public void Catalog_scenarios_all_validate_and_start_on_expected_branch()
    {
        foreach (var info in ScenarioBuilder.Catalog)
        {
            var (m, dir, repo) = Create(info.Name);
            Assert.That(LocalGitWorkflow.CurrentBranch(repo), Is.EqualTo(m.StartBranch));
            Assert.That(m.Commits, Is.Not.Empty);
            Assert.That(m.Commits.All(c => LocalGitWorkflow.CommitExists(repo, c.Sha)), Is.True);
            ScenarioGuard.Validate(Path.Combine(dir, ScenarioManifest.FileName));
            ScenarioGuard.RevalidateRemote(m);
        }
    }

    [Test]
    public void Generation_is_deterministic()
    {
        var a = Create("clean-multi");
        var dir2 = Path.Combine(Path.GetDirectoryName(a.Dir)!, "clean-multi-2");
        var b = ScenarioBuilder.Create("clean-multi", dir2);
        Assert.That(b.Commits.Select(c => c.Sha), Is.EqualTo(a.Manifest.Commits.Select(c => c.Sha)));
    }

    [Test]
    public void Non_empty_destination_is_refused()
    {
        var (_, dir, _) = Create("clean-single");
        Assert.That(() => ScenarioBuilder.Create("clean-single", dir), Throws.Exception);
    }

    [Test]
    public void Clean_cherry_pick_applies()
    {
        var (m, _, repo) = Create("clean-single");
        LocalGitWorkflow.Fetch(repo);
        Assert.That(LocalGitWorkflow.CreateAndSwitchBranch(repo, "merge/test", "support/v4.18").Success, Is.True);
        Assert.That(LocalGitWorkflow.CherryPick(repo, m.Commits[0].Sha).Outcome, Is.EqualTo(CherryPickOutcome.Applied));
        Assert.That(LocalGitWorkflow.HasConflicts(repo), Is.False);
    }

    [Test]
    public void Content_conflict_is_detected_and_can_be_aborted()
    {
        var (m, _, repo) = Create("content-conflict");
        Assert.That(LocalGitWorkflow.CreateAndSwitchBranch(repo, "merge/test", "support/v4.18").Success, Is.True);
        Assert.That(LocalGitWorkflow.CherryPick(repo, m.Commits[0].Sha).Outcome, Is.EqualTo(CherryPickOutcome.Conflicts));
        Assert.That(LocalGitWorkflow.HasConflicts(repo), Is.True);
        Assert.That(LocalGitWorkflow.AbortCherryPick(repo).Success, Is.True);
        Assert.That(LocalGitWorkflow.HasConflicts(repo), Is.False);
    }

    [Test]
    public void Resolved_conflict_can_be_continued()
    {
        var (m, _, repo) = Create("content-conflict");
        LocalGitWorkflow.CreateAndSwitchBranch(repo, "merge/test", "support/v4.18");
        LocalGitWorkflow.CherryPick(repo, m.Commits[0].Sha);
        var conflicted = G(repo, "diff", "--name-only", "--diff-filter=U")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.That(conflicted, Is.Not.Empty);
        foreach (var file in conflicted)
        {
            File.WriteAllText(Path.Combine(repo, file), "resolved\n");
            Assert.That(LocalGitWorkflow.StageResolvedFile(repo, file).Success, Is.True);
        }
        var cont = LocalGitWorkflow.ContinueCherryPick(repo);
        Assert.That(cont.Success, Is.True, cont.Message);
        Assert.That(LocalGitWorkflow.HasConflicts(repo), Is.False);
    }

    [TestCase("modify-delete-destination-deleted")]
    [TestCase("delete-modify-source-deleted")]
    public void Delete_conflicts_are_detected(string name)
    {
        var (m, _, repo) = Create(name);
        LocalGitWorkflow.CreateAndSwitchBranch(repo, "merge/test", "support/v4.18");
        Assert.That(LocalGitWorkflow.CherryPick(repo, m.Commits[0].Sha).Outcome, Is.EqualTo(CherryPickOutcome.Conflicts));
        Assert.That(LocalGitWorkflow.HasConflicts(repo), Is.True);
    }

    [TestCase("staged-changes")]
    [TestCase("unstaged-changes")]
    public void Dirty_tree_blocks_checkout(string name)
    {
        var (_, _, repo) = Create(name);
        var r = LocalGitWorkflow.CreateAndSwitchBranch(repo, "merge/test", "support/v4.18");
        Assert.That(r.Success, Is.False);
    }

    [Test]
    public void Untracked_files_do_not_block()
    {
        var (m, _, repo) = Create("untracked-files");
        Assert.That(LocalGitWorkflow.CreateAndSwitchBranch(repo, "merge/test", "support/v4.18").Success, Is.True);
        Assert.That(LocalGitWorkflow.CherryPick(repo, m.Commits[0].Sha).Outcome, Is.EqualTo(CherryPickOutcome.Applied));
    }

    [Test]
    public void Already_applied_commit_is_not_a_conflict()
    {
        var (m, _, repo) = Create("already-applied");
        LocalGitWorkflow.CreateAndSwitchBranch(repo, "merge/test", "support/v4.18");
        var r = LocalGitWorkflow.CherryPick(repo, m.Commits[0].Sha);
        Assert.That(LocalGitWorkflow.HasConflicts(repo), Is.False);
        Assert.That(r.Outcome is CherryPickOutcome.Applied or CherryPickOutcome.Failed, Is.True, r.Outcome + ": " + r.Message);
    }

    [Test]
    public void Cherry_pick_in_progress_reports_conflicts()
    {
        var (_, _, repo) = Create("cherry-pick-in-progress");
        Assert.That(LocalGitWorkflow.HasConflicts(repo), Is.True);
    }

    [Test]
    public void Push_updates_only_the_local_bare_origin()
    {
        var (m, dir, repo) = Create("clean-single");
        LocalGitWorkflow.CreateAndSwitchBranch(repo, "merge/test", "support/v4.18");
        LocalGitWorkflow.CherryPick(repo, m.Commits[0].Sha);
        var push = LocalGitWorkflow.Push(repo);
        Assert.That(push.Success, Is.True, push.Message);
        var originPath = Path.Combine(dir, m.OriginDirectory);
        var refs = G(dir, "--git-dir", originPath, "branch", "--list", "merge/test");
        Assert.That(refs, Does.Contain("merge/test"));
    }

    [Test]
    public void Guard_rejects_network_remote()
    {
        var (m, _, repo) = Create("clean-single");
        G(repo, "remote", "set-url", "origin", "https://example.com/x.git");
        Assert.That(() => ScenarioGuard.RevalidateRemote(m), Throws.Exception);
    }

    [Test]
    public void Guard_rejects_wrong_token()
    {
        var (_, dir, _) = Create("clean-single");
        File.WriteAllText(Path.Combine(dir, ScenarioManifest.MarkerFileName), "other");
        Assert.That(() => ScenarioGuard.Validate(Path.Combine(dir, ScenarioManifest.FileName)), Throws.Exception);
    }

    [Test]
    public void Local_request_store_round_trips()
    {
        var (m, _, _) = Create("clean-single");
        Assert.That(LocalRequestStore.Read(m), Is.Empty);
        var r = LocalRequestStore.Append(m, "t", "src", "support/v4.18", true, ["abc"]);
        Assert.That(LocalRequestStore.Read(m), Has.Count.EqualTo(1));
        Assert.That(r, Is.Not.Null);
    }
}

public class PlannerTests
{
    private static readonly string[] Branches =
    [
        "support/v4.15", "support/v4.16", "support/v4.17", "support/v4.18", "stable", "deploy/v4.20.0",
    ];

    [TestCase("support/v4.17", BranchType.support, 4, 17)]
    [TestCase("deploy/v4.20", BranchType.deploy, 4, 20)]
    [TestCase("stable", BranchType.stable, 0, 0)]
    [TestCase("main", BranchType.unknown, 0, 0)]
    [TestCase("support/", BranchType.support, 0, 0)]
    public void BranchRef_parses(string name, BranchType type, int major, int minor)
    {
        var b = BranchRef.TryParse(name);
        Assert.That(b, Is.EqualTo(new BranchRef(type, major, minor)));
    }

    [TestCase("feature/ABC-123")]
    [TestCase("patch/v4.17/x")]
    public void BranchRef_rejects_non_version_branches(string name) => Assert.That(BranchRef.TryParse(name), Is.Null);

    [Test]
    public void Planner_produces_targets_above_and_below()
    {
        var plan = MergeTargetPlanner.Plan(BranchRef.TryParse("support/v4.17")!, Branches, 4, 15);
        Assert.That(plan.UpMerge, Is.Not.Empty);
        Assert.That(plan.MaxMajor, Is.EqualTo(4));
    }
}
