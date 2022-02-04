using System.Collections.Generic;
using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class PreferReturningCollectionAbstractionInsteadOfImplementationAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<PreferReturningCollectionAbstractionInsteadOfImplementationAnalyzer>();
    }

    public static IEnumerable<object[]> ReturnTypeValues
    {
        get
        {
            yield return new object[] { "private", "List<int>", true };
            yield return new object[] { "public", "Task<List<int>>", false };
            yield return new object[] { "public", "List<int>", false };
            yield return new object[] { "protected", "List<int>", false };
            yield return new object[] { "private protected", "List<int>", true };
            yield return new object[] { "public", "string", true };
        }
    }

    public static IEnumerable<object[]> ParametersTypeValues
    {
        get
        {
            yield return new object[] { "private", "List<int>", true };
            yield return new object[] { "public", "List<int>", false };
            yield return new object[] { "public", "System.Collections.ObjectModel.Collection<int>", false };
            yield return new object[] { "protected", "List<int>", false };
            yield return new object[] { "private protected", "List<int>", true };
            yield return new object[] { "public", "string", true };
        }
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValues))]
    public async Task Fields(string visibility, string type, bool isValid)
    {
        var project = CreateProjectBuilder()
              .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @"
    " + (isValid ? "" : "[||]") + type + @" _a;
}");

        await project.ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValues))]
    public async Task Delegates(string visibility, string type, bool isValid)
    {
        var project = CreateProjectBuilder()
              .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @" delegate
    " + (isValid ? "" : "[||]") + type + @" A();
}");

        await project.ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ParametersTypeValues))]
    public async Task Delegates_Parameters(string visibility, string type, bool isValid)
    {
        var project = CreateProjectBuilder()
              .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @" delegate void A(
    " + (isValid ? "" : "[||]") + type + @" p);
}");

        await project.ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValues))]
    public async Task Indexers(string visibility, string type, bool isValid)
    {
        var project = CreateProjectBuilder()
              .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @"
    " + (isValid ? "" : "[||]") + type + @" this[int value] => throw null;
}");

        await project.ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ParametersTypeValues))]
    public async Task Indexers_Parameters(string visibility, string type, bool isValid)
    {
        var project = CreateProjectBuilder()
              .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @" int this[
    " + (isValid ? "" : "[||]") + type + @" value] => throw null;
}");

        await project.ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValues))]
    public async Task Properties(string visibility, string type, bool isValid)
    {
        var project = CreateProjectBuilder()
              .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @"
    " + (isValid ? "" : "[||]") + type + @" A => throw null;
}");

        await project.ValidateAsync();
    }

    [Fact]
    public async Task Properties_XmlSerializable_XmlIgnore()
    {
        var project = CreateProjectBuilder()
              .WithSourceCode(@"
using System.Collections.Generic;
using System.Xml.Serialization;

public class Test
{
    [XmlIgnore]
    public [|List<int>|] A { get; set; }
}");

        await project.ValidateAsync();
    }

    [Fact]
    public async Task Properties_XmlSerializable_PropertyAttribute()
    {
        var project = CreateProjectBuilder()
              .WithSourceCode(@"
using System.Collections.Generic;
using System.Xml.Serialization;

public class Test
{
    [XmlArray(""sample"")]
    public List<int> A { get; set; }
}");

        await project.ValidateAsync();
    }

    [Fact]
    public async Task Properties_XmlSerializable_ClassAttribute()
    {
        var project = CreateProjectBuilder()
              .WithSourceCode(@"
using System.Collections.Generic;
using System.Xml.Serialization;

[XmlRoot(""sample"")]
public class Test
{
    public List<int> A { get; set; }
}");

        await project.ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValues))]
    public async Task Methods(string visibility, string type, bool isValid)
    {
        var project = CreateProjectBuilder()
              .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @"
    " + (isValid ? "" : "[||]") + type + @" A() => throw null;
}");

        await project.ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ParametersTypeValues))]
    public async Task Methods_Parameters(string visibility, string type, bool isValid)
    {
        var project = CreateProjectBuilder()
              .WithSourceCode(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @" void A(
    " + (isValid ? "" : "[||]") + type + @" p) => throw null;
}");

        await project.ValidateAsync();
    }

    [Fact]
    public async Task PrivateContainer()
    {
        const string SourceCode = @"using System.Collections.Generic;
internal class Test
{
    public delegate List<int> B();
    public List<int> _a;
    protected List<int> _b;
    public List<int> A() => throw null;
}";
        await CreateProjectBuilder()
             .WithSourceCode(SourceCode)
             .ValidateAsync();
    }

    [Fact]
    public async Task InterfaceImplementation()
    {
        const string SourceCode = @"using System.Collections.Generic;
public interface ITest
{
    [||]List<int> A();
}

public class Test : ITest
{
    public List<int> A() => throw null;
}";
        await CreateProjectBuilder()
             .WithSourceCode(SourceCode)
             .ValidateAsync();
    }
}
