using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseRegexSourceGeneratorFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseRegexSourceGenerator);

    public override FixAllProvider? GetFixAllProvider()
    {
        return null;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: false);
        if (nodeToFix is null)
            return;

        // Check if C# 14 or later is available
        var isCSharp14OrAbove = false;
        if (context.Document.Project.ParseOptions is CSharpParseOptions parseOptions)
        {
            isCSharp14OrAbove = parseOptions.LanguageVersion.IsCSharp14OrAbove();
        }

        // Always offer partial method
        context.RegisterCodeFix(
            CodeAction.Create(
                "Use Regex Source Generator (partial method)",
                cancellationToken => ConvertToSourceGenerator(context.Document, context.Diagnostics[0], usePartialProperty: false, cancellationToken),
                equivalenceKey: "Use Regex Source Generator (partial method)"),
            context.Diagnostics);

        // Offer partial property if C# 14 or later
        if (isCSharp14OrAbove)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use Regex Source Generator (partial property)",
                    cancellationToken => ConvertToSourceGenerator(context.Document, context.Diagnostics[0], usePartialProperty: true, cancellationToken),
                    equivalenceKey: "Use Regex Source Generator (partial property)"),
                context.Diagnostics);
        }
    }

    private static async Task<Document> ConvertToSourceGenerator(Document document, Diagnostic diagnostic, bool usePartialProperty, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var compilation = semanticModel!.Compilation;

        var regexSymbol = compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.Regex");
        var regexGeneratorAttributeSymbol = compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.GeneratedRegexAttribute");
        if (regexSymbol is null || regexGeneratorAttributeSymbol is null)
            return document;

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: false);
        var typeDeclaration = nodeToFix?.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (root is null || nodeToFix is null || typeDeclaration is null)
            return document;

        // Get type info before changing the root
        var properties = diagnostic.Properties;
        var operation = semanticModel.GetOperation(nodeToFix, cancellationToken);
        if (operation is null)
            return document;

        // Check if we're in a field or variable initializer context
        var (fieldOrVariableToRemove, fieldOrVariableSymbol) = GetFieldOrVariableToRemove(operation, cancellationToken);
        
        // Find all references to field/variable if we'll be removing it
        var nodesToReplace = new List<SyntaxNode>();
        if (usePartialProperty && fieldOrVariableToRemove is not null && fieldOrVariableSymbol is not null)
        {
            // For local variables, search manually in the containing method
            if (fieldOrVariableSymbol.Kind == SymbolKind.Local)
            {
                // Find the containing method/accessor
                var containingMethod = fieldOrVariableToRemove.Ancestors().FirstOrDefault(n => 
                    n is MethodDeclarationSyntax or AccessorDeclarationSyntax or PropertyDeclarationSyntax);
                
                if (containingMethod is not null)
                {
                    // Find all IdentifierNameSyntax nodes with the same name
                    var identifiers = containingMethod.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Where(id => id.Identifier.Text == fieldOrVariableSymbol.Name);
                    
                    foreach (var identifier in identifiers)
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(identifier, cancellationToken);
                        if (SymbolEqualityComparer.Default.Equals(symbolInfo.Symbol, fieldOrVariableSymbol))
                        {
                            // Don't include the identifier if it's part of the variable declarator itself
                            if (!identifier.Ancestors().Contains(fieldOrVariableToRemove))
                            {
                                nodesToReplace.Add(identifier);
                            }
                        }
                    }
                }
            }
            else
            {
                // For fields, use SymbolFinder
                var references = await SymbolFinder.FindReferencesAsync(fieldOrVariableSymbol, document.Project.Solution, cancellationToken).ConfigureAwait(false);
                var referenceLocations = references
                    .SelectMany(r => r.Locations)
                    .Where(loc => loc.Location.IsInSource && loc.Document.Id == document.Id)
                    .ToList();
                
                foreach (var refLocation in referenceLocations)
                {
                    var refNode = root.FindNode(refLocation.Location.SourceSpan);
                    if (refNode is IdentifierNameSyntax identifier)
                    {
                        // Don't include the identifier if it's part of the field declarator itself
                        if (!identifier.Ancestors().Contains(fieldOrVariableToRemove))
                        {
                            nodesToReplace.Add(identifier);
                        }
                    }
                }
            }
        }

        // Compute suggested name from context
        var methodName = GetSuggestedNameFromContext(operation) ?? "MyRegex";

        // Ensure the name is unique in the containing type
        var typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);
        if (typeSymbol is not null)
        {
            var members = typeSymbol.GetAllMembers().ToArray();
            // If we're going to remove a field/variable, exclude it from the uniqueness check
            if (usePartialProperty && fieldOrVariableSymbol is not null)
            {
                members = members.Where(m => !SymbolEqualityComparer.Default.Equals(m, fieldOrVariableSymbol)).ToArray();
            }
            while (members.Any(m => m.Name == methodName))
            {
                methodName += "_";
            }
        }

        // Add an annotation to track the field/variable if we need to remove it later
        var fieldOrVariableAnnotation = new SyntaxAnnotation();
        if (usePartialProperty && fieldOrVariableToRemove is not null)
        {
            root = root.ReplaceNode(fieldOrVariableToRemove, fieldOrVariableToRemove.WithAdditionalAnnotations(fieldOrVariableAnnotation));
            nodeToFix = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: false);
            typeDeclaration = nodeToFix?.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
            if (nodeToFix is null || typeDeclaration is null)
                return document;
        }

        // Add partial to the type hierarchy
        var count = 0;
        root = root.ReplaceNodes(nodeToFix.Ancestors().OfType<TypeDeclarationSyntax>(), (_, r) =>
        {
            if (!r.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                count++;
                return r.AddModifiers(Token(SyntaxKind.PartialKeyword));
            }

            return r;
        });

        // Get the new node to fix in the new syntax tree
        nodeToFix = root.FindNode(new Microsoft.CodeAnalysis.Text.TextSpan(nodeToFix.Span.Start + (count * "partial".Length), nodeToFix.Span.Length));
        if (nodeToFix is null)
            return document;

        typeDeclaration = nodeToFix.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (typeDeclaration is null)
            return document;

        var newTypeDeclaration = typeDeclaration;

        // Use new method or property
        if (operation is IObjectCreationOperation)
        {
            if (usePartialProperty)
            {
                var accessProperty = generator.IdentifierName(methodName);
                newTypeDeclaration = newTypeDeclaration.ReplaceNode(nodeToFix, accessProperty);
            }
            else
            {
                var invokeMethod = generator.InvocationExpression(generator.IdentifierName(methodName));
                newTypeDeclaration = newTypeDeclaration.ReplaceNode(nodeToFix, invokeMethod);
            }
        }
        else if (operation is IInvocationOperation invocationOperation)
        {
            var arguments = invocationOperation.Arguments;
            var indices = new[]
            {
                TryParseInt32(properties, UseRegexSourceGeneratorAnalyzerCommon.PatternIndexName),
                TryParseInt32(properties, UseRegexSourceGeneratorAnalyzerCommon.RegexOptionsIndexName),
                TryParseInt32(properties, UseRegexSourceGeneratorAnalyzerCommon.RegexTimeoutIndexName),
            };
            foreach (var index in indices.Where(value => value is not null).OrderDescending())
            {
                arguments = arguments.RemoveAt(index.GetValueOrDefault());
            }

            if (usePartialProperty)
            {
                var accessProperty = generator.IdentifierName(methodName);
                var method = generator.InvocationExpression(generator.MemberAccessExpression(accessProperty, invocationOperation.TargetMethod.Name), [.. arguments.Select(arg => arg.Syntax)]);
                newTypeDeclaration = newTypeDeclaration.ReplaceNode(nodeToFix, method);
            }
            else
            {
                var createRegexMethod = generator.InvocationExpression(generator.IdentifierName(methodName));
                var method = generator.InvocationExpression(generator.MemberAccessExpression(createRegexMethod, invocationOperation.TargetMethod.Name), [.. arguments.Select(arg => arg.Syntax)]);
                newTypeDeclaration = newTypeDeclaration.ReplaceNode(nodeToFix, method);
            }
        }

        // Generate method
        SyntaxNode? patternValue = null;
        SyntaxNode? regexOptionsValue = null;
        SyntaxNode? timeoutValue = null;

        var timeout = TryParseInt32(properties, UseRegexSourceGeneratorAnalyzerCommon.RegexTimeoutName);
        if (timeout is not null)
        {
            timeoutValue = generator.LiteralExpression(timeout.Value);
        }

        if (operation is IObjectCreationOperation objectCreationOperation)
        {
            patternValue = GetNode(objectCreationOperation.Arguments, properties, UseRegexSourceGeneratorAnalyzerCommon.PatternIndexName);
            regexOptionsValue = GetNode(objectCreationOperation.Arguments, properties, UseRegexSourceGeneratorAnalyzerCommon.RegexOptionsIndexName);
        }
        else if (operation is IInvocationOperation invocationOperation)
        {
            patternValue = GetNode(invocationOperation.Arguments, properties, UseRegexSourceGeneratorAnalyzerCommon.PatternIndexName);
            regexOptionsValue = GetNode(invocationOperation.Arguments, properties, UseRegexSourceGeneratorAnalyzerCommon.RegexOptionsIndexName);
        }

        if (timeoutValue is not null && regexOptionsValue is null)
        {
            regexOptionsValue = generator.MemberAccessExpression(generator.TypeExpression(compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.RegexOptions")!), "None");
        }

        SyntaxNode newMember;

        if (usePartialProperty)
        {
            // Generate partial property manually to ensure proper syntax
            var propertyType = (TypeSyntax)generator.TypeExpression(regexSymbol);
            var accessorList = AccessorList(
                List([
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                ]));

            var newProperty = PropertyDeclaration(propertyType, methodName)
                .WithModifiers(TokenList(
                    Token(SyntaxKind.PrivateKeyword),
                    Token(SyntaxKind.StaticKeyword),
                    Token(SyntaxKind.PartialKeyword)))
                .WithAccessorList(accessorList);

            newProperty = newProperty.ReplaceToken(newProperty.Identifier, Identifier(methodName).WithAdditionalAnnotations(RenameAnnotation.Create()));

            // Extract arguments (pattern,options,timeout)
            var attributes = generator.Attribute(generator.TypeExpression(regexGeneratorAttributeSymbol), attributeArguments: (patternValue, regexOptionsValue, timeoutValue) switch
            {
                ({ }, null, null) => [patternValue],
                ({ }, { }, null) => [patternValue, regexOptionsValue],
                ({ }, { }, { }) => [patternValue, regexOptionsValue, AttributeArgument((ExpressionSyntax)timeoutValue).WithNameColon(NameColon(IdentifierName("matchTimeoutMilliseconds")))],
                _ => Array.Empty<SyntaxNode>(),
            });

            newMember = (PropertyDeclarationSyntax)generator.AddAttributes(newProperty, attributes);
        }
        else
        {
            // Generate partial method
            var newMethod = (MethodDeclarationSyntax)generator.MethodDeclaration(
                name: methodName,
                returnType: generator.TypeExpression(regexSymbol),
                modifiers: DeclarationModifiers.Static | DeclarationModifiers.Partial,
                accessibility: Accessibility.Private);

            newMethod = newMethod.ReplaceToken(newMethod.Identifier, Identifier(methodName).WithAdditionalAnnotations(RenameAnnotation.Create()));

            // Extract arguments (pattern,options,timeout)
            var attributes = generator.Attribute(generator.TypeExpression(regexGeneratorAttributeSymbol), attributeArguments: (patternValue, regexOptionsValue, timeoutValue) switch
            {
                ({ }, null, null) => [patternValue],
                ({ }, { }, null) => [patternValue, regexOptionsValue],
                ({ }, { }, { }) => [patternValue, regexOptionsValue, AttributeArgument((ExpressionSyntax)timeoutValue).WithNameColon(NameColon(IdentifierName("matchTimeoutMilliseconds")))],
                _ => Array.Empty<SyntaxNode>(),
            });

            newMember = (MethodDeclarationSyntax)generator.AddAttributes(newMethod, attributes);
        }

        newTypeDeclaration = newTypeDeclaration.AddMembers((MemberDeclarationSyntax)newMember);
        
        // When using partial property and we're in a field or variable initializer,
        // remove the field/variable and replace all usages with the new property
        if (usePartialProperty && fieldOrVariableToRemove is not null && fieldOrVariableSymbol is not null)
        {
            // Add tracking annotations to all nodes that need to be replaced
            var replacementAnnotation = new SyntaxAnnotation();
            if (nodesToReplace.Count > 0)
            {
                root = root.ReplaceNodes(nodesToReplace, (original, _) => original.WithAdditionalAnnotations(replacementAnnotation));
                
                // Re-find the nodes after annotation
                nodeToFix = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: false);
                typeDeclaration = nodeToFix?.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
                newTypeDeclaration = typeDeclaration;
                if (nodeToFix is null || typeDeclaration is null)
                    return document;
                
                // Re-create the property in the new tree
                newTypeDeclaration = typeDeclaration;
                
                // Re-apply the replacement for nodeToFix
                if (operation is IObjectCreationOperation)
                {
                    if (usePartialProperty)
                    {
                        var accessProperty = generator.IdentifierName(methodName);
                        newTypeDeclaration = newTypeDeclaration.ReplaceNode(nodeToFix, accessProperty);
                    }
                }
                else if (operation is IInvocationOperation invocationOperation)
                {
                    var arguments = invocationOperation.Arguments;
                    var indices = new[]
                    {
                        TryParseInt32(properties, UseRegexSourceGeneratorAnalyzerCommon.PatternIndexName),
                        TryParseInt32(properties, UseRegexSourceGeneratorAnalyzerCommon.RegexOptionsIndexName),
                        TryParseInt32(properties, UseRegexSourceGeneratorAnalyzerCommon.RegexTimeoutIndexName),
                    };
                    foreach (var index in indices.Where(value => value is not null).OrderDescending())
                    {
                        arguments = arguments.RemoveAt(index.GetValueOrDefault());
                    }

                    if (usePartialProperty)
                    {
                        var accessProperty = generator.IdentifierName(methodName);
                        var method = generator.InvocationExpression(generator.MemberAccessExpression(accessProperty, invocationOperation.TargetMethod.Name), [.. arguments.Select(arg => arg.Syntax)]);
                        newTypeDeclaration = newTypeDeclaration.ReplaceNode(nodeToFix, method);
                    }
                }
                
                newTypeDeclaration = newTypeDeclaration.AddMembers((MemberDeclarationSyntax)newMember);
            }

            // Now apply the type declaration changes
            var updatedRoot = root.ReplaceNode(typeDeclaration, newTypeDeclaration);
            
            // Replace all annotated references with the property name
            var annotatedNodes = updatedRoot.GetAnnotatedNodes(replacementAnnotation).ToList();
            if (annotatedNodes.Count > 0)
            {
                var replacements = new Dictionary<SyntaxNode, SyntaxNode>();
                foreach (var annotatedNode in annotatedNodes)
                {
                    var newIdentifier = IdentifierName(methodName);
                    replacements[annotatedNode] = newIdentifier;
                }
                
                updatedRoot = updatedRoot.ReplaceNodes(replacements.Keys, (original, _) => replacements[original]);
            }

            // Remove the field or variable declaration
            var fieldOrVariableInUpdatedRoot = updatedRoot.GetAnnotatedNodes(fieldOrVariableAnnotation).FirstOrDefault();
            if (fieldOrVariableInUpdatedRoot is not null)
            {
                var newRoot = RemoveFieldOrVariable(updatedRoot, fieldOrVariableInUpdatedRoot);
                if (newRoot is not null)
                {
                    updatedRoot = newRoot;
                }
            }

            return document.WithSyntaxRoot(updatedRoot);
        }

        return document.WithSyntaxRoot(root.ReplaceNode(typeDeclaration, newTypeDeclaration));
    }

    private static SyntaxNode? GetNode(ImmutableArray<IArgumentOperation> args, ImmutableDictionary<string, string?> properties, string name)
    {
        var index = TryParseInt32(properties, name);
        if (index is null)
            return null;

        return args[index.Value].Value.Syntax;
    }

    private static int? TryParseInt32(ImmutableDictionary<string, string?> properties, string name)
    {
        if (!properties.TryGetValue(name, out var value))
            return null;

        if (!int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return null;

        return result;
    }

    private static string? GetSuggestedNameFromContext(IOperation operation)
    {
        // Walk up the operation tree to find if we're in a field or variable initializer
        foreach (var ancestor in operation.Ancestors())
        {
            // Check for field initializer
            if (ancestor is IFieldInitializerOperation fieldInitializer)
            {
                var field = fieldInitializer.InitializedFields.FirstOrDefault();
                if (field is not null)
                {
                    var fieldName = field.Name;
                    // Only use the field name if it's meaningful (more than 1 character)
                    if (fieldName.Length > 1)
                    {
                        return fieldName;
                    }
                }
            }

            // Check for variable declarator (local variable)
            if (ancestor is IVariableDeclaratorOperation variableDeclarator)
            {
                var variableName = variableDeclarator.Symbol.Name;
                // Only use the variable name if it's meaningful (more than 1 character)
                if (variableName.Length > 1)
                {
                    return ConvertToPascalCase(variableName);
                }
            }
        }

        return null;
    }

    private static string ConvertToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "MyRegex";

        // If already starts with uppercase, return as-is
        if (char.IsUpper(name[0]))
            return name;

        // Convert first letter to uppercase
        if (name.Length == 1)
            return char.ToUpperInvariant(name[0]).ToString();

        return char.ToUpperInvariant(name[0]) + name[1..];
    }

    private static (SyntaxNode? fieldOrVariable, ISymbol? symbol) GetFieldOrVariableToRemove(IOperation operation, CancellationToken cancellationToken)
    {
        // Walk up the operation tree to find if we're in a field or variable initializer
        foreach (var ancestor in operation.Ancestors())
        {
            // Check for field initializer
            if (ancestor is IFieldInitializerOperation fieldInitializer)
            {
                var field = fieldInitializer.InitializedFields.FirstOrDefault();
                if (field is not null)
                {
                    // Find the field declaration syntax
                    var fieldSyntax = field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken);
                    if (fieldSyntax is VariableDeclaratorSyntax fieldDeclarator)
                    {
                        return (fieldDeclarator, field);
                    }
                }
            }

            // Check for variable declarator (local variable)
            if (ancestor is IVariableDeclaratorOperation variableDeclaratorOp)
            {
                var variable = variableDeclaratorOp.Symbol;
                var variableSyntax = variable.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken);
                if (variableSyntax is VariableDeclaratorSyntax varDeclarator)
                {
                    return (varDeclarator, variable);
                }
            }
        }

        return (null, null);
    }

    private static SyntaxNode? RemoveFieldOrVariable(SyntaxNode root, SyntaxNode fieldOrVariableDeclarator)
    {
        if (fieldOrVariableDeclarator is not VariableDeclaratorSyntax declarator)
            return root;

        // Check if this is part of a VariableDeclaration (multiple variables in one statement)
        if (declarator.Parent is VariableDeclarationSyntax variableDeclaration)
        {
            // If it's the only variable, remove the entire declaration
            if (variableDeclaration.Variables.Count == 1)
            {
                // Check if it's part of a field declaration
                if (variableDeclaration.Parent is FieldDeclarationSyntax fieldDeclaration)
                {
                    return root.RemoveNode(fieldDeclaration, SyntaxRemoveOptions.KeepNoTrivia);
                }
                // Check if it's part of a local declaration statement
                else if (variableDeclaration.Parent is LocalDeclarationStatementSyntax localDeclaration)
                {
                    return root.RemoveNode(localDeclaration, SyntaxRemoveOptions.KeepNoTrivia);
                }
            }
            else
            {
                // Multiple variables, just remove this one
                return root.RemoveNode(declarator, SyntaxRemoveOptions.KeepNoTrivia);
            }
        }

        return root;
    }
}
