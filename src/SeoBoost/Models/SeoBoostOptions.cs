namespace SeoBoost.Models
{
    public class SeoBoostOptions
    {
        public bool UseTrailingSlash { get; set; } = true;
        public bool UseSimpleAddress { get; set; } = false;
        public bool CreateRobotsFile { get; set; } = false;
        public bool UseSiteUrlAsDefault { get; set; } = false;
        public string CustomCanonicalTagFieldName { get; set; } = "";
    }
}
