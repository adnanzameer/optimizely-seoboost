# Changelog

## Changes in version 2.1.0

Overhaul the codebase and upgrade the framework to .NET Core 6.

The latest update introduces several new features to enhance website management. Developers now have seamless control over the robots.txt file through the setup actions options in startup.cs. 

The Canonical Link feature supports a Custom canonical tag in the CMS, partial routing, Simple address, Page shortcuts (Fetchdata, Internal shortcut), multi-site & multi-domain support, and automatic handling of trailing slashes. 

Similarly, Alternate Links (hreflang attributes) offer similar functionalities, including Custom canonical tags in the CMS, partial routing, Simple address, Page shortcuts (Fetchdata, Internal shortcut), multi-site & multi-domain support, and automatic trailing slash handling.

## Changes in version 2.0.2		
- Bug fixes

## Changes in version 2.0.1		
- Bug fixes for robots.txt file and code improvements 

## Changes in version 2.0.0		
- .NET Core 5 and CMS 12 support 

## Changes in version 1.6.8
- Fix issue -  Robots.txt: Injecting the dependency and changing from the ContentRepository to ContentLoader
- PR #6 Credits to [AbsolunetCSarrazin](https://github.com/AbsolunetCSarrazin).

## Changes in version 1.6.7
- Option to create robots.txt page by the editor. Removed option to automatically create  robots.txt page on website initialization.

## Changes in version 1.6.5
- Exception handling in initialization module for empty database project.

## Changes in version 1.6.0
- Removed support for simple url/ external url in canonical tags and hreflang attributes.
- General code improvements

## Changes in version 1.5.0
- Added functionality to support editable robots.txt in CMS 