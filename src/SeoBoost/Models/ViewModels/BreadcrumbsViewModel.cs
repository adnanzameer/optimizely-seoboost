using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace SeoBoost.Models.ViewModels
{
    public class BreadcrumbsViewModel
    {
        private static readonly Injected<IContentLoader> _contentLoader;

        public readonly List<BreadcrumbItemListElementViewModel> BreadcrumbItemList;

        private int _index = 1;

        public BreadcrumbsViewModel(PageData currentPage, ContentReference startPageReference)
        {
            BreadcrumbItemList = GetBreadcrumbItemList(currentPage, startPageReference);
        }

        private List<BreadcrumbItemListElementViewModel> GetBreadcrumbItemList(PageData currentPage, ContentReference startPageReference = null)
        {
            var breadcrumbItemList = new List<BreadcrumbItemListElementViewModel>();

            PageData startPage;
            if (startPageReference == null || ContentReference.IsNullOrEmpty(startPageReference))
                startPage = GetStartPage(currentPage);
            else
                startPage = GetPageData(startPageReference);


            var startPageBreadcrumbElement = GetPageBreadcrumbElement(startPage, false);
            breadcrumbItemList.Add(startPageBreadcrumbElement);

            if (currentPage != startPage)
            {
                var root = ContentReference.RootPage;
                var parents = new List<PageData>();

                GetParentBreadcrumbs(currentPage, ref parents);

                parents.Reverse();

                foreach (var parent in parents)
                {
                    if (parent.PageLink.ID == root.ID) continue;
                    if (parent.PageLink.ID == startPage.ContentLink.ID) continue;

                    breadcrumbItemList.Add(GetPageBreadcrumbElement(parent, false));
                }
                breadcrumbItemList.Add(GetPageBreadcrumbElement(currentPage, true));
            }

            return breadcrumbItemList;
        }

        private static ICollection<PageData> GetParentBreadcrumbs(PageData page, ref List<PageData> parents)
        {
            var parent = GetParent(page);

            if (parent != null)
            {
                if (parent.CheckPublishedStatus(PagePublishedStatus.Published))
                    parents.Add(parent);

                return GetParentBreadcrumbs(parent, ref parents);
            }

            return parents;
        }

        private static PageData GetStartPage(PageData page)
        {
            var parent = GetParent(page);

            if (parent != null)
            {
                if (parent.CheckPublishedStatus(PagePublishedStatus.Published) &&
                    parent.PageLink.ID == ContentReference.RootPage.ID)
                    return page;

                return GetStartPage(parent);
            }

            return GetPageData(ContentReference.StartPage);
        }

        private static PageData GetPageData(ContentReference reference)
        {
            
            var pageData = _contentLoader.Service.Get<IContent>(reference) as PageData;
            return pageData;
        }

        private BreadcrumbItemListElementViewModel GetPageBreadcrumbElement(PageData page, bool selected)
        {

            var breadcrumbCurrentPageElement = new BreadcrumbItemListElementViewModel(
                page,
                IncrementIndex(),
                selected,
                _contentLoader.Service.GetChildren<PageData>(page.ContentLink).Any()
                );

            return breadcrumbCurrentPageElement;
        }

        private int IncrementIndex()
        {
            return _index++;
        }

        private static PageData GetParent(PageData currentPage)
        {
            if (currentPage.ParentLink == PageReference.EmptyReference)
                return null;

            return _contentLoader.Service.Get<IContent>(currentPage.ParentLink) as PageData;
        }
    }
}