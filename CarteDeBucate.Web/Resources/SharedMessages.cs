namespace CarteDeBucate.Web.Resources
{
    public static class SharedMessages
    {
        private static readonly System.Resources.ResourceManager ResourceManager = new(
            "CarteDeBucate.Web.Resources.SharedResource",
            typeof(global::CarteDeBucate.Web.SharedResource).Assembly);

        public static string AuthInvalidCredentials => Get("Auth_InvalidCredentials");
        public static string RegisterDuplicateUsername => Get("Register_DuplicateUsername");

        private static string Get(string name) => ResourceManager.GetString(name) ?? name;
    }
}
