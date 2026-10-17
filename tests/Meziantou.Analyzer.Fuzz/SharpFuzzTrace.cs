using System.Runtime.InteropServices;

namespace Meziantou.Analyzer.Fuzz;

internal static class SharpFuzzTrace
{
    // The size of the coverage map of libFuzzer (MAP_SIZE in libfuzzer-dotnet)
    private const int MapSize = 1 << 16;

    /// <summary>
    /// The instrumented assemblies record the coverage in <see cref="SharpFuzz.Common.Trace.SharedMem"/>, which is only allocated
    /// when the harness runs under libFuzzer. Allocate it, so the instrumented assemblies can run outside of libFuzzer.
    /// </summary>
    public static void EnsureInitialized()
    {
        unsafe
        {
            if (SharpFuzz.Common.Trace.SharedMem is null)
            {
                SharpFuzz.Common.Trace.SharedMem = (byte*)NativeMemory.AllocZeroed(MapSize);
            }
        }
    }
}
