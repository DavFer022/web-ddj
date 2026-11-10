using MudBlazor;

namespace SistemaDDJ.Web.Components.Theme;

public static class DdjTheme
{
    public static readonly MudTheme Value = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0F4C81",
            PrimaryContrastText = "#FFFFFF",
            Secondary = "#D81B60",
            SecondaryContrastText = "#FFFFFF",
            Tertiary = "#00897B",
            Background = "#F4F7FB",
            Surface = "#FFFFFF",
            AppbarBackground = "#0F4C81",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#0B1F33",
            DrawerText = "#E8EEF7",
            DrawerIcon = "#9FB6D4",
            TextPrimary = "#182433",
            TextSecondary = "#5B6B7F",
            Info = "#0277BD",
            Success = "#2E7D32",
            Warning = "#ED6C02",
            Error = "#C62828",
            ActionDefault = "#49657F"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#64B5F6",
            Secondary = "#F06292",
            Tertiary = "#4DB6AC",
            Background = "#0E1621",
            Surface = "#162334",
            AppbarBackground = "#0B1F33",
            DrawerBackground = "#081624",
            DrawerText = "#E8EEF7",
            TextPrimary = "#EEF4FB",
            TextSecondary = "#B0C0D2",
            Info = "#4FC3F7",
            Success = "#81C784",
            Warning = "#FFB74D",
            Error = "#EF9A9A"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px",
            AppbarHeight = "64px"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "Roboto", "Helvetica", "Arial", "sans-serif"]
            }
        }
    };
}
