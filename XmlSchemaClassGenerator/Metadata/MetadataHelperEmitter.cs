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
    {
        if (_configuration.MetadataEmissionMode == MetadataEmissionMode.None)
            return;

        var existingNamespace = codeNamespaces.FirstOrDefault(ns => ns.Name == _configuration.MetadataNamespace);

        if (existingNamespace == null)
        {
            var metadataNamespace = GenerateMetadataNamespace(_configuration.MetadataNamespace);
            metadataNamespace.Types.Add(CreateFractionDigitsAttributeType());
            codeNamespaces.Add(metadataNamespace);
            return;
        }

        if (!ContainsType(existingNamespace, Attributes.FractionDigitsAttributeName))
            existingNamespace.Types.Add(CreateFractionDigitsAttributeType());
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
        var attribute = new CodeTypeDeclaration(Attributes.FractionDigitsAttributeName)
        {
            IsClass = true,
            TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed,
        };
        attribute.BaseTypes.Add(CodeUtilities.CreateTypeReference(typeof(Attribute), _configuration));

        attribute.CustomAttributes.Add(new CodeAttributeDeclaration(
            CodeUtilities.CreateTypeReference(typeof(AttributeUsageAttribute), _configuration),
            new CodeAttributeArgument(new CodeSnippetExpression("System.AttributeTargets.Property | System.AttributeTargets.Field")),
            new CodeAttributeArgument("AllowMultiple", new CodePrimitiveExpression(false))));

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
}
