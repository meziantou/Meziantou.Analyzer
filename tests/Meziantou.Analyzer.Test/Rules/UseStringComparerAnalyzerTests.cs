using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseStringComparerAnalyzerTests
{
#if ROSLYN_5_6_OR_GREATER
    private const string ReportCollectionExpressionsConfigurationName = "MA0002.report_collection_expressions";
#endif
    private const string ReportOnlyNonOrdinalConfigurationName = "MA0002.report_only_non_ordinal";

    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp9)
            .WithAnalyzer<UseStringComparerAnalyzer>()
            .WithCodeFixProvider<UseStringComparerFixer>();
    }

#if CSHARP15_OR_GREATER
    private static ProjectBuilder CreatePreviewProjectBuilder()
    {
        return new ProjectBuilder()
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview)
            .WithAnalyzer<UseStringComparerAnalyzer>()
            .WithCodeFixProvider<UseStringComparerFixer>();
    }
#endif

    [Fact]
    public async Task MethodOnSetStoredInPrivateReadonlyFieldInOtherSyntaxTree_ShouldNotReportDiagnostic()
    {
        var builder = CreateProjectBuilder()
              .WithSourceCode("""
                partial class TypeName
                {
                    public void Test()
                    {
                        _ = Values.Contains("a");
                    }
                }
                """);
        builder.ApiReferences.Add("""
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

        await builder.ValidateAsync();
    }

    [Fact]
    public async Task HashSet_Int32_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    new System.Collections.Generic.HashSet<int>();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SortedList_string_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    [|new System.Collections.Generic.SortedList<string, int>()|];
                }
            }
            """;

        const string CodeFix = """
            class TypeName
            {
                public void Test()
                {
                    new System.Collections.Generic.SortedList<string, int>(System.StringComparer.Ordinal);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task HashSet_String_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    [|new System.Collections.Generic.HashSet<string>()|];
                }
            }
            """;
        const string CodeFix = """
            class TypeName
            {
                public void Test()
                {
                    new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task SortedDictionary_String_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    [|new System.Collections.Generic.SortedDictionary<string, int>()|];
                }
            }
            """;
        const string CodeFix = """
            class TypeName
            {
                public void Test()
                {
                    new System.Collections.Generic.SortedDictionary<string, int>(System.StringComparer.Ordinal);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task HashSet_String__ShortNew_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.HashSet<string> a = [|new()|];
                }
            }
            """;
        const string CodeFix = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.HashSet<string> a = new(System.StringComparer.Ordinal);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task HashSet_String_StringEqualityComparer_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Dictionary_String_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    [|new System.Collections.Generic.Dictionary<string, int>()|];
                }
            }
            """;
        const string CodeFix = """
            class TypeName
            {
                public void Test()
                {
                    new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task Dictionary_String_WithoutArgumentListAndWithInitializer_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    [|new System.Collections.Generic.Dictionary<string, object?>
                    {
                        ["c"] = true,
                    }|];
                }
            }
            """;
        const string CodeFix = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

#if  ROSLYN_5_6_OR_GREATER
    [Fact]
    public async Task Dictionary_String_CollectionExpression_DefaultOnCSharp12_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Generic.Dictionary<string, int> a = [];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task HashSet_String_CollectionExpression_DefaultOnCSharp12_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Generic.HashSet<string> a = [];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Dictionary_String_CollectionExpression_CSharp12_OptionEnabled_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
              .AddAnalyzerConfiguration(ReportCollectionExpressionsConfigurationName, "true")
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Generic.Dictionary<string, int> a = [|[]|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task HashSet_String_CollectionExpression_CSharp12_OptionEnabled_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
              .AddAnalyzerConfiguration(ReportCollectionExpressionsConfigurationName, "true")
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Generic.HashSet<string> a = [|[]|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task HashSet_String_CollectionExpression_ReportOnlyNonOrdinal_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
              .AddAnalyzerConfiguration(ReportCollectionExpressionsConfigurationName, "true")
              .AddAnalyzerConfiguration(ReportOnlyNonOrdinalConfigurationName, "true")
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Generic.HashSet<string> a = [];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task HashSet_String_CollectionExpression_WithElements_CSharp12_OptionEnabled_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
              .AddAnalyzerConfiguration(ReportCollectionExpressionsConfigurationName, "true")
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Generic.HashSet<string> a = [|["a", "b"]|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task HashSet_String_CollectionExpression_WithElements_ReportOnlyNonOrdinal_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
              .AddAnalyzerConfiguration(ReportCollectionExpressionsConfigurationName, "true")
              .AddAnalyzerConfiguration(ReportOnlyNonOrdinalConfigurationName, "true")
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Generic.HashSet<string> a = ["a", "b"];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task FrozenSet_String_CollectionExpression_DefaultOnCSharp12_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Frozen.FrozenSet<string> a = [];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task FrozenSet_String_CollectionExpression_CSharp12_OptionEnabled_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
              .AddAnalyzerConfiguration(ReportCollectionExpressionsConfigurationName, "true")
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Frozen.FrozenSet<string> a = [|[]|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ImmutableHashSet_String_CollectionExpression_DefaultOnCSharp12_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Immutable.ImmutableHashSet<string> a = [];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ImmutableHashSet_String_CollectionExpression_CSharp12_OptionEnabled_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
              .AddAnalyzerConfiguration(ReportCollectionExpressionsConfigurationName, "true")
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Immutable.ImmutableHashSet<string> a = [|[]|];
                      }
                  }
                  """)
              .ValidateAsync();
    }
