﻿using System;
using ColossalFramework.UI;
using UnityEngine;

namespace ModTools
{

    public class ModTools : GUIWindow
    {
        public SimulationManager.UpdateMode updateMode = SimulationManager.UpdateMode.Undefined;

#if DEBUG
        public static readonly bool DEBUG_MODTOOLS = true;
#else
        public static readonly bool DEBUG_MODTOOLS = false;
#endif
        private static readonly object LoggingLock = new object();

        private Vector2 mainScroll = Vector2.zero;

        public Console console;
        public SceneExplorer sceneExplorer;
        private DebugRenderer debugRenderer;
        public SceneExplorerColorConfig sceneExplorerColorConfig;

        public ScriptEditor scriptEditor;

        public Watches watches;
        public ColorPicker colorPicker;

        private GamePanelExtender panelExtender;

        public Configuration config = new Configuration();
        public static readonly string configPath = "ModToolsConfig.xml";

        private static ModTools instance = null;

        public void OnUnityDestroyCallback()
        {

            Destroy(console);
            Destroy(sceneExplorer);
            Destroy(sceneExplorerColorConfig);
            Destroy(scriptEditor);
            Destroy(watches);
            Destroy(panelExtender);
            Destroy(colorPicker);

            CustomPrefabs.Revert();

            instance = null;
        }

        public static ModTools Instance
        {
            get
            {
                instance = instance ?? FindObjectOfType<ModTools>();
                return instance;
            }
        }

        public void LoadConfig()
        {
            config = Configuration.Deserialize(configPath);
            if (config == null)
            {
                config = new Configuration();
                SaveConfig();
            }

            if (console != null)
            {
                console.rect = config.consoleRect;
                console.visible = config.consoleVisible;
            }

            watches.rect = config.watchesRect;
            watches.visible = config.watchesVisible;

            sceneExplorer.rect = config.sceneExplorerRect;
            sceneExplorer.visible = config.sceneExplorerVisible;

            if (sceneExplorer.visible)
            {
                sceneExplorer.Refresh();
            }

            scriptEditor.ReloadProjectWorkspace();
        }

        public void SaveConfig()
        {
            if (config != null)
            {
                if (console != null)
                {
                    config.consoleRect = console.rect;
                    config.consoleVisible = console.visible;
                }

                config.watchesRect = watches.rect;
                config.watchesVisible = watches.visible;

                config.sceneExplorerRect = sceneExplorer.rect;
                config.sceneExplorerVisible = sceneExplorer.visible;

                Configuration.Serialize(configPath, config);
            }
        }

        public ModTools() : base("Mod Tools", new Rect(128, 128, 356, 290), skin)
        {
            onDraw = DoMainWindow;
            onUnityDestroy = OnUnityDestroyCallback;
            resizable = false;
        }

        private static bool loggingInitialized = false;

        public void Initialize(SimulationManager.UpdateMode _updateMode)
        {
            updateMode = _updateMode;

            if (!loggingInitialized)
            {
                Application.logMessageReceivedThreaded += OnApplicationOnLogMessageReceived;

                loggingInitialized = true;
            }

            sceneExplorer = gameObject.AddComponent<SceneExplorer>();
            watches = gameObject.AddComponent<Watches>();
            colorPicker = gameObject.AddComponent<ColorPicker>();
            scriptEditor = gameObject.AddComponent<ScriptEditor>();
            scriptEditor.visible = false;
            sceneExplorerColorConfig = gameObject.AddComponent<SceneExplorerColorConfig>();

            LoadConfig();

            //TODO(earalov): replace numbers with enum values
            if (config.extendGamePanels && (updateMode == (SimulationManager.UpdateMode)2 || updateMode == (SimulationManager.UpdateMode)11 || updateMode == SimulationManager.UpdateMode.LoadGame))
            {
                panelExtender = gameObject.AddComponent<GamePanelExtender>();
            }

            if (config.useModToolsConsole)
            {
                console = gameObject.AddComponent<Console>();
            }

            if (updateMode != SimulationManager.UpdateMode.Undefined && config.customPrefabsObject)
            {
                CustomPrefabs.Bootstrap();
            }
        }

        private void OnApplicationOnLogMessageReceived(string condition, string trace, LogType type)
        {
            lock (LoggingLock)
            {
                if (!config.hookUnityLogging)
                {
                    return;
                }
                if (type == LogType.Exception)
                {
                    var message = condition;
                    if (config.logExceptionsToConsole)
                    {
                        if (trace != null)
                        {
                            message = $"{message}\n\n{trace}";
                        }
                    }
                    Log.Error(message);
                }
                else if (type == LogType.Error || type == LogType.Assert)
                {
                    Log.Error(condition);
                }
                else if (type == LogType.Warning)
                {
                    Log.Warning(condition);
                }
                else
                {
                    Log.Message(condition);
                }
            }
        }

