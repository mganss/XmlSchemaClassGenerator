using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace XmlSchemaClassGenerator;

public class EnumModel(GeneratorConfiguration configuration) : TypeModel(configuration)
{
    public List<EnumValueModel> Values { get; set; } = [];

    public override CodeTypeDeclaration Generate()
    {
        var enumDeclaration = base.Generate();

        GenerateSerializableAttribute(enumDeclaration);
        GenerateTypeAttribute(enumDeclaration);

        enumDeclaration.IsEnum = true;
        if (Configuration.AssemblyVisible)
        {
            enumDeclaration.TypeAttributes = (enumDeclaration.TypeAttributes & ~System.Reflection.TypeAttributes.VisibilityMask) | System.Reflection.TypeAttributes.NestedAssembly;
        }

        foreach (var val in Values)
        {
            var member = new CodeMemberField { Name = val.Name };
            var docs = new List<DocumentationModel>(val.Documentation);

            AddDescription(member.CustomAttributes, docs);

            if (val.Name != val.Value) // illegal identifier chars in value
            {
                var enumAttribute = AttributeDecl<XmlEnumAttribute>(new CodeAttributeArgument(new CodePrimitiveExpression(val.Value)));
                member.CustomAttributes.Add(enumAttribute);
            }

            if (val.IsDeprecated)
            {
                // From .NET 3.5 XmlSerializer doesn't serialize objects with [Obsolete] >(

                DocumentationModel obsolete = new() { Language = English, Text = "[Obsolete]" };
                docs.Add(obsolete);
            }

            member.Comments.AddRange(GetComments(docs).ToArray());

            enumDeclaration.Members.Add(member);
        }

        if (RootElementName != null)
        {
            var rootAttribute = AttributeDecl<XmlRootAttribute>(
                new(new CodePrimitiveExpression(RootElementName.Name)),
                new(nameof(XmlRootAttribute.Namespace), new CodePrimitiveExpression(RootElementName.Namespace)));
            enumDeclaration.CustomAttributes.Add(rootAttribute);
        }
        Configuration.TypeVisitor(enumDeclaration, this);
        return enumDeclaration;
    }

    public override CodeExpression GetDefaultValueFor(string defaultString, bool attribute)
    {
        return new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(GetReferenceFor(referencingNamespace: null)),
            Values.First(v => v.Value == defaultString).Name);
    }
}
