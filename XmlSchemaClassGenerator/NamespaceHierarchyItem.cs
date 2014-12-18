using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlSchemaClassGenerator
{
    public class NamespaceHierarchyItem
    {
        private readonly List<NamespaceHierarchyItem> InternalSubNamespaces = new List<NamespaceHierarchyItem>();
        private readonly List<NamespaceModel> InternalNamespaceModels = new List<NamespaceModel>();
        private readonly List<TypeModel> InternalTypeModels = new List<TypeModel>();

        private NamespaceHierarchyItem(string name, string fullName)
        {
            Name = name;
            FullName = fullName;
        }

        public string Name { get; private set; }
        public string FullName { get; private set; }
        public IEnumerable<NamespaceModel> Models { get { return InternalNamespaceModels; } }
        public IEnumerable<NamespaceHierarchyItem> SubNamespaces { get { return InternalSubNamespaces; } }
        public IEnumerable<TypeModel> TypeModels { get { return InternalTypeModels; } }

        public static IEnumerable<NamespaceHierarchyItem> Build(IEnumerable<NamespaceModel> namespaceModels)
        {
            var rootItem = new NamespaceHierarchyItem(null, null);
            var activeParts = new List<NamespaceHierarchyItem>();
            foreach (var namespaceModel in namespaceModels.OrderBy(x => x.Name))
            {
                var parts = namespaceModel.Name.Split('.');
                var prevItem = rootItem;
                for (var i = 0; i != parts.Length; ++i)
                {
                    var name = parts[i];
                    var fullName = string.Join(".", parts.Take(i + 1));
                    bool createNewItem;
                    if (activeParts.Count == i)
                    {
                        createNewItem = true;
                    }
                    else if (name != activeParts[i].Name)
                    {
                        activeParts.RemoveRange(i, activeParts.Count - i);
                        createNewItem = true;
                    }
                    else
                        createNewItem = false;
                    if (createNewItem)
                    {
                        var newItem = new NamespaceHierarchyItem(name, fullName);
                        prevItem.InternalSubNamespaces.Add(newItem);
                        activeParts.Add(newItem);
                        prevItem = newItem;
                    }
                    else
                    {
                        prevItem = activeParts[i];
                    }
                    if (i == parts.Length - 1)
                    {
                        prevItem.InternalNamespaceModels.Add(namespaceModel);
                        prevItem.InternalTypeModels.AddRange(namespaceModel.Types.Values);
                    }
                }
            }
            return rootItem.InternalSubNamespaces;
        }
    }
}
