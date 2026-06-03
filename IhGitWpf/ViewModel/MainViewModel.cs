using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using IhGitWpf.Properties;
using LibGit2Sharp;
using MaterialDesignThemes.Wpf;
using Octokit;
using Octokit.GraphQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Repository = LibGit2Sharp.Repository;

namespace IhGitWpf.ViewModel;

public sealed partial class MainViewModel : ObservableRecipient
{
    private const string PRODUCT = "IhGit";
    private const string ORGA = "airsphere-gmbh";
    private const string REPO = "PaxControl";
    private const long REPO_ID = 194316446;
    private const string DOWNMERGE_LABEL = "downmerge";
    private const string UPMERGE_LABEL = "upmerge";

    private int maxMajorVersion = 4;
    private int maxMinorVersion = 20;
    private int minMajorVersion = 4;
    private int minMinorVersion = 15;
    private bool hasDeploy;

    [ObservableProperty]
    private int minSupportMajorVersion = Settings.Default.MinSupportMajorVersion;

    partial void OnMinSupportMajorVersionChanged(int value)
    {
        Settings.Default.MinSupportMajorVersion = value;
    }

    [ObservableProperty]
    private int minSupportMinorVersion = Settings.Default.MinSupportMinorVersion;

    partial void OnMinSupportMinorVersionChanged(int value)
    {
        Settings.Default.MinSupportMinorVersion = value;
    }

    [ObservableProperty]
    private bool addToMergeQueue = Settings.Default.AddToMergeQueue;

