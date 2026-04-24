using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Web;
using EPiServer.Web.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SeoBoost.Models.Pages;

namespace SeoBoost.Business.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(InitializationModule))]
    internal class CustomizedRenderingInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            context.Services.GetRequiredService<ITemplateResolverEvents>().TemplateResolved += TemplateCoordinator.OnTemplateResolved;
        }

        public void Uninitialize(InitializationEngine context)
        {
            context.Services.GetRequiredService<ITemplateResolverEvents>().TemplateResolved -= TemplateCoordinator.OnTemplateResolved;
        }
    }

    internal class TemplateCoordinator : IViewTemplateModelRegistrator
    {
        public static void OnTemplateResolved(object sender, TemplateResolverEventArgs args)
        {
            if (args.ItemToRender is SBRobotsTxt)
            {
                args.SelectedTemplate = null;
            }
        }

        public void Register(TemplateModelCollection viewTemplateModelRegistrator)
        {

        }
    }
}
