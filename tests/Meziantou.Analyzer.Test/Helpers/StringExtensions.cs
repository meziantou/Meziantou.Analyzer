
/* Unmerged change from project 'Meziantou.Analyzer.Test (net461)'
Before:
namespace TestHelper
{
#if NET461
    internal static class StringExtensions
    {
        public static bool Contains(this string str, string substring, System.StringComparison stringComparison)
        {
            return str.IndexOf(substring, stringComparison) >= 0;
        }
    }
#endif
}
After:
namespace TestHelper;
#if NET461
internal static class StringExtensions
{
    public static bool Contains(this string str, string substring, System.StringComparison stringComparison)
    {
        return str.IndexOf(substring, stringComparison) >= 0;
    }
}
*/
namespace TestHelper;
