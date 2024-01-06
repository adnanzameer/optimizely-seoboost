namespace SeoBoost.Models
{
    public class SeoBoostOptions
    {
        public bool UseSimpleAddress { get; set; } = false;
        public bool EnableRobotsFileSupport { get; set; } = false;
        public bool UseSiteUrlAsDefault { get; set; } = false;
        public string CustomCanonicalTagFieldName { get; set; } = "";
    }
}
