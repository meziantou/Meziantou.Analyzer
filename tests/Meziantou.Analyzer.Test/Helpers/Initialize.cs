using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace TestHelper
{
    [TestClass]
    public sealed class Initialize
    {
        public static IReadOnlyList<string> NetStandard2_0 { get; private set; }

        [AssemblyInitialize]
        public static async Task AssemblyInitialize(TestContext _)
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), "Meziantou.AnalyzerTests", "ref", "netstandard2.0");
            if (!Directory.Exists(tempFolder) || !Directory.EnumerateFileSystemEntries(tempFolder).Any())
            {
                Directory.CreateDirectory(tempFolder);
                using (var httpClient = new HttpClient())
                {
                    using (var stream = await httpClient.GetStreamAsync("https://www.nuget.org/api/v2/package/NETStandard.Library/2.0.3"))
                    using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
                    {
                        foreach (var entry in zip.Entries.Where(file => file.FullName.StartsWith("build/netstandard2.0/ref/", System.StringComparison.Ordinal)))
                        {
                            entry.ExtractToFile(Path.Combine(tempFolder, entry.Name));
                        }
                    }
                }
            }

            NetStandard2_0 = Directory.GetFiles(tempFolder, "*.dll");
        }
    }
}
