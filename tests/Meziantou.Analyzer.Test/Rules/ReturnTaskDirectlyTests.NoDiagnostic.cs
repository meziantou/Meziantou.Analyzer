using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed partial class ReturnTaskDirectlyTests
{
    [Fact]
    public Task NonTaskMethod()
    {
        const string Source = @"
public void Run() {
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task NonTaskMethod2()
    {
        const string Source = @"
public int Run() {
	return 5;
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task NoAwait()
    {
        const string Source = @"
public async Task Run() {
	Console.WriteLine(""Hello World"");
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task AsyncVoidMethod()
    {
        const string Source = @"
public async void Run() {
	await Task.CompletedTask;
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CorrectUsage()
    {
        const string Source = @"
public Task RunAsync() {
	return Task.Delay(1000);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CorrectUsage2()
    {
        const string Source = @"
public Task<int> RunAsync() {
	return Task.FromResult(2);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CorrectUsageWithMultipleStatements()
    {
        const string Source = @"
public Task RunAsync() {
	var guid = Guid.NewGuid();
	Console.WriteLine(guid);

	return Task.Delay(1000);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CorrectUsageWithMultipleStatements2()
    {
        const string Source = @"
public Task<int> RunAsync() {
	var guid = Guid.NewGuid();
	Console.WriteLine(guid);

	return Task.FromResult(2);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CorrectUsageWithMultipleReturnStatements()
    {
        const string Source = @"
public Task RunAsync() {
	var guid = Guid.NewGuid().ToString();
	if(guid.StartsWith(""a"")) {
		return Task.Delay(1000);
	}

	return Task.CompletedTask;
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CorrectUsageWithMultipleReturnStatements2()
    {
        const string Source = @"
public Task RunAsync() {
	var guid = Guid.NewGuid().ToString();
	if(guid.StartsWith(""a"")) {
		return Task.FromResult(5);
	}

	return Task.FromResult(2);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task MixedReturn()
    {
        const string Source = @"
public async Task<int> RunAsync()
{
	var guid = Guid.NewGuid();
	if (guid.ToString().StartsWith(""a""))
	{
		return await Task.FromResult(2);
	}

	return 6;
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task MixedAwaits()
    {
        const string Source = @"
public async Task<int> RunAsync()
{
    foreach (var i in Enumerable.Range(0, 10))
    {
        var result = await Task.FromResult(i);
    }

    return await Task.FromResult(2);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task InUsingBlock()
    {
        const string Source = @"
public async Task RunAsync() {
	using (var _ = new MyDisposable()) {
		await Task.Delay(1000);
	}
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task InUsingBlockWithReturn()
    {
        const string Source = @"
public async Task RunAsync() {
	using (var _ = new MyDisposable()) {
		await Task.Delay(1000);
		return;
	}
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task InUsingBlockWithMultipleStatements()
    {
        const string Source = @"
public async Task RunAsync() {
	using (var _ = new MyDisposable()) {
		await Task.Delay(1000);
	}

	await Task.CompletedTask;
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task InUsingBlockWithMultipleStatementsWithReturn()
    {
        const string Source = @"
public async Task RunAsync() {
	using (var _ = new MyDisposable()) {
		await Task.Delay(1000);
		return;
	}

	await Task.CompletedTask;
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task InUsingStatement()
    {
        const string Source = @"
public async Task RunAsync() {
	using var _ = new MyDisposable();
	await Task.Delay(1000);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task InNestedUsingStatement()
    {
        const string Source = @"
public async Task RunAsync() {
	var x = new Random().Next();
	if(x == 3)
	{
		using var _ = new MyDisposable();
		await Task.Delay(1000);
		return;
	}

	await Task.CompletedTask;
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task InNestedUsingStatement2()
    {
        const string Source = @"
public async Task<int> RunAsync() {
	var x = new Random().Next();
	if(x == 3)
	{
		using var _ = new MyDisposable();
		return await Task.FromResult(2);
	}

	return await Task.FromResult(5);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task InAwaitUsingBlock()
    {
        const string Source = @"
public async Task RunAsync() {
	await using (var _ = new MyDisposable()) {
		await Task.Delay(1000);
	}
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task InAwaitUsingStatement()
    {
        const string Source = @"
public async Task RunAsync() {
	await using var _ = new MyDisposable();
	await Task.Delay(1000);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task InTryBlock()
    {
        const string Source = @"
public async Task RunAsync() {
	try {
		await Task.Delay(1000);
	} catch (Exception) {
	}
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task InTryBlockWithReturn()
    {
        const string Source = @"
public async Task RunAsync() {
	try {
		await Task.Delay(1000);
		return;
	} catch (Exception) {
	}
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task InTryBlockWithMultipleStatements()
    {
        const string Source = @"
public async Task RunAsync() {
	try {
		await Task.Delay(1000);
	} catch (Exception) {
	}

	await Task.Delay(1000);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task InTryBlockWithMultipleStatementsWithReturn()
    {
        const string Source = @"
public async Task RunAsync() {
	try {
		await Task.Delay(1000);
		return;
	} catch (Exception) {
	}

	await Task.Delay(1000);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task InTryBlockWithValueReturn()
    {
        const string Source = @"
public async Task<int> RunAsync()
{
	try
	{
		return await Task.FromResult(2);
	}
	catch (Exception)
	{
		return 2;
	}
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task MultipleAwaitExpressions()
    {
        const string Source = @"
public async Task RunAsync() {
	var x = 2;
	await Task.Delay(1000);
	Console.WriteLine(x);
	await Task.Delay(1000);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task MultipleAwaitExpressionsNested()
    {
        const string Source = @"
public async Task RunAsync() {
	var x = 2;
	if(x % 2 == 0) {
		await Task.Delay(1000);
	}

	Console.WriteLine(x);
	await Task.Delay(1000);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task MultipleAwaitExpressionsNestedWithReturn()
    {
        const string Source = @"
private async Task<int> RunAsync()
{
	var x = new Random().Next();
	if(x == 3)
	{
		await Task.Delay(1000);
		return 2;
	}

	return await Task.FromResult(1000);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task MultipleAwaitExpressionsInLoop()
    {
        const string Source = @"
public async Task RunAsync() {
	for(var i = 0; i < 10; i++) {
		await Task.Delay(200);
	}
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CovariantReturnType()
    {
        const string Source = @"
public async Task<IEnumerable<int>> RunAsync() {
	return await GetListAsync();
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CovariantReturnTypeWithMethodCall()
    {
        const string Source = @"
public async Task<IEnumerable<int>> RunAsync() {
	return await _dataService.GetListAsync();
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CovariantValueTaskReturnType()
    {
        const string Source = @"
private ValueTask GetValueTaskAsync() => throw null;

public async Task RunAsync() {
	await GetValueTaskAsync();
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CovariantValueTaskReturnType2()
    {
        const string Source = @"
public async Task RunAsync() {
	await new ValueTask();
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CovariantValueTaskReturnTypeWithConfigureAwait()
    {
        const string Source = @"
public async Task RunAsync() {
	await new ValueTask().ConfigureAwait(false);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CovariantValueTaskReturnTypeWithConfigureAwait2()
    {
        const string Source = @"
ValueTask GetValueTaskAsync() => throw null;

public async Task RunAsync() {
	await GetValueTaskAsync().ConfigureAwait(false);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CovariantValueTaskExpressionReturnType()
    {
        const string Source = @"
private ValueTask GetValueTaskAsync() => throw null;
public async Task RunAsync() => await GetValueTaskAsync();";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CovariantValueTaskExpressionReturnType2()
    {
        const string Source = @"
public async Task RunAsync() => await new ValueTask();";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CovariantValueTaskWithValueReturnType()
    {
        const string Source = @"
public async Task<int> RunAsync() {
	return await new ValueTask<int>(2);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CovariantValueTaskWithValueExpressionReturnType()
    {
        const string Source = @"
public async Task<int> RunAsync() => await new ValueTask<int>(2);";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CorrectLambdaExpression()
    {
        const string Source = @"
public void Run() {
	Accept(() => Task.Delay(1000));
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CorrectLambdaExpression2()
    {
        const string Source = @"
private Task DoSomethingAsync() => throw null;

public void Run() {
	Accept(DoSomethingAsync);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CorrectLambdaExpressionWithReturn()
    {
        const string Source = @"
public void Run() {
	AcceptValue(() => Task.FromResult(2));
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CorrectLambdaExpressionWithReturn2()
    {
        const string Source = @"
private Task<int> GetValueAsync() => throw null;

public void Run() {
	AcceptValue<int>(GetValueAsync);
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CorrectLambdaBlock()
    {
        const string Source = @"
public async Task RunAsync() {
	Accept(() => {
		var x = Guid.NewGuid();

		return Task.Delay(1000);
	});
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }

    [Fact]
    public Task CorrectLambdaBlockWithReturn()
    {
        const string Source = @"
public async Task RunAsync() {
	AcceptValue(() => {
		var x = Guid.NewGuid();

		return Task.FromResult(2);
	});
}";

        return CreateProjectBuilder()
            .WithSourceCode(string.Format(CultureInfo.InvariantCulture, Scaffold, Source))
            .ValidateAsync();
    }
}
