using CommunityToolkit.Mvvm.ComponentModel;
using Octokit;

namespace IhGitWpf.ViewModel;

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

    public override string ToString()
        => Value?.Sha ?? Title;
}
