using System;
using System.Threading;
using System.Threading.Tasks;
using JewerlySystem.Common.Application.Documentation.Abstractions;
using JewerlySystem.Common.Application.Documentation.Queries;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace JewerlySystem.Common.UnitTests;

public class GetDocumentationPageQueryHandlerTests
{
    private readonly IDocumentationProvider _provider = Substitute.For<IDocumentationProvider>();
    private readonly ILogger<GetDocumentationPageQueryHandler> _logger = Substitute.For<ILogger<GetDocumentationPageQueryHandler>>();

    [Fact]
    public async Task Handle_WhenFilePathIsEmpty_ReturnsValidationError()
    {
        // Arrange
        var handler = new GetDocumentationPageQueryHandler(_provider, _logger);
        var query = new GetDocumentationPageQuery("Inventario", "");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Documentation.InvalidPath", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenContentNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _provider.GetContentAsync("docs/missing.md", Arg.Any<CancellationToken>())
            .Returns((string.Empty, string.Empty));

        var handler = new GetDocumentationPageQueryHandler(_provider, _logger);
        var query = new GetDocumentationPageQuery("Manual", "docs/missing.md");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Documentation.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenContentFound_ReturnsSuccessWithMarkdownAndHtml()
    {
        // Arrange
        const string markdown = "# Proceso 1\nGuía de inventario.";
        const string html = "<h1>Proceso 1</h1><p>Guía de inventario.</p>";

        _provider.GetContentAsync("docs/inventory.md", Arg.Any<CancellationToken>())
            .Returns((markdown, html));

        var handler = new GetDocumentationPageQueryHandler(_provider, _logger);
        var query = new GetDocumentationPageQuery("Inventario y Catálogo", "docs/inventory.md");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Inventario y Catálogo", result.Value.Title);
        Assert.Equal(markdown, result.Value.MarkdownContent);
        Assert.Equal(html, result.Value.HtmlContent);
    }
}
