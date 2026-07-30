using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using ErrorOr;
using Vivcord.Server.DTO;

namespace Vivcord.Server.Services
{
    public interface IBlobStorageService
    {
        ErrorOr<UploadTokenResponse> GenerateUploadSasToken(string fileName, string contentType);
        ErrorOr<string> GenerateSasReadUrl(string blobName);
    }

    public class BlobStorageService : IBlobStorageService
    {
        private readonly string _connectionString;
        private readonly string _containerName;

        public BlobStorageService(IConfiguration configuration)
        {
            _connectionString = configuration["AzureBlobStorage:ConnectionString"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException("AzureBlobStorage:ConnectionString is not configured.");

            _containerName = configuration["AzureBlobStorage:ContainerName"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(_containerName))
                throw new InvalidOperationException("AzureBlobStorage:ContainerName is not configured.");
        }

        /// <summary>
        /// Generates a short-lived SAS upload URL (15 min, write-only).
        /// The client PUTs the file directly to Azure — no bytes pass through the backend.
        /// </summary>
        public ErrorOr<UploadTokenResponse> GenerateUploadSasToken(string fileName, string contentType)
        {
            try
            {
                var blobName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";

                var blobClient = new BlobClient(_connectionString, _containerName, blobName);

                if (!blobClient.CanGenerateSasUri)
                    return Error.Unexpected(description: "BlobClient cannot generate SAS URI. Ensure the connection string includes the account key.");

                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = _containerName,
                    BlobName = blobName,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15),
                    ContentType = contentType,
                };
                sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

                var uploadUrl = blobClient.GenerateSasUri(sasBuilder).ToString();

                return new UploadTokenResponse(uploadUrl, blobName);
            }
            catch (Exception ex)
            {
                return Error.Unexpected(description: $"Failed to generate upload SAS token: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates a read-only SAS URL (1 hour) for an existing blob.
        /// Called on each GetChatHistory — tokens are never stored in the database.
        /// </summary>
        public ErrorOr<string> GenerateSasReadUrl(string blobName)
        {
            try
            {
                var blobClient = new BlobClient(_connectionString, _containerName, blobName);

                if (!blobClient.CanGenerateSasUri)
                    return Error.Unexpected(description: "BlobClient cannot generate SAS URI. Ensure the connection string includes the account key.");

                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = _containerName,
                    BlobName = blobName,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
                };
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                return blobClient.GenerateSasUri(sasBuilder).ToString();
            }
            catch (Exception ex)
            {
                return Error.Unexpected(description: $"Failed to generate read SAS URL: {ex.Message}");
            }
        }
    }
}
