﻿using Microsoft.Extensions.Configuration;
using Octokit;
using SS14.GithubApiHelper.Helpers;
using SS14.MapServer.Configuration;
using SS14.MapServer.Exceptions;
using ILogger = Serilog.ILogger;

namespace SS14.GithubApiHelper.Services;

public abstract class GithubApiService
{
    protected readonly GithubAppApiClientStore? ClientStore;
    protected readonly IssueRateLimiterService RateLimiter;
    protected readonly GithubConfiguration Configuration = new();

    protected readonly ILogger Log;

    protected GithubApiService(IConfiguration configuration, IssueRateLimiterService rateLimiter)
    {
        configuration.Bind(GithubConfiguration.Name, Configuration);
        RateLimiter = rateLimiter;

        Log = Serilog.Log.ForContext(typeof(GithubApiService));

        if (!Configuration.Enabled)
            return;
        
        var productHeader = Configuration.AppName;
        if (productHeader == null)
            throw new ConfigurationException("The github app name needs to be configured. [Github:AppName]");
        
        var keyLocation = Configuration.AppPrivateKeyLocation;
        if (keyLocation == null)
            throw new ConfigurationException("The github private key location needs to be configured. [Github:AppPrivateKeyLocation]");

        var appId = Configuration.AppId;
        
        if (!appId.HasValue)
            throw new ConfigurationException("The github app id needs to be configured. [Github:AppId]");

        ClientStore = new GithubAppApiClientStore(productHeader, keyLocation, appId.Value);
    }

    /// <summary>
    /// Gets a list of all installations of this github app
    /// </summary>
    /// <remarks>
    /// Used for selecting which installation to configure in the administration for example.
    /// Refrain from iterating over all installations and creating clients for them. Use installation ids saved in the database instead.
    /// </remarks>
    /// <returns>List of installations</returns>
    public async Task<IReadOnlyList<Installation>> GetInstallations()
    {
        return await ClientStore.AppClient.GitHubApps.GetAllInstallationsForCurrent();
    }

    /// <summary>
    /// Gets a list of repositories the installation with the given id has access to.
    /// </summary>
    /// <remarks>
    /// Used for configuration in the administration interface
    /// </remarks>
    /// <param name="installationId">The installation id</param>
    /// <returns>A list of repositories the app has access to</returns>
    public async Task<RepositoriesResponse> GetRepositories(long installationId)
    {
        var client = await ClientStore.GetInstallationClient(installationId);
        return await client.GitHubApps.Installation.GetAllRepositoriesForCurrent();
    }
}