    partial void OnAddToMergeQueueChanged(bool value)
    {
        Settings.Default.AddToMergeQueue = value;
    }

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(LoadPrCommand))]
    private string prNumber = "";

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(OpenGithubCommand))]
    private PullRequest? pr = null;

    [ObservableProperty]
    private ObservableCollectionEx<Reviewer> reviewers = [];

    [ObservableProperty]
    private ObservableCollectionEx<Label> labels = [];

    [ObservableProperty]
    private string reviewerFilter = "";

    [ObservableProperty]
    private string labelFilter = "";

    partial void OnReviewerFilterChanged(string value)
    {
        this.reviewerView?.Refresh();
    }

    [ObservableProperty]
    private string title = "";

    [ObservableProperty]
    private string body = "";

    [ObservableProperty]
    private string currentVersion = "";

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(UpMergeCommand)), NotifyCanExecuteChangedFor(nameof(DownMergeCommand))]
    private ObservableCollectionEx<Commit> commits = [];

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(UpMergeCommand))]
    private ObservableCollectionEx<BranchVersion> upMergeVersions = [];

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(DownMergeCommand))]
    private ObservableCollectionEx<BranchVersion> downMergeVersions = [];

    [ObservableProperty]
    private bool showZohoButton;

    [ObservableProperty]
    private ObservableCollection<string> logs = [];

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(OpenZohoCommand))]
    private string zohoUrl = "";

    [ObservableProperty]
    private string userName = Settings.Default.UserName;

    partial void OnUserNameChanged(string value)
    {
        Settings.Default.UserName = value;
    }

    [ObservableProperty]
    private string password = Settings.Default.Password;

    partial void OnPasswordChanged(string value)
    {
        Settings.Default.Password = value;
    }

    [ObservableProperty]
    private string gitHubToken = Settings.Default.GitHubToken;

    partial void OnGitHubTokenChanged(string value)
    {
        Settings.Default.GitHubToken = value;
    }

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(StatusCommand)), NotifyCanExecuteChangedFor(nameof(UpMergeCommand)), NotifyCanExecuteChangedFor(nameof(DownMergeCommand))]
    private string repoPath = Settings.Default.RepoPath;

    partial void OnRepoPathChanged(string value)
    {
        Settings.Default.RepoPath = value;
    }

    [ObservableProperty]
    private string externalEditorPath = Settings.Default.ExternalEditorPath;

    partial void OnExternalEditorPathChanged(string value)
    {
        Settings.Default.ExternalEditorPath = value;
    }

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(UpMergeCommand)), NotifyCanExecuteChangedFor(nameof(DownMergeCommand))]
    private string featureName = "";

    private readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
    private ListCollectionView? reviewerView;
    private ListCollectionView? labelView;
    private CancellationTokenSource progressCts = new();
    private ProgressDialogViewModel? progressDialogViewModel = null;

    [GeneratedRegex("I(\\d*)")]
    private static partial Regex ZohoTicketRegex();

    [RelayCommand]
    private void Loaded()
    {
    }

    #region Collections Notify Hack
    partial void OnUpMergeVersionsChanging(ObservableCollectionEx<BranchVersion>? oldValue, ObservableCollectionEx<BranchVersion> newValue)
    {
        if (oldValue is not null)
        {
            ((INotifyPropertyChanged)oldValue).PropertyChanged -= UpMergeVersionsChanged;
        }

        if (newValue is not null)
        {
            ((INotifyPropertyChanged)newValue).PropertyChanged += UpMergeVersionsChanged;
        }
    }

    partial void OnDownMergeVersionsChanged(ObservableCollectionEx<BranchVersion>? oldValue, ObservableCollectionEx<BranchVersion> newValue)
    {
        if (oldValue is not null)
        {
            ((INotifyPropertyChanged)oldValue).PropertyChanged -= DownMergeVersionsChanged;
        }

        if (newValue is not null)
        {
            ((INotifyPropertyChanged)newValue).PropertyChanged += DownMergeVersionsChanged;
        }
    }

    partial void OnCommitsChanging(ObservableCollectionEx<Commit>? oldValue, ObservableCollectionEx<Commit> newValue)
    {
        if (oldValue is not null)
        {
            ((INotifyPropertyChanged)oldValue).PropertyChanged -= CommitsChanged;
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

    private void UpMergeVersionsChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpMergeCommand.NotifyCanExecuteChanged();
    }

    private void DownMergeVersionsChanged(object? sender, PropertyChangedEventArgs e)
    {
        DownMergeCommand.NotifyCanExecuteChanged();
    }
    #endregion

    private bool IsPrNumberNumber() => int.TryParse(PrNumber, out _);

    [RelayCommand(CanExecute = nameof(IsPrNumberNumber))]
    private async Task LoadPr()
    {
        if (PrNumber == "0")
        {
            using var vm = new MergeConflictViewModel();
            vm.Items.Add(new()
            {
                Name = "main1.yml",
                Path = @".github\workflows\",
                NumberOfConflicts = 1,
                FullPath = @"D:\Entwicklung\GitHub\Projects\ActionsTest\.github\workflows\main.yml"
            });
            vm.Items.Add(new()
            {
                Name = "main1.yml",
                Path = @".github\workflows\",
                NumberOfConflicts = 3,
                FullPath = @"D:\Entwicklung\GitHub\Projects\ActionsTest\.github\workflows\main.yml"
            });
            vm.Items.Add(new()
            {
                Name = "main1.yml",
                Path = @".github\workflows\",
                NumberOfConflicts = 1,
                FullPath = @"D:\Entwicklung\GitHub\Projects\ActionsTest\.github\workflows\main.yml",
                DeletedOnRemote = true,
                RemoteName = "feature/MergeConflict"
            });
            var mergeConflict = new Dialogs.MergeConflict()
            {
                DataContext = vm
            };
            var res = await DialogHost.Show(mergeConflict);
            return;
        }
        else if (PrNumber == "01")
        {
            var vm = new ProgressDialogViewModel()
            {
                Step = "checkout",
                StartingBranch = CurrentBranchName(),
                CurrentBranchIndex = 1,
                CurrentCommitIndex = 2,
                IsUpmerge = true,
                Branches = ["feature/test", "support/v4.21", "stable"],
                Commits = ["commit1", "commit2", "commit3"],
            };
            var progressDialog = new Dialogs.ProgressDialog()
            {
                DataContext = vm
            };
            _ = await DialogHost.Show(progressDialog);
            return;
        }
        else if (PrNumber == "02")
        {
            var vm = new ProgressDialogViewModel()
            {
                Step = "checkout",
                StartingBranch = CurrentBranchName(),
                CurrentBranchIndex = 1,
                CurrentCommitIndex = 2,
                IsUpmerge = false,
                Branches = ["feature/test", "support/v4.21", "support/v4.20"],
                Commits = ["commit1", "commit2", "commit3"],
            };
            var progressDialog = new Dialogs.ProgressDialog()
            {
                DataContext = vm
            };
            _ = await DialogHost.Show(progressDialog);
            return;
        }

        if (!int.TryParse(PrNumber, out var prNum))
            return;

        ClearUi();

        var client = new GitHubClient(new Octokit.ProductHeaderValue(PRODUCT));
        var tokenAuth = new Octokit.Credentials(GitHubToken);
        client.Credentials = tokenAuth;

        //var all = await client.Repository.GetAllForOrg("airsphere-gmbh");

        try
        {
            Pr = await client.PullRequest.Get(REPO_ID, prNum);
        }
        catch (Exception ex) when (ex is Octokit.NotFoundException or AuthorizationException or ForbiddenException)
        {
            MessageBox.Show(ex.Message);
            return;
        }

        Title = Pr.Title;
        Body = Pr.Body;
        var a = await client.PullRequest.Commits(REPO_ID, prNum);
        var b = a.Select(x => new Commit(x)).ToArray();
        Commits = [.. b];

        var orgaUsers = await client.Organization.Member.GetAll("airsphere-gmbh");

        var requestedReviews = await client.PullRequest.ReviewRequest.Get(REPO_ID, prNum);
        var reviews = await client.PullRequest.Review.GetAll(REPO_ID, prNum);

        // Exclude commented, you be the author of the pr
        var allReviewers = Enumerable.Concat(requestedReviews.Users, reviews.Where(x => x.State.Value != PullRequestReviewState.Commented).Select(x => x.User)).Select(x => new Reviewer(x)).Distinct();

        Reviewers = [.. orgaUsers.Select(x =>
        {
            var reviewer = new Reviewer(x);
            reviewer.IsSelected = allReviewers.Contains(reviewer);

            return reviewer;
        })];

        reviewerView = (ListCollectionView)CollectionViewSource.GetDefaultView(Reviewers);
        reviewerView.IsLiveSorting = true;
        reviewerView.SortDescriptions.Add(new(nameof(Reviewer.IsSelected), ListSortDirection.Descending));
        reviewerView.Filter = FilterReviewers;

        var repoLabels = await client.Issue.Labels.GetAllForRepository(REPO_ID);

        var labels = await client.Issue.Labels.GetAllForIssue(REPO_ID, Pr.Number);

        // exclude up- and downmerge, they will be added later when merging
        Labels = [.. repoLabels
            .Where(x => !string.Equals(x.Name, UPMERGE_LABEL, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(x.Name, DOWNMERGE_LABEL, StringComparison.OrdinalIgnoreCase))
            .Select(x =>
            {
                var label = new Label(x);
                label.IsSelected = labels.Any(x => string.Equals(x.Name, label.ToString(), StringComparison.OrdinalIgnoreCase));
                return label;
            })];

        labelView = (ListCollectionView)CollectionViewSource.GetDefaultView(Labels);
        labelView.IsLiveSorting = true;
        labelView.SortDescriptions.Add(new(nameof(Label.IsSelected), ListSortDirection.Descending));
        labelView.Filter = FilterLabels;

        var index = Pr.Head.Ref.LastIndexOf('/');
        if (index == -1 || index == Pr.Head.Ref.Length - 1)
        {
            FeatureName = Pr.Head.Ref;
        }
        else
        {
            FeatureName = Pr.Head.Ref[(index + 1)..];
        }

        var regex = ZohoTicketRegex().Match(Title);
        ShowZohoButton = regex.Success;

        if (regex.Success)
        {
            ZohoUrl = $"https://sprints.zoho.eu/team/airsphere#itemdetails/P9/I{regex.Groups[1].Value}";
        }
        else
        {
            ZohoUrl = "";
        }

        var allBranches = await client.Repository.Branch.GetAll(REPO_ID);

        if (allBranches is not { Count: > 0 })
            return;

        var allBranchVersions = allBranches
            .Select(x => BranchVersion.TryParse(x.Name))
            .OfType<BranchVersion>(); //It kinda a bug that nullable tracking doesn't work for this -> .Where(x => x is not null);

        var allSupportBranches = allBranchVersions
            .Where(x => x.BranchType == BranchType.support);

        var deployBranch = allBranchVersions
            .Where(x => x.BranchType == BranchType.deploy)
            .MaxBy(x => x.Minor);

        hasDeploy = deployBranch is not null;

        maxMajorVersion = allSupportBranches.MaxBy(x => x.Major)?.Major ?? 0;
        maxMinorVersion = allSupportBranches.Where(x => x.Major == maxMajorVersion).MaxBy(x => x.Minor)?.Minor ?? 0;

        // TODO: major jumps, 4.00 -> 5.00 // 5.00 -> 4.00
        minMajorVersion = allSupportBranches.MinBy(x => x.Major)?.Major ?? 0;
        minMinorVersion = allSupportBranches.Where(x => x.Major == minMajorVersion).MinBy(x => x.Minor)?.Minor ?? 0;

        minMajorVersion = Math.Max(minMajorVersion, MinSupportMajorVersion);
        minMinorVersion = Math.Max(minMinorVersion, MinSupportMinorVersion);

        var prBranchVersion = new BranchVersion(Pr.Base.Ref);

        CurrentVersion = prBranchVersion.ToString();

        // UpMerge
        switch (prBranchVersion.BranchType)
        {
            case BranchType.support:
                List<BranchVersion> result = [];

                if (prBranchVersion.Minor < maxMinorVersion)
                {
                    // Count up all support versions
                    for (int i = prBranchVersion.Minor + 1; i <= maxMinorVersion; i++)
                        result.Add(new(prBranchVersion.Major, i));
                }

                if (deployBranch is not null)
                    result.Add(deployBranch);

                // Add stable
                result.Add(new(0, 0, BranchType.stable));

                UpMergeVersions = [.. result];
                break;

            case BranchType.deploy:
                UpMergeVersions = [new(0, 0, BranchType.stable)];
                break;

            case BranchType.stable:
            default:
                // Nothing todo
                break;
        }

        // DownMerge
        switch (prBranchVersion.BranchType)
        {
            case BranchType.support:
            case BranchType.deploy:
            case BranchType.stable:
                List<BranchVersion> result = [];

                var downMergeMaxMinorVersion = prBranchVersion.Minor;
                var downMergeMajorVersion = prBranchVersion.Major;

                if (prBranchVersion.BranchType == BranchType.stable)
                {
                    // We are on stable, so we need to also include the max support version
                    downMergeMaxMinorVersion = maxMinorVersion + 1;
                    downMergeMajorVersion = maxMajorVersion;

                    if (deployBranch is not null)
                        result.Add(deployBranch);
                }

                if (downMergeMaxMinorVersion > minMinorVersion)
                {
                    // Count up all support versions
                    for (int i = downMergeMaxMinorVersion - 1; i >= minMinorVersion; i--)
                        result.Add(new(downMergeMajorVersion, i));
                }

                DownMergeVersions = [.. result];
                break;

            default:
                // Nothing todo
                break;
        }
    }

    private bool FilterReviewers(object obj)
    {
        if (string.IsNullOrWhiteSpace(ReviewerFilter))
            return true;

        return obj is Reviewer reviewer && reviewer.ToString().Contains(ReviewerFilter, StringComparison.OrdinalIgnoreCase);
    }

    private bool FilterLabels(object obj)
    {
        if (string.IsNullOrWhiteSpace(LabelFilter))
            return true;

        return obj is Label label && label.ToString().Contains(LabelFilter, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private void ClearUi(bool clearAll = false)
    {
        Pr = null;
        Title = "";
        Body = "";
        Commits = [];
        UpMergeVersions = [];
        DownMergeVersions = [];
        FeatureName = "";
        ZohoUrl = "";
        ReviewerFilter = "";
        Reviewers.Clear();
        LabelFilter = "";
        Labels.Clear();
        CurrentVersion = "";

        progressDialogViewModel?.Dispose();
        progressDialogViewModel = null;

        if (clearAll)
        {
            PrNumber = "";
        }
    }

    [RelayCommand(CanExecute = nameof(CanOpenZoho))]
    private void OpenZoho()
    {
        OpenUrl(ZohoUrl);
    }

    private bool CanOpenZoho() => !string.IsNullOrWhiteSpace(ZohoUrl);

    [RelayCommand(CanExecute = nameof(CanOpenGithub))]
    private void OpenGithub()
    {
        if (Pr is null)
            return;

        OpenUrl(Pr.HtmlUrl);
    }

    private bool CanOpenGithub() => !string.IsNullOrWhiteSpace(Pr?.HtmlUrl);

    [RelayCommand]
    private void ClearLogs()
    {
        dispatcher.Invoke(Logs.Clear);
    }

    private bool CanUpmerge()
    {
        if (string.IsNullOrWhiteSpace(FeatureName))
            return false;

        if (!CanStatus())
            return false;

        if (UpMergeVersions is null || UpMergeVersions.Count == 0 || Commits is null || Commits.Count == 0)
            return false;

        return Commits.Any(x => x.IsSelected) && UpMergeVersions.Any(x => x.IsSelected && !x.IsCherryPicked);
    }

    [RelayCommand(CanExecute = nameof(CanUpmerge))]
    private async Task UpMerge()
    {
        progressDialogViewModel = new()
        {
            StartingBranch = CurrentBranchName(),
            Branches = [.. UpMergeVersions.Where(x => !x.IsCherryPicked && x.IsSelected).Select(x => x.GetRemoteBranchName())],
            Step = "Starting..."
        };
        progressCts = new();
        progressCts.Token.Register(() =>
        {
            if (DialogHost.IsDialogOpen(null))
                DialogHost.Close(null);
        });
        ShowProgressDialog();

        var watch = Stopwatch.StartNew();
        try
        {
            for (int i = 0; i < UpMergeVersions.Count; i++)
            {
                var version = UpMergeVersions[i];

                if (version.IsCherryPicked || !version.IsSelected)
                    continue;

                progressDialogViewModel.CurrentBranchIndex++;
                progressCts.Token.ThrowIfCancellationRequested();

                var success = await MergeOne(version, true, progressCts.Token);
                if (success)
                {
                    version.IsCherryPicked = true;
                }
                else
                {
                    var result = MessageBox.Show(
                        $"Upmerge of version {version} failed.\r\n" +
                        $"Yes: Retry\r\n" +
                        $"No: Skip this version\r\n" +
                        $"Cancel: Cancel the rest of the upmerge",
                        "Upmerge failed",
                        MessageBoxButton.YesNoCancel);
                    switch (result)
                    {
                        case MessageBoxResult.Cancel:
                            progressCts.Cancel();
                            return;
                        case MessageBoxResult.Yes:
                            i--;
                            break;
                        case MessageBoxResult.No:
                            progressDialogViewModel.CurrentBranchIndex++;
                            continue;
                    }
                }
            }
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == progressCts.Token)
        {
            Log("Upmerge cancelled by user");
        }
        watch.Stop();

        progressCts.Cancel();
        MessageBox.Show($"Upmerge finished in {watch.Elapsed.Humanize()} successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ShowProgressDialog()
    {
        _ = DialogHost.Show(new Dialogs.ProgressDialog() { DataContext = progressDialogViewModel }, ProgressDialogClosing);
    }

    private void ProgressDialogClosing(object sender, DialogClosingEventArgs args)
    {
        if (args.Parameter is false)
        {
            progressCts.Cancel();
        }
    }

    private bool CanDownmerge()
    {
        if (string.IsNullOrWhiteSpace(FeatureName))
            return false;

        if (!CanStatus())
            return false;

        if (DownMergeVersions is null || DownMergeVersions.Count == 0 || Commits is null || Commits.Count == 0)
            return false;

        return Commits.Any(x => x.IsSelected) && DownMergeVersions.Any(x => x.IsSelected && !x.IsCherryPicked);
    }

    [RelayCommand(CanExecute = nameof(CanDownmerge))]
    private async Task DownMerge()
    {
        progressDialogViewModel = new()
        {
            StartingBranch = CurrentBranchName(),
            Branches = [.. DownMergeVersions.Where(x => !x.IsCherryPicked && x.IsSelected).Select(x => x.GetRemoteBranchName())],
            Step = "Starting..."
        };
        progressCts = new();
        progressCts.Token.Register(() => DialogHost.Close(null));
        ShowProgressDialog();

        var watch = Stopwatch.StartNew();
        try
        {
            for (int i = 0; i < DownMergeVersions.Count; i++)
            {
                var version = DownMergeVersions[i];

                if (version.IsCherryPicked || !version.IsSelected)
                    continue;

                progressDialogViewModel.CurrentBranchIndex++;
                progressCts.Token.ThrowIfCancellationRequested();

                var success = await MergeOne(version, false, progressCts.Token);
                if (success)
                {
                    version.IsCherryPicked = true;
                }
                else
                {
                    var result = MessageBox.Show(
                        $"Downmerge of version {version} failed.\r\n" +
                        $"Yes: Retry\r\n" +
                        $"No: Skip this version\r\n" +
                        $"Cancel: Cancel the rest of the Downmerge",
                        "Downmerge failed",
                        MessageBoxButton.YesNoCancel);
                    switch (result)
                    {
                        case MessageBoxResult.Cancel:
                            progressCts.Cancel();
                            return;
                        case MessageBoxResult.Yes:
                            i--;
                            break;
                        case MessageBoxResult.No:
                            progressDialogViewModel.CurrentBranchIndex++;
                            continue;
                    }
                }
            }
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == progressCts.Token)
        {
            Log("Downmerge cancelled by user");
        }
        watch.Stop();
        MessageBox.Show($"Downmerge finished in {watch.Elapsed.Humanize()} successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task<bool> Fetch()
    {
        return await Git("fetch", "origin");
    }

    /// <summary>
    /// Runs git commands. If the command fails, shows a message box with the error and asks the user if it should retry or not.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    private async Task<bool> Git(params string[] args)
    {
        bool retry = true;
        while (retry)
        {
            var success = await RunGitCommand(args);
            if (success)
            {
                return true;
            }

            // Show error dialog and ask user to retry or abort
            var result = MessageBox.Show($"git {string.Join(' ', args)}", "Git command failed", MessageBoxButton.RetryCancel);

            switch (result)
            {
                case MessageBoxResult.Retry:
                    retry = true;
                    break;
                case MessageBoxResult.Cancel:
                default:
                    return false;
            }
        }

        return false;
    }

    private async Task<bool> RunGitCommand(params string[] args)
    {
        using Process process = Process.Start(new ProcessStartInfo("git", args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = RepoPath,
        })!;
        await foreach (ProcessOutputLine line in process.ReadAllLinesAsync())
        {
            if (line.StandardError)
                Log("ERROR " + line.Content);
            else
                Log(line.Content);
        }
        return process.ExitCode == 0;
    }

    private void Log(string msg)
    {
        dispatcher.Invoke(() => Logs.Add($"[{DateTime.Now:T}] {msg}"));
    }

    private async Task<bool> CreateAndSwitchBranch(string newBranchName, string remoteBranchName)
    {
        using var repo = new Repository(RepoPath);
        if (repo.Branches[newBranchName] is null)
        {
            // checkout a new branch from remote branch and switch to the new branch
            return await Git("checkout", "-b", newBranchName, $"origin/{remoteBranchName}", "--no-track");
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
            await Git("checkout", newBranch);
            using var repo = new Repository(RepoPath);
            return repo.Branches[newBranch] is not null;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, $"Error switching to branch '{newBranch}'");
            return false;
        }
    }

    private async Task<bool> MergeOne(BranchVersion mergeToVersion, bool isUpMerge, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var commits = Commits.Where(x => x.IsSelected).ToArray();

            progressDialogViewModel?.IsUpmerge = isUpMerge;
            progressDialogViewModel?.Commits = [.. commits.Select(x => x.ToString())];

            progressDialogViewModel?.Step = "Fetching";
            await Fetch();

            cancellationToken.ThrowIfCancellationRequested();

            progressDialogViewModel?.Step = "Switching branch";
            if (!await CreateAndSwitchBranch(mergeToVersion.GetBranchNameForChanges(FeatureName), mergeToVersion.ToString()))
                return false;

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var commit in commits)
            {
                var hasConflicts = false;
                progressDialogViewModel?.Step = "Checking for conflicts";
                if (HasConflicts())
                {
                    hasConflicts = true;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                else
                {
                    using var repo = new Repository(RepoPath);
                    var options = new CherryPickOptions()
                    {
                        CommitOnSuccess = true,
                    };

                    Log("Cherry pick: " + commit);
                    progressDialogViewModel?.Step = "Lookup commit";
                    progressDialogViewModel?.CurrentCommitIndex = progressDialogViewModel.CurrentCommitIndex + 1;

                    try
                    {
                        var sha = commit.Value.Sha;
                        var c = repo.Lookup<LibGit2Sharp.Commit>(sha);

                        cancellationToken.ThrowIfCancellationRequested();

                        if (c is null)
                        {
                            progressDialogViewModel?.Step = "Fetching origin";

                            if (await Git("fetch", "origin", sha))
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                progressDialogViewModel?.Step = "Lookup commit";
                                c = repo.Lookup<LibGit2Sharp.Commit>(sha);
                            }
                        }

                        if (c is null)
                        {
                            do
                            {
                                if (Pr is null || Pr.ClosedAt is null)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    MessageBox.Show($"Commit: {sha}", "Commit doesn't exist");
                                }
                                else
                                {
                                    var mbox = MessageBox.Show(
                                        $"It seems you PR was closed at {Pr.ClosedAt.Value.ToLocalTime()}.\r\n" +
                                        $"You need to restore the branch\r\n" +
                                        $"Commit: {sha}\r\n\r\n" +
                                        $"Yes: try to find commit again\r\n" +
                                        $"No: Skip cherry-pick commit\r\n" +
                                        $"Cancel: Abort",
                                        "Commit doesn't exist", MessageBoxButton.YesNoCancel);

                                    if (mbox == MessageBoxResult.Yes)
                                    {
                                        progressDialogViewModel?.Step = "Lookup commit";

                                        cancellationToken.ThrowIfCancellationRequested();
                                        c = repo.Lookup<LibGit2Sharp.Commit>(sha);
                                    }
                                    else if (mbox == MessageBoxResult.No)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                            }
                            while (c is null);
                        }

                        if (c is null)
                            continue;

                        cancellationToken.ThrowIfCancellationRequested();
                        progressDialogViewModel?.Step = "Cherry pick";
                        var result = repo.CherryPick(c, c.Author, options);
                        hasConflicts = result.Status == CherryPickStatus.Conflicts;

                        cancellationToken.ThrowIfCancellationRequested();
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
                    progressDialogViewModel?.Step = "Merge conflicts";

                    if (!await ResolveMergeConflicts(cancellationToken))
                        return false;
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
            await Push();
            await PullRequest(mergeToVersion, isUpMerge);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == progressCts.Token)
        {
            throw;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error during merge");
            return false;
        }
        return true;
    }

    private async Task<bool> ResolveMergeConflicts(CancellationToken cancellationToken)
    {
        if (!HasConflicts())
            return true;

        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var repo = new Repository(RepoPath);
            var conflicts = repo.Index.Conflicts.Cast<Conflict>();
            var files = Environment.NewLine + string.Join(Environment.NewLine, conflicts.Select(x => x?.Ancestor?.Path ?? ""));

            using var vm = new MergeConflictViewModel();
            foreach (var conflict in conflicts)
            {
                // conflict.Ours is the destination branch
                // conflict.Theirs is the branch is is currently being upmerged
                // If Ours is null, the file doesnt exist anymore on remote
                var entry = conflict.Ours ?? conflict.Theirs ?? conflict.Ancestor;
                var entryPath = entry?.Path ?? "";
                var name = Path.GetFileName(entryPath);
                var path = Path.GetDirectoryName(entryPath)?.Replace('\\', '/')?.TrimEnd('/');
                var fullPath = Path.Combine(RepoPath, entryPath).Replace('\\', '/');

                vm.Items.Add(new()
                {
                    Name = name,
                    Path = path,
                    RepoPath = RepoPath,
                    FullPath = fullPath,
                    RemoteName = repo.Head.FriendlyName,
                    DeletedOnRemote = conflict?.Ours is null,
                });
            }

            cancellationToken.ThrowIfCancellationRequested();

            var mergeConflict = new Dialogs.MergeConflict()
            {
                DataContext = vm
            };

            if (DialogHost.IsDialogOpen(null))
            {
                // Hide the progress dialog
                DialogHost.Close(null);
            }

            var res = await DialogHost.Show(mergeConflict);

            ShowProgressDialog();

            if (res is true)
            {
                foreach (var item in vm.Items.Where(x => x.DeletedOnRemoteAction != MergeConflictAction.None))
                {
                    var path = item.GitPath;
                    if (string.IsNullOrWhiteSpace(path))
                        continue;

                    Log($"Resolving conflict for {path} with action {item.DeletedOnRemoteAction}");

                    cancellationToken.ThrowIfCancellationRequested();
                    if (item.DeletedOnRemoteAction == MergeConflictAction.UseModifiedFile)
                    {
                        // Use modified file
                        await Git("add", path);
                    }
                    else if (item.DeletedOnRemoteAction == MergeConflictAction.DoNotIncludeFile)
                    {
                        // Use deleted file
                        await Git("rm", path);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                // cherry pick continue
                await Git("add", ".");

                cancellationToken.ThrowIfCancellationRequested();
                await Git("cherry-pick", "--continue");
            }
            else if (res is false)
            {
                await Git("cherry-pick", "--abort");
                cancellationToken.ThrowIfCancellationRequested();
                return false;
            }
        } while (HasConflicts());

        return true;
    }

    [RelayCommand(CanExecute = nameof(CanStatus))]
    private async Task Status()
    {
        await Git("status");
        await Fetch();
        await ResolveMergeConflicts(CancellationToken.None);
    }

    private bool CanStatus()
    {
        return RepoPath is not null && Directory.Exists(RepoPath);
    }

    private bool HasConflicts()
    {
        using var repo = new Repository(RepoPath);
        return !repo.Index.IsFullyMerged;
    }

    private async Task PullRequest(BranchVersion newBranchVersion, bool isUpMerge)
    {
        progressDialogViewModel?.Step = "Creating pull request";

        var client = new GitHubClient(new Octokit.ProductHeaderValue(PRODUCT));
        var tokenAuth = new Octokit.Credentials(GitHubToken);
        client.Credentials = tokenAuth;

        var title = string.IsNullOrWhiteSpace(Title)
            ? $"({newBranchVersion})"
            : $"{Title} ({newBranchVersion})";

        var branchName = newBranchVersion.GetRemoteBranchName();
        var newBranchName = newBranchVersion.GetBranchNameForChanges(FeatureName);

        var mergeLabel = isUpMerge ? UPMERGE_LABEL : DOWNMERGE_LABEL;

        var newLine = Body.EndsWith('\n') ? "" : "\n";
        var newBody = $"{Body}{newLine}<sub>IhGit {mergeLabel} from #{Pr?.Number}</sub>";

        var newPr = await client.PullRequest.Create(REPO_ID, new NewPullRequest(title, newBranchName, branchName)
        {
            Body = newBody,
        });

        if (newPr is null)
        {
            var line = new string('#', 20);
            Log(line);
            Log("Could not create PR");
            Log(line);
            return;
        }

        newPr = await client.PullRequest.ReviewRequest.Create(REPO_ID, newPr.Number, new PullRequestReviewRequest([.. Reviewers.Where(x => x.IsSelected).Select(x => x.User.Login)], []));
        await client.Issue.Labels.AddToIssue(REPO_ID, newPr.Number, [.. Labels.Where(x => x.IsSelected).Select(x => x.GithubLabel.Name), mergeLabel]);

        if (AddToMergeQueue)
        {
            progressDialogViewModel?.Step = "Adding to merge queue";
            try
            {
                await MergeQueue(newPr);
            }
            catch (Exception ex)
            {
                Log($"Adding to merge queue failed with: {ex.Message}");
            }
        }

        progressDialogViewModel?.Step = "Opening pull request url";
        try
        {
            OpenUrl(newPr.HtmlUrl);
        }
        catch (Exception ex)
        {
            Log($"Opening pull request url failed with: {ex.Message}");
        }
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

    private string CurrentBranchName()
    {
        using var repo = new Repository(RepoPath);
        return repo.Head.FriendlyName; // support/v4.17
    }

    private async Task CreateNewBranch(string name)
    {
        await Git("checkout", "-b", name);
    }

    record ErrorInfo(string ErrorTitle, string? ErrorMessage = null, bool ShowDialog = true);

    private async Task Push()
    {
        progressDialogViewModel?.Step = "Pushing";
        await Git("push", "-u", "origin", CurrentBranchName().ToString());
    }

    private async Task MergeQueue(PullRequest? inputPr)
    {
        if (inputPr is null)
            return;

        var connection = new Octokit.GraphQL.Connection(new(PRODUCT), GitHubToken);

        var hasMergeQueueQuery = new Query()
            .Repository(new(REPO), new(ORGA))
            .MergeQueue(new(inputPr.Base.Ref))
            .Select(x => x.Id);

        var hasMergeQueue = await connection.Run(hasMergeQueueQuery);
        if (hasMergeQueue is { Value.Length: > 0 })
        {
            var enable = new Mutation()
                .EnablePullRequestAutoMerge(new Octokit.GraphQL.Model.EnablePullRequestAutoMergeInput()
                {
                    PullRequestId = new(inputPr.NodeId),
                })
                .Select(x => x.PullRequest.Number);

            _ = await connection.Run(enable);
        }
        else
        {
            Log($"Found no merge queue for PR #{inputPr.Number} with base branch {inputPr.Base.Ref}");
        }
    }
}
