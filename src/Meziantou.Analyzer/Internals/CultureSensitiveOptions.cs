using System;

namespace Meziantou.Analyzer.Internals;

[Flags]
internal enum CultureSensitiveOptions
{
    None,
    UnwrapNullableOfT = 1,
    UseInvocationReturnType = 2,
}
