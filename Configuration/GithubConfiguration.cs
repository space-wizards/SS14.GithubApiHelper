namespace SS14.GithubApiHelper.Configuration;

public sealed class GithubConfiguration
{
    public const string Name = "Github";

    public bool Enabled { get; set; } = true;
    public string? AppName { get; set; }
    public string? AppPrivateKeyLocation { get; set; }
    public int? AppId { get; set; }
    public string? TemplateLocation { get; set; }

    public RateLimitConfiguration RateLimit { get; set; } = new();
    
    public class RateLimitConfiguration
    {
        public int QueueLimit { get; set; } = 30;
        public int TokenLimit { get; set; } = 10;
        public int TokensPerPeriod { get; set; } = 3;
    }
}
