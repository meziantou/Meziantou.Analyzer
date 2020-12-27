using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotUseDefaultEqualsOnValueTypeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new(
            RuleIdentifiers.DoNotUseDefaultEqualsOnValueType,
            title: "Default ValueType.Equals or HashCode is used for struct's equality",
            messageFormat: "Default ValueType.Equals or HashCode is used for struct's equality",
            RuleCategories.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseDefaultEqualsOnValueType));

        private static readonly DiagnosticDescriptor s_rule2 = new(
            RuleIdentifiers.StructWithDefaultEqualsImplementationUsedAsAKey,
            title: "Hash table unfriendly type is used in a hash table",
            messageFormat: "Hash table unfriendly type is used in a hash table",
            RuleCategories.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.StructWithDefaultEqualsImplementationUsedAsAKey));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule, s_rule2);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(ctx =>
            {
                var analyzeContext = new Context(ctx.Compilation);
                ctx.RegisterOperationAction(analyzeContext.AnalyzeInvocationOperation, OperationKind.Invocation);
                ctx.RegisterOperationAction(analyzeContext.AnalyzeFieldReferenceOperation, OperationKind.FieldReference);
                ctx.RegisterOperationAction(analyzeContext.AnalyzeObjectCreationOperation, OperationKind.ObjectCreation);
            });
        }

        private sealed class Context
        {
            public Compilation Compilation { get; }
            private INamedTypeSymbol? IEqualityComparerSymbol { get; }
            private INamedTypeSymbol? IComparerSymbol { get; }
            private ITypeSymbol? ValueTypeSymbol { get; }
            private ITypeSymbol? ImmutableDictionarySymbol { get; }
            private ITypeSymbol? ImmutableHashSetSymbol { get; }
            private ITypeSymbol? ImmutableSortedDictionarySymbol { get; }
            private IMethodSymbol? ValueTypeEqualsSymbol { get; }
            private IMethodSymbol? ValueTypeGetHashCodeSymbol { get; }
            private ITypeSymbol[] HashSetSymbols { get; }

            public Context(Compilation compilation)
            {
                IEqualityComparerSymbol = compilation.GetTypeByMetadataName("System.Collections.Generic.IEqualityComparer`1");
                IComparerSymbol = compilation.GetTypeByMetadataName("System.Collections.Generic.IComparer`1");
                ValueTypeSymbol = compilation.GetTypeByMetadataName("System.ValueType");
                if (ValueTypeSymbol != null)
                {
                    ValueTypeEqualsSymbol = ValueTypeSymbol.GetMembers(nameof(ValueType.Equals)).OfType<IMethodSymbol>().FirstOrDefault();
                    ValueTypeGetHashCodeSymbol = ValueTypeSymbol.GetMembers(nameof(ValueType.Equals)).OfType<IMethodSymbol>().FirstOrDefault();
                }

                ImmutableDictionarySymbol = compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableDictionary");
                ImmutableHashSetSymbol = compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableHashSet");
                ImmutableSortedDictionarySymbol = compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableSortedDictionary");

                var types = new List<ITypeSymbol>();
                types.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Generic.HashSet`1"));
                types.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2"));
                types.AddIfNotNull(compilation.GetTypesByMetadataName("System.Collections.Concurrent.ConcurrentDictionary`2"));
                types.AddIfNotNull(compilation.GetTypesByMetadataName("System.Collections.Immutable.ImmutableHashSet`1"));
                types.AddIfNotNull(compilation.GetTypesByMetadataName("System.Collections.Immutable.ImmutableDictionary`2"));
                types.AddIfNotNull(compilation.GetTypesByMetadataName("System.Collections.Immutable.ImmutableSortedDictionary`2"));
                HashSetSymbols = types.ToArray();
                Compilation = compilation;
            }

            public void AnalyzeInvocationOperation(OperationAnalysisContext context)
            {
                var operation = (IInvocationOperation)context.Operation;

                if (operation.TargetMethod.Name == nameof(ValueType.GetHashCode))
                {
                    var actualType = operation.Children.FirstOrDefault()?.GetActualType();
                    if (actualType == null)
                        return;

                    if (IsStruct(actualType) && HasDefaultEqualsOrHashCodeImplementations(actualType))
                    {
                        context.ReportDiagnostic(s_rule, operation);
                    }
                }
                else if (operation.TargetMethod.Name == nameof(ValueType.Equals))
                {
                    var actualType = operation.Children.FirstOrDefault()?.GetActualType();
                    if (actualType == null)
                        return;

                    if (IsStruct(actualType) && HasDefaultEqualsOrHashCodeImplementations(actualType))
                    {
                        context.ReportDiagnostic(s_rule, operation);
                    }
                }
                else if (IsImmutableCreateMethod(operation.TargetMethod))
                {
                    var type = operation.TargetMethod.TypeArguments[0];
                    if (IsStruct(type) && HasDefaultEqualsOrHashCodeImplementations(type))
                    {
                        if (operation.TargetMethod.ContainingType.IsEqualTo(ImmutableSortedDictionarySymbol))
                        {
                            if (operation.TargetMethod.Parameters.Any(arg => arg.Type.IsEqualTo(IComparerSymbol?.Construct(type))))
                                return;
                        }
                        else
                        {
                            if (operation.TargetMethod.Parameters.Any(arg => arg.Type.IsEqualTo(IEqualityComparerSymbol?.Construct(type))))
                                return;
                        }

                        context.ReportDiagnostic(s_rule2, operation);
                    }
                }

                bool IsImmutableCreateMethod(IMethodSymbol methodSymbol)
                {
                    var names = new[]
                    {
                        "Create",
                        "CreateBuilder",
                        "CreateRange",
                    };

                    var builderTypes = new[]
                    {
                        ImmutableDictionarySymbol,
                        ImmutableHashSetSymbol,
                        ImmutableSortedDictionarySymbol,
                    };

                    return methodSymbol.Arity >= 1 && names.Contains(methodSymbol.Name, StringComparer.Ordinal) && builderTypes.Any(type => type.IsEqualTo(methodSymbol.ContainingType.OriginalDefinition));
                }
            }

            private static bool IsStruct(ITypeSymbol typeSymbol)
            {
                return typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsValueType && namedTypeSymbol.EnumUnderlyingType == null;
            }

            public void AnalyzeFieldReferenceOperation(OperationAnalysisContext context)
            {
                var operation = (IFieldReferenceOperation)context.Operation;
                if (operation.Field.Name == "Empty")
                {
                    var type = operation.Field.ContainingType;
                    if (type?.OriginalDefinition == null)
                        return;

                    if (HashSetSymbols.Any(t => type.OriginalDefinition.IsEqualTo(t)))
                    {
                        if (IsStruct(type.TypeArguments[0]) && HasDefaultEqualsOrHashCodeImplementations(type.TypeArguments[0]))
                        {
                            context.ReportDiagnostic(s_rule2, operation);
                        }
                    }
                }
            }

            public void AnalyzeObjectCreationOperation(OperationAnalysisContext context)
            {
                var operation = (IObjectCreationOperation)context.Operation;
                var type = operation.Type as INamedTypeSymbol;
                if (type?.OriginalDefinition == null)
                    return;

                if (HashSetSymbols.Any(t => type.OriginalDefinition.IsEqualTo(t)))
                {
                    if (operation.Constructor.Parameters.Any(arg => arg.Type.IsEqualTo(IEqualityComparerSymbol?.Construct(type.TypeArguments[0]))))
                        return;

                    if (IsStruct(type.TypeArguments[0]) && HasDefaultEqualsOrHashCodeImplementations(type.TypeArguments[0]))
                    {
                        context.ReportDiagnostic(s_rule2, operation);
                    }
                }
            }

            private bool HasDefaultEqualsOrHashCodeImplementations(ITypeSymbol typeSymbol)
            {
                if (ValueTypeEqualsSymbol != null && typeSymbol.GetMembers(ValueTypeEqualsSymbol.Name).OfType<IMethodSymbol>().FirstOrDefault(member => member.IsOverride && ValueTypeEqualsSymbol.IsEqualTo(member.OverriddenMethod)) == null)
                    return true;

                if (ValueTypeGetHashCodeSymbol != null && typeSymbol.GetMembers(ValueTypeGetHashCodeSymbol.Name).OfType<IMethodSymbol>().FirstOrDefault(member => member.IsOverride && ValueTypeGetHashCodeSymbol.IsEqualTo(member.OverriddenMethod)) == null)
                    return true;

                return false;
            }
        }
    }
}
