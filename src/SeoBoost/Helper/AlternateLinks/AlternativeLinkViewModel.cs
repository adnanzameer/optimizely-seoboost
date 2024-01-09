using System.Collections.Generic;

namespace SeoBoost.Helper.AlternateLinks
{
   public class AlternativeLinkViewModel
    {
        public readonly ICollection<AlternativePageLink> Alternates;
        public string XDefaultUrl { get; set; }

        public AlternativeLinkViewModel(ICollection<AlternativePageLink> alternates)
        {
            Alternates = alternates;
            XDefaultUrl = "";
        }

        public AlternativeLinkViewModel()
        {
            Alternates = new List<AlternativePageLink>();
            XDefaultUrl = "";
        }
    }
}
