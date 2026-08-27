namespace Meziantou.Analyzer.Test.Helpers;

public static class TargetFrameworkExtensions
{
    /// <summary>
    /// The moniker of the target framework, as the Roslyn test framework names the set of references of a project.
    /// The references themselves are the ones downloaded by <see cref="TestHelper.ProjectBuilder"/>.
    /// </summary>
    public static string ToTargetFrameworkMoniker(this TargetFramework targetFramework) => targetFramework switch
    {
        TargetFramework.NetStandard2_0 => "netstandard2.0",
        TargetFramework.NetStandard2_1 => "netstandard2.1",
        TargetFramework.Net4_8 => "net48",
        TargetFramework.Net5_0 or TargetFramework.AspNetCore5_0 or TargetFramework.WindowsDesktop5_0 => "net5.0",
        TargetFramework.Net6_0 or TargetFramework.AspNetCore6_0 => "net6.0",
        TargetFramework.Net7_0 or TargetFramework.AspNetCore7_0 => "net7.0",
        TargetFramework.Net8_0 or TargetFramework.AspNetCore8_0 => "net8.0",
        TargetFramework.Net9_0 or TargetFramework.AspNetCore9_0 => "net9.0",
        TargetFramework.Net10_0 => "net10.0",
        TargetFramework.Net11_0 => "net11.0",
        _ => throw new ArgumentOutOfRangeException(nameof(targetFramework)),
    };
}
