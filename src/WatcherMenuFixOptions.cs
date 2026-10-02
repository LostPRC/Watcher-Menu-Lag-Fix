using System;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace WatcherMenuLagFix
{
    public class WatcherMenuFixOptions : OptionInterface
    {
        public static Configurable<bool> cfgUseFlatBackground;
        public static Configurable<bool> cfgDisableDepthRaycast;
        public static Configurable<string> cfgMenuTheme;

        public WatcherMenuFixOptions()
        {
            cfgUseFlatBackground = config.Bind("cfgUseFlatBackground", true, new ConfigurableInfo(
                "When enabled, uses the official high-resolution 2D flat artwork for the Watcher main menu instead of 12 stacked 3D shader layers. Resolves severe menu FPS drops.",
                null, "", "Use 2D Flat Background"));

            cfgDisableDepthRaycast = config.Bind("cfgDisableDepthRaycast", true, new ConfigurableInfo(
                "Bypasses per-frame CPU texture pixel scanning on mouse hover in the main menu, preventing micro-stutters and input lag.",
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

            OpLabel subTitle = new OpLabel(leftX, y, "Fixes frame rate drops and micro-stutters on the main menu when the Watcher DLC is loaded.", false);
            y -= 45f;

            // 1. Flat Background Option
            OpCheckBox chkFlat = new OpCheckBox(cfgUseFlatBackground, new Vector2(leftX, y));
            OpLabel lblFlat = new OpLabel(leftX + 35f, y + 2f, "Use Flat Watcher Background (Recommended)", false);
            OpLabel descFlat = new OpLabel(leftX + 35f, y - 18f, "Renders official 2D artwork. Completely eliminates the heavy 12-layer shader lag (60+ FPS).", false)
            {
                color = new Color(0.7f, 0.7f, 0.7f)
            };
            y -= 55f;

            // 2. Depth Raycast Option
            OpCheckBox chkDepth = new OpCheckBox(cfgDisableDepthRaycast, new Vector2(leftX, y));
            OpLabel lblDepth = new OpLabel(leftX + 35f, y + 2f, "Disable Mouse Hover Depth Raycast (texture.GetPixel)", false);
            OpLabel descDepth = new OpLabel(leftX + 35f, y - 18f, "Stops the engine from scanning raw image pixels every frame on mouse movement.", false)
            {
                color = new Color(0.7f, 0.7f, 0.7f)
            };
            y -= 60f;

            // 3. Theme Selector
            OpLabel lblTheme = new OpLabel(leftX, y, "Force Menu Background Theme:", false);
            OpComboBox cmbTheme = new OpComboBox(cfgMenuTheme, new Vector2(leftX + 220f, y - 4f), 140f, new string[] { "Watcher", "Downpour", "Vanilla" });
            y -= 25f;

            OpLabel descTheme = new OpLabel(leftX, y, "Choose between Watcher, Downpour, or Classic Vanilla backgrounds.", false)
            {
                color = new Color(0.7f, 0.7f, 0.7f)
            };

            tab.AddItems(new UIelement[]
            {
                titleLabel,
                subTitle,
                chkFlat,
                lblFlat,
                descFlat,
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
