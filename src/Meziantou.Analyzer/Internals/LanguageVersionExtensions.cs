using Microsoft.CodeAnalysis.CSharp;

namespace Meziantou.Analyzer.Internals;

internal static class LanguageVersionExtensions
{
    public static bool IsCSharp10OrAbove(this LanguageVersion languageVersion)
    {
        return languageVersion >= (LanguageVersion)1000;
    }

    public static bool IsCSharp11OrAbove(this LanguageVersion languageVersion)
    {
        return languageVersion >= (LanguageVersion)1100;
    }

    public static bool IsCSharp13OrAbove(this LanguageVersion languageVersion)
    {
        return languageVersion >= (LanguageVersion)1300;
    }

    public static bool IsCSharp14OrAbove(this LanguageVersion languageVersion)
    {
        return languageVersion >= (LanguageVersion)1400;
    }

    public static bool IsCSharp12OrAbove(this LanguageVersion languageVersion)
    {
        return languageVersion >= LanguageVersion.CSharp12;
    }

    public static bool IsCSharp8OrAbove(this LanguageVersion languageVersion)
    {
        return languageVersion >= LanguageVersion.CSharp8;
    }
}
