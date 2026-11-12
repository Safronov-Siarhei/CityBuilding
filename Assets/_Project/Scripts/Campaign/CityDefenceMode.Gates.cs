using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The gates SHUT (the user's third playtest, 2026-11-12: «делай модель закрытых ворот и анимацию их закрытия»;
    /// models approved the same day, Tools/blender/gate_close_drafts.py). Each of the mission's two gates (the
    /// level-3 Barbican, its doors only painted on) gets a dark passage over both faces, two oak leaves on the town
    /// face and a portcullis on the field face. They stand OPEN, and the part's first wave shuts its gate: the
    /// leaves swing shut over m2_gate_close_sec and the portcullis drops behind them. A broken gate takes them with it.
    /// Model axes: Blender (x, y, z) lands on Unity (x, z, y) -- the gatehouse's passage runs along its local x.
    /// </summary>
    public partial class CityDefenceMode
    {
        [SerializeField] private GameObject gatePassageModel;   // Models/Buildings/FortifiedGate1-Passage.fbx
        [SerializeField] private GameObject gateLeafModel;      // Models/Buildings/FortifiedGate1-Leaf.fbx
        [SerializeField] private GameObject gateGrilleModel;    // Models/Buildings/FortifiedGate1-Grille.fbx

        // gatehouse sizes in cells (gate_close_drafts.py)
        private const float GateFace = 0.38f, GateHalfWidth = 0.21f, GrilleUp = 0.52f;

        private class Doors
        {
            public BuildingInstance gate;
            public Transform left, right, grille;
            public float shut;       // 0 open .. 1 shut
            public bool closing;
        }

        private readonly List<Doors> _doors = new List<Doors>();

        private Doors DoorsOf(BuildingInstance gate)
        {
            foreach (var d in _doors)
                if (d.gate == gate) return d;
            return null;
        }

        /// <summary>The leaves and the portcullis on a gate, open.</summary>
        private void HangDoors(BuildingInstance gate)
        {
            if (gate == null || gateLeafModel == null || DoorsOf(gate) != null) return;
            var cell = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
            var pivot = new GameObject("M2GateDoors").transform;
            pivot.SetParent(gate.transform, false);
            // the town face first: the gatehouse's local +x if it points into the town (south, -z), else turned round
            var townward = Vector3.Dot(gate.transform.right, Vector3.back) >= 0f;
            pivot.localRotation = townward ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            pivot.localPosition = Vector3.zero;
            pivot.localScale = Vector3.one * (cell / Mathf.Max(0.01f, gate.transform.lossyScale.x));
            if (gatePassageModel != null) Instantiate(gatePassageModel, pivot, false).name = "Passage";
            var d = new Doors { gate = gate };
            d.left = Hinge(pivot, "LeafL", new Vector3(GateFace + 0.004f, 0f, -GateHalfWidth), false);
            d.right = Hinge(pivot, "LeafR", new Vector3(GateFace + 0.004f, 0f, GateHalfWidth), true);
            if (gateGrilleModel != null)
            {
                d.grille = Instantiate(gateGrilleModel, pivot, false).transform;
                d.grille.name = "Grille";
            }
            _doors.Add(d);
            Pose(d);
        }

        private Transform Hinge(Transform pivot, string name, Vector3 at, bool mirrored)
        {
            var hinge = new GameObject(name).transform;
            hinge.SetParent(pivot, false);
            hinge.localPosition = at;
            var leaf = Instantiate(gateLeafModel, hinge, false);
            leaf.name = "Leaf";
            // the leaf runs along its local +z from the hinge; the right one mirrored runs along -z
            if (mirrored) leaf.transform.localScale = Vector3.Scale(leaf.transform.localScale, new Vector3(1f, 1f, -1f));
            return hinge;
        }

        /// <summary>Open (0): both leaves at right angles to the wall, into the town; shut (1): across the passage.</summary>
        private static void Pose(Doors d)
        {
            var k = d.shut * d.shut * (3f - 2f * d.shut);
            var open = 90f * (1f - k);
            if (d.left != null) d.left.localRotation = Quaternion.Euler(0f, open, 0f);
            if (d.right != null) d.right.localRotation = Quaternion.Euler(0f, -open, 0f);
            // the portcullis drops in the second half, behind the leaves
            var drop = Mathf.Clamp01(d.shut * 2f - 1f);
            if (d.grille != null) d.grille.localPosition = new Vector3(-GateFace - 0.03f, GrilleUp * (1f - drop * drop), 0f);
        }

        private void ShutGate(BuildingInstance gate)
        {
            if (gate == null) return;
            HangDoors(gate);
            var d = DoorsOf(gate);
            if (d != null) d.closing = true;
        }

        private void TickGates(float dt)
        {
            var seconds = Mathf.Max(0.2f, B.gateCloseSeconds);
            for (var i = _doors.Count - 1; i >= 0; i--)
            {
                var d = _doors[i];
                if (d.gate == null || d.left == null) { _doors.RemoveAt(i); continue; }
                if (!d.closing || d.shut >= 1f) continue;
                d.shut = Mathf.Min(1f, d.shut + dt / seconds);
                Pose(d);
            }
        }
    }
}
