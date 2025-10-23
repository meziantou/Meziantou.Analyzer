using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class NullableAttributeUsageAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<NullableAttributeUsageAnalyzer>();
    }

    [Fact]
    public async Task ParameterDoesNotExist()
    {
        const string SourceCode = @"
class Test
{
    [return: [|System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute(""unknown"")|]]
    public void A(string a) { }
}

namespace System.Diagnostics.CodeAnalysis
{
    using System;

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
    public class NotNullIfNotNullAttribute : System.Attribute
    {
        public NotNullIfNotNullAttribute (string parameterName) => throw null;
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ParameterExists()
    {
        const string SourceCode = @"
class Test
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute(""a"")]
    public void A(string a) { }
}

namespace System.Diagnostics.CodeAnalysis
{
    using System;

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
    public class NotNullIfNotNullAttribute : System.Attribute
    {
        public NotNullIfNotNullAttribute (string parameterName) => throw null;
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
