using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

var packages = new (string PackageName, string PackageVersion)[]
{
    ("Microsoft.NETCore.App.Ref", "6.0.0-preview.7.21377.19"),
    ("Microsoft.AspNetCore.App.Ref", "6.0.0-preview.7.21378.6"),
    ("Microsoft.WindowsDesktop.App.Ref", "6.0.0-preview.7.21378.9"),
};

using var httpClient = new HttpClient();
var types = new HashSet<string>();
foreach (var (packageName, packageVersion) in packages)
{
    using var nugetStream = await httpClient.GetStreamAsync($"https://www.nuget.org/api/v2/package/{packageName}/{packageVersion}");
    using var zipArchive = new ZipArchive(nugetStream, ZipArchiveMode.Read);

    foreach (var entry in zipArchive.Entries)
    {
        if (!string.Equals(Path.GetExtension(entry.Name), ".dll", StringComparison.OrdinalIgnoreCase))
            continue;

        using var entryStream = CopyToMemoryStream(zipArchive.GetEntry(entry.FullName)!);
        using var portableExecutableReader = new PEReader(entryStream);
        var metadataReader = portableExecutableReader.GetMetadataReader();

        foreach (var typeDefHandle in metadataReader.TypeDefinitions)
        {
            var typeDef = metadataReader.GetTypeDefinition(typeDefHandle);
            var ns = metadataReader.GetString(typeDef.Namespace);
            var typeName = metadataReader.GetString(typeDef.Name);
            if (string.IsNullOrEmpty(ns))
                continue;

            if (!typeDef.Attributes.HasFlag(TypeAttributes.Public))
                continue;

            types.Add(ns + "." + typeName);
        }
    }
}

File.WriteAllLines("../../../../Meziantou.Analyzer/Resources/bcl.txt", types.OrderBy(t => t));

static MemoryStream CopyToMemoryStream(ZipArchiveEntry entry)
{
    var ms = new MemoryStream();
    using var stream = entry.Open();
    stream.CopyTo(ms);
    ms.Seek(0, SeekOrigin.Begin);
    return ms;
}
