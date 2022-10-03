using System.Globalization;
using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed partial class ReturnTaskDirectlyTests
{
	private const string Scaffold = """
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnitTests;

public class Test {{
	private readonly DataService _dataService;

	{0}

	public Task<List<int>> GetListAsync() => throw null;

	public Task Accept(Func<Task> func) => func();

	public Task<T> AcceptValue<T>(Func<Task<T>> func) => func();
}}

public class MyDisposable : IDisposable, IAsyncDisposable {{
	public void Dispose() => throw null;

	public ValueTask DisposeAsync() => throw null;
}}

public class DataService {{
	public Task<List<int>> GetListAsync() => throw null;
}}
""";

    [Fact]
    public Task SingleAwait()
    {
        const string Source = @"
async Task RunAsync() {
	[|await Task.Delay(1000)|];
}";
        const string FixedSource = @"
Task RunAsync() {
	return Task.Delay(1000);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task SingleValueTaskAwait()
    {
        const string Source = @"
async ValueTask RunAsync() {
	[|await new ValueTask()|];
}";
        const string FixedSource = @"
ValueTask RunAsync() {
	return new ValueTask();
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task SingleAwaitExpression()
    {
        const string Source = @"
public async Task RunAsync() => [|await Task.Delay(1000)|];";
        const string FixedSource = @"
public Task RunAsync() => Task.Delay(1000);";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task SingleValueTaskAwaitExpression()
    {
        const string Source = @"
public async ValueTask RunAsync() => [|await new ValueTask()|];";
        const string FixedSource = @"
public ValueTask RunAsync() => new ValueTask();";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task SingleAwaitWithReturnExpression()
    {
        const string Source = @"
public async Task<int> RunAsync() => [|await Task.FromResult(2)|];";
        const string FixedSource = @"
public Task<int> RunAsync() => Task.FromResult(2);";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task SingleAwaitWithValueTaskReturnExpression()
    {
        const string Source = @"
public async ValueTask<int> RunAsync() => [|await new ValueTask<int>(2)|];";
        const string FixedSource = @"
public ValueTask<int> RunAsync() => new ValueTask<int>(2);";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task SingleAwaitWithReturn()
    {
        const string Source = @"
public async Task<int> RunAsync() {
	return [|await Task.FromResult(2)|];
}";
        const string FixedSource = @"
public Task<int> RunAsync() {
	return Task.FromResult(2);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task SingleValueTaskAwaitWithReturn()
    {
        const string Source = @"
public async ValueTask<int> RunAsync() {
	return [|await new ValueTask<int>(2)|];
}";
        const string FixedSource = @"
public ValueTask<int> RunAsync() {
	return new ValueTask<int>(2);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task MultipleStatementsWithSingleAwait()
    {
        const string Source = @"
public async Task RunAsync() {
	var guid = Guid.NewGuid();

	[|await Task.Delay(1000)|];
}";
        const string FixedSource = @"
public Task RunAsync() {
	var guid = Guid.NewGuid();

	return Task.Delay(1000);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task MultipleStatementsWithReturn()
    {
        const string Source = @"
public async Task<int> RunAsync() {
	var task = Task.FromResult(2);
	Console.WriteLine(task.Id);

	return [|await task|];
}";
        const string FixedSource = @"
public Task<int> RunAsync() {
	var task = Task.FromResult(2);
	Console.WriteLine(task.Id);

	return task;
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task MultipleReturnStatements()
    {
        const string Source = @"
public async Task<int> RunAsync() {
	var guid = Guid.NewGuid().ToString();
	if(guid.StartsWith(""a"")) {
		return [|await Task.FromResult(6)|];
	}

	if(guid.StartsWith(""b"")) {
		return [|await Task.FromResult(3)|];
	}

	return [|await Task.FromResult(2)|];
}";
        const string FixedSource = @"
public Task<int> RunAsync() {
	var guid = Guid.NewGuid().ToString();
	if(guid.StartsWith(""a"")) {
		return Task.FromResult(6);
	}

	if(guid.StartsWith(""b"")) {
		return Task.FromResult(3);
	}

	return Task.FromResult(2);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task WithNonRelevantUsingBlock()
    {
        const string Source = @"
public async Task<int> RunAsync() {
	var task = Task.FromResult(2);
	using (var _ = new MyDisposable()) {
		Console.WriteLine(task.Id);
	}

	if(task.IsCompleted) {
		return [|await Task.FromResult(5)|];
	}

	return [|await task|];
}";
        const string FixedSource = @"
public Task<int> RunAsync() {
	var task = Task.FromResult(2);
	using (var _ = new MyDisposable()) {
		Console.WriteLine(task.Id);
	}

	if(task.IsCompleted) {
		return Task.FromResult(5);
	}

	return task;
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task WithNonRelevantTryBlock()
    {
        const string Source = @"
public async Task RunAsync() {
	var task = Task.Delay(1000);
	try {
		Console.WriteLine(task.Id);
	} catch (Exception) {
	}

	[|await task|];
}";
        const string FixedSource = @"
public Task RunAsync() {
	var task = Task.Delay(1000);
	try {
		Console.WriteLine(task.Id);
	} catch (Exception) {
	}

	return task;
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task WithMultipleAwaits()
    {
        const string Source = @"
private async Task RunAsync()
{
	var x = new Random().Next();
	if(x == 3)
	{
		[|await Task.Delay(1000)|];
		return;
	}

	[|await Task.Delay(1000)|];
}";
        const string FixedSource = @"
private Task RunAsync()
{
	var x = new Random().Next();
	if(x == 3)
	{
		return Task.Delay(1000);
	}

	return Task.Delay(1000);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task WithLocalFunction()
    {
        const string Source = @"
private async Task RunAsync()
{
	var x = new Random().Next();
	if(x == 3)
	{
		[|await Task.Delay(1000)|];
		return;
	}

	async Task LocalFuncAsync() {
		if(x == 4) {
			await Task.Delay(500);
		}

		await Task.CompletedTask;
	}

	[|await LocalFuncAsync()|];
}";
        const string FixedSource = @"
private Task RunAsync()
{
	var x = new Random().Next();
	if(x == 3)
	{
		return Task.Delay(1000);
	}

	async Task LocalFuncAsync() {
		if(x == 4) {
			await Task.Delay(500);
		}

		await Task.CompletedTask;
	}

	return LocalFuncAsync();
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task LocalFunction()
    {
        const string Source = @"
private Task RunAsync()
{
	async Task ComputeAsync() {
		[|await Task.Delay(1000)|];
	}

	return ComputeAsync();
}";
        const string FixedSource = @"
private Task RunAsync()
{
	Task ComputeAsync() {
		return Task.Delay(1000);
	}

	return ComputeAsync();
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task WithConfigureAwait()
    {
        const string Source = @"
private async Task RunAsync()
{
	var x = new Random().Next();
	if(x == 3)
	{
		[|await Task.Delay(1000).ConfigureAwait(false)|];
		return;
	}

	[|await Task.Delay(1000)|];
}";
        const string FixedSource = @"
private Task RunAsync()
{
	var x = new Random().Next();
	if(x == 3)
	{
		return Task.Delay(1000);
	}

	return Task.Delay(1000);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task WithUnrelatedUsingStatement()
    {
        const string Source = @"
private async Task RunAsync()
{
	var x = new Random().Next();
	if(x == 3)
	{
		using var _ = new MyDisposable();
		Console.WriteLine(2);
	}

	[|await Task.Delay(1000)|];
}";
        const string FixedSource = @"
private Task RunAsync()
{
	var x = new Random().Next();
	if(x == 3)
	{
		using var _ = new MyDisposable();
		Console.WriteLine(2);
	}

	return Task.Delay(1000);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task LambdaExpression()
    {
        const string Source = @"
private Task RunAsync()
{
	return Accept(async () => [|await Task.Delay(1000)|]);
}";
        const string FixedSource = @"
private Task RunAsync()
{
	return Accept(() => Task.Delay(1000));
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task LambdaExpressionWithReturn()
    {
        const string Source = @"
private Task RunAsync()
{
	return AcceptValue(async () => [|await Task.FromResult(2)|]);
}";
        const string FixedSource = @"
private Task RunAsync()
{
	return AcceptValue(() => Task.FromResult(2));
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task LambdaBlock()
    {
        const string Source = @"
private Task RunAsync()
{
	return Accept(async () =>
	{
		var guid = Guid.NewGuid();
		if(guid == Guid.NewGuid()) {
			[|await Task.Delay(1000)|];
			return;
		}

		[|await Task.CompletedTask|];
	});
}";
        const string FixedSource = @"
private Task RunAsync()
{
	return Accept(() =>
	{
		var guid = Guid.NewGuid();
		if(guid == Guid.NewGuid()) {
			return Task.Delay(1000);
		}

		return Task.CompletedTask;
	});
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    [Fact]
    public Task LambdaBlockWithReturn()
    {
        const string Source = @"
private Task RunAsync()
{
	return AcceptValue(async () =>
	{
		var guid = Guid.NewGuid();
		if(guid == Guid.NewGuid()) {
			return [|await Task.FromResult(2)|];
		}

		return [|await Task.FromResult(5)|];
	});
}";
        const string FixedSource = @"
private Task RunAsync()
{
	return AcceptValue(() =>
	{
		var guid = Guid.NewGuid();
		if(guid == Guid.NewGuid()) {
			return Task.FromResult(2);
		}

		return Task.FromResult(5);
	});
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ShouldFixCodeWith(string.Format(CultureInfo.InvariantCulture, Scaffold, FixedSource))
            .ValidateAsync();
    }

    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<ReturnTaskDirectlyAnalyzer>()
            .WithCodeFixProvider<ReturnTaskDirectlyFixer>()
            .WithTargetFramework(TargetFramework.Net7_0);
    }
}
