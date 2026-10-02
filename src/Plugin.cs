using System;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Logging;
using Menu;
using UnityEngine;

// Security permission flags standard for Rain World BepInEx plugins
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace WatcherMenuLagFix
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.rainworld.watchermenulagfix";
        public const string PLUGIN_NAME = "Watcher Menu Lag Fix";
        public const string PLUGIN_VERSION = "1.0.0";
        public const string MOD_ID = "watchermenulagfix";

        public static new ManualLogSource Logger;
        private static WatcherMenuFixOptions optionsInstance;
        private bool isInitialized = false;

        private void OnEnable()
        {
            Logger = base.Logger;
            optionsInstance = new WatcherMenuFixOptions();

            // Hook Mod initialization for Remix OptionInterface
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;

            // Hook Watcher Main Menu Scene building
            On.Menu.MenuScene.BuildWatcherMainMenuScene += MenuScene_BuildWatcherMainMenuScene;

            // Hook Depth raycast to eliminate CPU pixel reading stutter
            On.Menu.MenuDepthIllustration.DepthAtPosition += MenuDepthIllustration_DepthAtPosition;

            // Hook Menu Scene creation for theme override
            On.Menu.InteractiveMenuScene.ctor += InteractiveMenuScene_ctor;

            Logger.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} initialized successfully!");
        }

        private void OnDisable()
        {
            On.RainWorld.OnModsInit -= RainWorld_OnModsInit;
            On.Menu.MenuScene.BuildWatcherMainMenuScene -= MenuScene_BuildWatcherMainMenuScene;
            On.Menu.MenuDepthIllustration.DepthAtPosition -= MenuDepthIllustration_DepthAtPosition;
            On.Menu.InteractiveMenuScene.ctor -= InteractiveMenuScene_ctor;
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);

            if (!isInitialized)
            {
                isInitialized = true;
                try
                {
                    MachineConnector.SetRegisteredOI(MOD_ID, optionsInstance);
                    Logger.LogInfo("Registered WatcherMenuFixOptions with Remix MachineConnector.");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error registering OptionInterface: {ex}");
                }
            }
        }

        /// <summary>
        /// Fixes heavy multi-pass shader lag by enabling 2D Flat Mode for the Watcher Main Menu.
        /// Rain World already contains official high-res 2D flat artwork for the Watcher menu.
        /// </summary>
        private void MenuScene_BuildWatcherMainMenuScene(On.Menu.MenuScene.orig_BuildWatcherMainMenuScene orig, MenuScene self)
        {
            if (WatcherMenuFixOptions.cfgUseFlatBackground != null && WatcherMenuFixOptions.cfgUseFlatBackground.Value)
            {
                bool originalFlatMode = self.flatMode;
                self.flatMode = true;
                try
                {
                    orig(self);
                    Logger.LogDebug("Built Watcher Main Menu with high-performance 2D Flat Mode.");
                }
                finally
                {
                    self.flatMode = originalFlatMode;
                }
            }
            else
            {
                orig(self);
            }
        }

        /// <summary>
        /// Prevents per-frame CPU texture.GetPixel calls across 12 massive textures during mouse hover,
        /// which otherwise causes severe micro-stuttering and cache thrashing on Linux/Wine.
        /// </summary>
        private float MenuDepthIllustration_DepthAtPosition(On.Menu.MenuDepthIllustration.orig_DepthAtPosition orig, MenuDepthIllustration self, Vector2 ps, bool devtool)
        {
            if (!devtool && WatcherMenuFixOptions.cfgDisableDepthRaycast != null && WatcherMenuFixOptions.cfgDisableDepthRaycast.Value)
            {
                if (self.owner is MenuScene scene && scene.sceneID != null)
                {
                    string sceneName = scene.sceneID.value;
                    if (sceneName == "MainMenu_Watcher" || sceneName == "MainMenu" || sceneName == "MainMenu_Downpour")
                    {
                        // Return layer depth directly without reading texture pixels from CPU memory
                        return self.depth;
                    }
                }
            }

            return orig(self, ps, devtool);
        }

        /// <summary>
        /// Allows user to optionally force Downpour or Vanilla background theme if desired.
        /// </summary>
        private void InteractiveMenuScene_ctor(On.Menu.InteractiveMenuScene.orig_ctor orig, InteractiveMenuScene self, Menu.Menu menu, MenuObject owner, MenuScene.SceneID sceneID)
        {
            if (menu is MainMenu && WatcherMenuFixOptions.cfgMenuTheme != null)
            {
                string theme = WatcherMenuFixOptions.cfgMenuTheme.Value;
                if (theme == "Downpour")
                {
                    sceneID = MenuScene.SceneID.MainMenu_Downpour;
                }
                else if (theme == "Vanilla")
                {
                    sceneID = MenuScene.SceneID.MainMenu;
                }
            }

            orig(self, menu, owner, sceneID);
        }
    }
}
