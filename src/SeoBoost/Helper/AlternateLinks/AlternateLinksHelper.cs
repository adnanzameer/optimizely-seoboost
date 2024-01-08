using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Html;
using SeoBoost.Business.Url;

namespace SeoBoost.Helper.AlternateLinks
{
    public interface IAlternateLinksHelper
    {
        HtmlString GetAlternateLinks(ContentReference contentReference);
        AlternativeLinkViewModel GetAlternateLinksModel(ContentReference contentReference);
    }

    public class AlternateLinksHelper : IAlternateLinksHelper
    {
        private readonly IContentRepository _contentRepository;
        private readonly ILanguageBranchRepository _languageBranchRepository;
        private readonly IContentRouteHelper _contentRouteHelper;
        private readonly IUrlService _urlService;
        public AlternateLinksHelper(IContentRepository contentRepository, ILanguageBranchRepository languageBranchRepository, IContentRouteHelper contentRouteHelper, IUrlService urlService)
        {
            _contentRepository = contentRepository;
            _languageBranchRepository = languageBranchRepository;
            _contentRouteHelper = contentRouteHelper;
            _urlService = urlService;
        }

        public AlternativeLinkViewModel GetAlternateLinksModel(ContentReference contentReference)
        {
            if (!ProcessRequest)
                return null;

            _contentRepository.TryGet<PageData>(contentReference, out var pageData);

            if (pageData != null)
            {
                var alternates = new List<AlternativePageLink>();
                var languages = _languageBranchRepository.ListEnabled();

                var pageLanguages = _contentRepository.GetLanguageBranches<PageData>(pageData.ContentLink);

                var pagesData = pageLanguages as IList<PageData> ?? pageLanguages.ToList();

                CultureInfo masterLanguageBranch = null;
                foreach (var language in languages)
                {
                    foreach (var p in pagesData)
                    {
                        if (string.Equals(p.Language.Name.ToLower(), language.LanguageID.ToLower(), StringComparison.Ordinal))
                        {
                            if (p.IsMasterLanguageBranch)
                            {
                                masterLanguageBranch = p.Language;
                            }

                            var url = _urlService.GetExternalUrl(pageData.ContentLink, new CultureInfo(p.Language.Name));

                            if (!alternates.Any(x => x.Url.Equals(url, StringComparison.OrdinalIgnoreCase)))
                            {
                                var alternate = new AlternativePageLink(url, language.LanguageID);
                                alternates.Add(alternate);
                                break;
                            }
                        }
                    }
                }
                masterLanguageBranch ??= languages.FirstOrDefault(l => l.ID == 1)?.Culture ?? languages.FirstOrDefault()?.Culture;
                
                var xDefault =
                    alternates.FirstOrDefault(a => string.Equals(a.Culture.ToLower(), masterLanguageBranch?.Name.ToLower()));

                var model = new AlternativeLinkViewModel(alternates);

                if (!string.IsNullOrEmpty(xDefault?.Url))
                    model.XDefaultUrl = xDefault.Url;

                return model;
            }

            return null;
        }

        public HtmlString GetAlternateLinks(ContentReference contentReference)
        {
            if (!ProcessRequest)
                return new HtmlString("");

            var model = GetAlternateLinksModel(contentReference);

            if (model != null)
            {
                var htmlString = CreateHtmlString(model);
                return htmlString;
            }

            return new HtmlString("");
        }

        private HtmlString CreateHtmlString(AlternativeLinkViewModel model)
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


        private bool ProcessRequest
        {
            get
            {
                var process = !IsInEditMode();

                if (process)
                {
                    process = IsPageData;
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

        private bool IsPageData => _contentRouteHelper.Content is PageData;
    }
}