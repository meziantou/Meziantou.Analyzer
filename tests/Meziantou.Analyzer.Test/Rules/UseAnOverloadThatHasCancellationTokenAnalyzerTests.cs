using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using ArgumentFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseAnOverloadThatHasCancellationTokenAnalyzer,
    Meziantou.Analyzer.Rules.UseAnOverloadThatHasCancellationTokenFixer_Argument>;
using AwaitForEachFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseAnOverloadThatHasCancellationTokenAnalyzer,
    Meziantou.Analyzer.Rules.UseAnOverloadThatHasCancellationTokenFixer_AwaitForEach>;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.UseAnOverloadThatHasCancellationTokenAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseAnOverloadThatHasCancellationTokenAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    private static ArgumentFixTest CreateArgumentFixTest() => new();

    private static AwaitForEachFixTest CreateAwaitForEachFixTest() => new();


    [Fact]
    public Task CallingMethodWithoutCancellationToken_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A()
                {
                    {|MA0032:MethodWithCancellationToken()|};
                }

                public void MethodWithCancellationToken() => throw null;
                public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallingMethodWithDefaultValueWithoutCancellationToken_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A()
                {
                    {|MA0032:MethodWithCancellationToken()|};
                }

                public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken = default) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallingMethodWithCancellationToken_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A()
                {
                    MethodWithCancellationToken(default);
                }

                public void MethodWithCancellationToken() => throw null;
                public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallingMethodWithATaskInContext_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A(System.Threading.Tasks.Task task)
                {
                    {|MA0032:MethodWithCancellationToken()|};
                }

                public void MethodWithCancellationToken() => throw null;
                public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallingMethodWithATaskOfTInContext_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A(System.Threading.Tasks.Task<int> task)
                {
                    {|MA0032:MethodWithCancellationToken()|};
                }

                public void MethodWithCancellationToken() => throw null;
                public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallingMethodWithCancellationToken_ShouldReportDiagnosticWithParameterName()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A(System.Threading.CancellationToken cancellationToken)
                {
                    {|#0:MethodWithCancellationToken()|};
                }

                public void MethodWithCancellationToken() => throw null;
                public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: cancellationToken"));

        return test.RunAsync();
    }

    [Fact]
    public Task CallingMethodWithClassThatContainsAPropertyOfTypeCancellationToken_ShouldReportDiagnosticWithParameterName()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public static void A(HttpRequest request)
                {
                    {|#0:MethodWithCancellationToken()|};
                }

                public static string Value { get; }
                public static void MethodWithCancellationToken() => throw null;
                public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
            }

            class HttpRequest
            {
                public System.Threading.CancellationToken RequestAborted { get; }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: request.RequestAborted"));

        return test.RunAsync();
    }

    [Fact]
    public Task CallingMethodWithStructThatContainsAPropertyOfTypeCancellationToken_ShouldReportDiagnosticWithParameterName()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public static void A(HttpRequest request)
                {
                    {|#0:MethodWithCancellationToken()|};
                }

                public static string Value { get; }
                public static void MethodWithCancellationToken() => throw null;
                public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
            }

            struct HttpRequest
            {
                public System.Threading.CancellationToken RequestAborted { get; }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: request.RequestAborted"));

        return test.RunAsync();
    }

    [Fact]
    public Task CallingMethodWithRecordPropsThatContainsAPropertyOfTypeCancellationToken_ShouldReportDiagnosticWithParameterName()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public static void A(HttpRequest request)
                {
                    {|#0:MethodWithCancellationToken()|};
                }

                public static string Value { get; }
                public static void MethodWithCancellationToken() => throw null;
                public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
            }

            record HttpRequest
            {
                public System.Threading.CancellationToken RequestAborted { get; }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: request.RequestAborted"));

        return test.RunAsync();
    }

    [Fact]
    public Task CallingMethodWithRecordCtorThatContainsAPropertyOfTypeCancellationToken_ShouldReportDiagnosticWithParameterName()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public static void A(HttpRequest request)
                {
                    {|#0:MethodWithCancellationToken()|};
                }

                public static string Value { get; }
                public static void MethodWithCancellationToken() => throw null;
                public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
            }

            record HttpRequest(System.Threading.CancellationToken RequestAborted);
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: request.RequestAborted"));

        return test.RunAsync();
    }

    [Fact]
    public Task CallingMethodWithStructRecordCtorThatContainsAPropertyOfTypeCancellationToken_ShouldReportDiagnosticWithParameterName()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public static void A(HttpRequest request)
                {
                    {|#0:MethodWithCancellationToken()|};
                }

                public static string Value { get; }
                public static void MethodWithCancellationToken() => throw null;
                public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
            }

            record struct HttpRequest(System.Threading.CancellationToken RequestAborted);
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: request.RequestAborted"));

        return test.RunAsync();
    }

    [Fact]
    public Task CallingMethodWithStructRecordPropsThatContainsAPropertyOfTypeCancellationToken_ShouldReportDiagnosticWithParameterName()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public static void A(HttpRequest request)
                {
                    {|#0:MethodWithCancellationToken()|};
                }

                public static string Value { get; }
                public static void MethodWithCancellationToken() => throw null;
                public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
            }

            record struct HttpRequest
            {
                public System.Threading.CancellationToken RequestAborted { get; }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: request.RequestAborted"));

        return test.RunAsync();
    }

    [Fact]
    public Task CallingMethodWithProperty_ShouldReportDiagnostic()
    {
        var test = CreateArgumentFixTest();
        test.TestCode = """
            class Test : ControllerBase
            {
                public void A()
                {
                    {|#0:MethodWithCancellationToken()|};
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
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: MyCancellationToken, Context.RequestAborted"));
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck;
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.FixedCode = """
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
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallingMethodWithProperty_ShouldReportDiagnostic2()
    {
        var test = CreateArgumentFixTest();
        test.TestCode = """
            class Test : ControllerBase
            {
                public void A()
                {
                    {|#0:MethodWithCancellationToken()|};
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
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: MyCancellationToken, Context.RequestAborted"));
        test.CodeActionIndex = 1;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck;
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.FixedCode = """
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
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallingMethodWithInstanceProperty_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public static void A()
                {
                    {|#0:MethodWithCancellationToken()|};
                }

                public static System.Threading.CancellationToken MyCancellationToken { get; }
                public HttpContext Context { get; }

                public static void MethodWithCancellationToken() => throw null;
                public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
            }

            class HttpContext
            {
                public System.Threading.CancellationToken RequestAborted { get; }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: MyCancellationToken"));

        return test.RunAsync();
    }

    [Fact]
    public Task CallingMethod_ShouldReportDiagnosticWithVariables()
    {
        var test = CreateArgumentFixTest();
        test.TestCode = """
            class Test
            {
                public static void A()
                {
                    {
                        System.Threading.CancellationToken unaccessible1 = default;
                    }

                    System.Threading.CancellationToken a = default;
                    {|#0:MethodWithCancellationToken()|};
                    System.Threading.CancellationToken unaccessible2 = default;
                }

                public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken = default) => throw null;
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: a"));
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallingMethod_ShouldReportDiagnosticWithVariables_OptionalParameter()
    {
        var test = CreateArgumentFixTest();
        test.TestCode = """
            class Test
            {
                public static void A()
                {
                    System.Threading.CancellationToken a = default;
                    {|#0:MethodWithCancellationToken()|};
                }

                public static void MethodWithCancellationToken(int a = 0, System.Threading.CancellationToken cancellationToken = default) => throw null;
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: a"));
        test.FixedCode = """
            class Test
            {
                public static void A()
                {
                    System.Threading.CancellationToken a = default;
                    MethodWithCancellationToken(cancellationToken: a);
                }

                public static void MethodWithCancellationToken(int a = 0, System.Threading.CancellationToken cancellationToken = default) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Record_ShouldReportDiagnosticWithProperty()
    {
        var test = CreateTest();
        test.TestCode = """
            record Test
            {
                public System.Threading.CancellationToken a;

                public void A()
                {
                    {|#0:MethodWithCancellationToken()|};
                }

                public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken = default) => throw null;
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: a"));

        return test.RunAsync();
    }

    [Fact]
    public Task RecordCtor_ShouldReportDiagnosticWithProperty()
    {
        var test = CreateArgumentFixTest();
        test.TestCode = """
            record Test(System.Threading.CancellationToken CancellationToken)
            {
                public void A()
                {
                    {|#0:MethodWithCancellationToken()|};
                }

                public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken = default) => throw null;
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: CancellationToken"));
        test.FixedCode = """
            record Test(System.Threading.CancellationToken CancellationToken)
            {
                public void A()
                {
                    MethodWithCancellationToken(CancellationToken);
                }

                public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken = default) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RecordStruct_ShouldReportDiagnosticWithProperty()
    {
        var test = CreateTest();
        test.TestCode = """
            record struct Test
            {
                public System.Threading.CancellationToken a;

                public void A()
                {
                    {|#0:MethodWithCancellationToken()|};
                }

                public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken = default) => throw null;
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: a"));

        return test.RunAsync();
    }

    [Fact]
    public Task RecordStructCtor_ShouldReportDiagnosticWithProperty()
    {
        var test = CreateTest();
        test.TestCode = """
            record struct Test(System.Threading.CancellationToken a)
            {
                public void A()
                {
                    {|#0:MethodWithCancellationToken()|};
                }

                public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken = default) => throw null;
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: a"));

        return test.RunAsync();
    }

    [Fact]
    public Task InterfaceImplicit_ShouldReportDiagnosticWithProperty()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITest
            {
                public System.Threading.CancellationToken A { get; }

                void Sample()
                {
                    {|#0:MethodWithCancellationToken()|};
                }

                void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken = default) => throw null;
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: A"));

        return test.RunAsync();
    }

    [Fact]
    public Task CancellationTokenSourceCreate_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading;
            class Test
            {
                public static void A()
                {
                    {
                        _ = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OverloadWithMultipleParametersOfSameType()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public static void A()
                {
                    Sample(""); // reported here
                }

                public static void Sample(string a) { }
                public static void Sample(string a, string b) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AwaitForEach()
    {
        var test = CreateArgumentFixTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            class Test
            {
                public static async Task A()
                {
                    var ct = new CancellationToken();
                    await foreach (var item in {|MA0040:AsyncEnumerable()|})
                    {
                    }
                }

                static async IAsyncEnumerable<int> AsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
                {
                    yield return 0;
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AwaitForEach_IAsyncEnumerable()
    {
        var test = CreateAwaitForEachFixTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            class Test
            {
                public static async Task A(IAsyncEnumerable<int> enumerable)
                {
                    var ct = new CancellationToken();
                    await foreach (var item in {|MA0079:enumerable|})
                    {
                    }
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AwaitForEach_IAsyncEnumerable_WithCancellation()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AwaitForEach_IAsyncEnumerable_WithCancellationAndConfigureAwait()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AwaitForEach_NoNeedForCancellationToken()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DisposeAsync_NoNeedForCancellationToken()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExtensionMethodOnCancellationToken_NoNeedForCancellationToken()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CancellationTokenAvailableAsLambdaParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading;
            class Test
            {
                public static void A(CancellationToken cancellationToken = default)
                {
                    _ = new System.Action<CancellationToken>(static ct => {|#0:A()|});
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: ct"));

        return test.RunAsync();
    }

    [Fact]
    public Task CancellationTokenAvailableAsParentLambdaParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading;
            class Test
            {
                public static void A(CancellationToken cancellationToken = default)
                {
                    _ = new System.Action<CancellationToken>(static ct1 =>
                    {
                        _ = new System.Action<CancellationToken>(ct2 => {|#0:A()|});
                    });
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: ct1, ct2"));

        return test.RunAsync();
    }

    [Fact]
    public Task CancellationTokenAvailableAsDelegateParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading;
            class Test
            {
                public static void A(CancellationToken cancellationToken = default)
                {
                    _ = new System.Action<CancellationToken>(static delegate(CancellationToken ct) { {|#0:A()|}; });
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: ct"));

        return test.RunAsync();
    }

    [Fact]
    public Task CancellationTokenAvailableAsLocalFunctionParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading;
            class Test
            {
                public static void A(CancellationToken cancellationToken = default)
                {
                    B(cancellationToken);
                    static void B(CancellationToken ct) => {|#0:A()|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: ct"));

        return test.RunAsync();
    }

    [Fact]
    public Task CancellationTokenAvailableAsLocalFunctionParameter_DoNotUseFromOutsideStatic()
    {
        var test = CreateTest();
        test.TestCode = """
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
                            {|#0:A()|};
                        }
                    }
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: ct1, ct2"));

        return test.RunAsync();
    }

    [Fact]
    public Task CancellationTokenNotAvailableAsVariableDeclarator()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading;
            class Test
            {
                public static void A()
                {
                    CancellationToken Foo(CancellationToken cancellationToken = default) => cancellationToken;

                    var token = {|MA0032:Foo()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AwaitForeach_FixerRemovesWithCancellationToken()
    {
        var test = CreateArgumentFixTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using System.Runtime.CompilerServices;
            class Foo
            {
                public static void Test()
                {
                    CancellationToken ct = default;
                    {|MA0040:A()|}.WithCancellation(ct);

                    async IAsyncEnumerable<int> A([EnumeratorCancellation]CancellationToken cancellationToken = default)
                    {
                        yield return 0;
                    }
                }
            }
            """;
        test.FixedCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task AwaitForeach_FixerDoesNotRemoveWithCancellationToken()
    {
        var test = CreateArgumentFixTest();
        test.TestCode = """
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
                    {|MA0040:A()|}.WithCancellation(ct2);

                    async IAsyncEnumerable<int> A([EnumeratorCancellation]CancellationToken cancellationToken = default)
                    {
                        yield return 0;
                    }
                }
            }
            """;
        test.FixedCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task AwaitForeach_FixerDoesNotRemoveWithCancellationTokenWhenAttributeIsNotPresent()
    {
        var test = CreateArgumentFixTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using System.Runtime.CompilerServices;
            class Foo
            {
                public static void Test()
                {
                    CancellationToken ct = default;
                    {|MA0040:A()|}.WithCancellation(ct);

                    async IAsyncEnumerable<int> A([EnumeratorCancellation]CancellationToken cancellationToken = default)
                    {
                        yield return 0;
                    }
                }
            }
            """;
        test.FixedCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task SuggestOverloadWithOptionalParameters_AllowOptionalParameters_True()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestState.SetConfiguration("MA0032.allowOverloadsWithOptionalParameters", "true");
        test.TestCode = """
            using System.Threading;
            using System.Threading.Tasks;

            {|MA0032:Sample.Repro()|};

            class Sample
            {
                public static void Repro() => throw null;
                public static void Repro(CancellationToken cancellationToken, bool dummy = false) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SuggestOverloadWithOptionalParameters_AllowOptionalParameters_False()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Threading;
            using System.Threading.Tasks;

            Sample.Repro();

            class Sample
            {
                public static void Repro() => throw null;
                public static void Repro(CancellationToken cancellationToken, bool dummy = false) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SuggestOverloadWithExperimentalAttribute()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading;
            using System.Diagnostics.CodeAnalysis;

            class Sample
            {
                public static void Repro(CancellationToken cancellationToken)
                {
                    Method();
                }

                public static void Method()
                {
                }

                [Experimental("EXTEXP0001")]
                public static void Method(CancellationToken cancellationToken)
                {
                }
            }

            namespace System.Diagnostics.CodeAnalysis
            {
                [AttributeUsage(AttributeTargets.All, Inherited = false)]
                public sealed class ExperimentalAttribute : Attribute
                {
                    public ExperimentalAttribute(string diagnosticId)
                    {
                    }

                    public string? UrlFormat { get; set; }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Xunit2()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([new PackageIdentity("xunit.abstractions", "2.0.3")]);
        test.TestCode = """
            using System.Threading;
            using System.Threading.Tasks;

            {|MA0032:Sample.Repro()|};

            class Sample
            {
                public static void Repro() => throw null;
                public static void Repro(CancellationToken cancellationToken) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Xunit3()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([new PackageIdentity("xunit.v3.extensibility.core", "1.0.0")]);
        test.TestCode = """
            using System.Threading;
            using System.Threading.Tasks;

            {|#0:Sample.Repro()|};

            class Sample
            {
                public static void Repro() => throw null;
                public static void Repro(CancellationToken cancellationToken) => throw null;
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0040", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a CancellationToken, available tokens: Xunit.TestContext.Current.CancellationToken"));

        return test.RunAsync();
    }

    [Fact]
    public Task TopLevelStatements()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Threading;
            using System.Threading.Tasks;

            var cancellationToken = CancellationToken.None;

            class Sample
            {
                void Foo()
                {
                    {|MA0032:Repro()|};
                }

                public static void Repro() => throw null;
                public static void Repro(CancellationToken cancellationToken) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludedMethod_Attribute_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Threading;
            class Test
            {
                public void A(CancellationToken cancellationToken)
                {
                    MethodWithCancellationToken();
                }

                [Meziantou.Analyzer.Annotations.ExcludeFromCancellationTokenAnalysis]
                public void MethodWithCancellationToken() => throw null;
                public void MethodWithCancellationToken(CancellationToken cancellationToken) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludedMethod_AttributeOnTheOverloadWithCancellationToken_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Threading;
            class Test
            {
                public void A(CancellationToken cancellationToken)
                {
                    {|MA0040:MethodWithCancellationToken()|};
                }

                public void MethodWithCancellationToken() => throw null;

                [Meziantou.Analyzer.Annotations.ExcludeFromCancellationTokenAnalysis]
                public void MethodWithCancellationToken(CancellationToken cancellationToken) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludedMethod_AssemblyAttributeWithDocumentationId_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Threading;

            [assembly: Meziantou.Analyzer.Annotations.ExcludeFromCancellationTokenAnalysis("M:Test.MethodWithCancellationToken")]

            class Test
            {
                public void A(CancellationToken cancellationToken)
                {
                    MethodWithCancellationToken();
                }

                public void MethodWithCancellationToken() => throw null;
                public void MethodWithCancellationToken(CancellationToken cancellationToken) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludedMethod_AssemblyAttributeWithMemberName_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Threading;

            [assembly: Meziantou.Analyzer.Annotations.ExcludeFromCancellationTokenAnalysis(typeof(Test), "MethodWithCancellationToken")]

            class Test
            {
                public void A(CancellationToken cancellationToken)
                {
                    MethodWithCancellationToken();
                }

                public void MethodWithCancellationToken() => throw null;
                public void MethodWithCancellationToken(CancellationToken cancellationToken) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludedMethod_AssemblyAttributeWithParameterTypes_ShouldReportDiagnosticForOtherOverloads()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Threading;

            [assembly: Meziantou.Analyzer.Annotations.ExcludeFromCancellationTokenAnalysis(typeof(Test), "MethodWithCancellationToken", typeof(int))]

            class Test
            {
                public void A(CancellationToken cancellationToken)
                {
                    MethodWithCancellationToken(0);
                    {|MA0040:MethodWithCancellationToken("")|};
                }

                public void MethodWithCancellationToken(int value) => throw null;
                public void MethodWithCancellationToken(int value, CancellationToken cancellationToken) => throw null;
                public void MethodWithCancellationToken(string value) => throw null;
                public void MethodWithCancellationToken(string value, CancellationToken cancellationToken) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludedMethod_NoCancellationTokenInScope_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Threading;
            class Test
            {
                public void A()
                {
                    MethodWithCancellationToken();
                    {|MA0032:OtherMethodWithCancellationToken()|};
                }

                [Meziantou.Analyzer.Annotations.ExcludeFromCancellationTokenAnalysis]
                public void MethodWithCancellationToken() => throw null;
                public void MethodWithCancellationToken(CancellationToken cancellationToken) => throw null;

                public void OtherMethodWithCancellationToken() => throw null;
                public void OtherMethodWithCancellationToken(CancellationToken cancellationToken) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludedMethod_AwaitForEach_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            class Test
            {
                public static async Task A(CancellationToken cancellationToken)
                {
                    await foreach (var item in Enumerate())
                    {
                    }
                }

                [Meziantou.Analyzer.Annotations.ExcludeFromCancellationTokenAnalysis]
                public static IAsyncEnumerable<int> Enumerate() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludedMethod_GenericAndExtensionMethods_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Threading;
            static class Extensions
            {
                [Meziantou.Analyzer.Annotations.ExcludeFromCancellationTokenAnalysis]
                public static void Extension<T>(this T value) => throw null;
                public static void Extension<T>(this T value, CancellationToken cancellationToken) => throw null;
            }

            class Test
            {
                public void A(CancellationToken cancellationToken)
                {
                    this.Extension();
                    Extensions.Extension(this);
                    Generic<int>();
                }

                [Meziantou.Analyzer.Annotations.ExcludeFromCancellationTokenAnalysis]
                public void Generic<T>() => throw null;
                public void Generic<T>(CancellationToken cancellationToken) => throw null;
            }
            """;

        return test.RunAsync();
    }
}
