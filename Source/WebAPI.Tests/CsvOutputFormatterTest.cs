using FluentAssertions;
using Moq;

using Xunit;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Http;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Threading.Tasks;
using WebAPI.Formatters;
using WebAPI.Tests.Common;

namespace WebAPI.Tests
{
    public class CsvOutputFormatterTest
    {
        private readonly CsvOutputFormatter formatter = new CsvOutputFormatter();
        private const string contentType = "text/csv";

        private CsvOutputformatterTestResponse GetCsvOutputformatterTestResponse(string stringElement, string stringElementNoEquals, decimal decimalElement, int intElement, long longElement)
            => new CsvOutputformatterTestResponse()
            {
                StringElement = stringElement,
                StringElementNoEquals = stringElementNoEquals,
                DecimalElement = decimalElement,
                IntElement = intElement,
                LongElement = longElement,
            };

        private OutputFormatterWriteContext GetContext(object returnObjects)
        {
            var testContentType = new StringSegment(contentType);
            formatter.SupportedMediaTypes.Clear();
            formatter.SupportedMediaTypes.Add(new Microsoft.Net.Http.Headers.MediaTypeHeaderValue(contentType));

            //Mock HttpContext.Response.Body and MttpContext.Response.Headers
            var body = new MemoryStream();
            var headerDictionary = new HeaderDictionary();
            var mockHttpResponse = new Mock<HttpResponse>();
            mockHttpResponse.Setup(m => m.Body).Returns(body);
            mockHttpResponse.Setup(m => m.Headers).Returns(headerDictionary);
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(m => m.Response).Returns(mockHttpResponse.Object);

            var context = new OutputFormatterWriteContext(
                mockHttpContext.Object,
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                objectType: typeof(IEnumerable<CsvOutputformatterTestResponse>),
                @object: returnObjects)
            {
                ContentType = testContentType,
            };

            return context;
        }

        [Fact]
        public void TestContentTypeNull()
        {
            // Arrange
            var canWriteContextMock = new Mock<OutputFormatterCanWriteContext>(Mock.Of<HttpContext>());
            canWriteContextMock.Setup(m => m.ObjectType).Returns(typeof(IEnumerable));

            // Act
            var canWrite = formatter.CanWriteResult(canWriteContextMock.Object);

            // Assert
            canWrite.Should().BeTrue();
        }

        [Fact]
        public void TestContentTypeNotSupported()
        {
            // Arrange
            var canWriteContextMock = new Mock<OutputFormatterCanWriteContext>(Mock.Of<HttpContext>());
            canWriteContextMock.Setup(m => m.ObjectType).Returns(typeof(CsvOutputformatterTestResponse));

            // Act
            var canWrite = formatter.CanWriteResult(canWriteContextMock.Object);

            // Assert
            canWrite.Should().BeFalse();
        }

        [Fact]
        public void TestContentTypeSupported()
        {
            // Arrange
            var canWriteContextMock = new Mock<OutputFormatterCanWriteContext>(Mock.Of<HttpContext>());
            canWriteContextMock.Setup(m => m.ObjectType).Returns(typeof(IEnumerable<CsvOutputformatterTestResponse>));

            // Act
            var canWrite = formatter.CanWriteResult(canWriteContextMock.Object);

            // Assert
            canWrite.Should().BeTrue();
        }

        [Fact]
        public void TestCreateFormatter()
        {
            // Assert
            formatter.ContentType.Should().NotBeNull();
            formatter.ContentType.Should().BeEquivalentTo(contentType);
            var expectedMediaTypes = new MediaTypeCollection
            {
                Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType)
            };
            formatter.SupportedMediaTypes.Should().BeEquivalentTo(expectedMediaTypes);
        }

        [Fact]
        public void TestWriteReponseHeader()
        {
            // Arrange
            var resultObjects = new[] { GetCsvOutputformatterTestResponse("1", "one no equals", (decimal)0.11, (int)11, (long)111) };

            var context = GetContext(resultObjects);

            // Act
            formatter.WriteResponseHeaders(context);

            // Assert
            context.ContentType.ToString().Should().Be(contentType);
        }

        [Fact]
        public void TestWriteResponseHeaderNullContext()
        {
            // Arrange
            OutputFormatterWriteContext context = null;

            // Act
            Action act = () => formatter.WriteResponseHeaders(context);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task TestWriteResponseBodyAsync()
        {
            using (new TemporaryCulture("en-US"))
            {
                // Arrange
                var resultObjects = new[] {
                    GetCsvOutputformatterTestResponse("1", "one no equals", (decimal)0.11, (int)11, (long)111),
                    GetCsvOutputformatterTestResponse("2", "two no equals", (decimal)0.22, (int)22, (long)222)
                };

                var context = GetContext(resultObjects);

                // Act
                await formatter.WriteResponseBodyAsync(context);

                // Assert
                context.HttpContext.Response.Body.Position = 0;
                StreamReader reader = new StreamReader(context.HttpContext.Response.Body);
                string readResponseBody = reader.ReadToEnd();
 
                var expectedResult =
                    "sep =," + Environment.NewLine +
                    "string_element,string_element_no_equals,decimal_element,int_element,long_element" + Environment.NewLine +
                    "=\"1\",one no equals,0.11,11,111" + Environment.NewLine +
                    "=\"2\",two no equals,0.22,22,222" + Environment.NewLine;
                readResponseBody.Should().BeEquivalentTo(expectedResult);
            }
        }

        [Fact]
        public async Task TestWriteResponseBodyAsyncSpecialChar()
        {
            using (new TemporaryCulture("en-US"))
            {
                // Arrange
                var resultObjects = new[] {
                    GetCsvOutputformatterTestResponse("1\n", "one no equals", (decimal)0.11, (int)11, (long)111),
                    GetCsvOutputformatterTestResponse("2\r", null, (decimal)0.22, (int)22, (long)222),
                    GetCsvOutputformatterTestResponse("3,3", "three, no equals", (decimal)0.22, (int)22, (long)222)
                };

                var context = GetContext(resultObjects);
                
                // Act
                await formatter.WriteResponseBodyAsync(context);

                // Assert
                context.HttpContext.Response.Body.Position = 0;
                StreamReader reader = new StreamReader(context.HttpContext.Response.Body);
                string readResponseBody = reader.ReadToEnd();

                var expectedResult = "sep =," + Environment.NewLine +
                    "string_element,string_element_no_equals,decimal_element,int_element,long_element" + Environment.NewLine +
                    "=\"1 \",one no equals,0.11,11,111" + Environment.NewLine +
                    "=\"2 \",,0.22,22,222" + Environment.NewLine +
                    "\"3,3\",\"three, no equals\",0.22,22,222" + Environment.NewLine;

                readResponseBody.Should().BeEquivalentTo(expectedResult);
            }
        }
    }

    public class CsvOutputformatterTestResponse
    {
        [CSVExportType("Text")]
        [Display(Name = "string_element")]
        public string StringElement { get; set; }

        [Display(Name = "string_element_no_equals")]
        public string StringElementNoEquals { get; set; }

        [Display(Name = "decimal_element")]
        public decimal DecimalElement { get; set; }

        [Display(Name = "int_element")]
        public int? IntElement { get; set; }

        [Display(Name = "long_element")]
        public long LongElement { get; set; }
    }

    public class TestHttpResponseStreamWriterFactory : IHttpResponseStreamWriterFactory
    {
        public TextWriter CreateWriter(Stream stream, Encoding encoding)
        {
            return new HttpResponseStreamWriter(stream, encoding);
        }
    }
}
