using Meziantou.Analyzer.Annotations;
using Microsoft.CodeAnalysis.Testing;
using MetadataReference = Microsoft.CodeAnalysis.MetadataReference;

namespace Meziantou.Analyzer.Test.Harness;

internal static class ProjectStateExtensions
{
    private const string EditorConfigFileName = "/.editorconfig";

    /// <summary>
    /// Configures the analyzers with an <c>.editorconfig</c> file applying to all the C# files of the project,
    /// the way <see cref="TestHelper.ProjectBuilder.AddAnalyzerConfiguration(string, string)"/> does.
    /// Replaces the configuration set by a previous call.
    /// </summary>
    public static void SetConfiguration(this ProjectState state, string key, string value) =>
        state.SetConfiguration((key, value));

    /// <inheritdoc cref="SetConfiguration(ProjectState, string, string)" />
    public static void SetConfiguration(this ProjectState state, params (string Key, string Value)[] values)
    {
        var content = new StringBuilder("[*.cs]\n");
        foreach (var (key, value) in values)
        {
            content.Append(key).Append(" = ").Append(value).Append('\n');
        }

        _ = state.AnalyzerConfigFiles.RemoveAll(file => file.filename == EditorConfigFileName);
        state.AnalyzerConfigFiles.Add((EditorConfigFileName, content.ToString()));
    }

    /// <summary>
    /// References the <c>Meziantou.Analyzer.Annotations</c> assembly, the way
    /// <see cref="TestHelper.ProjectBuilder.AddMeziantouAttributes"/> does.
    /// </summary>
    public static void AddMeziantouAnnotations(this ProjectState state) =>
        state.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(RequireNamedArgumentAttribute).Assembly.Location));
}
