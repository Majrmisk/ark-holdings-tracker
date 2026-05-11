using Application.Services;
using Domain;
using Infrastructure.Repositories;
using NetArchTest.Rules;
using Presentation.Areas.Admin.Controllers;
using Shouldly;
using System.Reflection;

namespace Tests.Architecture;

public class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(HoldingRecord).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(HoldingService).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(HoldingsRepository).Assembly;
    private static readonly Assembly PresentationAssembly = typeof(HomeController).Assembly;

    [Fact]
    public void Domain_DoesNotDependOn_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That().ResideInNamespace("Domain")
            .ShouldNot().HaveDependencyOn("Application")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Domain_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That().ResideInNamespace("Domain")
            .ShouldNot().HaveDependencyOn("Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Domain_DoesNotDependOn_Presentation()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That().ResideInNamespace("Domain")
            .ShouldNot().HaveDependencyOn("Presentation")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That().ResideInNamespace("Application")
            .ShouldNot().HaveDependencyOn("Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_DoesNotDependOn_Presentation()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That().ResideInNamespace("Application")
            .ShouldNot().HaveDependencyOn("Presentation")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Infrastructure_DoesNotDependOn_Presentation()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That().ResideInNamespace("Infrastructure")
            .ShouldNot().HaveDependencyOn("Presentation")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Interfaces_ShouldBeNamedWithIPrefix()
    {
        var assemblies = new[] { DomainAssembly, ApplicationAssembly, InfrastructureAssembly, PresentationAssembly };
        foreach (var assembly in assemblies)
        {
            var result = Types.InAssembly(assembly)
                .That().AreInterfaces()
                .Should().HaveNameStartingWith("I")
                .GetResult();

            result.IsSuccessful.ShouldBeTrue(
                $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    [Fact]
    public void Services_ShouldResideInApplicationOrDomainNamespace()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That().HaveNameEndingWith("Service")
            .And().AreNotInterfaces()
            .Should().ResideInNamespace("Application.Services")
            .Or().ResideInNamespace("Domain.Services")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Repositories_ShouldResideInInfrastructureNamespace()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That().HaveNameEndingWith("Repository")
            .Should().ResideInNamespace("Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Clients_ShouldResideInApplicationNamespace()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That().HaveNameEndingWith("Client")
            .Should().ResideInNamespace("Application")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Controllers_ShouldResideInPresentationNamespace()
    {
        var result = Types.InAssembly(PresentationAssembly)
            .That().HaveNameEndingWith("Controller")
            .Should().ResideInNamespace("Presentation")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Interfaces_ShouldBePublic()
    {
        var assemblies = new[] { DomainAssembly, ApplicationAssembly, InfrastructureAssembly, PresentationAssembly };
        foreach (var assembly in assemblies)
        {
            var result = Types.InAssembly(assembly)
                .That().AreInterfaces()
                .Should().BePublic()
                .GetResult();

            result.IsSuccessful.ShouldBeTrue(
                $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    [Fact]
    public void Controllers_ShouldBePublic()
    {
        var result = Types.InAssembly(PresentationAssembly)
            .That().HaveNameEndingWith("Controller")
            .Should().BePublic()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Services_ShouldBePublic()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That().HaveNameEndingWith("Service")
            .Should().BePublic()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Repositories_ShouldBePublic()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That().HaveNameEndingWith("Repository")
            .Should().BePublic()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}