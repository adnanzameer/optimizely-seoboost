namespace SeoBoost.Helper.AlternateLinks
{
    public class AlternativePageLink
    {
        public readonly string Url;
        public readonly string Culture;

        public AlternativePageLink(string url, string culture)
        {
            Url = url.ToLower();
            Culture = culture;
        }
    }
}