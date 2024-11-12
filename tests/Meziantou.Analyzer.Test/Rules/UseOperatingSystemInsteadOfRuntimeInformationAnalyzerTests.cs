using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public class UseOperatingSystemInsteadOfRuntimeInformationAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseOperatingSystemInsteadOfRuntimeInformationAnalyzer>()
            .WithTargetFramework(TargetFramework.Net8_0)
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication);
    }

    [Fact]
    public async Task ShouldReport()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [||]System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ShouldNotReport_WhenOperatingSystemIsNotAvailable()
    {
        await CreateProjectBuilder()
            .WithTargetFramework(TargetFramework.NetStandard2_0)
            .WithSourceCode("""
                System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ShouldNotReport_WhenDynamic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                var a = System.Runtime.InteropServices.OSPlatform.Windows;
                System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(a);
                """)
            .ValidateAsync();
    }
}
