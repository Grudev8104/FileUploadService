using Moq;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Threading;
using FileUploadServiceAPI1.Controllers;
using Moq.Protected;
using System.Text;

namespace FileUploadServiceAPI.Tests.Controllers
{
    public class FileUploadControllerTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly FileUploadController _controller;

        public FileUploadControllerTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            var mockMessageHandler = new Mock<HttpMessageHandler>();

            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var httpClient = new HttpClient(mockMessageHandler.Object)
            {
                BaseAddress = new Uri("https://localhost:5001")
            };

            _mockHttpClientFactory.Setup(factory => factory.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _controller = new FileUploadController(_mockHttpClientFactory.Object);
        }

        [Fact]
        public async Task UploadFile_ShouldReturnBadRequest_WhenFileIsNull()
        {
            var result = await _controller.UploadFile(null, null);  

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadFile_ShouldReturnServerError_WhenXmlIsInvalid()
        {
            var fileMock = new Mock<IFormFile>();
            var content = "invalid xml";
            var fileName = "test.xml";
            var memoryStream = new MemoryStream();
            var writer = new StreamWriter(memoryStream);
            writer.Write(content);
            writer.Flush();
            memoryStream.Position = 0;

            fileMock.Setup(_ => _.OpenReadStream()).Returns(memoryStream);
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(memoryStream.Length);

            var result = await _controller.UploadFile(fileMock.Object, "invalidFileName"); 

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task UploadFile_ShouldReturnOk_WhenFileIsLarge()
        {
            var fileMock = new Mock<IFormFile>();

            var stringBuilder = new StringBuilder();
            stringBuilder.Append("<root>");
            for (int i = 0; i < 10000; i++)
            {
                stringBuilder.Append($"<element{i}>This is some large content with index {i}</element{i}>");
            }
            stringBuilder.Append("</root>");

            var content = stringBuilder.ToString();
            var fileName = "test.xml";
            var memoryStream = new MemoryStream();

            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, 1024, true))
            {
                writer.Write(content);
                writer.Flush();
            }

            memoryStream.Position = 0;

            fileMock.Setup(_ => _.OpenReadStream()).Returns(memoryStream);
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(memoryStream.Length);

            var result = await _controller.UploadFile(fileMock.Object, "largeFileName"); 

            var objectResult = Assert.IsType<OkObjectResult>(result);
        }
    }
}
