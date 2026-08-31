using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.ConstructorArgumentParametersShouldExistInConstructorsAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ConstructorArgumentParametersShouldExistInConstructorsAnalyzerTests
{
    // ConstructorArgumentAttribute is in WindowsBase, which the default set of .NET Framework assemblies does not contain
    private static AnalyzerTest CreateTest() => new() { ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net48.Wpf };

    [Fact]
    public Task WrongParameterName()
    {
        var test = CreateTest();
        test.TestCode = """
            public class MyExtension
            {
                public MyExtension() { }

                public MyExtension(object value1)
                {
                    Value1 = value1;
                }

                [{|MA0083:System.Windows.Markup.ConstructorArgument("value2")|}]
                public object Value1 { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GoodParameterName()
    {
        var test = CreateTest();
        test.TestCode = """
            public class MyExtension
            {
                public MyExtension() { }

                public MyExtension(object value1)
                {
                    Value1 = value1;
                }

                [System.Windows.Markup.ConstructorArgument("value1")]
                public object Value1 { get; set; }
            }
            """;

        return test.RunAsync();
    }
}
