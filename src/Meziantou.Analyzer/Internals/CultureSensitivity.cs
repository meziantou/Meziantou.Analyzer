namespace Meziantou.Analyzer.Internals;

[Flags]
internal enum CultureSensitivity
{
    CultureInsensitive = 0,
    MaybeCultureSensitiveOpaqueRuntimeType = 1,
    MaybeCultureSensitiveUnsealedType = 2,
    CultureSensitive = 4,
}
