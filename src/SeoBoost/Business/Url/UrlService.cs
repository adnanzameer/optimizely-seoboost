using System.Globalization;
using EPiServer.Core;
using EPiServer.Globalization;

namespace SeoBoost.Business.Url
{
    public class UrlService : IUrlService
    {
        private readonly UrlBuilder _builder;

        public UrlService(UrlBuilder builder)
        {
            _builder = builder;
        }

        public string GetExternalUrl(ContentReference contentReference, CultureInfo culture)
        {
            return _builder.ContentExternalUrl(contentReference, culture);
        }

        public string GetExternalUrl(ContentReference contentReference)
        {
            return _builder.ContentExternalUrl(contentReference, ContentLanguage.PreferredCulture);
        }
    }
}