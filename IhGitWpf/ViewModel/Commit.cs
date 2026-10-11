using CommunityToolkit.Mvvm.ComponentModel;
using Octokit;

namespace IhGitWpf.ViewModel;

public partial class Commit : ObservableObject
{
    [ObservableProperty]
    private PullRequestCommit? value;

    [ObservableProperty]
    private bool isSelected = true;

    [ObservableProperty]
    private string title;

    public string Sha { get; }

    public Commit(PullRequestCommit c)
    {
        Value = c;
        Sha = c.Sha;
        var s = c.Commit.Message.Split("\n")[0].Trim();
        Title = s;
    }

    /// <summary>Commit of an offline scenario, there is no GitHub data behind it.</summary>
    public Commit(string sha, string title)
    {
        Sha = sha;
        Title = title;
    }

    public override string ToString()
        => Sha ?? Title;
}
