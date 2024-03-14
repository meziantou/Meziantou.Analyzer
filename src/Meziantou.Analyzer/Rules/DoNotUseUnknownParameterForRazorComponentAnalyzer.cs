using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseUnknownParameterForRazorComponentAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseUnknownParameterForRazorComponent,
        title: "Unknown component parameter",
        messageFormat: "The parameter '{0}' does not exist on component '{1}'",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseUnknownParameterForRazorComponent));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (analyzerContext.IsValid)
            {
                ctx.RegisterOperationAction(analyzerContext.AnalyzeBlockOptions, OperationKind.Block);
            }
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly ConcurrentDictionary<ITypeSymbol, ComponentDescriptor> _componentDescriptors = new(SymbolEqualityComparer.Default);

        public bool IsValid => IComponentSymbol is not null && ComponentBaseSymbol is not null && ParameterSymbol is not null;

        public INamedTypeSymbol? IComponentSymbol { get; } = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.IComponent");
        public INamedTypeSymbol? ComponentBaseSymbol { get; } = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.ComponentBase");
        public INamedTypeSymbol? ParameterSymbol { get; } = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.ParameterAttribute");
        public INamedTypeSymbol? RenderTreeBuilderSymbol { get; } = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder");

        public void AnalyzeBlockOptions(OperationAnalysisContext context)
        {
            var blockOperation = (IBlockOperation)context.Operation;

            ITypeSymbol? currentComponent = null;
            foreach (var operation in blockOperation.Operations)
            {
                if (operation is IExpressionStatementOperation expressionStatement)
                {
                    if (expressionStatement.Operation is IInvocationOperation invocation)
                    {
                        var targetMethod = invocation.TargetMethod;
                        if (targetMethod.ContainingType.IsEqualTo(RenderTreeBuilderSymbol))
                        {
                            if (targetMethod.Name == "OpenComponent" && targetMethod.TypeArguments.Length == 1)
                            {
                                var componentType = targetMethod.TypeArguments[0];
                                if (componentType.IsOrImplements(IComponentSymbol))
                                {
                                    currentComponent = targetMethod.TypeArguments[0];
                                }
                            }
                            else if (targetMethod.Name == "CloseComponent")
                            {
                                currentComponent = null;
                            }
                            else if (currentComponent is not null && targetMethod.Name is "AddAttribute" or "AddComponentParameter")
                            {
                                if (targetMethod.Parameters.Length >= 2 && targetMethod.Parameters[1].Type.IsString())
                                {
                                    var value = invocation.Arguments[1].Value.ConstantValue;
                                    if (value.HasValue && value.Value is string parameterName)
                                    {
                                        if (!IsValidAttribute(currentComponent, parameterName))
                                        {
                                            context.ReportDiagnostic(Rule, invocation.Syntax, parameterName, currentComponent.ToDisplayString(NullableFlowState.None));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool IsValidAttribute(ITypeSymbol componentType, string parameterName)
        {
            var descriptor = GetComponentDescriptor(componentType);
            if (descriptor.HasMatchUnmatchedParameters)
                return true;

            if (descriptor.Parameters.Contains(parameterName))
                return true;

            return false;
        }

        private ComponentDescriptor GetComponentDescriptor(ITypeSymbol typeSymbol)
        {
            return _componentDescriptors.GetOrAdd(typeSymbol, symbol =>
            {
                var descriptor = new ComponentDescriptor();
                var currentSymbol = symbol as INamedTypeSymbol;
                while (currentSymbol is not null)
                {
                    foreach (var member in currentSymbol.GetMembers())
                    {
                        if (member is IPropertySymbol property)
                        {
                            // https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.parameterattribute?view=aspnetcore-6.0&WT.mc_id=DT-MVP-5003978
                            var parameterAttribute = property.GetAttribute(ParameterSymbol, inherits: false); // the attribute is sealed
                            if (parameterAttribute is null)
                                continue;

                            if (descriptor.Parameters.Add(member.Name))
                            {
                                if (parameterAttribute.NamedArguments.Any(arg => arg.Key == "CaptureUnmatchedValues" && arg.Value.Value is true))
                                {
                                    descriptor.HasMatchUnmatchedParameters = true;
                                }
                            }
                        }
                    }

                    currentSymbol = currentSymbol.BaseType;
                }

                return descriptor;
            });
        }

        private sealed class ComponentDescriptor
        {
            public HashSet<string> Parameters { get; } = new HashSet<string>(System.StringComparer.Ordinal);
            public bool HasMatchUnmatchedParameters { get; set; }
        }
    }
}
