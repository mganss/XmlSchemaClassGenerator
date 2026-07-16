using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace XmlSchemaClassGenerator.Metadata;

internal sealed class MetadataHelperEmitter
{
    private readonly GeneratorConfiguration _configuration;

    public MetadataHelperEmitter(GeneratorConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void EnsureFractionDigitsAttributeEmitted(ICollection<CodeNamespace> codeNamespaces)
        => EnsureAttributeEmitted(codeNamespaces, Attributes.FractionDigitsAttributeName, CreateFractionDigitsAttributeType);

    public void EnsureCollectionItemStringLengthAttributeEmitted(ICollection<CodeNamespace> codeNamespaces)
        => EnsureAttributeEmitted(codeNamespaces, Attributes.CollectionItemStringLengthAttributeName, CreateCollectionItemStringLengthAttributeType);

    private void EnsureAttributeEmitted(ICollection<CodeNamespace> codeNamespaces, string typeName, Func<CodeTypeDeclaration> createType)
    {
        if (!_configuration.EmitMetadataAttributes)
            return;

        var existingNamespace = codeNamespaces.FirstOrDefault(ns => ns.Name == _configuration.MetadataNamespace);

        if (existingNamespace == null)
        {
            var metadataNamespace = GenerateMetadataNamespace(_configuration.MetadataNamespace);
            metadataNamespace.Types.Add(createType());
            codeNamespaces.Add(metadataNamespace);
            return;
        }

        if (!ContainsType(existingNamespace, typeName))
            existingNamespace.Types.Add(createType());
    }

    private static bool ContainsType(CodeNamespace codeNamespace, string typeName)
        => codeNamespace.Types.OfType<CodeTypeDeclaration>().Any(t => t.Name == typeName);

    private CodeNamespace GenerateMetadataNamespace(string namespaceName)
    {
        var codeNamespace = new CodeNamespace(namespaceName);

        foreach (var (Namespace, _) in CodeUtilities.UsingNamespaces.Where(n => n.Condition(_configuration)).OrderBy(n => n.Namespace))
            codeNamespace.Imports.Add(new CodeNamespaceImport(Namespace));

        return codeNamespace;
    }

    private CodeTypeDeclaration CreateFractionDigitsAttributeType()
    {
        var attribute = CreateAttributeTypeShell(Attributes.FractionDigitsAttributeName);

        attribute.Members.Add(new CodeMemberField(typeof(int), "_fractionDigits")
        {
            Attributes = MemberAttributes.Private,
        });

        var constructor = new CodeConstructor
        {
            Attributes = MemberAttributes.Public,
        };
        constructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "fractionDigits"));
        constructor.Statements.Add(new CodeConditionStatement(
            new CodeBinaryOperatorExpression(new CodeArgumentReferenceExpression("fractionDigits"), CodeBinaryOperatorType.LessThan, new CodePrimitiveExpression(0)),
            new CodeThrowExceptionStatement(new CodeObjectCreateExpression(typeof(ArgumentOutOfRangeException), new CodePrimitiveExpression("fractionDigits")))));
        constructor.Statements.Add(new CodeAssignStatement(
            new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_fractionDigits"),
            new CodeArgumentReferenceExpression("fractionDigits")));
        attribute.Members.Add(constructor);

        var valueProperty = new CodeMemberProperty
        {
            Name = "FractionDigits",
            Type = CodeUtilities.CreateTypeReference(typeof(int), _configuration),
            Attributes = MemberAttributes.Public | MemberAttributes.Final,
            HasGet = true,
            HasSet = false,
        };
        valueProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_fractionDigits")));
        attribute.Members.Add(valueProperty);

        return attribute;
    }

    private CodeTypeDeclaration CreateCollectionItemStringLengthAttributeType()
    {
        var attribute = CreateAttributeTypeShell(Attributes.CollectionItemStringLengthAttributeName);

        attribute.Members.Add(new CodeMemberField(typeof(int), "_maximumLength")
        {
            Attributes = MemberAttributes.Private,
        });
        attribute.Members.Add(new CodeMemberField(typeof(int), "_minimumLength")
        {
            Attributes = MemberAttributes.Private,
        });

        var constructor = new CodeConstructor
        {
            Attributes = MemberAttributes.Public,
        };
        constructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "maximumLength"));
        constructor.Statements.Add(new CodeConditionStatement(
            new CodeBinaryOperatorExpression(new CodeArgumentReferenceExpression("maximumLength"), CodeBinaryOperatorType.LessThan, new CodePrimitiveExpression(0)),
            new CodeThrowExceptionStatement(new CodeObjectCreateExpression(typeof(ArgumentOutOfRangeException), new CodePrimitiveExpression("maximumLength")))));
        constructor.Statements.Add(new CodeAssignStatement(
            new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_maximumLength"),
            new CodeArgumentReferenceExpression("maximumLength")));
        attribute.Members.Add(constructor);

        var maximumLengthProperty = new CodeMemberProperty
        {
            Name = "MaximumLength",
            Type = CodeUtilities.CreateTypeReference(typeof(int), _configuration),
            Attributes = MemberAttributes.Public | MemberAttributes.Final,
            HasGet = true,
            HasSet = false,
        };
        maximumLengthProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_maximumLength")));
        attribute.Members.Add(maximumLengthProperty);

        var minimumLengthProperty = new CodeMemberProperty
        {
            Name = "MinimumLength",
            Type = CodeUtilities.CreateTypeReference(typeof(int), _configuration),
            Attributes = MemberAttributes.Public | MemberAttributes.Final,
            HasGet = true,
            HasSet = true,
        };
        minimumLengthProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_minimumLength")));
        minimumLengthProperty.SetStatements.Add(new CodeAssignStatement(
            new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_minimumLength"),
            new CodePropertySetValueReferenceExpression()));
        attribute.Members.Add(minimumLengthProperty);

        return attribute;
    }

    private CodeTypeDeclaration CreateAttributeTypeShell(string typeName)
    {
        var attribute = new CodeTypeDeclaration(typeName)
        {
            IsClass = true,
            TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed,
        };
        attribute.BaseTypes.Add(CodeUtilities.CreateTypeReference(typeof(Attribute), _configuration));

        attribute.CustomAttributes.Add(new CodeAttributeDeclaration(
            CodeUtilities.CreateTypeReference(typeof(AttributeUsageAttribute), _configuration),
            new CodeAttributeArgument(new CodeSnippetExpression("System.AttributeTargets.Property | System.AttributeTargets.Field")),
            new CodeAttributeArgument("AllowMultiple", new CodePrimitiveExpression(false))));

        return attribute;
    }
}
