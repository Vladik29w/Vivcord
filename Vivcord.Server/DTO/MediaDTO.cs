namespace Vivcord.Server.DTO
{
    public record UploadTokenRequest(string FileName, string ContentType);

    public record UploadTokenResponse(string UploadUrl, string BlobName);
}
