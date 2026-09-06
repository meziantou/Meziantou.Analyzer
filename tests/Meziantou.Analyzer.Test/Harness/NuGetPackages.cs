using System.Collections.Concurrent;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using Meziantou.Analyzer.Test.Helpers;

namespace Meziantou.Analyzer.Test.Harness;

/// <summary>
/// Resolves the NuGet packages the tests reference, and caches them for the whole test run. The tests use it for
/// the analyzers they run besides the ones of this repository, which the testing library cannot resolve itself.
/// The packages the build already restored are read from the NuGet global packages folder, so that the tests
/// only download the ones that are not there yet.
/// </summary>
internal static class NuGetPackages
{
    private static readonly ConcurrentDictionary<string, Lazy<Task<string[]>>> Cache = new(StringComparer.Ordinal);

    // HttpClient.Timeout does not apply to reading the response stream, so a stalled connection would hang forever.
    // The result is shared by all the tests through Cache, so it would hang the whole test run.
    private static readonly TimeSpan NuGetDownloadTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The folder NuGet extracts the restored packages to, which the testing library also uses for the packages
    /// referenced by <see cref="Microsoft.CodeAnalysis.Testing.ReferenceAssemblies"/>. It is resolved the way NuGet
    /// does when no NuGet.config sets 'globalPackagesFolder', which is enough as the packages that are not found
    /// there are downloaded.
    /// </summary>
    private static readonly string GlobalPackagesFolder = Environment.GetEnvironmentVariable("NUGET_PACKAGES") is { Length: > 0 } folder
        ? folder
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

    public static async Task<string[]> GetReferencesAsync(string packageName, string version, string[] includedPaths)
    {
        var bytes = Encoding.UTF8.GetBytes("v2:" + packageName + '@' + version + ':' + string.Join(',', includedPaths));
        var hash = SHA256.HashData(bytes);
        var key = Convert.ToBase64String(hash).Replace('/', '_');
        var task = Cache.GetOrAdd(key, _ => new Lazy<Task<string[]>>(Resolve));
        try
        {
            return await task.Value.ConfigureAwait(false);
        }
        catch
        {
            _ = Cache.TryRemove(key, out _);
            throw;
        }

        async Task<string[]> Resolve()
        {
            var packageFolder = GetRestoredPackageFolder(packageName, version) ?? await DownloadPackageWithRetries().ConfigureAwait(false);
            return GetAssemblies(packageFolder, includedPaths);
        }

        async Task<string> DownloadPackageWithRetries()
        {
            var cacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Meziantou.AnalyzerTests", "ref", key);
            var completionFile = Path.Combine(cacheFolder, ".complete");

            // A folder without the completion marker was left behind by an interrupted download,
            // so it holds an incomplete package and must be downloaded again.
            bool IsCacheValid() => File.Exists(completionFile);

            if (IsCacheValid())
                return cacheFolder;

            const int MaxAttempts = 5;
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await DownloadPackage().ConfigureAwait(false);
                    return cacheFolder;
                }
                catch (Exception ex) when (!IsLastAttempt(attempt) && IsTransientException(ex))
                {
                    await Task.Delay(100 * attempt).ConfigureAwait(false);
                }
            }

            static bool IsLastAttempt(int attempt) => attempt >= MaxAttempts;
            static bool IsTransientException(Exception exception) => exception is HttpRequestException or IOException or InvalidDataException or OperationCanceledException or TimeoutException;

            async Task DownloadPackage()
            {
                // The temporary folder is a sibling of the cache folder, so that moving it is a rename.
                // Directory.Move cannot move a folder to another volume, which the temp folder may be on.
                var tempFolder = Path.Combine(Path.GetDirectoryName(cacheFolder)!, Guid.NewGuid().ToString("N"));
                try
                {
                    Directory.CreateDirectory(tempFolder);
                    var url = new Uri($"https://www.nuget.org/api/v2/package/{packageName}/{version}");
                    var content = new MemoryStream();
                    using (var cts = new CancellationTokenSource(NuGetDownloadTimeout))
                    {
                        try
                        {
                            await using var stream = await SharedHttpClient.Instance.GetStreamAsync(url, cts.Token).ConfigureAwait(false);
                            await stream.CopyToAsync(content, cts.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException ex) when (cts.IsCancellationRequested)
                        {
                            throw new TimeoutException($"Downloading '{url}' timed out after {NuGetDownloadTimeout}", ex);
                        }
                    }

                    content.Seek(0, SeekOrigin.Begin);
                    await using var zip = new ZipArchive(content, ZipArchiveMode.Read);

                    foreach (var entry in zip.Entries.Where(file => includedPaths.Any(path => file.FullName.StartsWith(path, StringComparison.Ordinal))))
                    {
                        if (string.IsNullOrEmpty(entry.Name))
                            continue;

                        var destinationPath = Path.Combine(tempFolder, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                        await entry.ExtractToFileAsync(destinationPath, overwrite: true);
                    }

                    await File.WriteAllTextAsync(Path.Combine(tempFolder, ".complete"), string.Empty).ConfigureAwait(false);

                    try
                    {
                        if (Directory.Exists(cacheFolder) && !IsCacheValid())
                        {
                            Directory.Delete(cacheFolder, recursive: true);
                        }

                        Directory.Move(tempFolder, cacheFolder);
                    }
                    catch (Exception ex)
                    {
                        // Another test run may have downloaded the package at the same time
                        if (!IsCacheValid())
                        {
                            throw new InvalidOperationException("Cannot download NuGet package " + packageName + "@" + version + "\n" + ex);
                        }
                    }
                }
                finally
                {
                    if (Directory.Exists(tempFolder))
                    {
                        Directory.Delete(tempFolder, recursive: true);
                    }
                }
            }
        }
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "NuGet names the folders of the global packages folder in lowercase")]
    private static string? GetRestoredPackageFolder(string packageName, string version)
    {
        // NuGet extracts a package to a folder named after the lowercase package name and version,
        // and writes '.nupkg.metadata' in it once the extraction is complete
        var packageFolder = Path.Combine(GlobalPackagesFolder, packageName.ToLowerInvariant(), version.ToLowerInvariant());
        return File.Exists(Path.Combine(packageFolder, ".nupkg.metadata")) ? packageFolder : null;
    }

    private static string[] GetAssemblies(string packageFolder, string[] includedPaths)
    {
        var result = new List<string>();
        foreach (var dll in Directory.EnumerateFiles(packageFolder, "*.dll", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(packageFolder, dll).Replace(Path.DirectorySeparatorChar, '/');
            if (!includedPaths.Any(path => relativePath.StartsWith(path, StringComparison.Ordinal)))
                continue;

            // Filter invalid .NET assembly
            if (Path.GetFileName(dll) is "System.EnterpriseServices.Wrapper.dll")
                continue;

            if (Path.GetFileName(dll).EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                using var stream = File.OpenRead(dll);
                using var peFile = new PEReader(stream);
                _ = peFile.GetMetadataReader();
            }
            catch
            {
                continue;
            }

            result.Add(dll);
        }

        return [.. result];
    }
}
