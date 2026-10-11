using System.Security.Cryptography;
using System.Text;

namespace IhGit.Core.Scenarios;

public sealed record ScenarioInfo(string Name, string Description);

/// <summary>
/// Creates disposable, deterministic git repositories (a working clone plus a local bare origin)
/// containing only synthetic content, for reproducing up/downmerge situations.
/// </summary>
public static class ScenarioBuilder
{
    private const string FeatureName = "ABC-123-sample";
    private const string FeatureBranch = "feature/" + FeatureName;
    private const string TargetBranch = "support/v4.17";
    private static readonly DateTimeOffset BaseDate = new(2024, 1, 1, 8, 0, 0, TimeSpan.Zero);

    private sealed record Definition(
        string Description,
        string Outcome,
        Action<SeedRepo> Feature,
        Action<SeedRepo>? Target18 = null,
        bool WithDeploy = false,
        Action<string, SeedRepo>? PrepareWorkingClone = null);

    private static readonly Dictionary<string, Definition> definitions = new()
    {
        ["clean-single"] = new(
            "One new file; cherry-picks cleanly onto every target.",
            ScenarioOutcomes.Applied,
            r =>
            {
                r.Write("src/feature.txt", "feature v1\n");
                r.Commit("Add feature file");
            }),

        ["clean-multi"] = new(
            "Three commits (add, modify, add) that cherry-pick cleanly in order.",
            ScenarioOutcomes.Applied,
            r =>
            {
                r.Write("src/feature.txt", "feature v1\n");
                r.Commit("Add feature file");
                r.Write("src/app.txt", "app line 1\napp line 2\napp line 3 (feature)\napp line 4\napp line 5\n");
                r.Commit("Change app line 3");
                r.Write("src/feature-extra.txt", "extra\n");
                r.Commit("Add extra file");
            }),

        ["clean-with-deploy"] = new(
            "Clean cherry-pick with a deploy/v4.20.0 branch between the support branches and stable.",
            ScenarioOutcomes.Applied,
            r =>
            {
                r.Write("src/feature.txt", "feature v1\n");
                r.Commit("Add feature file");
            },
            WithDeploy: true),

        ["content-conflict"] = new(
            "Both the request and support/v4.18 change the same line of src/config.txt.",
            ScenarioOutcomes.Conflicts,
            r =>
            {
                r.Write("src/config.txt", "setting.a=1\nsetting.b=feature\nsetting.c=3\n");
                r.Commit("Change setting.b");
            },
            Target18: r =>
            {
                r.Write("src/config.txt", "setting.a=1\nsetting.b=hotfix\nsetting.c=3\n");
                r.Commit("Hotfix setting.b");
            }),

        ["modify-delete-destination-deleted"] = new(
            "The request modifies src/legacy.txt, which support/v4.18 deleted.",
            ScenarioOutcomes.Conflicts,
            r =>
            {
                r.Write("src/legacy.txt", "legacy 1\nlegacy 2 (feature)\nlegacy 3\n");
                r.Commit("Modify legacy file");
            },
            Target18: r =>
            {
                r.Delete("src/legacy.txt");
                r.Commit("Remove legacy file");
            }),

        ["delete-modify-source-deleted"] = new(
            "The request deletes src/obsolete.txt, which support/v4.18 modified.",
            ScenarioOutcomes.Conflicts,
            r =>
            {
                r.Delete("src/obsolete.txt");
                r.Commit("Remove obsolete file");
            },
            Target18: r =>
            {
                r.Write("src/obsolete.txt", "obsolete 1\nobsolete 2 (hotfix)\nobsolete 3\n");
                r.Commit("Touch obsolete file");
            }),

        ["already-applied"] = new(
            "support/v4.18 already contains the identical change made by the request.",
            ScenarioOutcomes.AlreadyApplied,
            r =>
            {
                r.Write("src/app.txt", "app line 1\napp line 2\napp line 3 (feature)\napp line 4\napp line 5\n");
                r.Commit("Change app line 3");
            },
            Target18: r =>
            {
                r.Write("src/app.txt", "app line 1\napp line 2\napp line 3 (feature)\napp line 4\napp line 5\n");
                r.Commit("Same change applied directly");
            }),

        ["staged-changes"] = new(
            "Clean request, but CHANGELOG.txt has staged local edits that block switching to the target.",
            ScenarioOutcomes.CheckoutBlocked,
            CleanFeature,
            PrepareWorkingClone: (path, r) =>
            {
                r.Write("CHANGELOG.txt", "Release v4.15\nlocal edit\n", path);
                r.Git(path, "add", "CHANGELOG.txt");
            }),

        ["unstaged-changes"] = new(
            "Clean request, but CHANGELOG.txt has unstaged local edits that block switching to the target.",
            ScenarioOutcomes.CheckoutBlocked,
            CleanFeature,
            PrepareWorkingClone: (path, r) => r.Write("CHANGELOG.txt", "Release v4.15\nlocal edit\n", path)),

        ["untracked-files"] = new(
            "Clean request with an unrelated untracked file in the working directory.",
            ScenarioOutcomes.Applied,
            CleanFeature,
            PrepareWorkingClone: (path, r) => r.Write("scratch.txt", "scratch\n", path)),

        ["cherry-pick-in-progress"] = new(
            "A conflicting cherry-pick onto support/v4.18 has already started in the working clone.",
            ScenarioOutcomes.ConflictInProgress,
            r =>
            {
                r.Write("src/config.txt", "setting.a=1\nsetting.b=feature\nsetting.c=3\n");
                r.Commit("Change setting.b");
            },
            Target18: r =>
            {
                r.Write("src/config.txt", "setting.a=1\nsetting.b=hotfix\nsetting.c=3\n");
                r.Commit("Hotfix setting.b");
            },
            PrepareWorkingClone: (path, r) =>
            {
                r.Git(path, "checkout", "-b", $"patch/v4.18/{FeatureName}", "origin/support/v4.18", "--no-track");

                var sha = r.Git(path, "rev-parse", $"origin/{FeatureBranch}");
                r.GitUnchecked(path, "cherry-pick", sha);

                if (string.IsNullOrEmpty(r.Git(path, "ls-files", "-u")))
                    throw new InvalidOperationException("Expected the cherry-pick to leave conflicts");
            }),
    };

