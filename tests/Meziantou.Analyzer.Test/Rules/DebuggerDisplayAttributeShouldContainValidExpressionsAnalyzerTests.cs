using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

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

    [Fact]
    public async Task Valid()
    {
        const string SourceCode = """
            using System.Diagnostics;
            [DebuggerDisplay("{Display}")]
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

    [Fact]
    public async Task Valid_Method()
    {
        const string SourceCode = """
            using System.Diagnostics;
            [DebuggerDisplay("{Display()}")]
            public class Dummy
            {
                private string Display() => "";
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
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
}
