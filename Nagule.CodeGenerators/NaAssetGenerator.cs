﻿namespace Nagule.CodeGenerators;

using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Common;

[Generator]
internal partial class NaAssetGenerator : IIncrementalGenerator
{
    public static readonly string AttributeName = "NaAssetAttribute";
    public static readonly string AttributeType = $"Nagule.{AttributeName}";
    private static readonly string ttributeSource = $$"""
// <auto-generated/>
#nullable enable

namespace Nagule;

[{{Common.GeneratedCodeAttribute}}]
[global::System.AttributeUsage(
    global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct,
    Inherited = false, AllowMultiple = false)]
internal sealed class {{AttributeName}} : global::System.Attribute
{
}
""";

    protected record CodeGenerationInfo(
        INamespaceSymbol Namespace,
        ImmutableArray<TypeDeclarationSyntax> ParentTypes,
        TypeDeclarationSyntax AssetTypeSyntax,
        string ComponentType);
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(context => {
            context.AddSource(AttributeName + ".g.cs",
                SourceText.From(ttributeSource, Encoding.UTF8));
        });

        var codeGenInfos = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeType,
            static (syntaxNode, token) => true,
            static (syntax, token) =>
                (syntax, ParentTypes: GetParentTypes(syntax.TargetNode)))
            .Where(static t => t.ParentTypes.All(
                static typeDecl => typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword)))
            .Select(static (t, token) => {
                var (syntax, parentTypes) = t;
                var model = syntax.SemanticModel;
                var assetAttr = syntax.Attributes[0];

                var targetTypeSyntax = (TypeDeclarationSyntax)syntax.TargetNode;
                var targetTypeSymbol = (ITypeSymbol)model.GetDeclaredSymbol(targetTypeSyntax)!;

                return new CodeGenerationInfo(
                    Namespace: syntax.TargetSymbol.ContainingNamespace,
                    ParentTypes: parentTypes,
                    AssetTypeSyntax: targetTypeSyntax,
                    ComponentType: FindAssetComponentName(targetTypeSymbol)!
                );
            })
            .Where(static info => info.ComponentType != null);
        
        context.RegisterSourceOutput(codeGenInfos, (context, info) => {
            using var source = CreateSource(out var builder);
            GenerateSource(source, info);
            context.AddSource(GenerateFileName(info), builder.ToString());
        });
    }

    private static string GenerateFileName(CodeGenerationInfo info)
    {
        var builder = new StringBuilder();
        builder.Append(info.Namespace.ToDisplayString());
        builder.Append('.');
        foreach (var parentType in info.ParentTypes) {
            builder.Append(parentType.Identifier.ToString());
            builder.Append('.');
        }
        builder.Append(info.AssetTypeSyntax.Identifier.ToString());
        builder.Append(".g.cs");
        return builder.ToString();
    }

    private void GenerateSource(IndentedTextWriter source, CodeGenerationInfo info)
    {
        source.WriteLine("using Sia;");
        source.WriteLine();

        using (GenerateInNamespace(source, info.Namespace)) {
            using (GenerateInPartialTypes(source, info.ParentTypes)) {
                source.Write("partial record struct ");
                source.Write(info.ComponentType);
                WriteTypeParameters(source, info.AssetTypeSyntax);
                source.Write(" : global::Nagule.IAsset<");
                WriteType(source, info.AssetTypeSyntax);
                source.WriteLine('>');

                source.WriteLine("{");
                source.Indent++;
                GenerateStaticConstructor(source, info);
                source.WriteLine();
                GenerateCreateEntityMethods(source, info);
                source.Indent--;
                source.WriteLine("}");
            }
        }
    }

    private void GenerateStaticConstructor(IndentedTextWriter source, CodeGenerationInfo info)
    {
        source.Write("static ");
        source.Write(info.ComponentType);
        source.WriteLine("()");

        source.WriteLine("{");
        source.Indent++;

        source.Write("global::Nagule.AssetLibrary.RegisterAsset<");
        source.Write(info.ComponentType);
        WriteTypeParameters(source, info.AssetTypeSyntax);
        source.Write(", ");
        WriteType(source, info.AssetTypeSyntax);
        source.WriteLine(">();");

        source.Indent--;
        source.WriteLine("}");
    }

    private void GenerateCreateEntityMethods(IndentedTextWriter source, CodeGenerationInfo info)
    {
        source.Write("public static global::Sia.EntityRef CreateEntity(global::Sia.World world, ");
        WriteType(source, info.AssetTypeSyntax);
        source.WriteLine(" record, AssetLife life = AssetLife.Automatic)");
        source.WriteLine('{');

        source.Indent++;
        source.WriteLine("Construct(record, out var result);");

        source.Write("return world.CreateInBucketHost(global::Sia.Bundle.Create(");
        GenerateEntityComponents(source, info);
        source.WriteLine("));");

        source.Indent--;
        source.WriteLine('}');

        source.WriteLine();

        source.Write("public static global::Sia.EntityRef CreateEntity<TComponentBundle>(global::Sia.World world, ");
        WriteType(source, info.AssetTypeSyntax);
        source.WriteLine(" record, in TComponentBundle bundle, AssetLife life = AssetLife.Automatic)");
        source.Indent++;
        source.WriteLine("where TComponentBundle : struct, IComponentBundle");
        source.Indent--;
        source.WriteLine('{');

        source.Indent++;
        source.WriteLine("Construct(record, out var result);");

        source.Write("return world.CreateInBucketHost(global::Sia.Bundle.Create(");
        GenerateEntityComponents(source, info);
        source.WriteLine(", bundle));");

        source.Indent--;
        source.WriteLine('}');

        source.WriteLine();

        source.Write("public static global::Sia.EntityRef CreateEntity(global::Sia.World world, ");
        WriteType(source, info.AssetTypeSyntax);
        source.WriteLine(" record, global::Sia.EntityRef referrer, AssetLife life = AssetLife.Automatic)");
        source.WriteLine('{');

        source.Indent++;
        source.WriteLine("var entity = CreateEntity(world, record, life);");
        source.WriteLine("referrer.Modify(new global::Nagule.AssetMetadata.Refer(entity));");
        source.WriteLine("return entity;");
        source.Indent--;
        source.WriteLine('}');
    }

    private static readonly string DefaultEntityComponentsSource =
        "global::Nagule.AssetBundle.Create(result, life), " +
        "global::Sia.Sid.From<IAssetRecord>(record), " +
        "global::Sia.Sid.From(record.Id ?? Guid.Empty), " +
        "global::Sia.Sid.From(new Name(record.Name ?? \"\"))";

    protected virtual void GenerateEntityComponents(IndentedTextWriter source, CodeGenerationInfo info)
        => source.Write(DefaultEntityComponentsSource);
}