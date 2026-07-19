using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using Maliev.InventoryService.Domain.Models;
using Maliev.InventoryService.Infrastructure.HttpClients;

namespace Maliev.InventoryService.Tests.Infrastructure;

/// <summary>
/// Tests for the MaterialServiceClient.
/// </summary>
public class MaterialServiceClientTests
{
    /// <summary>
    /// Verifies that GetMaterialAsync returns material when found.
    /// </summary>
    [Fact]
    public async Task GetMaterialAsync_WhenFound_ReturnsMaterial()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        var expectedMaterial = new MaterialDto
        {
            Id = materialId,
            Name = "PLA",
            Density = 1.24m
        };

        var mockMessageHandler = new Mock<HttpMessageHandler>();
        mockMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request =>
                    request.RequestUri != null &&
                    request.RequestUri.PathAndQuery == $"/material/v1/materials/{materialId}"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(expectedMaterial)
            });

        var httpClient = new HttpClient(mockMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };

        var loggerMock = new Mock<ILogger<MaterialServiceClient>>();
        var client = new MaterialServiceClient(httpClient, loggerMock.Object);

        // Act
        var result = await client.GetMaterialAsync(materialId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(materialId, result.Id);
        Assert.Equal("PLA", result.Name);
        Assert.Equal(1.24m, result.Density);
        mockMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(request =>
                request.RequestUri != null &&
                request.RequestUri.PathAndQuery == $"/material/v1/materials/{materialId}"),
            ItExpr.IsAny<CancellationToken>());
    }

    /// <summary>
    /// Verifies that GetMaterialAsync returns null when material not found.
    /// </summary>
    [Fact]
    public async Task GetMaterialAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var materialId = Guid.NewGuid();

        var mockMessageHandler = new Mock<HttpMessageHandler>();
        mockMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var httpClient = new HttpClient(mockMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };

        var loggerMock = new Mock<ILogger<MaterialServiceClient>>();
        var client = new MaterialServiceClient(httpClient, loggerMock.Object);

        // Act
        var result = await client.GetMaterialAsync(materialId);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that GetMaterialAsync throws on HTTP error.
    /// </summary>
    [Fact]
    public async Task GetMaterialAsync_OnHttpError_ThrowsException()
    {
        // Arrange
        var materialId = Guid.NewGuid();

        var mockMessageHandler = new Mock<HttpMessageHandler>();
        mockMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var httpClient = new HttpClient(mockMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };

        var loggerMock = new Mock<ILogger<MaterialServiceClient>>();
        var client = new MaterialServiceClient(httpClient, loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetMaterialAsync(materialId));
    }
}
