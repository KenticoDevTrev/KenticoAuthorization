# Kentico Authorization Attribute w/ Dynamic Routing
Kentico Authorization Attribute for Kentico MVC, provides a [KenticoAuthorize] Attribute that you can add to your ActionResult methods that can allows for permissions on:

1. User Authenticated
1. User Names
1. User Roles
1. Page ACL Permissions (May require custom handling, see `Events` section below)
1. Resource/Module Permissions

It also allows for a custom Unauthorized Redirect path in case you need to specify a specific location to send unauthorized users.

This module uses the DynamicRouting.Kentico.MVC package to retrieve the current page.

# Installation
1. Install the `Authorization.Kentico.MVC` NuGet Package to your MVC Site
1. Overwrite any Events if you need
1. Add `[KenticoAuthorize()]` attributes to your ActionResult methods.

# Usage
1. Add the `[KenticoAuthorize()]` Attribute to your ActionResult and pass in any properties you wish to configure.

# Events
The Authorization Module has 2 events you can hook into in order to customize it's behavior. 

## AuthorizeEvents.GetUser
This allows you to modify the retrieval of the current user.  By default it will use the HttpContext.User.Identity to get the UserName of the current user.  Public is the default user if none found or the found user is disabled.

## AuthorizeEvents.Authorizing
This allows you to modify the Authorizing logic itself.  By default it will perform all the proper checks on User, Role, Module Permissions, Page ACL, and user allowed cultures.  If you do overwrite, you must set `SkipDefaultValidation` to true in the AuthorizingEventArgs.

# Contributions, but fixes and License
Feel free to Fork and submit pull requests to contribute.

You can submit bugs through the issue list and i will get to them as soon as i can, unless you want to fix it yourself and submit a pull request!

Check the License.txt for License information

# Compatability
Can be used on any Kentico 12 SP site (hotfix 29 or above).
