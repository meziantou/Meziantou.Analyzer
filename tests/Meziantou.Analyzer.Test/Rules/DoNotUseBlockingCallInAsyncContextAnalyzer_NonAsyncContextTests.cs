using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseBlockingCallInAsyncContextAnalyzer_NonAsyncContextTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotUseBlockingCallInAsyncContextAnalyzer>(id: "MA0045");
    }

    [Fact]
    public async Task PublicNonAsync_Wait_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Threading.Tasks;
public class Test
{
    public void A()
    {
        Task.Delay(1).Wait();
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task PublicNonAsync_AsyncSuffix_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Threading.Tasks;
public class Test
{
    public void A()
    {
        Write();
    }

    public void Write() => throw null;
    public Task WriteAsync() => throw null;
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task PrivateNonAsync_Wait_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Threading.Tasks;
public class Test
{
    private void A()
    {
        [|Task.Delay(1).Wait()|];
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task PrivateNonAsync_AsyncSuffix()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Threading.Tasks;
public class Test
{
    private void A()
    {
        [|Write()|];
    }

    public void Write() => throw null;
    public Task WriteAsync() => throw null;
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task PrivateNonAsync_AsyncSuffix_InLock()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Threading.Tasks;
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
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task LambdaInLock()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Threading.Tasks;
public class Test
{
    private void A()
    {
        lock (this)
        {
            _ = Task.FromResult(0).ContinueWith(t => t.Result);
        }
    }
}")
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public async Task PrivateNonAsync_SqliteConnection_CreateCommand_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddNuGetReference("Microsoft.Data.Sqlite.Core", "8.0.0", "lib/net8.0/")
              .WithSourceCode("""
                using Microsoft.Data.Sqlite;

                class Test
                {
                    private void A(SqliteConnection connection)
                    {
                        using var command = connection.CreateCommand();
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public async Task PrivateNonAsync_SqliteCommand_Prepare_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddNuGetReference("Microsoft.Data.Sqlite.Core", "8.0.0", "lib/net8.0/")
              .WithSourceCode("""
                using Microsoft.Data.Sqlite;

                class Test
                {
                    private void A(SqliteCommand command)
                    {
                        command.Prepare();
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public async Task PrivateNonAsync_SqliteConnection_Close_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddNuGetReference("Microsoft.Data.Sqlite.Core", "8.0.0", "lib/net8.0/")
              .WithSourceCode("""
                using Microsoft.Data.Sqlite;

                class Test
                {
                    private void A(SqliteConnection connection)
                    {
                        connection.Close();
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public async Task PrivateNonAsync_SqliteConnection_Close_OptionDisabled_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddNuGetReference("Microsoft.Data.Sqlite.Core", "8.0.0", "lib/net8.0/")
              .AddAnalyzerConfiguration("MA0042.enable_sqlite_special_cases", "false")
              .WithSourceCode("""
                using Microsoft.Data.Sqlite;

                class Test
                {
                    private void A(SqliteConnection connection)
                    {
                        [|connection.Close()|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public async Task PrivateNonAsync_SqliteCommand_Prepare_OptionDisabled_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddNuGetReference("Microsoft.Data.Sqlite.Core", "8.0.0", "lib/net8.0/")
              .AddAnalyzerConfiguration("MA0042.enable_sqlite_special_cases", "false")
              .WithSourceCode("""
                using Microsoft.Data.Sqlite;

                class Test
                {
                    private void A(SqliteCommand command)
                    {
                        [|command.Prepare()|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public async Task PrivateNonAsync_SqliteDataReader_Read_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddNuGetReference("Microsoft.Data.Sqlite.Core", "8.0.0", "lib/net8.0/")
              .WithSourceCode("""
                using Microsoft.Data.Sqlite;

                class Test
                {
                    private void A(SqliteDataReader reader)
                    {
                        reader.Read();
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public async Task PrivateNonAsync_SqliteDataReader_Read_OptionDisabled_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddNuGetReference("Microsoft.Data.Sqlite.Core", "8.0.0", "lib/net8.0/")
              .AddAnalyzerConfiguration("MA0042.enable_sqlite_special_cases", "false")
              .WithSourceCode("""
                using Microsoft.Data.Sqlite;

                class Test
                {
                    private void A(SqliteDataReader reader)
                    {
                        [|reader.Read()|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1134")]
    public async Task PrivateNonAsync_UsingFactoryMethod_DbTransaction_NoDisposeAsyncOverride_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
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
                """)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1134")]
    public async Task PrivateNonAsync_UsingFactoryMethod_DbTransaction_NoDisposeAsyncOverride_OptionDisabled_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddAnalyzerConfiguration("MA0042.enable_db_special_cases", "false")
              .WithSourceCode("""
                using System.Data;
                using System.Data.Common;

                class Test
                {
                    private void A()
                    {
                        [|using var transaction = CreateTransaction();|]
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
                """)
              .ValidateAsync();
    }
}
