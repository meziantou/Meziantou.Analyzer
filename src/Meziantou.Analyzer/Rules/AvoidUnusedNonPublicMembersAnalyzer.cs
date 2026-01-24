using System.Collections.Concurrent;
using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

// note: https://github.com/dotnet/roslyn/blob/bdf7c2666c7f3fe949f9f591272b23decf6d6be8/src/Analyzers/Core/Analyzers/RemoveUnusedMembers/AbstractRemoveUnusedMembersDiagnosticAnalyzer.cs#L43

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidUnusedNonPublicMembersAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.AvoidUnusedNonPublicMembers,
        title: "Avoid unused non-public members",
        messageFormat: "Member '{0}' is apparently never used",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AvoidUnusedNonPublicMembers),
        customTags: ["CompilationEnd"]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);

            // Gather potential unused members
            // Should it analyze type and use GetMember, so we can bail early for types that are used?
            ctx.RegisterSymbolAction(analyzerContext.AnalyzeField, SymbolKind.Field);
            ctx.RegisterSymbolAction(analyzerContext.AnalyzeMethod, SymbolKind.Method);
            ctx.RegisterSymbolAction(analyzerContext.AnalyzeEvent, SymbolKind.Event);
            ctx.RegisterSymbolAction(analyzerContext.AnalyzeNamedType, SymbolKind.NamedType);

            // Subscribe to potential usages
            ctx.RegisterOperationAction(analyzerContext.AnalyzeFieldReference, OperationKind.FieldReference);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeCollectionExpression, OperationKind.CollectionExpression);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeEventReference, OperationKind.EventReference);
            ctx.RegisterOperationAction(analyzerContext.AnalyzePropertyReference, OperationKind.PropertyReference);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeObjectCreation, OperationKind.ObjectCreation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeDeconstructionAssignment, OperationKind.DeconstructionAssignment);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeLoop, OperationKind.Loop);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeAwait, OperationKind.Await);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeConversion, OperationKind.Conversion);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeMethodReference, OperationKind.MethodReference);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeAnonymousFunction, OperationKind.AnonymousFunction);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeNameOf, OperationKind.NameOf);

            // At the end of the compilation, report unused members
            ctx.RegisterCompilationEndAction(analyzerContext.AnalyzeCompilationEnd);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly SymbolCollection _symbols = new(compilation);

        private readonly INamedTypeSymbol? _comImportAttribute = compilation.GetBestTypeByMetadataName("System.Runtime.InteropServices.ComImportAttribute");
        private readonly INamedTypeSymbol? _comVisibleAttribute = compilation.GetBestTypeByMetadataName("System.Runtime.InteropServices.ComVisibleAttribute");
        private readonly INamedTypeSymbol? _coClassAttribute = compilation.GetBestTypeByMetadataName("System.Runtime.InteropServices.CoClassAttribute");
        private readonly INamedTypeSymbol? _comRegisterFunctionAttribute = compilation.GetBestTypeByMetadataName("System.Runtime.InteropServices.ComRegisterFunctionAttribute");
        private readonly INamedTypeSymbol? _comUnregisterFunctionAttribute = compilation.GetBestTypeByMetadataName("System.Runtime.InteropServices.ComUnregisterFunctionAttribute");
        private readonly INamedTypeSymbol? _unmanagedCallersOnlyAttribute = compilation.GetBestTypeByMetadataName("System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute");
        private readonly INamedTypeSymbol? _debuggerDisplayAttribute = compilation.GetBestTypeByMetadataName("System.Diagnostics.DebuggerDisplayAttribute");
        private readonly INamedTypeSymbol? _dynamicallyAccessedMembersAttribute = compilation.GetBestTypeByMetadataName("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute");
        private readonly INamedTypeSymbol? _serializableAttribute = compilation.GetBestTypeByMetadataName("System.SerializableAttribute");
        private readonly INamedTypeSymbol? _jsonPropertyNameAttribute = compilation.GetBestTypeByMetadataName("System.Text.Json.Serialization.JsonPropertyNameAttribute");
        private readonly INamedTypeSymbol? _jsonIncludeAttribute = compilation.GetBestTypeByMetadataName("System.Text.Json.Serialization.JsonIncludeAttribute");
        private readonly INamedTypeSymbol? _jsonIgnoreAttribute = compilation.GetBestTypeByMetadataName("System.Text.Json.Serialization.JsonIgnoreAttribute");
        private readonly INamedTypeSymbol? _xmlElementAttribute = compilation.GetBestTypeByMetadataName("System.Xml.Serialization.XmlElementAttribute");
        private readonly INamedTypeSymbol? _xmlAttributeAttribute = compilation.GetBestTypeByMetadataName("System.Xml.Serialization.XmlAttributeAttribute");
        private readonly INamedTypeSymbol? _xmlArrayAttribute = compilation.GetBestTypeByMetadataName("System.Xml.Serialization.XmlArrayAttribute");
        private readonly INamedTypeSymbol? _xmlArrayItemAttribute = compilation.GetBestTypeByMetadataName("System.Xml.Serialization.XmlArrayItemAttribute");
        private readonly INamedTypeSymbol? _xmlIgnoreAttribute = compilation.GetBestTypeByMetadataName("System.Xml.Serialization.XmlIgnoreAttribute");
        private readonly INamedTypeSymbol? _dataMemberAttribute = compilation.GetBestTypeByMetadataName("System.Runtime.Serialization.DataMemberAttribute");
        private readonly INamedTypeSymbol? _yamlMemberAttribute = compilation.GetBestTypeByMetadataName("YamlDotNet.Serialization.YamlMemberAttribute");
        private readonly INamedTypeSymbol? _newtonsoftJsonPropertyAttribute = compilation.GetBestTypeByMetadataName("Newtonsoft.Json.JsonPropertyAttribute");
        private readonly INamedTypeSymbol? _newtonsoftJsonIgnoreAttribute = compilation.GetBestTypeByMetadataName("Newtonsoft.Json.JsonIgnoreAttribute");
        private readonly INamedTypeSymbol? _collectionBuilderAttribute = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.CollectionBuilderAttribute");
        private readonly INamedTypeSymbol? _inlineArrayAttribute = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.InlineArrayAttribute");

        private bool CouldSymbolBeRemoved(ISymbol symbol)
        {
            if (symbol.IsOverride)
                return false;

            if (symbol is IMethodSymbol methodSymbol)
            {
                var iface = methodSymbol.GetImplementingInterfaceSymbol();
                if (iface is not null)
                    return !iface.IsVisibleOutsideOfAssembly() && IsCurrentAssembly(iface);
            }
            else if (symbol is IPropertySymbol propertySymbol)
            {
                var iface = propertySymbol.GetImplementingInterfaceSymbol();
                if (iface is not null)
                    return !iface.IsVisibleOutsideOfAssembly() && IsCurrentAssembly(iface);
            }
            else if (symbol is IEventSymbol eventSymbol)
            {
                var iface = eventSymbol.GetImplementingInterfaceSymbol();
                if (iface is not null)
                    return !iface.IsVisibleOutsideOfAssembly() && IsCurrentAssembly(iface);
            }

            return true;

        }

        public void AnalyzeField(SymbolAnalysisContext context)
        {
            var field = (IFieldSymbol)context.Symbol;

            // Exclude backing fields in inline arrays
            if (_inlineArrayAttribute is not null && field.ContainingType.HasAttribute(_inlineArrayAttribute))
                return;

            _symbols.Add(field);
        }

        public void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;

            // Exclude static constructors
            if (method.MethodKind is MethodKind.StaticConstructor)
                return;

            // Exclude finalizers
            if (method.MethodKind is MethodKind.Destructor)
                return;

            // Exclude event accessors (handled by event analysis)
            if (method.MethodKind is MethodKind.EventAdd or MethodKind.EventRemove)
                return;

            // Exclude unit test methods
            if (method.IsUnitTestMethod())
                return;

            // Exclude UnmanagedCallersOnly methods
            if (method.HasAttribute(_unmanagedCallersOnlyAttribute))
                return;

            // Exclude COM register/unregister functions
            if (method.HasAttribute(_comRegisterFunctionAttribute))
                return;

            if (method.HasAttribute(_comUnregisterFunctionAttribute))
                return;

            // Exclude entry point
            if (method.IsEqualTo(compilation.GetEntryPoint(context.CancellationToken)))
                return;

            // Explicit implementation cannot be referenced through the interface
            if (method.ExplicitInterfaceImplementations.Length > 0)
                return;

            // Exclude methods in COM interfaces
            if (method.ContainingType.HasAttribute(_comImportAttribute))
                return;

            // Exclude methods in COM interfaces
            if (method.ContainingType.HasAttribute(_comVisibleAttribute))
                return;

            // Exclude ShouldSerialize<PropertyName> and Reset<PropertyName> methods when a matching property exists
            // These are Windows Forms designer patterns
            if (IsDesignerSerializationMethod(method))
                return;

            if (!CouldSymbolBeRemoved(method))
                return;

            _symbols.Add(method);
        }

        private static bool IsDesignerSerializationMethod(IMethodSymbol method)
        {
            // These patterns only work with instance methods
            if (method.IsStatic)
                return false;

            // ShouldSerialize<PropertyName>() -> bool
            if (method.Name.StartsWith("ShouldSerialize", StringComparison.Ordinal) &&
                method.Parameters.Length == 0 &&
                method.ReturnType.SpecialType == SpecialType.System_Boolean)
            {
                var propertyName = method.Name["ShouldSerialize".Length..];
                if (propertyName.Length > 0 && method.ContainingType.GetMembers(propertyName).OfType<IPropertySymbol>().Any(p => !p.IsStatic))
                {
                    return true;
                }
            }

            // Reset<PropertyName>() -> void
            if (method.Name.StartsWith("Reset", StringComparison.Ordinal) &&
                method.Parameters.Length == 0 &&
                method.ReturnType.SpecialType == SpecialType.System_Void)
            {
                var propertyName = method.Name["Reset".Length..];
                if (propertyName.Length > 0 && method.ContainingType.GetMembers(propertyName).OfType<IPropertySymbol>().Any(p => !p.IsStatic))
                {
                    return true;
                }
            }

            return false;
        }

        public void AnalyzeEvent(SymbolAnalysisContext context)
        {
            var eventSymbol = (IEventSymbol)context.Symbol;
            if (!CouldSymbolBeRemoved(eventSymbol))
                return;

            _symbols.Add(eventSymbol);
        }

        public void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var namedType = (INamedTypeSymbol)context.Symbol;

            // Check for DebuggerDisplay attribute and mark referenced members as used
            if (_debuggerDisplayAttribute is not null)
            {
                foreach (var attribute in namedType.GetAttributes())
                {
                    if (!attribute.AttributeClass.IsEqualTo(_debuggerDisplayAttribute))
                        continue;

                    // Check constructor argument (Value)
                    if (attribute.ConstructorArguments is [{ Value: string value }, ..])
                    {
                        MarkDebuggerDisplayMembersAsUsed(namedType, value);
                    }

                    // Check named arguments (Name and Type)
                    foreach (var argument in attribute.NamedArguments)
                    {
                        if (argument is { Key: "Name" or "Type", Value.Value: string namedValue })
                        {
                            MarkDebuggerDisplayMembersAsUsed(namedType, namedValue);
                        }
                    }
                }
            }

            // Check for DynamicallyAccessedMembers attribute and mark members as used based on flags
            if (_dynamicallyAccessedMembersAttribute is not null)
            {
                foreach (var attribute in namedType.GetAttributes())
                {
                    if (!attribute.AttributeClass.IsEqualTo(_dynamicallyAccessedMembersAttribute))
                        continue;

                    // Get the DynamicallyAccessedMemberTypes flags from the constructor argument
                    if (attribute.ConstructorArguments is [{ Value: int flags }, ..])
                    {
                        MarkDynamicallyAccessedMembers(namedType, flags);
                    }
                }
            }
        }

        private void MarkDebuggerDisplayMembersAsUsed(INamedTypeSymbol containingType, string debuggerDisplayValue)
        {
            var expressions = ExtractExpressions(debuggerDisplayValue.AsSpan());
            if (expressions is null)
                return;

            foreach (var expression in expressions)
            {
                if (string.IsNullOrWhiteSpace(expression))
                    continue;

                var expressionSyntax = SyntaxFactory.ParseExpression(expression);
                foreach (var memberPath in ExtractMemberPaths(expressionSyntax))
                {
                    if (memberPath.Count > 0)
                    {
                        // Mark the first member in the path as used (e.g., "Value" in "Value.ToString()")
                        var firstMember = memberPath[0];
                        foreach (var member in containingType.GetMembers(firstMember))
                        {
                            _symbols.MarkAsUsed(member);

                            // For properties in DebuggerDisplay, only the getter is used (read-only context)
                            if (member is IPropertySymbol property)
                            {
                                if (property.GetMethod is not null)
                                    _symbols.MarkAsUsed(property.GetMethod);
                            }
                        }
                    }
                }
            }

            static List<string>? ExtractExpressions(ReadOnlySpan<char> value)
            {
                List<string>? result = null;

                while (!value.IsEmpty)
                {
                    var startIndex = IndexOf(value, '{');
                    if (startIndex < 0)
                        break;

                    value = value[(startIndex + 1)..];
                    var endIndex = IndexOf(value, '}');
                    if (endIndex < 0)
                        break;

                    var expression = value[..endIndex];
                    result ??= [];
                    result.Add(expression.ToString());

                    value = value[(endIndex + 1)..];
                }

                return result;
            }

            static int IndexOf(ReadOnlySpan<char> value, char c)
            {
                var skipped = 0;
                while (!value.IsEmpty)
                {
                    var index = value.IndexOfAny(c, '\\');
                    if (index < 0)
                        return -1;

                    if (value[index] == c)
                        return index + skipped;

                    if (index + 1 < value.Length)
                    {
                        skipped += index + 2;
                        value = value[(index + 2)..];
                    }
                    else
                    {
                        return -1;
                    }
                }

                return -1;
            }

            static List<List<string>> ExtractMemberPaths(ExpressionSyntax expressionSyntax)
            {
                var paths = new List<List<string>>();
                ExtractMemberPathsRecursive(paths, expressionSyntax);
                return paths;

                static void ExtractMemberPathsRecursive(List<List<string>> results, ExpressionSyntax expressionSyntax)
                {
                    var path = new List<string>();

                    while (expressionSyntax is not null)
                    {
                        switch (expressionSyntax)
                        {
                            case ParenthesizedExpressionSyntax parenthesizedExpression:
                                if (path.Count is 0)
                                {
                                    expressionSyntax = parenthesizedExpression.Expression;
                                }
                                else
                                {
                                    if (parenthesizedExpression.Expression is MemberAccessExpressionSyntax or IdentifierNameSyntax or ParenthesizedExpressionSyntax)
                                    {
                                        expressionSyntax = parenthesizedExpression.Expression;
                                    }
                                    else
                                    {
                                        return;
                                    }
                                }
                                break;

                            case IdentifierNameSyntax identifierName:
                                path.Insert(0, identifierName.Identifier.ValueText);
                                results.Add(path);
                                return;

                            case MemberAccessExpressionSyntax memberAccessExpression:
                                path.Insert(0, memberAccessExpression.Name.Identifier.ValueText);
                                expressionSyntax = memberAccessExpression.Expression;
                                break;

                            case InvocationExpressionSyntax invocationExpression:
                                foreach (var argument in invocationExpression.ArgumentList.Arguments)
                                {
                                    ExtractMemberPathsRecursive(results, argument.Expression);
                                }
                                path.Clear();
                                expressionSyntax = invocationExpression.Expression;
                                break;

                            case BinaryExpressionSyntax binaryExpression:
                                ExtractMemberPathsRecursive(results, binaryExpression.Left);
                                ExtractMemberPathsRecursive(results, binaryExpression.Right);
                                return;

                            case PrefixUnaryExpressionSyntax unaryExpression:
                                ExtractMemberPathsRecursive(results, unaryExpression.Operand);
                                return;

                            case PostfixUnaryExpressionSyntax unaryExpression:
                                ExtractMemberPathsRecursive(results, unaryExpression.Operand);
                                return;

                            case ElementAccessExpressionSyntax elementAccess:
                                foreach (var argument in elementAccess.ArgumentList.Arguments)
                                {
                                    ExtractMemberPathsRecursive(results, argument.Expression);
                                }
                                path.Clear();
                                expressionSyntax = elementAccess.Expression;
                                break;

                            default:
                                return;
                        }
                    }

                    results.Add(path);
                }
            }
        }

        private void MarkDynamicallyAccessedMembers(INamedTypeSymbol containingType, int memberTypes)
        {
            // DynamicallyAccessedMemberTypes enum values (as defined in .NET)
            const int None = 0;
            const int PublicParameterlessConstructor = 0x0001;
            const int PublicConstructors = 0x0002;
            const int NonPublicConstructors = 0x0004;
            const int PublicMethods = 0x0008;
            const int NonPublicMethods = 0x0010;
            const int PublicFields = 0x0020;
            const int NonPublicFields = 0x0040;
            const int PublicNestedTypes = 0x0080;
            const int NonPublicNestedTypes = 0x0100;
            const int PublicProperties = 0x0200;
            const int NonPublicProperties = 0x0400;
            const int PublicEvents = 0x0800;
            const int NonPublicEvents = 0x1000;

            foreach (var member in containingType.GetMembers())
            {
                switch (member)
                {
                    case IMethodSymbol method when method.MethodKind is MethodKind.Constructor:
                        if ((memberTypes & NonPublicConstructors) != 0 && method.DeclaredAccessibility is not Accessibility.Public)
                        {
                            _symbols.MarkAsUsed(method);
                        }
                        else if ((memberTypes & PublicConstructors) != 0 && method.DeclaredAccessibility is Accessibility.Public)
                        {
                            _symbols.MarkAsUsed(method);
                        }
                        else if ((memberTypes & PublicParameterlessConstructor) != 0 && method.DeclaredAccessibility is Accessibility.Public && method.Parameters.Length == 0)
                        {
                            _symbols.MarkAsUsed(method);
                        }
                        break;

                    case IMethodSymbol method when method.MethodKind is MethodKind.Ordinary:
                        if ((memberTypes & NonPublicMethods) != 0 && method.DeclaredAccessibility is not Accessibility.Public)
                        {
                            _symbols.MarkAsUsed(method);
                        }
                        else if ((memberTypes & PublicMethods) != 0 && method.DeclaredAccessibility is Accessibility.Public)
                        {
                            _symbols.MarkAsUsed(method);
                        }
                        break;

                    case IFieldSymbol field:
                        if ((memberTypes & NonPublicFields) != 0 && field.DeclaredAccessibility is not Accessibility.Public)
                        {
                            _symbols.MarkAsUsed(field);
                        }
                        else if ((memberTypes & PublicFields) != 0 && field.DeclaredAccessibility is Accessibility.Public)
                        {
                            _symbols.MarkAsUsed(field);
                        }
                        break;

                    case IPropertySymbol property:
                        if ((memberTypes & NonPublicProperties) != 0 && property.DeclaredAccessibility is not Accessibility.Public)
                        {
                            _symbols.MarkAsUsed(property);
                            if (property.GetMethod is not null)
                                _symbols.MarkAsUsed(property.GetMethod);
                            if (property.SetMethod is not null)
                                _symbols.MarkAsUsed(property.SetMethod);
                        }
                        else if ((memberTypes & PublicProperties) != 0 && property.DeclaredAccessibility is Accessibility.Public)
                        {
                            _symbols.MarkAsUsed(property);
                            if (property.GetMethod is not null)
                                _symbols.MarkAsUsed(property.GetMethod);
                            if (property.SetMethod is not null)
                                _symbols.MarkAsUsed(property.SetMethod);
                        }
                        break;

                    case IEventSymbol eventSymbol:
                        if ((memberTypes & NonPublicEvents) != 0 && eventSymbol.DeclaredAccessibility is not Accessibility.Public)
                        {
                            _symbols.MarkAsUsed(eventSymbol);
                        }
                        else if ((memberTypes & PublicEvents) != 0 && eventSymbol.DeclaredAccessibility is Accessibility.Public)
                        {
                            _symbols.MarkAsUsed(eventSymbol);
                        }
                        break;

                    case INamedTypeSymbol nestedType:
                        if ((memberTypes & NonPublicNestedTypes) != 0 && nestedType.DeclaredAccessibility is not Accessibility.Public)
                        {
                            _symbols.MarkAsUsed(nestedType);
                        }
                        else if ((memberTypes & PublicNestedTypes) != 0 && nestedType.DeclaredAccessibility is Accessibility.Public)
                        {
                            _symbols.MarkAsUsed(nestedType);
                        }
                        break;
                }
            }
        }

        private void HandleDynamicallyAccessedMembersDataFlow(ITypeSymbol? flowedType, IParameterSymbol parameter)
        {
            if (flowedType is not INamedTypeSymbol namedType)
                return;

            if (_dynamicallyAccessedMembersAttribute is null)
                return;

            // Check if the parameter has DynamicallyAccessedMembers attribute
            foreach (var attribute in parameter.GetAttributes())
            {
                if (!attribute.AttributeClass.IsEqualTo(_dynamicallyAccessedMembersAttribute))
                    continue;

                // Get the DynamicallyAccessedMemberTypes flags from the constructor argument
                if (attribute.ConstructorArguments is [{ Value: int flags }, ..])
                {
                    MarkDynamicallyAccessedMembers(namedType, flags);
                }
            }
        }

        private void HandleDynamicallyAccessedMembersDataFlow(ITypeSymbol? flowedType, IFieldSymbol field)
        {
            if (flowedType is not INamedTypeSymbol namedType)
                return;

            if (_dynamicallyAccessedMembersAttribute is null)
                return;

            // Check if the field has DynamicallyAccessedMembers attribute
            foreach (var attribute in field.GetAttributes())
            {
                if (!attribute.AttributeClass.IsEqualTo(_dynamicallyAccessedMembersAttribute))
                    continue;

                // Get the DynamicallyAccessedMemberTypes flags from the constructor argument
                if (attribute.ConstructorArguments is [{ Value: int flags }, ..])
                {
                    MarkDynamicallyAccessedMembers(namedType, flags);
                }
            }
        }

        private void HandleDynamicallyAccessedMembersDataFlow(ITypeSymbol? flowedType, IPropertySymbol property)
        {
            if (flowedType is not INamedTypeSymbol namedType)
                return;

            if (_dynamicallyAccessedMembersAttribute is null)
                return;

            // Check if the property has DynamicallyAccessedMembers attribute
            foreach (var attribute in property.GetAttributes())
            {
                if (!attribute.AttributeClass.IsEqualTo(_dynamicallyAccessedMembersAttribute))
                    continue;

                // Get the DynamicallyAccessedMemberTypes flags from the constructor argument
                if (attribute.ConstructorArguments is [{ Value: int flags }, ..])
                {
                    MarkDynamicallyAccessedMembers(namedType, flags);
                }
            }
        }

        public void AnalyzeCompilationEnd(CompilationAnalysisContext context)
        {
            foreach (var member in _symbols.GetUnusedSymbol())
            {
                context.ReportDiagnostic(Rule, member, member.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            }
        }

        public void AnalyzeFieldReference(OperationAnalysisContext context)
        {
            var operation = (IFieldReferenceOperation)context.Operation;

            // Determine the usage kind based on the operation context
            var usage = GetUsageKind(operation);

            // Mark the field with the appropriate usage kind
            _symbols.MarkAsUsed(operation.Member, usage);

            // Track data flow for DynamicallyAccessedMembers on field
            if (usage.HasFlag(SymbolUsageKind.Write) &&
                operation.Parent is IAssignmentOperation assignment &&
                assignment.Target == operation &&
                GetTypeOfOperation(assignment.Value) is { } typeOfOperation)
            {
                HandleDynamicallyAccessedMembersDataFlow(typeOfOperation.TypeOperand, operation.Field);
            }

            static SymbolUsageKind GetUsageKind(IFieldReferenceOperation operation)
            {
                // Check for compound assignment (e.g., +=, -=, etc.) before regular assignment
                // because ICompoundAssignmentOperation inherits from IAssignmentOperation
                if (operation.Parent is ICompoundAssignmentOperation compoundAssignment && compoundAssignment.Target == operation)
                {
                    return SymbolUsageKind.ReadWrite;
                }

                // Check for coalesce assignment (??=) before regular assignment
                // because ICoalesceAssignmentOperation inherits from IAssignmentOperation
                if (operation.Parent is ICoalesceAssignmentOperation coalesceAssignment && coalesceAssignment.Target == operation)
                {
                    return SymbolUsageKind.ReadWrite;
                }

                // Check if the field reference is the target of an assignment
                if (operation.Parent is IAssignmentOperation assignment && assignment.Target == operation)
                {
                    return SymbolUsageKind.Write;
                }

                // Check for increment/decrement operations (e.g., ++, --)
                if (operation.Parent is IIncrementOrDecrementOperation)
                {
                    return SymbolUsageKind.ReadWrite;
                }

                // Default to Read
                return SymbolUsageKind.Read;
            }

            static ITypeOfOperation? GetTypeOfOperation(IOperation operation)
            {
                return operation switch
                {
                    ITypeOfOperation typeOf => typeOf,
                    IConversionOperation { Operand: ITypeOfOperation typeOf } => typeOf,
                    _ => null,
                };
            }
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;

            // Don't mark a method as used if it's only calling itself (recursive call)
            // Find the containing method
            var containingMethod = operation.SemanticModel?.GetEnclosingSymbol(operation.Syntax.SpanStart) as IMethodSymbol;
            var isSelfReference = containingMethod is not null && SymbolEqualityComparer.Default.Equals(containingMethod.OriginalDefinition, operation.TargetMethod.OriginalDefinition);

            if (!isSelfReference)
            {
                _symbols.MarkAsUsed(operation.TargetMethod);
            }

            // Track data flow for DynamicallyAccessedMembers on method parameters
            foreach (var argument in operation.Arguments)
            {
                if (argument.Value is IConversionOperation { Operand: ITypeOfOperation typeOfOperation })
                {
                    HandleDynamicallyAccessedMembersDataFlow(typeOfOperation.TypeOperand, argument.Parameter);
                }
                else if (argument.Value is ITypeOfOperation directTypeOf)
                {
                    HandleDynamicallyAccessedMembersDataFlow(directTypeOf.TypeOperand, argument.Parameter);
                }
            }
        }

        public void AnalyzeMethodReference(OperationAnalysisContext context)
        {
            var operation = (IMethodReferenceOperation)context.Operation;

            // Don't mark a method as used if it's only referencing itself (recursion)
            var containingMethod = operation.SemanticModel!.GetEnclosingSymbol(operation.Syntax.SpanStart) as IMethodSymbol;
            var isSelfReference = operation.Method.OriginalDefinition.IsEqualTo(containingMethod?.OriginalDefinition);

            if (!isSelfReference)
            {
                _symbols.MarkAsUsed(operation.Method);
            }
        }

        public void AnalyzeCollectionExpression(OperationAnalysisContext context)
        {
            var operation = (ICollectionExpressionOperation)context.Operation;
            _symbols.MarkAsUsed(operation.ConstructMethod);
        }

        public void AnalyzeEventReference(OperationAnalysisContext context)
        {
            var operation = (IEventReferenceOperation)context.Operation;
            _symbols.MarkAsUsed(operation.Event);
        }
        public void AnalyzePropertyReference(OperationAnalysisContext context)
        {
            var operation = (IPropertyReferenceOperation)context.Operation;

            // Determine the usage kind based on the operation context
            var usage = GetUsageKind(operation);

            // Mark the property with the appropriate usage kind
            _symbols.MarkAsUsed(operation.Property, usage);

            // Also mark the appropriate accessor as used
            if (usage.HasFlag(SymbolUsageKind.Read) && operation.Property.GetMethod is not null)
            {
                _symbols.MarkAsUsed(operation.Property.GetMethod, SymbolUsageKind.Read);
            }
            if (usage.HasFlag(SymbolUsageKind.Write) && operation.Property.SetMethod is not null)
            {
                _symbols.MarkAsUsed(operation.Property.SetMethod, SymbolUsageKind.Write);

                // Track data flow for DynamicallyAccessedMembers on setter parameter
                if (operation.Parent is IAssignmentOperation assignment &&
                    assignment.Target == operation &&
                    GetTypeOfOperation(assignment.Value) is { } typeOfOperation)
                {
                    // For property setters and indexers, the value parameter is always the last parameter
                    var valueParameter = operation.Property.SetMethod.Parameters[^1];
                    HandleDynamicallyAccessedMembersDataFlow(typeOfOperation.TypeOperand, valueParameter);
                }

                // Also check if the property itself has DynamicallyAccessedMembers attribute
                if (operation.Parent is IAssignmentOperation assignment2 &&
                    assignment2.Target == operation &&
                    GetTypeOfOperation(assignment2.Value) is { } typeOfOperation2)
                {
                    HandleDynamicallyAccessedMembersDataFlow(typeOfOperation2.TypeOperand, operation.Property);
                }
            }

            static SymbolUsageKind GetUsageKind(IPropertyReferenceOperation operation)
            {
                // Check for compound assignment (e.g., +=, -=, etc.) before regular assignment
                // because ICompoundAssignmentOperation inherits from IAssignmentOperation
                if (operation.Parent is ICompoundAssignmentOperation compoundAssignment && compoundAssignment.Target == operation)
                {
                    return SymbolUsageKind.ReadWrite;
                }

                // Check for coalesce assignment (??=) before regular assignment
                // because ICoalesceAssignmentOperation inherits from IAssignmentOperation
                if (operation.Parent is ICoalesceAssignmentOperation coalesceAssignment && coalesceAssignment.Target == operation)
                {
                    return SymbolUsageKind.ReadWrite;
                }

                // Check if the property reference is the target of an assignment
                if (operation.Parent is IAssignmentOperation assignment && assignment.Target == operation)
                {
                    return SymbolUsageKind.Write;
                }

                // Check for increment/decrement operations (e.g., ++, --)
                if (operation.Parent is IIncrementOrDecrementOperation)
                {
                    return SymbolUsageKind.ReadWrite;
                }

                // Default to Read
                return SymbolUsageKind.Read;
            }

            static ITypeOfOperation? GetTypeOfOperation(IOperation operation)
            {
                return operation switch
                {
                    ITypeOfOperation typeOf => typeOf,
                    IConversionOperation { Operand: ITypeOfOperation typeOf } => typeOf,
                    _ => null,
                };
            }
        }

        public void AnalyzeObjectCreation(OperationAnalysisContext context)
        {
            var operation = (IObjectCreationOperation)context.Operation;
            _symbols.MarkAsUsed(operation.Constructor);
        }

        public void AnalyzeDeconstructionAssignment(OperationAnalysisContext context)
        {
            var operation = (IDeconstructionAssignmentOperation)context.Operation;

            // For deconstruction, the compiler calls a Deconstruct method
            // This might be on the type itself or an extension method
            // We need to find it via the semantic model
            var syntax = operation.Syntax;
            var semanticModel = context.Operation.SemanticModel!;

            // Use GetDeconstructionInfo to get the Deconstruct method (supports extension methods)
            if (syntax is AssignmentExpressionSyntax assignmentSyntax)
            {
                var deconstructionInfo = semanticModel.GetDeconstructionInfo(assignmentSyntax);
                _symbols.MarkAsUsed(deconstructionInfo.Method);
            }
        }

        public void AnalyzeLoop(OperationAnalysisContext context)
        {
            if (context.Operation.Syntax is ForEachStatementSyntax foreachStatementSyntax)
            {
                var info = context.Operation.SemanticModel!.GetForEachStatementInfo(foreachStatementSyntax);
                _symbols.MarkAsUsed(info.GetEnumeratorMethod);
                _symbols.MarkAsUsed(info.MoveNextMethod);
                _symbols.MarkAsUsed(info.CurrentProperty, SymbolUsageKind.Read);
                _symbols.MarkAsUsed(info.DisposeMethod);
                _symbols.MarkAsUsed(info.ElementConversion.MethodSymbol);
                _symbols.MarkAsUsed(info.CurrentConversion.MethodSymbol);
            }
        }

        internal void AnalyzeAwait(OperationAnalysisContext context)
        {
            var operation = (IAwaitOperation)context.Operation;
            if (operation.Syntax is AwaitExpressionSyntax awaitSyntax)
            {
                var info = operation.SemanticModel!.GetAwaitExpressionInfo(awaitSyntax);
                _symbols.MarkAsUsed(info.GetAwaiterMethod);
                _symbols.MarkAsUsed(info.RuntimeAwaitMethod);
                _symbols.MarkAsUsed(info.GetResultMethod);
                _symbols.MarkAsUsed(info.IsCompletedProperty, SymbolUsageKind.Read);
            }
        }

        public void AnalyzeConversion(OperationAnalysisContext context)
        {
            var operation = (IConversionOperation)context.Operation;
            _symbols.MarkAsUsed(operation.OperatorMethod);
        }

        public void AnalyzeAnonymousFunction(OperationAnalysisContext context)
        {
            var operation = (IAnonymousFunctionOperation)context.Operation;
            var lambda = operation.Symbol;

            // Check for parameters with default values
            foreach (var parameter in lambda.Parameters)
            {
                if (!parameter.HasExplicitDefaultValue)
                    continue;

                // Get the parameter syntax to find field references in the default value expression
                foreach (var syntaxRef in parameter.DeclaringSyntaxReferences)
                {
                    var syntax = syntaxRef.GetSyntax(context.CancellationToken);
                    if (syntax is ParameterSyntax parameterSyntax && parameterSyntax.Default?.Value is { } defaultValueExpression)
                    {
                        var semanticModel = context.Operation.SemanticModel!;
                        MarkSymbolsInExpression(defaultValueExpression, semanticModel, context.CancellationToken);
                    }
                }
            }
        }

        public void AnalyzeNameOf(OperationAnalysisContext context)
        {
            var operation = (INameOfOperation)context.Operation;

            // Mark the referenced symbol as used since nameof() is often used for reflection-safe member access
            // ReadWrite as you don't know how the symbol is used
            switch (operation.Argument)
            {
                case IMemberReferenceOperation memberReference:
                    _symbols.MarkAsUsed(memberReference.Member, SymbolUsageKind.ReadWrite);
                    break;

                default:
                    // For cases where the operation is not a direct member reference (e.g., NoneOperation for method names),
                    // we need to use GetSymbolInfo to extract the referenced symbol
                    var symbolInfo = context.Operation.SemanticModel!.GetSymbolInfo(operation.Argument.Syntax, context.CancellationToken);

                    if (symbolInfo.Symbol is not null)
                    {
                        _symbols.MarkAsUsed(symbolInfo.Symbol, SymbolUsageKind.ReadWrite);
                    }
                    else if (!symbolInfo.CandidateSymbols.IsDefaultOrEmpty)
                    {
                        foreach (var candidateSymbol in symbolInfo.CandidateSymbols)
                        {
                            _symbols.MarkAsUsed(candidateSymbol, SymbolUsageKind.ReadWrite);
                        }
                    }
                    break;
            }
        }

        private void MarkSymbolsInExpression(SyntaxNode expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            foreach (var node in expression.DescendantNodesAndSelf())
            {
                var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
                if (symbolInfo.Symbol is not null)
                {
                    _symbols.MarkAsUsed(symbolInfo.Symbol, SymbolUsageKind.Read);
                }
            }
        }

        private bool IsCurrentAssembly(ISymbol symbol)
        {
            return symbol.ContainingAssembly.IsEqualTo(compilation.Assembly);
        }
    }

    [Flags]
    private enum SymbolUsageKind : byte
    {
        None = 0,
        Read = 1,
        Write = 2,
        ReadWrite = Read | Write,
    }

    private sealed class SymbolCollection(Compilation compilation)
    {
        private readonly ConcurrentDictionary<ISymbol, SymbolUsageKind> _symbols = new(SymbolEqualityComparer.Default);

        public void Add(ISymbol? symbol)
        {
            Add(symbol, SymbolUsageKind.None);
        }

        public void MarkAsUsed(ISymbol? symbol, SymbolUsageKind usage = SymbolUsageKind.ReadWrite)
        {
            Add(symbol, usage);
        }

        private void Add(ISymbol? symbol, SymbolUsageKind usage = SymbolUsageKind.ReadWrite)
        {
            if (symbol is null || !IsSupportedSymbol(symbol))
                return;

            if (symbol is INamedTypeSymbol namedTypeSymbol)
            {
                AddSymbol(namedTypeSymbol.OriginalDefinition, usage);
            }
            else if (symbol is IMethodSymbol methodSymbol)
            {
                AddSymbol(methodSymbol.OriginalDefinition, usage);

                // For partial methods, mark both declaration and implementation
                if (methodSymbol.PartialDefinitionPart is not null)
                {
                    AddSymbol(methodSymbol.PartialDefinitionPart, usage);
                }
                if (methodSymbol.PartialImplementationPart is not null)
                {
                    AddSymbol(methodSymbol.PartialImplementationPart, usage);
                }
            }
            else if (symbol is IPropertySymbol propertySymbol)
            {
                AddSymbol(propertySymbol.OriginalDefinition, usage);
                if (usage.HasFlag(SymbolUsageKind.Read) && propertySymbol.GetMethod is not null)
                {
                    AddSymbol(propertySymbol.GetMethod.OriginalDefinition, SymbolUsageKind.Read);
                }
                if (usage.HasFlag(SymbolUsageKind.Write) && propertySymbol.SetMethod is not null)
                {
                    AddSymbol(propertySymbol.SetMethod.OriginalDefinition, SymbolUsageKind.Write);
                }
            }
            else if (symbol is IFieldSymbol fieldSymbol)
            {
                AddSymbol(fieldSymbol.OriginalDefinition, usage);
            }
            else
            {
                AddSymbol(symbol, usage);
            }

            void AddSymbol(ISymbol symbol, SymbolUsageKind kind)
            {
                _symbols.AddOrUpdate(symbol, _ => usage, (_, existingValue) => existingValue | usage);
            }
        }

        private bool IsSupportedSymbol(ISymbol symbol)
        {
            if (symbol.IsVisibleOutsideOfAssembly())
                return false;

            if (symbol.IsImplicitlyDeclared)
                return false;

            if (!IsCurrentAssembly(symbol))
                return false;

            return true;
        }

        private bool IsCurrentAssembly(ISymbol symbol)
        {
            return symbol.ContainingAssembly.IsEqualTo(compilation.Assembly);
        }

        public IEnumerable<ISymbol> GetUnusedSymbol()
        {
            foreach (var symbol in _symbols)
            {
                if (symbol.Value is 0)
                    yield return symbol.Key;
            }
        }
    }
}
