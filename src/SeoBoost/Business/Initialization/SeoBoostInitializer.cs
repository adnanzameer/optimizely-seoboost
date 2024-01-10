using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;
using SeoBoost.Models.Pages;

namespace SeoBoost.Business.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class SeoBoostInitializer : IInitializableModule
    {
        private IContentLoader _contentLoader;

        public void Initialize(InitializationEngine context)
        {
            _contentLoader = context.Locate.Advanced.GetInstance<IContentLoader>();

            var events = context.Locate.ContentEvents();
            events.CreatingContent += ContentEvents_CreatingContent;
            events.PublishingContent += Instance_PublishingPage;
            events.MovingContent += ContentEvents_MovingContent;
        }

        private void Instance_PublishingPage(object sender, ContentEventArgs e)
        {
            if (e.Content is SBRobotsTxt && e.Content.ParentLink.ID != ContentReference.StartPage.ID)
            {
                e.CancelReason = "robots.txt page can only be published under site start page";
                e.CancelAction = true;
            }
        }

        private void ContentEvents_MovingContent(object sender, ContentEventArgs e)
        {
            if (e.Content is not SBRobotsTxt)
                return;

            if (e.TargetLink.ID == ContentReference.WasteBasket.ID)
                return;

            if (e.TargetLink.ID == ContentReference.StartPage.ID)
            {
                var loadingOptions = new LoaderOptions { LanguageLoaderOption.FallbackWithMaster(ContentLanguage.PreferredCulture) };

                var items = _contentLoader.GetChildren<SBRobotsTxt>(ContentReference.StartPage, loadingOptions);

                var robotTxtPages = items.ToList();
                if (robotTxtPages.Any())
                {
                    var parent = _contentLoader.Get<PageData>(robotTxtPages.First().ParentLink, loadingOptions);
                    e.CancelReason = $"robots.txt page already exist under {parent.Name} ({parent.ContentLink.ID})";
                    e.CancelAction = true;
                }

                return;
            }

            e.CancelReason = "robots.txt page can only be moved under site start page";
            e.CancelAction = true;
        }

        private void ContentEvents_CreatingContent(object sender, ContentEventArgs e)
        {
            if (e.Content is not SBRobotsTxt)
                return;

            if (e.Content.ParentLink.ID == ContentReference.StartPage.ID)
            {
                var loadingOptions = new LoaderOptions { LanguageLoaderOption.FallbackWithMaster(ContentLanguage.PreferredCulture) };

                var items = _contentLoader.GetChildren<SBRobotsTxt>(ContentReference.StartPage, loadingOptions);

                var robotTxtPages = items.ToList();
                if (robotTxtPages.Any())
                {
                    var parent = _contentLoader.Get<PageData>(robotTxtPages.First().ParentLink, loadingOptions);
                    e.CancelReason = $"robots.txt page already exist under {parent.Name} ({parent.ContentLink.ID})";
                    e.CancelAction = true;
                }

                return;
            }

            e.CancelReason = "robots.txt page can only be created under site start page";
            e.CancelAction = true;
        }

        public void Uninitialize(InitializationEngine context)
        {
            var events = context.Locate.ContentEvents();

            events.CreatingContent -= ContentEvents_CreatingContent;
            events.PublishingContent -= Instance_PublishingPage;
            events.MovingContent -= ContentEvents_MovingContent;
        }
    }
}