using Microsoft.CodeAnalysis.CSharp;

namespace Meziantou.Analyzer;

internal static class LanguageVersionExtensions
{
    public static bool IsCSharp10OrAbove(this LanguageVersion languageVersion)
    {
        return languageVersion >= (LanguageVersion)1000;
    }

    public static bool IsCSharp8OrAbove(this LanguageVersion languageVersion)
    {
        return languageVersion >= LanguageVersion.CSharp8;
    }
}