        void Update()
        {
            UpdateMouseScrolling();

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Q))
            {
                visible = !visible;
            }

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.E))
            {
                sceneExplorer.visible = !sceneExplorer.visible;
                if (sceneExplorer.visible)
                {
                    sceneExplorer.Refresh();
                }
            }

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
            {
                if (debugRenderer == null)
                {
                    debugRenderer = GameObject.FindObjectOfType<UIView>().gameObject.AddComponent<DebugRenderer>();
                }
                debugRenderer.drawDebugInfo = !debugRenderer.drawDebugInfo;
            }

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.W))
            {
                watches.visible = !watches.visible;
            }

            if (config.useModToolsConsole && Input.GetKeyDown(KeyCode.F7))
            {
                console.visible = !console.visible;
            }

            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                scriptEditor.visible = !scriptEditor.visible;
            }
        }

        void DoMainWindow()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Use ModTools console");
            var newUseConsole = GUILayout.Toggle(config.useModToolsConsole, "");
            GUILayout.EndHorizontal();

            if (newUseConsole != config.useModToolsConsole)
            {
                config.useModToolsConsole = newUseConsole;

                if (config.useModToolsConsole)
                {
                    console = gameObject.AddComponent<Console>();
                }
                else
                {
                    Destroy(console);
                    console = null;
                }

                SaveConfig();
            }

            if (!config.useModToolsConsole)
            {
                GUI.enabled = false;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hook Unity's logging");
            var newHookLogging = GUILayout.Toggle(config.hookUnityLogging, "");
            GUILayout.EndHorizontal();

            if (newHookLogging != config.hookUnityLogging)
            {
                config.hookUnityLogging = newHookLogging;
                SaveConfig();
            }

            GUI.enabled = true;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Log exceptions to console");
            var newLogExceptionsToConsole = GUILayout.Toggle(config.logExceptionsToConsole, "");
            GUILayout.EndHorizontal();
            if (newLogExceptionsToConsole != config.logExceptionsToConsole)
            {
                config.logExceptionsToConsole = newLogExceptionsToConsole;
                SaveConfig();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Game panel extensions");
            var newExtendGamePanels = GUILayout.Toggle(config.extendGamePanels, "");
            GUILayout.EndHorizontal();

            if (newExtendGamePanels != config.extendGamePanels)
            {
                config.extendGamePanels = newExtendGamePanels;
                SaveConfig();

                if (config.extendGamePanels)
                {
                    gameObject.AddComponent<GamePanelExtender>();
                }
                else
                {
                    Destroy(gameObject.GetComponent<GamePanelExtender>());
                }
            }

            GUILayout.BeginHorizontal();
            if (debugRenderer == null)
            {
                debugRenderer = GameObject.FindObjectOfType<UIView>().gameObject.AddComponent<DebugRenderer>();
            }
            GUILayout.Label("Debug Renderer (Ctrl+R)");
            debugRenderer.drawDebugInfo = GUILayout.Toggle(debugRenderer.drawDebugInfo, "");
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            GUILayout.Label("Custom Prefabs Object");
            var customPrefabsObject = GUILayout.Toggle(config.customPrefabsObject, "");
            GUILayout.EndHorizontal();
            if (customPrefabsObject != config.customPrefabsObject)
            {
                config.customPrefabsObject = customPrefabsObject;
                if (config.customPrefabsObject && updateMode != SimulationManager.UpdateMode.Undefined)
                {
                    CustomPrefabs.Bootstrap();
                }
                else if (updateMode != SimulationManager.UpdateMode.Undefined)
                {
                    CustomPrefabs.Revert();
                }
                SaveConfig();
            }

            if (GUILayout.Button("Debug console (F7)"))
            {
                if (console != null)
                {
                    console.visible = true;
                }
                else
                {
                    var debugOutputPanel = GameObject.Find("(Library) DebugOutputPanel").GetComponent<DebugOutputPanel>();
                    debugOutputPanel.enabled = true;
                    debugOutputPanel.GetComponent<UIPanel>().isVisible = true;
                }
            }

            if (GUILayout.Button("Watches (Ctrl+W)"))
            {
                watches.visible = !watches.visible;
            }

            if (GUILayout.Button("Scene explorer (Ctrl+E)"))
            {
                sceneExplorer.visible = !sceneExplorer.visible;
                if (sceneExplorer.visible)
                {
                    sceneExplorer.Refresh();
                }
            }

            if (GUILayout.Button("Script editor (Ctrl+`)"))
            {
                scriptEditor.visible = !scriptEditor.visible;
            }
        }
    }

}
