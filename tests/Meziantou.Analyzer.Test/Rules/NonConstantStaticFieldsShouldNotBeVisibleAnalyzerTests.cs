using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class NonConstantStaticFieldsShouldNotBeVisibleAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<NonConstantStaticFieldsShouldNotBeVisibleAnalyzer>();
    }

    [Fact]
    public async Task ReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
public class Sample
{
    public static int [||]a = 0;
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClass()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
internal class Sample
{
    public static int a = 0;
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task StaticReadOnly()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
public class Sample
{
    public static readonly int a = 0;
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task InstanceField()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
public class Sample
{
    public int a = 0;
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task EnumMembers()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
public enum Sample
{
    A = 1,
    B,
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Const()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
public class Sample
{
    public const int a = 0;
}")
              .ValidateAsync();
    }
}
