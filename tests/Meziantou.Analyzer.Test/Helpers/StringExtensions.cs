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
