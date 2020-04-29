using System;
using System.Collections.Generic;
using System.Text;

namespace XmlSchemaClassGenerator
{
    /// <summary>
    /// Determines the kind collection accessor modifiers to emit and controls baking collection fields initialization
    /// </summary>
    public enum CollectionSettersMode
    {
        /// <summary>
        /// All collection setters are private
        /// </summary>
        Private,
        /// <summary>
        /// All collection setters are public
        /// </summary>
        Public,
        /// <summary>
        /// All collection setters are public and baking collections fields not initialized in constructors
        /// </summary>
        PublicWithoutConstructorInitialization
    }
}
