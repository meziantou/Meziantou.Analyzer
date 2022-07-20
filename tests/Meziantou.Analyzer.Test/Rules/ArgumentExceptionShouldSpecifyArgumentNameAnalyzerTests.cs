using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ArgumentExceptionShouldSpecifyArgumentNameAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<ArgumentExceptionShouldSpecifyArgumentNameAnalyzer>(id: "MA0015");
    }

    [Fact]
    public async Task ArgumentNameIsSpecified_ShouldNotReportError()
    {
        var sourceCode = @"
class Sample
{
    string Prop
    {
        get { throw null; }
        set { throw new System.ArgumentNullException(nameof(value)); }
    }

    string this[int index]
    {
        get { throw new System.ArgumentNullException(nameof(index)); }
        set { throw new System.ArgumentNullException(nameof(index)); }
    }

    Sample(string test)
    {
        throw new System.Exception();
        throw new System.ArgumentException(""message"", nameof(test));
        throw new System.ArgumentNullException(nameof(test));
    }

    void Test(string test)
    {
        throw new System.Exception();
        throw new System.ArgumentException(""message"", nameof(test));
        throw new System.ArgumentNullException(nameof(test));
        throw new System.ComponentModel.InvalidEnumArgumentException(nameof(test), 0, typeof(System.Enum));

        void LocalFunction(string a)
        {
            throw new System.ArgumentNullException(nameof(a));
        }
    }

    public static Sample operator +(Sample first, Sample second)
    {
        throw new System.ArgumentNullException(nameof(first));
        throw new System.ArgumentNullException(nameof(second));
    }
}";

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
}
