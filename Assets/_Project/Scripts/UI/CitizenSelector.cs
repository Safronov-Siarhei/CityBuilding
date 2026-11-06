using System.Collections;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// Tap/click an idle citizen to select it (a small floating marker appears above its head),
    /// then tap/click anywhere else to send it walking there. Only idle citizens respond
    /// (CitizenAgent.IsIdle) -- redirecting a working citizen mid-commute would desync
    /// CitizenVisualsManager's job bookkeeping (ProductionBuilding.AssignedWorkers, claimed
    /// ResourceNodes) without a matching release, so those clicks are simply ignored.
    ///
    /// A destination click reads the point where the camera ray meets the ground mesh and pulls
    /// it onto the nearest walkable NavMesh position (see TryResolveDestination), so a click that
    /// lands on something unwalkable aims next to it rather than into it. The order is drawn on
    /// the ground by MoveOrderMarker -- a ripple where the tap landed, a flag at the destination and
    /// a trail of dots along the route the citizen will walk; a red ripple when the tap had nowhere
    /// valid to go -- alongside the centre-screen OK!/NO! flash.
    /// </summary>
    public class CitizenSelector : MonoBehaviour
    {
        private const float FeedbackSeconds = 1f;
        private const float MarkerHeight = 0.75f; // ~0.3 m over a citizen drawn at UnitSize.Scale
        private const float MarkerBobSpeed = 3f;
        private const float MarkerBobAmount = 0.08f;
        // How far a clicked point may be pulled onto walkable NavMesh (see TryResolveDestination).
        // Deliberately tight: this is the click's precision budget, and anything larger starts
        // silently sending citizens somewhere the player didn't point at. Enough to forgive a
        // click on a building's wall or a step into the water's edge, not enough to teleport the
        // order across a clearing.
        private const float DestinationSampleRadius = 2f;

        [SerializeField] private Camera targetCamera;
        [SerializeField] private Text feedbackText;
        [SerializeField] private LayerMask raycastMask = ~0;
        [SerializeField] private GameObject orderFlagPrefab;

        // Reused across clicks rather than using Physics.RaycastAll, which allocates a fresh array
        // every call. Sized generously: the camera looks down at a shallow angle, so the ray
        // travels tens of metres of forest before it reaches the cursor, piercing the click box of
        // every tree on the way. Overflowing the buffer isn't a truncation of far-away noise --
        // RaycastNonAlloc returns hits UNSORTED, so a full buffer can drop the very citizen or
        // tree that was clicked and keep a dozen irrelevant trunks instead.
        private readonly RaycastHit[] _hits = new RaycastHit[64];

        private CitizenAgent _selected;
        private GameObject _marker;
        private MoveOrderMarker _orderMarker;
        private Coroutine _feedbackRoutine;

        private MoveOrderMarker OrderMarker
        {
            get
            {
                if (_orderMarker != null) return _orderMarker;
                // Its own object, not this one: this selector lives under the UI canvas, and the
                // marker's pieces are world objects that must not inherit a canvas transform.
                var go = new GameObject("MoveOrderMarker");
                _orderMarker = go.AddComponent<MoveOrderMarker>();
                _orderMarker.Configure(orderFlagPrefab);
                return _orderMarker;
            }
        }

        private void Update()
        {
            if (ModalGate.IsBlocked) return;
            if (_selected == null) return;

            BobMarker();

            // Desktop escape hatches only. Touch has three of its own -- tap the citizen again,
            // tap a building, long press -- plus the HUD's cancel button. See WorldInputDispatcher.
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[Key.Escape].wasPressedThisFrame)
            {
                Deselect();
                return;
            }
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) Deselect();
        }

        /// <summary>True while a citizen is selected -- the dispatcher reads it to decide whether a world tap is an ORDER or plain selection.</summary>
        public bool HasSelection => _selected != null;

        /// <summary>The citizen picked up, or null (the tutorial waits on the one it showed).</summary>
        public CitizenAgent Selected => _selected;

        /// <summary>
        /// Plain selection, from browsing mode: picks up an idle citizen. Returns false when the
        /// tap met no citizen at all, so the dispatcher can pass it on to the building card.
        ///
        /// A BUSY citizen still consumes the tap, and says so out loud. Silence here is what made
        /// "I tapped my woodcutter and nothing happened" indistinguishable from a broken game:
        /// redirecting a working citizen is refused on purpose (it would desync the job
        /// bookkeeping CitizenVisualsManager keeps), but the refusal has to be visible.
        /// </summary>
        /// <summary>
        /// The tree or boulder under a tap, if any -- a plain tap on one shows how much is left in
        /// it (NodeYieldBubble). Null when the tap met no node.
        /// </summary>
        public ResourceNode FindNodeAt(Vector2 screenPosition)
        {
            if (targetCamera == null) return null;
            return NearestNode(RaycastWorld(screenPosition));
        }

        /// <summary>The tap within a finger's breadth of this point on the screen.</summary>
        private bool NearOnScreen(Vector3 world, Vector2 screen)
        {
            if (targetCamera == null) return false;
            var at = targetCamera.WorldToScreenPoint(world + Vector3.up * 0.6f);
            if (at.z <= 0f) return false;
            var reach = 140f * Screen.height / 1080f;
            return ((Vector2)at - screen).sqrMagnitude <= reach * reach;
        }

        /// <summary>The camera taps are read through.</summary>
        public Camera TapCamera => targetCamera;

        private ResourceNode NearestNode(int hitCount)
        {
            ResourceNode node = null;
            var nodeDistance = float.MaxValue;
            for (var i = 0; i < hitCount; i++)
            {
                if (_hits[i].distance >= nodeDistance) continue;
                var candidate = _hits[i].collider.GetComponentInParent<ResourceNode>();
                if (candidate == null) continue;
                node = candidate;
                nodeDistance = _hits[i].distance;
            }
            return node;
        }

        /// <summary>
        /// Picks this citizen up (the workforce panel's «Где», the user 2026-10-22: so orders can
        /// be given at once). Only a free one can take orders; for one at work it says he is busy
        /// and returns false.
        /// </summary>
        public bool SelectAgent(CitizenAgent citizen)
        {
            if (citizen == null) return false;
            if (!citizen.IsIdle)
            {
                ShowMessage(Localization.Get("#citizen_busy"), false);
                return false;
            }
            Select(citizen);
            return true;
        }

        public bool TrySelectCitizen(Vector2 screenPosition)
        {
            if (targetCamera == null) return false;

            var hitCount = RaycastWorld(screenPosition);
            var citizen = FindNearestCitizen(hitCount);
            if (citizen == null) return false;

            if (citizen.IsIdle) Select(citizen);
            else ShowMessage(Localization.Get("#citizen_busy"), false);

            return true;
        }

        /// <summary>
        /// A tap while a citizen is selected. Returns false only for the one case this mode
        /// deliberately hands on: a tap on a BUILDING, which drops the selection and lets the
        /// same tap open that building's card.
        /// </summary>
        public bool HandleWorldTap(Vector2 screenPosition)
        {
            if (targetCamera == null || _selected == null) return false;

            var hitCount = RaycastWorld(screenPosition);

            // Tapping the selected citizen again puts them down. On a phone this was the missing
            // half of selection -- there was no way to cancel one at all, at any point, so the
            // only way out was to select somebody else.
            var citizen = FindNearestCitizen(hitCount);
            if (citizen != null)
            {
                if (citizen == _selected) Deselect();
                else if (citizen.IsIdle) Select(citizen);
                else ShowMessage(Localization.Get("#citizen_busy"), false);
                return true;
            }

            // The nearest resource node along the ray, if the tap met one at all. Scanning every
            // hit rather than testing only the single nearest collider: a tree's click box is wide
            // enough that a boulder or a second tree standing just in front of the finger can be
            // the nearest hit while the thing actually under it is a few entries further down.
            //
            // GetComponentInParent, not GetComponent: a tree's click collider sits on its prefab
            // root but a boulder's cluster parts are children, and either could be what the
            // raycast reports depending on the model.
            var node = NearestNode(hitCount);
            // the tutorial: only the tree / boulder it shows, and a tap near it on the screen counts (the user,
            // 2026-11-06: a tree beside the boulder took the tap); any other tap is no order at all
            if (Tutorial.TutorialMode.IsActive && Tutorial.TutorialRules.WantedNode != null)
            {
                var wanted = Tutorial.TutorialRules.WantedNode;
                if (node != wanted && !NearOnScreen(wanted.transform.position, screenPosition)) return true;
                node = wanted;
            }
            if (node != null)
            {
                // An order, not a question: the citizen goes, no badge (the user, 2026-10-21).
                CommandGather(node);
                return true;
            }

            // A building is not a destination. Ordering someone to walk into a wall is what the
            // old code did here -- the tap fell through to the ground branch, landed on the
            // building's collider and resolved to the NavMesh next to its wall -- and it was
            // never once what the player meant.
            for (var i = 0; i < hitCount; i++)
            {
                if (_hits[i].collider.GetComponentInParent<BuildingInstance>() == null) continue;
                Deselect();
                return false;
            }

            if (hitCount == 0) return true;

            // Where the tap landed on the map, asked of the ground mesh itself (see
            // MeshMapApplier.TryRaycastGround) rather than read off whatever collider the ray met
            // first. Everything standing on the ground -- a tree's canopy box, a boulder, an
            // authored zone volume -- is metres above and beside the ground beneath it, and any of
            // those as "where the player tapped" skews the destination before the NavMesh sees it.
            var ray = targetCamera.ScreenPointToRay(screenPosition);
            var clickPoint = _hits[0].point;
            var solid = -1;
            for (var i = 0; i < hitCount; i++)
            {
                if (_hits[i].collider.isTrigger) continue;
                if (solid < 0 || _hits[i].distance < _hits[solid].distance) solid = i;
            }
            if (solid >= 0) clickPoint = _hits[solid].point;
            // No ground under the tap (water, off the map edge) leaves clickPoint as that
            // fallback, which TryResolveDestination then refuses with the usual red NO!.
            if (MeshMapApplier.Instance != null && MeshMapApplier.Instance.TryRaycastGround(ray, out var groundHit))
            {
                clickPoint = groundHit.point;
            }

            var resolved = TryResolveDestination(clickPoint, out var destination);

            ShowFeedback(resolved);
            if (!resolved)
            {
                OrderMarker.ShowRefused(clickPoint);
                return true;
            }

            // Order first, marker second: the marker draws the route MoveTo has just planned.
            _selected.MoveTo(destination);
            OrderMarker.ShowOrder(_selected, destination, true);
            return true;
        }

        /// <summary>
        /// Every hit along the ray, not just the nearest one. A tree's click collider is a box
        /// around its whole canopy, so with the camera looking down at an angle it sits between
        /// the finger and any citizen standing near that tree -- a plain Physics.Raycast returned
        /// the tree, the citizen check missed, and tapping your own people simply did nothing.
        /// </summary>
        private int RaycastWorld(Vector2 screenPosition)
        {
            var ray = targetCamera.ScreenPointToRay(screenPosition);
            return Physics.RaycastNonAlloc(ray, _hits, 500f, raycastMask);
        }

        /// <summary>The NEAREST citizen along the ray, not the first one the unsorted hit list happens to mention -- with two citizens under the finger that was choosing between them at random.</summary>
        private CitizenAgent FindNearestCitizen(int hitCount)
        {
            var best = -1;
            for (var i = 0; i < hitCount; i++)
            {
                if (_hits[i].collider.GetComponent<CitizenAgent>() == null) continue;
                if (best < 0 || _hits[i].distance < _hits[best].distance) best = i;
            }
            return best >= 0 ? _hits[best].collider.GetComponent<CitizenAgent>() : null;
        }

        /// <summary>
        /// Nearest walkable point to where the player clicked, straight from the NavMesh.
        ///
        /// Replaces a ring search over grid cells that was wrong in two ways at once. It treated
        /// tree cells as blocked (GridManager marks them occupied) even though trees are fully
        /// walkable now -- no NavMesh carving, trigger colliders only -- so in a forest almost
        /// every click got bounced to a different cell. And when it did bounce, it scanned each
        /// ring from -radius upward and took the first hit, which is always the (-x,-z) corner:
        /// a systematic shove down-and-left, exactly the offset the player measured.
        ///
        /// SamplePosition has neither problem: it returns the CLOSEST walkable point with no
        /// directional bias, and it answers about walkability rather than build-site occupancy,
        /// so a click on open forest floor resolves to itself.
        /// </summary>
        private static bool TryResolveDestination(Vector3 clickPoint, out Vector3 destination)
        {
            if (NavMesh.SamplePosition(clickPoint, out var navHit, DestinationSampleRadius, NavMesh.AllAreas))
            {
                destination = navHit.position;
                return true;
            }

            // No NavMesh at all (a failed bake -- see MeshMapApplier.BuildNavMesh, which warns):
            // fall back to the raw point if it's at least real ground, so click-to-move degrades
            // rather than refusing every order.
            if (MeshMapApplier.Instance != null && MeshMapApplier.Instance.IsGroundAt(clickPoint))
            {
                destination = clickPoint;
                return true;
            }

            destination = default;
            return false;
        }

        /// <summary>
        /// Sends the selected citizen to hand-gather the clicked tree/boulder (see
        /// CitizenAgent.GatherFrom). Refused, with the same red NO! feedback a bad move order
        /// gets, when the node is already spoken for by a Lumberjack worker or another citizen
        /// (ResourceNode.IsClaimed) or is still a growing sapling -- matching the rule
        /// CitizenVisualsManager.FindNearestFreeNode already applies to building workers.
        /// </summary>
        private void CommandGather(ResourceNode node)
        {
            var growth = node.GetComponent<TreeGrowth>();
            var isUnripeSapling = growth != null && !growth.IsFullyGrown;

            if (isUnripeSapling || !node.TryClaim())
            {
                ShowFeedback(false);
                OrderMarker.ShowRefused(node.transform.position);
                return;
            }

            _selected.GatherFrom(node);
            ShowFeedback(true);
            OrderMarker.ShowOrder(_selected, node.transform.position, false);
        }

        private void Select(CitizenAgent citizen)
        {
            // the tutorial: a person is picked only on its steps that ask for one (the user, 2026-11-06: picked again
            // and sent elsewhere, the step counted as done)
            if (Tutorial.TutorialMode.IsActive && !Tutorial.TutorialRules.CitizenSelectable) return;
            _selected = citizen;
            if (_marker == null) _marker = CreateMarker();
            _marker.SetActive(true);
            if (_orderMarker != null) _orderMarker.HideOrder();
        }

        /// <summary>Public because the HUD cancel button and the long-press escape hatch both reach it (see WorldInputDispatcher).</summary>
        public void Deselect()
        {
            _selected = null;
            if (_marker != null) _marker.SetActive(false);
            if (_orderMarker != null) _orderMarker.HideOrder();
        }

        private void BobMarker()
        {
            if (_marker == null || _selected == null) return;
            var bob = Mathf.Sin(Time.time * MarkerBobSpeed) * MarkerBobAmount;
            _marker.transform.position = _selected.transform.position + new Vector3(0f, MarkerHeight + bob, 0f);
            // the gem turns slowly; its glow faces the camera and breathes (the user's pick D, 2026-11-06)
            _marker.transform.rotation = Quaternion.Euler(0f, Time.time * GemTurnSpeed, 0f);
            if (_glow == null) return;
            var cam = Camera.main;
            if (cam != null) _glow.rotation = cam.transform.rotation;
            _glow.localScale = Vector3.one * (GlowSize * (1f + 0.12f * Mathf.Sin(Time.time * 3f)));
        }

        private const float GemTurnSpeed = 90f;     // degrees a second
        private const float GlowSize = 0.62f;
        private Transform _glow;

        /// <summary>
        /// The mark over the held person (the user's pick D, 2026-11-06, instead of the rough cube): a gold
        /// gem -- a tall octahedron, flat-shaded so its faces catch the light -- in a soft gold glow.
        /// </summary>
        private GameObject CreateMarker()
        {
            var marker = new GameObject("CitizenSelectionMarker", typeof(MeshFilter), typeof(MeshRenderer));
            marker.GetComponent<MeshFilter>().sharedMesh = GemMesh(0.11f, 0.17f);
            var gem = marker.GetComponent<MeshRenderer>();
            gem.sharedMaterial = new Material(RuntimeShaders.Lit) { color = new Color(1f, 0.82f, 0.18f) };
            gem.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var glow = new GameObject("Glow", typeof(MeshFilter), typeof(MeshRenderer));
            glow.transform.SetParent(marker.transform, false);
            glow.GetComponent<MeshFilter>().sharedMesh = QuadMesh();
            var halo = glow.GetComponent<MeshRenderer>();
            var material = MoveOrderMarker.CreateTransparentMaterial(new Color(1f, 0.88f, 0.4f, 0.55f));
            material.SetTexture("_BaseMap", GlowTexture());
            material.mainTexture = GlowTexture();
            halo.sharedMaterial = material;
            halo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            halo.receiveShadows = false;
            _glow = glow.transform;
            _glow.localScale = Vector3.one * GlowSize;
            return marker;
        }

        /// <summary>An octahedron `half` wide and `tall` from the middle to each tip, its faces unshared (flat light).</summary>
        private static Mesh GemMesh(float half, float tall)
        {
            var top = new Vector3(0f, tall, 0f);
            var bottom = new Vector3(0f, -tall, 0f);
            var ring = new[] { new Vector3(half, 0f, 0f), new Vector3(0f, 0f, half), new Vector3(-half, 0f, 0f), new Vector3(0f, 0f, -half) };
            var vertices = new System.Collections.Generic.List<Vector3>();
            for (var i = 0; i < 4; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % 4];
                vertices.Add(top); vertices.Add(b); vertices.Add(a);
                vertices.Add(bottom); vertices.Add(a); vertices.Add(b);
            }
            var triangles = new int[vertices.Count];
            for (var i = 0; i < triangles.Length; i++) triangles[i] = i;
            var mesh = new Mesh { name = "SelectionGem" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh QuadMesh()
        {
            var mesh = new Mesh { name = "SelectionGlow" };
            mesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Texture2D _glowTexture;

        /// <summary>A round soft glow, white (the material tints it), drawn once.</summary>
        private static Texture2D GlowTexture()
        {
            if (_glowTexture != null) return _glowTexture;
            const int size = 64;
            _glowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = (x + 0.5f) / size * 2f - 1f;
                var dy = (y + 0.5f) / size * 2f - 1f;
                var t = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255f * t * t));
            }
            _glowTexture.SetPixels32(pixels);
            _glowTexture.Apply(false, true);
            return _glowTexture;
        }

        private void ShowFeedback(bool ok)
        {
            // a taken order says nothing (the user, 2026-11-06: «OK» over the tree); a refused one still says so
            if (ok) return;
            ShowMessage("NO!", false);
        }

        /// <summary>The same one-second centre-screen flash, with words in it -- used for refusals that a bare "NO!" would leave the player guessing about.</summary>
        private void ShowMessage(string message, bool ok)
        {
            if (feedbackText == null) return;

            feedbackText.text = message;
            feedbackText.color = ok ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.95f, 0.35f, 0.3f);
            feedbackText.gameObject.SetActive(true);

            if (_feedbackRoutine != null) StopCoroutine(_feedbackRoutine);
            _feedbackRoutine = StartCoroutine(HideFeedbackAfterDelay());
        }

        private IEnumerator HideFeedbackAfterDelay()
        {
            yield return new WaitForSeconds(FeedbackSeconds);
            if (feedbackText != null) feedbackText.gameObject.SetActive(false);
            _feedbackRoutine = null;
        }
    }
}
