﻿using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class FileNameMustMatchTypeNameAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<FileNameMustMatchTypeNameAnalyzer>();
    }

    [Fact]
    public async Task DoesNotMatchFileName()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class [||]Sample
{
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task DoesMatchFileNameBeforeDot()
    {
        await CreateProjectBuilder()
              .WithSourceCode("Sample.xaml.cs", @"
class Sample
{
}")
              .ValidateAsync();
    }
    
    [Fact]
    public async Task MatchFileName()
    {
        await CreateProjectBuilder()
              .WithSourceCode("Root\\Foo/Bar.cs", @"
class Bar
{
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task DoesMatchFileName()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test0
{
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task DoesMatchFileName_Generic()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test0<T>
{
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task DoesMatchFileName_GenericUsingArity()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0`1.cs", @"
class Test0<T>
{
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task DoesMatchFileName_GenericUsingOfT()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0OfT.cs", @"
class Test0<T>
{
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task NestedTypeDoesMatchFileName_Ok()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0.cs", @"
class Test0
{
    class Test1
    {
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Brackets_MatchType()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0{T}.cs", @"
class Test0<T>
{
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Brackets_MatchTypes()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0{TKey,TValue}.cs", @"
class Test0<TKey, TValue>
{
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Brackets_DoesNotMatchTypeCount()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0{TKey}.cs", @"
class [||]Test0<TKey, TValue>
{
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Brackets_DoesNotMatchTypeName()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0{TKey,TNotSame}.cs", @"
class [||]Test0<TKey, TValue>
{
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task MatchOnlyFirstType_class1()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0.cs", """
                  class [||]Foo {}
                  class Bar {}
                  """)
              .AddAnalyzerConfiguration("MA0048.only_validate_first_type", "true")
              .ValidateAsync();
    }

    [Fact]
    public async Task MatchOnlyFirstType_class2()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0.cs", """
                  class Test0 {}
                  class Sample {}
                  """)
              .AddAnalyzerConfiguration("MA0048.only_validate_first_type", "true")
              .ValidateAsync();
    }
    
    [Fact]
    public async Task MatchOnlyFirstType_class3()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0.cs", """
                  class [||]Sample {}
                  class Test0 {}
                  """)
              .AddAnalyzerConfiguration("MA0048.only_validate_first_type", "true")
              .ValidateAsync();
    }
    
    [Fact]
    public async Task MatchOnlyFirstType_Enum()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0.cs", """
                  enum [||]Foo {}
                  enum Bar {}
                  """)
              .AddAnalyzerConfiguration("MA0048.only_validate_first_type", "true")
              .ValidateAsync();
    }
    
    [Fact]
    public async Task MatchOnlyFirstType_Interface()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0.cs", """
                  interface [||]Foo {}
                  interface Bar {}
                  """)
              .AddAnalyzerConfiguration("MA0048.only_validate_first_type", "true")
              .ValidateAsync();
    }    
    
    [Fact]
    public async Task MatchOnlyFirstType_Record()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0.cs", """
                  record [||]Foo {}
                  record Bar {}
                  """)
              .AddAnalyzerConfiguration("MA0048.only_validate_first_type", "true")
              .ValidateAsync();
    }

#if CSHARP11_OR_GREATER
    [Fact]
    public async Task MatchOnlyFirstType_RecordStruct()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0.cs", """
                  record struct [||]Foo {}
                  record struct Bar {}
                  """)
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp11)
              .AddAnalyzerConfiguration("MA0048.only_validate_first_type", "true")
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task MatchOnlyFirstType_Struct()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0.cs", """
                  struct [||]Foo {}
                  struct Bar {}
                  """)
              .AddAnalyzerConfiguration("MA0048.only_validate_first_type", "true")
              .ValidateAsync();
    }

    [Fact]
    public async Task MatchExcludedSymbolNames_SymbolName_ExactMatch()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0.cs", """
                  class Test0 {}
                  class Sample {}
                  """)
             .AddAnalyzerConfiguration("dotnet_diagnostic.MA0048.excluded_symbol_names", "Sample")
             .ValidateAsync();
    }

    [Fact]
    public async Task MatchExcludedSymbolNames_SymbolName_ExactMatch_Pipe()
    {
        await CreateProjectBuilder()
             .WithSourceCode(fileName: "Test0.cs", """
                  class Test0 {}
                  class Sample1 {}
                  class Sample2 {}
                  """)
             .AddAnalyzerConfiguration("dotnet_diagnostic.MA0048.excluded_symbol_names", "Sample1|Sample2")
             .ValidateAsync();
    }

    [Fact]
    public async Task MatchExcludedSymbolNames_DeclarationId_ExactMatch()
    {
        await CreateProjectBuilder()
             .WithSourceCode(fileName: "Test0.cs", """
                                                   namespace MyNamespace {
                                                     class Test0 {}
                                                     class Sample {}
                                                   }
                                                   """)
             .AddAnalyzerConfiguration("dotnet_diagnostic.MA0048.excluded_symbol_names", "T:MyNamespace.Sample")
             .ValidateAsync();
    }

    [Fact]
    public async Task MatchExcludedSymbolNames_DeclarationId_ExactMatch_Pipe()
    {
        await CreateProjectBuilder()
             .WithSourceCode(fileName: "Test0.cs", """
                                                   namespace MyNamespace {
                                                     class Test0 {}
                                                     class Sample1 {}
                                                     class Sample2 {}
                                                   }
                                                   """)
             .AddAnalyzerConfiguration("dotnet_diagnostic.MA0048.excluded_symbol_names", "T:MyNamespace.Sample1|T:MyNamespace.Sample2")
             .ValidateAsync();
    }

    [Fact]
    public async Task MatchExcludedSymbolNames_MixSymbolNameDeclarationId_ExactMatch_Pipe()
    {
        await CreateProjectBuilder()
             .WithSourceCode(fileName: "Test0.cs", """
                                                   namespace MyNamespace {
                                                     class Test0 {}
                                                     class Sample1 {}
                                                     class Sample2 {}
                                                   }
                                                   """)
             .AddAnalyzerConfiguration("dotnet_diagnostic.MA0048.excluded_symbol_names", "T:MyNamespace.Sample1|Sample2")
             .ValidateAsync();
    }

    [Fact]
    public async Task MatchExcludedSymbolNames_ExactMatch_DeclarationId_Pipe()
    {
        await CreateProjectBuilder()
             .WithSourceCode(fileName: "Test0.cs", """
                                                   namespace MyNamespace {
                                                     class Test0 {}
                                                     class Sample1 {}
                                                     class Sample2 {}
                                                   }
                                                   """)
             .AddAnalyzerConfiguration("dotnet_diagnostic.MA0048.excluded_symbol_names", "T:MyNamespace.Sample1|T:MyNamespace.Sample2")
             .ValidateAsync();
    }

    [Fact]
    public async Task MatchExcludedSymbolNames_SymbolName_WildcardMatch1()
    {
        await CreateProjectBuilder()
             .WithSourceCode(fileName: "Test0.cs", """
                  class Test0 {}
                  class Sample {}
                  """)
             .AddAnalyzerConfiguration("dotnet_diagnostic.MA0048.excluded_symbol_names", "*ample")
             .ValidateAsync();
    }

    [Fact]
    public async Task MatchExcludedSymbolNames_SymbolName_WildcardMatch2()
    {
        await CreateProjectBuilder()
             .WithSourceCode(fileName: "Test0.cs", """
                  class Test0 {}
                  class Sample {}
                  """)
             .AddAnalyzerConfiguration("dotnet_diagnostic.MA0048.excluded_symbol_names", "*ampl*")
             .ValidateAsync();
    }

    [Fact]
    public async Task MatchExcludedSymbolNames_SymbolName_WildcardMatch_Pipe()
    {
        await CreateProjectBuilder()
             .WithSourceCode(fileName: "Test0.cs", """
                  class Test0 {}
                  class Sample1 {}
                  class Sample2 {}
                  """)
             .AddAnalyzerConfiguration("dotnet_diagnostic.MA0048.excluded_symbol_names", "*ample1|Sample2")
             .ValidateAsync();
    }

#if ROSLYN_4_4_OR_GREATER
    [Fact]
    public async Task FileLocalTypes()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Dummy.cs", @"
class Dummy
{
}

file class Sample
{
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task FileLocalTypes_Configuration()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Dummy.cs", @"
class Dummy
{
}

file class [||]Sample
{
}
")
              .AddAnalyzerConfiguration("MA0048.exclude_file_local_types", "false")
              .ValidateAsync();
    }
#endif
}
