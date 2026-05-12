`Meziantou.Analyzer.Annotations` enables you to configure certain analyzer rules by adding annotations directly to your code.

By default, all usages of attributes from `Meziantou.Analyzer.Annotations` are removed from the compiled assembly metadata. This means your binaries will not reference the `Meziantou.Analyzer.Annotations.dll` assembly.

If you want to keep these attributes in the metadata (for example, for reflection or tooling purposes), define the `MEZIANTOU_ANALYZER_ANNOTATIONS` conditional compilation symbol in your project settings.

## ExcludeFromBlockingCallAnalysisAttribute

Use `ExcludeFromBlockingCallAnalysisAttribute` to exclude specific MA0042/MA0045 method/property diagnostics at the assembly level.

```csharp
[assembly: Meziantou.Analyzer.Annotations.ExcludeFromBlockingCallAnalysisAttribute("M:System.Threading.Tasks.Task.Wait")]
[assembly: Meziantou.Analyzer.Annotations.ExcludeFromBlockingCallAnalysisAttribute(typeof(System.Threading.Thread), "Sleep", typeof(int))]
```
