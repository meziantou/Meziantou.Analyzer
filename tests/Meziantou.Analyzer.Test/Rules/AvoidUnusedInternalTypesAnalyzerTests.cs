using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.AvoidUnusedInternalTypesAnalyzer,
    Meziantou.Analyzer.Rules.AvoidUnusedInternalTypesFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class AvoidUnusedInternalTypesAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();

        // The analyzer reports the unused types at the end of the compilation, so they are not local diagnostics
        test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipLocalDiagnosticCheck;
        return test;
    }

    [Fact]
    public Task PublicClass_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public class PublicClass
            {
                public string Name { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AbstractClass_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal abstract class {|MA0182:AbstractClass|}
            {
                public abstract void Method();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StaticClass_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal static class {|MA0182:StaticClass|}
            {
                public static void Method() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal interface {|MA0182:ITest|}
            {
                void Method();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Enum_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal enum {|MA0182:TestEnum|}
            {
                Value1,
                Value2
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UnusedInternalClass_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal class {|MA0182:UnusedClass|}
            {
                public string Name { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UnusedPrivateNestedClass_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public class OuterClass
            {
                private class {|MA0182:UnusedNestedClass|}
                {
                    public string Name { get; set; }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UnusedInternalNestedClass_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public class OuterClass
            {
                internal class {|MA0182:UnusedNestedClass|}
                {
                    public string Name { get; set; }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UnusedProtectedNestedClass_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public class OuterClass
            {
                protected class UnusedNestedClass
                {
                    public string Name { get; set; }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UnusedProtectedInternalNestedClass_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public class OuterClass
            {
                protected internal class UnusedNestedClass
                {
                    public string Name { get; set; }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UnusedPrivateProtectedNestedClass_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public class OuterClass
            {
                private protected class {|MA0182:UnusedNestedClass|}
                {
                    public string Name { get; set; }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsedPrivateNestedClass_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task PublicNestedClass_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public class OuterClass
            {
                public class NestedClass
                {
                    public string Name { get; set; }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PrivateNestedClassInInternalClass_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal class {|MA0182:OuterClass|}
            {
                private class {|MA0182:UnusedNestedClass|}
                {
                    public string Name { get; set; }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UnusedInternalStruct_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal struct {|MA0182:UnusedStruct|}
            {
                public string Name { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UnusedPrivateNestedStruct_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public class OuterClass
            {
                private struct {|MA0182:UnusedNestedStruct|}
                {
                    public int Value;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsedPrivateNestedStruct_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task UnusedInternalRecord_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal record {|MA0182:UnusedRecord|}
            {
                public string Name { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UnusedPrivateNestedRecord_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public class OuterClass
            {
                private record {|MA0182:UnusedNestedRecord|}(string Name);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsedPrivateNestedRecord_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public class OuterClass
            {
                private record UsedNestedRecord(string Name);

                public void Method()
                {
                    var obj = new UsedNestedRecord("Test");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UnusedInternalRecordStruct_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal record struct {|MA0182:UnusedRecordStruct|}
            {
                public string Name { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UnusedPrivateNestedRecordStruct_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public class OuterClass
            {
                private record struct {|MA0182:UnusedNestedRecordStruct|}(int Id);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsedPrivateNestedRecordStruct_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public class OuterClass
            {
                private record struct UsedNestedRecordStruct(int Id);

                public void Method()
                {
                    var obj = new UsedNestedRecordStruct(42);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedInObjectCreation_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalStructUsedInObjectCreation_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalRecordUsedInObjectCreation_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal record UsedRecord(string Name);

            public class Consumer
            {
                public void Method()
                {
                    var obj = new UsedRecord("Test");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalRecordStructUsedInObjectCreation_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal record struct UsedRecordStruct(string Name);

            public class Consumer
            {
                public void Method()
                {
                    var obj = new UsedRecordStruct("Test");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedAsFieldType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal class Data
            {
                public int Value;
            }

            public class Container
            {
                internal Data _data;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalStructUsedAsFieldType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal struct Data
            {
                public int Value;
            }

            public class Container
            {
                internal Data _data;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalRecordUsedAsPropertyType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal record Settings(string Key, string Value);

            public class Configuration
            {
                internal Settings AppSettings { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalRecordStructUsedAsParameterType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal record struct Point(int X, int Y);

            public class Graphics
            {
                internal void DrawAt(Point location)
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalStructUsedAsGenericTypeArgument_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalRecordUsedInTypeOf_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalRecordStructUsedInArrayCreation_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalCollectionUsedInCollectionExpression_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            internal class SampleCollectionBuilder
            {
                public static SampleCollection<T> Create<T>(ReadOnlySpan<T> items)
                {
                    throw null;
                }
            }

            [CollectionBuilder(typeof(SampleCollectionBuilder), "Create")]
            internal class SampleCollection<T> : IEnumerable<T>
            {
                public IEnumerator<T> GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => throw null;
            }

            public class Usage
            {
                public void Method()
                {
                    SampleCollection<int> a = [1, 2, 3];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MultipleInternalTypes_SomeUsedSomeNot()
    {
        var test = CreateTest();
        test.TestCode = """
            internal class {|MA0182:UnusedClass|}
            {
                public string Name { get; set; }
            }

            internal struct {|MA0182:UnusedStruct|}
            {
                public int Value;
            }

            internal record {|MA0182:UnusedRecord|}(string Data);

            internal record struct {|MA0182:UnusedRecordStruct|}(int Id);

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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedInTypeOfInAttribute_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task EntryPointClass_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task EntryPointInClassLibrary_Reported()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            internal sealed class Config
            {
            }

            internal static class {|MA0182:Program|}
            {
                private static void Main(string[] args)
                {
                    var list = Array.Empty<Config>();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedInGenericList_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedInNestedGenericType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedInMethodParameter_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedAsMethodReturnType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task MultipleInternalClasses_SomeUsedSomeNot()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            internal class {|MA0182:UnusedClass|}
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedInMethodTypeParameter_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedInActivatorCreateInstance_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedInLocalFunction_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassOnlyUsedAsTypeOf_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalSealedClass_UnusedClass_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal sealed class {|MA0182:SealedUnusedClass|}
            {
                public void Method() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedAsGenericTypeArgumentForStaticMemberAccess_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedByXmlSerializer_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedByNewtonsoftJsonSerializer_NoDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([new PackageIdentity("Newtonsoft.Json", "13.0.3")]);
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedByYamlDotNetSerializer_NoDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([new PackageIdentity("YamlDotNet", "16.3.0")]);
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedInMethodGenericConstraint_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedInTypeGenericConstraint_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal class BaseEntity
            {
                public int Id { get; set; }
            }

            internal class {|MA0182:Repository|}<T> where T : BaseEntity
            {
                public T Get(int id) => null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedInMultipleGenericConstraints_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal interface IValidator
            {
                bool Validate();
            }

            internal class BaseModel
            {
                public string Name { get; set; }
            }

            internal class {|MA0182:Processor|}<T> where T : BaseModel, IValidator, new()
            {
                public void Process(T item)
                {
                }
            }
            """;

        return test.RunAsync();
    }

#if CSHARP14_OR_GREATER

    [Fact]
    public Task InternalClassUsedInImplicitExtensionType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal class DataStore
            {
                public string Value { get; set; }
            }

            internal static class {|MA0182:DataStoreExtensions|}
            {
                extension (DataStore datastore)
                {
                    public void Save()
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedInExplicitExtensionType_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal class Settings
            {
                public string Key { get; set; }
            }

            internal static class {|MA0182:DataStoreExtensions|}
            {
                extension (Settings settings)
                {
                    public string GetValue() => settings.Key;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedInExplicitExtensionType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

            public class Sample
            {
                public void Test()
                {
                    var settings = new Settings { Key = "Test" };
                    var value = settings.GetValue();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedInGenericExtensionType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal class Entity
            {
                public int Id { get; set; }
            }

            internal static class {|MA0182:EntityExtension|}
            {
                extension<T>(T entity) where T : Entity
                {
                    public void Delete()
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedInExtensionTypeWithMultipleConstraints_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal interface IIdentifiable
            {
                int Id { get; }
            }

            internal class BaseEntity
            {
                public string Name { get; set; }
            }

            internal static class {|MA0182:RepositoryExtension|}
            {
                extension<T>(T entity) where T : BaseEntity, IIdentifiable, new()
                {
                    public void Save()
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedAsExtensionTypeParameter_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

#endif

    [Fact]
    public Task DeeplyNestedPrivateClass_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Level1
            {
                public class Level2
                {
                    private class {|MA0182:Level3|}
                    {
                        public string Name { get; set; }
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PrivateNestedClassUsedInSameType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public class OuterClass
            {
                private class NestedClass
                {
                    public string Name { get; set; }
                }

                private NestedClass _field;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PrivateNestedClassUsedAsMethodParameter_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task SelfReferencingInterface_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal interface {|MA0182:INumber|}<TSelf> where TSelf : INumber<TSelf>
            {
                TSelf Add(TSelf other);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SelfReferencingInterfaceUsedByType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task SelfReferencingInterfaceWithMultipleConstraints_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            internal interface {|MA0182:IComparable|}<TSelf> where TSelf : IComparable<TSelf>, IEquatable<TSelf>
            {
                int CompareTo(TSelf other);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterfaceWithCoClassAttribute_NoUsage_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.InteropServices;

            [ComImport]
            [Guid("00000000-0000-0000-0000-000000000001")]
            [CoClass(typeof(FileSaveDialogRCW))]
            internal interface {|MA0182:NativeFileSaveDialog|}
            {
            }

            [ComImport]
            [ClassInterface(ClassInterfaceType.None)]
            [Guid("00000000-0000-0000-0000-000000000002")]
            internal sealed class FileSaveDialogRCW
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalStructUsedAsPointerInMethodParameter_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedInVariableDeclaration_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalStructUsedInVariableDeclaration_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalRecordUsedInVariableDeclaration_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalRecordStructUsedInVariableDeclaration_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedInExplicitCast_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalStructUsedInExplicitCast_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedInAsOperator_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalStructUsedInImplicitCast_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalInterfaceOnlyUsedInTypeCheck_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public sealed class Foo
            {
                public static string? Run(object x)
                {
                    if (x is IRunnable)
                    {
                        return null;
                    }

                    return "X";
                }
            }

            internal interface IRunnable
            {
                void Run();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalRecordUsedInPatternMatching_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalRecordStructUsedInPatternMatching_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassUsedInCastExpression_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task MultipleInternalTypesUsedInCastsAndDeclarations_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassWithDynamicallyAccessedMembersAttribute_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            internal sealed class FakeTaskHandler
            {
                public string Name { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PrivateClassWithDynamicallyAccessedMembersAttribute_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task InternalStructWithDynamicallyAccessedMembersAttribute_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
            internal struct DataStruct
            {
                public int Value;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassAccessingOwnField_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal sealed class {|MA0182:NotUsed|}
            {
                private readonly string _name;

                public NotUsed(string name)
                {
                    _name = name;
                }

                public string X { get; } = "X";

                public string? Whatever()
                {
                    return _name;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassAttributeTypeOfOwnClass_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [Sample(typeof(NotUsed))]
            internal sealed class {|MA0182:NotUsed|}
            {
            }

            public sealed class SampleAttribute : System.Attribute
            {
                public SampleAttribute(System.Type type) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassAttributeOwnClass_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [NotUsed]
            internal sealed class {|MA0182:NotUsedAttribute|} : System.Attribute
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassNotAccessingField_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal sealed class {|MA0182:NotUsed|}
            {
                private readonly string _name;

                public string X { get; } = "X";

                public string? Whatever()
                {
                    return null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassInsidePublicClass_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public class PublicClass
            {
                internal sealed class {|MA0182:NotUsed|}
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalDelegateUsedInNewExpression_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            internal delegate void MyDelegate(string key, string comment, TimeSpan timeout, out string lockToken);

            public class Consumer
            {
                public void Method()
                {
                    var callback = new MyDelegate((string key, string comment, TimeSpan timeout, out string lockToken) =>
                    {
                        lockToken = "42";
                    });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PrivateDelegateUsedInNewExpression_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            public class OuterClass
            {
                private delegate void TryAcquireLockDelegate(string key, string comment, TimeSpan timeout, out string lockToken);

                public void Method()
                {
                    var callback = new TryAcquireLockDelegate((string key, string comment, TimeSpan timeout, out string lockToken) =>
                    {
                        lockToken = "test";
                    });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UnusedInternalDelegate_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            internal delegate void {|MA0182:UnusedDelegate|}(string message);

            public class Consumer
            {
                public void Method()
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassWithFactoryMethod_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal sealed class BugDemo
            {
                private BugDemo()
                {
                }

                public static BugDemo Create() => new();
            }

            public class Consumer
            {
                public void Method()
                {
                    var x = BugDemo.Create();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassWithFactoryMethodNotUsed_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal sealed class {|MA0182:UnusedFactory|}
            {
                private UnusedFactory()
                {
                }

                public static UnusedFactory Create() => new();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalClassWithFactoryMethodInternalUsage_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal sealed class ConfigurableCertificateValidatingHttpClientHandler
            {
                private ConfigurableCertificateValidatingHttpClientHandler()
                {
                }

                public static ConfigurableCertificateValidatingHttpClientHandler CreateClient()
                {
                    return new ConfigurableCertificateValidatingHttpClientHandler();
                }
            }

            public class ApiClient
            {
                public void Setup()
                {
                    var handler = ConfigurableCertificateValidatingHttpClientHandler.CreateClient();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NestedClass_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            internal sealed class Interop
            {
                internal static class Kernel32
                {
                    public const int ERROR_SUCCESS = 0;
                }
            }

            public class Sample
            {
                public void Setup()
                {
                    _ = Interop.Kernel32.ERROR_SUCCESS;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClassWithModuleInitializerMethod_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;

            internal class ModuleInit
            {
                [ModuleInitializer]
                internal static void Initialize()
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClassWithModuleInitializerMethodAndOtherMembers_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;

            internal sealed class Startup
            {
                private static bool _initialized;

                [ModuleInitializer]
                internal static void Init()
                {
                    _initialized = true;
                }

                internal static bool IsInitialized => _initialized;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_AddDynamicallyAccessedMembersAttribute_Class()
    {
        var test = CreateTest();
        test.TestCode = """
            internal class {|MA0182:UnusedClass|}
            {
                public string Name { get; set; }
            }
            """;
        test.FixedCode = """
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
            internal class UnusedClass
            {
                public string Name { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_AddDynamicallyAccessedMembersAttribute_Struct()
    {
        var test = CreateTest();
        test.TestCode = """
            internal struct {|MA0182:UnusedStruct|}
            {
                public int Value { get; set; }
            }
            """;
        test.FixedCode = """
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
            internal struct UnusedStruct
            {
                public int Value { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_AddDynamicallyAccessedMembersAttribute_Record()
    {
        var test = CreateTest();
        test.TestCode = """
            internal record {|MA0182:UnusedRecord|}(string Name);
            """;
        test.FixedCode = """
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
            internal record UnusedRecord(string Name);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_RemoveType_SingleTypeInFile()
    {
        var test = CreateTest();
        test.TestCode = """
            internal class {|MA0182:UnusedClass|}
            {
                public string Name { get; set; }
            }
            """;
        test.CodeActionIndex = 1;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck;
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_RemoveType_MultipleTypesInFile()
    {
        var test = CreateTest();
        test.TestCode = """
            internal class {|MA0182:UnusedClass|}
            {
                public string Name { get; set; }
            }

            public class UsedClass
            {
                public void Method() { }
            }
            """;
        test.CodeActionIndex = 1;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck;
        test.FixedCode = """

            public class UsedClass
            {
                public void Method() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_RemoveType_NestedType()
    {
        var test = CreateTest();
        test.TestCode = """
            public class OuterClass
            {
                internal class {|MA0182:UnusedNestedClass|}
                {
                    public string Name { get; set; }
                }
            }
            """;
        test.CodeActionIndex = 1;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck;
        test.FixedCode = """
            public class OuterClass
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_RemoveType_WithUsings()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Collections.Generic;

            internal class {|MA0182:UnusedClass|}
            {
                public string Name { get; set; }
            }
            """;
        test.CodeActionIndex = 1;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck;
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_RemoveType_RecordStruct()
    {
        var test = CreateTest();
        test.TestCode = """
            internal record struct {|MA0182:UnusedRecordStruct|}(int Value);
            """;
        test.CodeActionIndex = 1;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck;
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_AddDynamicallyAccessedMembersAttribute_WithExistingAttributes()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            [Obsolete]
            internal class {|MA0182:UnusedClass|}
            {
                public string Name { get; set; }
            }
            """;
        test.FixedCode = """
            using System;

            [Obsolete]
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
            internal class UnusedClass
            {
                public string Name { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_RemoveType_WithNamespace()
    {
        var test = CreateTest();
        test.TestCode = """
            namespace MyNamespace
            {
                internal class {|MA0182:UnusedClass|}
                {
                    public string Name { get; set; }
                }
            }
            """;
        test.CodeActionIndex = 1;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck;
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_RemoveType_TwoUnusedTypesInFile()
    {
        var test = CreateTest();
        test.TestCode = """
            internal class {|MA0182:UnusedClass1|}
            {
                public string Name { get; set; }
            }

            internal class {|MA0182:UnusedClass2|}
            {
                public int Value { get; set; }
            }
            """;
        test.CodeActionIndex = 1;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.SkipFixAllCheck;
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_RemoveType_WithAssemblyAttribute()
    {
        var test = CreateTest();
        test.TestCode = """
            [assembly: System.Reflection.AssemblyVersion("1.0.0.0")]

            internal class {|MA0182:UnusedClass|}
            {
                public string Name { get; set; }
            }
            """;
        test.CodeActionIndex = 1;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck;
        test.FixedCode = """
            [assembly: System.Reflection.AssemblyVersion("1.0.0.0")]

            """;

        return test.RunAsync();
    }
}
