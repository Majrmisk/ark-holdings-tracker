using Application.Common;
using Domain;
using Infrastructure.Identity;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Moq;
using Shouldly;

namespace Tests.Infrastructure.Repositories;

public class UserRepositoryTests
{
    private const string UserId = "user-1";
    private const string Username = "testuser";
    private const string Password = "P@ssw0rd";

    private static readonly AppUser TestAppUser = new()
    {
        Id = UserId,
        UserName = Username,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static (UserRepository Repo, Mock<UserManager<AppUser>> UserManagerMock) CreateRepository()
    {
        var storeMock = new Mock<IUserStore<AppUser>>();
        var userManagerMock = new Mock<UserManager<AppUser>>(
            storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var repo = new UserRepository(userManagerMock.Object);
        return (repo, userManagerMock);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos_WhenUsersExist()
    {
        var (repo, userManagerMock) = CreateRepository();
        var users = new List<AppUser> { TestAppUser }.AsQueryable();
        userManagerMock.Setup(m => m.Users).Returns(users);
        userManagerMock.Setup(m => m.GetRolesAsync(TestAppUser))
            .ReturnsAsync(new List<string> { "Admin" });

        var result = await repo.GetAllAsync();

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(UserId);
        result[0].UserName.ShouldBe(Username);
        result[0].Role.ShouldBe("Admin");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoUsersExist()
    {
        var (repo, userManagerMock) = CreateRepository();
        userManagerMock.Setup(m => m.Users).Returns(new List<AppUser>().AsQueryable());

        var result = await repo.GetAllAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenUserExists()
    {
        var (repo, userManagerMock) = CreateRepository();
        userManagerMock.Setup(m => m.FindByIdAsync(UserId)).ReturnsAsync(TestAppUser);
        userManagerMock.Setup(m => m.GetRolesAsync(TestAppUser))
            .ReturnsAsync(new List<string> { "User" });

        var result = await repo.GetByIdAsync(UserId);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(UserId);
        result.UserName.ShouldBe(Username);
        result.Role.ShouldBe("User");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenUserNotFound()
    {
        var (repo, userManagerMock) = CreateRepository();
        userManagerMock.Setup(m => m.FindByIdAsync(UserId)).ReturnsAsync((AppUser?)null);

        var result = await repo.GetByIdAsync(UserId);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task CreateAsync_ReturnsSuccess_WhenCreateAndRoleSucceed()
    {
        var (repo, userManagerMock) = CreateRepository();
        userManagerMock.Setup(m => m.CreateAsync(It.IsAny<AppUser>(), Password))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<AppUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await repo.CreateAsync(Username, Password, UserRole.User);

        result.IsSuccess.ShouldBeTrue();
        userManagerMock.Verify(m => m.CreateAsync(
            It.Is<AppUser>(u => u.UserName == Username), Password), Times.Once);
        userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<AppUser>(), "User"), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailure_WhenCreateFails()
    {
        var (repo, userManagerMock) = CreateRepository();
        userManagerMock.Setup(m => m.CreateAsync(It.IsAny<AppUser>(), Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Duplicate username." }));

        var result = await repo.CreateAsync(Username, Password, UserRole.User);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldContain("Duplicate username.");
        userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailure_WhenAddToRoleFails()
    {
        var (repo, userManagerMock) = CreateRepository();
        userManagerMock.Setup(m => m.CreateAsync(It.IsAny<AppUser>(), Password))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<AppUser>(), "Admin"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role not found." }));

        var result = await repo.CreateAsync(Username, Password, UserRole.Admin);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldContain("Role not found.");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsSuccess_WhenUserDeletedSuccessfully()
    {
        var (repo, userManagerMock) = CreateRepository();
        userManagerMock.Setup(m => m.FindByIdAsync(UserId)).ReturnsAsync(TestAppUser);
        userManagerMock.Setup(m => m.DeleteAsync(TestAppUser)).ReturnsAsync(IdentityResult.Success);

        var result = await repo.DeleteAsync(UserId);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFailure_WhenUserNotFound()
    {
        var (repo, userManagerMock) = CreateRepository();
        userManagerMock.Setup(m => m.FindByIdAsync(UserId)).ReturnsAsync((AppUser?)null);

        var result = await repo.DeleteAsync(UserId);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("User not found.");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFailure_WhenDeleteFails()
    {
        var (repo, userManagerMock) = CreateRepository();
        userManagerMock.Setup(m => m.FindByIdAsync(UserId)).ReturnsAsync(TestAppUser);
        userManagerMock.Setup(m => m.DeleteAsync(TestAppUser))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Concurrency failure." }));

        var result = await repo.DeleteAsync(UserId);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldContain("Concurrency failure.");
    }

    [Fact]
    public async Task UpdateRoleAsync_ReturnsSuccess_WhenRoleUpdated()
    {
        var (repo, userManagerMock) = CreateRepository();
        userManagerMock.Setup(m => m.FindByIdAsync(UserId)).ReturnsAsync(TestAppUser);
        userManagerMock.Setup(m => m.GetRolesAsync(TestAppUser))
            .ReturnsAsync(new List<string> { "User" });
        userManagerMock.Setup(m => m.RemoveFromRoleAsync(TestAppUser, "User"))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(m => m.AddToRoleAsync(TestAppUser, "Admin"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await repo.UpdateRoleAsync(UserId, UserRole.Admin);

        result.IsSuccess.ShouldBeTrue();
        userManagerMock.Verify(m => m.RemoveFromRoleAsync(TestAppUser, "User"), Times.Once);
        userManagerMock.Verify(m => m.AddToRoleAsync(TestAppUser, "Admin"), Times.Once);
    }

    [Fact]
    public async Task UpdateRoleAsync_ReturnsFailure_WhenUserNotFound()
    {
        var (repo, userManagerMock) = CreateRepository();
        userManagerMock.Setup(m => m.FindByIdAsync(UserId)).ReturnsAsync((AppUser?)null);

        var result = await repo.UpdateRoleAsync(UserId, UserRole.Admin);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("User not found.");
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsSuccess_WhenPasswordChanged()
    {
        var (repo, userManagerMock) = CreateRepository();
        userManagerMock.Setup(m => m.FindByIdAsync(UserId)).ReturnsAsync(TestAppUser);
        userManagerMock.Setup(m => m.RemovePasswordAsync(TestAppUser))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(m => m.AddPasswordAsync(TestAppUser, "NewP@ss1"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await repo.ChangePasswordAsync(UserId, "NewP@ss1");

        result.IsSuccess.ShouldBeTrue();
        userManagerMock.Verify(m => m.RemovePasswordAsync(TestAppUser), Times.Once);
        userManagerMock.Verify(m => m.AddPasswordAsync(TestAppUser, "NewP@ss1"), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsFailure_WhenUserNotFound()
    {
        var (repo, userManagerMock) = CreateRepository();
        userManagerMock.Setup(m => m.FindByIdAsync(UserId)).ReturnsAsync((AppUser?)null);

        var result = await repo.ChangePasswordAsync(UserId, "NewP@ss1");

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("User not found.");
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsFailure_WhenAddPasswordFails()
    {
        var (repo, userManagerMock) = CreateRepository();
        userManagerMock.Setup(m => m.FindByIdAsync(UserId)).ReturnsAsync(TestAppUser);
        userManagerMock.Setup(m => m.RemovePasswordAsync(TestAppUser))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(m => m.AddPasswordAsync(TestAppUser, "weak"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too short." }));

        var result = await repo.ChangePasswordAsync(UserId, "weak");

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldContain("Password too short.");
    }
}
