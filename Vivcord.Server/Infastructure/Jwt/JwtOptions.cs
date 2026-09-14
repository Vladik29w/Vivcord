namespace Vivcord.Server.Infastructure.Jwt
{
    public class JwtOptions
    {
        public const string SectionName = "JwtSetting";

        public required string Key { get; init; }
        public required string VivcordServer { get; init; }
        public required string VivcordClient { get; init; }
    }
}
