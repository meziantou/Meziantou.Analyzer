# Meziantou.Analyzer's rules
|Id|Category|Description|Severity|Is enabled|Code fix|
|--|--------|-----------|:------:|:--------:|:------:|
|[MA0001](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0001.md)|Usage|StringComparison is missing|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0002](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0002.md)|Usage|IEqualityComparer\<string\> or IComparer\<string\> is missing|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0003](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0003.md)|Style|Add parameter name to improve readability|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0004](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0004.md)|Usage|Use Task.ConfigureAwait|<span title='Warning'>⚠️</span>|✔️|✔️|
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
|[MA0016](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0016.md)|Design|Prefer using collection abstraction instead of implementation|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0017](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0017.md)|Design|Abstract types should not have public or internal constructors|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0018](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0018.md)|Design|Do not declare static members on generic types (deprecated; use CA1000 instead)|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0019](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0019.md)|Usage|Use EventArgs.Empty|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0020](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0020.md)|Performance|Use direct methods instead of LINQ methods|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0021](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0021.md)|Usage|Use StringComparer.GetHashCode instead of string.GetHashCode|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0022](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0022.md)|Design|Return Task.FromResult instead of returning null|<span title='Warning'>⚠️</span>|✔️|✔️|
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
|[MA0037](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0037.md)|Usage|Remove empty statement|<span title='Error'>❌</span>|✔️|✔️|
|[MA0038](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0038.md)|Design|Make method static (deprecated, use CA1822 instead)|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0039](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0039.md)|Security|Do not write your own certificate validation method|<span title='Error'>❌</span>|✔️|❌|
|[MA0040](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0040.md)|Usage|Forward the CancellationToken parameter to methods that take one|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0041](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0041.md)|Design|Make property static (deprecated, use CA1822 instead)|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0042](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0042.md)|Design|Do not use blocking calls in an async method|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0043](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0043.md)|Usage|Use nameof operator in ArgumentException|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0044](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0044.md)|Performance|Remove useless ToString call|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0045](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0045.md)|Design|Do not use blocking calls in a sync method (need to make calling method async)|<span title='Info'>ℹ️</span>|❌|✔️|
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
|[MA0061](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0061.md)|Design|Method overrides should not change default values|<span title='Warning'>⚠️</span>|✔️|✔️|
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
|[MA0079](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0079.md)|Usage|Forward the CancellationToken using .WithCancellation()|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0080](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0080.md)|Usage|Use a cancellation token using .WithCancellation()|<span title='Info'>ℹ️</span>|❌|❌|
|[MA0081](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0081.md)|Design|Method overrides should not omit params keyword|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0082](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0082.md)|Design|NaN should not be used in comparisons|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0083](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0083.md)|Design|ConstructorArgument parameters should exist in constructors|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0084](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0084.md)|Design|Local variables should not hide other symbols|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0085](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0085.md)|Usage|Anonymous delegates should not be used to unsubscribe from Events|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0086](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0086.md)|Design|Do not throw from a finalizer|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0087](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0087.md)|Design|Parameters with \[DefaultParameterValue\] attributes should also be marked \[Optional\]|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0088](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0088.md)|Design|Use \[DefaultParameterValue\] instead of \[DefaultValue\]|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0089](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0089.md)|Performance|Optimize string method usage|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0090](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0090.md)|Design|Remove empty else/finally block|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0091](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0091.md)|Usage|Sender should be 'this' for instance events|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0092](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0092.md)|Usage|Sender should be 'null' for static events|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0093](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0093.md)|Usage|EventArgs should not be null|<span title='Warning'>⚠️</span>|✔️|✔️|
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
|[MA0113](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0113.md)|Design|Use DateTime.UnixEpoch|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0114](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0114.md)|Design|Use DateTimeOffset.UnixEpoch|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0115](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0115.md)|Usage|Unknown component parameter|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0116](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0116.md)|Design|Parameters with \[SupplyParameterFromQuery\] attributes should also be marked as \[Parameter\]|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0117](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0117.md)|Design|Parameters with \[EditorRequired\] attributes should also be marked as \[Parameter\]|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0118](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0118.md)|Design|\[JSInvokable\] methods must be public|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0119](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0119.md)|Design|JSRuntime must not be used in OnInitialized or OnInitializedAsync|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0120](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0120.md)|Performance|Use InvokeVoidAsync when the returned value is not used|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0121](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0121.md)|Design|Do not overwrite parameter value|<span title='Info'>ℹ️</span>|❌|❌|
|[MA0122](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0122.md)|Design|Parameters with \[SupplyParameterFromQuery\] attributes are only valid in routable components (@page)|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0123](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0123.md)|Design|Sequence number must be a constant|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0124](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0124.md)|Design|Log parameter type is not valid|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0125](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0125.md)|Design|The list of log parameter types contains an invalid type|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0126](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0126.md)|Design|The list of log parameter types contains a duplicate|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0127](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0127.md)|Usage|Use String.Equals instead of is pattern|<span title='Warning'>⚠️</span>|❌|❌|
|[MA0128](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0128.md)|Usage|Use 'is' operator instead of SequenceEqual|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0129](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0129.md)|Usage|Await task in using statement|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0130](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0130.md)|Usage|GetType() should not be used on System.Type instances|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0131](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0131.md)|Usage|ArgumentNullException.ThrowIfNull should not be used with non-nullable types|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0132](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0132.md)|Design|Do not convert implicitly to DateTimeOffset|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0133](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0133.md)|Design|Use DateTimeOffset instead of relying on the implicit conversion|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0134](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0134.md)|Usage|Observe result of async calls|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0135](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0135.md)|Design|The log parameter has no configured type|<span title='Warning'>⚠️</span>|❌|❌|
|[MA0136](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0136.md)|Usage|Raw String contains an implicit end of line character|<span title='Hidden'>👻</span>|✔️|❌|
|[MA0137](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0137.md)|Design|Use 'Async' suffix when a method returns an awaitable type|<span title='Warning'>⚠️</span>|❌|❌|
|[MA0138](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0138.md)|Design|Do not use 'Async' suffix when a method does not return an awaitable type|<span title='Warning'>⚠️</span>|❌|❌|
|[MA0139](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0139.md)|Design|Log parameter type is not valid|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0140](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0140.md)|Design|Both if and else branch have identical code|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0141](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0141.md)|Usage|Use pattern matching instead of inequality operators for null check|<span title='Info'>ℹ️</span>|❌|✔️|
|[MA0142](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0142.md)|Usage|Use pattern matching instead of equality operators for null check|<span title='Info'>ℹ️</span>|❌|✔️|
|[MA0143](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0143.md)|Design|Primary constructor parameters should be readonly|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0144](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0144.md)|Performance|Use System.OperatingSystem to check the current OS|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0145](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0145.md)|Usage|Signature for \[UnsafeAccessorAttribute\] method is not valid|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0146](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0146.md)|Usage|Name must be set explicitly on local functions|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0147](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0147.md)|Usage|Avoid async void method for delegate|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0148](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0148.md)|Usage|Use pattern matching instead of equality operators for discrete value|<span title='Info'>ℹ️</span>|❌|✔️|
|[MA0149](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0149.md)|Usage|Use pattern matching instead of inequality operators for discrete value|<span title='Info'>ℹ️</span>|❌|✔️|
|[MA0150](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0150.md)|Design|Do not call the default object.ToString explicitly|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0151](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0151.md)|Usage|DebuggerDisplay must contain valid members|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0152](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0152.md)|Performance|Use Unwrap instead of using await twice|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0153](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0153.md)|Design|Do not log symbols decorated with DataClassificationAttribute directly|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0154](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0154.md)|Design|Use langword in XML comment|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0155](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0155.md)|Design|Do not use async void methods|<span title='Warning'>⚠️</span>|❌|❌|
|[MA0156](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0156.md)|Design|Use 'Async' suffix when a method returns IAsyncEnumerable\<T\>|<span title='Warning'>⚠️</span>|❌|❌|
|[MA0157](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0157.md)|Design|Do not use 'Async' suffix when a method returns IAsyncEnumerable\<T\>|<span title='Warning'>⚠️</span>|❌|❌|
|[MA0158](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0158.md)|Performance|Use System.Threading.Lock|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0159](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0159.md)|Performance|Use 'Order' instead of 'OrderBy'|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0160](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0160.md)|Performance|Use ContainsKey instead of TryGetValue|<span title='Info'>ℹ️</span>|✔️|❌|
|[MA0161](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0161.md)|Usage|UseShellExecute must be explicitly set|<span title='Info'>ℹ️</span>|❌|❌|
|[MA0162](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0162.md)|Usage|Use Process.Start overload with ProcessStartInfo|<span title='Info'>ℹ️</span>|❌|❌|
|[MA0163](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0163.md)|Usage|UseShellExecute must be false when redirecting standard input or output|<span title='Warning'>⚠️</span>|✔️|❌|
|[MA0164](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0164.md)|Style|Use parentheses to make not pattern clearer|<span title='Warning'>⚠️</span>|✔️|✔️|
|[MA0165](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0165.md)|Usage|Make interpolated string|<span title='Hidden'>👻</span>|✔️|✔️|
|[MA0166](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0166.md)|Usage|Forward the TimeProvider to methods that take one|<span title='Info'>ℹ️</span>|✔️|✔️|
|[MA0167](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0167.md)|Usage|Use an overload with a TimeProvider argument|<span title='Info'>ℹ️</span>|❌|❌|
|[MA0168](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0168.md)|Performance|Use readonly struct for in or ref readonly parameter|<span title='Info'>ℹ️</span>|❌|❌|
|[MA0169](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0169.md)|Design|Use Equals method instead of operator|<span title='Warning'>⚠️</span>|✔️|❌|

