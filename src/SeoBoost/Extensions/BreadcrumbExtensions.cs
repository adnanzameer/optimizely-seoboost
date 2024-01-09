using System.Collections.Generic;
using EPiServer;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Mvc.Rendering;
using SeoBoost.Models.ViewModels;

namespace SeoBoost.Extensions
{
    public static class BreadcrumbExtensions
    {
        public static List<BreadcrumbItemListElementViewModel> GetBreadcrumbItemList(this ContentReference contentReference, ContentReference startPageReference = null)
        {
            if (ContentReference.IsNullOrEmpty(contentReference))
                return new List<BreadcrumbItemListElementViewModel>();

            var loadingOptions = new LoaderOptions { LanguageLoaderOption.FallbackWithMaster(ContentLanguage.PreferredCulture) };
            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            var pageData = contentLoader.Get<IContent>(contentReference, loadingOptions) as PageData;

            return pageData.GetBreadcrumbItemList(startPageReference);
        }

        public static List<BreadcrumbItemListElementViewModel> GetBreadcrumbItemList(this PageData pageData, ContentReference startPageReference = null)
        {
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

            if (ContentReference.IsNullOrEmpty(contentReference))
                return new List<BreadcrumbItemListElementViewModel>();

            return contentReference.GetBreadcrumbItemList(startPageReference);
        }
    }
}