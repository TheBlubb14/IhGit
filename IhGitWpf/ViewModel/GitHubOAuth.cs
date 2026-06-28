using Octokit;

namespace IhGitWpf.ViewModel;

public record GitHubOAuth(string? AccessToken)
{
    public GitHubOAuth() : this(AccessToken: null)
    {

    }

    public GitHubOAuth(OauthToken oauthToken) : this(oauthToken.AccessToken)
    {
    }
}
