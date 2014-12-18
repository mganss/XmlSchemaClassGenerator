namespace XmlSchemaClassGenerator
{
    /// <summary>
    /// Determines the kind of annotations to emit
    /// </summary>
    /// <remarks>
    /// The order in the source must be kept to ensure that <see cref="RestrictionModel.IsSupported"/> works
    /// as expected.
    /// </remarks>
    public enum DataAnnotationMode
    {
        /// <summary>
        /// All annotations for full frameworks
        /// </summary>
        All,
        /// <summary>
        /// Only annotations supported by PCLs
        /// </summary>
        Partial,
        /// <summary>
        /// No annotations (for Windows Phone compatible PCLs)
        /// </summary>
        None,
    }
}