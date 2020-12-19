
# Kentico Authorization Attribute
Kentico Authorization Attribute for Kentico MVC, provides a [KenticoAuthorize] Attribute that you can add to your ActionResult methods that can allows for permissions on:

1. User Authenticated
1. User Names
1. User Roles
1. Page ACL Permissions (May require custom handling, see `Events` section below)
1. Resource/Module Permissions

It also allows for a custom Unauthorized Redirect path in case you need to specify a specific location to send unauthorized users.

# Installation
1. Install the `Authorization.Kentico.MVC` (.net 4.8) or `Authorization.Kentico.MVC.Core` (.net Core) NuGet Package to your MVC Site
1. Overwrite any Events if you need (expecially the GetPage)
1. Add `[KenticoAuthorize()]` attributes to your ActionResult methods.

For .Net Core only, make sure to set the `LoginPath` (Not authorized and not logged in) and `AccessDeniedPath` (Not authorized and logged in)  in your `ConfigureApplicationCookie`, as the this tool will leverage these paths when redirecting for users.  Here's a sample below:

``` csharp
// Configures the application's authentication cookie
services.ConfigureApplicationCookie(c =>
{
    c.LoginPath = new PathString("/Account/Signin");
    c.AccessDeniedPath = new PathString("/Error/403");
    c.ExpireTimeSpan = TimeSpan.FromDays(14);
    c.SlidingExpiration = true;
    c.Cookie.Name = AUTHENTICATION_COOKIE_NAME;
});
```

# Usage
1. Add the `[KenticoAuthorize()]` Attribute to your ActionResult and pass in any properties you wish to configure.

# Events
The Authorization Module has 4 events you can hook into in order to customize it's behavior. 

## AuthorizeEvents.GetPage
This allows you to modify the retrieval of the current page.  By default, it will try to find the page based on the relative path with a match on the NodeAliasPath.

## AuthorizeEvents.GetCulture
This allows you to modify the retrieval of the current culture.  This is used in the GetPage logic to get the proper TreeNode.  By default, It will use the PreviewCulture (if in preview), LocalizationContext.CurrentCulture, and lastly the System.Globalization.CultureInfo.CurrentCulture.Name.

## AuthorizeEvents.GetUser
This allows you to modify the retrieval of the current user.  By default it will use the HttpContext.User.Identity to get the UserName of the current user.  Public is the default user if none found or the found user is disabled.

## AuthorizeEvents.Authorizing
This allows you to modify the Authorizing logic itself.  By default it will perform all the proper checks on User, Role, Module Permissions, Page ACL, and user allowed cultures.  If you do overwrite, you must set `SkipDefaultValidation` to true in the AuthorizingEventArgs.

# Contributions, bug fixes and License
Feel free to Fork and submit pull requests to contribute.

You can submit bugs through the issue list and i will get to them as soon as i can, unless you want to fix it yourself and submit a pull request!

Check the License.txt for License information

# Compatability
Can be used on any Kentico 12 SP site (hotfix 29 or above) and Kentico 13 (.net 4.8 or .net Core)
