using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Octokit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace IhGitWpf.ViewModel
{
    public enum BranchType
    {
        support,
        stable,
        deploy,
    }

    public partial class BranchVersion : ObservableObject
    {
        [ObservableProperty]
        private bool isSelected = true;

        [ObservableProperty]
        private BranchType branchType;

        [ObservableProperty]
        private int major;

        [ObservableProperty]
        private int minor;

        public BranchVersion(int major, int minor, BranchType branchType = BranchType.support)
        {
            BranchType = branchType;
            Major = major;
            Minor = minor;
        }

        public override string ToString()
        {
            return BranchType switch
            {
                BranchType.support => $"support/v{Major}.{Minor}",
                BranchType.deploy => $"deploy/v{Major}.{Minor}.0",
                BranchType.stable => "stable",
                _ => "?"
            };
        }
    }

    public partial class Commit : ObservableObject
    {
        [ObservableProperty]
        private PullRequestCommit value;

        [ObservableProperty]
        private bool isSelected = true;

        [ObservableProperty]
        private string title;

        public Commit(PullRequestCommit c)
        {
            Value = c;
            var s = c.Commit.Message.Split("\n")[0].Trim();
            Title = s;
        }
    }

    public sealed partial class MainViewModel : ObservableRecipient
    {
        [ObservableProperty]
        private int maxMajorVersion = 4;

        [ObservableProperty]
        private int maxMinorVersion = 19;

        [ObservableProperty]
        private bool maxVersionIsDeploy = true;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(LoadPrCommand))]
        private string prNumber;

        [ObservableProperty]
        private PullRequest pr;

        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private string body;

        [ObservableProperty]
        private IReadOnlyList<Commit> commits;

        [ObservableProperty]
        private BranchVersion[] versionsToConsider;

        [ObservableProperty]
        private bool showZohoButton;

        [ObservableProperty]
        private ObservableCollection<string> logs = new();

        private string zohoUrl = "";

        public MainViewModel()
        {
        }

        [RelayCommand]
        private void Loaded()
        {
        }

        private bool IsPrNumberNumber() => int.TryParse(PrNumber, out _);

        [RelayCommand(CanExecute = nameof(IsPrNumberNumber))]
        private async Task LoadPr()
        {
            if (!int.TryParse(PrNumber, out var prNum))
                return;

            VersionsToConsider = [];

            var client = new GitHubClient(new ProductHeaderValue("IhGit"));
            var tokenAuth = new Credentials("");
            client.Credentials = tokenAuth;

            //var all = await client.Repository.GetAllForOrg("airsphere-gmbh");
            const long repoId = 194316446;
            Pr = await client.PullRequest.Get(repoId, prNum);
            Title = Pr.Title;
            Body = Pr.Body;
            Commits = (await client.PullRequest.Commits(repoId, prNum)).Select(x => new Commit(x)).ToArray();

            var regex = ZohoTicketRegex().Match(Title);
            ShowZohoButton = regex.Success;

            if (regex.Success)
            {
                zohoUrl = $"https://sprints.zoho.eu/team/airsphere#itemdetails/P9/I{regex.Groups[1].Value}";
            }

            var r = Pr.Base.Ref;

            var matches = BranchRegex().Matches(r);
            var groups = matches.FirstOrDefault()?.Groups;

            if ((groups?.Count) != 3)
            {
                MessageBox.Show(r, "Base branch regex doesnt match");
                return;
            }

            var branch = groups[0].Value;
            var branchName = groups[1].Value;

            switch (branchName)
            {
                case "support":
                    var versionNumber = groups[2].Value?.Trim('v');
                    var split = versionNumber.Split('.', StringSplitOptions.RemoveEmptyEntries);
                    if (int.TryParse(split[0], out var major) && int.TryParse(split[1], out var minor))
                    {
                        List<BranchVersion> result = [];

                        if (minor + 1 <= MaxMinorVersion)
                        {
                            // Count up all support versions
                            for (int i = minor + 1; i < MaxMinorVersion; i++)
                                result.Add(new(major, i));
                        }

                        // Add max version
                        result.Add(new(major, MaxMinorVersion, MaxVersionIsDeploy ? BranchType.deploy : BranchType.support));

                        // Add stable
                        result.Add(new(0, 0, BranchType.stable));

                        VersionsToConsider = [.. result];
                    }
                    break;

                case "deploy":
                    VersionsToConsider = [new(0, 0, BranchType.stable)];
                    break;

                case "stable":
                default:
                    // Nothing todo
                    break;
            }
        }

        [RelayCommand]
        private void OpenZoho()
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = zohoUrl,
                UseShellExecute = true,
            });
        }

        [RelayCommand]
        private void ClearLogs()
        {
            Logs.Clear();
        }

        [GeneratedRegex("I(\\d*)")]
        private static partial Regex ZohoTicketRegex();

        [GeneratedRegex("^([A-Za-z]*)\\/*(v{0,1}[\\d\\.]*)$")]
        private static partial Regex BranchRegex();
    }
}
