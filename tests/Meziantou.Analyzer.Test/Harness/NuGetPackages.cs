using System.Collections.Concurrent;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using Meziantou.Analyzer.Test.Helpers;

namespace Meziantou.Analyzer.Test.Harness;

/// <summary>
/// Downloads the NuGet packages the tests reference, and caches them for the whole test run. The tests use it for
/// the analyzers they run besides the ones of this repository, which the testing library cannot resolve itself.
/// </summary>
internal static class NuGetPackages
{
    private static readonly ConcurrentDictionary<string, Lazy<Task<string[]>>> Cache = new(StringComparer.Ordinal);

    // HttpClient.Timeout does not apply to reading the response stream, so a stalled connection would hang forever.
    // The result is shared by all the tests through Cache, so it would hang the whole test run.
    private static readonly TimeSpan NuGetDownloadTimeout = TimeSpan.FromSeconds(60);

    public static async Task<string[]> GetReferencesAsync(string packageName, string version, string[] includedPaths)
    {
        var bytes = Encoding.UTF8.GetBytes("v2:" + packageName + '@' + version + ':' + string.Join(',', includedPaths));
        var hash = SHA256.HashData(bytes);
        var key = Convert.ToBase64String(hash).Replace('/', '_');
        var task = Cache.GetOrAdd(key, _ => new Lazy<Task<string[]>>(Download));
        try
        {
            return await task.Value.ConfigureAwait(false);
        }
        catch
        {
            _ = Cache.TryRemove(key, out _);
            throw;
        }

        async Task<string[]> Download()
        {
            var cacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Meziantou.AnalyzerTests", "ref", key);
            var completionFile = Path.Combine(cacheFolder, ".complete");
            bool IsCacheValid()
            {
                if (!Directory.Exists(cacheFolder))
                    return false;

                if (File.Exists(completionFile))
                    return true;

                return Directory.EnumerateFileSystemEntries(cacheFolder).Any();
            }

            if (!IsCacheValid())
            {
                await DownloadPackageWithRetries().ConfigureAwait(false);
            }

            async Task DownloadPackageWithRetries()
            {
                const int MaxAttempts = 5;
                for (var attempt = 1; ; attempt++)
                {
                    try
                    {
                        await DownloadPackage().ConfigureAwait(false);
                        return;
                    }
                    catch (Exception ex) when (!IsLastAttempt(attempt) && IsTransientException(ex))
                    {
                        await Task.Delay(100 * attempt).ConfigureAwait(false);
                    }
                }

                static bool IsLastAttempt(int attempt) => attempt >= MaxAttempts;
                static bool IsTransientException(Exception exception) => exception is HttpRequestException or IOException or InvalidDataException or OperationCanceledException or TimeoutException;
            }

            async Task DownloadPackage()
            {
                var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
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
                        Directory.CreateDirectory(Path.GetDirectoryName(cacheFolder)!);
                        Directory.Move(tempFolder, cacheFolder);
                    }
                    catch (Exception ex)
                    {
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

            var dlls = Directory.GetFiles(cacheFolder, "*.dll", SearchOption.AllDirectories);

            // Filter invalid .NET assembly
            var result = new List<string>();
            foreach (var dll in dlls)
            {
                if (Path.GetFileName(dll) == "System.EnterpriseServices.Wrapper.dll")
                    continue;

                if (Path.GetFileName(dll).EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    using var stream = File.OpenRead(dll);
                    using var peFile = new PEReader(stream);
                    var metadataReader = peFile.GetMetadataReader();
                    result.Add(dll);
                }
                catch
                {
                }
            }

            return [.. result];
        }
    }
}
