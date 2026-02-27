using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using Terranova.Terrain;
using Terranova.Buildings;
using Terranova.Camera;
using Terranova.UI;
using Terranova.Population;
using Terranova.Resources;
using Terranova.Input;
using Terranova.Discovery;
using Terranova.Orders;

namespace Terranova.Core
{
    /// <summary>
    /// Auto-creates all required game systems at runtime if they are missing.
    /// This avoids manual scene setup: just hit Play and everything works.
    ///
    /// Execution order is set early (-100) so systems exist before other scripts
    /// look for them. Each system is only created if not already present,
    /// so manual scene setup always takes priority.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public static class GameBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            // Subscribe to sceneLoaded so bootstrap runs on EVERY scene load,
            // not just the first. This is critical: when MainMenuUI calls
            // SceneManager.LoadScene the RuntimeInitializeOnLoadMethod callbacks
            // do NOT fire again, but sceneLoaded does.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            GameState.LaunchGameCallback = BootstrapGameSystems;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BootstrapAfterScene();
        }

        private static void BootstrapAfterScene()
        {
            // Show main menu on every fresh Play; skip once the player has started a game.
            // GameState.GameStarted is reset to false by ResetStatics (SubsystemRegistration)
            // and set to true by MainMenuUI before loading the game scene.
            if (!GameState.GameStarted)
            {
                EnsureMainMenu();
                EnsureEventSystem();
                Debug.Log("GameBootstrapper: Main menu ready.");
                return;
            }

            BootstrapGameSystems();
        }

        /// <summary>
        /// Public entry point called by MainMenuUI to create all game systems
        /// without requiring a scene reload. Safe to call multiple times —
        /// every Ensure* method is idempotent.
        /// </summary>
        public static void BootstrapGameSystems()
        {
            // Game scene bootstrap
            EnsureWorldManager();
            EnsureResourceManager();
            EnsureMaterialInventory(); // MS4: Material system
            EnsureBuildingRegistry();
            EnsureCamera();
            EnsureBuildingPlacer();
            EnsureUI();
            EnsureEventSystem();
            EnsureSettlerSpawner();
            EnsureResourceSpawner();
            EnsureResourceTaskAssigner();
            EnsureConstructionTaskAssigner();
            EnsureBuildingFunctionManager();
            EnsureDebugTerrainModifier();
            EnsureTerrainDecorator(); // v0.5.0: Trees, rocks, bushes, shelters
            EnsureFogOfWar(); // v0.5.1: Fog of war
            EnsureTrampledPaths(); // v0.5.1: Trampled paths
            EnsureTerrainDeformation(); // v0.5.1: Stumps, pits
            EnsureSelectionManager();
            EnsureDiscoverySystem();
            EnsureDayNightCycle(); // MS4: Day-night cycle
            EnsureSeasonManager(); // Feature 10: Seasons
            EnsureOrderSystem(); // Feature 7: Order grammar & Klappbuch
            EnsureChronicle(); // v0.5.10 Feature 12: Tribal Chronicle
            EnsureResourceStockpile(); // v0.5.12: Visual stockpile near campfire

            Debug.Log("GameBootstrapper: All systems ready.");
        }

        private static void EnsureWorldManager()
        {
            if (Object.FindFirstObjectByType<WorldManager>() != null)
                return;

            var go = new GameObject("World");
            go.AddComponent<WorldManager>();
            Debug.Log("GameBootstrapper: Created WorldManager.");
        }

        /// <summary>
        /// Registry of all buildable building types.
        /// Story 4.3: Gebäude-Typen Epoche I.1
        /// </summary>
        private static void EnsureBuildingRegistry()
        {
            if (BuildingRegistry.Instance != null)
                return;

            var go = new GameObject("BuildingRegistry");
            go.AddComponent<BuildingRegistry>();
            Debug.Log("GameBootstrapper: Created BuildingRegistry.");
        }

        /// <summary>
        /// Central resource storage. Must exist before UI and BuildingPlacer.
        /// Story 4.1: Baukosten-System
        /// </summary>
        private static void EnsureResourceManager()
        {
            if (ResourceManager.Instance != null)
                return;

            var go = new GameObject("ResourceManager");
            go.AddComponent<ResourceManager>();
            Debug.Log("GameBootstrapper: Created ResourceManager.");
        }

        private static void EnsureCamera()
        {
            if (Object.FindFirstObjectByType<RTSCameraController>() != null)
                return;

            var cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("GameBootstrapper: No Main Camera found.");
                return;
            }

            cam.gameObject.AddComponent<RTSCameraController>();
            Debug.Log("GameBootstrapper: Added RTSCameraController to Main Camera.");
        }

        private static void EnsureBuildingPlacer()
        {
            var placer = Object.FindFirstObjectByType<BuildingPlacer>();

            if (placer == null)
            {
                var go = new GameObject("GameManager");
                placer = go.AddComponent<BuildingPlacer>();
                Debug.Log("GameBootstrapper: Created BuildingPlacer.");
            }

            // Always ensure a building is assigned (even on manually-added placers)
            if (!placer.HasBuilding)
            {
                var campfire = ScriptableObject.CreateInstance<BuildingDefinition>();
                campfire.DisplayName = "Campfire";
                campfire.Description = "A simple campfire. The heart of your settlement.";
                campfire.Type = BuildingType.Campfire;
                campfire.WoodCost = 5;
                campfire.StoneCost = 0;
                campfire.FootprintSize = Vector2Int.one;
                campfire.PreviewColor = new Color(1f, 0.8f, 0.2f); // Warm yellow
                campfire.VisualHeight = 1f;

                placer.SetBuilding(campfire);
                Debug.Log("GameBootstrapper: Assigned default Campfire to BuildingPlacer.");
            }
        }

        private static void EnsureUI()
        {
            if (Object.FindFirstObjectByType<ResourceDisplay>() != null)
                return;

            var go = new GameObject("HUD");
            go.AddComponent<ResourceDisplay>();
            // Story 4.5: Build menu lives on the same Canvas
            go.AddComponent<BuildMenu>();
            // Story 6.1: Info panel for selection
            go.AddComponent<InfoPanel>();
            // Loading screen (auto-destroys after terrain generation)
            go.AddComponent<LoadingScreen>();
            // Render debug overlay for iPad grey screen diagnosis (temporary)
            go.AddComponent<RenderDebugOverlay>();
            // Game-state debug overlay (F3 toggle)
            go.AddComponent<DebugOverlay>();
            // Feature 7: Klappbuch and Order List UI (on same Canvas)
            go.AddComponent<KlappbuchUI>();
            go.AddComponent<OrderListUI>();
            // Feature 8.6: Discovery Log
            go.AddComponent<DiscoveryLogUI>();
            Debug.Log("GameBootstrapper: Created HUD with ResourceDisplay, BuildMenu, InfoPanel, LoadingScreen, RenderDebugOverlay, DebugOverlay, KlappbuchUI, OrderListUI, and DiscoveryLogUI.");
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
            Debug.Log("GameBootstrapper: Created EventSystem.");
        }

        private static void EnsureSettlerSpawner()
        {
            if (Object.FindFirstObjectByType<SettlerSpawner>() != null)
                return;

            var go = new GameObject("SettlerSpawner");
            go.AddComponent<SettlerSpawner>();
            Debug.Log("GameBootstrapper: Created SettlerSpawner.");
        }

        private static void EnsureResourceSpawner()
        {
            if (Object.FindFirstObjectByType<ResourceSpawner>() != null)
                return;

            var go = new GameObject("ResourceSpawner");
            go.AddComponent<ResourceSpawner>();
            Debug.Log("GameBootstrapper: Created ResourceSpawner.");
        }

        /// <summary>
        /// DEBUG ONLY - Click terrain to modify blocks (left=remove, right=add).
        /// Remove this when a proper terrain editing tool exists.
        /// Story 0.5: Terrain-Modifikation aktualisiert Mesh
        /// </summary>
        private static void EnsureDebugTerrainModifier()
        {
            if (Object.FindFirstObjectByType<DebugTerrainModifier>() != null)
                return;

            var go = new GameObject("DebugTerrainModifier");
            go.AddComponent<DebugTerrainModifier>();
            Debug.Log("GameBootstrapper: Created DebugTerrainModifier (left-click=remove, right-click=add).");
        }

        /// <summary>
        /// Automatic resource task assignment for idle settlers.
        /// Story 3.2: Sammel-Interaktion
        /// </summary>
        private static void EnsureResourceTaskAssigner()
        {
            if (Object.FindFirstObjectByType<ResourceTaskAssigner>() != null)
                return;

            var go = new GameObject("ResourceTaskAssigner");
            go.AddComponent<ResourceTaskAssigner>();
            Debug.Log("GameBootstrapper: Created ResourceTaskAssigner.");
        }

        /// <summary>
        /// Assigns idle settlers to unfinished construction sites.
        /// Story 4.2: Baufortschritt
        /// </summary>
        private static void EnsureConstructionTaskAssigner()
        {
            if (Object.FindFirstObjectByType<ConstructionTaskAssigner>() != null)
                return;

            var go = new GameObject("ConstructionTaskAssigner");
            go.AddComponent<ConstructionTaskAssigner>();
            Debug.Log("GameBootstrapper: Created ConstructionTaskAssigner.");
        }

        /// <summary>
        /// Manages building functions (worker assignment, housing capacity).
        /// Story 4.4: Gebäude-Funktion
        /// </summary>
        private static void EnsureBuildingFunctionManager()
        {
            if (Object.FindFirstObjectByType<BuildingFunctionManager>() != null)
                return;

            var go = new GameObject("BuildingFunctionManager");
            go.AddComponent<BuildingFunctionManager>();
            Debug.Log("GameBootstrapper: Created BuildingFunctionManager.");
        }

        /// <summary>
        /// v0.5.0: Terrain decoration (trees, rocks, bushes, ground variation, natural shelters).
        /// </summary>
        private static void EnsureTerrainDecorator()
        {
            if (Object.FindFirstObjectByType<TerrainDecorator>() != null)
                return;

            var go = new GameObject("TerrainDecorator");
            go.AddComponent<TerrainDecorator>();
            Debug.Log("GameBootstrapper: Created TerrainDecorator.");
        }

        /// <summary>
        /// v0.5.1: Fog of war — dark overlay cleared by settler exploration.
        /// </summary>
        private static void EnsureFogOfWar()
        {
            if (Object.FindFirstObjectByType<FogOfWar>() != null)
                return;

            var go = new GameObject("FogOfWar");
            go.AddComponent<FogOfWar>();
            Debug.Log("GameBootstrapper: Created FogOfWar.");
        }

        /// <summary>
        /// v0.5.1: Trampled paths — settlers create visible paths from repeated walking.
        /// </summary>
        private static void EnsureTrampledPaths()
        {
            if (Object.FindFirstObjectByType<TrampledPaths>() != null)
                return;

            var go = new GameObject("TrampledPaths");
            go.AddComponent<TrampledPaths>();
            Debug.Log("GameBootstrapper: Created TrampledPaths.");
        }

        /// <summary>
        /// v0.5.1: Terrain deformation — stumps from gathered trees, campfire clearing.
        /// </summary>
        private static void EnsureTerrainDeformation()
        {
            if (Object.FindFirstObjectByType<TerrainDeformation>() != null)
                return;

            var go = new GameObject("TerrainDeformation");
            go.AddComponent<TerrainDeformation>();
            Debug.Log("GameBootstrapper: Created TerrainDeformation.");
        }

        /// <summary>
        /// Selection manager for tap/long-press on settlers and buildings.
        /// Story 6.1–6.4: Selektion & Info-Panel
        /// </summary>
        private static void EnsureSelectionManager()
        {
            if (Object.FindFirstObjectByType<SelectionManager>() != null)
                return;

            var go = new GameObject("SelectionManager");
            go.AddComponent<SelectionManager>();
            Debug.Log("GameBootstrapper: Created SelectionManager.");
        }

        /// <summary>
        /// Discovery system: activity tracking, probability engine, state manager, registry.
        /// Story 1.1–1.5: Discovery Engine
        /// </summary>
        private static void EnsureDiscoverySystem()
        {
            if (Object.FindFirstObjectByType<DiscoveryEngine>() != null)
                return;

            var go = new GameObject("DiscoverySystem");
            go.AddComponent<ActivityTracker>();
            go.AddComponent<DiscoveryStateManager>();
            go.AddComponent<DiscoveryEngine>();
            go.AddComponent<DiscoveryPhaseManager>(); // v0.5.4: Phased discovery progression
            go.AddComponent<DiscoveryRegistry>();
            go.AddComponent<DiscoveryEffectsManager>();
            Debug.Log("GameBootstrapper: Created DiscoverySystem (ActivityTracker, StateManager, Engine, PhaseManager, Registry, EffectsManager).");
        }

        /// <summary>
        /// MS4 Feature 1.1: Main Menu UI.
        /// </summary>
        private static void EnsureMainMenu()
        {
            if (Object.FindFirstObjectByType<MainMenuUI>() != null)
                return;

            var go = new GameObject("MainMenu");
            go.AddComponent<MainMenuUI>();
            Debug.Log("GameBootstrapper: Created MainMenuUI.");
        }

        /// <summary>
        /// MS4 Feature 1.5: Day-Night Cycle.
        /// </summary>
        private static void EnsureDayNightCycle()
        {
            if (Object.FindFirstObjectByType<DayNightCycle>() != null)
                return;

            var go = new GameObject("DayNightCycle");
            go.AddComponent<DayNightCycle>();
            Debug.Log("GameBootstrapper: Created DayNightCycle.");
        }

        /// <summary>
        /// Feature 10: Season Manager.
        /// </summary>
        private static void EnsureSeasonManager()
        {
            if (Object.FindFirstObjectByType<SeasonManager>() != null)
                return;

            var go = new GameObject("SeasonManager");
            go.AddComponent<SeasonManager>();
            Debug.Log("GameBootstrapper: Created SeasonManager.");
        }

        /// <summary>
        /// Feature 7: Order Grammar & Klappbuch.
        /// OrderManager handles order lifecycle, OrderVocabulary tracks unlocked words.
        /// </summary>
        private static void EnsureOrderSystem()
        {
            if (OrderManager.Instance != null)
                return;

            var go = new GameObject("OrderSystem");
            go.AddComponent<OrderManager>();
            go.AddComponent<OrderVocabulary>();
            Debug.Log("GameBootstrapper: Created OrderSystem (OrderManager, OrderVocabulary).");
        }

        /// <summary>v0.5.10 Feature 12: Tribal Chronicle system.</summary>
        private static void EnsureChronicle()
        {
            if (ChronicleManager.Instance != null)
                return;

            var go = new GameObject("Chronicle");
            go.AddComponent<ChronicleManager>();

            // ChronicleUI needs a Canvas to render — attach to the HUD Canvas
            var hud = Object.FindFirstObjectByType<ResourceDisplay>();
            if (hud != null && ChronicleUI.Instance == null)
                hud.gameObject.AddComponent<ChronicleUI>();

            Debug.Log("GameBootstrapper: Created Chronicle (ChronicleManager, ChronicleUI).");
        }

        /// <summary>v0.5.12: Visual resource stockpiles near campfire.</summary>
        private static void EnsureResourceStockpile()
        {
            if (ResourceStockpileManager.Instance != null)
                return;

            var go = new GameObject("ResourceStockpile");
            go.AddComponent<ResourceStockpileManager>();
            Debug.Log("GameBootstrapper: Created ResourceStockpileManager.");
        }

        /// <summary>
        /// MS4 Feature 2: Material Inventory system.
        /// </summary>
        private static void EnsureMaterialInventory()
        {
            if (MaterialInventory.Instance != null)
                return;

            var go = new GameObject("MaterialInventory");
            go.AddComponent<MaterialInventory>();
            Debug.Log("GameBootstrapper: Created MaterialInventory.");
        }
    }
}
