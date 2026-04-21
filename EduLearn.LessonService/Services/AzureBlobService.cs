using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using EduLearn.LessonService.Interfaces;

namespace EduLearn.LessonService.Services;

// Azure Blob Storage service for lesson content (videos, PDFs)
// Dev mode: returns mock URLs when Azure not configured — no Azure account needed locally
public class AzureBlobService : IAzureBlobService
{
    private readonly string? _connectionString;
    private readonly string _containerName;
    private readonly ILogger<AzureBlobService> _logger;
    private readonly bool _isDevMode; // true when Azure not configured

    public AzureBlobService(IConfiguration config, ILogger<AzureBlobService> logger)
    {
        _connectionString = config["Azure:StorageConnectionString"];
        _containerName    = config["Azure:ContainerName"] ?? "edulearn-content";
        _logger           = logger;
        _isDevMode        = string.IsNullOrEmpty(_connectionString) ||
                            _connectionString == "YOUR_AZURE_CONNECTION_STRING";
    }

    // Upload file to Azure Blob Storage — returns blob URL
    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        if (_isDevMode)
        {
            _logger.LogInformation("Dev mode: mock upload for {FileName}", fileName);
            return $"https://mock-storage.blob.core.windows.net/{_containerName}/{fileName}";
        }

        var blobClient = GetBlobClient(fileName);
        await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });
        return blobClient.Uri.ToString();
    }

    // Generate SAS URL — time-limited secure access link
    // Videos: 24h expiry | Certificates: 1h expiry
    public async Task<string> GenerateSasUrlAsync(string blobUrl, TimeSpan expiry)
    {
        if (_isDevMode)
            return $"{blobUrl}?sv=mock&se={DateTime.UtcNow.Add(expiry):O}&sig=devmode";

        var blobName  = new Uri(blobUrl).LocalPath.TrimStart('/').Replace($"{_containerName}/", "");
        var blobClient = GetBlobClient(blobName);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName          = blobName,
            Resource          = "b",
            ExpiresOn         = DateTimeOffset.UtcNow.Add(expiry)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        await Task.CompletedTask;
        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }

    public async Task DeleteFileAsync(string blobUrl)
    {
        if (_isDevMode) return;
        var blobName = new Uri(blobUrl).LocalPath.TrimStart('/').Replace($"{_containerName}/", "");
        await GetBlobClient(blobName).DeleteIfExistsAsync();
    }

    private BlobClient GetBlobClient(string blobName)
        => new BlobContainerClient(_connectionString, _containerName).GetBlobClient(blobName);
}