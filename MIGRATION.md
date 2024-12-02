# Change Logs and Migration
If you used the Kentico Xperience 13 implementation (`XperienceCommunity.Authorization`), below are an outline of changes that were made when creating the newer version.

## Changes Overview
Overall, the primary changes were as follows:
1. Nullable as Errors was enabled so all methods now specify if they allow and/or will return a null or not.
2. Strings with delimited character splitting was removed as this was only to support MVC5 (which KX13 supported).  Xperience by Kentico is on .Net 8+ and you can now declare string arrays statically in Attributes
3. Member Roles no longer have **Module Permissions** so this filter option was removed.
4. Since Xperience has designated site users as `Members` and separate from Admin users, the IsGlobalAdmin, IsModerator designations are removed, along with Page ACL now is only ***Read*** access (since that's all Members can do, they can't modify edit delete or browse tree)
5. Synchronized the variable names in the attribute (some used `templateIdentifiersArePrefix` and others `templateValuesArePrefixed`)


## Individual Changes
**UserContext**

-   No more properties of Admin, global admin, editor, etc as all Members are now just normal users
-   Permissions removed since Roles no longer have module permissions, can replace with custom `IAuthorization` logic that can map roles to special permission sets if needed.

**AuthorizationConfiguration**

-   Removed Resource and permission Names, again use custom IAuthorization with custom role to permission mapping if needed.
-   NodePermissionToCheck removed, access is only "Read" checks

**IAuthorization.IsAuthorizedAsync**

-   currentPage changed from TreeNode to IWebPageFieldsSource to cover Content Types

**IAuthorizationContextCustomizer**

- GetCustomUserAsync changed from UserInfo to MemberInfo

**GetUserEventArgs**

-   Changed FoundUser type to MemberInfo

**GetPageEventArgs**

-   Changed FoundPage type from TreeNode to IWebPageFieldSource

**AuthorizationType**

-   Removed ByPermission as that option is no longer available.

**RegisterPageBuilderAuthorizationAttribute**

-   Removed nodePermissionToCheck (always Read now)
-   Removed resourceAndPermissionNames (use custom IAuthorization)

**ControllerActionAuthorizationAttribute**

-   Removed nodePermissionToCheck (always Read now)
-   Removed resourceAndPermissionNames (use custom IAuthorization)

**ControlerActionAuthorizationAttribute**

- Removed some un-needed constructors now that attribute array declaration is native

**RegisterPageBuilderAuthorizationAttribute** 

- Removed some un-needed constructors now that attribute array declaration is native
- Removed the `AuthorizationType`-less constructor (defaulted to required authentication), was confusing and no different than the ones with the `AuthorizationType` defined.