using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Exposes the properties of the Roslyn interfaces an object implements as attributes, named after the property,
/// such as <c>IsImplicit</c> or <c>TargetMethod</c>. The objects are implemented by internal types, so the properties
/// are the ones of their public interfaces. It is how the operations and the symbols expose their data.
/// </summary>
internal sealed class XPathPropertyAttributes
{
    private readonly ConcurrentDictionary<Type, ReflectedProperty[]> _properties = new();
    private readonly Type _rootInterface;
    private readonly HashSet<string> _ignoredProperties;

    /// <param name="rootInterface">The interface all the exposed interfaces derive from, such as <see cref="IOperation"/>.</param>
    /// <param name="ignoredProperties">The properties that are not attributes, such as the ones that are the child elements.</param>
    public XPathPropertyAttributes(Type rootInterface, HashSet<string> ignoredProperties)
    {
        _rootInterface = rootInterface;
        _ignoredProperties = ignoredProperties;
    }

    /// <summary>
    /// Adds the attributes of the properties of <paramref name="instance"/> that a query can select.
    /// </summary>
    public void Add(in XPathAttributeWriter writer, TextSpan span, object instance)
    {
        foreach (var property in GetProperties(instance.GetType()))
        {
            // Reading the property is a reflection call, so it is only done when a query can select one of the
            // attributes it produces
            if (!writer.IncludesAny(property.AttributeNames))
                continue;

            object? value;
            try
            {
                value = property.Property.GetValue(instance);
            }
            catch (TargetInvocationException)
            {
                continue;
            }

            if (value is null)
                continue;

            switch (property.Kind)
            {
                case PropertyKind.Type:
                    XPathAttributeFormatter.AddType(writer, span, (ITypeSymbol)value, property.TypeNames!);
                    break;

                case PropertyKind.Symbol:
                    XPathAttributeFormatter.AddSymbol(writer, span, (ISymbol)value, property.SymbolNames!);
                    break;

                case PropertyKind.Symbols:
                    AddSymbols(writer, span, value, property.SymbolNames!);
                    break;

                case PropertyKind.Types:
                    AddTypes(writer, span, value, property.TypeNames!);
                    break;

                case PropertyKind.Conversion:
                    XPathAttributeFormatter.AddConversion(writer, span, (CommonConversion)value, property.ConversionNames!);
                    break;

                case PropertyKind.Boolean:
                    writer.Add(span, property.Name, XPathAttributeFormatter.ToXPathBoolean((bool)value));
                    break;

                case PropertyKind.Accessibility:
                    writer.Add(span, property.Name, XPathAttributeFormatter.GetAccessibilityName((Accessibility)value));
                    break;

                case PropertyKind.RefKind:
                    writer.Add(span, property.Name, XPathAttributeFormatter.GetRefKindName((RefKind)value));
                    break;

                case PropertyKind.Enumeration:
                    writer.Add(span, property.Name, value.ToString());
                    break;

                case PropertyKind.String:
                    writer.Add(span, property.Name, (string)value);
                    break;

                case PropertyKind.Int32:
                    writer.Add(span, property.Name, ((int)value).ToString(CultureInfo.InvariantCulture));
                    break;

                case PropertyKind.Object:
                    writer.Add(span, property.Name, XPathAttributeFormatter.FormatConstantValue(value));
                    break;

                case PropertyKind.Constant:
                    var constantValue = (Optional<object>)value;
                    if (constantValue.HasValue)
                    {
                        writer.Add(span, property.HasValueName!, "true");
                        writer.Add(span, property.Name, XPathAttributeFormatter.FormatConstantValue(constantValue.Value));
                    }

                    break;
            }
        }
    }

