using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class EqualityShouldBeCorrectlyImplementedAnalyzerMA0095Tests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<EqualityShouldBeCorrectlyImplementedAnalyzer>()
            .WithCodeFixProvider<EqualityShouldBeCorrectlyImplementedFixer>();
    }

    [Fact]
    public async Task DirectImplementation_WithoutEqualsObject_ShouldTrigger()
    {
        var originalCode = """
using System;

public sealed class [|TriggersMA0095AndCA1067|] : IEquatable<TriggersMA0095AndCA1067>
{
    public bool Equals(TriggersMA0095AndCA1067? other) => true;
}
""";

        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DirectImplementation_WithEqualsObject_ShouldNotTrigger()
    {
        var originalCode = """
using System;

public sealed class Test : IEquatable<Test>
{
    public bool Equals(Test? other) => true;
    public override bool Equals(object? obj) => true;
    public override int GetHashCode() => 0;
}
""";

        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CRTP_WithoutEqualsObject_ShouldNotTrigger()
    {
        var originalCode = """
using System;

public abstract class Crtp<T> : IEquatable<T> where T : Crtp<T>
{
    public bool Equals(T? other) => true;
}

public sealed class TriggersMA0095Only : Crtp<TriggersMA0095Only>;
""";

        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CRTP_WithEqualsObjectInBase_ShouldNotTrigger()
    {
        var originalCode = """
using System;

public abstract class Crtp<T> : IEquatable<T> where T : Crtp<T>
{
    public bool Equals(T? other) => true;
    public override bool Equals(object? obj) => true;
    public override int GetHashCode() => 0;
}

public sealed class DerivedClass : Crtp<DerivedClass>;
""";

        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InheritedIEquatable_WithDirectImplementationToo_ShouldTrigger()
    {
        var originalCode = """
using System;

public abstract class Base : IEquatable<Base>
{
    public bool Equals(Base? other) => true;
    public override bool Equals(object? obj) => true;
    public override int GetHashCode() => 0;
}

public sealed class [|Derived|] : Base, IEquatable<Derived>
{
    public bool Equals(Derived? other) => true;
}
""";

        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Struct_DirectImplementation_WithoutEqualsObject_ShouldTrigger()
    {
        var originalCode = """
using System;

public struct [|TestStruct|] : IEquatable<TestStruct>
{
    public bool Equals(TestStruct other) => true;
}
""";

        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }
}
