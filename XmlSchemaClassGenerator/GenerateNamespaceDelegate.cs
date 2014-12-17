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
    /// <param name="key">The key to search for to create a .NET namespace</param>
    /// <returns>The corresponding .NET namespace</returns>
    public delegate string GenerateNamespaceDelegate(NamespaceKey key);
}
