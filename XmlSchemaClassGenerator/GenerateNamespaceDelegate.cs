using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlSchemaClassGenerator
{
    /// <summary>
    /// Delegate to be called to generate a .NET namespace for a file name and XML namespace
    /// </summary>
    /// <param name="fileName">File name to create the .NET namespace for</param>
    /// <param name="ns">The XML Namespace to create the .NET namespace for</param>
    /// <returns>The corresponding .NET namespace</returns>
    public delegate string GenerateNamespaceDelegate(string fileName, string ns);
}
