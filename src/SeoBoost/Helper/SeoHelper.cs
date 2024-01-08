using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using SeoBoost.Business.Url;
using SeoBoost.Helper.AlternateLinks;
using SeoBoost.Models.ViewModels;

namespace SeoBoost.Helper
{
    public static class SeoHelper
    {
        public static HtmlString GetAlternateLinks(this IHtmlHelper html)
        {
            var requestContext = html.ViewContext.HttpContext;
            var contentReference = requestContext.GetContentLink();

            var alternateLinksHelper =ServiceLocator.Current.GetInstance<IAlternateLinksHelper>();
            return alternateLinksHelper.GetAlternateLinks(contentReference);
        }

       public static HtmlString GetAlternateLinks(this PageData pageData)
        {
            var alternateLinksHelper = ServiceLocator.Current.GetInstance<IAlternateLinksHelper>();
            return alternateLinksHelper.GetAlternateLinks(pageData.ContentLink);
        }

        public static HtmlString GetCanonicalLink(this IHtmlHelper html)
        {
            var requestContext = html.ViewContext.HttpContext;
            var contentReference = requestContext.GetContentLink();
            return GetCanonicalLink(contentReference);
        }

        public static HtmlString GetCanonicalLink(this ContentReference contentReference)
        {
            if (!ProcessRequest)
                return new HtmlString("");

            var sb = new StringBuilder();
            var urlService = ServiceLocator.Current.GetInstance<IUrlService>();
            sb.AppendLine("<link rel=\"canonical\" href=\"" + urlService.GetExternalUrl(contentReference, ContentLanguage.PreferredCulture) + "\" />");
            return new HtmlString(sb.ToString());
        }

        public static HtmlString GetCanonicalLink(this PageData pageData)
        {
            return GetCanonicalLink(pageData.ContentLink);
        }

        public static List<BreadcrumbItemListElementViewModel> GetBreadcrumbItemList(
            this ContentReference contentReference, ContentReference startPageReference = null)
        {
            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            var pageData = contentLoader.Get<IContent>(contentReference) as PageData;

            return GetBreadcrumbItemList(pageData, startPageReference);
        }

        public static List<BreadcrumbItemListElementViewModel> GetBreadcrumbItemList(this PageData pageData, ContentReference startPageReference = null)
        {
            if (IsBlockContext)
                return new List<BreadcrumbItemListElementViewModel>();

            if (pageData == null)
                return new List<BreadcrumbItemListElementViewModel>();

            var reference = startPageReference;
            if (reference == null || ContentReference.IsNullOrEmpty(reference))
                reference = ContentReference.StartPage;

            var breadcrumbModel = new BreadcrumbsViewModel(pageData, reference);
            return breadcrumbModel.BreadcrumbItemList;
        }

        public static List<BreadcrumbItemListElementViewModel> GetBreadcrumbItemList(this IHtmlHelper html, ContentReference startPageReference = null)
        {
            var requestContext = html.ViewContext.HttpContext;
            var contentReference = requestContext.GetContentLink();

            return GetBreadcrumbItemList(contentReference, startPageReference);
        }

        private static void GetAlternativePageLink(ContentReference contentReference, IList<LanguageBranch> languages,
            List<AlternativePageLink> alternates)
        {
            var contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
            var page = contentRepository.Get<IContent>(contentReference) as PageData;

            if (page == null)
                return;

            var pageLanguages = contentRepository.GetLanguageBranches<PageData>(page.ContentLink);
            var urlService = ServiceLocator.Current.GetInstance<IUrlService>();

            var pagesData = pageLanguages as IList<PageData> ?? pageLanguages.ToList();


            foreach (var language in languages)
            {
                foreach (var p in pagesData)
                {
                    if (string.Equals(p.Language.Name.ToLower(), language.LanguageID.ToLower(),
                        StringComparison.Ordinal))
                    {
                        var url = urlService.GetExternalUrl(page.ContentLink, p.Language);
                        var alternate = new AlternativePageLink(url, language.LanguageID);

                        alternates.Add(alternate);
                        break;
                    }
                }
            }
        }

        private static HtmlString CreateHtmlString(AlternativeLinkViewModel model)
        {
            var sb = new StringBuilder();

            foreach (var alternate in model.Alternates)
            {
                sb.AppendLine("<link rel=\"alternate\" href=\"" + alternate.Url + "\" hreflang=\"" + alternate.Culture.ToLower() + "\" />");
            }

            if (!string.IsNullOrEmpty(model.XDefaultUrl))
            {
                sb.AppendLine(" <link rel=\"alternate\" href=\"" + model.XDefaultUrl + "\" hreflang=\"x-default\" />");
            }

            return new HtmlString(sb.ToString());
        }

        private static bool ProcessRequest
        {
            get
            {
                var process = !IsInEditMode();

                if (process)
                {
                    process = !IsBlockContext;
                }

                return process;

            }
        }

        private static bool IsInEditMode()
        {
            var contextModeResolver = ServiceLocator.Current.GetInstance<IContextModeResolver>();
            var mode = contextModeResolver.CurrentMode;
            return mode is ContextMode.Edit or ContextMode.Preview;
        }

        private static bool IsBlockContext
        {
            get
            {
                var contentRouteHelper = ServiceLocator.Current.GetInstance<IContentRouteHelper>();
                return contentRouteHelper.Content is BlockData;
            }
        }
    }
}