#endif

#if CSHARP15_OR_GREATER
    [Fact]
    public async Task Dictionary_String_CollectionExpression_Preview_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.Dictionary<string, int> a = [|[]|];
                }
            }
            """;
        const string CodeFix = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.Dictionary<string, int> a = [with(global::System.StringComparer.Ordinal)];
                }
            }
            """;
        await CreatePreviewProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task HashSet_String_CollectionExpression_Preview_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.HashSet<string> a = [|[]|];
                }
            }
            """;
        const string CodeFix = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.HashSet<string> a = [with(global::System.StringComparer.Ordinal)];
                }
            }
            """;
        await CreatePreviewProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task Dictionary_String_CollectionExpression_WithStringComparer_ShouldNotReportDiagnostic()
    {
        await CreatePreviewProjectBuilder()
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Generic.Dictionary<string, int> a = [with(System.StringComparer.Ordinal)];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task HashSet_String_CollectionExpression_WithStringComparer_ShouldNotReportDiagnostic()
    {
        await CreatePreviewProjectBuilder()
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Generic.HashSet<string> a = [with(System.StringComparer.Ordinal)];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Dictionary_String_CollectionExpression_WithCapacityNoComparer_ShouldReportDiagnostic()
    {
        await CreatePreviewProjectBuilder()
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Generic.Dictionary<string, int> a = [|[with(10)]|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task HashSet_String_CollectionExpression_WithElements_ShouldReportDiagnostic()
    {
        await CreatePreviewProjectBuilder()
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Generic.HashSet<string> a = [|["a", "b"]|];
                      }
                  }
                  """)
              .ShouldFixCodeWith("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Generic.HashSet<string> a = [with(global::System.StringComparer.Ordinal), "a", "b"];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task HashSet_String_CollectionExpression_WithSpread_ShouldReportDiagnostic()
    {
        await CreatePreviewProjectBuilder()
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          var other = new string[] { "a", "b" };
                          System.Collections.Generic.HashSet<string> a = [|[.. other]|];
                      }
                  }
                  """)
              .ShouldFixCodeWith("""
                  class TypeName
                  {
                      public void Test()
                      {
                          var other = new string[] { "a", "b" };
                          System.Collections.Generic.HashSet<string> a = [with(global::System.StringComparer.Ordinal), .. other];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task HashSet_String_CollectionExpression_WithComparerAndElements_ShouldNotReportDiagnostic()
    {
        await CreatePreviewProjectBuilder()
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Generic.HashSet<string> a = [with(System.StringComparer.Ordinal), "a", "b"];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task HashSet_String_CollectionExpression_WithElements_Spread_ShouldReportDiagnostic()
    {
        await CreatePreviewProjectBuilder()
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          var other = new string[] { "a", "b" };
                          System.Collections.Generic.HashSet<string> a = [|[.. other, "c"]|];
                      }
                  }
                  """)
              .ShouldFixCodeWith("""
                  class TypeName
                  {
                      public void Test()
                      {
                          var other = new string[] { "a", "b" };
                          System.Collections.Generic.HashSet<string> a = [with(global::System.StringComparer.Ordinal), .. other, "c"];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task FrozenSet_String_CollectionExpression_Preview_ShouldReportDiagnostic()
    {
        await CreatePreviewProjectBuilder()
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Frozen.FrozenSet<string> a = [|[]|];
                      }
                  }
                  """)
              .ShouldFixCodeWith("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Frozen.FrozenSet<string> a = [with(global::System.StringComparer.Ordinal)];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task FrozenSet_String_CollectionExpression_WithStringComparer_ShouldNotReportDiagnostic()
    {
        await CreatePreviewProjectBuilder()
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Frozen.FrozenSet<string> a = [with(System.StringComparer.Ordinal)];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ImmutableHashSet_String_CollectionExpression_Preview_ShouldReportDiagnostic()
    {
        await CreatePreviewProjectBuilder()
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Immutable.ImmutableHashSet<string> a = [|[]|];
                      }
                  }
                  """)
              .ShouldFixCodeWith("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Immutable.ImmutableHashSet<string> a = [with(global::System.StringComparer.Ordinal)];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ImmutableHashSet_String_CollectionExpression_WithStringComparer_ShouldNotReportDiagnostic()
    {
        await CreatePreviewProjectBuilder()
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Immutable.ImmutableHashSet<string> a = [with(System.StringComparer.Ordinal)];
                      }
                  }
                  """)
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task ConcurrentDictionary_String_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    [|new System.Collections.Concurrent.ConcurrentDictionary<string, int>()|];
                }
            }
            """;
        const string CodeFix = """
            class TypeName
            {
                public void Test()
                {
                    new System.Collections.Concurrent.ConcurrentDictionary<string, int>(System.StringComparer.Ordinal);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task ImmutableDictionary_CreateBuilder_String_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Immutable.ImmutableDictionary.[|CreateBuilder<string, string>()|];
                }
            }
            """;

        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net4_8)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ImmutableDictionary_CreateBuilder_String_WithComparer_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Immutable.ImmutableDictionary.CreateBuilder<string, string>(System.StringComparer.Ordinal);
                }
            }
            """;

        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net4_8)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ImmutableDictionary_Create_String_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Immutable.ImmutableDictionary.[|Create<string, string>()|];
                }
            }
            """;

        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net4_8)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MeziantouFrameworkAssertions_Assert_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
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
                          Meziantou.Framework.Assertions.Assert.[|AreEqual("a", "b")|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task MeziantouFrameworkAssertions_Assert_ReportOnlyNonOrdinal_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .AddAnalyzerConfiguration(ReportOnlyNonOrdinalConfigurationName, "true")
              .WithSourceCode("""
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
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task MeziantouFrameworkAssertions_Assert_WithoutComparerOverload_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net10_0)
              .AddNuGetReference("Meziantou.Framework.Assertions", "2.0.1", "lib/net10.0/")
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Generic.ICollection<string> values = new[] { "abc" };
                          Meziantou.Framework.Assertions.Assert.Contains("abc", values);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportOnlyNonOrdinal_HashSet_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .AddAnalyzerConfiguration(ReportOnlyNonOrdinalConfigurationName, "true")
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          new System.Collections.Generic.HashSet<string>();
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportOnlyNonOrdinal_Dictionary_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .AddAnalyzerConfiguration(ReportOnlyNonOrdinalConfigurationName, "true")
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          new System.Collections.Generic.Dictionary<string, int>();
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportOnlyNonOrdinal_ConcurrentDictionary_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .AddAnalyzerConfiguration(ReportOnlyNonOrdinalConfigurationName, "true")
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportOnlyNonOrdinal_EnumerableDistinct_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .AddAnalyzerConfiguration(ReportOnlyNonOrdinalConfigurationName, "true")
              .WithSourceCode("""
                  using System.Linq;
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Generic.IEnumerable<string> obj = null;
                          obj.Distinct();
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportOnlyNonOrdinal_EnumerableToDictionary_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .AddAnalyzerConfiguration(ReportOnlyNonOrdinalConfigurationName, "true")
              .WithSourceCode("""
                  using System.Linq;
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Generic.IEnumerable<string> obj = null;
                          obj.ToDictionary(p => p);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportOnlyNonOrdinal_ImmutableDictionaryCreateBuilder_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net4_8)
              .AddAnalyzerConfiguration(ReportOnlyNonOrdinalConfigurationName, "true")
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Immutable.ImmutableDictionary.CreateBuilder<string, string>();
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportOnlyNonOrdinal_ImmutableDictionaryCreate_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net4_8)
              .AddAnalyzerConfiguration(ReportOnlyNonOrdinalConfigurationName, "true")
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Immutable.ImmutableDictionary.Create<string, string>();
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportOnlyNonOrdinal_ImmutableSortedDictionaryCreate_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net4_8)
              .AddAnalyzerConfiguration(ReportOnlyNonOrdinalConfigurationName, "true")
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Immutable.ImmutableSortedDictionary.[|Create<string, string>()|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportOnlyNonOrdinal_SortedDictionary_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .AddAnalyzerConfiguration(ReportOnlyNonOrdinalConfigurationName, "true")
              .WithSourceCode("""
                  class TypeName
                  {
                      public void Test()
                      {
                          [|new System.Collections.Generic.SortedDictionary<string, int>()|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportOnlyNonOrdinal_OrderBy_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .AddAnalyzerConfiguration(ReportOnlyNonOrdinalConfigurationName, "true")
              .WithSourceCode("""
                  using System.Linq;
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Generic.IEnumerable<string> obj = null;
                          obj.[|OrderBy(p => p)|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportOnlyNonOrdinal_Order_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net7_0)
              .AddAnalyzerConfiguration(ReportOnlyNonOrdinalConfigurationName, "true")
              .WithSourceCode("""
                  using System.Linq;
                  class TypeName
                  {
                      public void Test()
                      {
                          System.Collections.Generic.IEnumerable<string> obj = null;
                          obj.[|Order()|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task EnumerableContains_String_ShouldReportDiagnostic()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.[|Contains("""")|];
    }
}";
        const string CodeFix = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.Contains("""", System.StringComparer.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task EnumerableDistinct_String_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    System.Collections.Generic.IEnumerable<string> obj = null;
                    obj.[|Distinct()|];
                }
            }
            """;
        const string CodeFix = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task QueryableDistinct_String_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task QueryableContains_String_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task QueryableOrderBy_String_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task EnumerableToDictionary_String_ShouldReportDiagnostic()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.[|ToDictionary(p => p)|];
    }
}";
        const string CodeFix = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.ToDictionary(p => p, System.StringComparer.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task Order_String_ShouldReportDiagnostic()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.[|Order()|];
    }
}";
        const string CodeFix = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.Order(System.StringComparer.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task OrderBy_String_ShouldReportDiagnostic()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.[|OrderBy(p => p)|];
    }
}";
        const string CodeFix = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.OrderBy(p => p, System.StringComparer.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task OrderByDescending_String_ShouldReportDiagnostic()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.[|OrderByDescending(p => p)|];
    }
}";
        const string CodeFix = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.OrderByDescending(p => p, System.StringComparer.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task ThenBy_String_ShouldReportDiagnostic()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.OrderBy(p => p, System.StringComparer.Ordinal).[|ThenBy(p => p)|];
    }
}";
        const string CodeFix = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.OrderBy(p => p, System.StringComparer.Ordinal).ThenBy(p => p, System.StringComparer.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task ThenByDescending_String_ShouldReportDiagnostic()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.OrderBy(p => p, System.StringComparer.Ordinal).[|ThenByDescending(p => p)|];
    }
}";
        const string CodeFix = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.OrderBy(p => p, System.StringComparer.Ordinal).ThenByDescending(p => p, System.StringComparer.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task FindExtensionMethods()
    {
        const string SourceCode = """
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
                    a.[|Test()|];
                }
            }
            """;
        const string CodeFix = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task HashSet_Contain()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.HashSet<string> obj = null;
        obj.Contains("""");
    }
}";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ISet_Contain()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.ISet<string> obj = null;
        obj.Contains("""");
    }
}";

        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task IReadOnlySet_Contain()
    {
        const string SourceCode = """
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

        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task IImmutableSet_Contain()
    {
        const string SourceCode = """
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

        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringArray_QuerySyntax_GroupBy_NoConfiguration()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        var collection = new string[0];
        _ = from item in collection
            [|group item by item|] into g
            select g;
    }
}";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringArray_QuerySyntax_GroupBy()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        var collection = new string[0];
        _ = from item in collection
            group item by item into g
            select g;
    }
}";

        await CreateProjectBuilder()
              .AddAnalyzerConfiguration("MA0002.exclude_query_operator_syntaxes", "true")
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringArray_QuerySyntax_OrderBy_NoConfiguration()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        var collection = new string[0];
        _ = from item in collection
            orderby [|item|]
            select item;
    }
}";

        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringArray_QuerySyntax_OrderBy()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        var collection = new string[0];
        _ = from item in collection
            orderby item
            select item;
    }
}";

        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .AddAnalyzerConfiguration("MA0002.exclude_query_operator_syntaxes", "true")
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringArray_QuerySyntax_OrderByDescending_NoConfiguration()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        var collection = new string[0];
        _ = from item in collection
            orderby [|item descending|]
            select item;
    }
}";

        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringArray_QuerySyntax_OrderByDescending()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        var collection = new string[0];
        _ = from item in collection
            orderby item descending
            select item;
    }
}";

        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .AddAnalyzerConfiguration("MA0002.exclude_query_operator_syntaxes", "true")
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringArray_QuerySyntax_Join_NoConfiguration()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        var collection = new string[0];
        _ = from item1 in collection
            [|join item2 in collection on item1 equals item2|]
            select (item1, item2);
    }
}";

        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringArray_QuerySyntax_Join()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        var collection = new string[0];
        _ = from item1 in collection
            join item2 in collection on item1 equals item2
            select (item1, item2);
    }
}";

        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .AddAnalyzerConfiguration("MA0002.exclude_query_operator_syntaxes", "true")
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringArray_QuerySyntax_JoinInto_NoConfiguration()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        var collection = new string[0];
        _ = from item1 in collection
            [|join item2 in collection on item1 equals item2 into joinGroup|]
            select (item1, joinGroup);
    }
}";

        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringArray_QuerySyntax_JoinInto()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        var collection = new string[0];
        _ = from item1 in collection
            join item2 in collection on item1 equals item2 into joinGroup
            select (item1, joinGroup);
    }
}";

        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .AddAnalyzerConfiguration("MA0002.exclude_query_operator_syntaxes", "true")
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ExcludeWhenInAnExpressionContext()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_InsertComparerBeforeMessage_Issue1249()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            class Sample
            {
                void Test()
                {
                    AreEqual[|("a", "b", "message")|];
                }

                static void AreEqual(string expected, string actual, string message) { }
                static void AreEqual(string expected, string actual, IComparer<string> comparer, string message) { }
            }
            """;
        const string CodeFix = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_InsertComparerBeforeCancellationToken_Issue1250()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            using System.Threading;
            class Sample
            {
                void Test(CancellationToken ct)
                {
                    var list = new string[0];
                    list.[|ToDictionaryCustom(s => s, ct)|];
                }
            }
            static class Extensions
            {
                public static Dictionary<TKey, T> ToDictionaryCustom<T, TKey>(this IEnumerable<T> source, System.Func<T, TKey> keySelector, CancellationToken ct) => throw null;
                public static Dictionary<TKey, T> ToDictionaryCustom<T, TKey>(this IEnumerable<T> source, System.Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer, CancellationToken ct) => throw null;
            }
            """;
        const string CodeFix = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }
}
