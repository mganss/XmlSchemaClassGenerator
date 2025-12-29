using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace XmlSchemaClassGenerator;

public class GeneratorModel
{
    protected const string OnPropertyChanged = nameof(OnPropertyChanged);
    protected const string EqualsMethod = nameof(object.Equals);
    protected const string HasValue = nameof(Nullable<int>.HasValue);

    protected const string English = "en";
    protected const string German = "de";

    protected GeneratorModel(GeneratorConfiguration configuration) => Configuration = configuration;

    public GeneratorConfiguration Configuration { get; }

    protected CodeTypeReferenceExpression TypeRefExpr<T>() => new(TypeRef<T>());

    protected CodeAttributeDeclaration AttributeDecl<T>(params CodeAttributeArgument[] args) => new(TypeRef<T>(), args);

    private protected CodeAttributeDeclaration AttributeDecl(TypeInfo attribute, CodeAttributeArgument arg)
        => new(CodeUtilities.CreateTypeReference(attribute, Configuration), arg);

    protected CodeTypeReference TypeRef<T>() => CodeUtilities.CreateTypeReference(typeof(T), Configuration);

    protected CodeTypeReference NullableTypeRef(CodeTypeReference typeReference)
    {
        var nullableType = CodeUtilities.CreateTypeReference(typeof(Nullable<>), Configuration);
        nullableType.TypeArguments.Add(typeReference);
        return nullableType;
    }
    public static bool DisableComments { get; set; }

    protected IEnumerable<CodeCommentStatement> GetComments(IReadOnlyList<DocumentationModel> docs)
    {
        if (DisableComments || docs.Count == 0)
            yield break;

        yield return new CodeCommentStatement("<summary>", true);

        foreach (var doc in docs.Where
                (
                    d => !string.IsNullOrWhiteSpace(d.Text)
                         && (string.IsNullOrEmpty(d.Language)
                             || Configuration.CommentLanguages.Count is 0
                             || Configuration.CommentLanguages.Contains(d.Language)
                             || Configuration.CommentLanguages
                                 .Any(l => d.Language.StartsWith(l, StringComparison.OrdinalIgnoreCase)))
                )
                .OrderBy(d => d.Language))
        {
            var text = doc.Text;
            var comment = $"<para{(string.IsNullOrEmpty(doc.Language) ? "" : $@" xml:lang=""{doc.Language}""")}>{CodeUtilities.NormalizeNewlines(text).Trim()}</para>";

            yield return new(comment, true);
        }

        yield return new CodeCommentStatement("</summary>", true);
    }

    protected void AddDescription(CodeAttributeDeclarationCollection attributes, IReadOnlyList<DocumentationModel> docs)
    {
        if (!Configuration.GenerateDescriptionAttribute || DisableComments || docs.Count is 0) return;

        var docText = GetSingleDoc(docs);

        if (!string.IsNullOrWhiteSpace(docText))
        {
            var descriptionAttribute = AttributeDecl<DescriptionAttribute>(new CodeAttributeArgument(new CodePrimitiveExpression(Regex.Replace(docText, @"\s+", " ").Trim())));
            attributes.Add(descriptionAttribute);
        }
    }

    private string GetSingleDoc(IReadOnlyList<DocumentationModel> docs) => string.Join
    (
            " ",
            docs.Where
                (
                    d => !string.IsNullOrWhiteSpace(d.Text)
                         && (string.IsNullOrEmpty(d.Language)
                             || Configuration.CommentLanguages.Count is 0
                             || Configuration.CommentLanguages.Contains(d.Language)
                             || Configuration.CommentLanguages
                                 .Any(l => d.Language.StartsWith(l, StringComparison.OrdinalIgnoreCase)))
                )
                .Select(x => x.Text)
    );
}
