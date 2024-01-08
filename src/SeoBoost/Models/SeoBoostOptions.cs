namespace SeoBoost.Models
{
    public class SeoBoostOptions
    {
        public bool UseSimpleAddress { get; set; } = false;
        public bool UseMirrorPageReference { get; set; } = true;
        public bool EnableRobotsFileSupport { get; set; } = false;
        public bool UseSiteUrlAsHost { get; set; } = false;
        public string CustomCanonicalTagFieldName { get; set; } = "";
    }
}