    public static IReadOnlyList<ScenarioInfo> Catalog
        => [.. definitions.Select(x => new ScenarioInfo(x.Key, x.Value.Description))];

    public static ScenarioManifest Create(string name, string destination)
    {
        if (!definitions.TryGetValue(name, out var definition))
            throw new ScenarioValidationException($"Unknown scenario '{name}'. Known scenarios: {string.Join(", ", definitions.Keys)}");

        var root = Path.GetFullPath(destination);

        if (File.Exists(root) || (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any()))
            throw new ScenarioValidationException($"'{root}' already exists and is not empty. Choose a new directory.");

        Directory.CreateDirectory(root);

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        try
        {
            File.WriteAllText(Path.Combine(root, ScenarioManifest.MarkerFileName), token);
            return Build(name, definition, root, token);
        }
        catch
        {
            // The directory was empty or created above, so everything inside is ours.
            ScenarioGuard.DeleteOwnedDirectory(root);
            throw;
        }
    }

    private static void CleanFeature(SeedRepo r)
    {
        r.Write("src/feature.txt", "feature v1\n");
        r.Commit("Add feature file");
    }

    private static ScenarioManifest Build(string name, Definition definition, string root, string token)
    {
        var emptyConfig = Path.Combine(root, ".isolated-gitconfig");
        File.WriteAllText(emptyConfig, "");

        var repo = new SeedRepo(emptyConfig);
        var seed = Path.Combine(root, "seed");
        var origin = Path.Combine(root, "origin.git");
        var work = Path.Combine(root, "work");

        Directory.CreateDirectory(seed);
        repo.Git(seed, "init", "-b", "support/v4.15");
        repo.Git(seed, "config", "core.autocrlf", "false");

        repo.Seed = seed;
        repo.Write(".gitattributes", "* text=auto eol=lf\n");
        repo.Write("CHANGELOG.txt", "Release v4.15\n");
        repo.Write("src/app.txt", "app line 1\napp line 2\napp line 3\napp line 4\napp line 5\n");
        repo.Write("src/config.txt", "setting.a=1\nsetting.b=2\nsetting.c=3\n");
        repo.Write("src/legacy.txt", "legacy 1\nlegacy 2\nlegacy 3\n");
        repo.Write("src/obsolete.txt", "obsolete 1\nobsolete 2\nobsolete 3\n");
        repo.Commit("Initial import");

        for (var minor = 16; minor <= 19; minor++)
        {
            repo.Git(seed, "checkout", "-b", $"support/v4.{minor}");
            repo.Write("CHANGELOG.txt", File.ReadAllText(Path.Combine(seed, "CHANGELOG.txt")) + $"Release v4.{minor}\n");
            repo.Commit($"Release v4.{minor}");

            if (minor == 18)
                definition.Target18?.Invoke(repo);
        }

        if (definition.WithDeploy)
        {
            repo.Git(seed, "checkout", "-b", "deploy/v4.20.0");
            repo.Write("CHANGELOG.txt", File.ReadAllText(Path.Combine(seed, "CHANGELOG.txt")) + "Deploy v4.20.0\n");
            repo.Commit("Deploy v4.20.0");
        }

        repo.Git(seed, "checkout", "-b", "stable");
        repo.Write("CHANGELOG.txt", File.ReadAllText(Path.Combine(seed, "CHANGELOG.txt")) + "Stable\n");
        repo.Commit("Stable");

        repo.Git(seed, "checkout", TargetBranch);
        repo.Git(seed, "checkout", "-b", FeatureBranch);
        definition.Feature(repo);

        var commits = repo.Git(seed, "rev-list", "--reverse", "--format=%H%x09%s", $"{TargetBranch}..{FeatureBranch}")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(x => !x.StartsWith("commit ", StringComparison.Ordinal))
            .Select(x => x.Split('\t', 2))
            .Select(x => new ScenarioCommit(x[0].Trim(), x[1].Trim()))
            .ToList();

        Directory.CreateDirectory(origin);
        repo.Git(origin, "init", "--bare", "-b", TargetBranch);
        repo.Git(seed, "push", origin, "--all");

        DeleteDirectory(seed);

        repo.Git(root, "clone", origin, work);
        repo.Git(work, "config", "user.name", "IhGit Scenario");
        repo.Git(work, "config", "user.email", "scenario@ihgit.invalid");
        repo.Git(work, "config", "core.autocrlf", "false");
        repo.Git(work, "config", "core.editor", "true");
        repo.Git(work, "config", "commit.gpgsign", "false");
        repo.Git(work, "config", "core.hooksPath", Path.Combine(root, "no-hooks"));

        definition.PrepareWorkingClone?.Invoke(work, repo);

        var manifest = new ScenarioManifest
        {
            Name = name,
            Description = definition.Description,
            OwnerToken = token,
            TargetBranch = TargetBranch,
            SourceBranch = FeatureBranch,
            FeatureName = FeatureName,
            Title = "ABC-123 Sample change",
            Body = $"Synthetic request generated by IhGit scenario '{name}'.",
            StartBranch = repo.Git(work, "rev-parse", "--abbrev-ref", "HEAD"),
            Commits = commits,
            ExpectedFirstOutcome = definition.Outcome,
            DirectoryPath = root,
        };

        manifest.Write();
        return manifest;
    }

