using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.PreferReturningCollectionAbstractionInsteadOfImplementationAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class PreferReturningCollectionAbstractionInsteadOfImplementationAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

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
    public Task Fields_NoReport(string visibility, string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Test
            {
                {{visibility}} {{type}} _dummy;
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesInvalid))]
    public Task Fields_Reports(string visibility, string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Test
            {
                {{visibility}} [|{{type}}|] _dummy;
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesValid))]
    public Task Delegates_ReturnType_NoReport(string visibility, string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Test
            {
                {{visibility}} delegate {{type}} Dummy(int p);
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesValid))]
    public Task Delegates_Parameter_NoReport(string visibility, string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Test
            {
                {{visibility}} delegate void Dummy({{type}} p);
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesInvalid))]
    public Task Delegates_ReturnType_Report(string visibility, string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Test
            {
                {{visibility}} delegate [|{{type}}|] Dummy(int p);
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesInvalid))]
    public Task Delegates_Parameter_Report(string visibility, string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Test
            {
                {{visibility}} delegate void Dummy([|{{type}}|] p);
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesValid))]
    public Task Indexers_Valid(string visibility, string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Test
            {
                {{visibility}} {{type}} this[int value] => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesValid))]
    public Task Indexers_Parameter_Valid(string visibility, string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Test
            {
                {{visibility}} int this[{{type}} value] => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesInvalid))]
    public Task Indexers_Invalid(string visibility, string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Test
            {
                {{visibility}} [|{{type}}|] this[int value] => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesInvalid))]
    public Task Indexers_Parameter_Invalid(string visibility, string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Test
            {
                {{visibility}} int this[[|{{type}}|] value] => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesValid))]
    public Task Properties_Valid(string visibility, string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Test
            {
                {{visibility}} {{type}} Dummy => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesInvalid))]
    public Task Properties_Invalid(string visibility, string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Test
            {
                {{visibility}} [|{{type}}|] Dummy => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Properties_XmlSerializable_XmlIgnore()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Xml.Serialization;

            public class Test
            {
                [XmlIgnore]
                public [|List<int>|] A { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Properties_XmlSerializable_PropertyAttribute()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Xml.Serialization;

            public class Test
            {
                [XmlArray("dummy")]
                public List<int> A { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Properties_XmlSerializable_ClassAttribute()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Xml.Serialization;

            [XmlRoot("sample")]
            public class Test
            {
                public List<int> A { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesValid))]
    public Task Methods_Valid(string visibility, string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Test
            {
                {{visibility}} {{type}} Dummy() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesInvalid))]
    public Task Methods_Invalid(string visibility, string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Test
            {
                {{visibility}} [|{{type}}|] Dummy() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesValid))]
    public Task Methods_Parameter_Valid(string visibility, string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Test
            {
                {{visibility}} void Dummy({{type}} p) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(ReturnTypeValuesInvalid))]
    public Task Methods_Parameter_Invalid(string visibility, string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public class Test
            {
                {{visibility}} void Dummy([|{{type}}|] p) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PrivateContainer()
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            internal class Test
            {
                public delegate List<int> B();
                public List<int> _a;
                protected List<int> _b;
                public List<int> A() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterfaceImplementation()
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            public interface ITest
            {
                [|List<int>|] A();
            }

            public class Test : ITest
            {
                public List<int> A() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConversionOperator()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Sample
            {
                public static implicit operator Sample(System.Collections.Generic.List<string> _) => throw null;
                public static implicit operator System.Collections.Generic.List<string>(Sample _) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AddOperator()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Sample
            {
                public static Sample operator+(Sample instance, [|System.Collections.Generic.List<int>|] value) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AddOperator_Instance()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Sample : System.Collections.Generic.List<int>
            {
                public static Sample operator+(Sample instance, int value) => throw null;
            }
            """;

        return test.RunAsync();
    }
}
