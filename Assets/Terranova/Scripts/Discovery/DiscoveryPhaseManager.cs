using System.Collections.Generic;
using UnityEngine;
using Terranova.Core;
using Terranova.Population;
using Terranova.Terrain;

namespace Terranova.Discovery
{
    /// <summary>
    /// Phase of a discovery in progress.
    /// Feature 8.2: Discovery Phases.
    /// </summary>
    public enum DiscoveryPhase
    {
        Inactive,        // Not yet started (prerequisites not met)
        Observation,     // Phase A: passive counter incrementing
        Spark,           // Phase B: "!" moment (transient, auto-advances)
        Experimentation, // Phase C: active experiment with success/failure
        Complete         // Phase D: Eureka — discovery done
    }

    /// <summary>
    /// Tracks the progress of a single discovery through its phases.
    /// </summary>
    public class DiscoveryProgress
    {
        public DiscoveryDefinition Definition;
        public DiscoveryPhase Phase = DiscoveryPhase.Inactive;
        public float ObservationCount;      // Phase A counter (float for biome multipliers)
        public bool IsInspired;             // Settler is "inspired" after spark
        public string InspiredSettlerName;  // Which settler got the spark
        public float ExperimentProgress;    // Phase C: 0-1
        public float ExperimentTimer;       // Seconds remaining in current experiment
        public int FailureCount;            // How many times experimentation failed
        public int DayDiscovered;           // Day number when completed
        public string DiscovererName;       // Who completed it
    }

    /// <summary>
    /// Manages the phased discovery progression system.
    ///
    /// Feature 8.2: Discovery Phases (A→B→C→D).
    /// Feature 8.4: Failure as Feature.
    /// Feature 8.5: Biome-Driven Discovery Modifiers.
    ///
    /// Replaces the probability-based DiscoveryEngine for phased discoveries.
    /// Lightning Fire and other spontaneous discoveries still use the old engine.
    ///
    /// Phase A (Observation): passive counter increments on ResourceDeliveredEvent.
    ///   - Hidden from player. Biome and trait modifiers apply.
    /// Phase B (Spark): settler stops, "!" icon, hint notification.
    ///   - 0.5s pause, then settler is "inspired."
    /// Phase C (Experimentation): inspired settler experiments at task.
    ///   - Progress bar above settler. Can fail (50% counter advance).
    ///   - After 3 failures: guaranteed success (bad luck protection).
    /// Phase D (Eureka): modal overlay, game pauses.
    ///   - MAJOR discoveries: camera zoom + particle + 2s dramatic pause.
    /// </summary>
    public class DiscoveryPhaseManager : MonoBehaviour
    {
        public static DiscoveryPhaseManager Instance { get; private set; }

        private const int MAX_FAILURES_BEFORE_GUARANTEED = 3;
        private const float SPARK_PAUSE_DURATION = 0.5f;
        private const float EXPERIMENT_CHECK_INTERVAL = 1f;

        // All tracked discovery progress
        private readonly Dictionary<string, DiscoveryProgress> _progress = new();

        // Currently experimenting settler → discovery ID
        private readonly Dictionary<string, string> _experimentingSettlers = new();

        private float _experimentTimer;

        /// <summary>All discovery progress states (for UI/debug).</summary>
        public IReadOnlyDictionary<string, DiscoveryProgress> AllProgress => _progress;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ResourceDeliveredEvent>(OnResourceDelivered);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ResourceDeliveredEvent>(OnResourceDelivered);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Register a discovery for phased tracking.</summary>
        public void RegisterDiscovery(DiscoveryDefinition def)
        {
            if (_progress.ContainsKey(def.DisplayName)) return;
            _progress[def.DisplayName] = new DiscoveryProgress
            {
                Definition = def,
                Phase = DiscoveryPhase.Inactive
            };
        }

        private void Update()
        {
            UpdatePhaseActivation();
            UpdateExperiments();
        }

        // ─── Phase Activation ────────────────────────────────────

        /// <summary>
        /// Check if inactive discoveries should become active
        /// (prerequisites met).
        /// </summary>
        private void UpdatePhaseActivation()
        {
            var stateManager = DiscoveryStateManager.Instance;
            if (stateManager == null) return;

            foreach (var kvp in _progress)
            {
                var prog = kvp.Value;
                if (prog.Phase != DiscoveryPhase.Inactive) continue;
                if (stateManager.IsDiscovered(prog.Definition.DisplayName)) continue;

                // Check prerequisites
                bool prereqsMet = true;
                if (prog.Definition.PrerequisiteDiscoveries != null)
                {
                    foreach (var prereq in prog.Definition.PrerequisiteDiscoveries)
                    {
                        if (!stateManager.IsDiscovered(prereq))
                        {
                            prereqsMet = false;
                            break;
                        }
                    }
                }

                if (prereqsMet)
                {
                    prog.Phase = DiscoveryPhase.Observation;
                    Debug.Log($"[DiscoveryPhase] {prog.Definition.DisplayName} → Observation phase active.");
                }
            }
        }

