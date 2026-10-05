using System.Security.Cryptography;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace CF.Events.Web.Services;

public interface IAssetIntegrityService
{
    string? GetIntegrityHash(string? assetPath);
}

public class AssetIntegrityService : IAssetIntegrityService
{
    private static readonly string[] ScannableFolders = ["css", "js"];
    private readonly Dictionary<string, string> _hashes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<AssetIntegrityService> _logger;

    public AssetIntegrityService(
        IWebHostEnvironment environment,
        ILogger<AssetIntegrityService> logger)
    {
        _logger = logger;
        LoadHashes(environment);
    }

    private void LoadHashes(IWebHostEnvironment environment)
    {
        try
        {
            var webRootDir = environment.WebRootPath;
            if (!Directory.Exists(webRootDir))
            {
                _logger.LogWarning("wwwroot directory not found at {WebRootDir}", webRootDir);
                return;
            }

            var extensions = new[] { ".min.css", ".min.js" };

            foreach (var folder in ScannableFolders)
            {
                if (string.IsNullOrWhiteSpace(folder)) continue;

                var cleanFolder = folder.Trim().TrimStart('/', '\\');
                var folderPath = Path.IsPathRooted(folder) ? folder : Path.Combine(webRootDir, cleanFolder);

                if (!Directory.Exists(folderPath))
                {
                    _logger.LogInformation("Asset folder not found at {FolderPath}, skipping", folderPath);
                    continue;
                }

                foreach (var filePath in Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories))
                {
                    if (!extensions.Any(ext => filePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    var bytes = File.ReadAllBytes(filePath);
                    var hash = $"sha256-{Convert.ToBase64String(SHA256.HashData(bytes))}";

                    var relativeToWebRoot = Path.GetRelativePath(webRootDir, filePath).Replace('\\', '/');
                    var relativeToContentRoot = $"wwwroot/{relativeToWebRoot}";

                    _hashes[relativeToWebRoot] = hash;
                    _hashes[relativeToContentRoot] = hash;

                    var normalized = NormalizePath(relativeToWebRoot);
                    _hashes[normalized] = hash;
                }
            }

            _logger.LogInformation("Loaded {Count} asset integrity hashes into memory from {WebRootDir}", _hashes.Count, webRootDir);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute asset integrity hashes from wwwroot");
        }
    }

    public string? GetIntegrityHash(string? assetPath)
    {
        if (!assetPath.HasValue()) return null;

        var normalizedPath = NormalizePath(assetPath);
        return _hashes.TryGetValue(normalizedPath, out var hash)
               || _hashes.TryGetValue($"wwwroot/{normalizedPath}", out hash) ? hash : null;
    }

    private static string NormalizePath(string path)
    {
        var clean = path.Split('?')[0].Split('#')[0].Trim().Replace('\\', '/');
        if (clean.StartsWith("~/"))
            clean = clean[2..];
        else if (clean.StartsWith('/'))
            clean = clean[1..];
        return clean;
    }
}
