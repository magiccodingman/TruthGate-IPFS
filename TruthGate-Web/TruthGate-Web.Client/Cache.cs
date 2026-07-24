using MudBlazor;

namespace TruthGate_Web.Client
{
    public static class Cache
    {
        public static MudTheme CustomTheme = new()
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#2D2A5A",
                Secondary = "#00E3C0",
                Black = "#110e2d",
                AppbarText = "#424242",
                AppbarBackground = "rgba(255,255,255,0.8)",
                DrawerBackground = "#ffffff",
                GrayLight = "#e8e8e8",
                GrayLighter = "#f9f9f9",
            },

            PaletteDark = new PaletteDark
            {
                Primary = "#00E3C0",
                PrimaryContrastText = "#000000",
                Secondary = "#3C3970",
                Background = "#1E1F22",
                Surface = "#2B2D31",
                DrawerBackground = "#232428",
                AppbarBackground = "#1B1C1F",
                AppbarText = "#E0E0E0",
                TextPrimary = "#FFFFFF",
                TextSecondary = "#A3A6AA",
                ActionDefault = "#00E3C0",
                Divider = "#383A40"
            },

            LayoutProperties = new LayoutProperties()
        };
    }
}