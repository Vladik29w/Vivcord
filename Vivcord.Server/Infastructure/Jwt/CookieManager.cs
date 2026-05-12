namespace Vivcord.Server.Infastructure.Jwt
{
    public static class CookieManager
    {
        private const string Jwt = "jwt";
        private const string RefToken = "refToken";

        private static CookieOptions GetOptions(DateTime? expires = null) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expires
        };

        public static void SetCookie(this HttpResponse response, string token)
        {
            response.Cookies.Append(Jwt, token, GetOptions(DateTime.UtcNow.AddMinutes(10)));
        }
        public static void SetRefreshCookie(this HttpResponse response, string token)
        {
            response.Cookies.Append(RefToken, token, GetOptions(DateTime.UtcNow.AddDays(1)));
        }
        public static void ClearCookies(this HttpResponse response)
        {
            var options = GetOptions();
            response.Cookies.Delete(Jwt, options);
            response.Cookies.Delete(RefToken, options);
        }
    }
}
