using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Html;
using SeoBoost.Business.Url;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

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
        private readonly IContentRouteHelper _contentRouteHelper;
        private readonly IUrlService _urlService;
        private readonly ILanguageBranchRepository _languageBranchRepository;
        private readonly IPageLanguageSettingsService _pageLanguageSettingsService;
        private readonly IContextModeResolver _contextModeResolver;

        public AlternateLinksHelper(IContentRepository contentRepository, IContentRouteHelper contentRouteHelper, IUrlService urlService, IPageLanguageSettingsService pageLanguageSettingsService, ILanguageBranchRepository languageBranchRepository, IContextModeResolver contextModeResolver)
        {
            _contentRepository = contentRepository;
            _contentRouteHelper = contentRouteHelper;
            _urlService = urlService;
            _pageLanguageSettingsService = pageLanguageSettingsService;
            _languageBranchRepository = languageBranchRepository;
            _contextModeResolver = contextModeResolver;
        }

        public AlternativeLinkViewModel GetAlternateLinksModel(ContentReference contentReference)
        {
            if (!ProcessRequest)
                return null;

            if (!_contentRepository.TryGet(contentReference, out PageData pageData) || pageData == null)
                return null;

            var alternates = new List<AlternativePageLink>();

            var availableSet = new HashSet<string>(
                _pageLanguageSettingsService.GetAvailableLanguages(pageData.ContentLink),
                StringComparer.OrdinalIgnoreCase);

            if (availableSet.Count == 0)
            {
                availableSet = new HashSet<string>(
                    _languageBranchRepository.ListEnabled().Select(x => x.LanguageID),
                    StringComparer.OrdinalIgnoreCase);
            }

            var pageLanguages = _contentRepository.GetLanguageBranches<PageData>(pageData.ContentLink);

            // Index by language name, only for languages that are available
            var byLang = pageLanguages
                .Where(p => availableSet.Contains(p.Language?.Name))
                .GroupBy(p => p.Language.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            CultureInfo masterLanguageBranch = null;

            // Prevent duplicate URLs
            var existingUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (language, p) in byLang)
            {
                if (p.IsMasterLanguageBranch)
                    masterLanguageBranch = p.Language;

                var culture = new CultureInfo(p.Language.Name);
                var url = _urlService.GetExternalUrl(p.ContentLink, culture);

                if (existingUrls.Add(url))
                    alternates.Add(new AlternativePageLink(url, language));
            }

            var model = new AlternativeLinkViewModel(alternates);

            // x-default: prefer master if present among alternates
            if (masterLanguageBranch != null)
            {
                var xDefault = alternates.FirstOrDefault(a =>
                    string.Equals(a.Culture, masterLanguageBranch.Name, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(xDefault?.Url))
                    model.XDefaultUrl = xDefault.Url;
            }

            return model;
        }

        public HtmlString GetAlternateLinks(ContentReference contentReference)
        {
            if (!ProcessRequest)
                return HtmlString.Empty;

            var model = GetAlternateLinksModel(contentReference);

            if (model != null)
            {
                var htmlString = CreateHtmlString(model);
                return htmlString;
            }

            return HtmlString.Empty;
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

        private bool IsInEditMode()
        {
            return _contextModeResolver.CurrentMode is ContextMode.Edit or ContextMode.Preview;
        }

        private bool IsPageData => _contentRouteHelper.Content is PageData;
    }
}