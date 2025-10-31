using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ValidateArgumentsCorrectlyAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<ValidateArgumentsCorrectlyAnalyzer>()
            .WithCodeFixProvider<ValidateArgumentsCorrectlyFixer>();
    }

    [Fact]
    public async Task ReturnVoid()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            class TypeName
            {
                void A()
                {
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReturnString()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            class TypeName
            {
                string A()
                {
                    throw null;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OutParameter()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            class TypeName
            {
                IEnumerable<int> A(out int a)
                {
                    throw null;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoValidation()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            class TypeName
            {
                IEnumerable<int> A(string a)
                {
                    yield return 0;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SameBlock()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StatementInMiddleOfArgumentValidation()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportDiagnostic()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            class TypeName
            {
                IEnumerable<int> [||]A(string a)
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

        const string CodeFix = """
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

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task ValidValidation()
    {
        const string SourceCode = """
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

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ValidValidation_ThrowIfNull()
    {
        const string SourceCode = """
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

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithTargetFramework(TargetFramework.Net8_0)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportDiagnostic_IAsyncEnumerable()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            class TypeName
            {
                async IAsyncEnumerable<int> [||]A(string a)
                {
                    if (a == null)
                        throw new System.ArgumentNullException(nameof(a));
            
                    await System.Threading.Tasks.Task.Delay(1);
                    yield return 0;
                    
                }
            }
            """;

        const string CodeFix = """
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

        await CreateProjectBuilder()
              .AddAsyncInterfaceApi()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportDiagnostic_IAsyncEnumerable_ThrowIfNull()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            class TypeName
            {
                async IAsyncEnumerable<int> [||]A(string a)
                {
                    System.ArgumentNullException.ThrowIfNull(a);
            
                    await System.Threading.Tasks.Task.Delay(1);
                    yield return 0;
                    
                }
            }
            """;

        const string CodeFix = """
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

        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportDiagnostic_FixerPreserveEnumerableCancellationAttribute()
    {
        const string SourceCode = """
            using System.Runtime.CompilerServices;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            class TypeName
            {
                async IAsyncEnumerable<int> [||]A(string a, [EnumeratorCancellation] CancellationToken ct = default)
                {
                    System.ArgumentNullException.ThrowIfNull(a);

                    await System.Threading.Tasks.Task.Delay(1);
                    yield return 0;
                    
                }
            }
            """;

        const string CodeFix = """
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

        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }
}
