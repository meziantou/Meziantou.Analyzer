using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotUseBlockingCallInAsyncContextAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseBlockingCallInAsyncContextAnalyzer_NonAsyncContextTests
{
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.DisabledDiagnostics.Add("MA0042");
        return test;
    }

    [Fact]
    public Task PublicNonAsync_Wait_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            public class Test
            {
                public void A()
                {
                    Task.Delay(1).Wait();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PublicNonAsync_AsyncSuffix_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            public class Test
            {
                public void A()
                {
                    Write();
                }

                public void Write() => throw null;
                public Task WriteAsync() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PrivateNonAsync_Wait_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            public class Test
            {
                private void A()
                {
                    {|MA0045:Task.Delay(1).Wait()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PrivateNonAsync_AsyncSuffix()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            public class Test
            {
                private void A()
                {
                    {|MA0045:Write()|};
                }

                public void Write() => throw null;
                public Task WriteAsync() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PrivateNonAsync_AsyncSuffix_InLock()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            public class Test
            {
                private void A()
                {
                    lock (this)
                    {
                        Write();
                    }
                }

                public void Write() => throw null;
                public Task WriteAsync() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LambdaInLock()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            public class Test
            {
                private void A()
                {
                    lock (this)
                    {
                        _ = Task.FromResult(0).ContinueWith(t => t.Result);
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public Task PrivateNonAsync_SqliteConnection_CreateCommand_NoDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([new PackageIdentity("Microsoft.Data.Sqlite.Core", "8.0.0")]);
        test.TestCode = """
            using Microsoft.Data.Sqlite;

            class Test
            {
                private void A(SqliteConnection connection)
                {
                    using var command = connection.CreateCommand();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public Task PrivateNonAsync_SqliteCommand_Prepare_NoDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([new PackageIdentity("Microsoft.Data.Sqlite.Core", "8.0.0")]);
        test.TestCode = """
            using Microsoft.Data.Sqlite;

            class Test
            {
                private void A(SqliteCommand command)
                {
                    command.Prepare();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public Task PrivateNonAsync_SqliteConnection_Close_NoDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([new PackageIdentity("Microsoft.Data.Sqlite.Core", "8.0.0")]);
        test.TestCode = """
            using Microsoft.Data.Sqlite;

            class Test
            {
                private void A(SqliteConnection connection)
                {
                    connection.Close();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public Task PrivateNonAsync_SqliteConnection_Close_OptionDisabled_Diagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.SetConfiguration("MA0042.enable_sqlite_special_cases", "false");
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([new PackageIdentity("Microsoft.Data.Sqlite.Core", "8.0.0")]);
        test.TestCode = """
            using Microsoft.Data.Sqlite;

            class Test
            {
                private void A(SqliteConnection connection)
                {
                    {|MA0045:connection.Close()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public Task PrivateNonAsync_SqliteCommand_Prepare_OptionDisabled_Diagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.SetConfiguration("MA0042.enable_sqlite_special_cases", "false");
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([new PackageIdentity("Microsoft.Data.Sqlite.Core", "8.0.0")]);
        test.TestCode = """
            using Microsoft.Data.Sqlite;

            class Test
            {
                private void A(SqliteCommand command)
                {
                    {|MA0045:command.Prepare()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public Task PrivateNonAsync_SqliteDataReader_Read_NoDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([new PackageIdentity("Microsoft.Data.Sqlite.Core", "8.0.0")]);
        test.TestCode = """
            using Microsoft.Data.Sqlite;

            class Test
            {
                private void A(SqliteDataReader reader)
                {
                    reader.Read();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public Task PrivateNonAsync_SqliteDataReader_Read_OptionDisabled_Diagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.SetConfiguration("MA0042.enable_sqlite_special_cases", "false");
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([new PackageIdentity("Microsoft.Data.Sqlite.Core", "8.0.0")]);
        test.TestCode = """
            using Microsoft.Data.Sqlite;

            class Test
            {
                private void A(SqliteDataReader reader)
                {
                    {|MA0045:reader.Read()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1134")]
    public Task PrivateNonAsync_UsingFactoryMethod_DbTransaction_NoDisposeAsyncOverride_NoDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestCode = """
            using System.Data;
            using System.Data.Common;

            class Test
            {
                private void A()
                {
                    using var transaction = CreateTransaction();
                }

                private MyDbTransaction CreateTransaction() => throw null;
            }

            class MyDbTransaction : DbTransaction
            {
                protected override DbConnection DbConnection => throw null;
                public override IsolationLevel IsolationLevel => throw null;
                public override void Commit() => throw null;
                public override void Rollback() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1134")]
    public Task PrivateNonAsync_UsingFactoryMethod_DbTransaction_NoDisposeAsyncOverride_OptionDisabled_Diagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.SetConfiguration("MA0042.enable_db_special_cases", "false");
        test.TestCode = """
            using System.Data;
            using System.Data.Common;

            class Test
            {
                private void A()
                {
                    {|MA0045:using var transaction = CreateTransaction();|}
                }

                private MyDbTransaction CreateTransaction() => throw null;
            }

            class MyDbTransaction : DbTransaction
            {
                protected override DbConnection DbConnection => throw null;
                public override IsolationLevel IsolationLevel => throw null;
                public override void Commit() => throw null;
                public override void Rollback() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludeFromBlockingCallAnalysisAttribute_DocumentationIdMethod()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.ExcludeFromBlockingCallAnalysisAttribute("M:System.Threading.Tasks.Task.Wait")]

            class Test
            {
                private void A()
                {
                    Task.Delay(1).Wait();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludeFromBlockingCallAnalysisAttribute_MethodSignature()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            [assembly: Meziantou.Analyzer.Annotations.ExcludeFromBlockingCallAnalysisAttribute(typeof(System.Threading.Thread), "Sleep", typeof(int))]

            class Test
            {
                private void A()
                {
                    System.Threading.Thread.Sleep(1);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludeFromBlockingCallAnalysisAttribute_MethodSignature_OnlyMatchingOverload()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System;
            [assembly: Meziantou.Analyzer.Annotations.ExcludeFromBlockingCallAnalysisAttribute(typeof(System.Threading.Thread), "Sleep", typeof(int))]

            class Test
            {
                private void A()
                {
                    {|MA0045:System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1))|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludeFromBlockingCallAnalysisAttribute_MethodSignature_DoesNotAffectAwaitUsing()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.ExcludeFromBlockingCallAnalysisAttribute(typeof(Test), "Create")]

            class Test
            {
                private void A()
                {
                    {|MA0045:using var value = Create();|}
                }

                private AsyncDisposable Create() => throw null;
            }

            class AsyncDisposable : IDisposable, IAsyncDisposable
            {
                public void Dispose() { }
                public ValueTask DisposeAsync() => default;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonAwaitableTypeAttribute_DoesNotAffectAwaitUsing()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.NonAwaitableTypeAttribute(typeof(AsyncDisposable))]

            class Test
            {
                private void A()
                {
                    {|MA0045:using var value = new AsyncDisposable();|}
                }
            }

            class AsyncDisposable : IDisposable, IAsyncDisposable
            {
                public void Dispose() { }
                public ValueTask DisposeAsync() => default;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonAsyncDisposableTypeAttribute_DoesAffectAwaitUsing()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.NonAsyncDisposableTypeAttribute(typeof(AsyncDisposable))]

            class Test
            {
                private void A()
                {
                    using var value = new AsyncDisposable();
                }
            }

            class AsyncDisposable : IDisposable, IAsyncDisposable
            {
                public void Dispose() { }
                public ValueTask DisposeAsync() => default;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonAwaitableTypeAttribute_DoesNotAffectTaskWrappedAwaitSuggestion()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.NonAwaitableTypeAttribute(typeof(AwaitResult))]

            class Test
            {
                private void A()
                {
                    {|MA0045:Create()|};
                }

                private AwaitResult Create() => throw null;
                private Task<AwaitResult> CreateAsync() => throw null;
            }

            class AwaitResult { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonAwaitableTypeAttribute_DoesNotAffectDerivedType_AwaitUsing()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.NonAwaitableTypeAttribute(typeof(BaseAsyncDisposable))]

            class Test
            {
                private void A()
                {
                    {|MA0045:using var value = new DerivedAsyncDisposable();|}
                }
            }

            class BaseAsyncDisposable : IDisposable, IAsyncDisposable
            {
                public void Dispose() { }
                public ValueTask DisposeAsync() => default;
            }

            class DerivedAsyncDisposable : BaseAsyncDisposable { }
            """;

        return test.RunAsync();
    }
}
