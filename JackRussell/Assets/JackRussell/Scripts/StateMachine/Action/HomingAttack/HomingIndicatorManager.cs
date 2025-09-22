using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace JackRussell.States.Action
{
    /// <summary>
    /// Manages the display of homing attack indicators on targets.
    /// Injectable service that handles instantiation and lifecycle of indicators.
    /// </summary>
    public class HomingIndicatorManager
    {
        [SerializeField] private GameObject _indicatorPrefab;

        private readonly Dictionary<HomingTarget, HomingIndicator> _activeIndicators = new();
        private Transform _indicatorParent;

        [Inject]
        public HomingIndicatorManager(GameObject indicatorPrefab)
        {
            _indicatorPrefab = indicatorPrefab;
            _indicatorParent = new GameObject("HomingIndicators").transform;
        }

        /// <summary>
        /// Shows indicators on the specified targets.
        /// Hides indicators on targets not in the list.
        /// </summary>
        public void ShowIndicators(IEnumerable<HomingTarget> targets)
        {
            HashSet<HomingTarget> currentTargets = new HashSet<HomingTarget>(targets);

            // Hide indicators for targets no longer valid
            List<HomingTarget> toRemove = new List<HomingTarget>();
            foreach (var kvp in _activeIndicators)
            {
                if (!currentTargets.Contains(kvp.Key) || !kvp.Key.IsActive)
                {
                    kvp.Value.Disappear();
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var target in toRemove)
            {
                _activeIndicators.Remove(target);
            }

            // Show indicators for new valid targets
            foreach (var target in currentTargets)
            {
                if (target.IsActive && !_activeIndicators.ContainsKey(target))
                {
                    GameObject indicatorGO = UnityEngine.Object.Instantiate(_indicatorPrefab, _indicatorParent);
                    HomingIndicator indicator = indicatorGO.GetComponent<HomingIndicator>();
                    if (indicator != null)
                    {
                        indicator.SetTarget(target.TargetTransform);
                        _activeIndicators[target] = indicator;
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(indicatorGO);
                        Debug.LogWarning("HomingIndicator prefab missing HomingIndicator component");
                    }
                }
            }
        }

        /// <summary>
        /// Hides all active indicators.
        /// </summary>
        public void HideAllIndicators()
        {
            foreach (var indicator in _activeIndicators.Values)
            {
                UnityEngine.Object.Destroy(indicator.gameObject);
            }
            _activeIndicators.Clear();
        }

        /// <summary>
        /// Hides the indicator for a specific target.
        /// </summary>
        public void HideIndicator(HomingTarget target)
        {
            if (_activeIndicators.TryGetValue(target, out var indicator))
            {
                UnityEngine.Object.Destroy(indicator.gameObject);
                _activeIndicators.Remove(target);
            }
        }
    }
}