    // The symbols of a property that returns several of them are joined by a space, like the tokens of a SyntaxTokenList
    private static void AddSymbols(in XPathAttributeWriter writer, TextSpan span, object value, SymbolAttributeNames names)
    {
        var includeQualifiedNames = writer.Includes(names.QualifiedName);
        var includeShortNames = writer.Includes(names.Name);
        var includeDocumentationIds = writer.Includes(names.DocumentationId);
        if (!includeQualifiedNames && !includeShortNames && !includeDocumentationIds)
            return;

        var qualifiedNames = new List<string>();
        var shortNames = new List<string>();
        var documentationIds = new List<string>();
        try
        {
            foreach (var item in (System.Collections.IEnumerable)value)
            {
                if (item is not ISymbol symbol)
                    continue;

                if (includeQualifiedNames && SymbolNameFormatter.GetSymbolName(symbol) is { Length: > 0 } qualifiedName)
                {
                    qualifiedNames.Add(qualifiedName);
                }

                if (includeShortNames && symbol.Name is { Length: > 0 } name)
                {
                    shortNames.Add(name);
                }

                if (includeDocumentationIds && SymbolNameFormatter.GetDocumentationId(symbol) is { Length: > 0 } documentationId)
                {
                    documentationIds.Add(documentationId);
                }
            }
        }
        catch (InvalidOperationException)
        {
            // A default ImmutableArray cannot be enumerated
            return;
        }

        writer.Add(span, names.QualifiedName, string.Join(" ", qualifiedNames));
        writer.Add(span, names.Name, string.Join(" ", shortNames));
        writer.Add(span, names.DocumentationId, string.Join(" ", documentationIds));
    }

    // The types of a property that returns several of them are joined by a space, like the symbols that are not types.
    // Only the names are joined, as a boolean or the name of a member of an enumeration is not meaningful for a list.
    private static void AddTypes(in XPathAttributeWriter writer, TextSpan span, object value, TypeAttributeNames names)
    {
        var includeNames = writer.Includes(names.Name);
        var includeMetadataNames = writer.Includes(names.MetadataName);
        var includeDocumentationIds = writer.Includes(names.DocumentationId);
        var includeReferenceIds = writer.Includes(names.ReferenceId);
        if (!includeNames && !includeMetadataNames && !includeDocumentationIds && !includeReferenceIds)
            return;

        var shortNames = new List<string>();
        var metadataNames = new List<string>();
        var documentationIds = new List<string>();
        var referenceIds = new List<string>();
        try
        {
            foreach (var item in (System.Collections.IEnumerable)value)
            {
                if (item is not ITypeSymbol type)
                    continue;

                if (includeNames && type.Name is { Length: > 0 } name)
                {
                    shortNames.Add(name);
                }

                if (includeMetadataNames && SymbolNameFormatter.GetMetadataName(type) is { Length: > 0 } metadataName)
                {
                    metadataNames.Add(metadataName);
                }

                if (includeDocumentationIds && SymbolNameFormatter.GetDocumentationId(type) is { Length: > 0 } documentationId)
                {
                    documentationIds.Add(documentationId);
                }

                if (includeReferenceIds && SymbolNameFormatter.GetReferenceId(type) is { Length: > 0 } referenceId)
                {
                    referenceIds.Add(referenceId);
                }
            }
        }
        catch (InvalidOperationException)
        {
            // A default ImmutableArray cannot be enumerated
            return;
        }

        writer.Add(span, names.Name, string.Join(" ", shortNames));
        writer.Add(span, names.MetadataName, string.Join(" ", metadataNames));
        writer.Add(span, names.DocumentationId, string.Join(" ", documentationIds));
        writer.Add(span, names.ReferenceId, string.Join(" ", referenceIds));
    }

