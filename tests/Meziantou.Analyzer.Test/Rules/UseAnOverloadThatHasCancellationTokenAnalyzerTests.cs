using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseAnOverloadThatHasCancellationTokenAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseAnOverloadThatHasCancellationTokenAnalyzer>();
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
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: cancellationToken")
              .ValidateAsync();
    }

    [Fact]
    public async Task CallingMethodWithClassThatContainsAPropertyOfTypeCancellationToken_ShouldReportDiagnosticWithParameterName()
    {
        const string SourceCode = @"
class Test
{
    public static void A(HttpRequest request)
    {
        [||]MethodWithCancellationToken();
    }

    public static string Value { get; }
    public static void MethodWithCancellationToken() => throw null;
    public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}

class HttpRequest
{
    public System.Threading.CancellationToken RequestAborted { get; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: request.RequestAborted")
              .ValidateAsync();
    }

    [Fact]
    public async Task CallingMethodWithStructThatContainsAPropertyOfTypeCancellationToken_ShouldReportDiagnosticWithParameterName()
    {
        const string SourceCode = @"
class Test
{
    public static void A(HttpRequest request)
    {
        [||]MethodWithCancellationToken();
    }

    public static string Value { get; }
    public static void MethodWithCancellationToken() => throw null;
    public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}

struct HttpRequest
{
    public System.Threading.CancellationToken RequestAborted { get; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: request.RequestAborted")
              .ValidateAsync();
    }

    [Fact]
    public async Task CallingMethodWithRecordPropsThatContainsAPropertyOfTypeCancellationToken_ShouldReportDiagnosticWithParameterName()
    {
        const string SourceCode = @"
class Test
{
    public static void A(HttpRequest request)
    {
        [||]MethodWithCancellationToken();
    }

    public static string Value { get; }
    public static void MethodWithCancellationToken() => throw null;
    public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}

record HttpRequest
{
    public System.Threading.CancellationToken RequestAborted { get; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithTargetFramework(TargetFramework.Net6_0)
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: request.RequestAborted")
              .ValidateAsync();
    }

    [Fact]
    public async Task CallingMethodWithRecordCtorThatContainsAPropertyOfTypeCancellationToken_ShouldReportDiagnosticWithParameterName()
    {
        const string SourceCode = @"
class Test
{
    public static void A(HttpRequest request)
    {
        [||]MethodWithCancellationToken();
    }

    public static string Value { get; }
    public static void MethodWithCancellationToken() => throw null;
    public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}

record HttpRequest(System.Threading.CancellationToken RequestAborted);
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithTargetFramework(TargetFramework.Net6_0)
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: request.RequestAborted")
              .ValidateAsync();
    }

#if CSHARP10_OR_GREATER
    [Fact]
    public async Task CallingMethodWithStructRecordCtorThatContainsAPropertyOfTypeCancellationToken_ShouldReportDiagnosticWithParameterName()
    {
        const string SourceCode = @"
class Test
{
    public static void A(HttpRequest request)
    {
        [||]MethodWithCancellationToken();
    }

    public static string Value { get; }
    public static void MethodWithCancellationToken() => throw null;
    public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}

record struct HttpRequest(System.Threading.CancellationToken RequestAborted);
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithTargetFramework(TargetFramework.Net6_0)
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: request.RequestAborted")
              .ValidateAsync();
    }
#endif

#if CSHARP10_OR_GREATER
    [Fact]
    public async Task CallingMethodWithStructRecordPropsThatContainsAPropertyOfTypeCancellationToken_ShouldReportDiagnosticWithParameterName()
    {
        const string SourceCode = @"
class Test
{
    public static void A(HttpRequest request)
    {
        [||]MethodWithCancellationToken();
    }

    public static string Value { get; }
    public static void MethodWithCancellationToken() => throw null;
    public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}

record struct HttpRequest
{
    public System.Threading.CancellationToken RequestAborted { get; }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithTargetFramework(TargetFramework.Net6_0)
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: request.RequestAborted")
              .ValidateAsync();
    }
#endif

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

        const string Fix = @"
class Test : ControllerBase
{
    public void A()
    {
        MethodWithCancellationToken(MyCancellationToken);
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
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: MyCancellationToken, Context.RequestAborted")
              .WithCodeFixProvider<UseAnOverloadThatHasCancellationTokenFixer_Argument>()
              .ShouldFixCodeWith(0, Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task CallingMethodWithProperty_ShouldReportDiagnostic2()
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

        const string Fix = @"
class Test : ControllerBase
{
    public void A()
    {
        MethodWithCancellationToken(Context.RequestAborted);
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
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: MyCancellationToken, Context.RequestAborted")
              .WithCodeFixProvider<UseAnOverloadThatHasCancellationTokenFixer_Argument>()
              .ShouldFixCodeWith(1, Fix)
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
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: MyCancellationToken")
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
        const string Fix = @"
class Test
{
    public static void A()
    {
        {
            System.Threading.CancellationToken unaccessible1 = default;
        }

        System.Threading.CancellationToken a = default;
        MethodWithCancellationToken(a);
        System.Threading.CancellationToken unaccessible2 = default;
    }

    public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken = default) => throw null;
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: a")
              .WithCodeFixProvider<UseAnOverloadThatHasCancellationTokenFixer_Argument>()
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task CallingMethod_ShouldReportDiagnosticWithVariables_OptionalParameter()
    {
        const string SourceCode = @"
class Test
{
    public static void A()
    {
        System.Threading.CancellationToken a = default;
        [||]MethodWithCancellationToken();
    }

    public static void MethodWithCancellationToken(int a = 0, System.Threading.CancellationToken cancellationToken = default) => throw null;
}
";
        const string Fix = @"
class Test
{
    public static void A()
    {
        System.Threading.CancellationToken a = default;
        MethodWithCancellationToken(cancellationToken: a);
    }

    public static void MethodWithCancellationToken(int a = 0, System.Threading.CancellationToken cancellationToken = default) => throw null;
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: a")
              .WithCodeFixProvider<UseAnOverloadThatHasCancellationTokenFixer_Argument>()
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task Record_ShouldReportDiagnosticWithProperty()
    {
        const string SourceCode = @"
record Test
{
    public System.Threading.CancellationToken a;

    public void A()
    {
        [||]MethodWithCancellationToken();
    }

    public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken = default) => throw null;
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: a")
              .ValidateAsync();
    }

    [Fact]
    public async Task RecordCtor_ShouldReportDiagnosticWithProperty()
    {
        const string SourceCode = @"
record Test(System.Threading.CancellationToken CancellationToken)
{
    public void A()
    {
        [||]MethodWithCancellationToken();
    }

    public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken = default) => throw null;
}
";
        const string Fix = @"
record Test(System.Threading.CancellationToken CancellationToken)
{
    public void A()
    {
        MethodWithCancellationToken(CancellationToken);
    }

    public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken = default) => throw null;
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithTargetFramework(TargetFramework.Net6_0)
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: CancellationToken")
              .WithCodeFixProvider<UseAnOverloadThatHasCancellationTokenFixer_Argument>()
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

#if CSHARP10_OR_GREATER
    [Fact]
    public async Task RecordStruct_ShouldReportDiagnosticWithProperty()
    {
        const string SourceCode = @"
record struct Test
{
    public System.Threading.CancellationToken a;

    public void A()
    {
        [||]MethodWithCancellationToken();
    }

    public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken = default) => throw null;
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: a")
              .ValidateAsync();
    }
#endif


#if CSHARP10_OR_GREATER
    [Fact]
    public async Task RecordStructCtor_ShouldReportDiagnosticWithProperty()
    {
        const string SourceCode = @"
record struct Test(System.Threading.CancellationToken a)
{
    public void A()
    {
        [||]MethodWithCancellationToken();
    }

    public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken = default) => throw null;
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: a")
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task InterfaceImplicit_ShouldReportDiagnosticWithProperty()
    {
        const string SourceCode = @"
interface ITest
{
    public System.Threading.CancellationToken A { get; }

    void Sample()
    {
        [||]MethodWithCancellationToken();
    }

    void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken = default) => throw null;
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithTargetFramework(TargetFramework.Net6_0)
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: A")
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

        const string Fix = @"
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
class Test
{
    public static async Task A()
    {
        var ct = new CancellationToken();
        await foreach (var item in AsyncEnumerable(ct))
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
              .WithCodeFixProvider<UseAnOverloadThatHasCancellationTokenFixer_Argument>()
              .ShouldFixCodeWith(Fix)
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

        const string Fix = @"
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
              .WithCodeFixProvider<UseAnOverloadThatHasCancellationTokenFixer_AwaitForEach>()
              .ShouldFixCodeWith(Fix)
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

    [Fact]
    public async Task AwaitForEach_IAsyncEnumerable_WithCancellationAndConfigureAwait()
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
        await foreach (var item in enumerable.WithCancellation(ct).ConfigureAwait(false))
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
    public async Task AwaitForEach_NoNeedForCancellationToken()
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
        await foreach (var item in AsyncEnumerable(ct).ConfigureAwait(false))
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
    public async Task DisposeAsync_NoNeedForCancellationToken()
    {
        const string SourceCode = @"
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class Test : System.IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        A();
        return default;
    }

    static void A(CancellationToken cancellationToken = default)
    {
    }
}
";

        await CreateProjectBuilder()
              .AddAsyncInterfaceApi()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ExtensionMethodOnCancellationToken_NoNeedForCancellationToken()
    {
        const string SourceCode = @"
using System.Threading;
using System.Threading.Tasks;

static class Test
{
    public static void WaitAsync(this CancellationToken cancellationToken)
    {
    }

    private static void A()
    {
        CancellationToken cancellationToken = default;
        cancellationToken.WaitAsync();
    }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CancellationTokenAvailableAsLambdaParameter()
    {
        const string SourceCode = @"
using System.Threading;
class Test
{
    public static void A(CancellationToken cancellationToken = default)
    {
        _ = new System.Action<CancellationToken>(static ct => [||]A());
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: ct")
              .ValidateAsync();
    }

    [Fact]
    public async Task CancellationTokenAvailableAsParentLambdaParameter()
    {
        const string SourceCode = @"
using System.Threading;
class Test
{
    public static void A(CancellationToken cancellationToken = default)
    {
        _ = new System.Action<CancellationToken>(static ct1 =>
        {
            _ = new System.Action<CancellationToken>(ct2 => [||]A());
        });
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: ct1, ct2")
              .ValidateAsync();
    }

    [Fact]
    public async Task CancellationTokenAvailableAsDelegateParameter()
    {
        const string SourceCode = @"
using System.Threading;
class Test
{
    public static void A(CancellationToken cancellationToken = default)
    {
        _ = new System.Action<CancellationToken>(static delegate(CancellationToken ct) { [||]A(); });
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: ct")
              .ValidateAsync();
    }

    [Fact]
    public async Task CancellationTokenAvailableAsLocalFunctionParameter()
    {
        const string SourceCode = @"
using System.Threading;
class Test
{
    public static void A(CancellationToken cancellationToken = default)
    {
        B(cancellationToken);
        static void B(CancellationToken ct) => [||]A();
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: ct")
              .ValidateAsync();
    }

    [Fact]
    public async Task CancellationTokenAvailableAsLocalFunctionParameter_DoNotUseFromOutsideStatic()
    {
        const string SourceCode = @"
using System.Threading;
class Test
{
    public static void A(CancellationToken cancellationToken = default)
    {
        B(cancellationToken);
        static void B(CancellationToken ct1)
        {
            CancellationToken ct2 = default;
            void C()
            {
                [||]A();
            }
        }            
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload with a CancellationToken, available tokens: ct1, ct2")
              .ValidateAsync();
    }

    [Fact]
    public async Task CancellationTokenNotAvailableAsVariableDeclarator()
    {
        const string SourceCode = @"
using System.Threading;
class Test
{
    public static void A()
    {
        CancellationToken Foo(CancellationToken cancellationToken = default) => cancellationToken;

        var token = [||]Foo();
    }
}";
        await CreateProjectBuilder()
              .WithDefaultAnalyzerId("MA0032")
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AwaitForeach_FixerRemovesWithCancellationToken()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using System.Runtime.CompilerServices;
            class Foo
            {
                public static void Test()
                {
                    CancellationToken ct = default;
                    [||]A().WithCancellation(ct);

                    async IAsyncEnumerable<int> A([EnumeratorCancellation]CancellationToken cancellationToken = default)
                    {
                        yield return 0;
                    }
                }
            }
            """;
        const string Fix = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using System.Runtime.CompilerServices;
            class Foo
            {
                public static void Test()
                {
                    CancellationToken ct = default;
                    A(ct);

                    async IAsyncEnumerable<int> A([EnumeratorCancellation]CancellationToken cancellationToken = default)
                    {
                        yield return 0;
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithSourceCode(SourceCode)
              .WithCodeFixProvider<UseAnOverloadThatHasCancellationTokenFixer_Argument>()
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task AwaitForeach_FixerDoesNotRemoveWithCancellationToken()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using System.Runtime.CompilerServices;
            class Foo
            {
                public static void Test()
                {
                    CancellationToken ct1 = default;
                    CancellationToken ct2 = default;
                    [||]A().WithCancellation(ct2);

                    async IAsyncEnumerable<int> A([EnumeratorCancellation]CancellationToken cancellationToken = default)
                    {
                        yield return 0;
                    }
                }
            }
            """;
        const string Fix = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using System.Runtime.CompilerServices;
            class Foo
            {
                public static void Test()
                {
                    CancellationToken ct1 = default;
                    CancellationToken ct2 = default;
                    A(ct1).WithCancellation(ct2);

                    async IAsyncEnumerable<int> A([EnumeratorCancellation]CancellationToken cancellationToken = default)
                    {
                        yield return 0;
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithSourceCode(SourceCode)
              .WithCodeFixProvider<UseAnOverloadThatHasCancellationTokenFixer_Argument>()
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task AwaitForeach_FixerDoesNotRemoveWithCancellationTokenWhenAttributeIsNotPresent()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using System.Runtime.CompilerServices;
            class Foo
            {
                public static void Test()
                {
                    CancellationToken ct = default;
                    [||]A().WithCancellation(ct);

                    async IAsyncEnumerable<int> A([EnumeratorCancellation]CancellationToken cancellationToken = default)
                    {
                        yield return 0;
                    }
                }
            }
            """;
        const string Fix = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using System.Runtime.CompilerServices;
            class Foo
            {
                public static void Test()
                {
                    CancellationToken ct = default;
                    A(ct);

                    async IAsyncEnumerable<int> A([EnumeratorCancellation]CancellationToken cancellationToken = default)
                    {
                        yield return 0;
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithSourceCode(SourceCode)
              .WithCodeFixProvider<UseAnOverloadThatHasCancellationTokenFixer_Argument>()
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }
}
