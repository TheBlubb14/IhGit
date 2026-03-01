using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using System;
using System.Diagnostics;
using System.Timers;

namespace IhGitWpf.ViewModel;

public partial class ProgressDialogViewModel : ObservableObject, IDisposable
{
    /// <summary>
    /// All branches to be upmerged, including the starting branch and target branch
    /// </summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TotalBranches)), NotifyPropertyChangedFor(nameof(BranchPercent)), NotifyPropertyChangedFor(nameof(StartingBranch))]
    public required partial string[] Branches { get; set; }

    /// <summary>
    /// All commits to be upmerged
    /// </summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TotalCommits)), NotifyPropertyChangedFor(nameof(CommitPercent)), NotifyPropertyChangedFor(nameof(StartingCommit))]
    public partial string[] Commits { get; set; } = [];

    [ObservableProperty]
    public partial bool IsUpmerge { get; set; }

    /// <summary>
    /// The name of the current git operation, e.g. checkout, ...
    /// </summary>
    [ObservableProperty]
    public required partial string Step { get; set; }

    public int TotalBranches => Branches?.Length ?? 0;

    /// <summary>
    /// Index starting from 1
    /// </summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(BranchPercent)), NotifyPropertyChangedFor(nameof(CurrentBranchName))]
    private int currentBranchIndex;

    public int BranchPercent => TotalBranches == 0 ? 0 : (int)((double)CurrentBranchIndex / TotalBranches * 100);

    public int TotalCommits => Commits?.Length ?? 0;

    /// <summary>
    /// Index starting from 1
    /// </summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CommitPercent)), NotifyPropertyChangedFor(nameof(CurrentCommit))]
    private int currentCommitIndex;

    public string CurrentBranchName => Branches is { Length: > 0 } ? Branches[Math.Clamp(CurrentBranchIndex - 1, 0, TotalBranches - 1)] : "";

    public string CurrentCommit => Commits is { Length: > 0 } ? Commits[Math.Clamp(CurrentCommitIndex - 1, 0, TotalCommits - 1)] : "";

    public int CommitPercent => TotalCommits == 0 ? 0 : (int)((double)Math.Clamp(CurrentCommitIndex, 0, TotalCommits) / TotalCommits * 100);

    /// <summary>
    /// The branch name were we start from. It is not included in the <see cref="Branches"/> list, and is only used for display.
    /// </summary>
    [ObservableProperty]
    public required partial string StartingBranch { get; set; }

    public string EndingBranch => Branches is { Length: > 0 } ? Branches[^1] : "";

    public string StartingCommit => Commits is { Length: > 0 } ? Commits[0] : "";

    public string EndingCommit => Commits is { Length: > 0 } ? Commits[^1] : "";

    public string ElapsedTime => watch.Elapsed.Humanize(2);

    private readonly Stopwatch watch;
    private readonly Timer timer;

    public ProgressDialogViewModel()
    {
        timer = new Timer(TimeSpan.FromMilliseconds(100))
        {
            AutoReset = true
        };
        timer.Elapsed += (s, e) => OnPropertyChanged(nameof(ElapsedTime));
        watch = Stopwatch.StartNew();
        timer.Start();
    }

    public void Dispose()
    {
        timer?.Stop();
        timer?.Dispose();
        watch?.Stop();
    }
}
