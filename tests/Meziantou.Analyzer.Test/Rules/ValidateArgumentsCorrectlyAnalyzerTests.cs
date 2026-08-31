using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.ValidateArgumentsCorrectlyAnalyzer,
    Meziantou.Analyzer.Rules.ValidateArgumentsCorrectlyFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ValidateArgumentsCorrectlyAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task ReturnVoid()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            class TypeName
            {
                void A()
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReturnString()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            class TypeName
            {
                string A()
                {
                    throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OutParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            class TypeName
            {
                IEnumerable<int> A(out int a)
                {
                    throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoValidation()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            class TypeName
            {
                IEnumerable<int> A(string a)
                {
                    yield return 0;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SameBlock()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            class TypeName
            {
                IEnumerable<int> A(string a)
                {
                    if (a == null)
                    {
                        throw new System.ArgumentNullException(nameof(a));
                        yield return 0;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StatementInMiddleOfArgumentValidation()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections;
            class TypeName
            {
                IEnumerable A(string a)
                {
                    if (a == null)
                        throw new System.ArgumentNullException(nameof(a));

                    yield break;

                    if (a == null)
                        throw new System.ArgumentNullException(nameof(a));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            class TypeName
            {
                IEnumerable<int> {|MA0050:A|}(string a)
                {
                    if (a == null)
                        throw new System.ArgumentNullException(nameof(a));

                    yield return 0;
                    if (a == null)
                    {
                        yield return 1;
                    }
                }
            }
            """;
        test.FixedCode = """
            using System.Collections.Generic;
            class TypeName
            {
                IEnumerable<int> A(string a)
                {
                    if (a == null)
                        throw new System.ArgumentNullException(nameof(a));

                    return A(a);
                    IEnumerable<int> A(string a)
                    {
                        yield return 0;
                        if (a == null)
                        {
                            yield return 1;
                        }
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidValidation()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            class TypeName
            {
                IEnumerable<int> A(string a)
                {
                    if (a == null)
                        throw new System.ArgumentNullException(nameof(a));

                    return A();

                    IEnumerable<int> A()
                    {
                        yield return 0;
                        if (a == null)
                        {
                            yield return 1;
                        }
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidValidation_ThrowIfNull()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestCode = """
            using System.Collections.Generic;
            class TypeName
            {
                IEnumerable<int> A(string a)
                {
                    System.ArgumentNullException.ThrowIfNull(a);

                    return A();

                    IEnumerable<int> A()
                    {
                        yield return 0;
                        if (a == null)
                        {
                            yield return 1;
                        }
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidValidation_ArgumentExceptionThrowIfNullOrEmpty()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestCode = """
            using System.Collections.Generic;
            class TypeName
            {
                IEnumerable<int> A(string a)
                {
                    System.ArgumentException.ThrowIfNullOrEmpty(a);

                    return A();

                    IEnumerable<int> A()
                    {
                        yield return 0;
                        if (a == null)
                        {
                            yield return 1;
                        }
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_ArgumentExceptionThrowIfNullOrEmpty()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestCode = """
            using System.Collections.Generic;
            class TypeName
            {
                IEnumerable<int> {|MA0050:A|}(string a)
                {
                    System.ArgumentException.ThrowIfNullOrEmpty(a);
                    yield return 0;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_CustomArgumentExceptionThrowIf()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            class CustomArgumentException : System.ArgumentException
            {
                public static void ThrowIf(bool condition, string paramName)
                {
                    if (condition)
                        throw new CustomArgumentException(paramName);
                }

                public CustomArgumentException(string paramName) : base(paramName)
                {
                }
            }

            class TypeName
            {
                IEnumerable<int> {|MA0050:A|}(string a)
                {
                    CustomArgumentException.ThrowIf(a is null, nameof(a));
                    yield return 0;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_CustomArgumentExceptionThrow()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            class CustomArgumentException : System.ArgumentException
            {
                public static void Throw(bool condition, string paramName)
                {
                    if (condition)
                        throw new CustomArgumentException(paramName);
                }

                public CustomArgumentException(string paramName) : base(paramName)
                {
                }
            }

            class TypeName
            {
                IEnumerable<int> {|MA0050:A|}(string a)
                {
                    CustomArgumentException.Throw(a is null, nameof(a));
                    yield return 0;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_IAsyncEnumerable()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            class TypeName
            {
                async IAsyncEnumerable<int> {|MA0050:A|}(string a)
                {
                    if (a == null)
                        throw new System.ArgumentNullException(nameof(a));

                    await System.Threading.Tasks.Task.Delay(1);
                    yield return 0;

                }
            }
            """;
        test.FixedCode = """
            using System.Collections.Generic;
            class TypeName
            {
                IAsyncEnumerable<int> A(string a)
                {
                    if (a == null)
                        throw new System.ArgumentNullException(nameof(a));

                    return A(a);

                    async IAsyncEnumerable<int> A(string a)
                    {
                        await System.Threading.Tasks.Task.Delay(1);
                        yield return 0;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_IAsyncEnumerable_ThrowIfNull()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestCode = """
            using System.Collections.Generic;
            class TypeName
            {
                async IAsyncEnumerable<int> {|MA0050:A|}(string a)
                {
                    System.ArgumentNullException.ThrowIfNull(a);

                    await System.Threading.Tasks.Task.Delay(1);
                    yield return 0;

                }
            }
            """;
        test.FixedCode = """
            using System.Collections.Generic;
            class TypeName
            {
                IAsyncEnumerable<int> A(string a)
                {
                    System.ArgumentNullException.ThrowIfNull(a);

                    return A(a);

                    async IAsyncEnumerable<int> A(string a)
                    {
                        await System.Threading.Tasks.Task.Delay(1);
                        yield return 0;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_FixerPreserveEnumerableCancellationAttribute()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestCode = """
            using System.Runtime.CompilerServices;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            class TypeName
            {
                async IAsyncEnumerable<int> {|MA0050:A|}(string a, [EnumeratorCancellation] CancellationToken ct = default)
                {
                    System.ArgumentNullException.ThrowIfNull(a);

                    await System.Threading.Tasks.Task.Delay(1);
                    yield return 0;

                }
            }
            """;
        test.FixedCode = """
            using System.Runtime.CompilerServices;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            class TypeName
            {
                IAsyncEnumerable<int> A(string a, CancellationToken ct = default)
                {
                    System.ArgumentNullException.ThrowIfNull(a);

                    return A(a, ct);

                    async IAsyncEnumerable<int> A(string a, [EnumeratorCancellation] CancellationToken ct)
                    {
                        await System.Threading.Tasks.Task.Delay(1);
                        yield return 0;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_ExtensionMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Collections.Generic;
            static class TypeName
            {
                public static IEnumerable<int> {|MA0050:A|}(this string a)
                {
                    if (a is null)
                        throw new ArgumentNullException(nameof(a));

                    yield return 1;
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Collections.Generic;
            static class TypeName
            {
                public static IEnumerable<int> A(this string a)
                {
                    if (a is null)
                        throw new ArgumentNullException(nameof(a));

                    return A(a);
                    IEnumerable<int> A(string a)
                    {
                        yield return 1;
                    }
                }
            }
            """;

        return test.RunAsync();
    }
}
