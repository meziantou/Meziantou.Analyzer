# v1.0.5

- MA0004 does not report diagnostic when the type implements `System.Windows.Input.ICommand`

# v1.0.6

- MA0003 does not report diagnostic for `Microsoft.VisualStudio.TestTools.UnitTesting.Assert`, `NUnit.Framework.Assert` and `Xunit.Assert`

# v1.0.7

- New analyzer: MA0005 Use `Array.Empty<T>` instead of `new int[0]`

# v1.0.8

- MA0003 reports as information instead of warning
- MA0003 doesn't process generated code
- MA0004 correctly detects ConfiguredTaskAwaitable<T>
- MA0005 doesn't report diagnostic for `params` arguments

# v1.0.9

- MA0002 now support `ConcurrentDictionary<string, TValue>`