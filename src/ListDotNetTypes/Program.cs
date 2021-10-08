using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

var packages = new (string PackageName, string PackageVersion)[]
{
    ("Microsoft.NETCore.App.Ref", "6.0.0-rc.1.21451.13"),       // https://www.nuget.org/packages/Microsoft.NETCore.App.Ref
    ("Microsoft.AspNetCore.App.Ref", "6.0.0-rc.1.21452.15"),    // https://www.nuget.org/packages/Microsoft.AspNetCore.App.Ref
    ("Microsoft.WindowsDesktop.App.Ref", "6.0.0-rc.1.21451.3"), // https://www.nuget.org/packages/Microsoft.WindowsDesktop.App.Ref
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

File.WriteAllLines(args.Length > 0 ? args[0] : "../../../../Meziantou.Analyzer/Resources/bcl.txt", types.OrderBy(t => t));

static MemoryStream CopyToMemoryStream(ZipArchiveEntry entry)
{
    var ms = new MemoryStream();
    using var stream = entry.Open();
    stream.CopyTo(ms);
    ms.Seek(0, SeekOrigin.Begin);
    return ms;
}
