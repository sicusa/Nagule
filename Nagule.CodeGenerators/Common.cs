namespace Nagule.CodeGenerators;

using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class Common
{
    public static readonly AssemblyName AssemblyName = typeof(Common).Assembly.GetName();
    public static readonly string GeneratedCodeAttribute =
        $@"global::System.CodeDom.Compiler.GeneratedCodeAttribute(""{AssemblyName.Name}"", ""{AssemblyName.Version}"")";
    
    public static ImmutableArray<TypeDeclarationSyntax> GetParentTypes(SyntaxNode node)
    {
        var builder = ImmutableArray.CreateBuilder<TypeDeclarationSyntax>();
        var parent = node.Parent;

        while (parent != null) {
            if (parent is TypeDeclarationSyntax typeDecl) {
                builder.Add(typeDecl);
            }
            parent = parent.Parent;
        }

        return builder.ToImmutable();
    }

    public static ITypeSymbol GetNodeType(SemanticModel model, SyntaxNode typeNode, CancellationToken token)
        => model.GetTypeInfo(typeNode, token).Type!;
    
    public static ITypeSymbol GetVariableType(SemanticModel model, VariableDeclaratorSyntax syntax, CancellationToken token) {
        var parentDecl = (VariableDeclarationSyntax)syntax.Parent!;
        return GetNodeType(model, parentDecl.Type, token);
    }

    public static IndentedTextWriter CreateSource(out StringBuilder builder)
    {
        builder = new StringBuilder();
        var writer = new StringWriter(builder, CultureInfo.InvariantCulture);
        var source = new IndentedTextWriter(writer, "    ");

        source.WriteLine("// <auto-generated/>");
        source.WriteLine("#nullable enable");
        source.WriteLine();

        return source;
    }
    
    private class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();

        public void Dispose() {}
    }

    private class EnclosingDisposable(IndentedTextWriter source, int count) : IDisposable
    {
        private readonly IndentedTextWriter _source = source;
        private readonly int _count = count;

        public void Dispose()
        {
            for (int i = 0; i < _count; ++i) {
                _source.Indent--;
                _source.WriteLine("}");
            }
        }
    }

    public static IDisposable GenerateInNamespace(IndentedTextWriter source, INamespaceSymbol ns)
    {
        var hasNamespace = !ns.IsGlobalNamespace;
        if (hasNamespace) {
            source.Write("namespace ");
            source.WriteLine(ns.ToDisplayString());
            source.WriteLine("{");
            source.Indent++;
            return new EnclosingDisposable(source, 1);
        }
        else {
            return EmptyDisposable.Instance;
        }
    }

    public static IDisposable GenerateInPartialTypes(IndentedTextWriter source, IEnumerable<TypeDeclarationSyntax> typeDecls)
    {
        int indent = 0;
        foreach (var typeDecl in typeDecls) {
            if (typeDecl.Modifiers.Any(SyntaxKind.StaticKeyword)) {
                source.Write("static ");
            }
            switch (typeDecl.Kind()) {
                case SyntaxKind.ClassDeclaration:
                    source.Write("partial class ");
                    break;
                case SyntaxKind.StructDeclaration:
                    source.Write("partial struct ");
                    break;
                case SyntaxKind.RecordDeclaration:
                    source.Write("partial record ");
                    break;
                case SyntaxKind.RecordStructDeclaration:
                    source.Write("partial record struct ");
                    break;
                default:
                    throw new InvalidDataException("Invalid containing type");
            }
            
            WriteType(source, typeDecl);

            source.WriteLine();
            source.WriteLine("{");
            source.Indent++;
            indent++;
        }
        return indent != 0 ? new EnclosingDisposable(source, indent) : EmptyDisposable.Instance;
    }


    public static void WriteType(IndentedTextWriter source, TypeDeclarationSyntax typeDecl)
    {
        source.Write(typeDecl.Identifier.ToString());
        WriteTypeParameters(source, typeDecl);
    }

    public static void WriteTypeParameters(IndentedTextWriter source, TypeDeclarationSyntax typeDecl)
    {
        var typeParams = typeDecl.TypeParameterList;
        if (typeParams != null) {
            WriteTypeParameters(source, typeParams);
        }
    }

    public static void WriteTypeParameters(IndentedTextWriter source, TypeParameterListSyntax typeParams)
    {
        source.Write('<');
        var paramsList = typeParams.Parameters;
        var lastIndex = paramsList.Count - 1;
        for (int i = 0; i != paramsList.Count; ++i) {
            source.Write(paramsList[i].Identifier.ToString());
            if (i != lastIndex) {
                source.Write(", ");
            }
        }
        source.Write('>');
    }

    public static string? FindAssetComponentName(ITypeSymbol templateType)
    {
        foreach (var attr in templateType.GetAttributes()) {
            var attrClass = attr.AttributeClass;
            if (attrClass == null || attrClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    != "global::Sia.SiaTemplateAttribute") {
                continue;
            }
            if (attr.ConstructorArguments[0].Value is not string componentType) {
                continue;
            }
            return componentType;
        }
        return null;
    }
}