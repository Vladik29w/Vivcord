namespace Vivcord.Server.Infastructure.Jwt
{
    public static class CookieManager
    {
        private const string Jwt = "jwt";
        private const string RefToken = "refToken";

        private static CookieOptions GetOptions(DateTimeOffset? expires = null) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expires
        };

        public static void SetCookie(this HttpResponse response, string token, TimeProvider timeProvider)
        {
            response.Cookies.Append(Jwt, token, GetOptions(timeProvider.GetUtcNow().AddMinutes(10)));
        }
        public static void SetRefreshCookie(this HttpResponse response, string token, TimeProvider timeProvider)
        {
            response.Cookies.Append(RefToken, token, GetOptions(timeProvider.GetUtcNow().AddDays(1)));
        }
        public static void ClearCookies(this HttpResponse response)
        {
            var options = GetOptions();
            response.Cookies.Delete(Jwt, options);
            response.Cookies.Delete(RefToken, options);
        }
    }
}
