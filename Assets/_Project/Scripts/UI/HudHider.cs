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
                foreach (Transform child in transform)
                {
                    if (child.GetComponent<HudOverlay>() != null) continue;
                    var group = child.GetComponent<CanvasGroup>();
                    if (group == null) group = child.gameObject.AddComponent<CanvasGroup>();
                    group.alpha = 0f;
                    group.blocksRaycasts = false;
                    group.interactable = false;
                    _groups.Add(group);
                }
                return;
            }

            foreach (var group in _groups)
            {
                if (group == null) continue;
                group.alpha = 1f;
                group.blocksRaycasts = true;
                group.interactable = true;
            }
            _groups.Clear();
        }
    }
}
