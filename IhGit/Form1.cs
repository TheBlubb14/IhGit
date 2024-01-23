using AdysTech.CredentialManager;
using CliWrap;
using CliWrap.Exceptions;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;

namespace IhGit
{
    public partial class Form1 : Form
    {
        private string repoPath => textBoxRepo.Text;
        private string userName => comboBoxUsername.Text;
        private string password => textBoxPassword.Text;

        // https://github.com/libgit2/libgit2sharp

        public Form1()
        {
            InitializeComponent();
            comboBoxUsername.Items.AddRange(new[] {
                "TheBlubb14",
                "as-spikechan",
                "fpfleide",
                "otabekgb",
                "tomwendel",
                "Gallimathias",
                "harutmik",
                "Matthias-Schw",
                "geraldgreiling",
            });
            comboBoxUsername.SelectedIndex = 0;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await Git(new("git status failed"), "status");
        }

        record ErrorInfo(string ErrorTitle, string? ErrorMessage = null, bool ShowDialog = true);
        private async Task<bool> Git(ErrorInfo errorInfo, params string[] arg)
        {
            if (checkBoxDryRun.Checked)
            {
                Log("Dry run: git " + string.Join(" ", arg));
                return true;
            }

            var retry = false;
            do
            {
                retry = false;
                var messages = new StringBuilder();
                try
                {
                    var pipe = PipeTarget.Merge(PipeTarget.ToDelegate(Log), PipeTarget.ToStringBuilder(messages));
                    var cmd = Cli.Wrap("git.exe")
                        .WithArguments(arg)
                        .WithStandardErrorPipe(pipe)
                        .WithStandardOutputPipe(pipe)
                        .WithWorkingDirectory(repoPath);
                    await cmd.ExecuteAsync();
                }
                catch (CommandExecutionException ex)
                {
                    if (!errorInfo.ShowDialog)
                        return false;

                    var msg = errorInfo.ErrorMessage is null ? messages.ToString() : errorInfo.ErrorMessage + Environment.NewLine + Environment.NewLine + messages.ToString();
                    var result = MessageBox.Show(msg, errorInfo.ErrorTitle, MessageBoxButtons.RetryCancel);

                    switch (result)
                    {
                        case DialogResult.Retry:
                            retry = true;
                            break;

                        case DialogResult.Cancel:
                        default:
                            return false;
                    }
                }
            } while (retry);

            return true;
        }

        private void Log(string m)
        {
            textBoxOutput.Invoke(() => textBoxOutput.Text += m + Environment.NewLine);
        }

        private void Fetch()
        {
            if (checkBoxDryRun.Checked)
            {
                Log("Dry run: git fetch origin");
                return;
            }

            using var repo = new Repository(repoPath);

            var log = "";
            var remote = repo.Network.Remotes["origin"];
            var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);

            FetchOptions options = new()
            {
                CredentialsProvider = GetCredentialsHandler()
            };

            if (options.CredentialsProvider is null)
                return;

            Commands.Fetch(repo, remote.Name, refSpecs, options, log);
            Log(log);
        }

