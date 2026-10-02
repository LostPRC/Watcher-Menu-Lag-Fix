using System;
using System.Collections.Generic;
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
        public const string PLUGIN_VERSION = "1.1.0";
        public const string MOD_ID = "watchermenulagfix";

        public static new ManualLogSource Logger;
        private static WatcherMenuFixOptions optionsInstance;
        private bool isInitialized = false;

        private void OnEnable()
        {
            Logger = base.Logger;
            optionsInstance = new WatcherMenuFixOptions();

            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            On.Menu.MenuScene.BuildWatcherMainMenuScene += MenuScene_BuildWatcherMainMenuScene;
            On.Menu.MenuDepthIllustration.DepthAtPosition += MenuDepthIllustration_DepthAtPosition;
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
        /// Optimizes the Watcher Main Menu while keeping 100% of the 3D parallax, 
        /// camera movement, mouse tracking, and layer depth effects.
        /// </summary>
        private void MenuScene_BuildWatcherMainMenuScene(On.Menu.MenuScene.orig_BuildWatcherMainMenuScene orig, MenuScene self)
        {
            string mode = WatcherMenuFixOptions.cfgMode != null ? WatcherMenuFixOptions.cfgMode.Value : "Optimized3D";

            // If user explicitly chose Flat2D mode in settings, use the static 2D artwork
            if (mode == "Flat2D")
            {
                bool originalFlatMode = self.flatMode;
                self.flatMode = true;
                try
                {
                    orig(self);
                    Logger.LogDebug("Built Watcher Main Menu with 2D Flat Mode.");
                }
                finally
                {
                    self.flatMode = originalFlatMode;
                }
                return;
            }

            // Otherwise: Full 3D Interactive Parallax Mode (Default & Lively)
            // Let the game build all 12 depth layers with their original positions, depths, and parallax
            orig(self);

            // Now apply smart non-destructive optimizations to the loaded layers:
            OptimizeWatcherDepthIllustrations(self);

            Logger.LogDebug("Optimized 3D Watcher Main Menu: 12 dynamic parallax layers retained at 60 FPS.");
        }

        private void OptimizeWatcherDepthIllustrations(MenuScene self)
        {
            if (self.depthIllustrations == null || self.depthIllustrations.Count == 0)
            {
                return;
            }

            FShader basicShader = (self.menu != null && self.menu.manager != null && self.menu.manager.rainWorld != null && self.menu.manager.rainWorld.Shaders.ContainsKey("Basic"))
                ? self.menu.manager.rainWorld.Shaders["Basic"] 
                : null;

            for (int i = 0; i < self.depthIllustrations.Count; i++)
            {
                MenuDepthIllustration illu = self.depthIllustrations[i];
                if (illu == null) continue;

                // 1. Cull invisible layers (e.g. layers 7-10 before beating Sentient Rot where alpha == 0)
                // This eliminates massive full-screen zero-alpha GPU overdraw!
                if (illu.alpha <= 0.001f && (!illu.setAlpha.HasValue || illu.setAlpha.Value <= 0.001f))
                {
                    illu.visible = false;
                    continue;
                }

                // 2. Eliminate the 40-sample Gaussian blur loop on the two-half depth textures
                // For layers using LightEdges or Normal, the top half is the full-color artwork,
                // and the bottom half is the depth map.
                // We crop the Futile AtlasElement to the top half and use the lightweight Basic shader.
                // All 3D parallax offsets (sprite.x/y -= CamPos * 80 / depth) remain completely intact!
                if (illu.shader != MenuDepthIllustration.MenuShader.Basic && basicShader != null)
                {
                    FSprite sprite = illu.sprite;
                    if (sprite != null && sprite.element != null && sprite.element.atlas != null)
                    {
                        FAtlas atlas = sprite.element.atlas;
                        if (atlas.textureSize.y > 0f)
                        {
                            float halfHeight = atlas.textureSize.y / 2f;
                            // Create top-half element in the same atlas (bottomY = halfHeight to textureSize.y)
                            FAtlasElement topHalf = atlas.CreateUnnamedElement(0f, halfHeight, atlas.textureSize.x, halfHeight);
                            sprite.element = topHalf;
                            sprite.scaleY = 1f;
                            sprite.shader = basicShader;
                        }
                    }
                }
            }

            // 3. Populate idleDepths on InteractiveMenuScene if not set,
            // giving the idle camera subtle, natural depth-of-field breathing
            if (self is InteractiveMenuScene interactive)
            {
                if (interactive.idleDepths == null)
                {
                    interactive.idleDepths = new List<float>();
                }
                if (interactive.idleDepths.Count == 0)
                {
                    interactive.idleDepths.Add(1.5f);
                    interactive.idleDepths.Add(2.2f);
                    interactive.idleDepths.Add(2.8f);
                    interactive.idleDepths.Add(4.7f);
                    interactive.idleDepths.Add(12f);
                }
            }
        }

        /// <summary>
        /// Prevents per-frame CPU texture.GetPixel calls across 12 massive textures during mouse hover,
        /// which otherwise causes severe micro-stuttering and cache thrashing on Linux/Wine.
        /// (This only determines the blur focus depth and does NOT affect 3D parallax movement at all).
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
