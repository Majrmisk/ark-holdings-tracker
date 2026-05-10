using Application.Services;
using Domain;
using Shouldly;

namespace Tests.Application.Services;

public class ColorServiceTests
{
    private static void ShouldMatchColorscheme(Colorscheme actual, Colorscheme expected)
    {
        actual.New.ShouldBe(expected.New);
        actual.Increased.ShouldBe(expected.Increased);
        actual.Reduced.ShouldBe(expected.Reduced);
        actual.Removed.ShouldBe(expected.Removed);
        actual.Unchanged.ShouldBe(expected.Unchanged);
        actual.WeightShifted.ShouldBe(expected.WeightShifted);
    }

    [Fact]
    public void GetColorSchemeByRole_ReturnsSuperAdminColors_WhenRoleIsSuperAdmin()
    {
        var result = ColorService.GetColorSchemeByRole(UserRole.SuperAdmin);

        ShouldMatchColorscheme(result, ColorService.GetSuperAdminColors());
    }

    [Fact]
    public void GetColorSchemeByRole_ReturnsAdminColors_WhenRoleIsAdmin()
    {
        var result = ColorService.GetColorSchemeByRole(UserRole.Admin);

        ShouldMatchColorscheme(result, ColorService.GetAdminColors());
    }

    [Fact]
    public void GetColorSchemeByRole_ReturnsPowerUserColors_WhenRoleIsPowerUser()
    {
        var result = ColorService.GetColorSchemeByRole(UserRole.PowerUser);

        ShouldMatchColorscheme(result, ColorService.GetPowerUserColors());
    }

    [Fact]
    public void GetColorSchemeByRole_ReturnsUserColors_WhenRoleIsUser()
    {
        var result = ColorService.GetColorSchemeByRole(UserRole.User);

        ShouldMatchColorscheme(result, ColorService.GetUserColors());
    }

    [Fact]
    public void GetColorSchemeByRole_ReturnsDefaultColors_WhenRoleIsUnknown()
    {
        var result = ColorService.GetColorSchemeByRole((UserRole)99);

        ShouldMatchColorscheme(result, new Colorscheme());
    }
}
