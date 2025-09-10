using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DebuggerDisplayAttributeShouldContainValidExpressionsAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DebuggerDisplayAttributeShouldContainValidExpressionsAnalyzer>()
            .WithTargetFramework(TargetFramework.NetLatest);
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("Invalid,np")]
    [InlineData("Invalid()")]
    [InlineData("Invalid.Length")]
    [InlineData("System.IO.Path.DirectorySeparatorChar.Unknown()")]
    public async Task UnknownMember(string memberName)
    {
        var sourceCode = $$"""
            using System.Diagnostics;
            [[|DebuggerDisplay("{{{memberName}}}")|]]
            public class Dummy
            {
                public string Display { get; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("Invalid,np")]
    [InlineData("Invalid()")]
    [InlineData("Invalid.Length")]
    public async Task UnknownMember_Name(string memberName)
    {
        var sourceCode = $$"""
            using System.Diagnostics;
            [[|DebuggerDisplay("", Name = "{{{memberName}}}")|]]
            public class Dummy
            {
                public string Display { get; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("Invalid,np")]
    [InlineData("Invalid()")]
    [InlineData("Invalid.Length")]
    [InlineData("Display.UnknownProperty")]
    public async Task UnknownMember_Type(string memberName)
    {
        var sourceCode = $$"""
            using System.Diagnostics;
            [[|DebuggerDisplay("", Type = "{{{memberName}}}")|]]
            public class Dummy
            {
                public string Display { get; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("Display")]
    [InlineData("System.IO.Path.DirectorySeparatorChar.ToString()")]
    public async Task Valid(string value)
    {
        var sourceCode = $$"""
            using System.Diagnostics;
            [DebuggerDisplay("{{{value}}}")]
            public class Dummy
            {
                public string Display { get; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Valid_Name()
    {
        const string SourceCode = """
            using System.Diagnostics;
            [DebuggerDisplay("", Name = "{Display}")]
            public class Dummy
            {
                public string Display { get; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Valid_Type()
    {
        const string SourceCode = """
            using System.Diagnostics;
            [DebuggerDisplay("", Type = "{Display}")]
            public class Dummy
            {
                public string Display { get; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ValidWithOptions()
    {
        const string SourceCode = """
            using System.Diagnostics;
            [DebuggerDisplay("{Display,nq}")]
            public class Dummy
            {
                public string Display { get; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Valid_Field()
    {
        const string SourceCode = """
            using System.Diagnostics;
            [DebuggerDisplay("{display}")]
            public class Dummy
            {
                private string display;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Valid_SubProperty()
    {
        const string SourceCode = """
            using System.Diagnostics;
            [DebuggerDisplay("{display.Length}")]
            public class Dummy
            {
                private string display;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("Display()")]
    [InlineData("Display().Length")]
    [InlineData("Display().Invalid")] // Invalid is ignored because we cannot determine the return type of Display()
    public async Task Valid_Method(string value)
    {
        var sourceCode = $$"""
            using System.Diagnostics;
            [DebuggerDisplay("{{{value}}}")]
            public class Dummy
            {
                private string Display() => "";
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Valid_FromBaseClass()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SkipEscapedBraces1()
    {
        const string SourceCode = """
            using System.Diagnostics;

            [DebuggerDisplay(@"Person \{ Name = {Name} \}")]
            public record Person(string Name);
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SkipEscapedBraces2()
    {
        const string SourceCode = """
            using System.Diagnostics;

            [[|DebuggerDisplay(@"Person \\{NameInvalid}")|]]
            public record Person(string Name);
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SkipEscapedBraces3()
    {
        const string SourceCode = """
            using System.Diagnostics;

            [DebuggerDisplay(@"Person \\\{NameInvalid}")]
            public record Person(string Name);
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task EscapeSingleChar()
    {
        const string SourceCode = """
            using System.Diagnostics;

            [DebuggerDisplay(@"\")]
            public record Person(int Value);
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Escape_IncompleteExpression()
    {
        const string SourceCode = """
            using System.Diagnostics;

            [DebuggerDisplay(@"{\")]
            public record Person(int Value);
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
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
    public async Task Valid_BinaryOperator(string value)
    {
        var sourceCode = $$"""
            using System.Diagnostics;

            [DebuggerDisplay(@"{{{value}}}")]
            public record Person(int Value);

            public class Demo
            {
                public static string Display(bool a) => throw null;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
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
    public async Task Invalid_BinaryOperator(string value)
    {
        var sourceCode = $$"""
            using System.Diagnostics;

            [[|DebuggerDisplay("{{{value}}}")|]]
            public record Person(int Value);

            public class Demo
            {
                public static string Display(bool a) => throw null;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("!Value")]
    public async Task Valid_UnaryOperator(string value)
    {
        var sourceCode = $$"""
            using System.Diagnostics;

            [DebuggerDisplay(@"{{{value}}}")]
            public record Person(bool Value);
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("!Unknown")]
    public async Task Invalid_UnaryOperator(string value)
    {
        var sourceCode = $$"""
            using System.Diagnostics;

            [[|DebuggerDisplay(@"{{{value}}}")|]]
            public record Person(bool Value);
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CallStaticMethodOnAnotherType()
    {
        const string SourceCode = """
            using System.Diagnostics;

            [DebuggerDisplay(@"{System.Linq.Enumerable.Count(Test)}")]
            public record Person(string[] Test);
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CallStaticMethodOnAnotherUsingKeyword()
    {
        const string SourceCode = """
            using System.Diagnostics;

            [DebuggerDisplay(@"{char.IsAscii(Test)}")]
            public record Person(char Test);
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CallStaticMethodOnAnotherType_InvalidMethodName()
    {
        const string SourceCode = """
            using System.Diagnostics;

            [[|DebuggerDisplay(@"{System.Linq.Enumerable.InvalidMethod(Test)}")|]]
            public record Person(string[] Test);
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task IListOfT_Count()
    {
        const string SourceCode = """
            using System.Diagnostics;
            [DebuggerDisplay("{referenceString} ({allStrings.Count})")]
            public struct ContainedStrings
            {
                private readonly System.Collections.Generic.IList<string> allStrings;
                private readonly string referenceString;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericTypes()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
