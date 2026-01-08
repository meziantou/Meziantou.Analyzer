using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class AvoidUninstantiatedInternalClassesAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<AvoidUninstantiatedInternalClassesAnalyzer>();
    }

    [Fact]
    public async Task PublicClass_NoDiagnostic()
    {
        const string SourceCode = """
            public class PublicClass
            {
                public string Name { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AbstractClass_NoDiagnostic()
    {
        const string SourceCode = """
            internal abstract class AbstractClass
            {
                public abstract void Method();
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StaticClass_NoDiagnostic()
    {
        const string SourceCode = """
            internal static class StaticClass
            {
                public static void Method() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Interface_NoDiagnostic()
    {
        const string SourceCode = """
            internal interface ITest
            {
                void Method();
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Enum_NoDiagnostic()
    {
        const string SourceCode = """
            internal enum TestEnum
            {
                Value1,
                Value2
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnusedInternalClass_Diagnostic()
    {
        const string SourceCode = """
            internal class [|UnusedClass|]
            {
                public string Name { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedInObjectCreation_NoDiagnostic()
    {
        const string SourceCode = """
            internal class UsedClass
            {
                public string Name { get; set; }
            }

            public class Consumer
            {
                public void Method()
                {
                    var obj = new UsedClass();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedAsGenericTypeArgument_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            using System.Text.Json;

            internal class InternalData
            {
                public string Name { get; set; }
                public string Value { get; set; }
            }

            public class Consumer
            {
                public void Method()
                {
                    string json = "{}";
                    var data = JsonSerializer.Deserialize<InternalData>(json);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithTargetFramework(TargetFramework.Net9_0)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedInTypeOfInAttribute_NoDiagnostic()
    {
        const string SourceCode = """
            using System;

            [AttributeUsage(AttributeTargets.Class)]
            public sealed class ConfigAttribute : Attribute
            {
                public Type Type { get; }

                public ConfigAttribute(Type type)
                {
                    Type = type;
                }
            }

            internal sealed class MultiFrameworkConfig
            {
            }

            [Config(typeof(MultiFrameworkConfig))]
            internal static class Program
            {
                private static void Main(string[] args)
                {
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedInArrayCreation_NoDiagnostic()
    {
        const string SourceCode = """
            using System;

            internal sealed class Config
            {
            }

            internal static class Program
            {
                private static void Main(string[] args)
                {
                    var list = Array.Empty<Config>();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedInGenericList_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Collections.Generic;

            internal class Item
            {
                public string Name { get; set; }
            }

            public class Container
            {
                internal List<Item> Items { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedInNestedGenericType_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Collections.Generic;

            internal class InnerData
            {
                public int Value { get; set; }
            }

            public class Outer
            {
                internal Dictionary<string, List<InnerData>> Data { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedInMethodParameter_NoDiagnostic()
    {
        const string SourceCode = """
            internal class Config
            {
                public string Value { get; set; }
            }

            public class Service
            {
                internal void ProcessConfig(Config config)
                {
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedAsMethodReturnType_NoDiagnostic()
    {
        const string SourceCode = """
            internal class Result
            {
                public bool Success { get; set; }
            }

            public class Service
            {
                internal Result GetResult()
                {
                    return new Result();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MultipleInternalClasses_SomeUsedSomeNot()
    {
        const string SourceCode = """
            using System;

            internal class [|UnusedClass|]
            {
                public string Name { get; set; }
            }

            internal class UsedClass
            {
                public string Value { get; set; }
            }

            public class Consumer
            {
                public void Method()
                {
                    var obj = new UsedClass();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedInMethodTypeParameter_NoDiagnostic()
    {
        const string SourceCode = """
            using System;

            internal class Settings
            {
                public string Value { get; set; }
            }

            public class Service
            {
                public T GetConfiguration<T>() where T : new()
                {
                    return new T();
                }

                public void Use()
                {
                    var settings = GetConfiguration<Settings>();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedInActivatorCreateInstance_NoDiagnostic()
    {
        const string SourceCode = """
            using System;

            internal class DynamicClass
            {
                public string Name { get; set; }
            }

            public class Factory
            {
                public object Create()
                {
                    return Activator.CreateInstance(typeof(DynamicClass));
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedInLocalFunction_NoDiagnostic()
    {
        const string SourceCode = """
            internal class Data
            {
                public int Value { get; set; }
            }

            public class Processor
            {
                public void Process()
                {
                    void LocalFunc()
                    {
                        var data = new Data { Value = 42 };
                    }
                    LocalFunc();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassOnlyUsedAsTypeOf_NoDiagnostic()
    {
        const string SourceCode = """
            using System;

            internal class MetadataClass
            {
            }

            public class Registry
            {
                public void Register()
                {
                    var type = typeof(MetadataClass);
                    Console.WriteLine(type.Name);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalSealedClass_UnusedClass_Diagnostic()
    {
        const string SourceCode = """
            internal sealed class [|SealedUnusedClass|]
            {
                public void Method() { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedAsGenericTypeArgumentForStaticMemberAccess_NoDiagnostic()
    {
        const string SourceCode = """
            internal class Sample<T>
            {
                public static int Empty { get; } = 0;
            }

            internal class InternalClass
            {
            }

            public class Consumer
            {
                public void A()
                {
                    _ = Sample<InternalClass>.Empty;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedByXmlSerializer_NoDiagnostic()
    {
        const string SourceCode = """
            using System.IO;
            using System.Xml.Serialization;

            internal class InternalData
            {
                public string Name { get; set; }
                public int Value { get; set; }
            }

            public class Consumer
            {
                public void Method()
                {
                    var serializer = new XmlSerializer(typeof(InternalData));
                    using var writer = new StringWriter();
                    serializer.Serialize(writer, new InternalData());
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedByNewtonsoftJsonSerializer_NoDiagnostic()
    {
        const string SourceCode = """
            using Newtonsoft.Json;

            internal class InternalData
            {
                public string Name { get; set; }
                public int Value { get; set; }
            }

            public class Consumer
            {
                public void Method()
                {
                    string json = "{}";
                    var data = JsonConvert.DeserializeObject<InternalData>(json);
                }
            }
            """;
        await CreateProjectBuilder()
              .AddNuGetReference("Newtonsoft.Json", "13.0.3", "lib/netstandard2.0/")
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedByYamlDotNetSerializer_NoDiagnostic()
    {
        const string SourceCode = """
            using YamlDotNet.Serialization;

            internal class InternalData
            {
                public string Name { get; set; }
                public int Value { get; set; }
            }

            public class Consumer
            {
                public void Method()
                {
                    var deserializer = new DeserializerBuilder().Build();
                    var data = deserializer.Deserialize<InternalData>("name: test");
                }
            }
            """;
        await CreateProjectBuilder()
              .AddNuGetReference("YamlDotNet", "16.3.0", "lib/netstandard2.0/")
              .WithSourceCode(SourceCode)
              .WithTargetFramework(TargetFramework.NetStandard2_0)
              .ValidateAsync();
    }
}
