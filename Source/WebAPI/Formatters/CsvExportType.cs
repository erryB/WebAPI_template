using System;

namespace WebAPI.Formatters
{
    /// <summary>
    /// Property Attribute to set the formatting when exporting to CSV files. i.e.: CSVExportType("Text") can be useful for fields containing phone numbers, because Excel could intepret them as numbers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CSVExportType : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CSVExportType"/> class.
        /// </summary>
        /// <param name="type">Type.</param>
        public CSVExportType(string type)
        {
            if (type == "Text")
            {
                Type = Types.Text;
            }
        }

        /// <summary>
        /// Types.
        /// </summary>
        public enum Types
        {
            /// <summary>
            /// Text.
            /// </summary>
            Text,
        }

        /// <summary>
        /// Gets or sets type.
        /// </summary>
        /// <value>
        /// Type.
        /// </value>
        public Types Type { get; set; }
    }
}
