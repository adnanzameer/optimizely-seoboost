using System.Globalization;
using EPiServer.Core;

namespace SeoBoost.Business.Url
{
    public interface IUrlService
    {
        string GetExternalUrl(ContentReference contentReference, CultureInfo culture);
        string GetExternalUrl(ContentReference contentReference);
        string GetCanonicalLink(ContentReference contentReference, CultureInfo culture);
        string GetCanonicalLink(ContentReference contentReference);
    }
}