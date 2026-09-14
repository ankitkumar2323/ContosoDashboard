using Microsoft.AspNetCore.Http;

namespace ContosoDashboard.Services;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;

    public LocalFileStorageService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredRoot = configuration["Documents:StorageRoot"] ?? "AppData/uploads";
        _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredRoot));
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveQuarantineAsync(IFormFile file, string relativePath, CancellationToken cancellationToken = default)
    {
        var safePath = Resolve(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(safePath)!);
        await using var target = File.Create(safePath);
        await file.CopyToAsync(target, cancellationToken);
        return relativePath;
    }

    public Task PromoteAsync(string quarantinePath, string availablePath, CancellationToken cancellationToken = default)
    {
        var source = Resolve(quarantinePath);
        var target = Resolve(availablePath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Move(source, target, overwrite: false);
        return Task.CompletedTask;
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var path = Resolve(relativePath);
        if (!File.Exists(path)) return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(File.OpenRead(path));
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var path = Resolve(relativePath);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string Resolve(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(normalized) || normalized.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid storage path.");

        var fullPath = Path.GetFullPath(Path.Combine(_root, normalized));
        if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid storage path.");
        return fullPath;
    }
}
