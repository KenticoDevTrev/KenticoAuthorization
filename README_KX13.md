# XperienceCommunity.Authorization
This package provides request Authorization for both Controller/Actions as well as Page Builder requests, allowing you to restrict access based on:

1. User Authenticated
2. User Names
3. User Roles
4. Page ACL Permissions (May require custom handling, see `Events` section below)
5. Resource/Module Permissions
6. Custom `IAuthorization` Authentication Logic

It also allows for a custom Unauthorized Redirect path in case you need to specify a specific location to send unauthorized users.

# Installation and Requirements
This package only works on Kentico Xperience 13 (.net core 5.0) on hotfix 5 or above.  If you have Kentico Xperience 12 or 13 on .net Full framework, there is [partial supported packages available](https://github.com/KenticoDevTrev/KenticoAuthorization/tree/PreviousVersions)

To install...
1. Install the `XperienceCommunity.Authorization`  NuGet Package to your MVC Site
2. In your startup, `services.AddKenticoAuthorization()` 
3. Also add to the Controller Option Filters:
``` csharp
	services.AddControllersWithViews(options => options.Filters.AddKenticoAuthorization())
```

4. Make sure to set the `LoginPath` (Not authorized and not logged in) and `AccessDeniedPath` (Not authorized and logged in)  in your `ConfigureApplicationCookie`, as the this tool will leverage these paths when redirecting for users.  Here's a sample below:

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
For Controller/Actions, add the `[ControllerActionAuthorization()]` above your Action.  

For Page Builder requests, add `[assembly: RegisterPageBuilderAuthorization()]` in any assembly that is registered with Kentico Xperience (has the `[assembly: AssemblyDiscoverable]` attribute)

Both attributes have multiple constructions to cover basic scenarios, as well as a full constructor to allow you complete control.

Empty constructor (`[ControllerActionAuthorization()]`) means only check for Authenticated (logged in).

# Migration from Previous Packages
If you either used `Authorization.Kentico.MVC` (.net 4.8) or `Authorization.Kentico.MVC.Core` (.net Core) on your MVC Site, you will need to perform the following steps:
1. Uninstall `Authorization.Kentico.MVC` / `Authorization.Kentico.MVC.Core` packages
2. Replace `KenticoAuthorize` Attributes with `ControllerActionAuthorization` attributes

The global events for authorization have been replaced with Interfaces for you to overwrite.
1. `AuthorizeEvent` has been replaced with `IAuthorize` interface, which you can overwrite globally by implementing and adding your own to the service collection after you call `services.AddKenticoAuthorization()`, OR on your Authorization Attributes you can define a custom `IAuthorize` typed class to perform custom logic on that specific authorization attribute.
2. `GetCultureEvent` has been replaced with `IAuthorizationContextCustomizer.GetCustomCultureAsync` 
3. `GetUserEvent` has been replaced with `IAuthorizationContextCustomizer.GetCustomUserAsync` and/or `IAuthorizationContextCustomizer.GetCustomuserContextAsync`
4. `GetPageEvent` has been replaced with `IAuthorizationContextCustomizer.GetCustomPageAsync`

In the case of `IAuthorizationContextCustomizer` you can `return null` to opt out of performing any custom logic for that particular event. 

# Customization and Events
There are 3 interfaces that you can leverage to customize the Authorization logic.
## IAuthorize
This interface allows you to implement custom Authorization logic.  You can implement your own version of this and pass it into your `ControllerActionAuthorization` or `RegisterPageBuilderAuthorization` parameters, or you can add your own implementation to your services collection after the `services.AddKenticoAuthorization` to overwrite the default logic completely.

## IAuthorizationContextCustomizer
This interface allows you to have control over Culture, Page, User, and User Context both before and after default logic is executed.  Returning null bypasses any custom logic, where as returning a result will use your returned object for building the AuthorizationContext.  

This is useful if...
* You have custom routing (Page context not from the Page Builder, or matching request path to NodeAliasPath

* Your culture is not determined by the `System.Globalization.CultureInfo.CurrentCulture.Name` or  `Page Builder Preview Culture` 

* Your user is not determined by basic `HttpContext.User.Identity.Name` (username) and/or permissions not based on standard Kentico Role/permissions

## IAuthorizationContext
This interface takes the current objects (from `IAuthorizationContextCustomizer` and default logic) to build out the Authorization Context that is passed to the `IAuthorization.IsAuthorizedAsync`  You should probably not need to implement your own unless you wish to do testing.


# Contributions, bug fixes and License
Big thanks to [Sean Wright](https://github.com/seangwright) for all his tutoring and help on .net core, he helped me get this package where it needed to be!

Feel free to Fork and submit pull requests to contribute.

You can submit bugs through the issue list and i will get to them as soon as i can, unless you want to fix it yourself and submit a pull request!

Check the License.txt for License information
