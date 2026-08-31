using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.EqualityShouldBeCorrectlyImplementedAnalyzer,
    Meziantou.Analyzer.Rules.EqualityShouldBeCorrectlyImplementedFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class EqualityShouldBeCorrectlyImplementedAnalyzerMA0095Tests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task DirectImplementation_WithoutEqualsObject_ShouldTrigger()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            public sealed class {|MA0095:TriggersMA0095AndCA1067|} : IEquatable<TriggersMA0095AndCA1067>
            {
                public bool Equals(TriggersMA0095AndCA1067? other) => true;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MA0095_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            public sealed class {|MA0095:Test|} : IEquatable<Test>
            {
                public bool Equals(Test? other) => true;
            }
            """;
        test.FixedCode = """
            using System;

            public sealed class Test : IEquatable<Test>
            {
                public bool Equals(Test? other) => true;
                public override bool Equals(object obj) => obj is Test other && Equals(other);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DirectImplementation_WithEqualsObject_ShouldNotTrigger()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            public sealed class Test : IEquatable<Test>
            {
                public bool Equals(Test? other) => true;
                public override bool Equals(object? obj) => true;
                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CRTP_WithoutEqualsObject_ShouldNotTrigger()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            public abstract class Crtp<T> : IEquatable<T> where T : Crtp<T>
            {
                public bool Equals(T? other) => true;
            }

            public sealed class TriggersMA0095Only : Crtp<TriggersMA0095Only>;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CRTP_WithEqualsObjectInBase_ShouldNotTrigger()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            public abstract class Crtp<T> : IEquatable<T> where T : Crtp<T>
            {
                public bool Equals(T? other) => true;
                public override bool Equals(object? obj) => true;
                public override int GetHashCode() => 0;
            }

            public sealed class DerivedClass : Crtp<DerivedClass>;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InheritedIEquatable_WithDirectImplementationToo_ShouldTrigger()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            public abstract class Base : IEquatable<Base>
            {
                public bool Equals(Base? other) => true;
                public override bool Equals(object? obj) => true;
                public override int GetHashCode() => 0;
            }

            public sealed class {|MA0095:Derived|} : Base, IEquatable<Derived>
            {
                public bool Equals(Derived? other) => true;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Struct_DirectImplementation_WithoutEqualsObject_ShouldTrigger()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            public struct {|MA0095:TestStruct|} : IEquatable<TestStruct>
            {
                public bool Equals(TestStruct other) => true;
            }
            """;

        return test.RunAsync();
    }
}
