using AdysTech.CredentialManager;
using CliWrap.Exceptions;
using CliWrap;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Octokit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using Repository = LibGit2Sharp.Repository;
using System.ComponentModel;
using System.Collections.Specialized;

namespace IhGitWpf.ViewModel;

public class ObservableCollectionEx<T> : ObservableCollection<T>, INotifyPropertyChanged where T : INotifyPropertyChanged
{
    public ObservableCollectionEx() : base()
    {
        Init();
    }

    public ObservableCollectionEx(IEnumerable<T> collection) : base(collection)
    {
        Init();
    }

    private void Init()
    {
        CollectionChanged += OnCollectionChanged;
        foreach (var item in Items)
        {
            if (item is not null)
                item.PropertyChanged += ItemOnPropertyChanged;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (T item in e.NewItems)
            {
                if (item is not null)
                    item.PropertyChanged += ItemOnPropertyChanged;
            }
        }

        if (e.OldItems is not null)
        {
            foreach (T item in e.OldItems)
            {
                if (item is not null)
                    item.PropertyChanged -= ItemOnPropertyChanged;
            }
        }
    }

    private void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}

public enum BranchType
{
    unknown,
    support,
    stable,
    deploy,
}

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

    public BranchVersion(string branchVersion)
    {
        var matches = BranchRegex().Matches(branchVersion);
        var groups = matches.FirstOrDefault()?.Groups;

        if ((groups?.Count) != 3)
        {
            MessageBox.Show(branchVersion, "Base branch regex doesnt match");
            return;
        }

        var branch = groups[0].Value;
        var branchName = groups[1].Value;

        var versionNumber = groups[2].Value?.Trim('v');
        var split = versionNumber.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (int.TryParse(split[0], out major) && int.TryParse(split[1], out var minor))
        {
            Major = major;
            Minor = minor;
        }

        BranchType = branchName switch
        {
            "support" => BranchType.support,
            "deploy" => BranchType.deploy,
            "stable" => BranchType.stable,
            _ => BranchType.unknown,
        };
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

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(UpMergeCommand))]
    private ObservableCollectionEx<Commit> commits = [];

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(UpMergeCommand))]
    private ObservableCollectionEx<BranchVersion> versionsToConsider = [];

    [ObservableProperty]
    private bool showZohoButton;

    [ObservableProperty]
    private ObservableCollection<string> logs = new();

    private string zohoUrl = "";

    [ObservableProperty]
    private string userName = "TheBlubb14";

    [ObservableProperty]
    private string password;

    [ObservableProperty]
    private string repoPath = "C:\\Dev\\Projects\\GitHub\\paxcontrol";

    private string FeatureName => "myFeaturew";

    public MainViewModel()
    {
    }

    [RelayCommand]
    private void Loaded()
    {
    }

    partial void OnVersionsToConsiderChanging(ObservableCollectionEx<BranchVersion>? oldValue, ObservableCollectionEx<BranchVersion> newValue)
    {
        if (oldValue is not null)
        {
            ((INotifyPropertyChanged)newValue).PropertyChanged -= VersionsToConsiderChanged;
        }

        if (newValue is not null)
        {
            ((INotifyPropertyChanged)newValue).PropertyChanged += VersionsToConsiderChanged;
        }
    }

    partial void OnCommitsChanging(ObservableCollectionEx<Commit>? oldValue, ObservableCollectionEx<Commit> newValue)
    {
        if (oldValue is not null)
        {
            ((INotifyPropertyChanged)newValue).PropertyChanged -= CommitsChanged;
        }

        if (newValue is not null)
        {
            ((INotifyPropertyChanged)newValue).PropertyChanged += CommitsChanged;
        }
    }

    private void CommitsChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpMergeCommand.NotifyCanExecuteChanged();
    }

    private void VersionsToConsiderChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpMergeCommand.NotifyCanExecuteChanged();
    }

    private bool IsPrNumberNumber() => int.TryParse(PrNumber, out _);

    [RelayCommand(CanExecute = nameof(IsPrNumberNumber))]
    private async Task LoadPr()
    {
        if (!int.TryParse(PrNumber, out var prNum))
            return;

        VersionsToConsider = [];

        var client = new GitHubClient(new ProductHeaderValue("IhGit"));
        var tokenAuth = new Octokit.Credentials("");
        client.Credentials = tokenAuth;

        //var all = await client.Repository.GetAllForOrg("airsphere-gmbh");
        const long repoId = 194316446;
        Pr = await client.PullRequest.Get(repoId, prNum);
        Title = Pr.Title;
        Body = Pr.Body;
        var a = await client.PullRequest.Commits(repoId, prNum);
        var b = a.Select(x => new Commit(x)).ToArray();
        Commits = [..b];

        var regex = ZohoTicketRegex().Match(Title);
        ShowZohoButton = regex.Success;

        if (regex.Success)
        {
            zohoUrl = $"https://sprints.zoho.eu/team/airsphere#itemdetails/P9/I{regex.Groups[1].Value}";
        }

        var prBranchVersion = new BranchVersion(Pr.Base.Ref);



        switch (prBranchVersion.BranchType)
        {
            case BranchType.support:
                List<BranchVersion> result = [];

                if (prBranchVersion.Minor + 1 <= MaxMinorVersion)
                {
                    // Count up all support versions
                    for (int i = prBranchVersion.Minor + 1; i < MaxMinorVersion; i++)
                        result.Add(new(prBranchVersion.Major, i));
                }

                // Add max version
                result.Add(new(prBranchVersion.Major, MaxMinorVersion, MaxVersionIsDeploy ? BranchType.deploy : BranchType.support));

                // Add stable
                result.Add(new(0, 0, BranchType.stable));

                VersionsToConsider = [.. result];
                break;

            case BranchType.deploy:
                VersionsToConsider = [new(0, 0, BranchType.stable)];
                break;

            case BranchType.stable:
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

    private bool CanUpmerge()
    {
        if (VersionsToConsider is null || VersionsToConsider.Count == 0 || Commits is null || Commits.Count == 0)
            return false;

        return Commits.Any(x => x.IsSelected) && VersionsToConsider.Any(x => x.IsSelected && !x.IsCherryPicked);
    }

    [RelayCommand(CanExecute = nameof(CanUpmerge))]
    private async Task UpMerge()
    {
        for (int i = 0; i < VersionsToConsider.Count; i++)
        {
            var version = VersionsToConsider[i];

            if (version.IsCherryPicked || version.IsCherryPicked)
                continue;

            var success = await UpmergeOne(version);
            if (success)
            {
                version.IsCherryPicked = true;
            }
            else
            {
                var result = MessageBox.Show($"Upmerge of version {version} failed.\r\nYes: Retry\r\nNo: Skip this version\r\nCancel: Cancel the rest of the upmerge", "Upmerge failed", MessageBoxButton.YesNoCancel);
                switch (result)
                {
                    case MessageBoxResult.Cancel:
                        return;
                    case MessageBoxResult.Yes:
                        i--;
                        break;
                    case MessageBoxResult.No:
                        continue;
                }
            }
        }
    }

    [GeneratedRegex("I(\\d*)")]
    private static partial Regex ZohoTicketRegex();

    private CredentialsHandler? GetCredentialsHandler()
    {
        try
        {
            var pw = string.IsNullOrWhiteSpace(Password) ? CredentialManager.GetCredentials("git:https://github.com")?.Password : Password;

            if (pw is null)
            {
                MessageBox.Show("Either provide one in the textbox or ensure 'git:https://github.com' is in the windows credential store", "Could not read password");
                return null;
            }

            return new CredentialsHandler((url, usernameFromUrl, types) =>
            new UsernamePasswordCredentials()
            {
                Username = UserName,
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

    private void Fetch()
    {
        using var repo = new Repository(RepoPath);

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

    private void Log(string msg)
    {
        Logs.Add(msg);
    }
    private async Task<bool> CreateAndSwitchBranch(string newBranchName, string remoteBranchName)
    {
        using var repo = new Repository(RepoPath);
        if (repo.Branches[newBranchName] is null)
        {
            // checkout a new branch from remote branch and switch to the new branch
            return await Git(new("git checkout failed", $"git checkout -b {newBranchName} origin/{remoteBranchName} failed"), "checkout", "-b", newBranchName, $"origin/{remoteBranchName}", "--no-track");
        }
        else
        {
            // switch to new the branch
            return await SwitchBranch(newBranchName);
        }
    }

    private async Task<bool> SwitchBranch(string newBranch)
    {
        try
        {
            await Git(new("checkout failed"), $"checkout", newBranch);
            using var repo = new Repository(RepoPath);
            return repo.Branches[newBranch] is not null;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, $"Error switching to branch '{newBranch}'");
            return false;
        }
    }

    private async Task<bool> UpmergeOne(BranchVersion upmergeToVersion)
    {
        try
        {
            Fetch();
            if (!await CreateAndSwitchBranch(upmergeToVersion.GetBranchNameForChanges(FeatureName), upmergeToVersion.ToString()))
                return false;

            foreach (var commit in Commits.Where(x => x.IsSelected).ToArray())
            {
                var hasConflicts = false;
                if (HasConflicts())
                {
                    hasConflicts = true;
                }
                else
                {
                    using var repo = new Repository(RepoPath);
                    var options = new CherryPickOptions()
                    {
                        CommitOnSuccess = true,
                    };
                    Log("Cherry pick: " + commit);
                    try
                    {
                        var c = repo.Lookup<LibGit2Sharp.Commit>(commit.Value.Sha);
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

                if (hasConflicts)
                {
                    do
                    {
                        using var repo = new Repository(RepoPath);
                        var conflicts = repo.Index.Conflicts.Cast<Conflict>();
                        var files = Environment.NewLine + string.Join(Environment.NewLine, conflicts.Select(x => x.Ancestor.Path));
                        var mbox = MessageBox.Show("Waiting for merge conflicts to be solved..\r\n Yes: try-again\r\nNo: cherry-pick --continue\r\nCancel: Abort" + files, "cherry pick failed", MessageBoxButton.YesNoCancel);

                        switch (mbox)
                        {
                            case MessageBoxResult.Yes:
                                // try again
                                break;

                            case MessageBoxResult.No:
                                // cherry pick continue
                                await Git(new("add . failed", ShowDialog: false), "add", ".");
                                await Git(new("cherry pick continue failed"), "cherry-pick", "--continue");
                                break;

                            case MessageBoxResult.Cancel:
                            default:
                                return false;
                        }
                    } while (HasConflicts());
                }
            }
            await Push();
            PullRequest(upmergeToVersion);
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
        using var repo = new Repository(RepoPath);
        return !repo.Index.IsFullyMerged;
    }

    private void PullRequest(BranchVersion newBranchVersion)
    {
        var description = "";

        var title = string.IsNullOrWhiteSpace(Title)
            ? "&title=" + HttpUtility.UrlEncode($"({newBranchVersion})")
            : "&title=" + HttpUtility.UrlEncode($"{Title} ({newBranchVersion})");

        if (!string.IsNullOrWhiteSpace(Body))
        {
            description = "&body=" + HttpUtility.UrlEncode(Body);
        }

        var branchName = newBranchVersion.GetRemoteBranchName();
        var newBranchName = newBranchVersion.GetBranchNameForChanges(FeatureName);
        OpenUrl($"https://github.com/airsphere-gmbh/PaxControl/compare/{branchName}...{newBranchName}?quick_pull=1&labels=upmerge{title}{description}");
    }

    private void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }

    private async Task<string> NewBranch(BranchVersion newBranchVersion)
    {
        var s = newBranchVersion.ToString();
        await CreateNewBranch(s);
        return s;
    }

    private BranchVersion CurrentBranchName()
    {
        using var repo = new Repository(RepoPath);
        var branchName = repo.Head.FriendlyName; // support/v4.17
        return new BranchVersion(branchName);
    }

    private async Task CreateNewBranch(string name)
    {
        await Git(new("git checkout failed", $"git checkout -b {name} failed"), "checkout", "-b", name);
    }

    record ErrorInfo(string ErrorTitle, string? ErrorMessage = null, bool ShowDialog = true);
    private async Task<bool> Git(ErrorInfo errorInfo, params string[] arg)
    {
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
                    .WithWorkingDirectory(RepoPath);
                await cmd.ExecuteAsync();
            }
            catch (CommandExecutionException ex)
            {
                if (!errorInfo.ShowDialog)
                    return false;

                var msg = errorInfo.ErrorMessage is null ? messages.ToString() : errorInfo.ErrorMessage + Environment.NewLine + Environment.NewLine + messages.ToString();
                var result = MessageBox.Show("Ok: Retry\r\nCancel: Abort\r\n\r\n" + msg, errorInfo.ErrorTitle, MessageBoxButton.OKCancel);

                switch (result)
                {
                    case MessageBoxResult.OK:
                        retry = true;
                        break;

                    case MessageBoxResult.Cancel:
                    default:
                        return false;
                }
            }
        } while (retry);

        return true;
    }

    private async Task Push()
    {
        await Git(new("git push failed", "git push -u origin failed"), "push", "-u", "origin", CurrentBranchName().ToString());
    }

}
