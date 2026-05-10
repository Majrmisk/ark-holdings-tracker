using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Domain;
using Moq;
using Shouldly;

namespace Tests.Application.Services;

public class UserServiceTests
{
    private const string UserId = "user-1";
    private const string Username = "testuser";
    private const string Password = "P@ssw0rd";
    private const string AdminUserId = "admin-1";
    private const string AdminUsername = "admin";
    private const string SuperAdminUserId = "sa-1";
    private const string CallerUserId = "caller-1";

    private static readonly UserDto TestUser = new(UserId, Username, nameof(UserRole.User), DateTime.UtcNow);
    private static readonly UserDto AdminUser = new(AdminUserId, AdminUsername, nameof(UserRole.Admin), DateTime.UtcNow);
    private static readonly UserDto SuperAdminUser = new(SuperAdminUserId, "superadmin", nameof(UserRole.SuperAdmin), DateTime.UtcNow);
    private static readonly UserDto PowerUserDto = new("pu-1", "poweruser", nameof(UserRole.PowerUser), DateTime.UtcNow);

    private static (UserService Service, Mock<IUserRepository> RepoMock) CreateService()
    {
        var repoMock = new Mock<IUserRepository>();
        var service = new UserService(repoMock.Object);
        return (service, repoMock);
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsSuccessWithUsers_WhenUsersExist()
    {
        var (service, repoMock) = CreateService();
        var users = new List<UserDto> { TestUser, AdminUser };
        repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        var result = await service.GetAllUsersAsync();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(2);
        result.Value.ShouldContain(u => u.Id == UserId && u.UserName == Username);
        result.Value.ShouldContain(u => u.Id == AdminUserId && u.UserName == AdminUsername);
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsSuccessWithEmptyList_WhenNoUsersExist()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<UserDto>());

        var result = await service.GetAllUsersAsync();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsSuccessWithUser_WhenUserExists()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.GetByIdAsync(UserId)).ReturnsAsync(TestUser);

