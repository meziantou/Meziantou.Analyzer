using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Test.Internals;

public sealed class LocalDataFlowAnalysisTests
{
    private static readonly PortableExecutableReference[] References = GetReferences();

    [Fact]
    public void GetActualType_FlowsFromLocalInitializer()
    {
        var operation = GetOperation("""
            class Test
            {
                void M()
                {
                    Base target = new Derived();
                    var result = target;
                }
            }

            class Base { }
            class Derived : Base { }
            """);

        var actualType = operation.GetActualType(CancellationToken.None);

        Assert.Equal("Derived", actualType?.Name);
    }

    [Fact]
    public void GetActualType_FlowsFromLastLocalAssignment()
    {
        var operation = GetOperation("""
            class Test
            {
                void M()
                {
                    Base target;
                    target = new Derived();
                    var result = target;
                }
            }

            class Base { }
            class Derived : Base { }
            """);

        var actualType = operation.GetActualType(CancellationToken.None);

        Assert.Equal("Derived", actualType?.Name);
    }

    [Fact]
    public void GetActualType_UsesDeclaredTypeWhenLocalIsReassignedBeforeUsage()
    {
        var operation = GetOperation("""
            class Test
            {
                void M(Base value)
                {
                    Base target = new Derived();
                    target = value;
                    var result = target;
                }
            }

            class Base { }
            class Derived : Base { }
            """);

        var actualType = operation.GetActualType(CancellationToken.None);

        Assert.Equal("Base", actualType?.Name);
    }

    [Fact]
    public void GetActualType_FlowsFromPrivateReadonlyFieldInitializer()
    {
        var operation = GetOperation("""
            class Test
            {
                private readonly Base target = new Derived();

                void M()
                {
                    var result = target;
                }
            }

            class Base { }
            class Derived : Base { }
            """);

        var actualType = operation.GetActualType(CancellationToken.None);

        Assert.Equal("Derived", actualType?.Name);
    }

    [Fact]
    public void GetActualType_UsesDeclaredTypeWhenReadonlyFieldIsAssignedOutsideInitializer()
    {
        var operation = GetOperation("""
            class Test
            {
                private readonly Base target = new Derived();

                Test(Base value)
                {
                    target = value;
                }

                void M()
                {
                    var result = target;
                }
            }

            class Base { }
            class Derived : Base { }
            """);

        var actualType = operation.GetActualType(CancellationToken.None);

        Assert.Equal("Base", actualType?.Name);
    }

    [Fact]
    public void TryGetConstantValue_FlowsFromLocalInitializer()
    {
        var operation = GetOperation("""
            class Test
            {
                void M()
                {
                    var target = 42;
                    var result = target;
                }
            }
            """);

        var result = operation.TryGetConstantValue(out var value, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGetConstantValue_FlowsFromPrivateGetOnlyPropertyInitializer()
    {
        var operation = GetOperation("""
            class Test
            {
                private string Target { get; } = "value";

                void M()
                {
                    var result = Target;
                }
            }
            """, "Target");

        var result = operation.TryGetConstantValue(out var value, CancellationToken.None);

        Assert.True(result);
        Assert.Equal("value", value);
    }

    [Fact]
    public void TryGetConstantValue_ReturnsFalseWhenLocalIsWrittenBetweenInitializerAndUsage()
    {
        var operation = GetOperation("""
            class Test
            {
                void M()
                {
                    var target = 42;
                    target++;
                    var result = target;
                }
            }
            """);

        var result = operation.TryGetConstantValue(out var value, CancellationToken.None);

        Assert.False(result);
        Assert.Null(value);
    }

    private static IOperation GetOperation(string source, string identifierName = "target")
    {
        var compilation = CreateCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.Single();
        var diagnostics = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(diagnostics);

        var root = syntaxTree.GetRoot(CancellationToken.None);
        var identifier = root.DescendantNodes().OfType<IdentifierNameSyntax>().Last(node => node.Identifier.ValueText == identifierName);
        var operation = compilation.GetSemanticModel(syntaxTree).GetOperation(identifier, CancellationToken.None);

        return operation ?? throw new InvalidOperationException("The selected syntax node is not an operation.");
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create(
            assemblyName: "Test",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview))],
            references: References,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static PortableExecutableReference[] GetReferences()
    {
        return ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
    }
}
