using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace XmlSchemaClassGenerator;

public class Particle(XmlSchemaParticle particle, XmlSchemaObject parent)
{
    public XmlSchemaParticle XmlParticle { get; set; } = particle;
    public XmlSchemaObject XmlParent { get; } = parent;
    public decimal MaxOccurs { get; set; } = particle?.MaxOccurs ?? 1;
    public decimal MinOccurs { get; set; } = particle?.MinOccurs ?? 1;
}
