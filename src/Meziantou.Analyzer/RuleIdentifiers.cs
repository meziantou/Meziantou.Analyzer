using System.Globalization;

namespace Meziantou.Analyzer
{
    internal static class RuleIdentifiers
    {
        public const string UseStringComparison = "MA0001";
        public const string UseStringComparerInHashSetConstructor = "MA0002";
        public const string UseNamedParameter = "MA0003";
        public const string UseConfigureAwaitFalse = "MA0004";
        public const string UseArrayEmpty = "MA0005";

        public static string GetHelpUri(string idenfifier)
        {
            return string.Format(CultureInfo.InvariantCulture, "https://github.com/meziantou/Meziantou.Analyzer/blob/master/docs/Rules/{0}.md", idenfifier);
        }
    }
}
