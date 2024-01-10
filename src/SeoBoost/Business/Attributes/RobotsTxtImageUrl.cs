using EPiServer.DataAnnotations;

public class RobotsTxtImageUrl : ImageUrlAttribute
{
    public RobotsTxtImageUrl() : base("/_content/images/page-robots.png")
    {
    }

    public RobotsTxtImageUrl(string path) : base(path)
    {
    }
}