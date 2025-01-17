using Octokit;

namespace SS14.GithubApiHelper.Helpers;

/// <summary>
/// Manages github api clients for specific installations and renews them on retrieval when they have expired
/// </summary>
public sealed class GithubAppApiClientStore
{
    private const int InstallationExpirationInHours = 1;

    private readonly string _appName;
    
    /// <summary>
    /// The github api client authenticated as the github app for accessing global app data
    /// </summary>
    public GitHubClient AppClient { get; }
    private Dictionary<long, InstallationClient> _installationClients = new();

    public GithubAppApiClientStore(string appName, string keyFileLocation, int appId)
    {
        _appName = appName;
        var credentialStore = new GithubAppTokenCredentialStore(keyFileLocation, appId);
        
            
        AppClient = new GitHubClient(new ProductHeaderValue(appName), credentialStore);
    }

    /// <summary>
    /// Returns a github api client authorized for the specified installation id if it's present and not expired.
    /// Otherwise creates and returns a new client for that installation id while saving it inside a list if installation clients internally.
    /// </summary>
    /// <remarks>
    /// The api client authorized for a specific installation is used to access that installations repositories.
    /// </remarks>
    /// <param name="installationId">The installation id to retrieve the github api client for.</param>
    /// <returns>The github api client authorized for the specified installation</returns>
    public async Task<GitHubClient> GetInstallationClient(long installationId)
    {
        if (_installationClients.ContainsKey(installationId) && IsNotExpired(_installationClients[installationId]))
            return _installationClients[installationId].Client;
        
        var response = await AppClient.GitHubApps.CreateInstallationToken(installationId);
        
        var client = new GitHubClient(new ProductHeaderValue(_appName + "-Installation-" + installationId))
        {
            Credentials = new Credentials(response.Token)
        };

        var installationClient = new InstallationClient(response.ExpiresAt, client);
        _installationClients[installationId] = installationClient;
        return client;
    }

    public void ForgetInstallationClient(int installationId)
    {
        _installationClients.Remove(installationId);
    }

    /// <summary>
    /// Returns a new GitHub API client authenticated with the specified OAuth token.
    /// </summary>
    /// <remarks>
    /// This client is intended to be used for accessing GitHub resources authorized via the given OAuth token.
    /// </remarks>
    /// <example>
    ///     Basic use of <i>GetOauthClient</i> considering github oauth is set up and <see cref="Microsoft.AspNetCore.Http.HttpContextAccessor"/> is available
    ///     <code>
    ///     var token = await HttpContext.GetTokenAsync("access_token");
    ///     if (token != null)
    ///     {
    ///         var client = _clientStore.GetOauthClient(token);
    ///     ...
    ///     }
    ///     </code>
    /// </example>
    /// <param name="oauthToken">The OAuth token to use for authenticating the GitHub client.</param>
    /// <returns>A GitHub client authenticated with the specified OAuth token.</returns>
    public GitHubClient GetOauthClient(string oauthToken)
    {
        return new GitHubClient(new ProductHeaderValue(_appName))
        {
            Credentials = new Credentials(oauthToken)
        };
    }
    
    private static bool IsNotExpired(InstallationClient client)
    {
        return client.ExpiresAt.LocalDateTime.CompareTo(DateTime.Now) > 0;
    }
    
    private record InstallationClient(DateTimeOffset ExpiresAt, GitHubClient Client)
    {
        public readonly DateTimeOffset ExpiresAt = ExpiresAt;
        public readonly GitHubClient Client = Client;
    }
}