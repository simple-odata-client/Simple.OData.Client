using System;

namespace Simple.OData.Client
{
    /// <summary>
    /// Specifies expansion levels.
    /// </summary>
    public enum ODataExpandLevels
    {
        /// <summary>
        /// Specifies maximum expansion levels.
        /// </summary>
        Max,
    }

    /// <summary>
    /// Specifies expansion mode (by value or by reference).
    /// </summary>
    public enum ODataExpandMode
    {
        /// <summary>
        /// Associations should be expanded by value.
        /// </summary>
        ByValue,

        /// <summary>
        /// Associations should be expanded by reference.
        /// </summary>
        ByReference,
    }

    /// <summary>
    /// Specifies how to expand entity associations.
    /// </summary>
    public class ODataExpandOptions : IEquatable<ODataExpandOptions>
    {
        /// <summary>
        /// The number of levels to expand.
        /// </summary>
        public int Levels { get; private set; }

        /// <summary>
        /// The expansion mode (by value or by reference).
        /// </summary>
        public ODataExpandMode ExpandMode { get; private set; }

        private ODataExpandOptions(int levels = 1, ODataExpandMode expandMode = ODataExpandMode.ByValue)
        {
            this.Levels = levels;
            this.ExpandMode = expandMode;
        }

        private ODataExpandOptions(ODataExpandLevels levels, ODataExpandMode expandMode = ODataExpandMode.ByValue)
            : this(0, expandMode)
        {
        }

        /// <summary>
        /// Expansion by value.
        /// </summary>
        /// <param name="levels">The number of levels to expand.</param>
        public static ODataExpandOptions ByValue(int levels = 1)
        {
            return new ODataExpandOptions(levels, ODataExpandMode.ByValue);
        }

        /// <summary>
        /// Expansion by value.
        /// </summary>
        /// <param name="levels">The number of levels to expand.</param>
        public static ODataExpandOptions ByValue(ODataExpandLevels levels)
        {
            return new ODataExpandOptions(levels, ODataExpandMode.ByValue);
        }

        /// <summary>
        /// Expansion by reference.
        /// </summary>
        /// <param name="levels">The number of levels to expand.</param>
        public static ODataExpandOptions ByReference(int levels = 1)
        {
            return new ODataExpandOptions(levels, ODataExpandMode.ByReference);
        }

        /// <summary>
        /// Expansion by reference.
        /// </summary>
        /// <param name="levels">The number of levels to expand.</param>
        public static ODataExpandOptions ByReference(ODataExpandLevels levels)
        {
            return new ODataExpandOptions(levels, ODataExpandMode.ByReference);
        }

        public bool Equals(ODataExpandOptions other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ExpandMode == other.ExpandMode && Levels == other.Levels;
        }

        public override int GetHashCode()
        {
            return (ExpandMode, Levels).GetHashCode();
        }
    }
}
