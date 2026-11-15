using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// A click under every interface button in every scene (the user, 2026-11-15: «нужны звуки нажатия кнопок
    /// интерфейса в игре»). Buttons are made in dozens of places, at setup and at run time, so nothing is hung on them:
    /// one object for the whole game watches the pointer, and when a press is let go over a live Selectable (a
    /// button, toggle, slider...) it plays Audio/Resources/Ui/click_N. Volume: ui_click_volume (sheet).
    /// One raycast per release, the hit list reused: nothing per frame otherwise.
    /// </summary>
    public class UiClickSound : MonoBehaviour
    {
        private static UiClickSound _instance;
        private readonly List<RaycastResult> _hits = new List<RaycastResult>();
        private PointerEventData _pointer;
        private EventSystem _pointerFor;
        private AudioSource _source;
        private AudioClip[] _clips;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (_instance != null) return;
            var go = new GameObject("UiClickSound");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<UiClickSound>();
        }

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.ignoreListenerPause = true;   // the menus open over a paused game
            _clips = UnityEngine.Resources.LoadAll<AudioClip>("Ui");
        }

        private void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasReleasedThisFrame) return;
            var events = EventSystem.current;
            if (events == null || _clips == null || _clips.Length == 0) return;
            if (_pointer == null || _pointerFor != events)
            {
                _pointer = new PointerEventData(events);
                _pointerFor = events;
            }
            _pointer.position = pointer.position.ReadValue();
            _hits.Clear();
            events.RaycastAll(_pointer, _hits);
            if (_hits.Count == 0) return;
            var hit = _hits[0].gameObject;
            var selectable = hit != null ? hit.GetComponentInParent<Selectable>() : null;
            if (selectable == null || !selectable.IsActive() || !selectable.IsInteractable()) return;
            var config = BalanceConfig.Instance;
            var volume = config != null ? config.UiClickVolume : 0.5f;
            if (volume <= 0f) return;
            _source.pitch = Random.Range(0.97f, 1.03f);
            _source.PlayOneShot(_clips[Random.Range(0, _clips.Length)], volume);
        }
    }
}
