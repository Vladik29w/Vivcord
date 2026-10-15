namespace Vivcord.Server.DTO
{
    public record UploadTokenRequest(string FileName);

    public record UploadTokenResponse(string UploadUrl, string BlobName);
}
