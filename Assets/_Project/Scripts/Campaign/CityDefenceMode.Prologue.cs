using System;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The level-3 prologue «Пленные» (the user's story 2026-11-22; storyboard, models and all three animation groups
    /// approved 2026-11-22/23; Design/features/campaign.md «Level 3»). It plays right after mission 2's last scene
    /// «Подкрепление», in the same burnt town, before the win: the King rides onto the square to Ардан and the Captain;
    /// a horseman reports a monster caught alive -- the cage cart in the street; a second has tracked the prisoners --
    /// the monsters' glade, the old man in a cage; Ардан wants to go, the King must take the monster to the capital,
    /// notices Ардан's amulet and gives him volunteers; the Captain goes with him. Then the win, and the campaign map
    /// offers level 3's two branches.
    ///
    /// The takes (Models/Cinematic/Cast/CapL3*.fbx; Campaign/L3Prologue/&lt;group&gt;.json --
    /// Tools/blender/cinematic/export_prologue.py) are laid out by Campaign/Level3Prologue.json
    /// (Tools/campaign/prologue_json.py) on Capital.cs's film machinery (StartFilm, `inTown`).
    /// </summary>
    public partial class CityDefenceMode
    {
        [SerializeField] private TextAsset prologueScript;                         // Campaign/Level3Prologue.json
        [SerializeField] private TextAsset[] prologueTakes = new TextAsset[0];      // Campaign/L3Prologue/*.json + M2Help/dead.json
        [System.NonSerialized] private GameObject[] prologueSets = new GameObject[0];    // Models/Cinematic/L3Glade*Set.fbx
        [SerializeField] private AudioClip prologueMusic;                          // Audio/Campaign/mus_gate.ogg (the square)
        [SerializeField] private AudioClip prologueMusic2;                         // Audio/Campaign/mus_portal.ogg (from the glade)

        /// <summary>The campaign map's film flag (Campaign/CampaignMap.json): watched once, level 3's branches open.</summary>
        public const string PrologueId = "m3p";

        private bool _prologueDone;

        /// <summary>Played from the campaign map's flag (the scene alone, nothing of the mission is running).</summary>
        public bool PrologueAlone { get; private set; }

        /// <summary>After «Подкрепление»: the prologue, then the win (the win at once when it cannot play).</summary>
        private void AfterHelp()
        {
            if (!StartPrologue(() => { CampaignProgress.MarkDone(PrologueId); Win(); })) Win();
        }

        /// <summary>From the map's film flag: the town sacked as «Подкрепление» leaves it, the prologue, back to the map.</summary>
        private void PlayPrologueAlone()
        {
            PrologueAlone = true;
            // Part1, not Won: Update steps a film only in Part1 (and while one plays, nothing of the siege runs)
            Current = Stage.Part1;
            if (!StartPrologue(PrologueOver)) PrologueOver();
        }

        private static void PrologueOver()
        {
            CampaignProgress.MarkDone(PrologueId);
            Time.timeScale = 1f;
            UI.LoadingScreen.ToCampaignMap("CampaignMap");
        }

        private bool StartPrologue(Action onEnd)
        {
            if (_prologueDone) return false;
            _prologueDone = true;
            var ended = false;
            void End()
            {
                ended = true;
                onEnd?.Invoke();
            }
            try
            {
                var on = StartFilm(prologueScript, prologueTakes, capitalFigures, capitalClips, capitalProps, prologueSets,
                    prologueMusic, prologueMusic2, null, true, End);
                // no darkness over the land's edge: from the square's low cameras it lay as a black band across the
                // houses (the user, 2026-11-23)
                if (on && _capital != null)
                {
                    _capital.dark = false;
                    Ravage(_capital.script);
                    _capital.onEnd = Ungloom(_capital.script.gloom, _capital.onEnd);
                }
                return on;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                AbortCapital();
                return ended;           // the abort ran its end already: the caller must not run it again
            }
        }

        /// <summary>The town as the prologue shows it (the user, 2026-11-23: «город не выглядит разрушенным»): houses round
        /// the square burnt out and burning (on top of what «Подкрепление» left), both gates broken.</summary>
        private void Ravage(CapitalScript sc)
        {
            Vector3? near = null;
            if (sc.sackNear != null && sc.sackNear.Length == 2) near = World(sc.sackNear[0], sc.sackNear[1]);
            SackTown(null, sc.ruins, sc.fires, sc.fireSize, near, sc.sackJitter);
            if (!sc.breakGates) return;
            foreach (var gates in new[] { OuterGates, InnerGates })
            {
                var g = gates[0];
                if (g == null) continue;
                var spot = g.transform.position;
                for (var k = 0; k < gates.Length; k++) gates[k] = null;     // every lane's entry is this one gate
                Ruin(g);
                Fire(new Vector3(spot.x, GroundAt(spot) + 0.3f, spot.z), Mathf.Max(0.3f, sc.fireSize * 0.6f));
            }
        }

        private class Lighting
        {
            public CameraClearFlags clear;
            public Color background, fogColor, ambient, sky, equator, ground, sunColor;
            public bool fog;
            public FogMode fogMode;
            public float fogStart, fogEnd, ambientIntensity, sunIntensity;
        }

        /// <summary>The smoke-dark sky on (the camera's sky a flat colour, fog of it, the sun and ambient down); the
        /// returned end puts it all back before the film's own end runs.</summary>
        private Action Ungloom(Gloom g, Action onEnd)
        {
            if (g == null || g.sky == null || g.sky.Length < 3) return onEnd;
            var cam = Camera.main;
            var sun = RenderSettings.sun;
            if (sun == null)
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type == LightType.Directional) { sun = l; break; }
            var was = new Lighting
            {
                clear = cam != null ? cam.clearFlags : CameraClearFlags.Skybox,
                background = cam != null ? cam.backgroundColor : Color.black,
                fog = RenderSettings.fog, fogMode = RenderSettings.fogMode, fogColor = RenderSettings.fogColor,
                fogStart = RenderSettings.fogStartDistance, fogEnd = RenderSettings.fogEndDistance,
                ambientIntensity = RenderSettings.ambientIntensity, ambient = RenderSettings.ambientLight,
                sky = RenderSettings.ambientSkyColor, equator = RenderSettings.ambientEquatorColor, ground = RenderSettings.ambientGroundColor,
                sunIntensity = sun != null ? sun.intensity : 0f, sunColor = sun != null ? sun.color : Color.white,
            };
            var tint = new Color(g.sky[0], g.sky[1], g.sky[2]);
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = tint;
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = tint;
            RenderSettings.fogStartDistance = g.fogStart;
            RenderSettings.fogEndDistance = g.fogEnd;
            RenderSettings.ambientIntensity = was.ambientIntensity * g.ambient;
            RenderSettings.ambientLight = was.ambient * g.ambient;
            RenderSettings.ambientSkyColor = was.sky * g.ambient;
            RenderSettings.ambientEquatorColor = was.equator * g.ambient;
            RenderSettings.ambientGroundColor = was.ground * g.ambient;
            if (sun != null)
            {
                sun.intensity = was.sunIntensity * g.sun;
                if (g.sunColor != null && g.sunColor.Length >= 3) sun.color = new Color(g.sunColor[0], g.sunColor[1], g.sunColor[2]);
            }
            return () =>
            {
                if (cam != null)
                {
                    cam.clearFlags = was.clear;
                    cam.backgroundColor = was.background;
                }
                RenderSettings.fog = was.fog;
                RenderSettings.fogMode = was.fogMode;
                RenderSettings.fogColor = was.fogColor;
                RenderSettings.fogStartDistance = was.fogStart;
                RenderSettings.fogEndDistance = was.fogEnd;
                RenderSettings.ambientIntensity = was.ambientIntensity;
                RenderSettings.ambientLight = was.ambient;
                RenderSettings.ambientSkyColor = was.sky;
                RenderSettings.ambientEquatorColor = was.equator;
                RenderSettings.ambientGroundColor = was.ground;
                if (sun != null)
                {
                    sun.intensity = was.sunIntensity;
                    sun.color = was.sunColor;
                }
                onEnd?.Invoke();
            };
        }
    }
}
