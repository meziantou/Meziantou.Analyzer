using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.ValidateFixedAddressValueTypeAttributeUsageAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ValidateFixedAddressValueTypeAttributeUsageAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task ValidField()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                [System.Runtime.CompilerServices.FixedAddressValueType]
                static int _field;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FieldMustBeStatic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                [System.Runtime.CompilerServices.FixedAddressValueType]
                int {|MA0207:_field|};
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FieldTypeMustBeValueType()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                [System.Runtime.CompilerServices.FixedAddressValueType]
                static {|MA0208:string|} _field;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task BothDiagnostics()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                [System.Runtime.CompilerServices.FixedAddressValueType]
                {|MA0208:string|} {|MA0207:_field|};
            }
            """;

        return test.RunAsync();
    }
}
