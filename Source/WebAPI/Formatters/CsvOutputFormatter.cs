using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace WebAPI.Formatters
{
    /// <summary>
    /// Adapted from code at https://github.com/WebApiContrib/WebAPIContrib.Core
    /// Supports localization via the Accept-Language header.
    /// </summary>
    public class CsvOutputFormatter : OutputFormatter
    {
        private readonly bool useSingleLineHeaderInCsv = true;
        private readonly Encoding encoding = Encoding.UTF8;
        private readonly bool includeExcelDelimiterHeader = true;
        private readonly string csvDelimiter = ",";

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvOutputFormatter"/> class.
        /// </summary>
        public CsvOutputFormatter()
        {
            ContentType = "text/csv";
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/csv"));
        }

        /// <summary>
        /// Gets Content Type.
        /// </summary>
        /// <value>Content Type.</value>
        public string ContentType { get; private set; }

        /// <summary>
        /// Sets the filename and attachment header values.
        /// </summary>
        /// <param name="context">Asp.Net write context for the response.</param>
        public override void WriteResponseHeaders(OutputFormatterWriteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var datetag = DateTime.UtcNow.ToString("yyyyMMdd");
            var fileName = $"output_{datetag}.csv";

            context.HttpContext.Response.Headers["Content-Disposition"] =
                new ContentDispositionHeaderValue("attachment") { FileName = fileName }.ToString();
            context.HttpContext.Response.ContentType = ContentType;
        }

        /// <summary>
        /// Writes response object to body as csv.
        /// </summary>
        /// <param name="context">Asp.Net write context for the response.</param>
        /// <returns>Task.</returns>
        public async override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            var response = context.HttpContext.Response;

            var type = context.Object.GetType();
            Type itemType;

            if (type.GetGenericArguments().Length > 0)
            {
                itemType = type.GetGenericArguments()[0];
            }
            else
            {
                itemType = type.GetElementType();
            }

            var streamWriter = new StreamWriter(response.Body, encoding);

            if (includeExcelDelimiterHeader)
            {
                await streamWriter.WriteLineAsync($"sep ={csvDelimiter}");
            }

            if (useSingleLineHeaderInCsv)
            {
                var values = itemType
                    .GetProperties()
                    .Where(pi => !pi.GetCustomAttributes<JsonIgnoreAttribute>(false).Any()) // Only get the properties that do not define JsonIgnore
                    .Select(pi => new
                    {
                        Order = pi.GetCustomAttribute<JsonPropertyAttribute>(false)?.Order ?? 0,
                        Prop = pi,
                    })
                    .OrderBy(d => d.Order)
                    .Select(d => GetDisplayNameFromNewtonsoftJsonAnnotations(d.Prop));

                await streamWriter.WriteLineAsync(string.Join(csvDelimiter, values));
            }

            foreach (var obj in (IEnumerable<object>)context.Object)
            {
                var vals = obj
                    .GetType()
                    .GetProperties()
                    .Where(pi => !pi.GetCustomAttributes<JsonIgnoreAttribute>().Any())
                    .Select(pi => new
                    {
                        Order = pi.GetCustomAttribute<JsonPropertyAttribute>(false)?.Order ?? 0,
                        Value = pi.GetValue(obj, null),
                        Type = pi.PropertyType,
                        ExportType = pi.GetCustomAttribute<CSVExportType>(false)?.Type,
                        pi.Name,
                    })
                    .OrderBy(d => d.Order)
                    .Select(d => new { d.Value, d.Type, d.Name, d.ExportType });

                string valueLine = string.Empty;

                foreach (var objvalue in vals)
                {
                    if (objvalue.Value != null)
                    {
                        var val = objvalue.Value.ToString();

                        // Substitute smart quotes in Windows-1252.
                        if (encoding.EncodingName == "Western European (ISO)")
                        {
                            val = val.Replace('“', '"').Replace('”', '"');
                        }

                        // Escape quotes.
                        val = val.Replace("\"", "\"\"");

                        // Wrap strings in quotes.
                        if (objvalue.Type == typeof(string))
                        {
                            if (val.Contains(csvDelimiter))
                            {
                                val = $"\"{val}\"";
                            }
                            else if (objvalue.ExportType == CSVExportType.Types.Text)
                            {
                                // Used for strings that may look like numbers to Excel.
                                val = $"=\"{val}\"";
                            }
                        }

                        // Replace any \r or \n special characters from a new line with a space.
                        if (val.Contains("\r"))
                        {
                            val = val.Replace("\r", " ");
                        }

                        if (val.Contains("\n"))
                        {
                            val = val.Replace("\n", " ");
                        }

                        valueLine = string.Concat(valueLine, val, csvDelimiter);
                    }
                    else
                    {
                        valueLine = string.Concat(valueLine, string.Empty, csvDelimiter);
                    }
                }

                await streamWriter.WriteLineAsync(valueLine.Remove(valueLine.Length - csvDelimiter.Length));
            }

            await streamWriter.FlushAsync();
        }

        /// <summary>
        /// Returns true if formatter can serialize this type of object.
        /// </summary>
        /// <param name="type">Response Object Type.</param>
        /// <returns>true/false if supported type.</returns>
        protected override bool CanWriteType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            return IsTypeOfIEnumerable(type);
        }

        /// <summary>
        /// Returns the JsonProperty data annotation name.
        /// </summary>
        /// <param name="pi">Property Info.</param>
        /// <returns>JsonProperty data annotation name.</returns>
        private string GetDisplayNameFromNewtonsoftJsonAnnotations(PropertyInfo pi)
        {
            if (pi.GetCustomAttribute<JsonPropertyAttribute>(false)?.PropertyName is string value)
            {
                return value;
            }

            return pi.GetCustomAttribute<DisplayAttribute>(false)?.Name ?? pi.Name;
        }

        /// <summary>
        /// Helper method - Only IEnumerable objects are supported.
        /// </summary>
        /// <param name="type">Response Object Type.</param>
        /// <returns>true/false if type is IEnumberable and not null.</returns>
        private bool IsTypeOfIEnumerable(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            return typeof(IEnumerable).IsAssignableFrom(type);
        }
    }
}