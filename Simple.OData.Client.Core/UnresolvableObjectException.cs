using System;

namespace Simple.OData.Client
{
    /// <summary>
    /// The exception that is thrown when service metadata doesn't contain the requested metadata object
    /// </summary>
    public sealed class UnresolvableObjectException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnresolvableObjectException"/> class.
        /// </summary>
        public UnresolvableObjectException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnresolvableObjectException"/> class.
        /// </summary>
        /// <param name="objectName">Name of the metadata object.</param>
        public UnresolvableObjectException(string objectName)
        {
            ObjectName = objectName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnresolvableObjectException"/> class.
        /// </summary>
        /// <param name="objectName">Name of the metadata object.</param>
        /// <param name="message">The message that describes the error.</param>
        public UnresolvableObjectException(string objectName, string message)
            : base(message)
        {
            ObjectName = objectName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnresolvableObjectException"/> class.
        /// </summary>
        /// <param name="objectName">Name of the object.</param>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The inner exception.</param>
        public UnresolvableObjectException(string objectName, string message, Exception inner)
            : base(message, inner)
        {
            ObjectName = objectName;
        }

        /// <summary>
        /// Gets the name of the unresolved metadata object.
        /// </summary>
        /// <value>
        /// The name of the object.
        /// </value>
        public string ObjectName
        {
            get { return Data.Contains("ObjectName") ? (Data["ObjectName"] != null ? Data["ObjectName"].ToString() : "{{null}}") : null; }
            private set { Data["ObjectName"] = value; }
        }
    }
}
