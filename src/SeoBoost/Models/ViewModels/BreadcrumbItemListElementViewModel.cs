using EPiServer.Core;

namespace SeoBoost.Models.ViewModels
{
    public class BreadcrumbItemListElementViewModel
    {
        public readonly PageData PageData;
        public readonly int Position;
        public readonly bool Selected;
        public readonly bool HasChildren;

        public BreadcrumbItemListElementViewModel(PageData pageData, int position, bool selected, bool hasChildren)
        {
            PageData = pageData;
            Position = position;
            Selected = selected;
            HasChildren = hasChildren;
        }
    }
}