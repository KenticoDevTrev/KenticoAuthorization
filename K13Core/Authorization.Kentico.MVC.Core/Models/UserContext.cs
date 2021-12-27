using System;
using System.Collections.Generic;

namespace Authorization.Kentico
{
    public class UserContext
    {
        public bool IsAuthenticated { get; set; } = false;
        public bool IsGlobalAdmin { get; set; } = false;
        public bool IsAdministrator { get; set; } = false;
        public bool IsEditor { get; set; } = false;
        public string UserName { get; set; } = string.Empty;
        public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
        public IEnumerable<string> Permissions { get; set; } = Array.Empty<string>();
    }
}
