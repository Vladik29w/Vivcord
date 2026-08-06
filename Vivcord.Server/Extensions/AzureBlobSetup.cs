using Microsoft.Extensions.DependencyInjection;
using Vivcord.Server.Services;

namespace Vivcord.Server.Extensions
{
    public static class AzureBlobSetup
    {
        public static IServiceCollection AddVivcordAzureBlob(this IServiceCollection services)
        {
            // BlobStorageService builds BlobClient internally with StorageSharedKeyCredential (read from config)
            // which is required for CanGenerateSasUri == true. Registered as Singleton — stateless, thread-safe.
            services.AddSingleton<IBlobStorageService, BlobStorageService>();

            return services;
        }
    }
}
