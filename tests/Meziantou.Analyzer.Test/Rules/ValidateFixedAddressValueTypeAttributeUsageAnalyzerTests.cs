namespace Meziantou.Analyzer.Test.Rules;

public sealed class ValidateFixedAddressValueTypeAttributeUsageAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder() => new ProjectBuilder()
        .WithTargetFramework(TargetFramework.Net10_0)
        .WithAnalyzer<ValidateFixedAddressValueTypeAttributeUsageAnalyzer>();

    [Fact]
    public async Task ValidField()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class Sample
                {
                    [System.Runtime.CompilerServices.FixedAddressValueType]
                    static int _field;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task FieldMustBeStatic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class Sample
                {
                    [System.Runtime.CompilerServices.FixedAddressValueType]
                    int {|MA0207:_field|};
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task FieldTypeMustBeValueType()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class Sample
                {
                    [System.Runtime.CompilerServices.FixedAddressValueType]
                    static {|MA0208:string|} _field;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task BothDiagnostics()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class Sample
                {
                    [System.Runtime.CompilerServices.FixedAddressValueType]
                    {|MA0208:string|} {|MA0207:_field|};
                }
                """)
            .ValidateAsync();
    }
}
