using CommunityToolkit.Mvvm.ComponentModel;

namespace IhGitWpf.ViewModel;

public partial class ProgressDialogViewModel : ObservableObject
{
    [ObservableProperty]
    public required partial string Step { get; set; }

    [ObservableProperty]
    public required partial string Branch {get; set;}


    [ObservableProperty, NotifyPropertyChangedFor(nameof(BranchPercent))]
    private int totalBranches;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(BranchPercent))]
    private int currentBranchIndex;

    public int BranchPercent => TotalBranches == 0 ? 0 : (int)((double)CurrentBranchIndex / TotalBranches * 100);


    [ObservableProperty, NotifyPropertyChangedFor(nameof(BranchPercent))]
    private int totalCommits;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(BranchPercent))]
    private int currentCommitIndex;

    public int CommitPercent => TotalCommits == 0 ? 0 : (int)((double)CurrentCommitIndex / TotalCommits * 100);
}
