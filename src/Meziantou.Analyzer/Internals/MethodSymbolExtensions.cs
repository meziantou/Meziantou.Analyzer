namespace Meziantou.Analyzer.Internals;

internal static class MethodSymbolExtensions
{
    private static readonly string[] MsTestNamespaceParts = ["Microsoft", "VisualStudio", "TestTools", "UnitTesting"];
    private static readonly string[] NunitNamespaceParts = ["NUnit", "Framework"];
    private static readonly string[] XunitNamespaceParts = ["Xunit"];

    public static bool IsUnitTestMethod(this IMethodSymbol methodSymbol)
    {
        var attributes = methodSymbol.GetAttributes();
        foreach (var attribute in attributes)
        {
            var type = attribute.AttributeClass;
            while (type is not null)
            {
                var ns = type.ContainingNamespace;
                if (ns.MatchesNamespace(MsTestNamespaceParts) ||
                    ns.MatchesNamespace(NunitNamespaceParts) ||
                    ns.MatchesNamespace(XunitNamespaceParts))
                {
                    return true;
                }

                type = type.BaseType;
            }
        }

        return false;
    }
}
