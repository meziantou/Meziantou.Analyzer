#pragma warning disable CA1812
#pragma warning disable CA1852
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

var packages = new[]
{
    "Microsoft.NETCore.App.Ref",        // https://www.nuget.org/packages/Microsoft.NETCore.App.Ref
    "Microsoft.AspNetCore.App.Ref",     // https://www.nuget.org/packages/Microsoft.AspNetCore.App.Ref
    "Microsoft.WindowsDesktop.App.Ref", // https://www.nuget.org/packages/Microsoft.WindowsDesktop.App.Ref
};

var cache = new SourceCacheContext();
var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

foreach (var includePreview in new[] { false, true })
{
    var types = new HashSet<string>();
    foreach (var packageName in packages)
    {
        var versions = await resource.GetAllVersionsAsync(packageName, cache, NullLogger.Instance, CancellationToken.None);
        var latestVersion = versions.Where(v => includePreview || !v.IsPrerelease).Max();
        Console.WriteLine(packageName + "@" + latestVersion);

        using var packageStream = new MemoryStream();
        await resource.CopyNupkgToStreamAsync(packageName, latestVersion, packageStream, cache, NullLogger.Instance, CancellationToken.None);
        packageStream.Seek(0, SeekOrigin.Begin);

        using var zipArchive = new ZipArchive(packageStream, ZipArchiveMode.Read);
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

    await File.WriteAllLinesAsync(Path.Combine(args.Length > 0 ? args[0] : "../../../../Meziantou.Analyzer/Resources/", includePreview ? "bcl-preview.txt" : "bcl.txt"), types.OrderBy(t => t));
}

static MemoryStream CopyToMemoryStream(ZipArchiveEntry entry)
{
    var ms = new MemoryStream();
    using var stream = entry.Open();
    stream.CopyTo(ms);
    ms.Seek(0, SeekOrigin.Begin);
    return ms;
}
