using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Infastructure.Jwt;

namespace Vivcord.Server.Services
{
    public interface IProfileService
    {
        public Task<ErrorOr<UserProfileDTO>> GetUserProfile(Guid userId, CancellationToken ct = default);
        public Task<ErrorOr<string>> ChangeUserDisplayName(Guid userId, string displayName, CancellationToken ct = default);
        public ErrorOr<UploadTokenResponse> GetProfilePictureSasToken(string fileName, string contentType);
        public Task<ErrorOr<Success>> UpdateProfilePictureUrl(Guid userId, string blobName, CancellationToken ct = default);
    }
    public class ProfileService(MainDbContext dbContext, IBlobStorageService blobStorageService, ITokenService tokenService) : IProfileService
    {
        public async Task<ErrorOr<UserProfileDTO>> GetUserProfile(Guid userId, CancellationToken ct = default)
        {
            var user = await dbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => new UserProfileDTO(u.Id, u.UserName, u.DisplayName, u.ProfilePictureUrl))
                .FirstOrDefaultAsync(ct);

            if (user is null)
                return Error.NotFound("UserNotFound", "User not found.");

            return user;
        }

        public async Task<ErrorOr<string>> ChangeUserDisplayName(Guid userId, string displayName, CancellationToken ct = default)
        { 
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null)
                return Error.NotFound("UserNotFound", "User not found.");

            user.DisplayName = displayName;
            await dbContext.SaveChangesAsync(ct);

            var token = await tokenService.GetTokenAsync(user);
            return token;
        }

        public ErrorOr<UploadTokenResponse> GetProfilePictureSasToken(string fileName, string contentType)
        => blobStorageService.GenerateUploadSasToken(BlobContainers.ProfilePictures, fileName, contentType, TimeSpan.FromMinutes(15));

        public async Task<ErrorOr<Success>> UpdateProfilePictureUrl(Guid userId, string blobName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blobName))
                return Error.Validation("InvalidBlobName", "Blob name is required.");

            var user = await dbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.ProfilePictureUrl })
                .FirstOrDefaultAsync(ct);

            if (user is null)
                return Error.NotFound("UserNotFound", "User not found.");

            string newBlobUrl = blobStorageService.GetPublicUrl(BlobContainers.ProfilePictures, blobName);

            await dbContext.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(u => u.SetProperty(p => p.ProfilePictureUrl, newBlobUrl), ct);

            string? oldBlobName = Uri.TryCreate(user.ProfilePictureUrl, UriKind.Absolute, out var uri) ? Path.GetFileName(uri.LocalPath) : null;

            if (!string.IsNullOrWhiteSpace(oldBlobName) && !string.Equals(oldBlobName, blobName, StringComparison.OrdinalIgnoreCase))
                await blobStorageService.DeleteBlobAsync(BlobContainers.ProfilePictures, oldBlobName, ct);

            return Result.Success;
        }
    }
}