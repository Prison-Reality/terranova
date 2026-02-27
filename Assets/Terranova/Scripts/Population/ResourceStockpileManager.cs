using System.Collections.Generic;
using UnityEngine;
using Terranova.Core;
using Terranova.Terrain;

namespace Terranova.Population
{
    /// <summary>
    /// Manages visual resource stockpiles near the campfire.
    ///
    /// Extracted from Settler.cs to reduce its static field count and
    /// consolidate drop/pile logic in one place.
    ///
    /// Three stockpile zones around the campfire:
    ///   Wood  = East  (+X)
    ///   Stone = South (-Z)
    ///   Food  = West  (-X)
    ///
    /// Individual items accumulate; every ITEMS_PER_PILE items the loose
    /// props are replaced with a denser pile visual.
    /// </summary>
    public class ResourceStockpileManager : MonoBehaviour
    {
        public static ResourceStockpileManager Instance { get; private set; }

        private const int ITEMS_PER_PILE = 5;
        private const float DROP_OFFSET = 3.0f;

        // ─── Wood stockpile (East) ───────────────────────────────
        private int _woodDropCount;
        private int _woodPileCount;
        private readonly List<GameObject> _droppedWoodItems = new();
        private readonly List<GameObject> _woodPiles = new();

        // ─── Stone stockpile (South) ─────────────────────────────
        private int _stoneDropCount;
        private int _stonePileCount;
        private readonly List<GameObject> _droppedStoneItems = new();

        // ─── Food stockpile (West) ───────────────────────────────
        private int _foodDropCount;
        private readonly List<GameObject> _droppedFoodItems = new();

        // ─── Drop positions ──────────────────────────────────────
        private Vector3 _woodDropPos, _stoneDropPos, _foodDropPos;
        private bool _dropPositionsSet;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ─── Public API ──────────────────────────────────────────

        /// <summary>
        /// Drop a visual resource at the appropriate stockpile near the campfire.
        /// Called by Settler on delivery.
        /// </summary>
        public void DropResource(SettlerTaskType taskType, Vector3 campfirePos, string settlerName)
        {
            EnsureDropPositions(campfirePos);

            switch (taskType)
            {
                case SettlerTaskType.GatherWood:
                    DropWood(settlerName);
                    break;
                case SettlerTaskType.GatherStone:
                    DropStone(settlerName);
                    break;
                case SettlerTaskType.Hunt:
                    DropFood(settlerName);
                    break;
            }
        }

        /// <summary>Reset all stockpiles (e.g. on new tribe).</summary>
        public void ResetStockpiles()
        {
            _woodDropCount = 0; _woodPileCount = 0;
            _stoneDropCount = 0; _stonePileCount = 0;
            _foodDropCount = 0;
            ClearList(_droppedWoodItems);
            ClearList(_woodPiles);
            ClearList(_droppedStoneItems);
            ClearList(_droppedFoodItems);
            _dropPositionsSet = false;
        }

        // ─── Drop Position Setup ─────────────────────────────────

        private void EnsureDropPositions(Vector3 campfirePos)
        {
            if (_dropPositionsSet) return;

            var world = WorldManager.Instance;

            _woodDropPos = campfirePos + new Vector3(DROP_OFFSET, 0f, 0f);
            if (world != null)
                _woodDropPos.y = world.GetSmoothedHeightAtWorldPos(_woodDropPos.x, _woodDropPos.z);

            _stoneDropPos = campfirePos + new Vector3(0f, 0f, -DROP_OFFSET);
            if (world != null)
                _stoneDropPos.y = world.GetSmoothedHeightAtWorldPos(_stoneDropPos.x, _stoneDropPos.z);

            _foodDropPos = campfirePos + new Vector3(-DROP_OFFSET, 0f, 0f);
            if (world != null)
                _foodDropPos.y = world.GetSmoothedHeightAtWorldPos(_foodDropPos.x, _foodDropPos.z);

            _dropPositionsSet = true;
        }

        // ─── Wood ────────────────────────────────────────────────

