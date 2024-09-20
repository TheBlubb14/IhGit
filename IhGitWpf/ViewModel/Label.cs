using CommunityToolkit.Mvvm.ComponentModel;

namespace IhGitWpf.ViewModel;

public partial class Label : ObservableObject
{
    [ObservableProperty]
    private Octokit.Label githubLabel;

    [ObservableProperty]
    private bool isSelected = true;

    public Label(Octokit.Label label)
    {
        GithubLabel = label;
    }

    public override string ToString()
    => GithubLabel.Name;
}
