using AdysTech.CredentialManager;
using CliWrap;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Web;

namespace IhGit
{
    public partial class Form1 : Form
    {
        const int max_support_version = 17;

        private string repoPath => textBoxRepo.Text;
        private string userName => textBoxUserName.Text;

        // https://github.com/libgit2/libgit2sharp
        //private Repository repo;

        public Form1()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await Git("status");
        }

        private async Task Git(params string[] arg)
        {
            if (checkBoxDryRun.Checked)
            {
                Log("Dry run: git " + string.Join(" ", arg));
                return;
            }

            var cmd = Cli.Wrap("git.exe")
                .WithArguments(arg)
                .WithWorkingDirectory(repoPath) | Log;
            await cmd.ExecuteAsync();

        }

        private void Log(string m)
        {
            textBoxOutput.Invoke(() => textBoxOutput.Text += m + Environment.NewLine);
        }

        private string? Fetch()
        {
            if (checkBoxDryRun.Checked)
            {
                Log("Dry run: git fetch origin");
                return null;
            }

            using var repo = new Repository(repoPath);

            var log = "";
            var remote = repo.Network.Remotes["origin"];
            var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);

            FetchOptions options = new()
            {
                CredentialsProvider = GetCredentialsHandler()
            };

            Commands.Fetch(repo, remote.Name, refSpecs, options, log);
            return string.IsNullOrWhiteSpace(log) ? null : log;
        }

        private CredentialsHandler GetCredentialsHandler()
        {
            var cred = CredentialManager.GetCredentials("git:https://github.com");
            return new CredentialsHandler((url, usernameFromUrl, types) =>
            new UsernamePasswordCredentials()
            {
                Username = userName,
                Password = cred.Password,
            });
        }

        private void CreateNewBranch(string name)
        {
            if (checkBoxDryRun.Checked)
            {
                Log($"Dry run: git checkout -b {name}");
                return;
            }

            using var repo = new Repository(repoPath);
            var branch = repo.CreateBranch(name);

            Commands.Checkout(repo, branch);
        }

        private void Push()
        {
            if (checkBoxDryRun.Checked)
            {
                Log($"Dry run: git push -u origin");
                return;
            }

            using var repo = new Repository(repoPath);
            PushOptions options = new()
            {
                CredentialsProvider = GetCredentialsHandler(),
            };
            repo.Network.Push(repo.Network.Remotes["origin"], repo.Head.CanonicalName, options);
        }

        // Select base branch                               support/v4.16
        // Input Branch name, without base branch           FastId
        // Input ordered list of commits to cherry pick     xxxx, yyyy
        // -- loop
        // Fetch next support branch up                     support/v4.17
        // Make new branch from up and checkout             patch/v4.17/FastId
        // Cherry pick commits                              xxxx, yyyy
        // Open PR Website                                  https://github.com/airsphere-gmbh/PaxControl/compare/support/v4.17...patch/v4.17/FastIdPatch?quick_pull=1&labels=upmerge
        // -- loop

        protected override void OnClosed(EventArgs e)
        {
            //repo.Dispose();
            base.OnClosed(e);
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            textBoxOutput.Clear();
        }

        // Only when we have already once a pr made, then we need to be in the next support branch
        private async void buttonUpmerge_Click(object sender, EventArgs e)
        {
            var commits = textBoxCommits.Text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            Fetch();
            await UpmergeOne(commits);
        }

        private async Task UpmergeOne(string[] commits)
        {
            var info = GetBranchInfo(checkBoxStartOnSameVersion.Checked);

            if (info is null)
                return;

            if (!await SwitchBranch(info.NewOrigin))
                return;

            CreateNewBranch(info.New);

            foreach (var commit in commits)
            {
                try
                {
                    await Git("cherry-pick", commit);
                }
                catch (Exception ex)
                {
                    var result = MessageBox.Show(ex.Message, "Cherry pick failed", MessageBoxButtons.OKCancel);

                    if (result == DialogResult.Cancel)
                        return;

                    await Git("cherry-pick", "--continue");
                }
            }
            Push();
            PullRequest();
        }

        private void PullRequest()
        {
            var info = GetBranchInfo(true);

            if (info is null)
                return;

            var title = "";
            var description = "";

            if (!string.IsNullOrWhiteSpace(textBoxTitle.Text))
            {
                title = "&title=" + HttpUtility.UrlEncode(textBoxTitle.Text);
            }

            if (!string.IsNullOrWhiteSpace(textBoxDescription.Text))
            {
                description = "&body=" + HttpUtility.UrlEncode(textBoxDescription.Text);
            }

            var branchName = info.Origin;
            var newBranchName = info.Current;
            OpenUrl($"https://github.com/airsphere-gmbh/PaxControl/compare/{branchName}...{newBranchName}?quick_pull=1&labels=upmerge{title}{description}");
        }

        private async Task<bool> SwitchBranch(string newBranch)
        {
            if (checkBoxDryRun.Checked)
            {
                Log($"Dry run: git switch {newBranch}");
                return true;
            }

            using var repo = new Repository(repoPath);
            var branch = repo.Branches[newBranch];

            if (branch is null)
            {
                await Git($"checkout", newBranch);
                branch = repo.Branches[newBranch];
            }

            if (branch is null)
                return false;

            return Commands.Checkout(repo, branch) is not null;
        }

        private void OpenUrl(string url)
        {
            if (checkBoxDryRun.Checked)
            {
                Log($"Dry run: open url {url}");
                return;
            }

            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }

        private string? NewBranch()
        {
            var info = GetBranchInfo(checkBoxStartOnSameVersion.Checked);

            if (info is null)
                return null;

            //using var repo = new Repository(repoPath);
            //var branchName = repo.Head.FriendlyName; // support/v4.17
            //var splitBranch = branchName.Split('/');

            //var isSupport = splitBranch[0] == "support";
            //var isStable = splitBranch[0] == "stable";
            //if (!isStable && !isSupport && !checkBoxDryRun.Checked)
            //    return null;

            //var featureName = textBoxFeatureName.Text?.Trim();
            //if (string.IsNullOrWhiteSpace(featureName))
            //    return null;

            //string? newBranchName;
            //if (isSupport)
            //{
            //    var version = splitBranch[1];
            //    newBranchName = $"patch/{version}/{featureName}";
            //}
            //else
            //{
            //    newBranchName = $"work/{featureName}";
            //}

            //var newBranchName = $"{info.New}/{featureName}";
            CreateNewBranch(info.New);
            return info.New;
        }

        private string CurrentBranchName()
        {
            using var repo = new Repository(repoPath);
            var branchName = repo.Head.FriendlyName; // support/v4.17
            return branchName;
        }

        private void buttonFetch_Click(object sender, EventArgs e)
        {
            Fetch();
        }

        private void buttonPush_Click(object sender, EventArgs e)
        {
            Push();
        }

        private void buttonNewBranch_Click(object sender, EventArgs e)
        {
            NewBranch();
        }

        private async void buttonPullRequestMultiple_Click(object sender, EventArgs e)
        {
            var commits = textBoxCommits.Text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            // support/v4.16
            BranchInfo? info = null;
            do
            {
                Fetch();
                info = GetBranchInfo(checkBoxStartOnSameVersion.Checked);

                if (info is null)
                    break;

                //if (!SwitchBranch(info.NewOrigin))
                //    break;

                await UpmergeOne(commits);

                if (checkBoxDryRun.Checked)
                    Log("Warning, will not walk through each upmerge in dry-run");

            } while (!info.IsStable && !checkBoxDryRun.Checked);
        }

        record BranchInfo(string Current, string New, bool IsStable, string Origin, string NewOrigin);
        private BranchInfo? GetBranchInfo(bool doNotCountVersionUp)
        {
            var currentBranch = CurrentBranchName();
            var currentBranchSplitted = currentBranch.Split("/", StringSplitOptions.RemoveEmptyEntries);

            var isStable = false;
            var version = 0;

            if (currentBranchSplitted.Length < 1)
            {
                if (currentBranchSplitted[0] == "stable")
                {
                    isStable = true;
                }
                else
                {
                    Log("Could not parse branch version");
                    return null;
                }
            }
            else if (currentBranchSplitted[0] == "work")
            {
                isStable = true;
            }
            else
            {
                var supportMatch = Regex.Match(currentBranchSplitted[1], "v4.(1\\d)");
                if (supportMatch.Success)
                {
                    version = int.Parse(supportMatch.Groups[1].Value) + 1;

                    if (doNotCountVersionUp)
                        version--;
                }
                else
                {
                    Log($"Could not parse branch version: {currentBranchSplitted[1]}");
                    return null;
                }
            }

            var shouldBaseOnStable = isStable || version > max_support_version;
            var originVersion = version - (doNotCountVersionUp ? 0 : 1);

            var featureName = textBoxFeatureName.Text?.Trim();
            if (string.IsNullOrWhiteSpace(featureName))
                return null;

            var newBranchName = shouldBaseOnStable ? "work" : $"patch/v4.{version}";
            newBranchName += $"/{featureName}";

            return new BranchInfo(currentBranch,
                newBranchName,
                isStable,
                shouldBaseOnStable ? "stable" : $"support/v4.{originVersion}",
                shouldBaseOnStable ? "stable" : $"support/v4.{version}");
        }

        private void buttonPullRequest_Click(object sender, EventArgs e)
        {
            PullRequest();
        }
    }
}