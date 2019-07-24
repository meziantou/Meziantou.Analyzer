using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class UseAnOverloadThatHaveCancellationTokenAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<UseAnOverloadThatHaveCancellationTokenAnalyzer>();
        }

        [TestMethod]
        public async Task CallingMethodWithoutCancellationToken_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class Test
{
    public void A()
    {
        [|]MethodWithCancellationToken();
    }

    public void MethodWithCancellationToken() => throw null;
    public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task CallingMethodWithDefaultValueWithoutCancellationToken_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class Test
{
    public void A()
    {
        [|]MethodWithCancellationToken();
    }

    public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken = default) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task CallingMethodWithCancellationToken_ShouldNotReportDiagnosticAsync()
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

        [TestMethod]
        public async Task CallingMethodWithATaskInContext_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class Test
{
    public void A(System.Threading.Tasks.Task task)
    {
        [|]MethodWithCancellationToken();
    }

    public void MethodWithCancellationToken() => throw null;
    public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}";

            // Should not report MA0040 with task.Factory.CancellationToken
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task CallingMethodWithATaskOfTInContext_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class Test
{
    public void A(System.Threading.Tasks.Task<int> task)
    {
        [|]MethodWithCancellationToken();
    }

    public void MethodWithCancellationToken() => throw null;
    public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task CallingMethodWithCancellationToken_ShouldReportDiagnosticWithParameterNameAsync()
        {
            const string SourceCode = @"
class Test
{
    public void A(System.Threading.CancellationToken cancellationToken)
    {
        [|]MethodWithCancellationToken();
    }

    public void MethodWithCancellationToken() => throw null;
    public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnosticWithMessage("Specify a CancellationToken (cancellationToken)")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task CallingMethodWithObjectThatContainsAPropertyOfTypeCancellationToken_ShouldReportDiagnosticWithParameterNameAsync()
        {
            const string SourceCode = @"
class Test
{
    public static void A(HttpRequest request)
    {
        [|]MethodWithCancellationToken();
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
                  .ShouldReportDiagnosticWithMessage("Specify a CancellationToken (request.RequestAborted)")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task CallingMethodWithProperty_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class Test
{
    public void A()
    {
        [|]MethodWithCancellationToken();
    }

    public System.Threading.CancellationToken MyCancellationToken { get; }
    public HttpContext Context { get; }

    public void MethodWithCancellationToken() => throw null;
    public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}

class HttpContext
{
    public System.Threading.CancellationToken RequestAborted { get; }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnosticWithMessage("Specify a CancellationToken (MyCancellationToken, Context.RequestAborted)")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task CallingMethodWithInstanceProperty_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class Test
{
    public static void A()
    {
        [|]MethodWithCancellationToken();
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
                  .ShouldReportDiagnosticWithMessage("Specify a CancellationToken (MyCancellationToken)")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task CallingMethod_ShouldReportDiagnosticWithVariablesAsync()
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
        [|]MethodWithCancellationToken();
        System.Threading.CancellationToken unaccessible2 = default;
    }

    public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken = default) => throw null;
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnosticWithMessage("Specify a CancellationToken (a)")
                  .ValidateAsync();
        }

        [TestMethod]
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

        [TestMethod]
        [TestProperty("WI", "https://github.com/meziantou/Meziantou.Analyzer/issues/67")]
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
    }
}
