using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using System;
using System.Diagnostics;
using System.Drawing;
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
    public required partial string[] Commits { get; set; }

    [ObservableProperty]
    public required partial bool IsUpmerge { get; set; }

    /// <summary>
    /// The name of the current git operation, e.g. checkout, ...
    /// </summary>
    [ObservableProperty]
    public required partial string Step { get; set; }

    public int TotalBranches => Branches?.Length ?? 0;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(BranchPercent)), NotifyPropertyChangedFor(nameof(CurrentBranchName))]
    private int currentBranchIndex;

    public int BranchPercent => TotalBranches == 0 ? 0 : (int)((double)CurrentBranchIndex / TotalBranches * 100);

    public int TotalCommits => Commits?.Length ?? 0;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(CommitPercent)), NotifyPropertyChangedFor(nameof(CurrentCommit))]
    private int currentCommitIndex;

    public string CurrentBranchName => Branches is { Length: > 0 } ? Branches[CurrentBranchIndex] : "";

    public string CurrentCommit => Commits is { Length: > 0 } ? Commits[CurrentCommitIndex] : "";

    public int CommitPercent => TotalCommits == 0 ? 0 : (int)((double)CurrentCommitIndex / TotalCommits * 100);

    public string StartingBranch => Branches is { Length: > 0 } ? Branches[0] : "";

    public string EndingBranch => Branches is { Length: > 1 } ? Branches[^1] : "";

    public string StartingCommit => Commits is { Length: > 0 } ? Commits[0] : "";

    public string EndingCommit => Commits is { Length: > 1 } ? Commits[^1] : "";

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
