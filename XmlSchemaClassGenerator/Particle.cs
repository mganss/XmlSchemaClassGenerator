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
        public Particle(XmlSchemaParticle particle)
        {
            XmlParticle = particle;
            MinOccurs = particle.MinOccurs;
            MaxOccurs = particle.MaxOccurs;
        }

        public XmlSchemaParticle XmlParticle { get; set; }
        public decimal MaxOccurs { get; set; }
        public decimal MinOccurs { get; set; }
    }
}