        private CredentialsHandler? GetCredentialsHandler()
        {
            try
            {
                var pw = string.IsNullOrWhiteSpace(password) ? CredentialManager.GetCredentials("git:https://github.com")?.Password : password;

                if (pw is null)
                {
                    MessageBox.Show("Either provide one in the textbox or ensure 'git:https://github.com' is in the windows credential store", "Could not read password");
                    return null;
                }

                return new CredentialsHandler((url, usernameFromUrl, types) =>
                new UsernamePasswordCredentials()
                {
                    Username = userName,
                    Password = pw,
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot read 'git:https://github.com' from windows credential store{Environment.NewLine}{ex.Message}",
                    "Error reading git credtentials");
                return null;
            }
        }

        private async Task CreateNewBranch(string name)
        {
            if (checkBoxDryRun.Checked)
            {
                Log($"Dry run: git checkout -b {name}");
                return;
            }

            await Git(new("git checkout failed", $"git checkout -b {name} failed"), "checkout", "-b", name);
        }

        private async Task<bool> CreateAndSwitchBranch(string repoPath, string newBranchName, string remoteBranchName)
        {
            using var repo = new Repository(repoPath);
            if (repo.Branches[newBranchName] is null)
            {
                if (checkBoxDryRun.Checked)
                {
                    Log($"Dry run: git checkout -b {newBranchName} origin/{remoteBranchName}");
                    return true;
                }

                // checkout a new branch from remote branch and switch to the new branch
                return await Git(new("git checkout failed", $"git checkout -b {newBranchName} origin/{remoteBranchName} failed"), "checkout", "-b", newBranchName, $"origin/{remoteBranchName}", "--no-track");
            }
            else
            {
                // switch to new the branch
                return await SwitchBranch(newBranchName);
            }
        }

        private async Task Push()
        {
            if (checkBoxDryRun.Checked)
            {
                Log($"Dry run: git push -u origin");
                return;
            }

            await Git(new("git push failed", "git push -u origin failed"), "push", "-u", "origin", CurrentBranchName());
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

        private void buttonClear_Click(object sender, EventArgs e)
        {
            textBoxOutput.Clear();
        }

        // Only when we have already once a pr made, then we need to be in the next support branch
        private async void buttonUpmerge_Click(object sender, EventArgs e)
        {
            var commits = textBoxCommits.Text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                Fetch();
                await UpmergeOne(commits);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error during upmerge");
            }
        }

        private async Task<bool> UpmergeOne(string[] commits)
        {
            try
            {
                var info = GetBranchInfo(checkBoxStartOnSameVersion.Checked);

                if (info is null)
                    return false;

                if (!checkBoxUseCurrentBranch.Checked)
                {
                    Fetch();

                    if (!await CreateAndSwitchBranch(repoPath, info.New, info.NewOrigin))
                        return false;
                }

                foreach (var commit in commits)
                {
                    var hasConflicts = false;
                    if (HasConflicts())
                    {
                        hasConflicts = true;
                    }
                    else
                    {
                        if (checkBoxDryRun.Checked)
                        {
                            Log($"Dry run: git cherry-pick {commit}");
                        }
                        else
                        {
                            using var repo = new Repository(repoPath);
                            var options = new CherryPickOptions()
                            {
                                CommitOnSuccess = true,
                            };
                            Log("Cherry pick: " + commit);
                            try
                            {
                                var c = repo.Lookup<Commit>(commit);
                                var result = repo.CherryPick(c, c.Author, options);
                                hasConflicts = result.Status == CherryPickStatus.Conflicts;
                            }
                            catch (LibGit2SharpException ex)
                            {
                                Log(ex.Message);
                                MessageBox.Show(ex.Message);
                                return false;
                            }
                        }
                    }

                    if (hasConflicts)
                    {
                        do
                        {
                            using var repo = new Repository(repoPath);
                            var conflicts = repo.Index.Conflicts.Cast<Conflict>();
                            var files = Environment.NewLine + string.Join(Environment.NewLine, conflicts.Select(x => x.Ancestor.Path));
                            var mbox = MessageBox.Show("Waiting for merge conflicts to be solved.." + files, "cherry pick failed", MessageBoxButtons.CancelTryContinue);

                            switch (mbox)
                            {
                                case DialogResult.TryAgain:
                                    // try again
                                    break;

                                case DialogResult.Continue:
                                    // cherry pick continue
                                    await Git(new("add . failed", ShowDialog: false), "add", ".");
                                    await Git(new("cherry pick continue failed"), "cherry-pick", "--continue");
                                    break;

                                case DialogResult.Cancel:
                                default:
                                    return false;
                            }
                        } while (HasConflicts());
                    }
                }
                await Push();
                PullRequest();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error during upmerge");
                return false;
            }
            return true;
        }

        private bool HasConflicts()
        {
            if (checkBoxDryRun.Checked)
            {
                Log("Dry run: git diff --check");
                return false;
            }

            using var repo = new Repository(repoPath);
            return !repo.Index.IsFullyMerged;
        }

        private void PullRequest()
        {
            var info = GetBranchInfo(true);

            if (info is null)
                return;
            var description = "";

            var title = string.IsNullOrWhiteSpace(textBoxTitle.Text)
                ? "&title=" + HttpUtility.UrlEncode($"({info.PlainVersion})")
                : "&title=" + HttpUtility.UrlEncode($"{textBoxTitle.Text} ({info.PlainVersion})");

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

            try
            {
                await Git(new("checkout failed"), $"checkout", newBranch);
                using var repo = new Repository(repoPath);
                return repo.Branches[newBranch] is not null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, $"Error switching to branch '{newBranch}'");
                return false;
            }
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

        private async Task<string?> NewBranch()
        {
            var info = GetBranchInfo(checkBoxStartOnSameVersion.Checked);

            if (info is null)
                return null;

            await CreateNewBranch(info.New);
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

        private async void buttonPush_Click(object sender, EventArgs e)
        {
            await Push();
        }

        private async void buttonNewBranch_Click(object sender, EventArgs e)
        {
            await NewBranch();
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

                if (info is null || info.IsStable)
                    break;

                //if (!SwitchBranch(info.NewOrigin))
                //    break;

                if (!await UpmergeOne(commits))
                {
                    Log($"Upmerge failed. Aborting rest of the branches");
                    return;
                }

                Log("Waiting 5s for git operations to complete");
                await Task.Delay(TimeSpan.FromSeconds(5));

                if (checkBoxDryRun.Checked)
                    Log("Warning, will not walk through each upmerge in dry-run");

            } while (!info.IsStable && !checkBoxDryRun.Checked);
        }

        record BranchInfo(string Current, string New, bool IsStable, string Origin, string NewOrigin, string PlainVersion);
        private BranchInfo? GetBranchInfo(bool doNotCountVersionUp)
        {
            try
            {
                var currentBranch = CurrentBranchName();
                var currentBranchSplitted = currentBranch.Split("/", StringSplitOptions.RemoveEmptyEntries);

                var isStable = false;
                var version = 0;

                if (currentBranchSplitted.Length < 2)
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

                var maxVersion = (int)numericUpDownVersion.Value;

                var shouldBaseOnStable = isStable || version > maxVersion;
                var originVersion = version - (doNotCountVersionUp ? 0 : 1);

                var featureName = textBoxFeatureName.Text?.Trim();
                if (string.IsNullOrWhiteSpace(featureName))
                    return null;

                var nextDeploy = version == maxVersion && checkBoxDeploy.Checked;
                // deploy branch is always ending with .0, e.g. deploy/v4.19.0, so we need to append .0 at the end for deploy branch upmerge
                var nextDeployFeatureVersion = nextDeploy ? ".0" : "";

                var newBranchName = shouldBaseOnStable ? "work" : $"patch/v4.{version}{nextDeployFeatureVersion}";
                newBranchName += $"/{featureName}";

                var currentDeploy = originVersion == maxVersion && checkBoxDeploy.Checked;
                return new BranchInfo(currentBranch,
                    newBranchName,
                    isStable,
                    shouldBaseOnStable ? "stable" : currentDeploy ? $"deploy/v4.{originVersion}{nextDeployFeatureVersion}" : $"support/v4.{originVersion}",
                    shouldBaseOnStable ? "stable" : nextDeploy ? $"deploy/v4.{version}{nextDeployFeatureVersion}" : $"support/v4.{version}",
                    shouldBaseOnStable ? "stable" : $"4.{version}{nextDeployFeatureVersion}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error getting branch information");
                return null;
            }
        }

        private void buttonPullRequest_Click(object sender, EventArgs e)
        {
            PullRequest();
        }

        private void buttonDaniel_Click(object sender, EventArgs e)
        {
            comboBoxUsername.SelectedItem = "TheBlubb14";
            textBoxRepo.Text = "C:\\Dev\\Projects\\GitHub\\paxcontrol";
        }

        private void buttonSpike_Click(object sender, EventArgs e)
        {
            comboBoxUsername.SelectedItem = "as-spikechan";
            textBoxRepo.Text = "C:\\Users\\Spike\\Documents\\Github\\PaxControl";
        }
        private void buttonOtabek_Click(object sender, EventArgs e)
        {
            comboBoxUsername.SelectedItem = "otabekgb";
            textBoxRepo.Text = "D:\\Airsphere-Code\\PaxControl";
        }

        private void buttonGeneratePassword_Click(object sender, EventArgs e)
        {
            OpenUrl("https://github.com/settings/tokens/new?scopes=repo&description=Airsphere+IhGit");
            MessageBox.Show("Remember to save your token somewhere!");
        }

        private void buttonOpenRepo_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            dlg.AutoUpgradeEnabled = true;
            dlg.ShowNewFolderButton = false;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            textBoxRepo.Text = dlg.SelectedPath;
        }
    }
}