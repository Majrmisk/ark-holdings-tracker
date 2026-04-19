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
        return new Colorscheme()
        {
            New = "bg-green-600 text-white",
            Increased = "bg-emerald-500 text-white",
            Reduced = "bg-amber-500 text-white",
            Removed = "bg-red-600 text-white",
            Unchanged = "bg-gray-400 text-gray-900",
            WeightShifted = "bg-blue-600 text-white",
        };
    }

    public static Colorscheme GetAdminColors()
    {
        return new Colorscheme()
        {
            New = "bg-green-500 text-white",
            Increased = "bg-emerald-400 text-white",
            Reduced = "bg-amber-400 text-gray-900",
            Removed = "bg-red-500 text-white",
            Unchanged = "bg-gray-300 text-gray-800",
            WeightShifted = "bg-blue-500 text-white",
        };
    }

    public static Colorscheme GetPowerUserColors()
    {
        return new Colorscheme()
        {
            New = "bg-green-400 text-green-900",
            Increased = "bg-emerald-300 text-emerald-900",
            Reduced = "bg-amber-300 text-amber-900",
            Removed = "bg-red-400 text-red-900",
            Unchanged = "bg-gray-200 text-gray-700",
            WeightShifted = "bg-blue-400 text-blue-900",
        };
    }

    public static Colorscheme GetUserColors()
    {
        return new Colorscheme()
        {
            New = "bg-green-200 text-green-800",
            Increased = "bg-emerald-200 text-emerald-800",
            Reduced = "bg-amber-200 text-amber-800",
            Removed = "bg-red-200 text-red-800",
            Unchanged = "bg-gray-100 text-gray-600",
            WeightShifted = "bg-blue-200 text-blue-800",
        };
    }
}