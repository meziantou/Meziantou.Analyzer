using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class AvoidUnusedInternalTypesAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<AvoidUnusedInternalTypesAnalyzer>();
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
            internal abstract class [|AbstractClass|]
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
            internal interface [|ITest|]
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
    public async Task UnusedPrivateNestedClass_Diagnostic()
    {
        const string SourceCode = """
            public class OuterClass
            {
                private class [|UnusedNestedClass|]
                {
                    public string Name { get; set; }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnusedInternalNestedClass_Diagnostic()
    {
        const string SourceCode = """
            public class OuterClass
            {
                internal class [|UnusedNestedClass|]
                {
                    public string Name { get; set; }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnusedProtectedNestedClass_NoDiagnostic()
    {
        const string SourceCode = """
            public class OuterClass
            {
                protected class UnusedNestedClass
                {
                    public string Name { get; set; }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnusedProtectedInternalNestedClass_NoDiagnostic()
    {
        const string SourceCode = """
            public class OuterClass
            {
                protected internal class UnusedNestedClass
                {
                    public string Name { get; set; }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnusedPrivateProtectedNestedClass_Diagnostic()
    {
        const string SourceCode = """
            public class OuterClass
            {
                private protected class [|UnusedNestedClass|]
                {
                    public string Name { get; set; }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsedPrivateNestedClass_NoDiagnostic()
    {
        const string SourceCode = """
            public class OuterClass
            {
                private class UsedNestedClass
                {
                    public string Name { get; set; }
                }

                public void Method()
                {
                    var obj = new UsedNestedClass();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PublicNestedClass_NoDiagnostic()
    {
        const string SourceCode = """
            public class OuterClass
            {
                public class NestedClass
                {
                    public string Name { get; set; }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PrivateNestedClassInInternalClass_Diagnostic()
    {
        const string SourceCode = """
            internal class [|OuterClass|]
            {
                private class [|UnusedNestedClass|]
                {
                    public string Name { get; set; }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnusedInternalStruct_Diagnostic()
    {
        const string SourceCode = """
            internal struct [|UnusedStruct|]
            {
                public string Name { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnusedPrivateNestedStruct_Diagnostic()
    {
        const string SourceCode = """
            public class OuterClass
            {
                private struct [|UnusedNestedStruct|]
                {
                    public int Value;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsedPrivateNestedStruct_NoDiagnostic()
    {
        const string SourceCode = """
            public class OuterClass
            {
                private struct UsedNestedStruct
                {
                    public int Value;
                }

                public void Method()
                {
                    var obj = new UsedNestedStruct();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnusedInternalRecord_Diagnostic()
    {
        const string SourceCode = """
            internal record [|UnusedRecord|]
            {
                public string Name { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnusedPrivateNestedRecord_Diagnostic()
    {
        const string SourceCode = """
            public class OuterClass
            {
                private record [|UnusedNestedRecord|](string Name);
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsedPrivateNestedRecord_NoDiagnostic()
    {
        const string SourceCode = """
            public class OuterClass
            {
                private record UsedNestedRecord(string Name);

                public void Method()
                {
                    var obj = new UsedNestedRecord("Test");
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

#if CSHARP10_OR_GREATER
    [Fact]
    public async Task UnusedInternalRecordStruct_Diagnostic()
    {
        const string SourceCode = """
            internal record struct [|UnusedRecordStruct|]
            {
                public string Name { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnusedPrivateNestedRecordStruct_Diagnostic()
    {
        const string SourceCode = """
            public class OuterClass
            {
                private record struct [|UnusedNestedRecordStruct|](int Id);
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsedPrivateNestedRecordStruct_NoDiagnostic()
    {
        const string SourceCode = """
            public class OuterClass
            {
                private record struct UsedNestedRecordStruct(int Id);

                public void Method()
                {
                    var obj = new UsedNestedRecordStruct(42);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
#endif

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
    public async Task InternalStructUsedInObjectCreation_NoDiagnostic()
    {
        const string SourceCode = """
            internal struct UsedStruct
            {
                public string Name { get; set; }
            }

            public class Consumer
            {
                public void Method()
                {
                    var obj = new UsedStruct();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalRecordUsedInObjectCreation_NoDiagnostic()
    {
        const string SourceCode = """
            internal record UsedRecord(string Name);

            public class Consumer
            {
                public void Method()
                {
                    var obj = new UsedRecord("Test");
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

#if CSHARP10_OR_GREATER
    [Fact]
    public async Task InternalRecordStructUsedInObjectCreation_NoDiagnostic()
    {
        const string SourceCode = """
            internal record struct UsedRecordStruct(string Name);

            public class Consumer
            {
                public void Method()
                {
                    var obj = new UsedRecordStruct("Test");
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task InternalClassUsedAsFieldType_NoDiagnostic()
    {
        const string SourceCode = """
            internal class Data
            {
                public int Value;
            }

            public class Container
            {
                internal Data _data;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalStructUsedAsFieldType_NoDiagnostic()
    {
        const string SourceCode = """
            internal struct Data
            {
                public int Value;
            }

            public class Container
            {
                internal Data _data;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalRecordUsedAsPropertyType_NoDiagnostic()
    {
        const string SourceCode = """
            internal record Settings(string Key, string Value);

            public class Configuration
            {
                internal Settings AppSettings { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

#if CSHARP10_OR_GREATER
    [Fact]
    public async Task InternalRecordStructUsedAsParameterType_NoDiagnostic()
    {
        const string SourceCode = """
            internal record struct Point(int X, int Y);

            public class Graphics
            {
                internal void DrawAt(Point location)
                {
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task InternalStructUsedAsGenericTypeArgument_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Collections.Generic;

            internal struct ItemData
            {
                public int Id { get; set; }
            }

            public class Service
            {
                internal List<ItemData> GetData()
                {
                    return new List<ItemData>();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalRecordUsedInTypeOf_NoDiagnostic()
    {
        const string SourceCode = """
            using System;

            internal record Config(string Key);

            public class Registry
            {
                public void Register()
                {
                    var type = typeof(Config);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

#if CSHARP10_OR_GREATER
    [Fact]
    public async Task InternalRecordStructUsedInArrayCreation_NoDiagnostic()
    {
        const string SourceCode = """
            using System;

            internal record struct Vector(double X, double Y);

            public class Math
            {
                public void Process()
                {
                    var vectors = Array.Empty<Vector>();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
#endif

#if CSHARP10_OR_GREATER
    [Fact]
    public async Task MultipleInternalTypes_SomeUsedSomeNot()
    {
        const string SourceCode = """
            internal class [|UnusedClass|]
            {
                public string Name { get; set; }
            }

            internal struct [|UnusedStruct|]
            {
                public int Value;
            }

            internal record [|UnusedRecord|](string Data);

            internal record struct [|UnusedRecordStruct|](int Id);

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
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
#endif

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

    [Fact]
    public async Task InternalClassUsedInMethodGenericConstraint_NoDiagnostic()
    {
        const string SourceCode = """
            internal class BaseConfig
            {
                public string Value { get; set; }
            }

            public class Service
            {
                internal T Create<T>() where T : BaseConfig, new()
                {
                    return new T();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedInTypeGenericConstraint_NoDiagnostic()
    {
        const string SourceCode = """
            internal class BaseEntity
            {
                public int Id { get; set; }
            }

            internal class [|Repository|]<T> where T : BaseEntity
            {
                public T Get(int id) => null;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedInMultipleGenericConstraints_NoDiagnostic()
    {
        const string SourceCode = """
            internal interface IValidator
            {
                bool Validate();
            }

            internal class BaseModel
            {
                public string Name { get; set; }
            }

            internal class [|Processor|]<T> where T : BaseModel, IValidator, new()
            {
                public void Process(T item)
                {
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

#if CSHARP14_OR_GREATER
    [Fact]
    public async Task InternalClassUsedInImplicitExtensionType_NoDiagnostic()
    {
        const string SourceCode = """
            internal class DataStore
            {
                public string Value { get; set; }
            }

            internal static class DataStoreExtensions
            {
                extension (DataStore datastore)
                {
                    public void Save()
                    {
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedInExplicitExtensionType_NoDiagnostic()
    {
        const string SourceCode = """
            internal class Settings
            {
                public string Key { get; set; }
            }

            internal static class DataStoreExtensions
            {
                extension (Settings settings)
                {
                    public string GetValue() => settings.Key;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedInGenericExtensionType_NoDiagnostic()
    {
        const string SourceCode = """
            internal class Entity
            {
                public int Id { get; set; }
            }

            internal static class EntityExtension
            {
                extension<T>(T entity) where T : Entity
                {
                    public void Delete()
                    {
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedInExtensionTypeWithMultipleConstraints_NoDiagnostic()
    {
        const string SourceCode = """
            internal interface IIdentifiable
            {
                int Id { get; }
            }

            internal class BaseEntity
            {
                public string Name { get; set; }
            }

            internal static class RepositoryExtension
            {
                extension<T>(T entity) where T : BaseEntity, IIdentifiable, new()
                {
                    public void Save()
                    {
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedAsExtensionTypeParameter_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Collections.Generic;

            internal class Item
            {
                public string Name { get; set; }
            }

            public static class ListExtensions
            {
                extension (List<Item> items)
                {
                    internal Item GetFirst() => items[0];
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task DeeplyNestedPrivateClass_Diagnostic()
    {
        const string SourceCode = """
            public class Level1
            {
                public class Level2
                {
                    private class [|Level3|]
                    {
                        public string Name { get; set; }
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PrivateNestedClassUsedInSameType_NoDiagnostic()
    {
        const string SourceCode = """
            public class OuterClass
            {
                private class NestedClass
                {
                    public string Name { get; set; }
                }

                private NestedClass _field;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PrivateNestedClassUsedAsMethodParameter_NoDiagnostic()
    {
        const string SourceCode = """
            public class OuterClass
            {
                private class NestedClass
                {
                    public string Name { get; set; }
                }

                private void Method(NestedClass parameter)
                {
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SelfReferencingInterface_Diagnostic()
    {
        const string SourceCode = """
            internal interface [|INumber|]<TSelf> where TSelf : INumber<TSelf>
            {
                TSelf Add(TSelf other);
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SelfReferencingInterfaceUsedByType_NoDiagnostic()
    {
        const string SourceCode = """
            internal interface INumber<TSelf> where TSelf : INumber<TSelf>
            {
                TSelf Add(TSelf other);
            }

            internal class MyNumber : INumber<MyNumber>
            {
                public MyNumber Add(MyNumber other) => this;
            }

            public class Consumer
            {
                public void Method()
                {
                    var num = new MyNumber();
                    num.Add(num);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SelfReferencingInterfaceWithMultipleConstraints_Diagnostic()
    {
        const string SourceCode = """
            using System;

            internal interface [|IComparable|]<TSelf> where TSelf : IComparable<TSelf>, IEquatable<TSelf>
            {
                int CompareTo(TSelf other);
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterfaceWithCoClassAttribute_NoUsage_Diagnostic()
    {
        const string SourceCode = """
            using System.Runtime.InteropServices;

            [ComImport]
            [Guid("00000000-0000-0000-0000-000000000001")]
            [CoClass(typeof(FileSaveDialogRCW))]
            internal interface [|NativeFileSaveDialog|]
            {
            }

            [ComImport]
            [ClassInterface(ClassInterfaceType.None)]
            [Guid("00000000-0000-0000-0000-000000000002")]
            internal sealed class FileSaveDialogRCW
            {
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalStructUsedAsPointerInMethodParameter_NoDiagnostic()
    {
        const string SourceCode = """
            using System;

            internal struct SECURITY_ATTRIBUTES
            {
                internal uint nLength;
                internal IntPtr lpSecurityDescriptor;
                internal bool bInheritHandle;
            }

            public class FileOperations
            {
                private static unsafe void CreateFilePrivate(
                    string lpFileName,
                    int dwDesiredAccess,
                    int dwShareMode,
                    SECURITY_ATTRIBUTES* lpSecurityAttributes)
                {
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedInVariableDeclaration_NoDiagnostic()
    {
        const string SourceCode = """
            internal class Data
            {
                public string Value { get; set; }
            }

            public class Consumer
            {
                public void Method()
                {
                    Data sample;
                    sample = null;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalStructUsedInVariableDeclaration_NoDiagnostic()
    {
        const string SourceCode = """
            internal struct Point
            {
                public int X;
                public int Y;
            }

            public class Graphics
            {
                public void Draw()
                {
                    Point location;
                    location = default;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalRecordUsedInVariableDeclaration_NoDiagnostic()
    {
        const string SourceCode = """
            internal record Config(string Key);

            public class Service
            {
                public void Process()
                {
                    Config config;
                    config = null;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

#if CSHARP10_OR_GREATER
    [Fact]
    public async Task InternalRecordStructUsedInVariableDeclaration_NoDiagnostic()
    {
        const string SourceCode = """
            internal record struct Vector(double X, double Y);

            public class Math
            {
                public void Calculate()
                {
                    Vector v;
                    v = default;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task InternalClassUsedInExplicitCast_NoDiagnostic()
    {
        const string SourceCode = """
            internal class BaseData
            {
                public string Value { get; set; }
            }

            internal class DerivedData : BaseData
            {
            }

            public class Consumer
            {
                internal void Method(BaseData data)
                {
                    var derived = (DerivedData)data;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalStructUsedInExplicitCast_NoDiagnostic()
    {
        const string SourceCode = """
            internal struct CustomValue
            {
                public int Value;

                public static explicit operator CustomValue(int value)
                {
                    return new CustomValue { Value = value };
                }
            }

            public class Service
            {
                public void Method()
                {
                    var custom = (CustomValue)42;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassUsedInAsOperator_NoDiagnostic()
    {
        const string SourceCode = """
            internal class SpecialData
            {
                public string Value { get; set; }
            }

            public class Consumer
            {
                public void Method(object obj)
                {
                    var special = obj as SpecialData;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalStructUsedInImplicitCast_NoDiagnostic()
    {
        const string SourceCode = """
            internal struct Wrapper
            {
                public int Value;

                public static implicit operator Wrapper(int value)
                {
                    return new Wrapper { Value = value };
                }
            }

            public class Service
            {
                public void Method()
                {
                    Wrapper wrapper = 10;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalRecordUsedInPatternMatching_NoDiagnostic()
    {
        const string SourceCode = """
            internal record Message(string Text);

            public class Handler
            {
                public void Handle(object obj)
                {
                    if (obj is Message message)
                    {
                        System.Console.WriteLine(message.Text);
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

#if CSHARP10_OR_GREATER
    [Fact]
    public async Task InternalRecordStructUsedInPatternMatching_NoDiagnostic()
    {
        const string SourceCode = """
            internal record struct Coordinate(int X, int Y);

            public class Mapper
            {
                public void Map(object obj)
                {
                    if (obj is Coordinate { X: > 0 } coord)
                    {
                        System.Console.WriteLine(coord.Y);
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task InternalClassUsedInCastExpression_NoDiagnostic()
    {
        const string SourceCode = """
            internal interface IData
            {
            }

            internal class ConcreteData : IData
            {
                public string Value { get; set; }
            }

            public class Service
            {
                internal void Process(IData data)
                {
                    var concrete = (ConcreteData)data;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MultipleInternalTypesUsedInCastsAndDeclarations_NoDiagnostic()
    {
        const string SourceCode = """
            internal class BaseType
            {
            }

            internal class DerivedType : BaseType
            {
            }

            internal struct ValueType
            {
                public int Value;
            }

            public class Consumer
            {
                public void Method(object obj)
                {
                    BaseType baseVar;
                    var derived = obj as DerivedType;
                    ValueType valueVar = default;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalClassWithDynamicallyAccessedMembersAttribute_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            internal sealed class FakeTaskHandler
            {
                public string Name { get; set; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PrivateClassWithDynamicallyAccessedMembersAttribute_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;

            public class OuterClass
            {
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                private sealed class InternalHandler
                {
                    public void Method() { }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InternalStructWithDynamicallyAccessedMembersAttribute_NoDiagnostic()
    {
        const string SourceCode = """
            using System.Diagnostics.CodeAnalysis;

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
            internal struct DataStruct
            {
                public int Value;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
