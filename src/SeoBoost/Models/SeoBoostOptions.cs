namespace SeoBoost.Models
{
    public class SeoBoostOptions
    {
        public bool UseSimpleAddressAsPath { get; set; } = false;
        public bool UseMirrorPageReference { get; set; } = true;
        public bool EnableRobotsTxtSupport { get; set; } = false;
        public bool UseSiteUrlAsHost { get; set; } = false;
        public string CustomCanonicalTagFieldName { get; set; } = "";
    }
}