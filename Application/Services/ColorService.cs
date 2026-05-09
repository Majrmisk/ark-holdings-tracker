using Application.Interfaces;
using Domain;

namespace Application.Services;

public static class ColorService
{
    public static Colorscheme GetColorSchemeByRole(UserRole role)
    {
        return role switch
        {
            UserRole.SuperAdmin => GetSuperAdminColors(),
            UserRole.Admin => GetAdminColors(),
            UserRole.PowerUser => GetPowerUserColors(),
            UserRole.User => GetUserColors(),
            _ => new Colorscheme()
        };
    }
    
    public static Colorscheme GetSuperAdminColors()
    {
        return new Colorscheme(
            "bg-green-600 text-white",
            "bg-emerald-500 text-white",
            "bg-amber-500 text-white",
            "bg-red-600 text-white",
            "bg-gray-400 text-gray-900",
            "bg-blue-600 text-white");
    }

    public static Colorscheme GetAdminColors()
    {
        return new Colorscheme(
            "bg-green-500 text-white",
            "bg-emerald-400 text-white",
            "bg-amber-400 text-gray-900",
            "bg-red-500 text-white",
            "bg-gray-300 text-gray-800",
            "bg-blue-500 text-white");
    }

    public static Colorscheme GetPowerUserColors()
    {
        return new Colorscheme(
            "bg-green-400 text-green-900",
            "bg-emerald-300 text-emerald-900",
            "bg-amber-300 text-amber-900",
            "bg-red-400 text-red-900",
            "bg-gray-200 text-gray-700",
            "bg-blue-400 text-blue-900");
    }

    public static Colorscheme GetUserColors()
    {
        return new Colorscheme(
            "bg-green-200 text-green-800",
            "bg-emerald-200 text-emerald-800",
            "bg-amber-200 text-amber-800",
            "bg-red-200 text-red-800",
            "bg-gray-100 text-gray-600",
            "bg-blue-200 text-blue-800");
    }
}
