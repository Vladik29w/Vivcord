namespace Vivcord.Server.DTO
{
    public record UploadTokenRequest(string FileName, string ContentType);

    public record UploadTokenResponse(
        string UploadUrl,  // SAS URL for PUT directly to Azure Blob
        string BlobName    // blob path to store in DB and pass via SignalR
    );
}
