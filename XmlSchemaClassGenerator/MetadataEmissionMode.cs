namespace XmlSchemaClassGenerator;

/// <summary>
/// Determines whether metadata helper types are emitted.
/// </summary>
public enum MetadataEmissionMode
{
    /// <summary>
    /// Do not emit metadata helper types.
    /// </summary>
    None,
    /// <summary>
    /// Emit metadata helper types required by generated attributes.
    /// </summary>
    Enabled,
}
