|Id|Category|Description|Severity|Is enabled|Code fix|
|--|--------|-----------|:------:|:--------:|:------:|
|[MA0001](Rules/MA0001)|Usage|StringComparison is missing|Warning|True|True|
|[MA0002](Rules/MA0002)|Usage|IEqualityComparer<string> is missing|Warning|True|True|
|[MA0003](Rules/MA0003)|Style|Name parameter|Info|True|True|
|[MA0004](Rules/MA0004)|Usage|Use .ConfigureAwait(false)|Warning|True|True|
|[MA0005](Rules/MA0005)|Performance|Use Array.Empty<T>()|Warning|True|True|
|[MA0006](Rules/MA0006)|Usage|use String.Equals|Warning|True|True|
|[MA0007](Rules/MA0007)|Style|Add comma after the last property|Info|True|True|
|[MA0008](Rules/MA0008)|Performance|Add StructLayoutAttribute|Warning|True|True|
|[MA0009](Rules/MA0009)|Security|Add timeout parameter|Warning|True|False|
|[MA0010](Rules/MA0010)|Design|Mark attributes with AttributeUsageAttribute|Warning|True|True|
|[MA0011](Rules/MA0011)|Usage|IFormatProvider is missing|Warning|True|False|
|[MA0012](Rules/MA0012)|Design|Do not raise reserved exception type|Warning|True|False|
|[MA0013](Rules/MA0013)|Design|Types should not extend System.ApplicationException|Warning|True|False|
|[MA0014](Rules/MA0014)|Design|Do not raise System.ApplicationException type|Warning|True|False|
|[MA0015](Rules/MA0015)|Usage|Specify the parameter name|Warning|True|False|
|[MA0016](Rules/MA0016)|Design|Prefer return collection abstraction instead of implementation|Warning|True|False|
|[MA0017](Rules/MA0017)|Design|Abstract types should not have public or internal constructors|Warning|True|True|
|[MA0018](Rules/MA0018)|Design|Do not declare static members on generic types|Warning|True|False|
|[MA0019](Rules/MA0019)|Usage|Use EventArgs.Empty|Warning|True|False|
|[MA0020](Rules/MA0020)|Performance|Use direct methods instead of extension methods|Info|True|True|
|[MA0021](Rules/MA0021)|Usage|Use StringComparer.GetHashCode|Warning|True|True|
|[MA0022](Rules/MA0022)|Design|Return Task.FromResult instead of returning null|Warning|True|False|
|[MA0023](Rules/MA0023)|Security|Add RegexOptions.ExplicitCapture|Warning|True|False|
|[MA0024](Rules/MA0024)|Usage|Use StringComparer.Ordinal|Warning|True|True|
|[MA0025](Rules/MA0025)|Design|TODO Implement the functionality|Warning|True|False|
|[MA0026](Rules/MA0026)|Design|Fix TODO comment|Warning|True|False|
|[MA0027](Rules/MA0027)|Usage|Do not remove original exception|Warning|True|True|
|[MA0028](Rules/MA0028)|Performance|Optimize StringBuilder usage|Info|True|True|
|[MA0029](Rules/MA0029)|Performance|Optimize LINQ usage|Info|True|True|
|[MA0030](Rules/MA0030)|Performance|Optimize LINQ usage|Info|True|True|
|[MA0031](Rules/MA0031)|Performance|Optimize Enumerable.Count usage|Info|True|True|
|[MA0032](Rules/MA0032)|Usage|Use a cancellation token|Hidden|True|False|
|[MA0033](Rules/MA0033)|Design|Don't tag instance fields with ThreadStaticAttribute|Warning|True|False|
|[MA0034](Rules/MA0034)|Design|Don't use instance fields of type AsyncLocal<T>|Warning|True|False|
|[MA0035](Rules/MA0035)|Usage|Don't use dangerous threading methods|Warning|True|False|
|[MA0036](Rules/MA0036)|Design|Make class static|Info|True|True|
|[MA0037](Rules/MA0037)|Usage|Remove empty statement|Error|True|False|
|[MA0038](Rules/MA0038)|Design|Make method static|Info|True|True|
|[MA0039](Rules/MA0039)|Security|Do not write your own certificate validation method|Error|True|False|
|[MA0040](Rules/MA0040)|Usage|Use a cancellation token|Info|True|False|
|[MA0041](Rules/MA0041)|Design|Make property static|Info|True|True|
|[MA0042](Rules/MA0042)|Design|Do not use blocking call|Info|True|False|
|[MA0043](Rules/MA0043)|Usage|Use nameof operator|Info|True|True|
|[MA0044](Rules/MA0044)|Performance|Remove ToString call|Info|True|False|
|[MA0045](Rules/MA0045)|Design|Do not use blocking call (make method async)|Info|True|False|
|[MA0046](Rules/MA0046)|Design|Use EventHandler<T>|Warning|True|False|
|[MA0047](Rules/MA0047)|Design|Declare types in namespaces|Warning|True|False|
|[MA0048](Rules/MA0048)|Design|File name must match type name|Warning|True|False|
|[MA0049](Rules/MA0049)|Design|Type name should not match namespace|Error|True|False|
|[MA0050](Rules/MA0050)|Design|Validate arguments correctly|Info|True|True|
|[MA0051](Rules/MA0051)|Design|Method is too long|Warning|True|False|
|[MA0052](Rules/MA0052)|Performance|Replace with nameof|Info|True|True|
|[MA0053](Rules/MA0053)|Design|Make class sealed|Info|True|True|
|[MA0054](Rules/MA0054)|Design|Preserve the catched exception in the innerException|Warning|True|False|
|[MA0055](Rules/MA0055)|Design|Do not use destructor|Warning|True|False|
|[MA0056](Rules/MA0056)|Design|Do not call overridable members in constructor|Warning|True|False|
|[MA0057](Rules/MA0057)|Naming|Class name should end with 'Attribute'|Info|True|False|
|[MA0058](Rules/MA0058)|Naming|Class name should end with 'Exception'|Info|True|False|
|[MA0059](Rules/MA0059)|Naming|Class name should end with 'EventArgs'|Info|True|False|
|[MA0060](Rules/MA0060)|Design|The value returned by Stream.Read is not used|Warning|True|False|
|[MA0061](Rules/MA0061)|Design|Method overrides should not change parameter defaults|Warning|True|False|
|[MA0062](Rules/MA0062)|Design|Non-flags enums should not be marked with "FlagsAttribute"|Warning|True|False|
|[MA0063](Rules/MA0063)|Performance|Optimize Enumerable.OrderBy usage|Info|True|False|