|Id|Suppressed rule|Justification|
|--|---------------|-------------|
|`MAS0001`|[CA1822](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1822?WT.mc_id=DT-MVP-5003978)|Suppress CA1822 on methods decorated with BenchmarkDotNet attributes.|
|`MAS0002`|[CA1822](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1822?WT.mc_id=DT-MVP-5003978)|Suppress CA1822 on methods decorated with a System.Text.Json attribute such as \[JsonPropertyName\] or \[JsonInclude\].|
|`MAS0003`|[IDE0058](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0058?WT.mc_id=DT-MVP-5003978)|Suppress IDE0058 on well-known types|
|`MAS0004`|[CA1507](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1507?WT.mc_id=DT-MVP-5003978)|Suppress CA1507 on methods decorated with a \[Newtonsoft.Json.JsonPropertyAttribute\].|


# .editorconfig - default values

```editorconfig
# MA0001: StringComparison is missing
dotnet_diagnostic.MA0001.severity = suggestion

# MA0002: IEqualityComparer<string> or IComparer<string> is missing
dotnet_diagnostic.MA0002.severity = warning

# MA0003: Add parameter name to improve readability
dotnet_diagnostic.MA0003.severity = suggestion

# MA0004: Use Task.ConfigureAwait
dotnet_diagnostic.MA0004.severity = warning

# MA0005: Use Array.Empty<T>()
dotnet_diagnostic.MA0005.severity = warning

# MA0006: Use String.Equals instead of equality operator
dotnet_diagnostic.MA0006.severity = warning

# MA0007: Add a comma after the last value
dotnet_diagnostic.MA0007.severity = suggestion

# MA0008: Add StructLayoutAttribute
dotnet_diagnostic.MA0008.severity = warning

# MA0009: Add regex evaluation timeout
dotnet_diagnostic.MA0009.severity = warning

# MA0010: Mark attributes with AttributeUsageAttribute
dotnet_diagnostic.MA0010.severity = warning

# MA0011: IFormatProvider is missing
dotnet_diagnostic.MA0011.severity = warning

# MA0012: Do not raise reserved exception type
dotnet_diagnostic.MA0012.severity = warning

# MA0013: Types should not extend System.ApplicationException
dotnet_diagnostic.MA0013.severity = warning

# MA0014: Do not raise System.ApplicationException type
dotnet_diagnostic.MA0014.severity = warning

# MA0015: Specify the parameter name in ArgumentException
dotnet_diagnostic.MA0015.severity = warning

# MA0016: Prefer using collection abstraction instead of implementation
dotnet_diagnostic.MA0016.severity = warning

# MA0017: Abstract types should not have public or internal constructors
dotnet_diagnostic.MA0017.severity = warning

# MA0018: Do not declare static members on generic types (deprecated; use CA1000 instead)
dotnet_diagnostic.MA0018.severity = suggestion

# MA0019: Use EventArgs.Empty
dotnet_diagnostic.MA0019.severity = warning

# MA0020: Use direct methods instead of LINQ methods
dotnet_diagnostic.MA0020.severity = suggestion

# MA0021: Use StringComparer.GetHashCode instead of string.GetHashCode
dotnet_diagnostic.MA0021.severity = warning

# MA0022: Return Task.FromResult instead of returning null
dotnet_diagnostic.MA0022.severity = warning

# MA0023: Add RegexOptions.ExplicitCapture
dotnet_diagnostic.MA0023.severity = warning

# MA0024: Use an explicit StringComparer when possible
dotnet_diagnostic.MA0024.severity = warning

# MA0025: Implement the functionality instead of throwing NotImplementedException
dotnet_diagnostic.MA0025.severity = warning

# MA0026: Fix TODO comment
dotnet_diagnostic.MA0026.severity = warning

# MA0027: Prefer rethrowing an exception implicitly
dotnet_diagnostic.MA0027.severity = warning

# MA0028: Optimize StringBuilder usage
dotnet_diagnostic.MA0028.severity = suggestion

# MA0029: Combine LINQ methods
dotnet_diagnostic.MA0029.severity = suggestion

# MA0030: Remove useless OrderBy call
dotnet_diagnostic.MA0030.severity = warning

# MA0031: Optimize Enumerable.Count() usage
dotnet_diagnostic.MA0031.severity = suggestion

# MA0032: Use an overload with a CancellationToken argument
dotnet_diagnostic.MA0032.severity = none

# MA0033: Do not tag instance fields with ThreadStaticAttribute
dotnet_diagnostic.MA0033.severity = warning

# MA0035: Do not use dangerous threading methods
dotnet_diagnostic.MA0035.severity = warning

# MA0036: Make class static
dotnet_diagnostic.MA0036.severity = suggestion

# MA0037: Remove empty statement
dotnet_diagnostic.MA0037.severity = error

# MA0038: Make method static (deprecated, use CA1822 instead)
dotnet_diagnostic.MA0038.severity = suggestion

# MA0039: Do not write your own certificate validation method
dotnet_diagnostic.MA0039.severity = error

# MA0040: Forward the CancellationToken parameter to methods that take one
dotnet_diagnostic.MA0040.severity = suggestion

# MA0041: Make property static (deprecated, use CA1822 instead)
dotnet_diagnostic.MA0041.severity = suggestion

# MA0042: Do not use blocking calls in an async method
dotnet_diagnostic.MA0042.severity = suggestion

# MA0043: Use nameof operator in ArgumentException
dotnet_diagnostic.MA0043.severity = suggestion

# MA0044: Remove useless ToString call
dotnet_diagnostic.MA0044.severity = suggestion

# MA0045: Do not use blocking calls in a sync method (need to make calling method async)
dotnet_diagnostic.MA0045.severity = none

# MA0046: Use EventHandler<T> to declare events
dotnet_diagnostic.MA0046.severity = warning

# MA0047: Declare types in namespaces
dotnet_diagnostic.MA0047.severity = warning

# MA0048: File name must match type name
dotnet_diagnostic.MA0048.severity = warning

# MA0049: Type name should not match containing namespace
dotnet_diagnostic.MA0049.severity = error

# MA0050: Validate arguments correctly in iterator methods
dotnet_diagnostic.MA0050.severity = suggestion

# MA0051: Method is too long
dotnet_diagnostic.MA0051.severity = warning

# MA0052: Replace constant Enum.ToString with nameof
dotnet_diagnostic.MA0052.severity = suggestion

# MA0053: Make class sealed
dotnet_diagnostic.MA0053.severity = suggestion

# MA0054: Embed the caught exception as innerException
dotnet_diagnostic.MA0054.severity = warning

# MA0055: Do not use finalizer
dotnet_diagnostic.MA0055.severity = warning

# MA0056: Do not call overridable members in constructor
dotnet_diagnostic.MA0056.severity = warning

# MA0057: Class name should end with 'Attribute'
dotnet_diagnostic.MA0057.severity = suggestion

# MA0058: Class name should end with 'Exception'
dotnet_diagnostic.MA0058.severity = suggestion

# MA0059: Class name should end with 'EventArgs'
dotnet_diagnostic.MA0059.severity = suggestion

# MA0060: The value returned by Stream.Read/Stream.ReadAsync is not used
dotnet_diagnostic.MA0060.severity = warning

# MA0061: Method overrides should not change default values
dotnet_diagnostic.MA0061.severity = warning

# MA0062: Non-flags enums should not be marked with "FlagsAttribute"
dotnet_diagnostic.MA0062.severity = warning

# MA0063: Use Where before OrderBy
dotnet_diagnostic.MA0063.severity = suggestion

# MA0064: Avoid locking on publicly accessible instance
dotnet_diagnostic.MA0064.severity = warning

# MA0065: Default ValueType.Equals or HashCode is used for struct equality
dotnet_diagnostic.MA0065.severity = warning

# MA0066: Hash table unfriendly type is used in a hash table
dotnet_diagnostic.MA0066.severity = warning

# MA0067: Use Guid.Empty
dotnet_diagnostic.MA0067.severity = suggestion

# MA0068: Invalid parameter name for nullable attribute
dotnet_diagnostic.MA0068.severity = warning

# MA0069: Non-constant static fields should not be visible
dotnet_diagnostic.MA0069.severity = warning

# MA0070: Obsolete attributes should include explanations
dotnet_diagnostic.MA0070.severity = warning

# MA0071: Avoid using redundant else
dotnet_diagnostic.MA0071.severity = suggestion

# MA0072: Do not throw from a finally block
dotnet_diagnostic.MA0072.severity = warning

# MA0073: Avoid comparison with bool constant
dotnet_diagnostic.MA0073.severity = suggestion

# MA0074: Avoid implicit culture-sensitive methods
dotnet_diagnostic.MA0074.severity = warning

# MA0075: Do not use implicit culture-sensitive ToString
dotnet_diagnostic.MA0075.severity = suggestion

# MA0076: Do not use implicit culture-sensitive ToString in interpolated strings
dotnet_diagnostic.MA0076.severity = suggestion

# MA0077: A class that provides Equals(T) should implement IEquatable<T>
dotnet_diagnostic.MA0077.severity = warning

# MA0078: Use 'Cast' instead of 'Select' to cast
dotnet_diagnostic.MA0078.severity = suggestion

# MA0079: Forward the CancellationToken using .WithCancellation()
dotnet_diagnostic.MA0079.severity = suggestion

# MA0080: Use a cancellation token using .WithCancellation()
dotnet_diagnostic.MA0080.severity = none

# MA0081: Method overrides should not omit params keyword
dotnet_diagnostic.MA0081.severity = warning

# MA0082: NaN should not be used in comparisons
dotnet_diagnostic.MA0082.severity = warning

# MA0083: ConstructorArgument parameters should exist in constructors
dotnet_diagnostic.MA0083.severity = warning

# MA0084: Local variables should not hide other symbols
dotnet_diagnostic.MA0084.severity = warning

# MA0085: Anonymous delegates should not be used to unsubscribe from Events
dotnet_diagnostic.MA0085.severity = warning

# MA0086: Do not throw from a finalizer
dotnet_diagnostic.MA0086.severity = warning

# MA0087: Parameters with [DefaultParameterValue] attributes should also be marked [Optional]
dotnet_diagnostic.MA0087.severity = warning

# MA0088: Use [DefaultParameterValue] instead of [DefaultValue]
dotnet_diagnostic.MA0088.severity = warning

# MA0089: Optimize string method usage
dotnet_diagnostic.MA0089.severity = suggestion

# MA0090: Remove empty else/finally block
dotnet_diagnostic.MA0090.severity = suggestion

# MA0091: Sender should be 'this' for instance events
dotnet_diagnostic.MA0091.severity = warning

# MA0092: Sender should be 'null' for static events
dotnet_diagnostic.MA0092.severity = warning

# MA0093: EventArgs should not be null
dotnet_diagnostic.MA0093.severity = warning

# MA0094: A class that provides CompareTo(T) should implement IComparable<T>
dotnet_diagnostic.MA0094.severity = warning

# MA0095: A class that implements IEquatable<T> should override Equals(object)
dotnet_diagnostic.MA0095.severity = warning

# MA0096: A class that implements IComparable<T> should also implement IEquatable<T>
dotnet_diagnostic.MA0096.severity = warning

# MA0097: A class that implements IComparable<T> or IComparable should override comparison operators
dotnet_diagnostic.MA0097.severity = warning

# MA0098: Use indexer instead of LINQ methods
dotnet_diagnostic.MA0098.severity = suggestion

# MA0099: Use Explicit enum value instead of 0
dotnet_diagnostic.MA0099.severity = warning

# MA0100: Await task before disposing of resources
dotnet_diagnostic.MA0100.severity = warning

# MA0101: String contains an implicit end of line character
dotnet_diagnostic.MA0101.severity = silent

# MA0102: Make member readonly
dotnet_diagnostic.MA0102.severity = suggestion

# MA0103: Use SequenceEqual instead of equality operator
dotnet_diagnostic.MA0103.severity = warning

# MA0104: Do not create a type with a name from the BCL
dotnet_diagnostic.MA0104.severity = none

# MA0105: Use the lambda parameters instead of using a closure
dotnet_diagnostic.MA0105.severity = suggestion

# MA0106: Avoid closure by using an overload with the 'factoryArgument' parameter
dotnet_diagnostic.MA0106.severity = suggestion

# MA0107: Do not use culture-sensitive object.ToString
dotnet_diagnostic.MA0107.severity = none

# MA0108: Remove redundant argument value
dotnet_diagnostic.MA0108.severity = suggestion

# MA0109: Consider adding an overload with a Span<T> or Memory<T>
dotnet_diagnostic.MA0109.severity = none

# MA0110: Use the Regex source generator
dotnet_diagnostic.MA0110.severity = suggestion

# MA0111: Use string.Create instead of FormattableString
dotnet_diagnostic.MA0111.severity = suggestion

# MA0112: Use 'Count > 0' instead of 'Any()'
dotnet_diagnostic.MA0112.severity = none

# MA0113: Use DateTime.UnixEpoch
dotnet_diagnostic.MA0113.severity = suggestion

# MA0114: Use DateTimeOffset.UnixEpoch
dotnet_diagnostic.MA0114.severity = suggestion

# MA0115: Unknown component parameter
dotnet_diagnostic.MA0115.severity = warning

# MA0116: Parameters with [SupplyParameterFromQuery] attributes should also be marked as [Parameter]
dotnet_diagnostic.MA0116.severity = warning

# MA0117: Parameters with [EditorRequired] attributes should also be marked as [Parameter]
dotnet_diagnostic.MA0117.severity = warning

# MA0118: [JSInvokable] methods must be public
dotnet_diagnostic.MA0118.severity = warning

# MA0119: JSRuntime must not be used in OnInitialized or OnInitializedAsync
dotnet_diagnostic.MA0119.severity = warning

# MA0120: Use InvokeVoidAsync when the returned value is not used
dotnet_diagnostic.MA0120.severity = suggestion

# MA0121: Do not overwrite parameter value
dotnet_diagnostic.MA0121.severity = none

# MA0122: Parameters with [SupplyParameterFromQuery] attributes are only valid in routable components (@page)
dotnet_diagnostic.MA0122.severity = suggestion

# MA0123: Sequence number must be a constant
dotnet_diagnostic.MA0123.severity = warning

# MA0124: Log parameter type is not valid
dotnet_diagnostic.MA0124.severity = warning

# MA0125: The list of log parameter types contains an invalid type
dotnet_diagnostic.MA0125.severity = warning

# MA0126: The list of log parameter types contains a duplicate
dotnet_diagnostic.MA0126.severity = warning

# MA0127: Use String.Equals instead of is pattern
dotnet_diagnostic.MA0127.severity = none

# MA0128: Use 'is' operator instead of SequenceEqual
dotnet_diagnostic.MA0128.severity = suggestion

# MA0129: Await task in using statement
dotnet_diagnostic.MA0129.severity = warning

# MA0130: GetType() should not be used on System.Type instances
dotnet_diagnostic.MA0130.severity = warning

# MA0131: ArgumentNullException.ThrowIfNull should not be used with non-nullable types
dotnet_diagnostic.MA0131.severity = warning

# MA0132: Do not convert implicitly to DateTimeOffset
dotnet_diagnostic.MA0132.severity = warning

# MA0133: Use DateTimeOffset instead of relying on the implicit conversion
dotnet_diagnostic.MA0133.severity = suggestion

# MA0134: Observe result of async calls
dotnet_diagnostic.MA0134.severity = warning

# MA0135: The log parameter has no configured type
dotnet_diagnostic.MA0135.severity = none

# MA0136: Raw String contains an implicit end of line character
dotnet_diagnostic.MA0136.severity = silent

# MA0137: Use 'Async' suffix when a method returns an awaitable type
dotnet_diagnostic.MA0137.severity = none

# MA0138: Do not use 'Async' suffix when a method does not return an awaitable type
dotnet_diagnostic.MA0138.severity = none

# MA0139: Log parameter type is not valid
dotnet_diagnostic.MA0139.severity = warning

# MA0140: Both if and else branch have identical code
dotnet_diagnostic.MA0140.severity = warning

# MA0141: Use pattern matching instead of inequality operators for null check
dotnet_diagnostic.MA0141.severity = none

# MA0142: Use pattern matching instead of equality operators for null check
dotnet_diagnostic.MA0142.severity = none

# MA0143: Primary constructor parameters should be readonly
dotnet_diagnostic.MA0143.severity = warning

# MA0144: Use System.OperatingSystem to check the current OS
dotnet_diagnostic.MA0144.severity = warning

# MA0145: Signature for [UnsafeAccessorAttribute] method is not valid
dotnet_diagnostic.MA0145.severity = warning

# MA0146: Name must be set explicitly on local functions
dotnet_diagnostic.MA0146.severity = warning

# MA0147: Avoid async void method for delegate
dotnet_diagnostic.MA0147.severity = warning

# MA0148: Use pattern matching instead of equality operators for discrete value
dotnet_diagnostic.MA0148.severity = none

# MA0149: Use pattern matching instead of inequality operators for discrete value
dotnet_diagnostic.MA0149.severity = none

# MA0150: Do not call the default object.ToString explicitly
dotnet_diagnostic.MA0150.severity = warning

# MA0151: DebuggerDisplay must contain valid members
dotnet_diagnostic.MA0151.severity = warning

# MA0152: Use Unwrap instead of using await twice
dotnet_diagnostic.MA0152.severity = suggestion

# MA0153: Do not log symbols decorated with DataClassificationAttribute directly
dotnet_diagnostic.MA0153.severity = warning

# MA0154: Use langword in XML comment
dotnet_diagnostic.MA0154.severity = suggestion

# MA0155: Do not use async void methods
dotnet_diagnostic.MA0155.severity = none

# MA0156: Use 'Async' suffix when a method returns IAsyncEnumerable<T>
dotnet_diagnostic.MA0156.severity = none

# MA0157: Do not use 'Async' suffix when a method returns IAsyncEnumerable<T>
dotnet_diagnostic.MA0157.severity = none

# MA0158: Use System.Threading.Lock
dotnet_diagnostic.MA0158.severity = warning

# MA0159: Use 'Order' instead of 'OrderBy'
dotnet_diagnostic.MA0159.severity = suggestion

# MA0160: Use ContainsKey instead of TryGetValue
dotnet_diagnostic.MA0160.severity = suggestion

# MA0161: UseShellExecute must be explicitly set
dotnet_diagnostic.MA0161.severity = none

# MA0162: Use Process.Start overload with ProcessStartInfo
dotnet_diagnostic.MA0162.severity = none

# MA0163: UseShellExecute must be false when redirecting standard input or output
dotnet_diagnostic.MA0163.severity = warning

# MA0164: Use parentheses to make not pattern clearer
dotnet_diagnostic.MA0164.severity = warning

# MA0165: Make interpolated string
dotnet_diagnostic.MA0165.severity = silent

# MA0166: Forward the TimeProvider to methods that take one
dotnet_diagnostic.MA0166.severity = suggestion

# MA0167: Use an overload with a TimeProvider argument
dotnet_diagnostic.MA0167.severity = none

# MA0168: Use readonly struct for in or ref readonly parameter
dotnet_diagnostic.MA0168.severity = none

# MA0169: Use Equals method instead of operator
dotnet_diagnostic.MA0169.severity = warning
```