        var result = await service.GetUserByIdAsync(UserId);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe(UserId);
        result.Value.UserName.ShouldBe(Username);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsFailure_WhenUserNotFound()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.GetByIdAsync(UserId)).ReturnsAsync((UserDto?)null);

        var result = await service.GetUserByIdAsync(UserId);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("User not found.");
    }

    [Fact]
    public async Task CreateUserAsync_ReturnsFailure_WhenRoleIsSuperAdmin()
    {
        var (service, repoMock) = CreateService();

        var result = await service.CreateUserAsync(Username, Password, UserRole.SuperAdmin);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("Cannot create a SuperAdmin user.");
        repoMock.Verify(r => r.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserRole>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_DelegatesToRepository_WhenRoleIsNotSuperAdmin()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.CreateAsync(Username, Password, UserRole.User)).ReturnsAsync(Result.Success());

        var result = await service.CreateUserAsync(Username, Password, UserRole.User);

        result.IsSuccess.ShouldBeTrue();
        repoMock.Verify(r => r.CreateAsync(Username, Password, UserRole.User), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_ReturnsRepositoryFailure_WhenRepoFails()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.CreateAsync(Username, Password, UserRole.User))
            .ReturnsAsync(Result.Failure("Username already exists."));

        var result = await service.CreateUserAsync(Username, Password, UserRole.User);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("Username already exists.");
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFailure_WhenUserDeletesSelf()
    {
        var (service, repoMock) = CreateService();

        var result = await service.DeleteUserAsync(UserId, UserId);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("You cannot delete your own account.");
        repoMock.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
        repoMock.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFailure_WhenUserNotFound()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.GetByIdAsync(UserId)).ReturnsAsync((UserDto?)null);

        var result = await service.DeleteUserAsync(UserId, CallerUserId);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("User not found.");
        repoMock.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFailure_WhenTargetIsSuperAdmin()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.GetByIdAsync(SuperAdminUserId)).ReturnsAsync(SuperAdminUser);

        var result = await service.DeleteUserAsync(SuperAdminUserId, CallerUserId);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("SuperAdmin accounts cannot be deleted.");
        repoMock.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUserAsync_DelegatesToRepository_WhenAllChecksPass()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.GetByIdAsync(UserId)).ReturnsAsync(TestUser);
        repoMock.Setup(r => r.DeleteAsync(UserId)).ReturnsAsync(Result.Success());

        var result = await service.DeleteUserAsync(UserId, CallerUserId);

        result.IsSuccess.ShouldBeTrue();
        repoMock.Verify(r => r.DeleteAsync(UserId), Times.Once);
    }

    [Fact]
    public async Task UpdateRoleAsync_ReturnsFailure_WhenNewRoleIsSuperAdmin()
    {
        var (service, repoMock) = CreateService();

        var result = await service.UpdateRoleAsync(UserId, UserRole.SuperAdmin, UserRole.SuperAdmin);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("Cannot assign the SuperAdmin role.");
        repoMock.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
        repoMock.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateRoleAsync_ReturnsFailure_WhenUserNotFound()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.GetByIdAsync(UserId)).ReturnsAsync((UserDto?)null);

        var result = await service.UpdateRoleAsync(UserId, UserRole.Admin, UserRole.SuperAdmin);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("User not found.");
        repoMock.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateRoleAsync_ReturnsFailure_WhenTargetIsSuperAdmin()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.GetByIdAsync(SuperAdminUserId)).ReturnsAsync(SuperAdminUser);

        var result = await service.UpdateRoleAsync(SuperAdminUserId, UserRole.User, UserRole.SuperAdmin);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("Cannot modify a SuperAdmin account.");
        repoMock.Verify(r => r.UpdateRoleAsync(It.IsAny<string>(), It.IsAny<UserRole>()), Times.Never);
    }

    [Fact]
    public async Task UpdateRoleAsync_ReturnsFailure_WhenAdminManagesAdminAccount()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.GetByIdAsync(AdminUserId)).ReturnsAsync(AdminUser);

        var result = await service.UpdateRoleAsync(AdminUserId, UserRole.User, UserRole.Admin);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("Admins can only manage User and PowerUser accounts.");
        repoMock.Verify(r => r.UpdateRoleAsync(It.IsAny<string>(), It.IsAny<UserRole>()), Times.Never);
    }

    [Fact]
    public async Task UpdateRoleAsync_ReturnsFailure_WhenAdminAssignsAdminRole()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.GetByIdAsync(UserId)).ReturnsAsync(TestUser);

        var result = await service.UpdateRoleAsync(UserId, UserRole.Admin, UserRole.Admin);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("Admins can only assign User or PowerUser roles.");
        repoMock.Verify(r => r.UpdateRoleAsync(It.IsAny<string>(), It.IsAny<UserRole>()), Times.Never);
    }

    [Theory]
    [InlineData(UserRole.User)]
    [InlineData(UserRole.PowerUser)]
    public async Task UpdateRoleAsync_Succeeds_WhenAdminAssignsManageableRole(UserRole newRole)
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.GetByIdAsync(UserId)).ReturnsAsync(TestUser);
        repoMock.Setup(r => r.UpdateRoleAsync(UserId, newRole)).ReturnsAsync(Result.Success());

        var result = await service.UpdateRoleAsync(UserId, newRole, UserRole.Admin);

        result.IsSuccess.ShouldBeTrue();
        repoMock.Verify(r => r.UpdateRoleAsync(UserId, newRole), Times.Once);
    }

    [Fact]
    public async Task UpdateRoleAsync_Succeeds_WhenSuperAdminAssignsAdminRole()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.GetByIdAsync(UserId)).ReturnsAsync(TestUser);
        repoMock.Setup(r => r.UpdateRoleAsync(UserId, UserRole.Admin)).ReturnsAsync(Result.Success());

        var result = await service.UpdateRoleAsync(UserId, UserRole.Admin, UserRole.SuperAdmin);

        result.IsSuccess.ShouldBeTrue();
        repoMock.Verify(r => r.UpdateRoleAsync(UserId, UserRole.Admin), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsFailure_WhenUserNotFound()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.GetByIdAsync(UserId)).ReturnsAsync((UserDto?)null);

        var result = await service.ChangePasswordAsync(UserId, Password, UserRole.SuperAdmin);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("User not found.");
        repoMock.Verify(r => r.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsFailure_WhenTargetIsSuperAdmin()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.GetByIdAsync(SuperAdminUserId)).ReturnsAsync(SuperAdminUser);

        var result = await service.ChangePasswordAsync(SuperAdminUserId, Password, UserRole.SuperAdmin);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("Cannot change the password of a SuperAdmin account.");
        repoMock.Verify(r => r.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsFailure_WhenAdminTargetsNonManageableRole()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.GetByIdAsync(AdminUserId)).ReturnsAsync(AdminUser);

        var result = await service.ChangePasswordAsync(AdminUserId, Password, UserRole.Admin);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("Admins can only manage User and PowerUser accounts.");
        repoMock.Verify(r => r.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_Succeeds_WhenCallerIsSuperAdmin()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.GetByIdAsync(UserId)).ReturnsAsync(TestUser);
        repoMock.Setup(r => r.ChangePasswordAsync(UserId, Password)).ReturnsAsync(Result.Success());

        var result = await service.ChangePasswordAsync(UserId, Password, UserRole.SuperAdmin);

        result.IsSuccess.ShouldBeTrue();
        repoMock.Verify(r => r.ChangePasswordAsync(UserId, Password), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_Succeeds_WhenAdminTargetsManageableRole()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.GetByIdAsync(UserId)).ReturnsAsync(TestUser);
        repoMock.Setup(r => r.ChangePasswordAsync(UserId, Password)).ReturnsAsync(Result.Success());

        var result = await service.ChangePasswordAsync(UserId, Password, UserRole.Admin);

        result.IsSuccess.ShouldBeTrue();
        repoMock.Verify(r => r.ChangePasswordAsync(UserId, Password), Times.Once);
    }
}
