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
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.DoNotUseUnknownParameterForRazorComponent,
        title: "Unknown component parameter",
        messageFormat: "The parameter '{0}' does not exist on component '{1}'",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseUnknownParameterForRazorComponent));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

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

    private sealed class AnalyzerContext
    {
        private readonly ConcurrentDictionary<ITypeSymbol, ComponentDescriptor> _componentDescriptors = new(SymbolEqualityComparer.Default);

        public AnalyzerContext(Compilation compilation)
        {
            IComponentSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.IComponent");
            ParameterSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.ParameterAttribute");
            RenderTreeBuilderSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder");
            ComponentBaseSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.ComponentBase");
        }

        public bool IsValid => IComponentSymbol != null && ComponentBaseSymbol != null && ParameterSymbol != null;

        public INamedTypeSymbol? IComponentSymbol { get; }
        public INamedTypeSymbol? ComponentBaseSymbol { get; }
        public INamedTypeSymbol? ParameterSymbol { get; }
        public INamedTypeSymbol? RenderTreeBuilderSymbol { get; }

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
                            else if (currentComponent != null && targetMethod.Name == "AddAttribute")
                            {
                                if (targetMethod.Parameters.Length >= 2 && targetMethod.Parameters[1].Type.IsString())
                                {
                                    var value = invocation.Arguments[1].Value.ConstantValue;
                                    if (value.HasValue && value.Value is string parameterName)
                                    {
                                        if (!IsValidAttribute(currentComponent, parameterName))
                                        {
                                            context.ReportDiagnostic(s_rule, invocation.Syntax, parameterName, currentComponent.ToDisplayString(NullableFlowState.None));
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
                while (currentSymbol != null)
                {
                    foreach (var member in currentSymbol.GetMembers())
                    {
                        if (member is IPropertySymbol property)
                        {
                            // https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.parameterattribute?view=aspnetcore-6.0&WT.mc_id=DT-MVP-5003978
                            var parameterAttribute = property.GetAttribute(ParameterSymbol, inherits: false); // the attribute is sealed
                            if (parameterAttribute == null)
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
            public ISet<string> Parameters { get; } = new HashSet<string>(System.StringComparer.Ordinal);
            public bool HasMatchUnmatchedParameters { get; set; }
        }
    }
}
