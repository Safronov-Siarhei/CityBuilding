using System;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The house going up in the main menu's village (concept 1 stage 2, approved by the user 2026-11-17):
    /// the half-built house with its scaffold, the plank pile and stack, the sawhorse; the carpenter on the
    /// scaffold hammering, the labourer carrying planks from the pile to the stack -- both on loops, so
    /// the house never grows between visits (the user). The site's own layout and the labourer's and the
    /// plank's paths come from Campaign/MenuBuilders.json (Tools/blender/cinematic/export_builders.py);
    /// where the site stands in the village from Campaign/MenuVillage.json ("buildSite"). The village
    /// keeps that ground clear (asked in Awake, before it builds in Start).
    /// </summary>
    public class MenuBuildSite : MonoBehaviour
    {
        [Serializable] private class SiteFile
        {
            /// <summary>x, z, yaw (degrees) of the site's origin in the village.</summary>
            public float[] buildSite;
            /// <summary>x, z, radius in the site's frame: the ground the village leaves empty.</summary>
            public float[] buildSiteKeep;
        }

        [Serializable] private class Piece { public string id; public float[] p; public float[] r; }

        [Serializable] private class Worker
        {
            public string figure;
            public string clip;
            public float[] at;
            public float yaw;
            public float seconds;
            public int frames;
            public float[] root;
        }

        [Serializable] private class Plank { public string piece; public int frames; public float[] m; }

        [Serializable] private class Layout
        {
            public float fps;
            public Piece[] pieces;
            public Worker carpenter;
            public Worker labourer;
            public Plank plank;
        }

        [SerializeField] private OpeningVillage village;
        [SerializeField] private TextAsset siteFile;
        [SerializeField] private TextAsset layoutFile;
        [SerializeField] private OpeningLife.CastMember[] cast = new OpeningLife.CastMember[0];

        private Layout _l;
        private Transform _root;
        private CineFigure _carpenter, _labourer;
        private Transform _plank;
        private float _t;

        private void Awake()
        {
            if (siteFile == null || layoutFile == null || village == null) return;
            var site = JsonUtility.FromJson<SiteFile>(siteFile.text);
            if (site?.buildSite == null || site.buildSite.Length < 3) return;
            _l = JsonUtility.FromJson<Layout>(layoutFile.text);
            _root = new GameObject("BuildSite").transform;
            _root.SetParent(transform, false);
            _root.SetPositionAndRotation(new Vector3(site.buildSite[0], 0f, site.buildSite[1]), Quaternion.Euler(0f, site.buildSite[2], 0f));
            if (site.buildSiteKeep != null && site.buildSiteKeep.Length >= 3)
            {
                var c = _root.TransformPoint(new Vector3(site.buildSiteKeep[0], 0f, site.buildSiteKeep[1]));
                village.KeepClear(new Vector2(c.x, c.z), site.buildSiteKeep[2]);
            }
        }

        private void Start()
        {
            if (_l == null) return;
            foreach (var p in _l.pieces) Spawn(p.id, V(p.p, 0), Q(p.r, 0));
            if (_l.plank != null) _plank = Spawn(_l.plank.piece, Vector3.zero, Quaternion.identity);
            _carpenter = Figure(_l.carpenter);
            if (_carpenter != null)
                _carpenter.transform.SetLocalPositionAndRotation(V(_l.carpenter.at, 0), Quaternion.Euler(0f, _l.carpenter.yaw, 0f));
            _labourer = Figure(_l.labourer);
            Step(0f);
        }

        private void Update() => Step(Time.deltaTime);

        /// <summary>Moves the loops on by dt (a test or the editor can step it by hand).</summary>
        public void Step(float dt)
        {
            if (_l == null) return;
            _t += dt;
            if (_carpenter != null) _carpenter.Tick(dt);
            var w = _l.labourer;
            if (_labourer != null && w != null && w.frames > 1 && w.root != null)
            {
                var (i, j, f) = Frame(w.seconds, w.frames);
                var r = w.root;
                var p = Vector3.Lerp(new Vector3(r[i * 4], r[i * 4 + 1], r[i * 4 + 2]), new Vector3(r[j * 4], r[j * 4 + 1], r[j * 4 + 2]), f);
                _labourer.transform.SetLocalPositionAndRotation(p, Quaternion.Euler(0f, Mathf.LerpAngle(r[i * 4 + 3], r[j * 4 + 3], f), 0f));
                _labourer.Tick(dt);
                if (_plank != null && _l.plank?.m != null)
                    _plank.SetLocalPositionAndRotation(Vector3.Lerp(V(_l.plank.m, i * 7), V(_l.plank.m, j * 7), f),
                        Quaternion.Slerp(Q(_l.plank.m, i * 7 + 3), Q(_l.plank.m, j * 7 + 3), f) * _plankModelRot);
            }
        }

        private Quaternion _plankModelRot = Quaternion.identity;

        private (int i, int j, float f) Frame(float seconds, int frames)
        {
            var x = (_t % seconds) / seconds * (frames - 1);
            var i = Mathf.Min((int)x, frames - 1);
            return (i, Mathf.Min(i + 1, frames - 1), x - i);
        }

        private Transform Spawn(string id, Vector3 at, Quaternion rot)
        {
            var model = village.SetPieceModel(id);
            if (model == null) return null;
            var go = Instantiate(model, _root);
            CineTwoSided.Apply(go);
            go.name = id;
            go.transform.SetLocalPositionAndRotation(at, rot * model.transform.rotation);
            if (_l.plank != null && id == _l.plank.piece) _plankModelRot = model.transform.rotation;
            return go.transform;
        }

        private CineFigure Figure(Worker w)
        {
            if (w == null) return null;
            var member = Array.Find(cast, m => m.id == w.figure);
            if (member.model == null) return null;
            var go = Instantiate(member.model, _root);
            go.name = w.figure;
            var fig = go.AddComponent<CineFigure>();
            fig.Init(member.clips);
            fig.Play(w.clip, 0f);
            return fig;
        }

        private static Vector3 V(float[] a, int o) => a != null && a.Length >= o + 3 ? new Vector3(a[o], a[o + 1], a[o + 2]) : Vector3.zero;

        private static Quaternion Q(float[] a, int o) => a != null && a.Length >= o + 4 ? new Quaternion(a[o], a[o + 1], a[o + 2], a[o + 3]) : Quaternion.identity;
    }
}
