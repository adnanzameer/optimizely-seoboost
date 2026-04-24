using System.Linq;
using EPiServer.DataAbstraction;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SeoBoost.Models;
using SeoBoost.Models.Pages;

namespace SeoBoost.Business.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(InitializationModule))]
    public class ContentTypeHandlingInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var option = context.Services.GetRequiredService<IOptions<SeoBoostOptions>>();

            var contentTypeRepository = context.Services.GetRequiredService<IContentTypeRepository>();

            var robotsTxtContentType = contentTypeRepository.List().FirstOrDefault(ct => ct.ModelType == typeof(SBRobotsTxt));

            if (robotsTxtContentType != null)
            {
                if (option.Value.EnableRobotsTxtSupport && !robotsTxtContentType.IsAvailable)
                {
                    var clone = robotsTxtContentType.CreateWritableClone() as ContentType;

                    if (clone != null)
                        clone.IsAvailable = true;

                    contentTypeRepository.Save(clone);

                }
                else if (!option.Value.EnableRobotsTxtSupport && robotsTxtContentType.IsAvailable)
                {
                    var clone = robotsTxtContentType.CreateWritableClone() as ContentType;

                    if (clone != null)
                        clone.IsAvailable = false;

                    contentTypeRepository.Save(clone);
                }
            }
        }

        public void Uninitialize(InitializationEngine context)
        {

        }
    }
}
