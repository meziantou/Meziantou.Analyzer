namespace Meziantou.Analyzer.Rules;

internal static class UseRegexSourceGeneratorAnalyzerCommon
{
    internal const string PatternIndexName = "PatternIndex";
    internal const string RegexOptionsIndexName = "RegexOptionsIndex";
    internal const string RegexTimeoutIndexName = "RegexTimeoutIndex";
    internal const string RegexTimeoutName = "RegexTimeout";

    /// <summary>
    /// <c>GeneratedRegexAttribute</c> can be applied to partial properties since .NET 9.
    /// It is limited to methods in the previous versions, such as .NET 8.
    /// </summary>
    internal static bool CanBeAppliedToProperties(Compilation compilation, INamedTypeSymbol generatedRegexAttributeSymbol)
    {
        var attributeUsageAttributeSymbol = compilation.GetTypeByMetadataName("System.AttributeUsageAttribute");
        foreach (var attribute in generatedRegexAttributeSymbol.GetAttributes())
        {
            if (attribute.AttributeClass.IsEqualTo(attributeUsageAttributeSymbol))
                return attribute.ConstructorArguments is [{ Value: int validOn }, ..] && ((AttributeTargets)validOn).HasFlag(AttributeTargets.Property);
        }

        // An attribute without AttributeUsage can be applied to all targets
        return true;
    }
}
