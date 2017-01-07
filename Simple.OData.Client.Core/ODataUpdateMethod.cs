namespace Simple.OData.Client
{
    /// <summary>
    /// The preferred entry update method.
    /// </summary>
    public enum ODataUpdateMethod
    {
        /// <summary>
        /// Use PATCH to update OData entries
        /// </summary>
        Patch,
        /// <summary>
        /// Use MERGE to update OData entries
        /// </summary>

        [System.Obsolete("This method is obsolete. Use Patch instead.")]
        Merge,
        /// <summary>
        /// Use PUT to update OData entries
        /// </summary>
        Put,
    }
}