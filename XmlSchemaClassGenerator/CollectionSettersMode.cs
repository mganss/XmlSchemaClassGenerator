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
        /// All collection setters are public and backing collections fields are not initialized in constructors
        /// </summary>
        PublicWithoutConstructorInitialization,
        /// <summary>
        /// All collections setters are init-only
        /// </summary>
        Init,
        /// <summary>
        /// All collections setters are init-only and backing collections fields are not initialized in constructors
        /// </summary>
        InitWithoutConstructorInitialization
    }
}
