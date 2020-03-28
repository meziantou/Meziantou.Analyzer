using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace TestHelper
{
    public static class CommonReferences
    {
        private static readonly Lazy<Task<string[]>> s_netStandard2_0;
        private static readonly Lazy<Task<string[]>> s_system_collection_immutable;
        private static readonly Lazy<Task<string[]>> s_system_numerics_vectors;

        public static Task<string[]> NetStandard2_0 => s_netStandard2_0.Value;
        public static Task<string[]> System_Collections_Immutable => s_system_collection_immutable.Value;
        public static Task<string[]> System_Numerics_Vectors => s_system_numerics_vectors.Value;

        static CommonReferences()
        {
            s_netStandard2_0 = new Lazy<Task<string[]>>(() => InitializeNetStandard2_0());
            s_system_collection_immutable = new Lazy<Task<string[]>>(() => InitializeSystem_Collections_Immutable());
            s_system_numerics_vectors = new Lazy<Task<string[]>>(() => InitializeSystem_Numerics_Vectors());
        }

        private static async Task<string[]> InitializeNetStandard2_0()
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), "Meziantou.AnalyzerTests", "ref", "netstandard2.0");
            if (!Directory.Exists(tempFolder) || !Directory.EnumerateFileSystemEntries(tempFolder).Any())
            {
                Directory.CreateDirectory(tempFolder);
                using var httpClient = new HttpClient();
                using var stream = await httpClient.GetStreamAsync("https://www.nuget.org/api/v2/package/NETStandard.Library/2.0.3").ConfigureAwait(false);
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

                foreach (var entry in zip.Entries.Where(file => file.FullName.StartsWith("build/netstandard2.0/ref/", System.StringComparison.Ordinal)))
                {
                    entry.ExtractToFile(Path.Combine(tempFolder, entry.Name));
                }
            }

            return Directory.GetFiles(tempFolder, "*.dll");
        }

        private static async Task<string[]> InitializeSystem_Collections_Immutable()
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), "Meziantou.AnalyzerTests", "ref", "System_Collections_Immutable");
            if (!Directory.Exists(tempFolder) || !Directory.EnumerateFileSystemEntries(tempFolder).Any())
            {
                Directory.CreateDirectory(tempFolder);
                using var httpClient = new HttpClient();
                using var stream = await httpClient.GetStreamAsync("https://www.nuget.org/api/v2/package/System.Collections.Immutable/1.5.0").ConfigureAwait(false);
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

                foreach (var entry in zip.Entries.Where(file => file.FullName.StartsWith("lib/netstandard2.0/", System.StringComparison.Ordinal)))
                {
                    entry.ExtractToFile(Path.Combine(tempFolder, entry.Name));
                }
            }

            return Directory.GetFiles(tempFolder, "*.dll");
        }


        private static async Task<string[]> InitializeSystem_Numerics_Vectors()
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), "Meziantou.AnalyzerTests", "ref", "System_Numerics_Vectors");
            if (!Directory.Exists(tempFolder) || !Directory.EnumerateFileSystemEntries(tempFolder).Any())
            {
                Directory.CreateDirectory(tempFolder);
                using var httpClient = new HttpClient();
                using var stream = await httpClient.GetStreamAsync("https://www.nuget.org/api/v2/package/System.Numerics.Vectors/4.5.0").ConfigureAwait(false);
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

                foreach (var entry in zip.Entries.Where(file => file.FullName.StartsWith("ref/netstandard2.0/", StringComparison.Ordinal)))
                {
                    entry.ExtractToFile(Path.Combine(tempFolder, entry.Name));
                }
            }

            return Directory.GetFiles(tempFolder, "*.dll");
        }
    }
}
