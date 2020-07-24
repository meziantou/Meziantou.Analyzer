using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class UseAnOverloadThatHaveCancellationTokenAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<UseAnOverloadThatHaveCancellationTokenAnalyzer>();
        }

        [Fact]
        public async Task CallingMethodWithoutCancellationToken_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class Test
{
    public void A()
    {
        [||]MethodWithCancellationToken();
    }

    public void MethodWithCancellationToken() => throw null;
    public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CallingMethodWithDefaultValueWithoutCancellationToken_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class Test
{
    public void A()
    {
        [||]MethodWithCancellationToken();
    }

    public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken = default) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CallingMethodWithCancellationToken_ShouldNotReportDiagnostic()
        {
            const string SourceCode = @"
class Test
{
    public void A()
    {
        MethodWithCancellationToken(default);
    }

    public void MethodWithCancellationToken() => throw null;
    public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CallingMethodWithATaskInContext_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class Test
{
    public void A(System.Threading.Tasks.Task task)
    {
        [||]MethodWithCancellationToken();
    }

    public void MethodWithCancellationToken() => throw null;
    public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}";

            // Should not report MA0040 with task.Factory.CancellationToken
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CallingMethodWithATaskOfTInContext_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class Test
{
    public void A(System.Threading.Tasks.Task<int> task)
    {
        [||]MethodWithCancellationToken();
    }

    public void MethodWithCancellationToken() => throw null;
    public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CallingMethodWithCancellationToken_ShouldReportDiagnosticWithParameterName()
        {
            const string SourceCode = @"
class Test
{
    public void A(System.Threading.CancellationToken cancellationToken)
    {
        [||]MethodWithCancellationToken();
    }

    public void MethodWithCancellationToken() => throw null;
    public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnosticWithMessage("Specify a CancellationToken. Available tokens: cancellationToken")
                  .ValidateAsync();
        }

        [Fact]
        public async Task CallingMethodWithObjectThatContainsAPropertyOfTypeCancellationToken_ShouldReportDiagnosticWithParameterName()
        {
            const string SourceCode = @"
class Test
{
    public static void A(HttpRequest request)
    {
        [||]MethodWithCancellationToken();
    }

    public static void MethodWithCancellationToken() => throw null;
    public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}

class HttpRequest
{
    public System.Threading.CancellationToken RequestAborted { get; }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnosticWithMessage("Specify a CancellationToken. Available tokens: request.RequestAborted")
                  .ValidateAsync();
        }

        [Fact]
        public async Task CallingMethodWithProperty_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class Test : ControllerBase
{
    public void A()
    {
        [||]MethodWithCancellationToken();
    }

    public System.Threading.CancellationToken MyCancellationToken { get; }

    public void MethodWithCancellationToken() => throw null;
    public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}

class ControllerBase
{
    public HttpContext Context { get; }
}

class HttpContext
{
    public System.Threading.CancellationToken RequestAborted { get; }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnosticWithMessage("Specify a CancellationToken. Available tokens: MyCancellationToken, Context.RequestAborted")
                  .ValidateAsync();
        }

        [Fact]
        public async Task CallingMethodWithInstanceProperty_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class Test
{
    public static void A()
    {
        [||]MethodWithCancellationToken();
    }

    public static System.Threading.CancellationToken MyCancellationToken { get; }
    public HttpContext Context { get; }

    public static void MethodWithCancellationToken() => throw null;
    public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}

class HttpContext
{
    public System.Threading.CancellationToken RequestAborted { get; }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnosticWithMessage("Specify a CancellationToken. Available tokens: MyCancellationToken")
                  .ValidateAsync();
        }

        [Fact]
        public async Task CallingMethod_ShouldReportDiagnosticWithVariables()
        {
            const string SourceCode = @"
class Test
{
    public static void A()
    {
        {
            System.Threading.CancellationToken unaccessible1 = default;
        }

        System.Threading.CancellationToken a = default;
        [||]MethodWithCancellationToken();
        System.Threading.CancellationToken unaccessible2 = default;
    }

    public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken = default) => throw null;
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnosticWithMessage("Specify a CancellationToken. Available tokens: a")
                  .ValidateAsync();
        }

        [Fact]
        public async Task CancellationTokenSourceCreate_ShouldNotReportDiagnostic()
        {
            const string SourceCode = @"using System.Threading;
class Test
{
    public static void A()
    {
        {
            _ = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
        }
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task OverloadWithMultipleParametersOfSameType()
        {
            const string SourceCode = @"
class Test
{
    public static void A()
    {
        Sample(""""); // reported here
    }

    public static void Sample(string a) { }
    public static void Sample(string a, string b) { }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task AwaitForEach()
        {
            const string SourceCode = @"
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
class Test
{
    public static async Task A()
    {
        var ct = new CancellationToken();
        await foreach (var item in [|AsyncEnumerable()|])
        {
        }
    }

    static async IAsyncEnumerable<int> AsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return 0;
    }
}
";

            await CreateProjectBuilder()
                  .AddAsyncInterfaceApi()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task AwaitForEach_IAsyncEnumerable()
        {
            const string SourceCode = @"
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
class Test
{
    public static async Task A(IAsyncEnumerable<int> enumerable)
    {
        var ct = new CancellationToken();
        await foreach (var item in [|enumerable|])
        {
        }
    }
}
";

            await CreateProjectBuilder()
                  .AddAsyncInterfaceApi()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task AwaitForEach_IAsyncEnumerable_WithCancellation()
        {
            const string SourceCode = @"
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
class Test
{
    public static async Task A(IAsyncEnumerable<int> enumerable)
    {
        var ct = new CancellationToken();
        await foreach (var item in enumerable.WithCancellation(ct))
        {
        }
    }
}
";

            await CreateProjectBuilder()
                  .AddAsyncInterfaceApi()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
