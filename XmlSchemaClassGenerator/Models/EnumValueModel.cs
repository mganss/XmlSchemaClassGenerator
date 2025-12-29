using System.Collections.Generic;

namespace XmlSchemaClassGenerator;

public class EnumValueModel
{
    public string Name { get; set; }
    public string Value { get; set; }
    public bool IsDeprecated { get; set; }
    public List<DocumentationModel> Documentation { get; } = [];
}
