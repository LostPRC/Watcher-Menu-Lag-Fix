using System;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace WatcherMenuLagFix
{
    public class WatcherMenuFixOptions : OptionInterface
    {
        public static Configurable<string> cfgMode;
        public static Configurable<bool> cfgDisableDepthRaycast;
        public static Configurable<string> cfgMenuTheme;

        public WatcherMenuFixOptions()
        {
            cfgMode = config.Bind("cfgMode", "Optimized3D", new ConfigurableInfo(
                "Optimization mode:\n'Optimized 3D': Keeps all 12 parallax layers, camera drift, and lively 3D movement intact, but eliminates the 40-sample blur loop and overdraw lag (Smooth 60 FPS).\n'Flat 2D': Uses the single official 2D artwork (Max power saving).",
                new ConfigAcceptableList<string>("Optimized3D", "Flat2D"), "", "Rendering Mode"));

            cfgDisableDepthRaycast = config.Bind("cfgDisableDepthRaycast", true, new ConfigurableInfo(
                "Bypasses per-frame CPU texture.GetPixel scans during mouse movement in the main menu, eliminating cursor hitching and micro-stutters.",
                null, "", "Disable Hover Depth Raycast"));

            cfgMenuTheme = config.Bind("cfgMenuTheme", "Watcher", new ConfigurableInfo(
                "Select which background theme to display on the main menu.",
                new ConfigAcceptableList<string>("Watcher", "Downpour", "Vanilla"), "", "Main Menu Theme"));
        }

        public override void Initialize()
        {
            base.Initialize();

            Tabs = new OpTab[]
            {
                new OpTab(this, "General")
            };

            OpTab tab = Tabs[0];

            float y = 540f;
            float leftX = 40f;

            // Title Header
            OpLabel titleLabel = new OpLabel(leftX, y, "Watcher Menu Lag Fix Settings", true);
            y -= 35f;

            OpLabel subTitle = new OpLabel(leftX, y, "Fixes Watcher DLC main menu performance while keeping the lively 3D parallax effects intact.", false);
            y -= 45f;

            // 1. Rendering Mode Selector
            OpLabel lblMode = new OpLabel(leftX, y, "Performance Mode:", false);
            OpComboBox cmbMode = new OpComboBox(cfgMode, new Vector2(leftX + 170f, y - 4f), 160f, new string[] { "Optimized3D", "Flat2D" });
            y -= 25f;

            OpLabel descMode = new OpLabel(leftX, y, "• Optimized3D (Default): Keeps 100% of the 3D parallax, camera drift & interactive liveliness.\n• Flat2D: Uses official static 2D artwork for ultra-low power devices.", false)
            {
                color = new Color(0.7f, 0.7f, 0.7f)
            };
            y -= 55f;

            // 2. Depth Raycast Option
            OpCheckBox chkDepth = new OpCheckBox(cfgDisableDepthRaycast, new Vector2(leftX, y));
            OpLabel lblDepth = new OpLabel(leftX + 35f, y + 2f, "Disable Mouse Hover Depth Raycast (texture.GetPixel)", false);
            OpLabel descDepth = new OpLabel(leftX + 35f, y - 18f, "Stops the engine from scanning raw image pixels on CPU every frame when mouse moves.", false)
            {
                color = new Color(0.7f, 0.7f, 0.7f)
            };
            y -= 60f;

            // 3. Theme Selector
            OpLabel lblTheme = new OpLabel(leftX, y, "Main Menu Theme:", false);
            OpComboBox cmbTheme = new OpComboBox(cfgMenuTheme, new Vector2(leftX + 170f, y - 4f), 160f, new string[] { "Watcher", "Downpour", "Vanilla" });
            y -= 25f;

            OpLabel descTheme = new OpLabel(leftX, y, "Override menu background between Watcher, Downpour, or Classic Vanilla.", false)
            {
                color = new Color(0.7f, 0.7f, 0.7f)
            };

            tab.AddItems(new UIelement[]
            {
                titleLabel,
                subTitle,
                lblMode,
                cmbMode,
                descMode,
                chkDepth,
                lblDepth,
                descDepth,
                lblTheme,
                cmbTheme,
                descTheme
            });
        }
    }
}