    private ReflectedProperty[] GetProperties(Type type)
    {
        return _properties.GetOrAdd(type, type =>
        {
            // The objects are implemented by internal types, so the properties are the ones of their interfaces. The
            // interfaces overlap, and a few of them redeclare a property, so they are deduplicated by name.
            var properties = new Dictionary<string, ReflectedProperty>(StringComparer.Ordinal);
            foreach (var contract in type.GetInterfaces())
            {
                if (!_rootInterface.IsAssignableFrom(contract))
                    continue;

                if (contract.Namespace is not ("Microsoft.CodeAnalysis" or "Microsoft.CodeAnalysis.Operations"))
                    continue;

                foreach (var property in contract.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.GetIndexParameters().Length is not 0)
                        continue;

                    if (property.GetCustomAttribute<ObsoleteAttribute>() is not null)
                        continue;

                    if (_ignoredProperties.Contains(property.Name) || properties.ContainsKey(property.Name))
                        continue;

                    if (CreateProperty(property) is { } reflectedProperty)
                    {
                        properties.Add(property.Name, reflectedProperty);
                    }
                }
            }

            return [.. properties.Values.OrderBy(property => property.Name, StringComparer.Ordinal)];
        });
    }

    // The names of the attributes are computed once, so nothing is concatenated when an object is visited
    private static ReflectedProperty? CreateProperty(PropertyInfo property)
    {
        var name = property.Name;
        var type = property.PropertyType;

        if (typeof(ITypeSymbol).IsAssignableFrom(type))
        {
            var names = CreateTypeNames(name);
            return new ReflectedProperty(property, PropertyKind.Type, name, names.All, TypeNames: names);
        }

        if (typeof(ISymbol).IsAssignableFrom(type))
        {
            var names = CreateSymbolNames(name);
            return new ReflectedProperty(property, PropertyKind.Symbol, name, names.All, SymbolNames: names);
        }

        if (type == typeof(Optional<object>))
        {
            var hasValueName = "Has" + name;
            return new ReflectedProperty(property, PropertyKind.Constant, name, [name, hasValueName], HasValueName: hasValueName);
        }

        if (type == typeof(CommonConversion))
        {
            var names = CreateConversionNames(name);
            return new ReflectedProperty(property, PropertyKind.Conversion, name, names.All, ConversionNames: names);
        }

        // The types are also symbols, so they are tested first to expose the names a type has
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>) && typeof(ITypeSymbol).IsAssignableFrom(type.GetGenericArguments()[0]))
        {
            var names = CreateTypeNames(name);
            return new ReflectedProperty(property, PropertyKind.Types, name, names.List, TypeNames: names);
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>) && typeof(ISymbol).IsAssignableFrom(type.GetGenericArguments()[0]))
        {
            var names = CreateSymbolNames(name);
            return new ReflectedProperty(property, PropertyKind.Symbols, name, names.List, SymbolNames: names);
        }

        if (type == typeof(bool))
            return new ReflectedProperty(property, PropertyKind.Boolean, name, [name]);

        if (type == typeof(string))
            return new ReflectedProperty(property, PropertyKind.String, name, [name]);

        if (type == typeof(int))
            return new ReflectedProperty(property, PropertyKind.Int32, name, [name]);

        // The accessibility is not exposed when it does not apply, such as for a parameter or a local
        if (type == typeof(Accessibility))
            return new ReflectedProperty(property, PropertyKind.Accessibility, name, [name]);

        // RefKind has aliases, so its name is not left to Enum.ToString
        if (type == typeof(RefKind))
            return new ReflectedProperty(property, PropertyKind.RefKind, name, [name]);

        if (type.IsEnum)
            return new ReflectedProperty(property, PropertyKind.Enumeration, name, [name]);

        // A value typed 'object' is a constant, such as the value of a constant field or the default value of a parameter
        if (type == typeof(object))
            return new ReflectedProperty(property, PropertyKind.Object, name, [name]);

        // The other types, such as IOperation, are the child elements or are not exposed
        return null;
    }

    internal static TypeAttributeNames CreateTypeNames(string name)
        => new(name + "Name", name + "MetadataName", name + "DocumentationId", name + "ReferenceId", name + "Kind", name + "IsValueType", name + "NullableAnnotation", name + "SpecialType");

    internal static SymbolAttributeNames CreateSymbolNames(string name)
        => new(name, name + "Name", name + "DocumentationId", name + "Kind", name + "IsStatic", name + "IsAbstract", name + "IsVirtual", name + "IsOverride", name + "IsSealed", name + "IsAsync", name + "IsExtensionMethod", name + "Arity", name + "RefKind", name + "IsParams", name + "IsOptional", name + "IsConst", name + "IsReadOnly");

    private static ConversionAttributeNames CreateConversionNames(string name)
        => new(name + "Exists", name + "IsIdentity", name + "IsImplicit", name + "IsNullable", name + "IsNumeric", name + "IsReference", name + "IsUserDefined", CreateSymbolNames(name + "Method"));

    private enum PropertyKind
    {
        Type,
        Types,
        Symbol,
        Symbols,
        Conversion,
        Boolean,
        Accessibility,
        RefKind,
        Enumeration,
        String,
        Int32,
        Object,
        Constant,
    }

    /// <summary>
    /// A property and the names of the attributes it produces. A property that produces a symbol, a type or a
    /// conversion produces one attribute per format of its name and per member it exposes.
    /// </summary>
    private sealed record ReflectedProperty(PropertyInfo Property, PropertyKind Kind, string Name, string[] AttributeNames, TypeAttributeNames? TypeNames = null, SymbolAttributeNames? SymbolNames = null, ConversionAttributeNames? ConversionNames = null, string? HasValueName = null);
}
