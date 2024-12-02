namespace XperienceCommunity.Authorization
{
    public class UserContext
    {
        /// <summary>
        /// If the user is authenticated or not (public)
        /// </summary>
        public bool IsAuthenticated { get; set; } = false;

        /// <summary>
        /// The Username of the user.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// What Roles the user is assigned to
        /// </summary>
        public IEnumerable<string> Roles { get; set; } = [];
    }
}
