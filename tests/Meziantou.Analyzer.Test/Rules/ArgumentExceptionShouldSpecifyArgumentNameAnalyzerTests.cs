using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ArgumentExceptionShouldSpecifyArgumentNameAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<ArgumentExceptionShouldSpecifyArgumentNameAnalyzer>(id: "MA0015");
    }

    [Fact]
    public async Task ArgumentNameIsSpecified_Record_ShouldNotReportError()
    {
        var sourceCode = """
            using System;
            internal sealed record ManuscriptId(int Id)
            {
                public int Id { get; } = Id > 0 ? Id : throw new ArgumentOutOfRangeException(paramName: nameof(Id), Id, message: "Must be greater than 0");
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ArgumentNameIsSpecified_LocalFunction_ShouldNotReportError()
    {
        var sourceCode = """
            class Sample
            {
                void Test(string test)
                {
                    void LocalFunction(string a)
                    {
                        throw new System.ArgumentNullException(nameof(a));
                    }
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ArgumentNameIsSpecified_LocalFunction_Static_ShouldNotReportError()
    {
        var sourceCode = """
            class Sample
            {
                void Test(string test)
                {
                    static void LocalFunction(string a)
                    {
                        throw new System.ArgumentNullException([|"test"|]);
                    }
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ArgumentNameIsSpecified_LocalFunction_ArgumentFromParentMethod_ShouldNotReportError()
    {
        var sourceCode = """
            class Sample
            {
                void Test(string test)
                {
                    void LocalFunction()
                    {
                        throw new System.ArgumentNullException(nameof(test));
                    }
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ArgumentNameIsSpecified_Operator_ShouldNotReportError()
    {
        var sourceCode = """
            class Sample
            {
                public static Sample operator +(Sample first, Sample second)
                {
                    throw new System.ArgumentNullException(nameof(first));
                    throw new System.ArgumentNullException(nameof(second));
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ArgumentNameIsSpecified_Method_ShouldNotReportError()
    {
        var sourceCode = """
            class Sample
            {
                Sample(string test)
                {
                    throw new System.Exception();
                    throw new System.ArgumentException("message", nameof(test));
                    throw new System.ArgumentNullException(nameof(test));
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ArgumentNameIsSpecified_Indexer_ShouldNotReportError()
    {
        var sourceCode = """
            class Sample
            {
                string this[int index]
                {
                    get { throw new System.ArgumentNullException(nameof(index)); }
                    set { throw new System.ArgumentNullException(nameof(index)); }
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ArgumentNameIsSpecified_Setter_ShouldNotReportError()
    {
        var sourceCode = """
            class Sample
            {
                string Prop
                {
                    get { throw null; }
                    set { throw new System.ArgumentNullException(nameof(value)); }
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ArgumentNameDoesNotMatchAParameter_Properties_ShouldReportError()
    {
        const string SourceCode = @"
class TestAttribute
{
    string Prop
    {
        get { throw null; }
        set { throw new System.ArgumentNullException([||]""unknown""); }
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("'unknown' is not a valid parameter name")
              .ValidateAsync();
    }

    [Fact]
    public async Task ArgumentNameDoesNotMatchAParameter_Methods_ShouldReportError()
    {
        const string SourceCode = @"
class TestAttribute
{
    void Test(string test)
    {
        throw new System.ArgumentException(""message"", [||]""unknown"");
    }  
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("'unknown' is not a valid parameter name")
              .ValidateAsync();
    }

    [Fact]
    public async Task OverloadWithoutParameterName_Properties_ShouldReportError()
    {
        const string SourceCode = @"
class TestAttribute
{
    string Prop
    {
        get { throw null; }
        set { throw [||]new System.ArgumentNullException(); }
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OverloadWithoutParameterName_Methods_ShouldReportError()
    {
        const string SourceCode = @"
class TestAttribute
{
    void Test(string test)
    {
        throw [||]new System.ArgumentException(""message"");
    }    
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ValidParameterName_Lambda()
    {
        const string SourceCode = @"
class TestAttribute
{
    void Test(string test)
    {
        _ = new System.Action<int>((int a) =>
        {
            throw new System.ArgumentOutOfRangeException(paramName: nameof(a), a, message: ""address out of range"");
            throw new System.ArgumentOutOfRangeException(paramName: nameof(test), a, message: ""address out of range"");
	    });
    }    
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InvalidParameterName_Lambda()
    {
        const string SourceCode = @"
class TestAttribute
{
    void Test(string test)
    {
        _ = new System.Action<int>((int a) =>
        {
		    if (a < 0)
                throw new System.ArgumentOutOfRangeException(paramName: [|""dummy""|], a, message: ""address out of range"");
	    });
    }    
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InvalidParameterName_StaticLambda()
    {
        const string SourceCode = @"
class TestAttribute
{
    void Test(string test)
    {
        _ = new System.Action<int>(static (int a) =>
        {
		    if (a < 0)
                throw new System.ArgumentOutOfRangeException(paramName: [|""test""|], a, message: ""address out of range"");
	    });
    }    
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ValidParameterName_LambdaWithoutParentheses()
    {
        const string SourceCode = @"
class TestAttribute
{
    void Test(string test)
    {
        _ = new System.Action<int>(a =>
        {
            throw new System.ArgumentOutOfRangeException(paramName: nameof(a), a, message: ""address out of range"");
	    });
    }    
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ValidParameterName_StaticLambdaWithoutParameter()
    {
        const string SourceCode = @"
class TestAttribute
{
    void Test(string test)
    {
        _ = new System.Action(static () =>
        {
            throw new System.ArgumentNullException([|""test""|]);
	    });
    }    
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InvalidParameterName_Delegate()
    {
        const string SourceCode = @"
class TestAttribute
{
    void Test(string test)
    {
        _ = new System.Action<int>(delegate (int a)
        {
		    if (a < 0)
                throw new System.ArgumentOutOfRangeException(paramName: [|""dummy""|], a, message: ""address out of range"");
	    });
    }    
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ValidParameterName_Delegate()
    {
        const string SourceCode = @"
class TestAttribute
{
    void Test(string test)
    {
        _ = new System.Action<int>(delegate (int a)
        {
		    if (a < 0)
                throw new System.ArgumentOutOfRangeException(paramName: nameof(a), a, message: ""address out of range"");
	    });
    }    
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InvalidParameterName_StaticDelegate()
    {
        const string SourceCode = @"
class TestAttribute
{
    void Test(string test)
    {
        _ = new System.Action<int>(static delegate (int a)
        {
            throw new System.ArgumentOutOfRangeException(paramName: [|""test""|], a, message: ""address out of range"");
	    });
    }    
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoCtorWithParameterName()
    {
        const string SourceCode = """"
            using System;

            void Sample1(string str)
            {
                throw new CustomArgumentException("Message");
            }

            void Sample2(string str)
            {
                throw new CustomArgumentException("Message", new InvalidOperationException());
            }

            public class CustomArgumentException : ArgumentException
            {
                public CustomArgumentException(string message)
                    : base(message)
                {
                }

                public CustomArgumentException(string message, Exception cause)
                    : base(message, cause)
                {
                }

                public CustomArgumentException(string message, string description, Exception cause)
                    : base(message, cause)
                {
                }
             }
            """";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
              .ValidateAsync();
    }

#if CSHARP12_OR_GREATER
    [Fact]
    public async Task PrimaryConstructor()
    {
        const string SourceCode = """"
            using System;

            public class Sample(string id)
            {
                void A() => throw new ArgumentException("", nameof(id));
            }
            """";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task ThrowIfNull_ValidParameter_ShouldNotReportError()
    {
        var sourceCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNull(test);
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ThrowIfNull_InvalidParameter_ShouldReportError()
    {
        var sourceCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNull([|Name|]);
                }

                public static string Name { get; }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldReportDiagnosticWithMessage("'Name' is not a valid parameter name")
              .ValidateAsync();
    }

    [Fact]
    public async Task ThrowIfNullOrEmpty_ValidParameter_ShouldNotReportError()
    {
        var sourceCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNullOrEmpty(test);
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ThrowIfNullOrEmpty_InvalidParameter_ShouldReportError()
    {
        var sourceCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNullOrEmpty([|Name|]);
                }

                public static string Name { get; }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldReportDiagnosticWithMessage("'Name' is not a valid parameter name")
              .ValidateAsync();
    }

    [Fact]
    public async Task ThrowIfNullOrWhiteSpace_ValidParameter_ShouldNotReportError()
    {
        var sourceCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNullOrWhiteSpace(test);
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ThrowIfNullOrWhiteSpace_InvalidParameter_ShouldReportError()
    {
        var sourceCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNullOrWhiteSpace([|Name|]);
                }

                public static string Name { get; }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldReportDiagnosticWithMessage("'Name' is not a valid parameter name")
              .ValidateAsync();
    }

    [Fact]
    public async Task ArgumentException_ThrowIfNullOrEmpty_ValidParameter_ShouldNotReportError()
    {
        var sourceCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentException.ThrowIfNullOrEmpty(test);
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ArgumentException_ThrowIfNullOrEmpty_InvalidParameter_ShouldReportError()
    {
        var sourceCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentException.ThrowIfNullOrEmpty([|Name|]);
                }

                public static string Name { get; }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldReportDiagnosticWithMessage("'Name' is not a valid parameter name")
              .ValidateAsync();
    }

    [Fact]
    public async Task ArgumentException_ThrowIfNullOrWhiteSpace_ValidParameter_ShouldNotReportError()
    {
        var sourceCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(test);
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ArgumentException_ThrowIfNullOrWhiteSpace_InvalidParameter_ShouldReportError()
    {
        var sourceCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace([|Name|]);
                }

                public static string Name { get; }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldReportDiagnosticWithMessage("'Name' is not a valid parameter name")
              .ValidateAsync();
    }
}
