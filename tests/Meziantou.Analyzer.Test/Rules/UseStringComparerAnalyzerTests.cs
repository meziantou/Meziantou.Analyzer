using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseStringComparerAnalyzer,
    Meziantou.Analyzer.Rules.UseStringComparerFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseStringComparerAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.LanguageVersion = LanguageVersion.CSharp9;
        return test;
    }

    private static CodeFixTest CreatePreviewTest()
    {
        var test = new CodeFixTest();
        test.LanguageVersion = LanguageVersion.Preview;
        return test;
    }

    private static CodeFixTest CreateMSTestTest()
    {
        var test = new CodeFixTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([new PackageIdentity("MSTest.TestFramework", "4.3.3")]);
        return test;
    }

#if ROSLYN_5_6_OR_GREATER
    private const string ReportCollectionExpressionsConfigurationName = "MA0002.report_collection_expressions";
#endif
    private const string ReportOnlyNonOrdinalConfigurationName = "MA0002.report_only_non_ordinal";

    // MSTest 4 declares the comparer before the message and after the interpolated string handler,
    // so the code fix must use the real overloads to compute where the comparer must be inserted.

    [Fact]
    public Task MethodOnSetStoredInPrivateReadonlyFieldInOtherSyntaxTree_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            partial class TypeName
            {
                public void Test()
                {
                    _ = Values.Contains("a");
                }
            }
            """;
        test.TestState.Sources.Add("""
            interface IContainer
            {
                bool Contains(string value);
            }

            sealed class CustomSet : System.Collections.Generic.HashSet<string>, IContainer
            {
            }

            partial class TypeName
            {
                private readonly IContainer Values = new CustomSet();
            }
            """);

        return test.RunAsync();
    }

    [Fact]
    public Task HashSet_Int32_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    new System.Collections.Generic.HashSet<int>();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SortedList_string_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|MA0002:new System.Collections.Generic.SortedList<string, int>()|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    new System.Collections.Generic.SortedList<string, int>(System.StringComparer.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HashSet_String_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|MA0002:new System.Collections.Generic.HashSet<string>()|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SortedDictionary_String_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|MA0002:new System.Collections.Generic.SortedDictionary<string, int>()|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    new System.Collections.Generic.SortedDictionary<string, int>(System.StringComparer.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HashSet_String__ShortNew_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.HashSet<string> a = {|MA0002:new()|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.HashSet<string> a = new(System.StringComparer.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HashSet_String_StringEqualityComparer_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Dictionary_String_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|MA0002:new System.Collections.Generic.Dictionary<string, int>()|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Dictionary_String_WithoutArgumentListAndWithInitializer_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|MA0002:new System.Collections.Generic.Dictionary<string, object?>
                    {
                        ["c"] = true,
                    }|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    new System.Collections.Generic.Dictionary<string, object?>(System.StringComparer.Ordinal)
                    {
                        ["c"] = true,
                    };
                }
            }
            """;

        return test.RunAsync();
    }

