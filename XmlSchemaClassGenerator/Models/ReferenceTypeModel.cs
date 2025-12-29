using System.Collections.Generic;

namespace XmlSchemaClassGenerator;

public class ReferenceTypeModel(GeneratorConfiguration configuration) : TypeModel(configuration)
{
    public List<PropertyModel> Properties { get; } = [];
    public List<InterfaceModel> Interfaces { get; } = [];

    public void AddInterfaces(IEnumerable<InterfaceModel> interfaces)
    {
        foreach (var interfaceModel in interfaces)
        {
            if (!Interfaces.Contains(interfaceModel) && interfaceModel != this)
            {
                Interfaces.Add(interfaceModel);
                interfaceModel.DerivedTypes.Add(this);
            }
        }
    }
}
