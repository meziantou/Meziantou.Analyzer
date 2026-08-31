using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotDeclareStaticMembersOnGenericTypes>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotDeclareStaticMembersOnGenericTypesTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task StaticMembersInNonGenericClass()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Test
            {
                public static string field;
                public static string Prop => throw null;
                public static string Method() => throw null;

                public string field2;
                public string Prop2 => throw null;
                public string Method2() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonStaticMembersInGenericClass()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Test<T>
            {
                public string field2;
                public string Prop2 => throw null;
                public string Method2() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StaticMembers_Field()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Test<T>
            {
                public static string {|MA0018:field|};
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StaticMembers_Property()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Test<T>
            {
                public static string {|MA0018:Prop|} => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StaticMembers_Method()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Test<T>
            {
                public static string {|MA0018:Method|}() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StaticMembers_Operator()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Test<T>
            {
                public static implicit operator Test<T>(int i) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Const()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Test<T>
            {
                public const string PasswordlessSignInPurpose = "PasswordlessSignIn";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonPublicStaticMembers()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Test<T>
            {
                internal protected static string Method1() => throw null;
                protected static string Method2() => throw null;
                private protected static string Method3() => throw null;
                internal static string Method4() => throw null;
                private static string Method5() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StaticAbstract()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net70;
        test.TestCode = """
            public interface IFactory<TSelf> where TSelf : IFactory<TSelf>
            {
                static abstract TSelf Create();
            }
            """;

        return test.RunAsync();
    }
}
