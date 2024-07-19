using System.Text;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using SeoBoost.Business.Url;

namespace SeoBoost.Extensions
{
    public static class CanonicalLinkExtensions
    {
        private static Injected<IUrlService> UrlService { get; }

        public static HtmlString GetCanonicalLink(this IHtmlHelper html)
        {
            var requestContext = html.ViewContext.HttpContext;
            var contentReference = requestContext.GetContentLink();

            if (ContentReference.IsNullOrEmpty(contentReference))
                return HtmlString.Empty;

            return GetCanonicalLink(contentReference);
        }

        public static HtmlString GetCanonicalLink(this ContentReference contentReference)
        {
            if (ContentReference.IsNullOrEmpty(contentReference))
                return HtmlString.Empty;

            if (SeoBoostExtensions.IsInEditMode())
                return HtmlString.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("<link rel=\"canonical\" href=\"" + UrlService.Service.GetExternalUrl(contentReference, ContentLanguage.PreferredCulture) + "\" />");
            return new HtmlString(sb.ToString());
        }

        public static HtmlString GetCanonicalLink(this PageData pageData)
        {
            if (pageData == null)
                return HtmlString.Empty;

            return GetCanonicalLink(pageData.ContentLink);
        }

        public static string GetCanonicalLinkString(this IHtmlHelper html)
        {
            var requestContext = html.ViewContext.HttpContext;
            var contentReference = requestContext.GetContentLink();

            if (ContentReference.IsNullOrEmpty(contentReference))
                return string.Empty;

            return GetCanonicalLinkString(contentReference);
        }

        public static string GetCanonicalLinkString(this ContentReference contentReference)
        {
            if (ContentReference.IsNullOrEmpty(contentReference))
                return string.Empty;

            return SeoBoostExtensions.IsInEditMode() ?
            string.Empty :
            UrlService.Service.GetExternalUrl(contentReference, ContentLanguage.PreferredCulture);
        }

        public static string GetCanonicalLinkString(this PageData pageData)
        {
            if (pageData == null)
                return string.Empty;

            return GetCanonicalLinkString(pageData.ContentLink);
        }
    }
}