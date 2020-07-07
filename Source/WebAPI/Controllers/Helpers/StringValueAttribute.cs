using System;

namespace WebAPI.Controllers.Helpers
{
    /// <summary>
    /// String value for an enum.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class StringValueAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringValueAttribute"/> class.
        /// </summary>
        /// <param name="value">String value.</param>
        public StringValueAttribute(string value) => StringValue = value;

        /// <summary>
        /// Gets string value.
        /// </summary>
        /// <value> String value.</value>
        public string StringValue { get; }
    }
}
