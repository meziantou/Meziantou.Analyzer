using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.MissingNotNullWhenAttributeOnEqualsAnalyzer,
    Meziantou.Analyzer.Rules.MissingNotNullWhenAttributeOnEqualsFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class MissingNotNullWhenAttributeOnEqualsAnalyzerTests
{
    [Fact]
    public Task Equals_Object_WithoutAttribute_ShouldReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class Sample
            {
                public override bool Equals(object? {|MA0186:obj|})
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_Object_WithoutAttribute_ShouldFix()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            class Sample
            {
                public override bool Equals(object? {|MA0186:obj|})
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;
        test.FixedCode = """
            using System.Diagnostics.CodeAnalysis;

            class Sample
            {
                public override bool Equals([NotNullWhen(true)] object? obj)
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_Object_WithWrongAttributeValue_ShouldFix()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            class Sample
            {
                public override bool Equals([NotNullWhen(false)] object? {|MA0186:obj|})
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;
        test.FixedCode = """
            using System.Diagnostics.CodeAnalysis;

            class Sample
            {
                public override bool Equals([NotNullWhen(true)] object? obj)
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_Object_WithAttribute_ShouldNotReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            class Sample
            {
                public override bool Equals([NotNullWhen(true)] object? obj)
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_IEquatable_WithoutAttribute_ShouldReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System;

            class Sample : IEquatable<Sample>
            {
                public bool Equals(Sample? {|MA0186:other|})
                {
                    return false;
                }

                public override bool Equals(object? {|MA0186:obj|})
                {
                    return Equals(obj as Sample);
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_IEquatable_WithAttribute_ShouldNotReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System;
            using System.Diagnostics.CodeAnalysis;

            class Sample : IEquatable<Sample>
            {
                public bool Equals([NotNullWhen(true)] Sample? other)
                {
                    return false;
                }

                public override bool Equals([NotNullWhen(true)] object? obj)
                {
                    return Equals(obj as Sample);
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_NonNullableParameter_ShouldNotReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class Sample
            {
                public override bool Equals(object obj)
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotEqualsMethod_ShouldNotReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class Sample
            {
                public bool IsEqual(object? obj)
                {
                    return false;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PrivateEquals_ShouldNotReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class Sample
            {
                private bool Equals(object? obj)
                {
                    return false;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StaticEquals_ShouldNotReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class Sample
            {
                public static bool Equals(object? obj1, object? obj2)
                {
                    return false;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_WrongSignature_ShouldNotReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class Sample
            {
                public bool Equals(object? obj, int x)
                {
                    return false;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_IEquatable_BothMethodsWithoutAttribute_ShouldReportBothDiagnostics()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System;

            class Sample : IEquatable<Sample>
            {
                public bool Equals(Sample? {|MA0186:other|})
                {
                    return false;
                }

                public override bool Equals(object? {|MA0186:obj|})
                {
                    return Equals(obj as Sample);
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_IEquatable_ValueType_ShouldNotReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System;

            struct Sample : IEquatable<Sample>
            {
                public bool Equals(Sample other)
                {
                    return false;
                }

                public override bool Equals(object? {|MA0186:obj|})
                {
                    return obj is Sample other && Equals(other);
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_NullableDisabled_ShouldNotReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            #nullable disable
            class Sample
            {
                public override bool Equals(object obj)
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_NullableEnabled_ShouldReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            #nullable enable
            class Sample
            {
                public override bool Equals(object? {|MA0186:obj|})
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_NullableEnabledThenDisabled_ShouldNotReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            #nullable enable
            class Sample1
            {
                public override bool Equals(object? {|MA0186:obj|})
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }

            #nullable disable
            class Sample2
            {
                public override bool Equals(object obj)
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }
}
