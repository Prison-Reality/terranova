using UnityEngine;
using Terranova.Core;
using Terranova.Buildings;

namespace Terranova.Population
{
    /// <summary>
    /// Periodically scans for unfinished construction sites and assigns
    /// idle settlers to build them.
    ///
    /// Priority: Construction sites are checked BEFORE resource gathering,
    /// so new buildings get built as soon as a settler is free.
    ///
    /// Story 4.2: Baufortschritt
    /// </summary>
    public class ConstructionTaskAssigner : MonoBehaviour
    {
        private const float CHECK_INTERVAL = 1f;

        private float _checkTimer;

        private void Update()
        {
            _checkTimer -= Time.deltaTime;
            if (_checkTimer > 0f) return;

            _checkTimer = CHECK_INTERVAL;
            AssignBuildersToSites();
        }

        private void AssignBuildersToSites()
        {
            var campfire = GameObject.Find("Campfire");
            if (campfire == null) return;
            Vector3 basePos = campfire.transform.position;

            var buildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
            var settlers = FindObjectsByType<Settler>(FindObjectsSortMode.None);

            foreach (var building in buildings)
            {
                // Skip completed buildings and those already being built
                if (building.IsConstructed || building.IsBeingBuilt) continue;

                // Find nearest idle settler for this construction site
                Settler nearest = null;
                float nearestDist = float.MaxValue;

                foreach (var settler in settlers)
                {
                    if (settler.HasTask) continue;

                    float dist = Vector3.Distance(settler.transform.position, building.EntrancePosition);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = settler;
                    }
                }

                if (nearest == null) break; // No more idle settlers

                if (!building.TryReserveConstruction()) continue;

                float duration = building.GetBuildStepDuration();
                var task = new SettlerTask(
                    SettlerTaskType.Build,
                    building.EntrancePosition,
                    basePos,
                    duration
                );
                task.TargetBuilding = building;

                if (!nearest.AssignTask(task))
                {
                    building.ReleaseConstruction();
                }
            }
        }
    }
}
