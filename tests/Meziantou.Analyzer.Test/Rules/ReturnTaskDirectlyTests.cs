using System.Globalization;
using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public class ReturnTaskDirectlyTests
{
	private const string Scaffold = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnitTests;

public class Test {{
	private readonly DataService _dataService;

	{0}

	public Task DoSomethingAsync() {{
		return Task.Delay(1000);
	}}

	public Task<int> GetSomethingAsync() {{
		return Task.FromResult(2);
	}}

	public Task<List<int>> GetListAsync() {{
		return Task.FromResult(new List<int>() {{ 1, 2, 3 }});
	}}

	public ValueTask GetValueTaskAsync() {{
		return new ValueTask();
	}}

	public ValueTask<int> GetValueTaskWithValueAsync() {{
		return new ValueTask<int>(3);
	}}

	public Task Accept(Func<Task> func) {{
		return func();
	}}

	public Task<T> AcceptValue<T>(Func<Task<T>> func) {{
		return func();
	}}
}}

public class MyDisposable : IDisposable, IAsyncDisposable {{
	public void Dispose() {{
	}}

	public ValueTask DisposeAsync() {{
		return default;
	}}
}}

public class DataService {{
	public Task<List<int>> GetListAsync() {{
		return Task.FromResult(new List<int>() {{ 1, 2, 3 }});
	}}
}}";

	// I know regions are bad, please don't report me to the region police.

	#region ShouldRaise

	private const string SingleAwait = @"/*SingleAwait*/
async Task RunAsync() {
	{|#0:await DoSomethingAsync()|};
}";

	private const string SingleAwaitFixed = @"/*SingleAwait*/
Task RunAsync() {
	return DoSomethingAsync();
}";

	private const string SingleAwait2 = @"/*SingleAwait2*/
public async Task RunAsync() {
	{|#0:await Task.Delay(1000)|};
}";

	private const string SingleAwaitFixed2 = @"/*SingleAwait2*/
public Task RunAsync() {
	return Task.Delay(1000);
}";

	private const string SingleValueTaskAwait = @"/*SingleValueTaskAwait*/
async ValueTask RunAsync() {
	{|#0:await GetValueTaskAsync()|};
}";

	private const string SingleValueTaskAwaitFixed = @"/*SingleValueTaskAwait*/
ValueTask RunAsync() {
	return GetValueTaskAsync();
}";

	private const string SingleAwaitExpression = @"/*SingleAwaitExpression*/
public async Task RunAsync() => {|#0:await DoSomethingAsync()|};";

	private const string SingleAwaitExpressionFixed = @"/*SingleAwaitExpression*/
public Task RunAsync() => DoSomethingAsync();";

	private const string SingleValueTaskAwaitExpression = @"/*SingleValueTaskAwaitExpression*/
public async ValueTask RunAsync() => {|#0:await GetValueTaskAsync()|};";

	private const string SingleValueTaskAwaitExpressionFixed = @"/*SingleValueTaskAwaitExpression*/
public ValueTask RunAsync() => GetValueTaskAsync();";

	private const string SingleAwaitWithReturnExpression = @"/*SingleAwaitWithReturnExpression*/
public async Task<int> RunAsync() => {|#0:await GetSomethingAsync()|};";

	private const string SingleAwaitWithReturnExpressionFixed = @"/*SingleAwaitWithReturnExpression*/
public Task<int> RunAsync() => GetSomethingAsync();";

	private const string SingleAwaitWithValueTaskReturnExpression = @"/*SingleAwaitWithValueTaskReturnExpression*/
public async ValueTask<int> RunAsync() => {|#0:await GetValueTaskWithValueAsync()|};";

	private const string SingleAwaitWithValueTaskReturnExpressionFixed = @"/*SingleAwaitWithValueTaskReturnExpression*/
public ValueTask<int> RunAsync() => GetValueTaskWithValueAsync();";

	private const string SingleAwaitWithReturn = @"/*SingleAwaitWithReturn*/
public async Task<int> RunAsync() {
	return {|#0:await GetSomethingAsync()|};
}";

	private const string SingleAwaitWithReturnFixed = @"/*SingleAwaitWithReturn*/
public Task<int> RunAsync() {
	return GetSomethingAsync();
}";

	private const string SingleValueTaskAwaitWithReturn = @"/*SingleValueTaskAwaitWithReturn*/
public async ValueTask<int> RunAsync() {
	return {|#0:await GetValueTaskWithValueAsync()|};
}";

	private const string SingleValueTaskAwaitWithReturnFixed = @"/*SingleValueTaskAwaitWithReturn*/
public ValueTask<int> RunAsync() {
	return GetValueTaskWithValueAsync();
}";

	private const string MultipleStatementsWithSingleAwait = @"/*MultipleStatementsWithSingleAwait*/
public async Task RunAsync() {
	var guid = Guid.NewGuid();

	{|#0:await DoSomethingAsync()|};
}";

	private const string MultipleStatementsWithSingleAwaitFixed = @"/*MultipleStatementsWithSingleAwait*/
public Task RunAsync() {
	var guid = Guid.NewGuid();

	return DoSomethingAsync();
}";

	private const string MultipleStatementsWithReturn = @"/*MultipleStatementsWithReturn*/
public async Task<int> RunAsync() {
	var task = GetSomethingAsync();
	Console.WriteLine(task.Id);

	return {|#0:await task|};
}";

	private const string MultipleStatementsWithReturnFixed = @"/*MultipleStatementsWithReturn*/
public Task<int> RunAsync() {
	var task = GetSomethingAsync();
	Console.WriteLine(task.Id);

	return task;
}";

	private const string MultipleReturnStatements = @"/*MultipleReturnStatements*/
public async Task<int> RunAsync() {
	var guid = Guid.NewGuid().ToString();
	if(guid.StartsWith(""a"")) {
		return {|#0:await Task.FromResult(6)|};
	}

	if(guid.StartsWith(""b"")) {
		return {|#1:await Task.FromResult(3)|};
	}

	return {|#2:await GetSomethingAsync()|};
}";

	private const string MultipleReturnStatementsFixed = @"/*MultipleReturnStatements*/
public Task<int> RunAsync() {
	var guid = Guid.NewGuid().ToString();
	if(guid.StartsWith(""a"")) {
		return Task.FromResult(6);
	}

	if(guid.StartsWith(""b"")) {
		return Task.FromResult(3);
	}

	return GetSomethingAsync();
}";

	private const string WithNonRelevantUsingBlock = @"/*WithNonRelevantUsingBlock*/
public async Task<int> RunAsync() {
	var task = GetSomethingAsync();
	using (var _ = new MyDisposable()) {
		Console.WriteLine(task.Id);
	}

	if(task.IsCompleted) {
		return {|#0:await Task.FromResult(5)|};
	}

	return {|#1:await task|};
}";

	private const string WithNonRelevantUsingBlockFixed = @"/*WithNonRelevantUsingBlock*/
public Task<int> RunAsync() {
	var task = GetSomethingAsync();
	using (var _ = new MyDisposable()) {
		Console.WriteLine(task.Id);
	}

	if(task.IsCompleted) {
		return Task.FromResult(5);
	}

	return task;
}";

	private const string WithNonRelevantTryBlock = @"/*WithNonRelevantTryBlock*/
public async Task RunAsync() {
	var task = DoSomethingAsync();
	try {
		Console.WriteLine(task.Id);
	} catch (Exception) {
	}

	{|#0:await task|};
}";

	private const string WithNonRelevantTryBlockFixed = @"/*WithNonRelevantTryBlock*/
public Task RunAsync() {
	var task = DoSomethingAsync();
	try {
		Console.WriteLine(task.Id);
	} catch (Exception) {
	}

	return task;
}";

	private const string WithMultipleAwaits = @"/*WithMultipleAwaits*/
private async Task RunAsync()
{
	var x = new Random().Next();
	if(x == 3)
	{
		{|#0:await DoSomethingAsync()|};
		return;
	}

	{|#1:await Task.Delay(1000)|};
}";

	private const string WithMultipleAwaitsFixed = @"/*WithMultipleAwaits*/
private Task RunAsync()
{
	var x = new Random().Next();
	if(x == 3)
	{
		return DoSomethingAsync();
	}

	return Task.Delay(1000);
}";

	private const string WithLocalFunction = @"/*WithLocalFunction*/
private async Task RunAsync()
{
	var x = new Random().Next();
	if(x == 3)
	{
		{|#0:await DoSomethingAsync()|};
		return;
	}

	async Task LocalFuncAsync() {
		if(x == 4) {
			await Task.Delay(500);
		}

		await Task.CompletedTask;
	}

	{|#1:await LocalFuncAsync()|};
}";

	private const string WithLocalFunctionFixed = @"/*WithLocalFunction*/
private Task RunAsync()
{
	var x = new Random().Next();
	if(x == 3)
	{
		return DoSomethingAsync();
	}

	async Task LocalFuncAsync() {
		if(x == 4) {
			await Task.Delay(500);
		}

		await Task.CompletedTask;
	}

	return LocalFuncAsync();
}";

	private const string LocalFunction = @"/*LocalFunction*/
private Task RunAsync()
{
	async Task ComputeAsync() {
		{|#0:await Task.Delay(1000)|};
	}

	return ComputeAsync();
}";

	private const string LocalFunctionFixed = @"/*LocalFunction*/
private Task RunAsync()
{
	Task ComputeAsync() {
		return Task.Delay(1000);
	}

	return ComputeAsync();
}";

	private const string WithConfigureAwait = @"/*WithConfigureAwait*/
private async Task RunAsync()
{
	var x = new Random().Next();
	if(x == 3)
	{
		{|#0:await DoSomethingAsync().ConfigureAwait(false)|};
		return;
	}

	{|#1:await Task.Delay(1000)|};
}";

	private const string WithConfigureAwaitFixed = @"/*WithConfigureAwait*/
private Task RunAsync()
{
	var x = new Random().Next();
	if(x == 3)
	{
		return DoSomethingAsync();
	}

	return Task.Delay(1000);
}";

	private const string WithUnrelatedUsingStatement = @"/*WithUnrelatedUsingStatement*/
private async Task RunAsync()
{
	var x = new Random().Next();
	if(x == 3)
	{
		using var _ = new MyDisposable();
		Console.WriteLine(2);
	}

	{|#0:await Task.Delay(1000)|};
}";

	private const string WithUnrelatedUsingStatementFixed = @"/*WithUnrelatedUsingStatement*/
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

	private const string LambdaExpression = @"/*LambdaExpression*/
private Task RunAsync()
{
	return Accept(async () => {|#0:await DoSomethingAsync()|});
}";

	private const string LambdaExpressionFixed = @"/*LambdaExpression*/
private Task RunAsync()
{
	return Accept(() => DoSomethingAsync());
}";

	private const string LambdaExpressionWithReturn = @"/*LambdaExpressionWithReturn*/
private Task RunAsync()
{
	return AcceptValue(async () => {|#0:await GetSomethingAsync()|});
}";

	private const string LambdaExpressionWithReturnFixed = @"/*LambdaExpressionWithReturn*/
private Task RunAsync()
{
	return AcceptValue(() => GetSomethingAsync());
}";

	private const string LambdaBlock = @"/*LambdaBlock*/
private Task RunAsync()
{
	return Accept(async () =>
	{
		var guid = Guid.NewGuid();
		if(guid == Guid.NewGuid()) {
			{|#0:await DoSomethingAsync()|};
			return;
		}

		{|#1:await Task.CompletedTask|};
	});
}";

	private const string LambdaBlockFixed = @"/*LambdaBlock*/
private Task RunAsync()
{
	return Accept(() =>
	{
		var guid = Guid.NewGuid();
		if(guid == Guid.NewGuid()) {
			return DoSomethingAsync();
		}

		return Task.CompletedTask;
	});
}";

	private const string LambdaBlockWithReturn = @"/*LambdaBlockWithReturn*/
private Task RunAsync()
{
	return AcceptValue(async () =>
	{
		var guid = Guid.NewGuid();
		if(guid == Guid.NewGuid()) {
			return {|#0:await GetSomethingAsync()|};
		}

		return {|#1:await Task.FromResult(5)|};
	});
}";

	private const string LambdaBlockWithReturnFixed = @"/*LambdaBlockWithReturn*/
private Task RunAsync()
{
	return AcceptValue(() =>
	{
		var guid = Guid.NewGuid();
		if(guid == Guid.NewGuid()) {
			return GetSomethingAsync();
		}

		return Task.FromResult(5);
	});
}";

	#endregion

	#region ShouldNotRaise

	private const string NonTaskMethod = @"/*NonTaskMethod*/
public void Run() {
}";

	private const string NonTaskMethod2 = @"/*NonTaskMethod2*/
public int Run() {
	return 5;
}";

	private const string NoAwait = @"/*NoAwait*/
public async Task Run() {
	Console.WriteLine(""Hello World"");
}";

	private const string AsyncVoidMethod = @"/*AsyncVoidMethod*/
public async void Run() {
	await Task.CompletedTask;
}";

	private const string CorrectUsage = @"/*CorrectUsage*/
public Task RunAsync() {
	return DoSomethingAsync();
}";

	private const string CorrectUsage2 = @"/*CorrectUsage2*/
public Task<int> RunAsync() {
	return GetSomethingAsync();
}";

	private const string CorrectUsageWithMultipleStatements = @"/*CorrectUsageWithMultipleStatements*/
public Task RunAsync() {
	var guid = Guid.NewGuid();
	Console.WriteLine(guid);

	return DoSomethingAsync();
}";

	private const string CorrectUsageWithMultipleStatements2 = @"/*CorrectUsageWithMultipleStatements2*/
public Task<int> RunAsync() {
	var guid = Guid.NewGuid();
	Console.WriteLine(guid);

	return GetSomethingAsync();
}";

	private const string CorrectUsageWithMultipleReturnStatements = @"/*CorrectUsageWithMultipleReturnStatements*/
public Task RunAsync() {
	var guid = Guid.NewGuid().ToString();
	if(guid.StartsWith(""a"")) {
		return DoSomethingAsync();
	}

	return Task.CompletedTask;
}";

	private const string CorrectUsageWithMultipleReturnStatements2 = @"/*CorrectUsageWithMultipleReturnStatements2*/
public Task RunAsync() {
	var guid = Guid.NewGuid().ToString();
	if(guid.StartsWith(""a"")) {
		return Task.FromResult(5);
	}

	return GetSomethingAsync();
}";

	private const string MixedReturn = @"/*MixedReturn*/
public async Task<int> RunAsync()
{
	var guid = Guid.NewGuid();
	if (guid.ToString().StartsWith(""a""))
	{
		return await Task.FromResult(2);
	}

	return 6;
}";

	private const string MixedAwaits = @"/*MixedAwaits*/
private async Task<int> RunAsync()
{
    foreach (var i in Enumerable.Range(0, 10))
    {
        var result = await Task.FromResult(i);
    }

    return await GetSomethingAsync();
}";

	private const string InUsingBlock = @"/*InUsingBlock*/
public async Task RunAsync() {
	using (var _ = new MyDisposable()) {
		await DoSomethingAsync();
	}
}";

	private const string InUsingBlockWithReturn = @"/*InUsingBlockWithReturn*/
public async Task RunAsync() {
	using (var _ = new MyDisposable()) {
		await DoSomethingAsync();
		return;
	}
}";

	private const string InUsingBlockWithMultipleStatements = @"/*InUsingBlockWithMultipleStatements*/
public async Task RunAsync() {
	using (var _ = new MyDisposable()) {
		await DoSomethingAsync();
	}

	await Task.CompletedTask;
}";

	private const string InUsingBlockWithMultipleStatementsWithReturn = @"/*InUsingBlockWithMultipleStatementsWithReturn*/
public async Task RunAsync() {
	using (var _ = new MyDisposable()) {
		await DoSomethingAsync();
		return;
	}

	await Task.CompletedTask;
}";

	private const string InUsingStatement = @"/*InUsingStatement*/
public async Task RunAsync() {
	using var _ = new MyDisposable();
	await DoSomethingAsync();
}";

	private const string InNestedUsingStatement = @"/*InNestedUsingStatement*/
public async Task RunAsync() {
	var x = new Random().Next();
	if(x == 3)
	{
		using var _ = new MyDisposable();
		await DoSomethingAsync();
		return;
	}

	await Task.CompletedTask;
}";

	private const string InNestedUsingStatement2 = @"/*InNestedUsingStatement2*/
public async Task<int> RunAsync() {
	var x = new Random().Next();
	if(x == 3)
	{
		using var _ = new MyDisposable();
		return await GetSomethingAsync();
	}

	return await Task.FromResult(5);
}";

	private const string InAwaitUsingBlock = @"/*InAwaitUsingBlock*/
public async Task RunAsync() {
	await using (var _ = new MyDisposable()) {
		await DoSomethingAsync();
	}
}";

	private const string InAwaitUsingStatement = @"/*InAwaitUsingStatement*/
public async Task RunAsync() {
	await using var _ = new MyDisposable();
	await DoSomethingAsync();
}";

	private const string InTryBlock = @"/*InTryBlock*/
public async Task RunAsync() {
	try {
		await Task.Delay(1000);
	} catch (Exception) {
	}
}";

	private const string InTryBlockWithReturn = @"/*InTryBlockWithReturn*/
public async Task RunAsync() {
	try {
		await Task.Delay(1000);
		return;
	} catch (Exception) {
	}
}";

	private const string InTryBlockWithMultipleStatements = @"/*InTryBlockWithMultipleStatements*/
public async Task RunAsync() {
	try {
		await Task.Delay(1000);
	} catch (Exception) {
	}

	await DoSomethingAsync();
}";

	private const string InTryBlockWithMultipleStatementsWithReturn = @"/*InTryBlockWithMultipleStatementsWithReturn*/
public async Task RunAsync() {
	try {
		await Task.Delay(1000);
		return;
	} catch (Exception) {
	}

	await DoSomethingAsync();
}";

	private const string InTryBlockWithValueReturn = @"/*InTryBlockWithValueReturn*/
public async Task<int> RunAsync()
{
	try
	{
		return await GetSomethingAsync();
	}
	catch (Exception)
	{
		return 2;
	}
}";

	private const string MultipleAwaitExpressions = @"/*MultipleAwaitExpressions*/
public async Task RunAsync() {
	var x = 2;
	await Task.Delay(1000);
	Console.WriteLine(x);
	await DoSomethingAsync();
}";

	private const string MultipleAwaitExpressionsNested = @"/*MultipleAwaitExpressionsNested*/
public async Task RunAsync() {
	var x = 2;
	if(x % 2 == 0) {
		await Task.Delay(1000);
	}

	Console.WriteLine(x);
	await DoSomethingAsync();
}";

	private const string MultipleAwaitExpressionsNestedWithReturn = @"/*MultipleAwaitExpressionsNestedWithReturn*/
private async Task<int> RunAsync()
{
	var x = new Random().Next();
	if(x == 3)
	{
		await DoSomethingAsync();
		return 2;
	}

	return await Task.FromResult(1000);
}";

	private const string MultipleAwaitExpressionsInLoop = @"/*MultipleAwaitExpressionsInLoop*/
public async Task RunAsync() {
	for(var i = 0; i < 10; i++) {
		await Task.Delay(200);
	}
}";

	private const string CovariantReturnType = @"/*CovariantReturnType*/
public async Task<IEnumerable<int>> RunAsync() {
	return await GetListAsync();
}";

	private const string CovariantReturnTypeWithMethodCall = @"/*CovariantReturnTypeWithMethodCall*/
public async Task<IEnumerable<int>> RunAsync() {
	return await _dataService.GetListAsync();
}";

	private const string CovariantValueTaskReturnType = @"/*CovariantValueTaskReturnType*/
public async Task RunAsync() {
	await GetValueTaskAsync();
}";

	private const string CovariantValueTaskReturnTypeWithConfigureAwait = @"/*CovariantValueTaskReturnTypeWithConfigureAwait*/
public async Task RunAsync() {
	await GetValueTaskAsync().ConfigureAwait(false);
}";

	private const string CovariantValueTaskExpressionReturnType = @"/*CovariantValueTaskExpressionReturnType*/
public async Task RunAsync() => await GetValueTaskAsync();";

	private const string CovariantValueTaskWithValueReturnType = @"/*CovariantValueTaskWithValueReturnType*/
public async Task<int> RunAsync() {
	return await GetValueTaskWithValueAsync();
}";

	private const string CovariantValueTaskWithValueExpressionReturnType = @"/*CovariantValueTaskWithValueExpressionReturnType*/
public async Task<int> RunAsync() => await GetValueTaskWithValueAsync();";

	private const string CorrectLambdaExpression = @"/*CorrectLambdaExpression*/
public void Run() {
	Accept(() => DoSomethingAsync());
}";

	private const string CorrectLambdaExpression2 = @"/*CorrectLambdaExpression2*/
public void Run() {
	Accept(DoSomethingAsync);
}";

	private const string CorrectLambdaExpressionWithReturn = @"/*CorrectLambdaExpressionWithReturn*/
public void Run() {
	AcceptValue(() => GetSomethingAsync());
}";

	private const string CorrectLambdaExpressionWithReturn2 = @"/*CorrectLambdaExpressionWithReturn2*/
public void Run() {
	AcceptValue(GetSomethingAsync);
}";

	private const string CorrectLambdaBlock = @"/*CorrectLambdaBlock*/
public async Task RunAsync() {
	Accept(() => {
		var x = Guid.NewGuid();

		return DoSomethingAsync();
	});
}";

	private const string CorrectLambdaBlockWithReturn = @"/*CorrectLambdaBlockWithReturn*/
public async Task RunAsync() {
	AcceptValue(() => {
		var x = Guid.NewGuid();

		return GetSomethingAsync();
	});
}";

	#endregion

	[Theory]
	[InlineData(SingleAwait, SingleAwaitFixed)]
	[InlineData(SingleAwait2, SingleAwaitFixed2)]
	[InlineData(SingleValueTaskAwait, SingleValueTaskAwaitFixed)]
	[InlineData(SingleAwaitExpression, SingleAwaitExpressionFixed)]
	[InlineData(SingleValueTaskAwaitExpression, SingleValueTaskAwaitExpressionFixed)]
	[InlineData(SingleAwaitWithReturnExpression, SingleAwaitWithReturnExpressionFixed)]
	[InlineData(SingleAwaitWithValueTaskReturnExpression, SingleAwaitWithValueTaskReturnExpressionFixed)]
	[InlineData(SingleAwaitWithReturn, SingleAwaitWithReturnFixed)]
	[InlineData(SingleValueTaskAwaitWithReturn, SingleValueTaskAwaitWithReturnFixed)]
	[InlineData(MultipleStatementsWithSingleAwait, MultipleStatementsWithSingleAwaitFixed)]
	[InlineData(MultipleStatementsWithReturn, MultipleStatementsWithReturnFixed)]
	[InlineData(MultipleReturnStatements, MultipleReturnStatementsFixed, 3)]
	[InlineData(WithNonRelevantUsingBlock, WithNonRelevantUsingBlockFixed, 2)]
	[InlineData(WithNonRelevantTryBlock, WithNonRelevantTryBlockFixed)]
	[InlineData(WithMultipleAwaits, WithMultipleAwaitsFixed, 2)]
	[InlineData(WithLocalFunction, WithLocalFunctionFixed, 2)]
	[InlineData(LocalFunction, LocalFunctionFixed)]
	[InlineData(WithConfigureAwait, WithConfigureAwaitFixed, 2)]
	[InlineData(WithUnrelatedUsingStatement, WithUnrelatedUsingStatementFixed)]
	[InlineData(LambdaExpression, LambdaExpressionFixed)]
	[InlineData(LambdaExpressionWithReturn, LambdaExpressionWithReturnFixed)]
	[InlineData(LambdaBlock, LambdaBlockFixed, 2)]
	[InlineData(LambdaBlockWithReturn, LambdaBlockWithReturnFixed, 2)]
	public Task ShouldRaiseAsync(string method, string fixedMethod, int diagnosticLocations = 1)
	{
		var source = string.Format(CultureInfo.InvariantCulture, Scaffold, method);
		var fixedSource = string.Format(CultureInfo.InvariantCulture, Scaffold, fixedMethod);

        return new ProjectBuilder()
            .WithAnalyzer<ReturnTaskDirectlyAnalyzer>()
            .WithCodeFixProvider<ReturnTaskDirectlyFixer>()
            .WithSourceCode(source)
            .ShouldFixCodeWith(fixedSource)
            .AddAsyncInterfaceApi()
            .ValidateAsync();
	}

	[Theory]
	[InlineData(NonTaskMethod)]
	[InlineData(NonTaskMethod2)]
	[InlineData(NoAwait)]
	[InlineData(AsyncVoidMethod)]
	[InlineData(CorrectUsage)]
	[InlineData(CorrectUsage2)]
	[InlineData(CorrectUsageWithMultipleStatements)]
	[InlineData(CorrectUsageWithMultipleStatements2)]
	[InlineData(CorrectUsageWithMultipleReturnStatements)]
	[InlineData(CorrectUsageWithMultipleReturnStatements2)]
	[InlineData(MixedReturn)]
	[InlineData(MixedAwaits)]
	[InlineData(InUsingBlock)]
	[InlineData(InUsingBlockWithReturn)]
	[InlineData(InUsingBlockWithMultipleStatements)]
	[InlineData(InUsingBlockWithMultipleStatementsWithReturn)]
	[InlineData(InUsingStatement)]
	[InlineData(InNestedUsingStatement)]
	[InlineData(InNestedUsingStatement2)]
	[InlineData(InAwaitUsingBlock)]
	[InlineData(InAwaitUsingStatement)]
	[InlineData(InTryBlock)]
	[InlineData(InTryBlockWithReturn)]
	[InlineData(InTryBlockWithMultipleStatements)]
	[InlineData(InTryBlockWithMultipleStatementsWithReturn)]
	[InlineData(InTryBlockWithValueReturn)]
	[InlineData(MultipleAwaitExpressions)]
	[InlineData(MultipleAwaitExpressionsNested)]
	[InlineData(MultipleAwaitExpressionsNestedWithReturn)]
	[InlineData(MultipleAwaitExpressionsInLoop)]
	[InlineData(CovariantReturnType)]
	[InlineData(CovariantReturnTypeWithMethodCall)]
	[InlineData(CovariantValueTaskReturnType)]
	[InlineData(CovariantValueTaskReturnTypeWithConfigureAwait)]
	[InlineData(CovariantValueTaskExpressionReturnType)]
	[InlineData(CovariantValueTaskWithValueReturnType)]
	[InlineData(CovariantValueTaskWithValueExpressionReturnType)]
	[InlineData(CorrectLambdaExpression)]
	[InlineData(CorrectLambdaExpression2)]
	[InlineData(CorrectLambdaExpressionWithReturn)]
	[InlineData(CorrectLambdaExpressionWithReturn2)]
	[InlineData(CorrectLambdaBlock)]
	[InlineData(CorrectLambdaBlockWithReturn)]
	public Task ShouldNotRaiseAsync(string method)
	{
		var source = string.Format(CultureInfo.InvariantCulture, Scaffold, method);

        return new ProjectBuilder()
            .WithAnalyzer<ReturnTaskDirectlyAnalyzer>()
            .WithSourceCode(source)
            .AddAsyncInterfaceApi()
            .ValidateAsync();
    }
}
