using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseAttributeIsDefinedAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseAttributeIsDefinedAnalyzer>()
            .WithCodeFixProvider<UseAttributeIsDefinedFixer>();
    }

    [Fact]
    public async Task GetCustomAttribute_NotEqualNull_MemberInfo()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = [|member.GetCustomAttribute<ObsoleteAttribute>() != null|];
    }
}
""")
              .ShouldFixCodeWith("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = Attribute.IsDefined(member, typeof(ObsoleteAttribute));
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task GetCustomAttribute_EqualNull_MemberInfo()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = [|member.GetCustomAttribute<ObsoleteAttribute>() == null|];
    }
}
""")
              .ShouldFixCodeWith("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = !Attribute.IsDefined(member, typeof(ObsoleteAttribute));
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task GetCustomAttribute_IsNull_MemberInfo()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = [|member.GetCustomAttribute<ObsoleteAttribute>() is null|];
    }
}
""")
              .ShouldFixCodeWith("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = !Attribute.IsDefined(member, typeof(ObsoleteAttribute));
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task GetCustomAttribute_IsNotNull_MemberInfo()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = [|member.GetCustomAttribute<ObsoleteAttribute>() is not null|];
    }
}
""")
              .ShouldFixCodeWith("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = Attribute.IsDefined(member, typeof(ObsoleteAttribute));
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task GetCustomAttributes_Any_MemberInfo()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
using System.Linq;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = [|member.GetCustomAttributes<ObsoleteAttribute>().Any()|];
    }
}
""")
              .ShouldFixCodeWith("""
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
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task GetCustomAttribute_NotEqualNull_Type()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;

class TestClass
{
    void Test(Type type)
    {
        _ = [|type.GetCustomAttribute<ObsoleteAttribute>() != null|];
    }
}
""")
              .ShouldFixCodeWith("""
using System;

class TestClass
{
    void Test(Type type)
    {
        _ = Attribute.IsDefined(type, typeof(ObsoleteAttribute));
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task GetCustomAttribute_NotEqualNull_Assembly()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
using System.Reflection;

class TestClass
{
    void Test(Assembly assembly)
    {
        _ = [|assembly.GetCustomAttribute<ObsoleteAttribute>() != null|];
    }
}
""")
              .ShouldFixCodeWith("""
using System;
using System.Reflection;

class TestClass
{
    void Test(Assembly assembly)
    {
        _ = Attribute.IsDefined(assembly, typeof(ObsoleteAttribute));
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task GetCustomAttribute_NotEqualNull_Module()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
using System.Reflection;

class TestClass
{
    void Test(Module module)
    {
        _ = [|module.GetCustomAttribute<ObsoleteAttribute>() != null|];
    }
}
""")
              .ShouldFixCodeWith("""
using System;
using System.Reflection;

class TestClass
{
    void Test(Module module)
    {
        _ = Attribute.IsDefined(module, typeof(ObsoleteAttribute));
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task GetCustomAttribute_WithInherit_NotEqualNull_MemberInfo()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = [|member.GetCustomAttribute<ObsoleteAttribute>(inherit: true) != null|];
    }
}
""")
              .ShouldFixCodeWith("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = Attribute.IsDefined(member, typeof(ObsoleteAttribute), inherit: true);
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task GetCustomAttributes_WithInherit_Any_MemberInfo()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
using System.Linq;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = [|member.GetCustomAttributes<ObsoleteAttribute>(inherit: true).Any()|];
    }
}
""")
              .ShouldFixCodeWith("""
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
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task GetCustomAttribute_UsedDirectly_ShouldNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
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
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task GetCustomAttributes_WithPredicate_ShouldNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
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
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task GetCustomAttributes_Any_WithTruePredicate_ShouldNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
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
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task GetCustomAttributes_Count_ShouldReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
using System.Linq;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = [|member.GetCustomAttributes<ObsoleteAttribute>().Count() > 0|];
    }
}
""")
              .ShouldFixCodeWith("""
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
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task GetCustomAttributes_Count_WithPredicate_ShouldNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
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
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task GetCustomAttribute_NullComparison_ReversedOrder()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = [|null != member.GetCustomAttribute<ObsoleteAttribute>()|];
    }
}
""")
              .ShouldFixCodeWith("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = Attribute.IsDefined(member, typeof(ObsoleteAttribute));
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task GetCustomAttributes_Length_GreaterThanZero()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = [|member.GetCustomAttributes(typeof(ObsoleteAttribute), false).Length > 0|];
    }
}
""")
              .ShouldFixCodeWith("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = Attribute.IsDefined(member, typeof(ObsoleteAttribute), false);
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task GetCustomAttributes_Length_NotEqualZero()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = [|member.GetCustomAttributes(typeof(ObsoleteAttribute), false).Length != 0|];
    }
}
""")
              .ShouldFixCodeWith("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = Attribute.IsDefined(member, typeof(ObsoleteAttribute), false);
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task GetCustomAttributes_Length_EqualZero()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = [|member.GetCustomAttributes(typeof(ObsoleteAttribute), false).Length == 0|];
    }
}
""")
              .ShouldFixCodeWith("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = !Attribute.IsDefined(member, typeof(ObsoleteAttribute), false);
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task GetCustomAttributes_Length_GreaterThanOrEqualOne()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = [|member.GetCustomAttributes(typeof(ObsoleteAttribute), false).Length >= 1|];
    }
}
""")
              .ShouldFixCodeWith("""
using System;
using System.Reflection;

class TestClass
{
    void Test(MemberInfo member)
    {
        _ = Attribute.IsDefined(member, typeof(ObsoleteAttribute), false);
    }
}
""")
              .ValidateAsync();
    }
}
