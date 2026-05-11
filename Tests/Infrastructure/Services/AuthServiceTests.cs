using Domain;
using Infrastructure.Identity;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using Shouldly;
using System.Security.Claims;

namespace Tests.Infrastructure.Services;

public class AuthServiceTests
{
    private const string TestUsername = "testuser";
    private const string TestPassword = "P@ssw0rd";
    private const string TestUserId = "user-1";

    private static readonly AppUser TestAppUser = new()
    {
        Id = TestUserId,
        UserName = TestUsername
    };

    private static (AuthService Service, Mock<UserManager<AppUser>> UserManagerMock,
        Mock<SignInManager<AppUser>> SignInManagerMock) CreateService()
    {
        var storeMock = new Mock<IUserStore<AppUser>>();
        var userManagerMock = new Mock<UserManager<AppUser>>(
            storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
        var signInManagerMock = new Mock<SignInManager<AppUser>>(
            userManagerMock.Object, contextAccessorMock.Object, claimsFactoryMock.Object,
            null!, null!, null!, null!);

        var service = new AuthService(userManagerMock.Object, signInManagerMock.Object);
        return (service, userManagerMock, signInManagerMock);
    }

    [Fact]
    public async Task SignInAsync_ReturnsTrue_WhenCredentialsAreValid()
    {
        var (service, _, signInManagerMock) = CreateService();
        signInManagerMock.Setup(m => m.PasswordSignInAsync(TestUsername, TestPassword, false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var result = await service.SignInAsync(TestUsername, TestPassword, false);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task SignInAsync_ReturnsFalse_WhenCredentialsAreInvalid()
    {
        var (service, _, signInManagerMock) = CreateService();
        signInManagerMock.Setup(m => m.PasswordSignInAsync(TestUsername, "wrong", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var result = await service.SignInAsync(TestUsername, "wrong", false);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task SignInAsync_PassesCorrectArguments_ToSignInManager()
    {
        var (service, _, signInManagerMock) = CreateService();
        signInManagerMock.Setup(m => m.PasswordSignInAsync(TestUsername, TestPassword, true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        await service.SignInAsync(TestUsername, TestPassword, true);

        signInManagerMock.Verify(
            m => m.PasswordSignInAsync(TestUsername, TestPassword, true, false), Times.Once);
    }

    [Fact]
    public async Task SignOutAsync_DelegatesToSignInManager()
    {
        var (service, _, signInManagerMock) = CreateService();

        await service.SignOutAsync();

        signInManagerMock.Verify(m => m.SignOutAsync(), Times.Once);
    }

    [Fact]
    public void GetCurrentUserId_ReturnsUserId_WhenPrincipalIsValid()
    {
        var (service, userManagerMock, _) = CreateService();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, TestUserId)], "TestAuth"));
        userManagerMock.Setup(m => m.GetUserId(principal)).Returns(TestUserId);

        var result = service.GetCurrentUserId(principal);

        result.ShouldBe(TestUserId);
    }

    [Fact]
    public void GetCurrentUserId_ReturnsNull_WhenPrincipalHasNoId()
    {
        var (service, userManagerMock, _) = CreateService();
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        userManagerMock.Setup(m => m.GetUserId(principal)).Returns((string?)null);

        var result = service.GetCurrentUserId(principal);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetCurrentUserRoleAsync_ReturnsUserRole_WhenUserIsNull()
    {
        var (service, userManagerMock, _) = CreateService();
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        userManagerMock.Setup(m => m.GetUserAsync(principal)).ReturnsAsync((AppUser?)null);

        var result = await service.GetCurrentUserRoleAsync(principal);

        result.ShouldBe(UserRole.User);
    }

    [Fact]
    public async Task GetCurrentUserRoleAsync_ReturnsParsedRole_WhenUserHasRole()
    {
        var (service, userManagerMock, _) = CreateService();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, TestUserId)], "TestAuth"));
        userManagerMock.Setup(m => m.GetUserAsync(principal)).ReturnsAsync(TestAppUser);
        userManagerMock.Setup(m => m.GetRolesAsync(TestAppUser))
            .ReturnsAsync(new List<string> { "Admin" });

        var result = await service.GetCurrentUserRoleAsync(principal);

        result.ShouldBe(UserRole.Admin);
    }

    [Fact]
    public async Task GetCurrentUserRoleAsync_ReturnsDefaultRole_WhenRoleIsInvalid()
    {
        var (service, userManagerMock, _) = CreateService();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, TestUserId)], "TestAuth"));
        userManagerMock.Setup(m => m.GetUserAsync(principal)).ReturnsAsync(TestAppUser);
        userManagerMock.Setup(m => m.GetRolesAsync(TestAppUser))
            .ReturnsAsync(new List<string> { "NonExistentRole" });

        var result = await service.GetCurrentUserRoleAsync(principal);

        result.ShouldBe(UserRole.User);
    }

    [Fact]
    public async Task GetCurrentUserRoleAsync_ReturnsDefaultRole_WhenRolesListIsEmpty()
    {
        var (service, userManagerMock, _) = CreateService();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, TestUserId)], "TestAuth"));
        userManagerMock.Setup(m => m.GetUserAsync(principal)).ReturnsAsync(TestAppUser);
        userManagerMock.Setup(m => m.GetRolesAsync(TestAppUser))
            .ReturnsAsync(new List<string>());

        var result = await service.GetCurrentUserRoleAsync(principal);

        result.ShouldBe(UserRole.User);
    }
}
