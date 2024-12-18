﻿using System.ComponentModel.DataAnnotations;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Web;
using SeoBoost.Extensions;

namespace SeoBoost.Models.Pages
{
    [ContentType(DisplayName = "robots.txt", 
        GUID = "20abf142-91eb-4a13-9196-f6de727b4e4c", 
        Order = 100, 
        GroupName = "SEOBOOST", 
        Description = "Utilized for generating a customizable robots.txt file specific to each site.")]
    [RobotsTxtImageUrl]
    public class SBRobotsTxt: PageData
    {
        [Display(
            Name = "Robots.txt content",
            GroupName = SystemTabNames.Content,
            Order = 10)]
        [UIHint(UIHint.Textarea)]
        public virtual string RobotsContent { get; set; }

        public override void SetDefaultValues(ContentType contentType)
        {
            base.SetDefaultValues(contentType);

            PageName = "Robots.txt";
            VisibleInMenu = false;
            RobotsContent = SeoBoostExtensions.GetDefaultRobotsContent();
        }
    }
}