        private void DropWood(string settlerName)
        {
            _woodDropCount++;

            if (_woodDropCount < ITEMS_PER_PILE)
            {
                var rng = new System.Random(_woodDropCount * 7919 + _woodPileCount * 3571 + GameState.Seed);
                SpawnDropItem(AssetPrefabRegistry.Twigs, _woodDropPos, rng, 0.5f, 0.8f,
                    $"DroppedStick_{_woodPileCount}_{_woodDropCount}", _droppedWoodItems);
                Debug.Log($"[{settlerName}] Dropped stick {_woodDropCount}/{ITEMS_PER_PILE} at campfire");
            }
            else
            {
                foreach (var item in _droppedWoodItems)
                    if (item != null) Destroy(item);
                _droppedWoodItems.Clear();

                float pileOffset = _woodPileCount * 1.2f;
                Vector3 pilePos = _woodDropPos + new Vector3(pileOffset, 0f, 0f);

                var pile = AssetPrefabRegistry.InstantiateSpecific(
                    "Props/Wood_Pile_1C", pilePos, Quaternion.Euler(0f, _woodPileCount * 45f, 0f), null, 0.6f);
                if (pile != null)
                {
                    pile.name = $"WoodPile_{_woodPileCount}";
                    foreach (var col in pile.GetComponentsInChildren<Collider>())
                        Destroy(col);
                    _woodPiles.Add(pile);
                }

                _woodPileCount++;
                _woodDropCount = 0;
                Debug.Log($"[{settlerName}] Created Wood Pile #{_woodPileCount} at campfire!");
            }
        }

        // ─── Stone ───────────────────────────────────────────────

        private void DropStone(string settlerName)
        {
            _stoneDropCount++;
            var rng = new System.Random(_stoneDropCount * 6271 + _stonePileCount * 4219 + GameState.Seed);

            if (_stoneDropCount >= ITEMS_PER_PILE)
            {
                foreach (var item in _droppedStoneItems)
                    if (item != null) Destroy(item);
                _droppedStoneItems.Clear();

                float pileOffset = _stonePileCount * 1.0f;
                Vector3 clusterPos = _stoneDropPos + new Vector3(pileOffset, 0f, 0f);

                for (int i = 0; i < 3; i++)
                {
                    float ox = (float)(rng.NextDouble() - 0.5) * 0.4f;
                    float oz = (float)(rng.NextDouble() - 0.5) * 0.4f;
                    var rock = AssetPrefabRegistry.InstantiateRandom(
                        AssetPrefabRegistry.RockSmall, clusterPos + new Vector3(ox, 0f, oz),
                        rng, null, 0.6f, 0.9f);
                    if (rock != null)
                    {
                        rock.name = $"StonePile_{_stonePileCount}_{i}";
                        foreach (var col in rock.GetComponentsInChildren<Collider>())
                            Destroy(col);
                    }
                }

                _stonePileCount++;
                _stoneDropCount = 0;
                Debug.Log($"[{settlerName}] Created Stone Pile #{_stonePileCount} at campfire!");
            }
            else
            {
                SpawnDropItem(AssetPrefabRegistry.RockSmall, _stoneDropPos, rng, 0.3f, 0.5f,
                    $"DroppedStone_{_stonePileCount}_{_stoneDropCount}", _droppedStoneItems);
                Debug.Log($"[{settlerName}] Dropped stone {_stoneDropCount}/{ITEMS_PER_PILE} at campfire");
            }
        }

        // ─── Food ────────────────────────────────────────────────

        private void DropFood(string settlerName)
        {
            _foodDropCount++;
            var rng = new System.Random(_foodDropCount * 8387 + GameState.Seed);

            SpawnDropItem(AssetPrefabRegistry.Mushrooms, _foodDropPos, rng, 0.25f, 0.4f,
                $"DroppedFood_{_foodDropCount}", _droppedFoodItems);
            Debug.Log($"[{settlerName}] Dropped food {_foodDropCount} at campfire");
        }

        // ─── Helpers ─────────────────────────────────────────────

        private static void SpawnDropItem(string[] prefabPool, Vector3 basePos, System.Random rng,
            float minScale, float maxScale, string itemName, List<GameObject> trackList)
        {
            float ox = (float)(rng.NextDouble() - 0.5) * 0.8f;
            float oz = (float)(rng.NextDouble() - 0.5) * 0.8f;
            float rot = (float)(rng.NextDouble() * 360.0);
            Vector3 pos = basePos + new Vector3(ox, 0f, oz);

            var item = AssetPrefabRegistry.InstantiateRandom(prefabPool, pos, rng, null, minScale, maxScale);
            if (item != null)
            {
                item.name = itemName;
                item.transform.rotation = Quaternion.Euler(0f, rot, 0f);
                foreach (var col in item.GetComponentsInChildren<Collider>())
                    Destroy(col);
                trackList.Add(item);
            }
        }

        private static void ClearList(List<GameObject> list)
        {
            foreach (var obj in list)
                if (obj != null) Destroy(obj);
            list.Clear();
        }
    }
}