#if  ROSLYN_5_6_OR_GREATER

    [Fact]
    public Task Dictionary_String_CollectionExpression_DefaultOnCSharp12_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.Dictionary<string, int> a = [];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HashSet_String_CollectionExpression_DefaultOnCSharp12_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.HashSet<string> a = [];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Dictionary_String_CollectionExpression_CSharp12_OptionEnabled_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestState.SetConfiguration(ReportCollectionExpressionsConfigurationName, "true");
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.Dictionary<string, int> a = {|MA0002:[]|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HashSet_String_CollectionExpression_CSharp12_OptionEnabled_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestState.SetConfiguration(ReportCollectionExpressionsConfigurationName, "true");
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.HashSet<string> a = {|MA0002:[]|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HashSet_String_CollectionExpression_ReportOnlyNonOrdinal_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestState.SetConfiguration((ReportCollectionExpressionsConfigurationName, "true"), (ReportOnlyNonOrdinalConfigurationName, "true"));
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.HashSet<string> a = [];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HashSet_String_CollectionExpression_WithElements_CSharp12_OptionEnabled_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestState.SetConfiguration(ReportCollectionExpressionsConfigurationName, "true");
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.HashSet<string> a = {|MA0002:["a", "b"]|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HashSet_String_CollectionExpression_WithElements_ReportOnlyNonOrdinal_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestState.SetConfiguration((ReportCollectionExpressionsConfigurationName, "true"), (ReportOnlyNonOrdinalConfigurationName, "true"));
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.HashSet<string> a = ["a", "b"];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FrozenSet_String_CollectionExpression_DefaultOnCSharp12_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Frozen.FrozenSet<string> a = [];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FrozenSet_String_CollectionExpression_CSharp12_OptionEnabled_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestState.SetConfiguration(ReportCollectionExpressionsConfigurationName, "true");
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Frozen.FrozenSet<string> a = {|MA0002:[]|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImmutableHashSet_String_CollectionExpression_DefaultOnCSharp12_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Immutable.ImmutableHashSet<string> a = [];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImmutableHashSet_String_CollectionExpression_CSharp12_OptionEnabled_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestState.SetConfiguration(ReportCollectionExpressionsConfigurationName, "true");
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Immutable.ImmutableHashSet<string> a = {|MA0002:[]|};
                }
            }
            """;

        return test.RunAsync();
    }

#endif

#if CSHARP15_OR_GREATER

    [Fact]
    public Task Dictionary_String_CollectionExpression_Preview_ShouldReportDiagnostic()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.Dictionary<string, int> a = {|MA0002:[]|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.Dictionary<string, int> a = [with(global::System.StringComparer.Ordinal)];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HashSet_String_CollectionExpression_Preview_ShouldReportDiagnostic()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.HashSet<string> a = {|MA0002:[]|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.HashSet<string> a = [with(global::System.StringComparer.Ordinal)];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Dictionary_String_CollectionExpression_WithStringComparer_ShouldNotReportDiagnostic()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.Dictionary<string, int> a = [with(System.StringComparer.Ordinal)];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HashSet_String_CollectionExpression_WithStringComparer_ShouldNotReportDiagnostic()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.HashSet<string> a = [with(System.StringComparer.Ordinal)];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Dictionary_String_CollectionExpression_WithCapacityNoComparer_ShouldReportDiagnostic()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.Dictionary<string, int> a = {|MA0002:[with(10)]|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HashSet_String_CollectionExpression_WithElements_ShouldReportDiagnostic()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.HashSet<string> a = {|MA0002:["a", "b"]|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.HashSet<string> a = [with(global::System.StringComparer.Ordinal), "a", "b"];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HashSet_String_CollectionExpression_WithSpread_ShouldReportDiagnostic()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var other = new string[] { "a", "b" };
                    System.Collections.Generic.HashSet<string> a = {|MA0002:[.. other]|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    var other = new string[] { "a", "b" };
                    System.Collections.Generic.HashSet<string> a = [with(global::System.StringComparer.Ordinal), .. other];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HashSet_String_CollectionExpression_WithComparerAndElements_ShouldNotReportDiagnostic()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.HashSet<string> a = [with(System.StringComparer.Ordinal), "a", "b"];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HashSet_String_CollectionExpression_WithElements_Spread_ShouldReportDiagnostic()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var other = new string[] { "a", "b" };
                    System.Collections.Generic.HashSet<string> a = {|MA0002:[.. other, "c"]|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    var other = new string[] { "a", "b" };
                    System.Collections.Generic.HashSet<string> a = [with(global::System.StringComparer.Ordinal), .. other, "c"];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FrozenSet_String_CollectionExpression_Preview_ShouldReportDiagnostic()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Frozen.FrozenSet<string> a = {|MA0002:[]|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Frozen.FrozenSet<string> a = [with(global::System.StringComparer.Ordinal)];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FrozenSet_String_CollectionExpression_WithStringComparer_ShouldNotReportDiagnostic()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Frozen.FrozenSet<string> a = [with(System.StringComparer.Ordinal)];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImmutableHashSet_String_CollectionExpression_Preview_ShouldReportDiagnostic()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Immutable.ImmutableHashSet<string> a = {|MA0002:[]|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Immutable.ImmutableHashSet<string> a = [with(global::System.StringComparer.Ordinal)];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImmutableHashSet_String_CollectionExpression_WithStringComparer_ShouldNotReportDiagnostic()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Immutable.ImmutableHashSet<string> a = [with(System.StringComparer.Ordinal)];
                }
            }
            """;

        return test.RunAsync();
    }

#endif

    [Fact]
    public Task ConcurrentDictionary_String_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|MA0002:new System.Collections.Concurrent.ConcurrentDictionary<string, int>()|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    new System.Collections.Concurrent.ConcurrentDictionary<string, int>(System.StringComparer.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImmutableDictionary_CreateBuilder_String_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net48.Wpf.AddPackages([new PackageIdentity("System.Collections.Immutable", "8.0.0")]);
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Immutable.ImmutableDictionary.{|MA0002:CreateBuilder<string, string>()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImmutableDictionary_CreateBuilder_String_WithComparer_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net48.Wpf.AddPackages([new PackageIdentity("System.Collections.Immutable", "8.0.0")]);
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Immutable.ImmutableDictionary.CreateBuilder<string, string>(System.StringComparer.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImmutableDictionary_Create_String_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net48.Wpf.AddPackages([new PackageIdentity("System.Collections.Immutable", "8.0.0")]);
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Immutable.ImmutableDictionary.{|MA0002:Create<string, string>()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MeziantouFrameworkAssertions_Assert_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            namespace Meziantou.Framework.Assertions
            {
                static class Assert
                {
                    public static void AreEqual(string expected, string actual) { }
                    public static void AreEqual(string expected, string actual, System.Collections.Generic.IEqualityComparer<string> comparer) { }
                }
            }

            class TypeName
            {
                public void Test()
                {
                    Meziantou.Framework.Assertions.Assert.{|MA0002:AreEqual("a", "b")|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MeziantouFrameworkAssertions_Assert_ReportOnlyNonOrdinal_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration(ReportOnlyNonOrdinalConfigurationName, "true");
        test.TestCode = """
            namespace Meziantou.Framework.Assertions
            {
                static class Assert
                {
                    public static void AreEqual(string expected, string actual) { }
                    public static void AreEqual(string expected, string actual, System.Collections.Generic.IEqualityComparer<string> comparer) { }
                }
            }

            class TypeName
            {
                public void Test()
                {
                    Meziantou.Framework.Assertions.Assert.AreEqual("a", "b");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MeziantouFrameworkAssertions_Assert_WithoutComparerOverload_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([new PackageIdentity("Meziantou.Framework.Assertions", "2.0.1")]);
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.ICollection<string> values = new[] { "abc" };
                    Meziantou.Framework.Assertions.Assert.Contains("abc", values);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportOnlyNonOrdinal_HashSet_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration(ReportOnlyNonOrdinalConfigurationName, "true");
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    new System.Collections.Generic.HashSet<string>();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportOnlyNonOrdinal_Dictionary_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration(ReportOnlyNonOrdinalConfigurationName, "true");
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    new System.Collections.Generic.Dictionary<string, int>();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportOnlyNonOrdinal_ConcurrentDictionary_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration(ReportOnlyNonOrdinalConfigurationName, "true");
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportOnlyNonOrdinal_EnumerableDistinct_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration(ReportOnlyNonOrdinalConfigurationName, "true");
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.Distinct();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportOnlyNonOrdinal_EnumerableToDictionary_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration(ReportOnlyNonOrdinalConfigurationName, "true");
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.ToDictionary(p => p);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportOnlyNonOrdinal_ImmutableDictionaryCreateBuilder_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net48.Wpf.AddPackages([new PackageIdentity("System.Collections.Immutable", "8.0.0")]);
        test.TestState.SetConfiguration(ReportOnlyNonOrdinalConfigurationName, "true");
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Immutable.ImmutableDictionary.CreateBuilder<string, string>();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportOnlyNonOrdinal_ImmutableDictionaryCreate_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net48.Wpf.AddPackages([new PackageIdentity("System.Collections.Immutable", "8.0.0")]);
        test.TestState.SetConfiguration(ReportOnlyNonOrdinalConfigurationName, "true");
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Immutable.ImmutableDictionary.Create<string, string>();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportOnlyNonOrdinal_ImmutableSortedDictionaryCreate_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net48.Wpf.AddPackages([new PackageIdentity("System.Collections.Immutable", "8.0.0")]);
        test.TestState.SetConfiguration(ReportOnlyNonOrdinalConfigurationName, "true");
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Immutable.ImmutableSortedDictionary.{|MA0002:Create<string, string>()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportOnlyNonOrdinal_SortedDictionary_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration(ReportOnlyNonOrdinalConfigurationName, "true");
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|MA0002:new System.Collections.Generic.SortedDictionary<string, int>()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportOnlyNonOrdinal_OrderBy_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration(ReportOnlyNonOrdinalConfigurationName, "true");
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.{|MA0002:OrderBy(p => p)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportOnlyNonOrdinal_Order_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration(ReportOnlyNonOrdinalConfigurationName, "true");
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.{|MA0002:Order()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EnumerableContains_String_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.{|MA0002:Contains("")|};
                }
            }
            """;
        test.FixedCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.Contains("", System.StringComparer.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EnumerableDistinct_String_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.{|MA0002:Distinct()|};
                }
            }
            """;
        test.FixedCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.Distinct(System.StringComparer.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task QueryableDistinct_String_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    IQueryable<string?> obj = null;
                    obj.Distinct();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task QueryableContains_String_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    IQueryable<string?> obj = null;
                    obj.Contains("");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task QueryableOrderBy_String_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    IQueryable<string?> obj = null;
                    obj.OrderBy(p => p);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EnumerableToDictionary_String_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.{|MA0002:ToDictionary(p => p)|};
                }
            }
            """;
        test.FixedCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.ToDictionary(p => p, System.StringComparer.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Order_String_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.{|MA0002:Order()|};
                }
            }
            """;
        test.FixedCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.Order(System.StringComparer.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OrderBy_String_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.{|MA0002:OrderBy(p => p)|};
                }
            }
            """;
        test.FixedCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.OrderBy(p => p, System.StringComparer.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OrderByDescending_String_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.{|MA0002:OrderByDescending(p => p)|};
                }
            }
            """;
        test.FixedCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.OrderByDescending(p => p, System.StringComparer.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThenBy_String_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.OrderBy(p => p, System.StringComparer.Ordinal).{|MA0002:ThenBy(p => p)|};
                }
            }
            """;
        test.FixedCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.OrderBy(p => p, System.StringComparer.Ordinal).ThenBy(p => p, System.StringComparer.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThenByDescending_String_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.OrderBy(p => p, System.StringComparer.Ordinal).{|MA0002:ThenByDescending(p => p)|};
                }
            }
            """;
        test.FixedCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.OrderBy(p => p, System.StringComparer.Ordinal).ThenByDescending(p => p, System.StringComparer.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FindExtensionMethods()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                }
            }

            static class Extensions
            {
                public static void Test(this TypeName type, System.Collections.Generic.IEqualityComparer<string> comparer)
                {
                }
            }

            class Usage
            {
                void A()
                {
                    var a = new TypeName();
                    a.{|MA0002:Test()|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                }
            }

            static class Extensions
            {
                public static void Test(this TypeName type, System.Collections.Generic.IEqualityComparer<string> comparer)
                {
                }
            }

            class Usage
            {
                void A()
                {
                    var a = new TypeName();
                    a.Test(System.StringComparer.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HashSet_Contain()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.HashSet<string> obj = null;
                    obj.Contains("");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ISet_Contain()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.ISet<string> obj = null;
                    obj.Contains("");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IReadOnlySet_Contain()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IReadOnlySet<string> obj = null;
                    obj.Contains("");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IImmutableSet_Contain()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Immutable.IImmutableSet<string> obj = null;
                    obj.Contains("");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringArray_QuerySyntax_GroupBy_NoConfiguration()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    var collection = new string[0];
                    _ = from item in collection
                        {|MA0002:group item by item|} into g
                        select g;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringArray_QuerySyntax_GroupBy()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0002.exclude_query_operator_syntaxes", "true");
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    var collection = new string[0];
                    _ = from item in collection
                        group item by item into g
                        select g;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringArray_QuerySyntax_OrderBy_NoConfiguration()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    var collection = new string[0];
                    _ = from item in collection
                        orderby {|MA0002:item|}
                        select item;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringArray_QuerySyntax_OrderBy()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0002.exclude_query_operator_syntaxes", "true");
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    var collection = new string[0];
                    _ = from item in collection
                        orderby item
                        select item;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringArray_QuerySyntax_OrderByDescending_NoConfiguration()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    var collection = new string[0];
                    _ = from item in collection
                        orderby {|MA0002:item descending|}
                        select item;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringArray_QuerySyntax_OrderByDescending()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0002.exclude_query_operator_syntaxes", "true");
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    var collection = new string[0];
                    _ = from item in collection
                        orderby item descending
                        select item;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringArray_QuerySyntax_Join_NoConfiguration()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    var collection = new string[0];
                    _ = from item1 in collection
                        {|MA0002:join item2 in collection on item1 equals item2|}
                        select (item1, item2);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringArray_QuerySyntax_Join()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0002.exclude_query_operator_syntaxes", "true");
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    var collection = new string[0];
                    _ = from item1 in collection
                        join item2 in collection on item1 equals item2
                        select (item1, item2);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringArray_QuerySyntax_JoinInto_NoConfiguration()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    var collection = new string[0];
                    _ = from item1 in collection
                        {|MA0002:join item2 in collection on item1 equals item2 into joinGroup|}
                        select (item1, joinGroup);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringArray_QuerySyntax_JoinInto()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0002.exclude_query_operator_syntaxes", "true");
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    var collection = new string[0];
                    _ = from item1 in collection
                        join item2 in collection on item1 equals item2 into joinGroup
                        select (item1, joinGroup);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludeWhenInAnExpressionContext()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Linq;
            using System.Linq.Expressions;
            class TypeName
            {
                void WithSomething()
                {
                    var op = new string[0];
                    _ = (Expression<Func<Something, bool>>)(s => op.ToList().Contains(s.SomeField));
                }

                public class Something
                {
                    public string SomeField { get; set; }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_InsertComparerBeforeMessage_Issue1249()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            class Sample
            {
                void Test()
                {
                    AreEqual{|MA0002:("a", "b", "message")|};
                }

                static void AreEqual(string expected, string actual, string message) { }
                static void AreEqual(string expected, string actual, IComparer<string> comparer, string message) { }
            }
            """;
        test.FixedCode = """
            using System.Collections.Generic;
            class Sample
            {
                void Test()
                {
                    AreEqual("a", "b", System.StringComparer.Ordinal, "message");
                }

                static void AreEqual(string expected, string actual, string message) { }
                static void AreEqual(string expected, string actual, IComparer<string> comparer, string message) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_InsertComparerBeforeCancellationToken_Issue1250()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Threading;
            class Sample
            {
                void Test(CancellationToken ct)
                {
                    var list = new string[0];
                    list.{|MA0002:ToDictionaryCustom(s => s, ct)|};
                }
            }
            static class Extensions
            {
                public static Dictionary<TKey, T> ToDictionaryCustom<T, TKey>(this IEnumerable<T> source, System.Func<T, TKey> keySelector, CancellationToken ct) => throw null;
                public static Dictionary<TKey, T> ToDictionaryCustom<T, TKey>(this IEnumerable<T> source, System.Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer, CancellationToken ct) => throw null;
            }
            """;
        test.FixedCode = """
            using System.Collections.Generic;
            using System.Threading;
            class Sample
            {
                void Test(CancellationToken ct)
                {
                    var list = new string[0];
                    list.ToDictionaryCustom(s => s, System.StringComparer.Ordinal, ct);
                }
            }
            static class Extensions
            {
                public static Dictionary<TKey, T> ToDictionaryCustom<T, TKey>(this IEnumerable<T> source, System.Func<T, TKey> keySelector, CancellationToken ct) => throw null;
                public static Dictionary<TKey, T> ToDictionaryCustom<T, TKey>(this IEnumerable<T> source, System.Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer, CancellationToken ct) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_MSTestAreEqualWithMessage_Issue1249()
    {
        var test = CreateMSTestTest();
        test.TestCode = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;
            class Sample
            {
                void Test(string[] fields)
                {
                    Assert.{|MA0002:AreEqual("id", fields[0], "First field should be the ID")|};
                }
            }
            """;
        test.FixedCode = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;
            class Sample
            {
                void Test(string[] fields)
                {
                    Assert.AreEqual("id", fields[0], System.StringComparer.Ordinal, "First field should be the ID");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_MSTestAreEqualWithInterpolatedMessage_Issue1249()
    {
        var test = CreateMSTestTest();
        test.TestCode = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;
            class Sample
            {
                void Test(string[] fields)
                {
                    Assert.{|MA0002:AreEqual("id", fields[0], $"First field should be the ID but was {fields[0]}")|};
                }
            }
            """;
        test.FixedCode = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;
            class Sample
            {
                void Test(string[] fields)
                {
                    Assert.AreEqual("id", fields[0], System.StringComparer.Ordinal, $"First field should be the ID but was {fields[0]}");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_UseNamedArgumentWhenArgumentsAreNamed()
    {
        var test = CreateMSTestTest();
        test.TestCode = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;
            class Sample
            {
                void Test(string[] fields)
                {
                    Assert.{|MA0002:AreEqual(actual: fields[0], expected: "id")|};
                }
            }
            """;
        test.FixedCode = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;
            class Sample
            {
                void Test(string[] fields)
                {
                    Assert.AreEqual(actual: fields[0], expected: "id", comparer: System.StringComparer.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }
}
