using MudBlazor;

namespace CleanserBlazorUI.Theme;

// Single source of truth for the app's visual identity, so every MudBlazor
// component (buttons, cards, data grids, charts) inherits the same palette
// instead of only the app bar/background being manually colored while
// everything else ran on MudBlazor's plain default theme.
//
// Direction: dark, dense "operations console" -- refined from the existing
// #001D23 / LightGreen.Accent3 combination already informally in use, but
// swapping the neon-lime accent (#76FF03) for a grounded teal-emerald.
// Neon-on-near-black reads as a generic/templated default; this keeps the
// same "dark + green" identity while making it feel like a deliberate
// choice for a credit bureau's data-operations tool.
public static class CleanserTheme
{
    public static readonly MudTheme Default = new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = "#1D9E75",           // teal-emerald brand accent -- buttons, links, active nav, chart primary series
            Secondary = "#4FD1C5",         // cyan, for secondary actions/highlights -- distinct enough from Primary to read separately
            Info = "#4FD1C5",
            Success = "#4CAF50",           // kept apart from Primary's teal-emerald so "success" stays legible as its own signal, not brand color
            Warning = "#E0A94E",
            Error = "#E0684E",

            Background = "#0D1B1D",
            Surface = "#132A2D",
            AppbarBackground = "#16232A",
            AppbarText = "#E7EDEA",
            DrawerBackground = "#132A2D",
            DrawerText = "#E7EDEA",
            DrawerIcon = "#93A29C",

            TextPrimary = "#E7EDEA",
            TextSecondary = "#93A29C",
            TextDisabled = "#5B6B67",

            ActionDefault = "#93A29C",
            ActionDisabled = "#3A4C48",

            Divider = "#22383B",
            LinesDefault = "#22383B",
            TableLines = "#22383B",
            TableStriped = "#12262A",
        },
        // Light palette kept as MudBlazor's default -- this app is built and
        // used as a dark-first tool (IsDark defaults to true in MainLayout),
        // so light mode is a fallback, not the primary identity to design.
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = new[] { "Segoe UI", "system-ui", "sans-serif" } }
        }
    };
}
