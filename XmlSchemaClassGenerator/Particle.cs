using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace XmlSchemaClassGenerator
{
    public class Particle
    {
        public Particle(XmlSchemaParticle particle, XmlSchemaObject parent)
        {
            XmlParticle = particle;
            XmlParent = parent;
            MinOccurs = particle?.MinOccurs ?? 1;
            MaxOccurs = particle?.MaxOccurs ?? 1;
        }

        public XmlSchemaParticle XmlParticle { get; set; }
        public XmlSchemaObject XmlParent { get; }
        public decimal MaxOccurs { get; set; }
        public decimal MinOccurs { get; set; }
    }
}