        // ─── Phase A: Observation ────────────────────────────────

        /// <summary>
        /// On resource delivery, increment observation counters for
        /// relevant discoveries. This IS the Phase A passive tracking.
        /// </summary>
        private void OnResourceDelivered(ResourceDeliveredEvent evt)
        {
            var stateManager = DiscoveryStateManager.Instance;
            if (stateManager == null) return;

            foreach (var kvp in _progress)
            {
                var prog = kvp.Value;
                if (prog.Phase != DiscoveryPhase.Observation) continue;
                if (prog.Definition.RequiredActivity == SettlerTaskType.None) continue;
                if (evt.TaskType != prog.Definition.RequiredActivity) continue;

                // Calculate observation increment with modifiers
                float increment = 1f;

                // Feature 8.5: Biome speed modifier
                if (prog.Definition.BiomeSpeedMultiplier > 1f)
                {
                    if (GameState.SelectedBiome == prog.Definition.BonusBiome)
                        increment *= prog.Definition.BiomeSpeedMultiplier;
                }

                // Feature 8.4: Curious trait — +25% observation speed
                var settler = FindSettlerByTaskEvent(evt);
                if (settler != null && settler.Trait == SettlerTrait.Curious)
                    increment *= 1.25f;

                prog.ObservationCount += increment;

                // Fire observation event (for debug/UI progress hints)
                EventBus.Publish(new DiscoveryObservationEvent
                {
                    DiscoveryId = prog.Definition.DisplayName,
                    CurrentCount = (int)prog.ObservationCount,
                    RequiredCount = prog.Definition.ObservationThreshold
                });

                // Check if observation threshold reached
                if (prog.ObservationCount >= prog.Definition.ObservationThreshold)
                {
                    if (prog.Definition.Tier == DiscoveryTier.Basic)
                    {
                        // Basic: skip spark and experimentation, go straight to Eureka
                        CompleteDiscovery(prog, settler?.name ?? "A settler");
                    }
                    else
                    {
                        // Standard/Major: trigger spark
                        TriggerSpark(prog, settler);
                    }
                }
            }
        }

        // ─── Phase B: Spark ──────────────────────────────────────

        private void TriggerSpark(DiscoveryProgress prog, Settler settler)
        {
            prog.Phase = DiscoveryPhase.Spark;
            prog.IsInspired = true;
            string settlerName = settler != null ? settler.name : "A settler";
            prog.InspiredSettlerName = settlerName;

            // Build hint message
            string hint = !string.IsNullOrEmpty(prog.Definition.SparkHint)
                ? prog.Definition.SparkHint.Replace("{name}", settlerName)
                : $"{settlerName} notices something interesting...";

            Vector3 pos = settler != null ? settler.transform.position : Vector3.zero;

            EventBus.Publish(new DiscoverySparkEvent
            {
                DiscoveryId = prog.Definition.DisplayName,
                SettlerName = settlerName,
                HintMessage = hint,
                Position = pos
            });

            Debug.Log($"[DiscoveryPhase] SPARK: {prog.Definition.DisplayName} — {hint}");

            // Auto-advance to Experimentation phase
            prog.Phase = DiscoveryPhase.Experimentation;
            prog.ExperimentProgress = 0f;
            prog.ExperimentTimer = prog.Definition.ExperimentDuration;

            // Feature 8.4: Skilled trait — 20% faster experimentation
            if (settler != null && settler.Trait == SettlerTrait.Skilled)
                prog.ExperimentTimer *= 0.8f;

            _experimentingSettlers[settlerName] = prog.Definition.DisplayName;
        }

        // ─── Phase C: Experimentation ────────────────────────────

        private void UpdateExperiments()
        {
            _experimentTimer += Time.deltaTime;
            if (_experimentTimer < EXPERIMENT_CHECK_INTERVAL) return;
            _experimentTimer -= EXPERIMENT_CHECK_INTERVAL;

            // Iterate a copy in case we modify _experimentingSettlers
            var toRemove = new List<string>();
            foreach (var kvp in _experimentingSettlers)
            {
                string settlerName = kvp.Key;
                string discoveryId = kvp.Value;

                if (!_progress.TryGetValue(discoveryId, out var prog)) continue;
                if (prog.Phase != DiscoveryPhase.Experimentation) continue;

                prog.ExperimentTimer -= EXPERIMENT_CHECK_INTERVAL;
                prog.ExperimentProgress = 1f - Mathf.Max(0f, prog.ExperimentTimer / prog.Definition.ExperimentDuration);

                // Broadcast progress for UI (progress bar above settler)
                EventBus.Publish(new DiscoveryExperimentEvent
                {
                    DiscoveryId = discoveryId,
                    SettlerName = settlerName,
                    Progress = prog.ExperimentProgress,
                    Failed = false
                });

                // Experiment complete?
                if (prog.ExperimentTimer <= 0f)
                {
                    ResolveExperiment(prog, settlerName);
                    toRemove.Add(settlerName);
                }
            }

            foreach (var name in toRemove)
                _experimentingSettlers.Remove(name);
        }

