using System;
using System.Linq;

namespace WebAPI.Controllers.Helpers
{
    /// <summary>
    /// Helper methods to work with errors.
    /// </summary>
    public static class EnumHelpers
    {
        /// <summary>
        /// Converts enum to string using StringValueAttribute.
        /// </summary>
        /// <param name="value">Enum value to convert.</param>
        /// <returns>String.</returns>
        public static string GetStringValue(this Enum value)
            => (value
                .GetType()
                .GetField(value.ToString())
                .GetCustomAttributes(typeof(StringValueAttribute), false)
                .FirstOrDefault() as StringValueAttribute)?.StringValue;
    }
}
