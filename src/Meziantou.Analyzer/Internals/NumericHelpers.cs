namespace Meziantou.Analyzer.Internals;

internal static class NumericHelpers
{
    public static bool IsZero(object? value)
    {
        return value switch
        {
            int i => i == 0,
            long l => l == 0,
            double d => d == 0.0,
            float f => f == 0.0f,
            decimal dec => dec == 0m,
            byte b => b == 0,
            sbyte sb => sb == 0,
            short s => s == 0,
            ushort us => us == 0,
            uint ui => ui == 0,
            ulong ul => ul == 0,
            _ => false,
        };
    }
}
