using Microsoft.AspNetCore.Http;

namespace IFixZoneWeb.Helpers
{
    public static class AuthHelper
    {
        public static bool HasRole(HttpContext context, string role)
        {
            var roles = context.Session.GetString("Role") ?? "";
            return roles.Split(',').Contains(role);
        }
    }
}
