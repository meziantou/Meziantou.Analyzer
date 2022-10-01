# Meziantou.Analyzer

[![Meziantou.Analyzer on NuGet](https://img.shields.io/nuget/v/Meziantou.Analyzer.svg)](https://www.nuget.org/packages/Meziantou.Analyzer/)

A Roslyn analyzer to enforce some good practices in C# in terms of design, usage, security, performance, and style.

## Installation

Install the NuGet package <https://www.nuget.org/packages/Meziantou.Analyzer/>

## Rules

If you are already using other analyzers, you can check [which rules are duplicated with well-known analyzers](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/comparison-with-other-analyzers.md)

<!-- rules -->

|Id|Category|Description|Severity|Is enabled|Code fix|
|--|--------|-----------|:------:|:--------:|:------:|
|[MA0001](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0001.md)|Usage|StringComparison is missing|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0002](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0002.md)|Usage|IEqualityComparer\<string\> or IComparer\<string\> is missing|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0003](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0003.md)|Style|Add parameter name to improve readability|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0004](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0004.md)|Usage|Use Task.ConfigureAwait(false)|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0005](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0005.md)|Performance|Use Array.Empty\<T\>()|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0006](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0006.md)|Usage|Use String.Equals instead of equality operator|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0007](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0007.md)|Style|Add a comma after the last value|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0008](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0008.md)|Performance|Add StructLayoutAttribute|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0009](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0009.md)|Security|Add regex evaluation timeout|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0010](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0010.md)|Design|Mark attributes with AttributeUsageAttribute|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0011](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0011.md)|Usage|IFormatProvider is missing|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0012](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0012.md)|Design|Do not raise reserved exception type|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0013](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0013.md)|Design|Types should not extend System.ApplicationException|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0014](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0014.md)|Design|Do not raise System.ApplicationException type|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0015](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0015.md)|Usage|Specify the parameter name in ArgumentException|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0016](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0016.md)|Design|Prefer returning collection abstraction instead of implementation|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0017](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0017.md)|Design|Abstract types should not have public or internal constructors|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0018](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0018.md)|Design|Do not declare static members on generic types|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0019](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0019.md)|Usage|Use EventArgs.Empty|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0020](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0020.md)|Performance|Use direct methods instead of LINQ methods|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0021](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0021.md)|Usage|Use StringComparer.GetHashCode instead of string.GetHashCode|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0022](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0022.md)|Design|Return Task.FromResult instead of returning null|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0023](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0023.md)|Performance|Add RegexOptions.ExplicitCapture|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0024](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0024.md)|Usage|Use an explicit StringComparer when possible|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0025](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0025.md)|Design|Implement the functionality instead of throwing NotImplementedException|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0026](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0026.md)|Design|Fix TODO comment|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0027](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0027.md)|Usage|Prefer rethrowing an exception implicitly|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0028](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0028.md)|Performance|Optimize StringBuilder usage|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0029](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0029.md)|Performance|Combine LINQ methods|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0030](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0030.md)|Performance|Remove useless OrderBy call|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0031](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0031.md)|Performance|Optimize Enumerable.Count() usage|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0032](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0032.md)|Usage|Use an overload with a CancellationToken argument|<span title='Info'>ℹ️</span>|❌|❌|
|[MA0033](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0033.md)|Design|Do not tag instance fields with ThreadStaticAttribute|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0035](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0035.md)|Usage|Do not use dangerous threading methods|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0036](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0036.md)|Design|Make class static|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0037](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0037.md)|Usage|Remove empty statement|<span title='Error'>❌</span>|✔️|❌|
|[MA0038](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0038.md)|Design|Make method static|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0039](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0039.md)|Security|Do not write your own certificate validation method|<span title='Error'>❌</span>|✔️|❌|
|[MA0040](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0040.md)|Usage|Forward the CancellationToken parameter to methods that take one|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0041](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0041.md)|Design|Make property static|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0042](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0042.md)|Design|Do not use blocking calls in an async method|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0043](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0043.md)|Usage|Use nameof operator in ArgumentException|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0044](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0044.md)|Performance|Remove useless ToString call|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0045](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0045.md)|Design|Do not use blocking calls in a sync method (need to make calling method async)|<span title='Info'>ℹ️</span>|❌|❌|
|[MA0046](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0046.md)|Design|Use EventHandler\<T\> to declare events|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0047](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0047.md)|Design|Declare types in namespaces|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0048](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0048.md)|Design|File name must match type name|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0049](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0049.md)|Design|Type name should not match containing namespace|<span title='Error'>❌</span>|✔️|❌|
|[MA0050](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0050.md)|Design|Validate arguments correctly in iterator methods|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0051](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0051.md)|Design|Method is too long|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0052](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0052.md)|Performance|Replace constant Enum.ToString with nameof|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0053](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0053.md)|Design|Make class sealed|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0054](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0054.md)|Design|Embed the caught exception as innerException|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0055](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0055.md)|Design|Do not use finalizer|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0056](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0056.md)|Design|Do not call overridable members in constructor|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0057](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0057.md)|Naming|Class name should end with 'Attribute'|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0058](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0058.md)|Naming|Class name should end with 'Exception'|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0059](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0059.md)|Naming|Class name should end with 'EventArgs'|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0060](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0060.md)|Design|The value returned by Stream.Read/Stream.ReadAsync is not used|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0061](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0061.md)|Design|Method overrides should not change default values|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0062](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0062.md)|Design|Non-flags enums should not be marked with "FlagsAttribute"|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0063](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0063.md)|Performance|Use Where before OrderBy|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0064](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0064.md)|Design|Avoid locking on publicly accessible instance|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0065](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0065.md)|Performance|Default ValueType.Equals or HashCode is used for struct equality|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0066](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0066.md)|Performance|Hash table unfriendly type is used in a hash table|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0067](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0067.md)|Design|Use Guid.Empty|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0068](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0068.md)|Design|Invalid parameter name for nullable attribute|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0069](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0069.md)|Design|Non-constant static fields should not be visible|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0070](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0070.md)|Design|Obsolete attributes should include explanations|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0071](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0071.md)|Style|Avoid using redundant else|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0072](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0072.md)|Design|Do not throw from a finally block|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0073](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0073.md)|Style|Avoid comparison with bool constant|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0074](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0074.md)|Usage|Avoid implicit culture-sensitive methods|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0075](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0075.md)|Design|Do not use implicit culture-sensitive ToString|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0076](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0076.md)|Design|Do not use implicit culture-sensitive ToString in interpolated strings|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0077](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0077.md)|Design|A class that provides Equals(T) should implement IEquatable\<T\>|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0078](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0078.md)|Performance|Use 'Cast' instead of 'Select' to cast|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0079](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0079.md)|Usage|Forward the CancellationToken using .WithCancellation()|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0080](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0080.md)|Usage|Use a cancellation token using .WithCancellation()|<span title='Info'>ℹ️</span>|❌|❌|
|[MA0081](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0081.md)|Design|Method overrides should not omit params keyword|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0082](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0082.md)|Design|NaN should not be used in comparisons|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0083](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0083.md)|Design|ConstructorArgument parameters should exist in constructors|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0084](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0084.md)|Design|Local variables should not hide other symbols|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0085](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0085.md)|Usage|Anonymous delegates should not be used to unsubscribe from Events|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0086](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0086.md)|Design|Do not throw from a finalizer|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0087](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0087.md)|Design|Parameters with \[DefaultParameterValue\] attributes should also be marked \[Optional\]|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0088](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0088.md)|Design|Use \[DefaultParameterValue\] instead of \[DefaultValue\]|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0089](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0089.md)|Performance|Optimize string method usage|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0090](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0090.md)|Design|Remove empty else/finally block|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0091](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0091.md)|Usage|Sender should be 'this' for instance events|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0092](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0092.md)|Usage|Sender should be 'null' for static events|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0093](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0093.md)|Usage|EventArgs should not be null|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0094](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0094.md)|Design|A class that provides CompareTo(T) should implement IComparable\<T\>|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0095](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0095.md)|Design|A class that implements IEquatable\<T\> should override Equals(object)|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0096](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0096.md)|Design|A class that implements IComparable\<T\> should also implement IEquatable\<T\>|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0097](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0097.md)|Design|A class that implements IComparable\<T\> or IComparable should override comparison operators|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0098](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0098.md)|Performance|Use indexer instead of LINQ methods|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0099](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0099.md)|Usage|Use Explicit enum value instead of 0|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0100](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0100.md)|Usage|Await task before disposing of resources|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0101](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0101.md)|Usage|String contains an implicit end of line character|<span title='Hidden'>👻</span>|✔️|✔️|
|[MA0102](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0102.md)|Design|Make member readonly|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0103](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0103.md)|Usage|Use SequenceEqual instead of equality operator|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0104](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0104.md)|Design|Do not create a type with a name from the BCL|<span title='Warning'>⚠️</span>|❌|❌|
|[MA0105](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0105.md)|Performance|Use the lambda parameters instead of using a closure|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0106](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0106.md)|Performance|Avoid closure by using an overload with the 'factoryArgument' parameter|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0107](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0107.md)|Design|Do not use culture-sensitive object.ToString|<span title='Info'>ℹ️</span>|❌|❌|
|[MA0108](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0108.md)|Usage|Remove redundant argument value|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0109](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0109.md)|Design|Consider adding an overload with a Span\<T\> or Memory\<T\>|<span title='Info'>ℹ️</span>|❌|❌|
|[MA0110](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0110.md)|Performance|Use the Regex source generator|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0111](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0111.md)|Performance|Use string.Create instead of FormattableString|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0112](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0112.md)|Performance|Use 'Count \> 0' instead of 'Any()'|<span title='Info'>ℹ️</span>|❌|❌|
|[MA0113](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0113.md)|Design|Use DateTime.UnixEpoch|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0114](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0114.md)|Design|Use DateTimeOffset.UnixEpoch|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0115](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0115.md)|Usage|Unknown component parameter|<span title='Warning'>⚠️</span>|✔️|❌|

<!-- rules -->