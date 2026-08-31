using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.FileNameMustMatchTypeNameAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class FileNameMustMatchTypeNameAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task DoesNotMatchFileName()
    {
        var test = CreateTest();
        test.TestCode = """
            class {|#0:Sample|}
            {
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0048", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("File name must match type name (class Sample), expected file name: 'Sample'"));

        return test.RunAsync();
    }

    [Fact]
    public Task DoesMatchFileNameBeforeDot()
    {
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Sample.xaml.cs", """
            class Sample
            {
            }
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task MatchFileName()
    {
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Root\\Foo/Bar.cs", """
            class Bar
            {
            }
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task DoesMatchFileName()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test0
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DoesMatchFileName_Generic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test0<T>
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DoesMatchFileName_GenericUsingArity()
    {
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Test0`1.cs", """
            class Test0<T>
            {
            }
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task DoesMatchFileName_GenericUsingOfT()
    {
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Test0OfT.cs", """
            class Test0<T>
            {
            }
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task DoesNotMatchFileName_GenericWithArityGreaterThan1UsingOfT_WithoutConfiguration()
    {
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Test0OfT.cs", """
            class {|MA0048:Test0|}<T1, T2>
            {
            }
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task DoesMatchFileName_GenericWithArityGreaterThan1UsingOfT_WithConfiguration()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0048.allow_oft_for_all_generic_types", "true");
        test.TestState.Sources.Add(("/0/Test0OfT.cs", """
            class Test0<T1, T2>
            {
            }
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task DoesMatchFileName_RecordStructWithArityGreaterThan1UsingOfT_WithConfiguration()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0048.allow_oft_for_all_generic_types", "true");
        test.TestState.Sources.Add(("/0/FooOfT.cs", """
            public record struct Foo<T1, T2>(T1 Key, T2 Value);
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task NestedTypeDoesMatchFileName_Ok()
    {
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Test0.cs", """
            class Test0
            {
                class Test1
                {
                }
            }
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task Brackets_MatchType()
    {
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Test0{T}.cs", """
            class Test0<T>
            {
            }
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task Brackets_MatchTypes()
    {
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Test0{TKey,TValue}.cs", """
            class Test0<TKey, TValue>
            {
            }
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task Brackets_DoesNotMatchTypeCount()
    {
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Test0{TKey}.cs", """
            class {|MA0048:Test0|}<TKey, TValue>
            {
            }
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task Brackets_DoesNotMatchTypeName()
    {
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Test0{TKey,TNotSame}.cs", """
            class {|MA0048:Test0|}<TKey, TValue>
            {
            }
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task DoesNotMatchFileNamePrefix_WithoutConfiguration()
    {
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Perk.cs", """
            class {|MA0048:PerkQuery|} {}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task MatchFileNamePrefix_WithModeConfiguration()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0048.mode", "Prefix");
        test.TestState.Sources.Add(("/0/Perk.cs", """
            class PerkQuery {}
            class PerkResponse {}
            class PerkHandler {}
            class {|#0:DummyHandler|} {}
            """));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0048", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("File name must match type name (class DummyHandler), expected file name: a prefix of 'DummyHandler'"));

        return test.RunAsync();
    }

    [Fact]
    public Task MatchFileNamePrefix_WithLegacyConfiguration()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0048.allow_type_name_prefix", "true");
        test.TestState.Sources.Add(("/0/Perk.cs", """
            class PerkQuery {}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task MatchFileNamePrefix_LongestCommonPrefixMode_WithoutLongestCommonPrefixInFileName()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0048.mode", "LongestCommonPrefix");
        test.TestState.Sources.Add(("/0/Sample.cs", """
            class {|#0:SampleProjectHandler|} {}
            class {|MA0048:SampleProjectQuery|} {}
            class {|MA0048:SampleProjectResponse|} {}
            """));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0048", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("File name must match type name (class SampleProjectHandler), expected file name: 'SampleProject'"));

        return test.RunAsync();
    }

    [Fact]
    public Task MatchFileNamePrefix_LongestCommonPrefixMode_WithLongestCommonPrefixInFileName()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0048.mode", "LongestCommonPrefix");
        test.TestState.Sources.Add(("/0/SampleProject.cs", """
            class SampleProjectHandler {}
            class SampleProjectQuery {}
            class SampleProjectResponse {}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task MatchFileNamePrefix_LongestCommonPrefix_WithLegacyConfiguration()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration(("MA0048.allow_type_name_prefix", "true"), ("MA0048.use_longest_type_name_prefix", "true"));
        test.TestState.Sources.Add(("/0/SampleProject.cs", """
            class SampleProjectHandler {}
            class SampleProjectQuery {}
            class SampleProjectResponse {}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task MatchOnlyFirstType_class1()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0048.only_validate_first_type", "true");
        test.TestState.Sources.Add(("/0/Test0.cs", """
            class {|MA0048:Foo|} {}
            class Bar {}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task MatchOnlyFirstType_class2()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0048.only_validate_first_type", "true");
        test.TestState.Sources.Add(("/0/Test0.cs", """
            class Test0 {}
            class Sample {}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task MatchOnlyFirstType_class3()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0048.only_validate_first_type", "true");
        test.TestState.Sources.Add(("/0/Test0.cs", """
            class {|MA0048:Sample|} {}
            class Test0 {}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task MatchOnlyFirstType_Enum()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0048.only_validate_first_type", "true");
        test.TestState.Sources.Add(("/0/Test0.cs", """
            enum {|MA0048:Foo|} {}
            enum Bar {}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task MatchOnlyFirstType_Interface()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0048.only_validate_first_type", "true");
        test.TestState.Sources.Add(("/0/Test0.cs", """
            interface {|MA0048:Foo|} {}
            interface Bar {}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task MatchOnlyFirstType_Record()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0048.only_validate_first_type", "true");
        test.TestState.Sources.Add(("/0/Test0.cs", """
            record {|MA0048:Foo|} {}
            record Bar {}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task MatchOnlyFirstType_RecordStruct()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0048.only_validate_first_type", "true");
        test.TestState.Sources.Add(("/0/Test0.cs", """
            record struct {|MA0048:Foo|} {}
            record struct Bar {}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task MatchOnlyFirstType_Struct()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0048.only_validate_first_type", "true");
        test.TestState.Sources.Add(("/0/Test0.cs", """
            struct {|MA0048:Foo|} {}
            struct Bar {}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task MatchOnlyFirstType_TypeWithBlockScopedNamespaceDeclaration()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0048.only_validate_first_type", "true");
        test.TestState.Sources.Add(("/0/Test0.cs", """
            namespace Sample
            {
                struct {|MA0048:Foo|} {}
                struct Bar {}
            }
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task MatchOnlyFirstType_TypeWithFileScopedNamespaceDeclaration()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0048.only_validate_first_type", "true");
        test.TestState.Sources.Add(("/0/Test0.cs", """
            namespace Sample;
            struct {|MA0048:Foo|} {}
            struct Bar {}
            """));

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Sample")]
    [InlineData("T:MyNamespace.Sample")]
    public Task MatchExcludedSymbolNames_ExactMatch(string value)
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("dotnet_diagnostic.MA0048.excluded_symbol_names", value);
        test.TestState.Sources.Add(("/0/Test0.cs", """
            namespace MyNamespace {
              class Test0 {}
              class Sample {}
            }
            """));

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Sample1|Sample2")]
    [InlineData("T:MyNamespace.Sample1|T:MyNamespace.Sample2")]
    [InlineData("Sample1|T:MyNamespace.Sample2")]
    public Task MatchExcludedSymbolNames_ExactMatch_Pipe(string value)
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("dotnet_diagnostic.MA0048.excluded_symbol_names", value);
        test.TestState.Sources.Add(("/0/Test0.cs", """
            namespace MyNamespace {
              class Test0 {}
              class Sample1 {}
              class Sample2 {}
             }
            """));

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Sample*")]
    [InlineData("*ample*")]
    public Task MatchExcludedSymbolNames_WildcardMatch(string value)
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("dotnet_diagnostic.MA0048.excluded_symbol_names", value);
        test.TestState.Sources.Add(("/0/Test0.cs", """
            namespace MyNamespace {
             class Test0 {}
             class Sample1 {}
             class Sample2 {}
            }
            """));

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Sample*|*1|*2")]
    [InlineData("*ample*|*oo*")]
    [InlineData("T:MyNamespace.Sample*|T:MyNamespace.Foo*")]
    [InlineData("T:MyNamespace.Sample*|Foo*")]
    public Task MatchExcludedSymbolNames_WildcardMatch_Pipe(string value)
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("dotnet_diagnostic.MA0048.excluded_symbol_names", value);
        test.TestState.Sources.Add(("/0/Test0.cs", """
            namespace MyNamespace {
             class Test0 {}
             class Sample1 {}
             class Sample2 {}
             class Foo1 {}
             class Foo2 {}
            }
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task FileLocalTypes()
    {
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Dummy.cs", """
            class Dummy
            {
            }

            file class Sample
            {
            }
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task FileLocalTypes_Configuration()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0048.exclude_file_local_types", "false");
        test.TestState.Sources.Add(("/0/Dummy.cs", """
            class Dummy
            {
            }

            file class {|#0:Sample|}
            {
            }
            """));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0048", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("File name must match type name (class Sample), expected file name: 'Sample'"));

        return test.RunAsync();
    }

    [Fact]
    public Task TypeKindIncludedInMessage_Class()
    {
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Test.cs", """
            class {|#0:Sample|}
            {
            }
            """));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0048", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("File name must match type name (class Sample), expected file name: 'Sample'"));

        return test.RunAsync();
    }

    [Fact]
    public Task TypeKindIncludedInMessage_Struct()
    {
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Test.cs", """
            struct {|#0:Sample|}
            {
            }
            """));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0048", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("File name must match type name (struct Sample), expected file name: 'Sample'"));

        return test.RunAsync();
    }

    [Fact]
    public Task TypeKindIncludedInMessage_Interface()
    {
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Test.cs", """
            interface {|#0:ISample|}
            {
            }
            """));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0048", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("File name must match type name (interface ISample), expected file name: 'ISample'"));

        return test.RunAsync();
    }

    [Fact]
    public Task TypeKindIncludedInMessage_Enum()
    {
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Test.cs", """
            enum {|#0:Sample|}
            {
                Value1
            }
            """));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0048", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("File name must match type name (enum Sample), expected file name: 'Sample'"));

        return test.RunAsync();
    }

    [Fact]
    public Task TypeKindIncludedInMessage_Record()
    {
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Test.cs", """
            record {|#0:Sample|};
            """));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0048", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("File name must match type name (record Sample), expected file name: 'Sample'"));

        return test.RunAsync();
    }

    [Fact]
    public Task TypeKindIncludedInMessage_RecordStruct()
    {
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Test.cs", """
            record struct {|#0:Sample|};
            """));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0048", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("File name must match type name (record struct Sample), expected file name: 'Sample'"));

        return test.RunAsync();
    }

    [Fact]
    public Task TypeKindIncludedInMessage_Delegate()
    {
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Test.cs", """
            delegate void {|#0:Sample|}();
            """));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0048", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("File name must match type name (delegate Sample), expected file name: 'Sample'"));

        return test.RunAsync();
    }
}
