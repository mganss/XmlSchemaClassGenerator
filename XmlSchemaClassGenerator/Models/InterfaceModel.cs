using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

namespace XmlSchemaClassGenerator;

public class InterfaceModel(GeneratorConfiguration configuration) : ReferenceTypeModel(configuration)
{
    public List<ReferenceTypeModel> DerivedTypes { get; } = [];

    public override CodeTypeDeclaration Generate()
    {
        var interfaceDeclaration = base.Generate();

        interfaceDeclaration.IsInterface = true;
        interfaceDeclaration.IsPartial = true;
        if (Configuration.AssemblyVisible)
        {
            interfaceDeclaration.TypeAttributes = (interfaceDeclaration.TypeAttributes & ~System.Reflection.TypeAttributes.VisibilityMask) | System.Reflection.TypeAttributes.NestedAssembly;
        }

        foreach (var property in Properties)
            property.AddInterfaceMembersTo(interfaceDeclaration);

        interfaceDeclaration.BaseTypes.AddRange(Interfaces.Select(i => i.GetReferenceFor(Namespace)).ToArray());

        Configuration.TypeVisitor(interfaceDeclaration, this);
        return interfaceDeclaration;
    }

    public IEnumerable<ReferenceTypeModel> AllDerivedReferenceTypes(List<ReferenceTypeModel> processedTypeModels = null)
    {
        processedTypeModels ??= [];

        foreach (var interfaceModelDerivedType in DerivedTypes.Except(processedTypeModels))
        {
            yield return interfaceModelDerivedType;

            processedTypeModels.Add(interfaceModelDerivedType);

            switch (interfaceModelDerivedType)
            {
                case InterfaceModel derivedInterfaceModel:
                    {
                        foreach (var referenceTypeModel in derivedInterfaceModel.AllDerivedReferenceTypes(processedTypeModels))
                        {
                            yield return referenceTypeModel;
                        }

                        break;
                    }
                case ClassModel derivedClassModel:
                    {
                        foreach (var baseClass in derivedClassModel.GetAllDerivedTypes())
                        {
                            yield return baseClass;
                        }

                        break;
                    }
            }
        }
    }
}
