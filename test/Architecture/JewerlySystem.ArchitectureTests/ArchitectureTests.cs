using NetArchTest.Rules;
using Xunit;

namespace JewerlySystem.ArchitectureTests;

public class ArchitectureTests
{
    private const string DomainNamespace = "JewerlySystem.Modules.Contracts.Domain";
    private const string ApplicationNamespace = "JewerlySystem.Modules.Contracts.Application";
    private const string InfrastructureNamespace = "JewerlySystem.Modules.Contracts.Infrastructure";

    [Fact]
    public void Domain_Should_Not_HaveDependencyOn_Application_Or_Infrastructure()
    {
        // Arrange
        var assembly = typeof(Modules.Contracts.Domain.Contract).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, "Domain layer must not depend on Application or Infrastructure layers.");
    }

    [Fact]
    public void Application_Should_Not_HaveDependencyOn_Infrastructure()
    {
        // Arrange
        var assembly = typeof(Modules.Contracts.Application.Commands.CreateContractCommand).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, "Application layer must not depend on Infrastructure layer.");
    }

    [Fact]
    public void Handlers_Should_Have_Name_EndingWith_Handler()
    {
        // Arrange
        var assembly = typeof(Modules.Contracts.Application.Commands.CreateContractCommand).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ImplementInterface(typeof(MediatR.IRequestHandler<,>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, "All MediatR request handlers must have a name ending with 'Handler'.");
    }

    [Fact]
    public void AuthDomain_Should_Not_HaveDependencyOn_AuthInfrastructure()
    {
        // Arrange
        var assembly = typeof(Modules.Auth.Domain.User).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn("JewerlySystem.Modules.Auth.Infrastructure")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, "Auth Domain must not depend on Auth Infrastructure.");
    }
}