        private void ResolveExperiment(DiscoveryProgress prog, string settlerName)
        {
            // Feature 8.4: Bad luck protection — after 3 failures, guaranteed success
            bool guaranteedSuccess = prog.FailureCount >= MAX_FAILURES_BEFORE_GUARANTEED;

            // Calculate failure chance with trait modifiers
            float failChance = prog.Definition.FailureChance;

            // Feature 8.4: Cautious trait — 10% less failure chance
            var settler = FindSettlerByName(settlerName);
            if (settler != null && settler.Trait == SettlerTrait.Cautious)
                failChance -= 0.1f;

            failChance = Mathf.Max(0f, failChance);

            bool success = guaranteedSuccess || Random.value >= failChance;

            if (success)
            {
                CompleteDiscovery(prog, settlerName);
            }
            else
            {
                // Failure — advance counter by 50% of success value (Feature 8.4)
                prog.FailureCount++;
                prog.Phase = DiscoveryPhase.Observation;
                prog.ObservationCount = Mathf.Max(
                    prog.ObservationCount,
                    prog.Definition.ObservationThreshold * 0.5f * prog.FailureCount
                );
                prog.ExperimentProgress = 0f;
                prog.IsInspired = false;

                string failMsg = $"The experiment didn't work... but {settlerName} learned something.";
                EventBus.Publish(new DiscoveryExperimentEvent
                {
                    DiscoveryId = prog.Definition.DisplayName,
                    SettlerName = settlerName,
                    Progress = 0f,
                    Failed = true,
                    FailureMessage = failMsg
                });

                Debug.Log($"[DiscoveryPhase] FAIL #{prog.FailureCount}: {prog.Definition.DisplayName} — {failMsg}");
            }
        }

        // ─── Phase D: Eureka ─────────────────────────────────────

        private void CompleteDiscovery(DiscoveryProgress prog, string settlerName)
        {
            prog.Phase = DiscoveryPhase.Complete;
            prog.DiscovererName = settlerName;

            // Get current day
            var dayNight = DayNightCycle.Instance;
            prog.DayDiscovered = dayNight != null ? dayNight.DayCount : 0;

            bool isMajor = prog.Definition.Tier == DiscoveryTier.Major;

            // Fire Eureka event (camera zoom for Major discoveries)
            var settler = FindSettlerByName(settlerName);
            Vector3 pos = settler != null ? settler.transform.position : Vector3.zero;

            EventBus.Publish(new DiscoveryEurekaEvent
            {
                DiscoveryId = prog.Definition.DisplayName,
                SettlerName = settlerName,
                Position = pos,
                IsMajor = isMajor
            });

            // Complete via state manager (fires DiscoveryMadeEvent for existing UI)
            var stateManager = DiscoveryStateManager.Instance;
            if (stateManager != null)
            {
                string reason = $"{settlerName} discovered this through observation and experimentation";
                stateManager.CompleteDiscovery(prog.Definition, reason);
            }

            Debug.Log($"[DiscoveryPhase] EUREKA: {prog.Definition.DisplayName} by {settlerName}" +
                      (isMajor ? " [MAJOR — epic staging]" : ""));
        }

        // ─── Helpers ─────────────────────────────────────────────

        private static Settler FindSettlerByTaskEvent(ResourceDeliveredEvent evt)
        {
            // Find the closest settler to the delivery position
            float bestDist = float.MaxValue;
            Settler best = null;
            foreach (var t in SettlerLocator.ActiveSettlers)
            {
                if (t == null) continue;
                var s = t.GetComponent<Settler>();
                if (s == null) continue;
                float d = Vector3.Distance(t.position, evt.Position);
                if (d < bestDist) { bestDist = d; best = s; }
            }
            return best;
        }

        private static Settler FindSettlerByName(string name)
        {
            foreach (var t in SettlerLocator.ActiveSettlers)
            {
                if (t != null && t.name == name)
                    return t.GetComponent<Settler>();
            }
            return null;
        }

        /// <summary>
        /// Get the progress state for a discovery by name.
        /// Used by DiscoveryLogUI for progress hints.
        /// </summary>
        public DiscoveryProgress GetProgress(string discoveryName)
        {
            return _progress.TryGetValue(discoveryName, out var p) ? p : null;
        }

        /// <summary>Reset all progress (new tribe).</summary>
        public void ResetAll()
        {
            foreach (var kvp in _progress)
            {
                var p = kvp.Value;
                p.Phase = DiscoveryPhase.Inactive;
                p.ObservationCount = 0;
                p.IsInspired = false;
                p.InspiredSettlerName = null;
                p.ExperimentProgress = 0;
                p.ExperimentTimer = 0;
                p.FailureCount = 0;
                p.DayDiscovered = 0;
                p.DiscovererName = null;
            }
            _experimentingSettlers.Clear();
            Debug.Log("[DiscoveryPhaseManager] All progress reset.");
        }
    }
}
