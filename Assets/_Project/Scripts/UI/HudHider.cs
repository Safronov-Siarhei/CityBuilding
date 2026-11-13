using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// Hides the standing HUD behind a menu or a full-screen panel (the user's rule, 2026-10-14:
    /// "the buttons behind it must disappear"). Lives on the safe-area root; every direct child that
    /// does not carry a HudOverlay is faded out through a CanvasGroup and stops catching taps.
    ///
    /// A CanvasGroup rather than SetActive: several HUD pieces switch themselves on and off every
    /// frame (the build menu, the migration line, the army panel), and would simply switch
    /// themselves back on. Hiding is counted per owner, so the menu and a panel opened from it can
    /// each ask without one's closing undoing the other.
    /// </summary>
    public class HudHider : MonoBehaviour
    {
        private readonly HashSet<Object> _owners = new HashSet<Object>();
        private readonly List<CanvasGroup> _groups = new List<CanvasGroup>();
        // each group's look before the hide, given back as it was: a piece hidden by its mode (campaign mission 2 hides
        // the sandbox HUD the same way) stays hidden (the user's tenth playtest: the menu brought the sandbox HUD back)
        private readonly List<float> _alphas = new List<float>();
        private readonly List<bool> _raycasts = new List<bool>();
        private readonly List<bool> _interactables = new List<bool>();

        public bool IsHidden => _owners.Count > 0;

        public void Hide(Object owner)
        {
            if (owner == null || !_owners.Add(owner) || _owners.Count > 1) return;
            Apply(true);
        }

        public void Show(Object owner)
        {
            if (owner == null || !_owners.Remove(owner) || _owners.Count > 0) return;
            Apply(false);
        }

        private void Apply(bool hidden)
        {
            if (hidden)
            {
                _groups.Clear();
                _alphas.Clear();
                _raycasts.Clear();
                _interactables.Clear();
                foreach (Transform child in transform)
                {
                    if (child.GetComponent<HudOverlay>() != null) continue;
                    var group = child.GetComponent<CanvasGroup>();
                    if (group == null) group = child.gameObject.AddComponent<CanvasGroup>();
                    _alphas.Add(group.alpha);
                    _raycasts.Add(group.blocksRaycasts);
                    _interactables.Add(group.interactable);
                    group.alpha = 0f;
                    group.blocksRaycasts = false;
                    group.interactable = false;
                    _groups.Add(group);
                }
                return;
            }

            for (var i = 0; i < _groups.Count; i++)
            {
                var group = _groups[i];
                if (group == null) continue;
                group.alpha = _alphas[i];
                group.blocksRaycasts = _raycasts[i];
                group.interactable = _interactables[i];
            }
            _groups.Clear();
        }
    }
}
