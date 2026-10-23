using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Infrastructure.Storage;

namespace Tabsan.EduSphere.API.Services;

public static class MediaStorageServiceCollectionExtensions
{
    public static IServiceCollection AddConfiguredMediaStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MediaStorageOptions>(configuration.GetSection(MediaStorageOptions.SectionName));

        var options = configuration.GetSection(MediaStorageOptions.SectionName).Get<MediaStorageOptions>() ?? new MediaStorageOptions();
        var provider = options.Provider?.Trim();

        // Final-Touches Phase 28 Stage 28.3 — configurable storage-provider selection.
        // Plan M14.2 — AzureBlob and S3 share files between API instances (submissions, conversions, uploads, SCORM).
        if (string.Equals(provider, "AzureBlob", StringComparison.OrdinalIgnoreCase) || string.Equals(provider, "S3", StringComparison.OrdinalIgnoreCase))
        {
            var stores = configuration.GetSection(MediaStorageOptions.SectionName).Get<ObjectStoreOptions>() ?? new ObjectStoreOptions();
            services.AddSingleton<IObjectStore>(_ => string.Equals(provider, "S3", StringComparison.OrdinalIgnoreCase)
                ? new S3ObjectStore(stores.S3)
                : new AzureBlobObjectStore(stores.AzureBlob));
            services.AddScoped<IMediaStorageService, ObjectStoreMediaStorageService>();
        }
        else if (string.Equals(provider, "Blob", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IMediaStorageService, BlobMediaStorageService>();
        }
        else
        {
            services.AddScoped<IMediaStorageService, LocalMediaStorageService>();
        }

        return services;
    }
}
