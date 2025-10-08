using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;

namespace SeoBoost.Helper.AlternateLinks;

public interface IPageLanguageSettingsService
{
    IEnumerable<string> GetAvailableLanguages(ContentReference contentLink);
}

public class PageLanguageSettingsService : IPageLanguageSettingsService
{
    private readonly ContentLanguageSettingRepository _languageSettingsRepository;
    private readonly IContentLoader _contentLoader;

    public PageLanguageSettingsService(
        ContentLanguageSettingRepository languageSettingsRepository,
        IContentLoader contentLoader)
    {
        _languageSettingsRepository = languageSettingsRepository;
        _contentLoader = contentLoader;
    }

    private IEnumerable<ContentLanguageSetting> GetEffectiveLanguageSettings(ContentReference contentLink)
    {
        var settings = new List<ContentLanguageSetting>();
        var current = contentLink;

        // Walk up the tree until RootPage
        while (!ContentReference.IsNullOrEmpty(current))
        {
            var currentSettings = _languageSettingsRepository.Load(current).ToList();

            if (currentSettings.Any())
            {
                settings.AddRange(currentSettings);
                break; // Stop when we find explicit settings
            }

            if (!_contentLoader.TryGet<IContent>(current, out var content))
                break;

            current = content.ParentLink;
        }

        return settings;
    }

    public IEnumerable<string> GetAvailableLanguages(ContentReference contentLink)
    {
        var settings = GetEffectiveLanguageSettings(contentLink);

        return settings
            .Where(s => s.IsActive)
            .Select(s => s.LanguageBranch)
            .ToList();
    }
}