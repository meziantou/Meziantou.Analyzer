# Meziantou.Analyzer

[![Meziantou.Analyzer on NuGet](https://img.shields.io/nuget/v/Meziantou.Analyzer.svg)](https://www.nuget.org/packages/Meziantou.Analyzer/)
[![Meziantou.Analyzer on NuGet](https://img.shields.io/nuget/dt/Meziantou.Analyzer)](https://www.nuget.org/packages/Meziantou.Analyzer/)

A Roslyn analyzer to enforce some good practices in C# in terms of design, usage, security, performance, and style.

## Installation

Install the NuGet package <https://www.nuget.org/packages/Meziantou.Analyzer/>

## Rules

If you are already using other analyzers, you can check [which rules are duplicated with well-known analyzers](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/comparison-with-other-analyzers.md)

<!-- rules -->

|Id|Category|Description|Severity|Is enabled|Code fix|
|--|--------|-----------|:------:|:--------:|:------:|
|[MA0001](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0001.md)|Usage|StringComparison is missing|ℹ️|✔️|✔️|
|[MA0002](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0002.md)|Usage|IEqualityComparer\<string\> or IComparer\<string\> is missing|⚠️|✔️|✔️|
|[MA0003](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0003.md)|Style|Add parameter name to improve readability|ℹ️|✔️|✔️|
|[MA0004](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0004.md)|Usage|Use Task.ConfigureAwait|⚠️|✔️|✔️|
|[MA0005](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0005.md)|Performance|Use Array.Empty\<T\>()|⚠️|✔️|✔️|
|[MA0006](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0006.md)|Usage|Use String.Equals instead of equality operator|⚠️|✔️|✔️|
|[MA0007](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0007.md)|Style|Add a comma after the last value|ℹ️|✔️|✔️|
|[MA0008](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0008.md)|Performance|Add StructLayoutAttribute|⚠️|✔️|✔️|
|[MA0009](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0009.md)|Security|Add regex evaluation timeout|⚠️|✔️|❌|
|[MA0010](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0010.md)|Design|Mark attributes with AttributeUsageAttribute|⚠️|✔️|✔️|
|[MA0011](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0011.md)|Usage|IFormatProvider is missing|⚠️|✔️|✔️|
|[MA0012](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0012.md)|Design|Do not raise reserved exception type|⚠️|✔️|❌|
|[MA0013](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0013.md)|Design|Types should not extend System.ApplicationException|⚠️|✔️|❌|
|[MA0014](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0014.md)|Design|Do not raise System.ApplicationException type|⚠️|✔️|❌|
|[MA0015](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0015.md)|Usage|Specify the parameter name in ArgumentException|⚠️|✔️|❌|
|[MA0016](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0016.md)|Design|Prefer using collection abstraction instead of implementation|⚠️|✔️|❌|
|[MA0017](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0017.md)|Design|Abstract types should not have public or internal constructors|⚠️|✔️|✔️|
|[MA0018](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0018.md)|Design|Do not declare static members on generic types (deprecated; use CA1000 instead)|ℹ️|✔️|❌|
|[MA0019](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0019.md)|Usage|Use EventArgs.Empty|⚠️|✔️|✔️|
|[MA0020](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0020.md)|Performance|Use direct methods instead of LINQ methods|ℹ️|✔️|✔️|
|[MA0021](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0021.md)|Usage|Use StringComparer.GetHashCode instead of string.GetHashCode|⚠️|✔️|✔️|
|[MA0022](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0022.md)|Design|Return Task.FromResult instead of returning null|⚠️|✔️|✔️|
|[MA0023](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0023.md)|Performance|Add RegexOptions.ExplicitCapture|⚠️|✔️|✔️|
|[MA0024](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0024.md)|Usage|Use an explicit StringComparer when possible|⚠️|✔️|✔️|
|[MA0025](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0025.md)|Design|Implement the functionality instead of throwing NotImplementedException|⚠️|✔️|❌|
|[MA0026](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0026.md)|Design|Fix TODO comment|⚠️|✔️|❌|
|[MA0027](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0027.md)|Usage|Prefer rethrowing an exception implicitly|⚠️|✔️|✔️|
|[MA0028](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0028.md)|Performance|Optimize StringBuilder usage|ℹ️|✔️|✔️|
|[MA0029](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0029.md)|Performance|Combine LINQ methods|ℹ️|✔️|✔️|
|[MA0030](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0030.md)|Performance|Remove useless OrderBy call|⚠️|✔️|✔️|
|[MA0031](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0031.md)|Performance|Optimize Enumerable.Count() usage|ℹ️|✔️|✔️|
|[MA0032](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0032.md)|Usage|Use an overload with a CancellationToken argument|ℹ️|❌|❌|
|[MA0033](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0033.md)|Design|Do not tag instance fields with ThreadStaticAttribute|⚠️|✔️|❌|
|[MA0035](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0035.md)|Usage|Do not use dangerous threading methods|⚠️|✔️|❌|
|[MA0036](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0036.md)|Design|Make class static|ℹ️|✔️|✔️|
|[MA0037](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0037.md)|Usage|Remove empty statement|❌|✔️|✔️|
|[MA0038](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0038.md)|Design|Make method static (deprecated, use CA1822 instead)|ℹ️|✔️|✔️|
|[MA0039](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0039.md)|Security|Do not write your own certificate validation method|❌|✔️|❌|
|[MA0040](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0040.md)|Usage|Forward the CancellationToken parameter to methods that take one|ℹ️|✔️|✔️|
|[MA0041](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0041.md)|Design|Make property static (deprecated, use CA1822 instead)|ℹ️|✔️|✔️|
|[MA0042](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0042.md)|Design|Do not use blocking calls in an async method|ℹ️|✔️|✔️|
|[MA0043](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0043.md)|Usage|Use nameof operator in ArgumentException|ℹ️|✔️|✔️|
|[MA0044](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0044.md)|Performance|Remove useless ToString call|ℹ️|✔️|✔️|
|[MA0045](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0045.md)|Design|Do not use blocking calls in a sync method (need to make calling method async)|ℹ️|❌|✔️|
|[MA0046](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0046.md)|Design|Use EventHandler\<T\> to declare events|⚠️|✔️|❌|
|[MA0047](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0047.md)|Design|Declare types in namespaces|⚠️|✔️|❌|
|[MA0048](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0048.md)|Design|File name must match type name|⚠️|✔️|❌|
|[MA0049](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0049.md)|Design|Type name should not match containing namespace|❌|✔️|❌|
|[MA0050](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0050.md)|Design|Validate arguments correctly in iterator methods|ℹ️|✔️|✔️|
|[MA0051](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0051.md)|Design|Method is too long|⚠️|✔️|❌|
|[MA0052](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0052.md)|Performance|Replace constant Enum.ToString with nameof|ℹ️|✔️|✔️|
|[MA0053](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0053.md)|Design|Make class or record sealed|ℹ️|✔️|✔️|
|[MA0054](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0054.md)|Design|Embed the caught exception as innerException|⚠️|✔️|✔️|
|[MA0055](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0055.md)|Design|Do not use finalizer|⚠️|✔️|❌|
|[MA0056](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0056.md)|Design|Do not call overridable members in constructor|⚠️|✔️|❌|
|[MA0057](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0057.md)|Naming|Class name should end with 'Attribute'|ℹ️|✔️|✔️|
|[MA0058](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0058.md)|Naming|Class name should end with 'Exception'|ℹ️|✔️|✔️|
|[MA0059](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0059.md)|Naming|Class name should end with 'EventArgs'|ℹ️|✔️|✔️|
|[MA0060](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0060.md)|Design|The value returned by Stream.Read/Stream.ReadAsync is not used|⚠️|✔️|❌|
|[MA0061](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0061.md)|Design|Method overrides should not change default values|⚠️|✔️|✔️|
|[MA0062](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0062.md)|Design|Non-flags enums should not be marked with "FlagsAttribute"|⚠️|✔️|✔️|
|[MA0063](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0063.md)|Performance|Use Where before OrderBy|ℹ️|✔️|✔️|
|[MA0064](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0064.md)|Design|Avoid locking on publicly accessible instance|⚠️|✔️|❌|
|[MA0065](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0065.md)|Performance|Default ValueType.Equals or HashCode is used for struct equality|⚠️|✔️|❌|
|[MA0066](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0066.md)|Performance|Hash table unfriendly type is used in a hash table|⚠️|✔️|❌|
|[MA0067](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0067.md)|Design|Use Guid.Empty|ℹ️|✔️|✔️|
|[MA0068](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0068.md)|Design|Invalid parameter name for nullable attribute|⚠️|✔️|❌|
|[MA0069](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0069.md)|Design|Non-constant static fields should not be visible|⚠️|✔️|❌|
|[MA0070](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0070.md)|Design|Obsolete attributes should include explanations|⚠️|✔️|❌|
|[MA0071](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0071.md)|Style|Avoid using redundant else|ℹ️|✔️|✔️|
|[MA0072](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0072.md)|Design|Do not throw from a finally block|⚠️|✔️|❌|
|[MA0073](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0073.md)|Style|Avoid comparison with bool constant|ℹ️|✔️|✔️|
|[MA0074](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0074.md)|Usage|Avoid implicit culture-sensitive methods|⚠️|✔️|✔️|
|[MA0075](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0075.md)|Design|Do not use implicit culture-sensitive ToString|ℹ️|✔️|❌|
|[MA0076](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0076.md)|Design|Do not use implicit culture-sensitive ToString in interpolated strings|ℹ️|✔️|✔️|
|[MA0077](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0077.md)|Design|A class that provides Equals(T) should implement IEquatable\<T\>|⚠️|✔️|✔️|
|[MA0078](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0078.md)|Performance|Use 'Cast' instead of 'Select' to cast|ℹ️|✔️|✔️|
|[MA0079](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0079.md)|Usage|Forward the CancellationToken using .WithCancellation()|ℹ️|✔️|✔️|
|[MA0080](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0080.md)|Usage|Use a cancellation token using .WithCancellation()|ℹ️|❌|❌|
|[MA0081](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0081.md)|Design|Method overrides should not omit params keyword|⚠️|✔️|✔️|
|[MA0082](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0082.md)|Design|NaN should not be used in comparisons|⚠️|✔️|✔️|
|[MA0083](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0083.md)|Design|ConstructorArgument parameters should exist in constructors|⚠️|✔️|❌|
|[MA0084](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0084.md)|Design|Local variables should not hide other symbols|⚠️|✔️|❌|
|[MA0085](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0085.md)|Usage|Anonymous delegates should not be used to unsubscribe from Events|⚠️|✔️|❌|
|[MA0086](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0086.md)|Design|Do not throw from a finalizer|⚠️|✔️|❌|
|[MA0087](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0087.md)|Design|Parameters with \[DefaultParameterValue\] attributes should also be marked \[Optional\]|⚠️|✔️|✔️|
|[MA0088](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0088.md)|Design|Use \[DefaultParameterValue\] instead of \[DefaultValue\]|⚠️|✔️|✔️|
|[MA0089](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0089.md)|Performance|Optimize string method usage|ℹ️|✔️|✔️|
|[MA0090](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0090.md)|Design|Remove empty else/finally block|ℹ️|✔️|✔️|
|[MA0091](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0091.md)|Usage|Sender should be 'this' for instance events|⚠️|✔️|✔️|
|[MA0092](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0092.md)|Usage|Sender should be 'null' for static events|⚠️|✔️|❌|
|[MA0093](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0093.md)|Usage|EventArgs should not be null when raising an event|⚠️|✔️|✔️|
|[MA0094](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0094.md)|Design|A class that provides CompareTo(T) should implement IComparable\<T\>|⚠️|✔️|✔️|
|[MA0095](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0095.md)|Design|A class that implements IEquatable\<T\> should override Equals(object)|⚠️|✔️|✔️|
|[MA0096](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0096.md)|Design|A class that implements IComparable\<T\> should also implement IEquatable\<T\>|⚠️|✔️|✔️|
|[MA0097](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0097.md)|Design|A class that implements IComparable\<T\> or IComparable should override comparison operators|⚠️|✔️|✔️|
|[MA0098](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0098.md)|Performance|Use indexer instead of LINQ methods|ℹ️|✔️|✔️|
|[MA0099](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0099.md)|Usage|Use Explicit enum value instead of 0|⚠️|✔️|✔️|
|[MA0100](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0100.md)|Usage|Await task before disposing of resources|⚠️|✔️|❌|
|[MA0101](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0101.md)|Usage|String contains an implicit end of line character|👻|✔️|✔️|
|[MA0102](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0102.md)|Design|Make member readonly|ℹ️|✔️|✔️|
|[MA0103](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0103.md)|Usage|Use SequenceEqual instead of equality operator|⚠️|✔️|✔️|
|[MA0104](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0104.md)|Design|Do not create a type with a name from the BCL|⚠️|❌|❌|
|[MA0105](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0105.md)|Performance|Use the lambda parameters instead of using a closure|ℹ️|✔️|✔️|
|[MA0106](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0106.md)|Performance|Avoid closure by using an overload with the 'factoryArgument' parameter|ℹ️|✔️|✔️|
|[MA0107](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0107.md)|Design|Do not use object.ToString|ℹ️|❌|❌|
|[MA0108](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0108.md)|Usage|Remove redundant argument value|ℹ️|✔️|✔️|
|[MA0109](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0109.md)|Design|Consider adding an overload with a Span\<T\> or Memory\<T\>|ℹ️|❌|❌|
|[MA0110](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0110.md)|Performance|Use the Regex source generator|ℹ️|✔️|✔️|
|[MA0111](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0111.md)|Performance|Use string.Create instead of FormattableString|ℹ️|✔️|✔️|
|[MA0112](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0112.md)|Performance|Use 'Count \> 0' instead of 'Any()'|ℹ️|❌|✔️|
|[MA0113](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0113.md)|Design|Use DateTime.UnixEpoch|ℹ️|✔️|✔️|
|[MA0114](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0114.md)|Design|Use DateTimeOffset.UnixEpoch|ℹ️|✔️|✔️|
|[MA0115](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0115.md)|Usage|Unknown component parameter|⚠️|✔️|❌|
|[MA0116](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0116.md)|Design|Parameters with \[SupplyParameterFromQuery\] attributes should also be marked as \[Parameter\]|⚠️|✔️|✔️|
|[MA0117](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0117.md)|Design|Parameters with \[EditorRequired\] attributes should also be marked as \[Parameter\]|⚠️|✔️|✔️|
|[MA0118](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0118.md)|Design|\[JSInvokable\] methods must be public|⚠️|✔️|✔️|
|[MA0119](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0119.md)|Design|JSRuntime must not be used in OnInitialized or OnInitializedAsync|⚠️|✔️|❌|
|[MA0120](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0120.md)|Performance|Use InvokeVoidAsync when the returned value is not used|ℹ️|✔️|✔️|
|[MA0121](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0121.md)|Design|Do not overwrite parameter value|ℹ️|❌|❌|
|[MA0122](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0122.md)|Design|Parameters with \[SupplyParameterFromQuery\] attributes are only valid in routable components (@page)|ℹ️|✔️|❌|
|[MA0123](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0123.md)|Design|Sequence number must be a constant|⚠️|✔️|❌|
|[MA0124](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0124.md)|Design|Log parameter type is not valid|⚠️|✔️|❌|
|[MA0125](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0125.md)|Design|The list of log parameter types contains an invalid type|⚠️|✔️|❌|
|[MA0126](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0126.md)|Design|The list of log parameter types contains a duplicate|⚠️|✔️|❌|
|[MA0127](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0127.md)|Usage|Use String.Equals instead of is pattern|👻|✔️|✔️|
|[MA0128](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0128.md)|Usage|Use 'is' operator instead of SequenceEqual|ℹ️|✔️|✔️|
|[MA0129](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0129.md)|Usage|Await task in using statement|⚠️|✔️|✔️|
|[MA0130](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0130.md)|Usage|GetType() should not be used on System.Type instances|⚠️|✔️|❌|
|[MA0131](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0131.md)|Usage|ArgumentNullException.ThrowIfNull should not be used with non-nullable types|⚠️|✔️|✔️|
|[MA0132](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0132.md)|Design|Do not convert implicitly to DateTimeOffset|⚠️|✔️|❌|
|[MA0133](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0133.md)|Design|Use DateTimeOffset instead of relying on the implicit conversion|ℹ️|✔️|❌|
|[MA0134](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0134.md)|Usage|Observe result of async calls|⚠️|✔️|❌|
|[MA0135](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0135.md)|Design|The log parameter has no configured type|⚠️|❌|❌|
|[MA0136](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0136.md)|Usage|Raw String contains an implicit end of line character|👻|✔️|❌|
|[MA0137](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0137.md)|Design|Use 'Async' suffix when a method returns an awaitable type|⚠️|❌|✔️|
|[MA0138](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0138.md)|Design|Do not use 'Async' suffix when a method does not return an awaitable type|⚠️|❌|✔️|
|[MA0139](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0139.md)|Design|Log parameter type is not valid|⚠️|✔️|❌|
|[MA0140](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0140.md)|Design|Both if and else branch have identical code|⚠️|✔️|❌|
|[MA0141](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0141.md)|Usage|Use pattern matching instead of inequality operators for null check|ℹ️|❌|✔️|
|[MA0142](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0142.md)|Usage|Use pattern matching instead of equality operators for null check|ℹ️|❌|✔️|
|[MA0143](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0143.md)|Design|Primary constructor parameters should be readonly|⚠️|✔️|❌|
|[MA0144](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0144.md)|Performance|Use System.OperatingSystem to check the current OS|⚠️|✔️|✔️|
|[MA0145](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0145.md)|Usage|Signature for \[UnsafeAccessorAttribute\] method is not valid|⚠️|✔️|❌|
|[MA0146](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0146.md)|Usage|Name must be set explicitly on local functions|⚠️|✔️|✔️|
|[MA0147](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0147.md)|Usage|Avoid async void method for delegate|⚠️|✔️|❌|
|[MA0148](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0148.md)|Usage|Use pattern matching instead of equality operators for discrete value|ℹ️|❌|✔️|
|[MA0149](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0149.md)|Usage|Use pattern matching instead of inequality operators for discrete value|ℹ️|❌|✔️|
|[MA0150](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0150.md)|Design|Do not call the default object.ToString explicitly|⚠️|✔️|❌|
|[MA0151](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0151.md)|Usage|DebuggerDisplay must contain valid members|⚠️|✔️|❌|
|[MA0152](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0152.md)|Performance|Use Unwrap instead of using await twice|ℹ️|✔️|✔️|
|[MA0153](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0153.md)|Design|Do not log symbols decorated with DataClassificationAttribute directly|⚠️|✔️|❌|
|[MA0154](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0154.md)|Design|Use langword in XML comment|ℹ️|✔️|✔️|
|[MA0155](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0155.md)|Design|Do not use async void methods|⚠️|❌|❌|
|[MA0156](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0156.md)|Design|Use 'Async' suffix when a method returns IAsyncEnumerable\<T\>|⚠️|❌|✔️|
|[MA0157](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0157.md)|Design|Do not use 'Async' suffix when a method returns IAsyncEnumerable\<T\>|⚠️|❌|✔️|
|[MA0158](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0158.md)|Performance|Use System.Threading.Lock|⚠️|✔️|✔️|
|[MA0159](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0159.md)|Performance|Use 'Order' instead of 'OrderBy'|ℹ️|✔️|✔️|
|[MA0160](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0160.md)|Performance|Use ContainsKey instead of TryGetValue|ℹ️|✔️|✔️|
|[MA0161](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0161.md)|Usage|UseShellExecute must be explicitly set|ℹ️|❌|✔️|
|[MA0162](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0162.md)|Usage|Use Process.Start overload with ProcessStartInfo|ℹ️|❌|❌|
|[MA0163](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0163.md)|Usage|UseShellExecute must be false when redirecting standard input or output|⚠️|✔️|❌|
|[MA0164](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0164.md)|Style|Use parentheses to make not pattern clearer|⚠️|✔️|✔️|
|[MA0166](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0166.md)|Usage|Forward the TimeProvider to methods that take one|ℹ️|✔️|✔️|
|[MA0167](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0167.md)|Usage|Use an overload with a TimeProvider argument|ℹ️|❌|❌|
|[MA0168](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0168.md)|Performance|Use readonly struct for in or ref readonly parameter|ℹ️|❌|❌|
|[MA0169](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0169.md)|Design|Use Equals method instead of operator|⚠️|✔️|✔️|
|[MA0170](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0170.md)|Design|Type cannot be used as an attribute argument|⚠️|❌|❌|
|[MA0171](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0171.md)|Usage|Use pattern matching instead of HasValue for Nullable\<T\> check|ℹ️|❌|✔️|
|[MA0172](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0172.md)|Usage|Both sides of the logical operation are identical|⚠️|❌|❌|
|[MA0173](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0173.md)|Design|Use LazyInitializer.EnsureInitialize|ℹ️|✔️|✔️|
|[MA0174](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0174.md)|Style|Record should use explicit 'class' keyword|ℹ️|❌|✔️|
|[MA0175](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0175.md)|Style|Record should not use explicit 'class' keyword|ℹ️|❌|✔️|
|[MA0176](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0176.md)|Performance|Optimize guid creation|ℹ️|✔️|✔️|
|[MA0177](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0177.md)|Style|Use single-line XML comment syntax when possible|ℹ️|❌|✔️|
|[MA0178](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0178.md)|Design|Use TimeSpan.Zero instead of TimeSpan.FromXXX(0)|ℹ️|✔️|✔️|
|[MA0179](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0179.md)|Performance|Use Attribute.IsDefined instead of GetCustomAttribute(s)|ℹ️|✔️|✔️|
|[MA0180](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0180.md)|Design|ILogger type parameter should match containing type|⚠️|❌|✔️|
|[MA0181](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0181.md)|Style|Do not use cast|ℹ️|❌|❌|
|[MA0182](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0182.md)|Design|Avoid unused internal types|ℹ️|✔️|✔️|
|[MA0183](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0183.md)|Usage|The format string should use placeholders|⚠️|✔️|❌|
|[MA0184](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0184.md)|Style|Do not use interpolated string without parameters|👻|✔️|✔️|
|[MA0185](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0185.md)|Performance|Simplify string.Create when all parameters are culture invariant|ℹ️|✔️|✔️|
|[MA0186](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0186.md)|Design|Equals method should use \[NotNullWhen(true)\] on the parameter|ℹ️|❌|✔️|
|[MA0187](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0187.md)|Design|Use constructor injection instead of \[Inject\] attribute|ℹ️|❌|✔️|
|[MA0188](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0188.md)|Design|Use System.TimeProvider instead of a custom time abstraction|ℹ️|✔️|❌|
|[MA0189](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0189.md)|Design|Use InlineArray instead of fixed-size buffers|ℹ️|✔️|✔️|
|[MA0190](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0190.md)|Design|Use partial property instead of partial method for GeneratedRegex|ℹ️|✔️|✔️|
|[MA0191](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0191.md)|Design|Do not use the null-forgiving operator|⚠️|❌|❌|

<!-- rules -->

# Suppressions

<!-- suppressions -->

|Id|Suppressed rule|Justification|
|--|---------------|-------------|
|`MAS0001`|[CA1822](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1822?WT.mc_id=DT-MVP-5003978)|Suppress CA1822 on methods decorated with BenchmarkDotNet attributes.|
|`MAS0002`|[CA1822](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1822?WT.mc_id=DT-MVP-5003978)|Suppress CA1822 on methods decorated with a System.Text.Json attribute such as \[JsonPropertyName\] or \[JsonInclude\].|
|`MAS0003`|[IDE0058](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0058?WT.mc_id=DT-MVP-5003978)|Suppress IDE0058 on well-known types|
|`MAS0004`|[CA1507](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1507?WT.mc_id=DT-MVP-5003978)|Suppress CA1507 on methods decorated with a \[Newtonsoft.Json.JsonPropertyAttribute\].|

<!-- suppressions -->

# Refactorings

<!-- refactorings -->

|Name|
|----|
|`ConvertToStringFormat`|
|`MakeInterpolatedString`|

<!-- refactorings -->

# Configuration

You can set the `<MeziantouAnalysisMode>` MSBuild property to configure the default severity of the rules. The default value is `Default`. You can set it to `None` to disable all rules by default.

```xml
<Project>
  <PropertyGroup>
    <MeziantouAnalysisMode>None</MeziantouAnalysisMode>
  </PropertyGroup>
</Project>
