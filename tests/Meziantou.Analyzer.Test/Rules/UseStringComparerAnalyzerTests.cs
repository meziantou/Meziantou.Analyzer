using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseStringComparerAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp9)
            .WithAnalyzer<UseStringComparerAnalyzer>()
            .WithCodeFixProvider<UseStringComparerFixer>();
    }

    [Fact]
    public async Task HashSet_Int32_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.HashSet<int>();
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SortedList_string_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        [||]new System.Collections.Generic.SortedList<string, int>();
    }
}";

        const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.SortedList<string, int>(System.StringComparer.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task HashSet_String_ShouldReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        [||]new System.Collections.Generic.HashSet<string>();
    }
}";
        const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task HashSet_String__ShortNew_ShouldReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.HashSet<string> a = [||]new();
    }
}";
        const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.HashSet<string> a = new(System.StringComparer.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task HashSet_String_StringEqualityComparer_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Dictionary_String_ShouldReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        [||]new System.Collections.Generic.Dictionary<string, int>();
    }
}";
        const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task ConcurrentDictionary_String_ShouldReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        [||]new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
    }
}";
        const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Concurrent.ConcurrentDictionary<string, int>(System.StringComparer.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
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
        [||]obj.Contains("""");
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
    public async Task EnumerableToDictionary_String_ShouldReportDiagnostic()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        [||]obj.ToDictionary(p => p);
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
        [||]obj.Order();
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
        [||]obj.OrderBy(p => p);
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
        [||]obj.OrderByDescending(p => p);
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
        [||]obj.OrderBy(p => p, System.StringComparer.Ordinal).ThenBy(p => p);
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
        [||]obj.OrderBy(p => p, System.StringComparer.Ordinal).ThenByDescending(p => p);
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
        const string SourceCode = @"
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
        [||]a.Test();
    }
}
";
        const string CodeFix = @"
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
";
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
    public async Task StringArray_QuerySyntax_GroupBy_NoConfiguration()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        var collection = new string[0];
        _ = from item in collection
            [||]group item by item into g
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
            orderby [||]item
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
            orderby [||]item descending
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
            [||]join item2 in collection on item1 equals item2
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
            [||]join item2 in collection on item1 equals item2 into joinGroup
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
}
