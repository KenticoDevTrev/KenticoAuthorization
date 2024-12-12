
# XperienceCommunity.DevTools.Authorization.MemberRoles
This package provides request Authorization for both Controller/Actions as well as Page Builder requests, allowing you to restrict access based on:

1. User Authenticated
2. User Names
3. User Roles
4. Page ACL Permissions (May require custom handling, see `Events` section below)
6. Custom `IAuthorization` Authentication Logic

It also allows for a custom Unauthorized Redirect path in case you need to specify a specific location to send unauthorized users.

## Library Version Matrix and Dependency Notice

This project is using [Xperience Version v30.0.0](https://docs.kentico.com/changelog#refresh-november-14-2024), and depends on the [XperienceCommunity.MemberRoles](https://github.com/KenticoDevTrev/MembershipRoles_Temp) package since Xperience by Kentico does not have Member Roles built in yet.  An additional version should be released at the time Kentico does implement member roles and permissions.

| Xperience Version  | Library Version |
| ------------------ | --------------- |
| >= 30.0.*          | 2.0.0           |
|    29.7.*          | 1.0.0           |


If you have Kentico Xperience 13 (.net core 5.0) on hotfix 5 or above, please see the [KX13 ReadMe](README_KX13.md).

# Package Installation

Add the package to your application using the .NET CLI
```powershell
dotnet add package XperienceCommunity.DevTools.Authorization.MemberRoles.Admin
```

Alternatively, you can elect to install only the required packages on specific projects if you have separation of concerns:

**XperienceCommunity.DevTools.Authorization.MemberRoles**: Kentico.Xperience.WebApp Dependent (No Admin)

**XperienceCommunity.DevTools.Authorization.MemberRoles.Admin** : Kentico.Xperience.Admin (Admin Items)

# Quick Start

In your startup...

1. Call `services.AddKenticoAuthorization()` to add required Dependencies
2. Call `services.AddControllersWithViews(option => options.AddKenticoAuthorizationFilters())` to enable the filters.
3. Make sure ASP.Net Identity, Kentico, and [XperienceCommunity.MemberRoles](https://github.com/KenticoDevTrev/MembershipRoles_Temp?tab=readme-ov-file#quick-start) are configured in your startup (as shown below)
``` csharp
// Adds Basic Kentico Authentication, needed for user context and some tools
builder.Services.AddAuthentication();

// Adds and configures ASP.NET Identity for the application
// XperienceCommunity.MemberRoles, make sure Role is TagApplicationUserRole or an inherited member here
builder.Services.AddIdentity<ApplicationUser, TagApplicationUserRole>(options => {
    // Ensures that disabled member accounts cannot sign in
    options.SignIn.RequireConfirmedAccount = true;
    // Ensures unique emails for registered accounts
    options.User.RequireUniqueEmail = true;
})
    .AddUserStore<ApplicationUserStore<ApplicationUser>>()
    .AddMemberRolesStores<ApplicationUser, TagApplicationUserRole>() // XperienceCommunity.MemberRoles
    .AddUserManager<UserManager<ApplicationUser>>()
    .AddSignInManager<SignInManager<ApplicationUser>>();

// Adds authorization support to the app
builder.Services.ConfigureApplicationCookie(options => {
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.LoginPath = new PathString("/Account/Signin"); // See Step 4
    options.AccessDeniedPath = new PathString("/Error/403"); // See Step 4
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddAuthorization();
```
4. Make sure to set the `LoginPath` (Not authorized and not logged in) and `AccessDeniedPath` (Not authorized and logged in)  in your `ConfigureApplicationCookie`, as this tool will leverage these paths when redirecting for users.  Here's a sample below:

``` csharp
// See code sample from Step 3 above
builder.Services.ConfigureApplicationCookie(options => {
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.LoginPath = new PathString("/Account/Signin"); // Customize
    options.AccessDeniedPath = new PathString("/Error/403"); // Customize
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
```
# Usage
For Controller/Actions, add the `[ControllerActionAuthorization()]` attribute above your Action.  

For Page Builder requests, add `[assembly: RegisterPageBuilderAuthorization()]` in any assembly that is registered with Xperience by Kentico (has the `[assembly: AssemblyDiscoverable]` attribute)

Both attributes have multiple constructions to cover basic scenarios, as well as a full constructor to allow you complete control.

Empty constructor (`[ControllerActionAuthorization()]`) means only check for Authenticated (logged in).

Examples below.

# Page Template Examples
```csharp

// Use Page ACL for these Content Types
[assembly: RegisterPageBuilderAuthorization(PageBuilderAuthorizationTypes.ByPageType, ["MySite.WebPages", "MySite.BlogPages"], AuthorizationType.ByPageACL)]

// Secure Docs only by Authenticated Users
[assembly: RegisterPageBuilderAuthorization(PageBuilderAuthorizationTypes.ByPageType, "Docs.Secure", AuthorizationType.ByAuthenticated)]

// By Page Template, only teachers
[assembly: RegisterPageBuilderAuthorization(PageBuilderAuthorizationTypes.ByPageTemplate, "MySite.SecurePages_Teachers", AuthorizationType.ByRole, ["teachers"])]

// By Page Template "MySite.UserPages_BigBossMan" where the username must be either BillyTheBoss@example.com or JoeTheBoss@example.com
[assembly: RegisterPageBuilderAuthorization(PageBuilderAuthorizationTypes.ByPageTemplate, "MySite.UserPages_BigBossMan", AuthorizationType.ByUser, ["BillyTheBoss@example.com", "JoeTheBoss@example.com"])]

// By Page Template's that start with MySite.BobPages_ using custom Authentication logic
[assembly: RegisterPageBuilderAuthorization(PageBuilderAuthorizationTypes.ByPageTemplate, "MySite.BobPages_", typeof(BobAuthorization), templateIdentifiersArePrefix: true)]

```

## Controller Examples
```csharp

// Only Authenticated users
[ControllerActionAuthorization(AuthorizationType.ByAuthenticated)]
public async Task<ViewResult> AuthenticationOnly() { ... }

// By Roles (Member Roles)
[ControllerActionAuthorization(AuthorizationType.ByRole, ["teacher", "student"])]
public async Task<ViewResult> TeacherAndStudentsOnly() { ... }

// By Usernames
[ControllerActionAuthorization(AuthorizationType.ByUser, ["billy@example.com", "bob@example.com"])]
public async Task<ViewResult> BillyAndBobOnly() { ... }

// By custom IAuthorization implementation
[ControllerActionAuthorization(typeof(BobAuthorization))]
public async Task<ViewResult> BobsOnly() { ... }

// Page ACL, will possibly require registering a custom IAuthorizationContextCustomizer and adding logic to GetCustomPageAsync
// to find the right page that matches this controller context.
[ControllerActionAuthorization(AuthorizationType.ByPageACL)]
public async Task<ViewResult> SomePage() { ... }

```

# Migration from Previous Packages
If you either used `Authorization.Kentico.MVC` (.net 4.8) or `Authorization.Kentico.MVC.Core` (.net Core) on your MVC Site, first see the Migration instructions on the [KX13 Readme](README_KX13.md)

If you have used the `XperienceCommunity.Authorization` on KX13, please see the Please see our [Migration](MIGRATION.md) for changes and migration.

# Customization and Events
There are 3 interfaces that you can leverage to customize the Authorization logic.
## IAuthorize
This interface allows you to implement custom Authorization logic.  You can implement your own version of this and pass it into your `ControllerActionAuthorization` or `RegisterPageBuilderAuthorization` parameters, or you can add your own implementation to your services collection **after** the `services.AddKenticoAuthorization` to overwrite the default logic completely.

Here's an example.
```csharp
public class BobAuthorization : IAuthorization
{
    public Task<bool> IsAuthorizedAsync(UserContext user, AuthorizationConfiguration authConfig, IWebPageFieldsSource currentPage = null, string pageTemplateIdentifier = null)
    {
        // Only Bobs...
        return Task.FromResult(user.UserName.Contains("Bob", StringComparison.OrdinalIgnoreCase));
    }
}

...

// In Startup
            builder.Services.AddKenticoAuthorization()
                .AddScoped<BobAuthorization>();

// Register assembly
[assembly: RegisterPageBuilderAuthorization(PageBuilderAuthorizationTypes.ByPageTemplate, "MySite.BobPages_", typeof(BobAuthorization), templateIdentifiersArePrefix: true)]

```


## IAuthorizationContextCustomizer
This interface allows you to have control over Culture, Page, User (Member), and User Context both before and after default logic is executed.  Returning null bypasses any custom logic, whereas returning a result will use your returned object for building the `AuthorizationContext`.  

This is useful if...
* You have custom routing (Page context not from the Page Builder, or matching request path to some value)

* Your culture is not determined by the `System.Globalization.CultureInfo.CurrentCulture.Name` or  `Page Builder Preview Culture` 

* Your user (Member) is not determined by basic `HttpContext.User.Identity.Name` (username) and/or permissions not based on the Member Roles defined in the Admin interface.

## IAuthorizationContext
This interface takes the current objects (from `IAuthorizationContextCustomizer` and default logic) to build out the Authorization Context that is passed to the `IAuthorization.IsAuthorizedAsync`  You should probably not need to implement your own unless you wish to do testing.

# Migration from KX13 XperienceCommunity.Authorization
Please see our Migration.MD for changes (there aren't many).

# Contributions, bug fixes and License
Big thanks to [Sean Wright](https://github.com/seangwright) for all his tutoring and help on .net core, he helped me get this package where it needed to be!

Feel free to Fork and submit pull requests to contribute.

You can submit bugs through the issue list and I will get to them as soon as i can, unless you want to fix it yourself and submit a pull request!

Check the [License](LICENSE) for License information