# .editorconfig - all rules disabled

```editorconfig
# MA0001: StringComparison is missing
dotnet_diagnostic.MA0001.severity = none

# MA0002: IEqualityComparer<string> or IComparer<string> is missing
dotnet_diagnostic.MA0002.severity = none

# MA0003: Add parameter name to improve readability
dotnet_diagnostic.MA0003.severity = none

# MA0004: Use Task.ConfigureAwait
dotnet_diagnostic.MA0004.severity = none

# MA0005: Use Array.Empty<T>()
dotnet_diagnostic.MA0005.severity = none

# MA0006: Use String.Equals instead of equality operator
dotnet_diagnostic.MA0006.severity = none

# MA0007: Add a comma after the last value
dotnet_diagnostic.MA0007.severity = none

# MA0008: Add StructLayoutAttribute
dotnet_diagnostic.MA0008.severity = none

# MA0009: Add regex evaluation timeout
dotnet_diagnostic.MA0009.severity = none

# MA0010: Mark attributes with AttributeUsageAttribute
dotnet_diagnostic.MA0010.severity = none

# MA0011: IFormatProvider is missing
dotnet_diagnostic.MA0011.severity = none

# MA0012: Do not raise reserved exception type
dotnet_diagnostic.MA0012.severity = none

# MA0013: Types should not extend System.ApplicationException
dotnet_diagnostic.MA0013.severity = none

# MA0014: Do not raise System.ApplicationException type
dotnet_diagnostic.MA0014.severity = none

# MA0015: Specify the parameter name in ArgumentException
dotnet_diagnostic.MA0015.severity = none

# MA0016: Prefer using collection abstraction instead of implementation
dotnet_diagnostic.MA0016.severity = none

# MA0017: Abstract types should not have public or internal constructors
dotnet_diagnostic.MA0017.severity = none

# MA0018: Do not declare static members on generic types (deprecated; use CA1000 instead)
dotnet_diagnostic.MA0018.severity = none

# MA0019: Use EventArgs.Empty
dotnet_diagnostic.MA0019.severity = none

# MA0020: Use direct methods instead of LINQ methods
dotnet_diagnostic.MA0020.severity = none

# MA0021: Use StringComparer.GetHashCode instead of string.GetHashCode
dotnet_diagnostic.MA0021.severity = none

# MA0022: Return Task.FromResult instead of returning null
dotnet_diagnostic.MA0022.severity = none

# MA0023: Add RegexOptions.ExplicitCapture
dotnet_diagnostic.MA0023.severity = none

# MA0024: Use an explicit StringComparer when possible
dotnet_diagnostic.MA0024.severity = none

# MA0025: Implement the functionality instead of throwing NotImplementedException
dotnet_diagnostic.MA0025.severity = none

# MA0026: Fix TODO comment
dotnet_diagnostic.MA0026.severity = none

# MA0027: Prefer rethrowing an exception implicitly
dotnet_diagnostic.MA0027.severity = none

# MA0028: Optimize StringBuilder usage
dotnet_diagnostic.MA0028.severity = none

# MA0029: Combine LINQ methods
dotnet_diagnostic.MA0029.severity = none

# MA0030: Remove useless OrderBy call
dotnet_diagnostic.MA0030.severity = none

# MA0031: Optimize Enumerable.Count() usage
dotnet_diagnostic.MA0031.severity = none

# MA0032: Use an overload with a CancellationToken argument
dotnet_diagnostic.MA0032.severity = none

# MA0033: Do not tag instance fields with ThreadStaticAttribute
dotnet_diagnostic.MA0033.severity = none

# MA0035: Do not use dangerous threading methods
dotnet_diagnostic.MA0035.severity = none

# MA0036: Make class static
dotnet_diagnostic.MA0036.severity = none

# MA0037: Remove empty statement
dotnet_diagnostic.MA0037.severity = none

# MA0038: Make method static (deprecated, use CA1822 instead)
dotnet_diagnostic.MA0038.severity = none

# MA0039: Do not write your own certificate validation method
dotnet_diagnostic.MA0039.severity = none

# MA0040: Forward the CancellationToken parameter to methods that take one
dotnet_diagnostic.MA0040.severity = none

# MA0041: Make property static (deprecated, use CA1822 instead)
dotnet_diagnostic.MA0041.severity = none

# MA0042: Do not use blocking calls in an async method
dotnet_diagnostic.MA0042.severity = none

# MA0043: Use nameof operator in ArgumentException
dotnet_diagnostic.MA0043.severity = none

# MA0044: Remove useless ToString call
dotnet_diagnostic.MA0044.severity = none

# MA0045: Do not use blocking calls in a sync method (need to make calling method async)
dotnet_diagnostic.MA0045.severity = none

# MA0046: Use EventHandler<T> to declare events
dotnet_diagnostic.MA0046.severity = none

# MA0047: Declare types in namespaces
dotnet_diagnostic.MA0047.severity = none

# MA0048: File name must match type name
dotnet_diagnostic.MA0048.severity = none

# MA0049: Type name should not match containing namespace
dotnet_diagnostic.MA0049.severity = none

# MA0050: Validate arguments correctly in iterator methods
dotnet_diagnostic.MA0050.severity = none

# MA0051: Method is too long
dotnet_diagnostic.MA0051.severity = none

# MA0052: Replace constant Enum.ToString with nameof
dotnet_diagnostic.MA0052.severity = none

# MA0053: Make class sealed
dotnet_diagnostic.MA0053.severity = none

# MA0054: Embed the caught exception as innerException
dotnet_diagnostic.MA0054.severity = none

# MA0055: Do not use finalizer
dotnet_diagnostic.MA0055.severity = none

# MA0056: Do not call overridable members in constructor
dotnet_diagnostic.MA0056.severity = none

# MA0057: Class name should end with 'Attribute'
dotnet_diagnostic.MA0057.severity = none

# MA0058: Class name should end with 'Exception'
dotnet_diagnostic.MA0058.severity = none

# MA0059: Class name should end with 'EventArgs'
dotnet_diagnostic.MA0059.severity = none

# MA0060: The value returned by Stream.Read/Stream.ReadAsync is not used
dotnet_diagnostic.MA0060.severity = none

# MA0061: Method overrides should not change default values
dotnet_diagnostic.MA0061.severity = none

# MA0062: Non-flags enums should not be marked with "FlagsAttribute"
dotnet_diagnostic.MA0062.severity = none

# MA0063: Use Where before OrderBy
dotnet_diagnostic.MA0063.severity = none

# MA0064: Avoid locking on publicly accessible instance
dotnet_diagnostic.MA0064.severity = none

# MA0065: Default ValueType.Equals or HashCode is used for struct equality
dotnet_diagnostic.MA0065.severity = none

# MA0066: Hash table unfriendly type is used in a hash table
dotnet_diagnostic.MA0066.severity = none

# MA0067: Use Guid.Empty
dotnet_diagnostic.MA0067.severity = none

# MA0068: Invalid parameter name for nullable attribute
dotnet_diagnostic.MA0068.severity = none

# MA0069: Non-constant static fields should not be visible
dotnet_diagnostic.MA0069.severity = none

# MA0070: Obsolete attributes should include explanations
dotnet_diagnostic.MA0070.severity = none

# MA0071: Avoid using redundant else
dotnet_diagnostic.MA0071.severity = none

# MA0072: Do not throw from a finally block
dotnet_diagnostic.MA0072.severity = none

# MA0073: Avoid comparison with bool constant
dotnet_diagnostic.MA0073.severity = none

# MA0074: Avoid implicit culture-sensitive methods
dotnet_diagnostic.MA0074.severity = none

# MA0075: Do not use implicit culture-sensitive ToString
dotnet_diagnostic.MA0075.severity = none

# MA0076: Do not use implicit culture-sensitive ToString in interpolated strings
dotnet_diagnostic.MA0076.severity = none

# MA0077: A class that provides Equals(T) should implement IEquatable<T>
dotnet_diagnostic.MA0077.severity = none

# MA0078: Use 'Cast' instead of 'Select' to cast
dotnet_diagnostic.MA0078.severity = none

# MA0079: Forward the CancellationToken using .WithCancellation()
dotnet_diagnostic.MA0079.severity = none

# MA0080: Use a cancellation token using .WithCancellation()
dotnet_diagnostic.MA0080.severity = none

# MA0081: Method overrides should not omit params keyword
dotnet_diagnostic.MA0081.severity = none

# MA0082: NaN should not be used in comparisons
dotnet_diagnostic.MA0082.severity = none

# MA0083: ConstructorArgument parameters should exist in constructors
dotnet_diagnostic.MA0083.severity = none

# MA0084: Local variables should not hide other symbols
dotnet_diagnostic.MA0084.severity = none

# MA0085: Anonymous delegates should not be used to unsubscribe from Events
dotnet_diagnostic.MA0085.severity = none

# MA0086: Do not throw from a finalizer
dotnet_diagnostic.MA0086.severity = none

# MA0087: Parameters with [DefaultParameterValue] attributes should also be marked [Optional]
dotnet_diagnostic.MA0087.severity = none

# MA0088: Use [DefaultParameterValue] instead of [DefaultValue]
dotnet_diagnostic.MA0088.severity = none

# MA0089: Optimize string method usage
dotnet_diagnostic.MA0089.severity = none

# MA0090: Remove empty else/finally block
dotnet_diagnostic.MA0090.severity = none

# MA0091: Sender should be 'this' for instance events
dotnet_diagnostic.MA0091.severity = none

# MA0092: Sender should be 'null' for static events
dotnet_diagnostic.MA0092.severity = none

# MA0093: EventArgs should not be null
dotnet_diagnostic.MA0093.severity = none

# MA0094: A class that provides CompareTo(T) should implement IComparable<T>
dotnet_diagnostic.MA0094.severity = none

# MA0095: A class that implements IEquatable<T> should override Equals(object)
dotnet_diagnostic.MA0095.severity = none

# MA0096: A class that implements IComparable<T> should also implement IEquatable<T>
dotnet_diagnostic.MA0096.severity = none

# MA0097: A class that implements IComparable<T> or IComparable should override comparison operators
dotnet_diagnostic.MA0097.severity = none

# MA0098: Use indexer instead of LINQ methods
dotnet_diagnostic.MA0098.severity = none

# MA0099: Use Explicit enum value instead of 0
dotnet_diagnostic.MA0099.severity = none

# MA0100: Await task before disposing of resources
dotnet_diagnostic.MA0100.severity = none

# MA0101: String contains an implicit end of line character
dotnet_diagnostic.MA0101.severity = none

# MA0102: Make member readonly
dotnet_diagnostic.MA0102.severity = none

# MA0103: Use SequenceEqual instead of equality operator
dotnet_diagnostic.MA0103.severity = none

# MA0104: Do not create a type with a name from the BCL
dotnet_diagnostic.MA0104.severity = none

# MA0105: Use the lambda parameters instead of using a closure
dotnet_diagnostic.MA0105.severity = none

# MA0106: Avoid closure by using an overload with the 'factoryArgument' parameter
dotnet_diagnostic.MA0106.severity = none

# MA0107: Do not use culture-sensitive object.ToString
dotnet_diagnostic.MA0107.severity = none

# MA0108: Remove redundant argument value
dotnet_diagnostic.MA0108.severity = none

# MA0109: Consider adding an overload with a Span<T> or Memory<T>
dotnet_diagnostic.MA0109.severity = none

# MA0110: Use the Regex source generator
dotnet_diagnostic.MA0110.severity = none

# MA0111: Use string.Create instead of FormattableString
dotnet_diagnostic.MA0111.severity = none

# MA0112: Use 'Count > 0' instead of 'Any()'
dotnet_diagnostic.MA0112.severity = none

# MA0113: Use DateTime.UnixEpoch
dotnet_diagnostic.MA0113.severity = none

# MA0114: Use DateTimeOffset.UnixEpoch
dotnet_diagnostic.MA0114.severity = none

# MA0115: Unknown component parameter
dotnet_diagnostic.MA0115.severity = none

# MA0116: Parameters with [SupplyParameterFromQuery] attributes should also be marked as [Parameter]
dotnet_diagnostic.MA0116.severity = none

# MA0117: Parameters with [EditorRequired] attributes should also be marked as [Parameter]
dotnet_diagnostic.MA0117.severity = none

# MA0118: [JSInvokable] methods must be public
dotnet_diagnostic.MA0118.severity = none

# MA0119: JSRuntime must not be used in OnInitialized or OnInitializedAsync
dotnet_diagnostic.MA0119.severity = none

# MA0120: Use InvokeVoidAsync when the returned value is not used
dotnet_diagnostic.MA0120.severity = none

# MA0121: Do not overwrite parameter value
dotnet_diagnostic.MA0121.severity = none

# MA0122: Parameters with [SupplyParameterFromQuery] attributes are only valid in routable components (@page)
dotnet_diagnostic.MA0122.severity = none

# MA0123: Sequence number must be a constant
dotnet_diagnostic.MA0123.severity = none

# MA0124: Log parameter type is not valid
dotnet_diagnostic.MA0124.severity = none

# MA0125: The list of log parameter types contains an invalid type
dotnet_diagnostic.MA0125.severity = none

# MA0126: The list of log parameter types contains a duplicate
dotnet_diagnostic.MA0126.severity = none

# MA0127: Use String.Equals instead of is pattern
dotnet_diagnostic.MA0127.severity = none

# MA0128: Use 'is' operator instead of SequenceEqual
dotnet_diagnostic.MA0128.severity = none

# MA0129: Await task in using statement
dotnet_diagnostic.MA0129.severity = none

# MA0130: GetType() should not be used on System.Type instances
dotnet_diagnostic.MA0130.severity = none

# MA0131: ArgumentNullException.ThrowIfNull should not be used with non-nullable types
dotnet_diagnostic.MA0131.severity = none

# MA0132: Do not convert implicitly to DateTimeOffset
dotnet_diagnostic.MA0132.severity = none

# MA0133: Use DateTimeOffset instead of relying on the implicit conversion
dotnet_diagnostic.MA0133.severity = none

# MA0134: Observe result of async calls
dotnet_diagnostic.MA0134.severity = none

# MA0135: The log parameter has no configured type
dotnet_diagnostic.MA0135.severity = none

# MA0136: Raw String contains an implicit end of line character
dotnet_diagnostic.MA0136.severity = none

# MA0137: Use 'Async' suffix when a method returns an awaitable type
dotnet_diagnostic.MA0137.severity = none

# MA0138: Do not use 'Async' suffix when a method does not return an awaitable type
dotnet_diagnostic.MA0138.severity = none

# MA0139: Log parameter type is not valid
dotnet_diagnostic.MA0139.severity = none

# MA0140: Both if and else branch have identical code
dotnet_diagnostic.MA0140.severity = none

# MA0141: Use pattern matching instead of inequality operators for null check
dotnet_diagnostic.MA0141.severity = none

# MA0142: Use pattern matching instead of equality operators for null check
dotnet_diagnostic.MA0142.severity = none

# MA0143: Primary constructor parameters should be readonly
dotnet_diagnostic.MA0143.severity = none

# MA0144: Use System.OperatingSystem to check the current OS
dotnet_diagnostic.MA0144.severity = none

# MA0145: Signature for [UnsafeAccessorAttribute] method is not valid
dotnet_diagnostic.MA0145.severity = none

# MA0146: Name must be set explicitly on local functions
dotnet_diagnostic.MA0146.severity = none

# MA0147: Avoid async void method for delegate
dotnet_diagnostic.MA0147.severity = none

# MA0148: Use pattern matching instead of equality operators for discrete value
dotnet_diagnostic.MA0148.severity = none

# MA0149: Use pattern matching instead of inequality operators for discrete value
dotnet_diagnostic.MA0149.severity = none

# MA0150: Do not call the default object.ToString explicitly
dotnet_diagnostic.MA0150.severity = none

# MA0151: DebuggerDisplay must contain valid members
dotnet_diagnostic.MA0151.severity = none

# MA0152: Use Unwrap instead of using await twice
dotnet_diagnostic.MA0152.severity = none

# MA0153: Do not log symbols decorated with DataClassificationAttribute directly
dotnet_diagnostic.MA0153.severity = none

# MA0154: Use langword in XML comment
dotnet_diagnostic.MA0154.severity = none

# MA0155: Do not use async void methods
dotnet_diagnostic.MA0155.severity = none

# MA0156: Use 'Async' suffix when a method returns IAsyncEnumerable<T>
dotnet_diagnostic.MA0156.severity = none

# MA0157: Do not use 'Async' suffix when a method returns IAsyncEnumerable<T>
dotnet_diagnostic.MA0157.severity = none

# MA0158: Use System.Threading.Lock
dotnet_diagnostic.MA0158.severity = none

# MA0159: Use 'Order' instead of 'OrderBy'
dotnet_diagnostic.MA0159.severity = none

# MA0160: Use ContainsKey instead of TryGetValue
dotnet_diagnostic.MA0160.severity = none

# MA0161: UseShellExecute must be explicitly set
dotnet_diagnostic.MA0161.severity = none

# MA0162: Use Process.Start overload with ProcessStartInfo
dotnet_diagnostic.MA0162.severity = none

# MA0163: UseShellExecute must be false when redirecting standard input or output
dotnet_diagnostic.MA0163.severity = none

# MA0164: Use parentheses to make not pattern clearer
dotnet_diagnostic.MA0164.severity = none

# MA0165: Make interpolated string
dotnet_diagnostic.MA0165.severity = none

# MA0166: Forward the TimeProvider to methods that take one
dotnet_diagnostic.MA0166.severity = none

# MA0167: Use an overload with a TimeProvider argument
dotnet_diagnostic.MA0167.severity = none

# MA0168: Use readonly struct for in or ref readonly parameter
dotnet_diagnostic.MA0168.severity = none

# MA0169: Use Equals method instead of operator
dotnet_diagnostic.MA0169.severity = none
```
