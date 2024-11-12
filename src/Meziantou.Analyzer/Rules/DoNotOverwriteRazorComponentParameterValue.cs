using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DoNotOverwriteRazorComponentParameterValue : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotOverwriteRazorComponentParameterValue,
        title: "Do not overwrite parameter value",
        messageFormat: "Do not overwrite parameter value",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false, // enable it by default when the rule is stable
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotOverwriteRazorComponentParameterValue));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (analyzerContext.IsValid)
            {
                ctx.RegisterOperationBlockStartAction(analyzerContext.OperationBlockStart);
            }
        });
    }

    private sealed class AnalyzerContext
    {
        public AnalyzerContext(Compilation compilation)
        {
            ComponentBaseSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.ComponentBase");
            ParameterAttributeSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.ParameterAttribute");
            CascadingParameterAttributeSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.CascadingParameterAttribute");
            IDisposable_DisposeMethodSymbol = compilation.GetBestTypeByMetadataName("System.IDisposable")?.GetMembers("Dispose").SingleOrDefaultIfMultiple() as IMethodSymbol;
            IAsyncDisposable_DisposeAsyncMethodSymbol = compilation.GetBestTypeByMetadataName("System.IAsyncDisposable")?.GetMembers("DisposeAsync").SingleOrDefaultIfMultiple() as IMethodSymbol;

            if (ComponentBaseSymbol is not null)
            {
                OnInitializedMethodSymbol = ComponentBaseSymbol.GetMembers("OnInitialized").SingleOrDefaultIfMultiple() as IMethodSymbol;
                OnInitializedAsyncMethodSymbol = ComponentBaseSymbol.GetMembers("OnInitializedAsync").SingleOrDefaultIfMultiple() as IMethodSymbol;
                SetParametersAsyncMethodSymbol = ComponentBaseSymbol.GetMembers("SetParametersAsync").SingleOrDefaultIfMultiple() as IMethodSymbol;
            }
        }

        public INamedTypeSymbol? ComponentBaseSymbol { get; }
        public INamedTypeSymbol? ParameterAttributeSymbol { get; }
        public INamedTypeSymbol? CascadingParameterAttributeSymbol { get; }

        public IMethodSymbol? OnInitializedMethodSymbol { get; }
        public IMethodSymbol? OnInitializedAsyncMethodSymbol { get; }
        public IMethodSymbol? SetParametersAsyncMethodSymbol { get; }
        public IMethodSymbol? IDisposable_DisposeMethodSymbol { get; }
        public IMethodSymbol? IAsyncDisposable_DisposeAsyncMethodSymbol { get; }

        public bool IsValid => ComponentBaseSymbol is not null && ParameterAttributeSymbol is not null;

        internal void OperationBlockStart(OperationBlockStartAnalysisContext context)
        {
            if (context.OwningSymbol is not IMethodSymbol methodSymbol)
                return;

            if (methodSymbol.MethodKind is MethodKind.Constructor or MethodKind.Destructor or MethodKind.StaticConstructor)
                return;

            if (methodSymbol.ContainingType.IsOrInheritFrom(ComponentBaseSymbol))
            {
                if (methodSymbol.Override(OnInitializedMethodSymbol) ||
                    methodSymbol.Override(OnInitializedAsyncMethodSymbol) ||
                    methodSymbol.Override(SetParametersAsyncMethodSymbol) ||
                    (IDisposable_DisposeMethodSymbol is not null && methodSymbol.GetImplementingInterfaceSymbol().IsEqualTo(IDisposable_DisposeMethodSymbol)) ||
                    (IAsyncDisposable_DisposeAsyncMethodSymbol is not null && methodSymbol.GetImplementingInterfaceSymbol().IsEqualTo(IAsyncDisposable_DisposeAsyncMethodSymbol)))
                {
                    return;
                }

                context.RegisterOperationAction(AnalyzeAssignment, OperationKind.SimpleAssignment);
            }
        }

        private void AnalyzeAssignment(OperationAnalysisContext context)
        {
            var operation = (IAssignmentOperation)context.Operation;
            if (operation.Target is not IPropertyReferenceOperation property)
                return;

            // this.Property
            if (property.Instance is not IInstanceReferenceOperation)
                return;

            // Ensure the property is a parameter
            if (!property.Property.HasAttribute(ParameterAttributeSymbol) && !property.Property.HasAttribute(CascadingParameterAttributeSymbol))
                return;

            context.ReportDiagnostic(Rule, operation);
        }
    }
}
