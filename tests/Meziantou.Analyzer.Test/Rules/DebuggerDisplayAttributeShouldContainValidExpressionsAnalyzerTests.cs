using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DebuggerDisplayAttributeShouldContainValidExpressionsAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DebuggerDisplayAttributeShouldContainValidExpressionsAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Theory]
    [InlineData("Invalid")]
    [InlineData("Invalid,np")]
    [InlineData("Invalid()")]
    [InlineData("Invalid.Length")]
    [InlineData("System.IO.Path.DirectorySeparatorChar.Unknown()")]
    public Task UnknownMember(string memberName)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Diagnostics;
            [{|MA0151:DebuggerDisplay("{{{memberName}}}")|}]
            public class Dummy
            {
                public string Display { get; }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("Invalid,np")]
    [InlineData("Invalid()")]
    [InlineData("Invalid.Length")]
    public Task UnknownMember_Name(string memberName)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Diagnostics;
            [{|MA0151:DebuggerDisplay("", Name = "{{{memberName}}}")|}]
            public class Dummy
            {
                public string Display { get; }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("Invalid,np")]
    [InlineData("Invalid()")]
    [InlineData("Invalid.Length")]
    [InlineData("Display.UnknownProperty")]
    public Task UnknownMember_Type(string memberName)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Diagnostics;
            [{|MA0151:DebuggerDisplay("", Type = "{{{memberName}}}")|}]
            public class Dummy
            {
                public string Display { get; }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Display")]
    [InlineData("System.IO.Path.DirectorySeparatorChar.ToString()")]
    public Task Valid(string value)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Diagnostics;
            [DebuggerDisplay("{{{value}}}")]
            public class Dummy
            {
                public string Display { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Valid_Name()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics;
            [DebuggerDisplay("", Name = "{Display}")]
            public class Dummy
            {
                public string Display { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Valid_Type()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics;
            [DebuggerDisplay("", Type = "{Display}")]
            public class Dummy
            {
                public string Display { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidWithOptions()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics;
            [DebuggerDisplay("{Display,nq}")]
            public class Dummy
            {
                public string Display { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Valid_Field()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics;
            [DebuggerDisplay("{display}")]
            public class Dummy
            {
                private string display;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Valid_SubProperty()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics;
            [DebuggerDisplay("{display.Length}")]
            public class Dummy
            {
                private string display;
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Display()")]
    [InlineData("Display().Length")]
    [InlineData("Display().Invalid")] // Invalid is ignored because we cannot determine the return type of Display()
    public Task Valid_Method(string value)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Diagnostics;
            [DebuggerDisplay("{{{value}}}")]
            public class Dummy
            {
                private string Display() => "";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Valid_FromBaseClass()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics;

            public class Base
            {
                private string Display => "";
            }

            [DebuggerDisplay("{Display}")]
            public class Dummy : Base
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SkipEscapedBraces1()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics;

            [DebuggerDisplay(@"Person \{ Name = {Name} \}")]
            public record Person(string Name);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SkipEscapedBraces2()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics;

            [{|MA0151:DebuggerDisplay(@"Person \\{NameInvalid}")|}]
            public record Person(string Name);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SkipEscapedBraces3()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics;

            [DebuggerDisplay(@"Person \\\{NameInvalid}")]
            public record Person(string Name);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EscapeSingleChar()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics;

            [DebuggerDisplay(@"\")]
            public record Person(int Value);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Escape_IncompleteExpression()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics;

            [DebuggerDisplay(@"{\")]
            public record Person(int Value);
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Value + 1")]
    [InlineData("Value - 1")]
    [InlineData("Value < 10")]
    [InlineData("Value <= 10")]
    [InlineData("Value > 10")]
    [InlineData("Value >= 10")]
    [InlineData("Value == 10")]
    [InlineData("Value != 10")]
    [InlineData("Demo.Display(Value > 1)")]
    [InlineData("Demo.Display(Value > 1, Value)")]
    public Task Valid_BinaryOperator(string value)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Diagnostics;

            [DebuggerDisplay(@"{{{value}}}")]
            public record Person(int Value);

            public class Demo
            {
                public static string Display(bool a) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("(Unknown + 1)")]
    [InlineData("(Unknown + (1))")]
    [InlineData("((Unknown) + (1))")]
    [InlineData("Unknown + 1")]
    [InlineData("Unknown - 1")]
    [InlineData("Unknown < 10")]
    [InlineData("Unknown <= 10")]
    [InlineData("Unknown > 10")]
    [InlineData("Unknown >= 10")]
    [InlineData("Unknown == 10")]
    [InlineData("Unknown != 10")]
    [InlineData("Unknown != \\\"abc\\\"")]
    [InlineData("Unknown != 'a'")]
    [InlineData("Demo.Display(Unknown > 1)")]
    [InlineData("Demo.Display(Unknown > 1.0)")]
    [InlineData("Demo.Display(Unknown > 1u)")]
    [InlineData("Demo.Display(Unknown > 1uL)")]
    [InlineData("Demo.Display(Unknown > 1L)")]
    [InlineData("Demo.Display(Unknown > 1.0f)")]
    [InlineData("Demo.Display(Unknown > 1.0d)")]
    [InlineData("Demo.Display(Unknown > 1.0m)")]
    [InlineData("Demo.Display(Unknown > 1e+3)")]
    [InlineData("Demo.Display(Value > 1, Unknown)")]
    public Task Invalid_BinaryOperator(string value)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Diagnostics;

            [{|MA0151:DebuggerDisplay("{{{value}}}")|}]
            public record Person(int Value);

            public class Demo
            {
                public static string Display(bool a) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("!Value")]
    public Task Valid_UnaryOperator(string value)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Diagnostics;

            [DebuggerDisplay(@"{{{value}}}")]
            public record Person(bool Value);
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("!Unknown")]
    public Task Invalid_UnaryOperator(string value)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Diagnostics;

            [{|MA0151:DebuggerDisplay(@"{{{value}}}")|}]
            public record Person(bool Value);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallStaticMethodOnAnotherType()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics;

            [DebuggerDisplay(@"{System.Linq.Enumerable.Count(Test)}")]
            public record Person(string[] Test);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallStaticMethodOnAnotherUsingKeyword()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics;

            [DebuggerDisplay(@"{char.IsAscii(Test)}")]
            public record Person(char Test);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallStaticMethodOnAnotherType_InvalidMethodName()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics;

            [{|MA0151:DebuggerDisplay(@"{System.Linq.Enumerable.InvalidMethod(Test)}")|}]
            public record Person(string[] Test);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IListOfT_Count()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics;
            [DebuggerDisplay("{referenceString} ({allStrings.Count})")]
            public struct ContainedStrings
            {
                private readonly System.Collections.Generic.IList<string> allStrings;
                private readonly string referenceString;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericTypes()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics;
            [DebuggerDisplay("{Condition.ToString(),nq}")]
            public class ValueConditionNode<TCondition> : IValueConditionNode<TCondition>
            {
                public TCondition Condition => throw null;
            }

            public interface IValueConditionNode<TCondition>
            {
            }
            """;

        return test.RunAsync();
    }
}
