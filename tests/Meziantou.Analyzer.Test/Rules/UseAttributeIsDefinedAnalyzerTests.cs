using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseAttributeIsDefinedAnalyzer,
    Meziantou.Analyzer.Rules.UseAttributeIsDefinedFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseAttributeIsDefinedAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task GetCustomAttribute_NotEqualNull_MemberInfo()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = {|MA0179:member.GetCustomAttribute<ObsoleteAttribute>() != null|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = Attribute.IsDefined(member, typeof(ObsoleteAttribute));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttribute_EqualNull_MemberInfo()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = {|MA0179:member.GetCustomAttribute<ObsoleteAttribute>() == null|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = !Attribute.IsDefined(member, typeof(ObsoleteAttribute));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttribute_IsNull_MemberInfo()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = {|MA0179:member.GetCustomAttribute<ObsoleteAttribute>() is null|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = !Attribute.IsDefined(member, typeof(ObsoleteAttribute));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttribute_IsNotNull_MemberInfo()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = {|MA0179:member.GetCustomAttribute<ObsoleteAttribute>() is not null|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = Attribute.IsDefined(member, typeof(ObsoleteAttribute));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttributes_Any_MemberInfo()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Linq;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = {|MA0179:member.GetCustomAttributes<ObsoleteAttribute>().Any()|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Linq;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = Attribute.IsDefined(member, typeof(ObsoleteAttribute));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttribute_NotEqualNull_Type()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(Type type)
                {
                    _ = {|MA0179:type.GetCustomAttribute<ObsoleteAttribute>() != null|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(Type type)
                {
                    _ = Attribute.IsDefined(type, typeof(ObsoleteAttribute));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttribute_NotEqualNull_Assembly()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(Assembly assembly)
                {
                    _ = {|MA0179:assembly.GetCustomAttribute<ObsoleteAttribute>() != null|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(Assembly assembly)
                {
                    _ = Attribute.IsDefined(assembly, typeof(ObsoleteAttribute));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttribute_NotEqualNull_Module()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(Module module)
                {
                    _ = {|MA0179:module.GetCustomAttribute<ObsoleteAttribute>() != null|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(Module module)
                {
                    _ = Attribute.IsDefined(module, typeof(ObsoleteAttribute));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttribute_WithInherit_NotEqualNull_MemberInfo()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = {|MA0179:member.GetCustomAttribute<ObsoleteAttribute>(inherit: true) != null|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = Attribute.IsDefined(member, typeof(ObsoleteAttribute), inherit: true);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttributes_WithInherit_Any_MemberInfo()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Linq;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = {|MA0179:member.GetCustomAttributes<ObsoleteAttribute>(inherit: true).Any()|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Linq;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = Attribute.IsDefined(member, typeof(ObsoleteAttribute), inherit: true);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttribute_UsedDirectly_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    var attr = member.GetCustomAttribute<ObsoleteAttribute>();
                    _ = attr.Message;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttribute_IsNotDeclarationPattern_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                string Test(Type type)
                {
                    if (type.GetCustomAttribute<ObsoleteAttribute>() is not ObsoleteAttribute attribute)
                        throw new ArgumentException(nameof(type));

                    return attribute.Message;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttribute_IsDeclarationPattern_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                string Test(Type type)
                {
                    if (type.GetCustomAttribute<ObsoleteAttribute>() is ObsoleteAttribute attribute)
                        return attribute.Message;

                    return string.Empty;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttributes_WithPredicate_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Linq;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = member.GetCustomAttributes<ObsoleteAttribute>().Any(a => a.Message != null);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttributes_Any_WithTruePredicate_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Linq;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = member.GetCustomAttributes<ObsoleteAttribute>().Any(attr => true);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttributes_Count_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Linq;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = {|MA0179:member.GetCustomAttributes<ObsoleteAttribute>().Count() > 0|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Linq;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = Attribute.IsDefined(member, typeof(ObsoleteAttribute));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttributes_Count_WithPredicate_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Linq;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = member.GetCustomAttributes<ObsoleteAttribute>().Count(a => a.Message != null) > 0;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttribute_NullComparison_ReversedOrder()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = {|MA0179:null != member.GetCustomAttribute<ObsoleteAttribute>()|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = Attribute.IsDefined(member, typeof(ObsoleteAttribute));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttributes_Length_GreaterThanZero()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = {|MA0179:member.GetCustomAttributes(typeof(ObsoleteAttribute), false).Length > 0|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = Attribute.IsDefined(member, typeof(ObsoleteAttribute), false);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttributes_Length_NotEqualZero()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = {|MA0179:member.GetCustomAttributes(typeof(ObsoleteAttribute), false).Length != 0|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = Attribute.IsDefined(member, typeof(ObsoleteAttribute), false);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttributes_Length_EqualZero()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = {|MA0179:member.GetCustomAttributes(typeof(ObsoleteAttribute), false).Length == 0|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = !Attribute.IsDefined(member, typeof(ObsoleteAttribute), false);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetCustomAttributes_Length_GreaterThanOrEqualOne()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = {|MA0179:member.GetCustomAttributes(typeof(ObsoleteAttribute), false).Length >= 1|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = Attribute.IsDefined(member, typeof(ObsoleteAttribute), false);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Attribute_GetCustomAttributes_Length_GreaterThanZero()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = {|MA0179:Attribute.GetCustomAttributes(member, typeof(ObsoleteAttribute)).Length > 0|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = Attribute.IsDefined(member, typeof(ObsoleteAttribute));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Attribute_GetCustomAttribute_NotEqualNull()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = {|MA0179:Attribute.GetCustomAttribute(member, typeof(ObsoleteAttribute)) != null|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Reflection;

            class TestClass
            {
                void Test(MemberInfo member)
                {
                    _ = Attribute.IsDefined(member, typeof(ObsoleteAttribute));
                }
            }
            """;

        return test.RunAsync();
    }
}
