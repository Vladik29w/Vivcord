using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using ErrorOr;
using Vivcord.Server.DTO;

namespace Vivcord.Server.Services
{
    public static class BlobContainers
    {
        public const string ChatMedia = "chat-media";
        public const string ProfilePictures = "profile-pictures";
    }

    public interface IBlobStorageService
    {
        ErrorOr<UploadTokenResponse> GenerateUploadSasToken(string containerName, string fileName, TimeSpan? expiry = null);
        ErrorOr<string> GenerateSasReadUrl(string containerName, string blobName, TimeSpan? expiry = null);
        string GetPublicUrl(string containerName, string blobName);
        Task<ErrorOr<Success>> DeleteBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    }

    public class BlobStorageService(IConfiguration configuration, TimeProvider timeProvider) : IBlobStorageService
    {
        private readonly string connectionString = configuration["AzureBlobStorage:ConnectionString"] ?? string.Empty;
        private readonly TimeProvider timeProvider = timeProvider ?? TimeProvider.System;

        private const string DefaultCacheControl = "public, max-age=31536000, immutable";

        public ErrorOr<UploadTokenResponse> GenerateUploadSasToken(
            string containerName,
            string fileName,
            TimeSpan? expiry = null)
        {
            try
            {
                var blobName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
                var blobClient = new BlobClient(connectionString, containerName, blobName);

                if (!blobClient.CanGenerateSasUri)
                    return Error.Unexpected(description: "BlobClient cannot generate SAS URI. Ensure the connection string includes the account key.");

                var now = timeProvider.GetUtcNow();

                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = containerName,
                    BlobName = blobName,
                    Resource = "b",
                    ExpiresOn = now.Add(expiry ?? TimeSpan.FromMinutes(15))
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

        public ErrorOr<string> GenerateSasReadUrl(
            string containerName,
            string blobName,
            TimeSpan? expiry = null)
        {
            try
            {
                var blobClient = new BlobClient(connectionString, containerName, blobName);

                if (!blobClient.CanGenerateSasUri)
                    return Error.Unexpected(description: "BlobClient cannot generate SAS URI. Ensure the connection string includes the account key.");

                var now = timeProvider.GetUtcNow();

                // Якщо expiry не задано, вирівнюємо до початку поточної доби (00:00 UTC) + 7 днів 
                // для стабільності URL в браузерному кеші.
                // Якщо передано кастомний expiry — рахуємо суворо від поточного часу now.
                DateTimeOffset expiresOn = expiry.HasValue
                    ? now.Add(expiry.Value)
                    : new DateTimeOffset(now.Date, TimeSpan.Zero).AddDays(7);

                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = containerName,
                    BlobName = blobName,
                    Resource = "b",
                    ExpiresOn = expiresOn,
                    CacheControl = DefaultCacheControl
                };
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                return blobClient.GenerateSasUri(sasBuilder).ToString();
            }
            catch (Exception ex)
            {
                return Error.Unexpected(description: $"Failed to generate read SAS URL: {ex.Message}");
            }
        }

        public string GetPublicUrl(string containerName, string blobName)
        {
            var blobClient = new BlobClient(connectionString, containerName, blobName);
            return blobClient.Uri.ToString();
        }

        public async Task<ErrorOr<Success>> DeleteBlobAsync(
            string containerName,
            string blobName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var blobClient = new BlobClient(connectionString, containerName, blobName);
                await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
                return Result.Success;
            }
            catch (Exception ex)
            {
                return Error.Unexpected(description: $"Failed to delete blob '{blobName}' from container '{containerName}': {ex.Message}");
            }
        }
    }
}