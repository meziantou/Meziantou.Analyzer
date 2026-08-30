using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseOperatingSystemInsteadOfRuntimeInformationAnalyzer,
    Meziantou.Analyzer.Rules.UseOperatingSystemInsteadOfRuntimeInformationFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public class UseOperatingSystemInsteadOfRuntimeInformationAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Fact]
    public Task ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
            [|System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)|];
            """;
        test.FixedCode = """
            System.OperatingSystem.IsWindows();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ShouldReport_MacOS()
    {
        var test = CreateTest();
        test.TestCode = """
            [|System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)|];
            """;
        test.FixedCode = """
            System.OperatingSystem.IsMacOS();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ShouldNotReport_WhenOperatingSystemIsNotAvailable()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
        test.TestCode = """
            System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ShouldNotReport_WhenDynamic()
    {
        var test = CreateTest();
        test.TestCode = """
            var a = System.Runtime.InteropServices.OSPlatform.Windows;
            System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(a);
            """;

        return test.RunAsync();
    }
}
