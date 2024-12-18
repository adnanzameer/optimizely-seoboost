using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;

namespace SeoBoost.Models.ViewModels
{
    public class BreadcrumbsViewModel
    {
        private static readonly Injected<IContentLoader> ContentLoader;
        public readonly List<BreadcrumbItemListElementViewModel> BreadcrumbItemList;
        private int _index = 1;

        public BreadcrumbsViewModel(PageData currentPage, ContentReference startPageReference)
        {
            BreadcrumbItemList = GetBreadcrumbItemList(currentPage, startPageReference);
        }

        private List<BreadcrumbItemListElementViewModel> GetBreadcrumbItemList(PageData currentPage, ContentReference startPageReference)
        {
            var breadcrumbItemList = new List<BreadcrumbItemListElementViewModel>();
            var startPage = GetStartPage(currentPage, startPageReference);
            breadcrumbItemList.Add(GetPageBreadcrumbElement(startPage, false));

            if (currentPage != startPage && IsChild(startPage.ContentLink, currentPage.ContentLink))
            {
                var parents = GetParentBreadcrumbs(startPage, currentPage).Reverse().ToList();
                breadcrumbItemList.AddRange(parents.Select(parent => GetPageBreadcrumbElement(parent, false)));
                breadcrumbItemList.Add(GetPageBreadcrumbElement(currentPage, true));
            }

            return breadcrumbItemList;
        }

        private PageData GetStartPage(PageData currentPage, ContentReference startPageReference)
        {
            if (startPageReference == null || ContentReference.IsNullOrEmpty(startPageReference))
            {
                return !ContentReference.IsNullOrEmpty(ContentReference.StartPage) 
                    ? GetPageData(ContentReference.StartPage) 
                    : FindStartPage(currentPage);
            }
                
            return GetPageData(startPageReference);
        }

        private PageData FindStartPage(PageData page)
        {
            var parent = GetParent(page);
            if (parent == null || parent.PageLink.ID == ContentReference.RootPage.ID)
                return page;

            return FindStartPage(parent);
        }

        private IEnumerable<PageData> GetParentBreadcrumbs(PageData startPage, PageData currentPage)
        {
            var parents = new List<PageData>();
            var parent = GetParent(currentPage);

            while (parent != null && parent.ContentLink.ID != startPage.ContentLink.ID)
            {
                if (parent.CheckPublishedStatus(PagePublishedStatus.Published))
                    parents.Add(parent);

                parent = GetParent(parent);
            }

            return parents;
        }

        private static bool IsChild(ContentReference startPageReference, ContentReference currentContentReference)
        {
            var descendants = ContentLoader.Service.GetDescendents(startPageReference);
            return descendants.Contains(currentContentReference);
        }

        private static PageData GetPageData(ContentReference reference)
        {
            var loadingOptions = new LoaderOptions { LanguageLoaderOption.FallbackWithMaster(ContentLanguage.PreferredCulture) };
            return ContentLoader.Service.Get<IContent>(reference, loadingOptions) as PageData;
        }

        private BreadcrumbItemListElementViewModel GetPageBreadcrumbElement(PageData page, bool selected)
        {
            return new BreadcrumbItemListElementViewModel(
                page,
                _index++,
                selected,
                ContentLoader.Service.GetChildren<PageData>(page.ContentLink).Any()
            );
        }

        private static PageData GetParent(PageData currentPage)
        {
            if (currentPage.ParentLink == PageReference.EmptyReference)
                return null;

            var loadingOptions = new LoaderOptions { LanguageLoaderOption.FallbackWithMaster(ContentLanguage.PreferredCulture) };
            return ContentLoader.Service.Get<IContent>(currentPage.ParentLink, loadingOptions) as PageData;
        }
    }
}