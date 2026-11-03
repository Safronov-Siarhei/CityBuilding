using System.Collections;
using System.Collections.Generic;
using CityBuilder.Maps;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// A dungeon's levers and the barred door they open (Dungeon 2, the user, 2026-11-04: «на рычаг
    /// нельзя нажать»): the map's `lever` spots get the swinging arm (map.LeverArmPrefab) and a tap
    /// box; a hero sent to one pulls it (DungeonEntrance.Place.Lever). When every lever is pulled
    /// the door's bars (map.DoorPrefab) rise and the way to the throne is open -- until then a
    /// carving NavMeshObstacle shuts it, so nobody walks through the bars.
    ///
    /// A map without levers or a door makes none of it.
    /// </summary>
    public class DungeonLevers : MonoBehaviour
    {
        // how long an arm takes to swing over, and how far
        private const float PullSeconds = 0.6f;
        private const float PullDegrees = 60f;
        // how high the bars rise into the arch, and how long that takes (cells, seconds)
        private const float DoorRise = 1.6f;
        private const float DoorSeconds = 2.5f;
        // the tap box round a lever (cells)
        private static readonly Vector3 LeverTapBox = new Vector3(1.6f, 1.6f, 1.6f);

        private readonly List<Lever> _levers = new List<Lever>();
        private Transform _door;
        private NavMeshObstacle _bars;

        public static DungeonLevers Instance { get; private set; }

        /// <summary>The door is up: every lever was pulled (or the map has a door and no lever).</summary>
        public bool DoorOpen { get; private set; }

        public int LeverCount => _levers.Count;
        public int PulledCount
        {
            get
            {
                var n = 0;
                foreach (var lever in _levers) if (lever.Pulled) n++;
                return n;
            }
        }

        private class Lever
        {
            public DungeonEntrance Place;
            public Transform Arm;
            public Quaternion Rest;
            public bool Pulled;
        }

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Begin(MeshMapDefinition map, string dungeonId)
        {
            if (map == null) return;
            if (map.DoorPrefab != null) MakeDoor(map.DoorPrefab);
            var half = LeverTapBox.x * 0.5f;
            foreach (var spot in map.SpotsOf("lever"))
            {
                var go = new GameObject("DungeonLever");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(spot.position.x, spot.height, spot.position.y);
                var box = go.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = LeverTapBox;
                box.center = Vector3.zero;
                var place = go.AddComponent<DungeonEntrance>();
                place.InitializeLever(dungeonId, half);
                Transform arm = null;
                if (map.LeverArmPrefab != null)
                {
                    arm = Instantiate(map.LeverArmPrefab, go.transform.position, map.LeverArmPrefab.transform.rotation, go.transform).transform;
                    arm.name = "LeverArm";
                }
                _levers.Add(new Lever { Place = place, Arm = arm, Rest = arm != null ? arm.rotation : Quaternion.identity });
            }
            if (_door != null && _levers.Count == 0) OpenDoor(instant: true);
        }

        private void MakeDoor(GameObject prefab)
        {
            var door = Instantiate(prefab, Vector3.zero, prefab.transform.rotation, transform);
            door.name = "DungeonDoor";
            _door = door.transform;
            var renderers = door.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            // the bars' own box, carved out of the NavMesh: a hero walks up to them and no further
            var holder = new GameObject("DoorBars");
            holder.transform.SetParent(transform, false);
            holder.transform.position = bounds.center;
            _bars = holder.AddComponent<NavMeshObstacle>();
            _bars.shape = NavMeshObstacleShape.Box;
            _bars.size = new Vector3(Mathf.Max(bounds.size.x, 0.6f), Mathf.Max(bounds.size.y, 1f), Mathf.Max(bounds.size.z, 0.6f));
            _bars.carving = true;
            _bars.carveOnlyStationary = false;
            DungeonWaypoints.Gate(bounds.center, Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f + 0.6f);
        }

        /// <summary>A tap on a lever (or a hero sent to it arrived): a hero by it pulls it, else the player is told to bring one.</summary>
        public void Use(DungeonEntrance place)
        {
            var lever = _levers.Find(l => l.Place == place);
            if (lever == null || lever.Pulled) return;
            if (!place.HeroNear())
            {
                EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_dungeon_lever_need_hero"), EventSeverity.Warning,
                    EventGlyph.Alert, null, place.transform.position));
                return;
            }
            Pull(lever);
        }

        private void Pull(Lever lever)
        {
            lever.Pulled = true;
            if (lever.Arm != null) StartCoroutine(Swing(lever));
            var pulled = PulledCount;
            if (pulled < _levers.Count)
            {
                EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_dungeon_lever_pulled", pulled, _levers.Count),
                    EventSeverity.Good, EventGlyph.Star, null, lever.Place.transform.position));
                return;
            }
            OpenDoor(instant: false);
        }

        private IEnumerator Swing(Lever lever)
        {
            var to = Quaternion.AngleAxis(PullDegrees, Vector3.right) * lever.Rest;
            for (var t = 0f; t < PullSeconds; t += Time.deltaTime)
            {
                if (lever.Arm == null) yield break;
                lever.Arm.rotation = Quaternion.Slerp(lever.Rest, to, Mathf.SmoothStep(0f, 1f, t / PullSeconds));
                yield return null;
            }
            if (lever.Arm != null) lever.Arm.rotation = to;
        }

        private void OpenDoor(bool instant)
        {
            if (DoorOpen) return;
            DoorOpen = true;
            if (_bars != null) _bars.enabled = false;
            DungeonWaypoints.Ungate();
            if (_door == null) return;
            if (instant)
            {
                _door.position += Vector3.up * DoorRise;
                return;
            }
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_dungeon_door_open"), EventSeverity.Good,
                EventGlyph.Star, null, _bars != null ? _bars.transform.position : _door.position));
            StartCoroutine(Rise());
        }

        private IEnumerator Rise()
        {
            var from = _door.position;
            var to = from + Vector3.up * DoorRise;
            for (var t = 0f; t < DoorSeconds; t += Time.deltaTime)
            {
                if (_door == null) yield break;
                _door.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / DoorSeconds));
                yield return null;
            }
            if (_door != null) _door.position = to;
        }
    }
}
