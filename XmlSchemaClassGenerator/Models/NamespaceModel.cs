using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

namespace XmlSchemaClassGenerator;

public class NamespaceModel(NamespaceKey key, GeneratorConfiguration configuration) : GeneratorModel(configuration)
{
    public string Name { get; set; }
    public NamespaceKey Key { get; } = key;
    public Dictionary<string, TypeModel> Types { get; set; } = [];
    /// <summary>
    /// Does the namespace of this type clashes with a class in the same or upper namespace?
    /// </summary>
    public bool IsAmbiguous { get; set; }

    public static CodeNamespace Generate(string namespaceName, IEnumerable<NamespaceModel> parts, GeneratorConfiguration conf)
    {
        var codeNamespace = new CodeNamespace(namespaceName);

        foreach (var (Namespace, _) in CodeUtilities.UsingNamespaces.Where(n => n.Condition(conf)).OrderBy(n => n.Namespace))
            codeNamespace.Imports.Add(new CodeNamespaceImport(Namespace));

        foreach (var typeModel in parts.SelectMany(x => x.Types.Values).ToList())
        {
            var type = typeModel.Generate();
            if (type != null)
            {
                codeNamespace.Types.Add(type);
            }
        }

        return codeNamespace;
    }
}
