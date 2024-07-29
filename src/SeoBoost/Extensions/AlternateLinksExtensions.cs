using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using SeoBoost.Helper.AlternateLinks;

namespace SeoBoost.Extensions
{
    public static class AlternateLinksExtensions
    {
        private static Injected<IAlternateLinksHelper> AlternateLinksHelper { get; }

        public static HtmlString GetAlternateLinks(this IHtmlHelper html)
        {
            if (SeoBoostExtensions.IsInEditMode())
                return HtmlString.Empty;

            var requestContext = html.ViewContext.HttpContext;
            var contentReference = requestContext.GetContentLink();

            if (ContentReference.IsNullOrEmpty(contentReference))
                return HtmlString.Empty;

            return AlternateLinksHelper.Service.GetAlternateLinks(contentReference);
        }

        public static HtmlString GetAlternateLinks(this PageData pageData)
        {
            if (pageData == null)
                return HtmlString.Empty;

            if (SeoBoostExtensions.IsInEditMode())
                return HtmlString.Empty;

            return AlternateLinksHelper.Service.GetAlternateLinks(pageData.ContentLink);
        }

        public static AlternativeLinkViewModel GetAlternateLinksModel(this ContentReference contentReference)
        {
            if (ContentReference.IsNullOrEmpty(contentReference))
                return new AlternativeLinkViewModel();

            if (SeoBoostExtensions.IsInEditMode())
                return new AlternativeLinkViewModel();

            return AlternateLinksHelper.Service.GetAlternateLinksModel(contentReference);
        }


        public static AlternativeLinkViewModel GetAlternateLinksModel(this PageData pageData)
        {
            if (pageData == null)
                return new AlternativeLinkViewModel();

            if (SeoBoostExtensions.IsInEditMode())
                return new AlternativeLinkViewModel();

            return GetAlternateLinksModel(pageData.ContentLink);
        }
    }
}