using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class PreferReturningCollectionAbstractionInsteadOfImplementationAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder() =>
        new ProjectBuilder()
            .WithAnalyzer<PreferReturningCollectionAbstractionInsteadOfImplementationAnalyzer>();

    public static TheoryData<string, string> ReturnTypeValuesValid => new()
    {
        { "private", "List<int>" },
        { "private protected", "List<int>" },
        { "public", "string" },
    };

    public static TheoryData<string, string> ReturnTypeValuesInvalid => new()
    {
        { "public", "Task<List<int>>" },
        { "public", "List<int>" },
        { "public", "System.Collections.ObjectModel.Collection<int>" },
        { "protected", "List<int>" },
        { "internal protected", "List<int>" },
    };

    [Theory]
    [MemberData(nameof(ReturnTypeValuesValid))]
    public async Task Fields_NoReport(string visibility, string type)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class Test
                {
                    {{visibility}} {{type}} _dummy;
                }
                """)
            .ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesInvalid))]
    public async Task Fields_Reports(string visibility, string type)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class Test
                {
                    {{visibility}} [|{{type}}|] _dummy;
                }
                """)
            .ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesValid))]
    public async Task Delegates_ReturnType_NoReport(string visibility, string type)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class Test
                {
                    {{visibility}} delegate {{type}} Dummy(int p);
                }
                """)
            .ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesValid))]
    public async Task Delegates_Parameter_NoReport(string visibility, string type)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class Test
                {
                    {{visibility}} delegate void Dummy({{type}} p);
                }
                """)
            .ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesInvalid))]
    public async Task Delegates_ReturnType_Report(string visibility, string type)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class Test
                {
                    {{visibility}} delegate [|{{type}}|] Dummy(int p);
                }
                """)
            .ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesInvalid))]
    public async Task Delegates_Parameter_Report(string visibility, string type)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class Test
                {
                    {{visibility}} delegate void Dummy([|{{type}}|] p);
                }
                """)
            .ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesValid))]
    public async Task Indexers_Valid(string visibility, string type)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class Test
                {
                    {{visibility}} {{type}} this[int value] => throw null;
                }
                """)
            .ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesValid))]
    public async Task Indexers_Parameter_Valid(string visibility, string type)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class Test
                {
                    {{visibility}} int this[{{type}} value] => throw null;
                }
                """)
            .ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesInvalid))]
    public async Task Indexers_Invalid(string visibility, string type)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class Test
                {
                    {{visibility}} [|{{type}}|] this[int value] => throw null;
                }
                """)
            .ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesInvalid))]
    public async Task Indexers_Parameter_Invalid(string visibility, string type)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class Test
                {
                    {{visibility}} int this[[|{{type}}|] value] => throw null;
                }
                """)
            .ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesValid))]
    public async Task Properties_Valid(string visibility, string type)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class Test
                {
                    {{visibility}} {{type}} Dummy => throw null;
                }
                """)
            .ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesInvalid))]
    public async Task Properties_Invalid(string visibility, string type)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class Test
                {
                    {{visibility}} [|{{type}}|] Dummy => throw null;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Properties_XmlSerializable_XmlIgnore()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Collections.Generic;
                using System.Xml.Serialization;

                public class Test
                {
                    [XmlIgnore]
                    public [|List<int>|] A { get; set; }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Properties_XmlSerializable_PropertyAttribute()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Collections.Generic;
                using System.Xml.Serialization;

                public class Test
                {
                    [XmlArray("dummy")]
                    public List<int> A { get; set; }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Properties_XmlSerializable_ClassAttribute()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Collections.Generic;
                using System.Xml.Serialization;

                [XmlRoot("sample")]
                public class Test
                {
                    public List<int> A { get; set; }
                }
                """)
            .ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesValid))]
    public async Task Methods_Valid(string visibility, string type)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class Test
                {
                    {{visibility}} {{type}} Dummy() => throw null;
                }
                """)
            .ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesInvalid))]
    public async Task Methods_Invalid(string visibility, string type)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class Test
                {
                    {{visibility}} [|{{type}}|] Dummy() => throw null;
                }
                """)
            .ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesValid))]
    public async Task Methods_Parameter_Valid(string visibility, string type)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class Test
                {
                    {{visibility}} void Dummy({{type}} p) => throw null;
                }
                """)
            .ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesInvalid))]
    public async Task Methods_Parameter_Invalid(string visibility, string type)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class Test
                {
                    {{visibility}} void Dummy([|{{type}}|] p) => throw null;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task PrivateContainer()
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                internal class Test
                {
                    public delegate List<int> B();
                    public List<int> _a;
                    protected List<int> _b;
                    public List<int> A() => throw null;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task InterfaceImplementation()
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public interface ITest
                {
                    [||]List<int> A();
                }
                
                public class Test : ITest
                {
                    public List<int> A() => throw null;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ConversionOperator()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""                
                public class Sample
                {
                    public static implicit operator Sample(System.Collections.Generic.List<string> _) => throw null;
                    public static implicit operator System.Collections.Generic.List<string>(Sample _) => throw null;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task AddOperator()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""                                
                public class Sample
                {
                    public static Sample operator+(Sample instance, [|System.Collections.Generic.List<int>|] value) => throw null;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task AddOperator_Instance()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""                                
                public class Sample : System.Collections.Generic.List<int>
                {
                    public static Sample operator+(Sample instance, int value) => throw null;
                }
                """)
            .ValidateAsync();
    }
}