    private static void DeleteDirectory(string path)
        => ScenarioGuard.DeleteOwnedDirectory(path);

    internal sealed class SeedRepo(string isolatedConfig)
    {
        private int counter;

        public string Seed { get; set; } = "";

        private Dictionary<string, string> Environment()
        {
            var date = BaseDate.AddMinutes(counter).ToString("yyyy-MM-ddTHH:mm:ss'+0000'");

            return new()
            {
                ["GIT_CONFIG_GLOBAL"] = isolatedConfig,
                ["GIT_CONFIG_NOSYSTEM"] = "1",
                ["GIT_AUTHOR_NAME"] = "IhGit Scenario",
                ["GIT_AUTHOR_EMAIL"] = "scenario@ihgit.invalid",
                ["GIT_COMMITTER_NAME"] = "IhGit Scenario",
                ["GIT_COMMITTER_EMAIL"] = "scenario@ihgit.invalid",
                ["GIT_AUTHOR_DATE"] = date,
                ["GIT_COMMITTER_DATE"] = date,
                ["GIT_EDITOR"] = "true",
            };
        }

        public string Git(string workingDirectory, params string[] args)
            => GitProcess.RunOrThrow(workingDirectory, args, Environment());

        public void GitUnchecked(string workingDirectory, params string[] args)
            => GitProcess.Run(workingDirectory, args, Environment());

        public void Write(string relativePath, string content, string? directory = null)
        {
            var path = Path.Combine(directory ?? Seed, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }

        public void Delete(string relativePath)
            => File.Delete(Path.Combine(Seed, relativePath));

        public void Commit(string message)
        {
            counter++;
            Git(Seed, "add", "-A");
            Git(Seed, "commit", "-m", message);
        }
    }
}
