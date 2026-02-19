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
using Microsoft.CodeAnalysis.Rename;
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

        // Offer partial property first if C# 14 or later
        if (isCSharp14OrAbove)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use Regex Source Generator (partial property)",
                    cancellationToken => ConvertToSourceGenerator(context.Document, context.Diagnostics[0], usePartialProperty: true, cancellationToken),
                    equivalenceKey: "Use Regex Source Generator (partial property)"),
                context.Diagnostics);
        }

        // Always offer partial method
        context.RegisterCodeFix(
            CodeAction.Create(
                "Use Regex Source Generator (partial method)",
                cancellationToken => ConvertToSourceGenerator(context.Document, context.Diagnostics[0], usePartialProperty: false, cancellationToken),
                equivalenceKey: "Use Regex Source Generator (partial method)"),
            context.Diagnostics);
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
        if (root is null || nodeToFix is null)
            return document;

        // Check if we're in a top-level statement (no type declaration)
        var isTopLevelStatement = typeDeclaration is null;

        // Get type info before changing the root
        var properties = diagnostic.Properties;
        var operation = semanticModel.GetOperation(nodeToFix, cancellationToken);
        if (operation is null)
            return document;

        // Check if we're in a field or variable initializer context that should be removed
        var (fieldOrVariableToRemove, fieldOrVariableSymbol) = GetFieldOrVariableToRemove(operation, cancellationToken);
        var shouldRemoveFieldOrVariable = usePartialProperty && fieldOrVariableToRemove is not null && fieldOrVariableSymbol is not null;

        // Compute suggested name from context
        var methodName = GetSuggestedNameFromContext(operation) ?? "MyRegex";

        // Ensure the name is unique in the containing type
        ITypeSymbol? typeSymbol = null;
        if (isTopLevelStatement)
        {
            // For top-level statements, we need to find or assume the Program type
            typeSymbol = compilation.GetBestTypeByMetadataName("Program");
        }
        else if (typeDeclaration is not null)
        {
            typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);
        }

        if (typeSymbol is not null)
        {
            var members = typeSymbol.GetAllMembers().ToArray();
            // If we're going to remove a field/variable, exclude it from the uniqueness check
            if (shouldRemoveFieldOrVariable)
            {
                members = members.Where(m => !SymbolEqualityComparer.Default.Equals(m, fieldOrVariableSymbol)).ToArray();
            }
            while (members.Any(m => m.Name == methodName))
            {
                methodName += "_";
            }
        }

        // If we need to rename the field/variable to match the property name, do it first
        // But only if there are references beyond the declaration
        if (shouldRemoveFieldOrVariable && fieldOrVariableSymbol!.Name != methodName)
        {
            // Check if there are any references to this symbol beyond the declaration
            var solution = document.Project.Solution;
            var references = await SymbolFinder.FindReferencesAsync(fieldOrVariableSymbol, solution, cancellationToken).ConfigureAwait(false);
            var referenceLocations = references
                .SelectMany(r => r.Locations)
                .Where(loc => loc.Location.IsInSource && loc.Document.Id == document.Id)
                .ToList();
                
            // Check if any reference is outside the declarator (not just the field declaration itself)
            var hasExternalReferences = false;
            if (fieldOrVariableToRemove is not null)
            {
                foreach (var refLoc in referenceLocations)
                {
                    var refNode = root.FindNode(refLoc.Location.SourceSpan);
                    if (refNode is not null && !refNode.Ancestors().Contains(fieldOrVariableToRemove))
                    {
                        hasExternalReferences = true;
                        break;
                    }
                }
            }
            
            // Only rename if there are actual references outside the declaration
            if (hasExternalReferences)
            {
                var renamedSolution = await Renamer.RenameSymbolAsync(solution, fieldOrVariableSymbol, new SymbolRenameOptions(), methodName, cancellationToken).ConfigureAwait(false);
                document = renamedSolution.GetDocument(document.Id)!;
                
                // Refresh our context after renaming
                root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                if (root is null || semanticModel is null)
                    return document;
                    
                nodeToFix = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: false);
                if (nodeToFix is null)
                    return document;
                    
                if (!isTopLevelStatement)
                {
                    typeDeclaration = nodeToFix.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
                    if (typeDeclaration is null)
                        return document;
                }
                    
                operation = semanticModel.GetOperation(nodeToFix, cancellationToken);
                if (operation is null)
                    return document;
                    
                // Re-get the field/variable after renaming
                (fieldOrVariableToRemove, fieldOrVariableSymbol) = GetFieldOrVariableToRemove(operation, cancellationToken);
            }
        }
        
        // Add an annotation to track the field/variable through transformations
        var fieldOrVariableAnnotation = new SyntaxAnnotation();
        if (shouldRemoveFieldOrVariable && fieldOrVariableToRemove is not null)
        {
            root = root.ReplaceNode(fieldOrVariableToRemove, fieldOrVariableToRemove.WithAdditionalAnnotations(fieldOrVariableAnnotation));
            nodeToFix = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: false);
            if (nodeToFix is null)
                return document;

            if (!isTopLevelStatement)
            {
                typeDeclaration = nodeToFix.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
                if (typeDeclaration is null)
                    return document;
            }
        }

        // For top-level statements, handle differently
        if (isTopLevelStatement)
        {
            return await ConvertTopLevelStatementToSourceGenerator(document, root, nodeToFix, operation, properties, compilation, regexSymbol, regexGeneratorAttributeSymbol, methodName, fieldOrVariableAnnotation, shouldRemoveFieldOrVariable, usePartialProperty).ConfigureAwait(false);
        }

        // Regular class handling
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
        root = root.ReplaceNode(typeDeclaration, newTypeDeclaration);
        
        // Remove the field or variable declaration if we're using partial property
        if (shouldRemoveFieldOrVariable)
        {
            // Find the field/variable using the annotation
            var fieldOrVariableInUpdatedRoot = root.GetAnnotatedNodes(fieldOrVariableAnnotation).FirstOrDefault();
                
            if (fieldOrVariableInUpdatedRoot is not null)
            {
                var newRoot = RemoveFieldOrVariable(root, fieldOrVariableInUpdatedRoot);
                if (newRoot is not null)
                {
                    root = newRoot;
                }
            }
        }
        
        return document.WithSyntaxRoot(root);
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
        // Only suggest names from context for object creation (new Regex(...))
        // For static method calls like Regex.IsMatch, don't use the variable name
        var isObjectCreation = operation is IObjectCreationOperation;

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

            // Check for property initializer
            if (ancestor is IPropertyInitializerOperation propertyInitializer)
            {
                var property = propertyInitializer.InitializedProperties.FirstOrDefault();
                if (property is not null)
                {
                    var propertyName = property.Name;
                    // Only use the property name if it's meaningful (more than 1 character)
                    if (propertyName.Length > 1)
                    {
                        return propertyName;
                    }
                }
            }

            // Check for variable declarator (local variable)
            // Only use variable name if we're creating a Regex object, not calling a static method
            if (isObjectCreation && ancestor is IVariableDeclaratorOperation variableDeclarator)
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
        // Only remove fields/variables if they're initialized with new Regex(), not static method calls
        // Check the operation kind
        var isObjectCreation = operation is IObjectCreationOperation;
        
        // Walk up the operation tree to find if we're in a field or variable initializer
        foreach (var ancestor in operation.Ancestors())
        {
            // Check for field initializer
            if (ancestor is IFieldInitializerOperation fieldInitializer)
            {
                // Only return the field if it's initialized with new Regex()
                if (!isObjectCreation)
                    return (null, null);
                    
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

            // Check for property initializer
            if (ancestor is IPropertyInitializerOperation propertyInitializer)
            {
                // Only return the property if it's initialized with new Regex()
                if (!isObjectCreation)
                    return (null, null);

                var property = propertyInitializer.InitializedProperties.FirstOrDefault();
                if (property is not null)
                {
                    // Find the property declaration syntax
                    var propertySyntax = property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken);
                    if (propertySyntax is PropertyDeclarationSyntax propertyDeclaration)
                    {
                        return (propertyDeclaration, property);
                    }
                }
            }

            // Check for variable declarator (local variable)
            if (ancestor is IVariableDeclaratorOperation variableDeclaratorOp)
            {
                // Only return the variable if it's initialized with new Regex()
                if (!isObjectCreation)
                    return (null, null);
                    
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
        // Handle property declaration
        if (fieldOrVariableDeclarator is PropertyDeclarationSyntax propertyDeclaration)
        {
            return root.RemoveNode(propertyDeclaration, SyntaxRemoveOptions.KeepNoTrivia);
        }

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
                    // In top-level statements, local declarations are wrapped in GlobalStatementSyntax
                    if (localDeclaration.Parent is GlobalStatementSyntax globalStatement)
                    {
                        return root.RemoveNode(globalStatement, SyntaxRemoveOptions.KeepNoTrivia);
                    }

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

    private static Task<Document> ConvertTopLevelStatementToSourceGenerator(
        Document document,
        SyntaxNode root,
        SyntaxNode nodeToFix,
        IOperation operation,
        ImmutableDictionary<string, string?> properties,
        Compilation compilation,
        INamedTypeSymbol regexSymbol,
        INamedTypeSymbol regexGeneratorAttributeSymbol,
        string methodName,
        SyntaxAnnotation fieldOrVariableAnnotation,
        bool shouldRemoveFieldOrVariable,
        bool usePartialProperty)
    {
        var generator = SyntaxGenerator.GetGenerator(document);

        // Get the compilation unit
        var compilationUnit = root as CompilationUnitSyntax;
        if (compilationUnit is null)
            return Task.FromResult(document);

        // Replace the usage with the new regex member access
        SyntaxNode replacementNode;
        if (operation is IObjectCreationOperation)
        {
            if (usePartialProperty)
            {
                replacementNode = generator.IdentifierName(methodName);
            }
            else
            {
                replacementNode = generator.InvocationExpression(generator.IdentifierName(methodName));
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
                replacementNode = generator.InvocationExpression(generator.MemberAccessExpression(accessProperty, invocationOperation.TargetMethod.Name), [.. arguments.Select(arg => arg.Syntax)]);
            }
            else
            {
                var createRegexMethod = generator.InvocationExpression(generator.IdentifierName(methodName));
                replacementNode = generator.InvocationExpression(generator.MemberAccessExpression(createRegexMethod, invocationOperation.TargetMethod.Name), [.. arguments.Select(arg => arg.Syntax)]);
            }
        }
        else
        {
            return Task.FromResult(document);
        }

        root = root.ReplaceNode(nodeToFix, replacementNode);

        // Remove the field or variable declaration if we're using partial property
        if (shouldRemoveFieldOrVariable)
        {
            // Find the field/variable using the annotation
            var fieldOrVariableInUpdatedRoot = root.GetAnnotatedNodes(fieldOrVariableAnnotation).FirstOrDefault();

            if (fieldOrVariableInUpdatedRoot is not null)
            {
                var newRoot = RemoveFieldOrVariable(root, fieldOrVariableInUpdatedRoot);
                if (newRoot is not null)
                {
                    root = newRoot;
                }
            }
        }

        // Generate method or property
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
        else if (operation is IInvocationOperation invocationOperation2)
        {
            patternValue = GetNode(invocationOperation2.Arguments, properties, UseRegexSourceGeneratorAnalyzerCommon.PatternIndexName);
            regexOptionsValue = GetNode(invocationOperation2.Arguments, properties, UseRegexSourceGeneratorAnalyzerCommon.RegexOptionsIndexName);
        }

        if (timeoutValue is not null && regexOptionsValue is null)
        {
            regexOptionsValue = generator.MemberAccessExpression(generator.TypeExpression(compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.RegexOptions")!), "None");
        }

        // Generate the member
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

        // Check if a partial Program class already exists in the file
        compilationUnit = root as CompilationUnitSyntax;
        if (compilationUnit is null)
            return Task.FromResult(document);

        var existingProgramClass = compilationUnit.Members
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == "Program" && c.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));

        if (existingProgramClass is not null)
        {
            // Add the member to the existing Program class
            var newProgramClass = existingProgramClass.AddMembers((MemberDeclarationSyntax)newMember);
            root = root.ReplaceNode(existingProgramClass, newProgramClass);
        }
        else
        {
            // Create a new partial Program class
            var programClass = ClassDeclaration("Program")
                .WithModifiers(TokenList(Token(SyntaxKind.PartialKeyword)))
                .WithMembers(SingletonList((MemberDeclarationSyntax)newMember));

            // Add the Program class to the compilation unit
            compilationUnit = compilationUnit.AddMembers(programClass);
            root = compilationUnit;
        }

        return Task.FromResult(document.WithSyntaxRoot(root));
    }
}
