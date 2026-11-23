using System;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 2's last scene «Подкрепление» (the user's story 2026-11-16; all five animation groups approved the same
    /// day; Design/features/campaign.md «END SCENE»). It plays when the clock runs out, instead of «every orc dies»:
    /// orcs raze a house (it burns, and others with it), Ардан, the captain and two guards fight their last on the square
    /// until the horn; the King and a great column of cavalry gallop down the north road, burst through the gate and ride
    /// the orcs down in the street; the last orcs run for the gate with two riders at their heels. Then the win.
    ///
    /// The takes (Models/Cinematic/Cast/Cap*.fbx; Campaign/M2Help/&lt;group&gt;.json --
    /// Tools/blender/cinematic/export_m2help.py) are laid out by Campaign/Mission2Help.json in the town itself: a place
    /// is a plan cell (`cell`); the raze's is the north face of the standing house nearest it (`anchor` "house"). The
    /// cavalry's copies are more `uses` of the same group, offset, with only the riders and their horses. It runs on
    /// Capital.cs's film machinery (StartFilm, `inTown`): the game stands, the town's living units are hidden, the
    /// darkness over the land's edge stays; «Пропустить» runs it to its end.
    /// </summary>
    public partial class CityDefenceMode
    {
        [SerializeField] private TextAsset helpScript;                         // Campaign/Mission2Help.json
        [SerializeField] private TextAsset[] helpTakes = new TextAsset[0];      // Campaign/M2Help/*.json (figures and props: capital*, all Cap*)
        [SerializeField] private GameObject[] helpSets = new GameObject[0];    // Models/Cinematic/M2HelpBridgeSet.fbx
        [SerializeField] private AudioClip helpMusic;                          // Audio/Campaign/mus_attack.ogg (the fight)
        [SerializeField] private AudioClip helpMusic2;                         // Audio/Campaign/mus_gate.ogg (the cavalry)

        private bool _noHelp;
        private bool _helpDone;

        /// <summary>The last scene is playing (the play-mode check).</summary>
        public bool HelpPlaying => _capital != null && _capital.inTown;

        /// <summary>The last scene's clock (s), -1 when it does not play.</summary>
        public float HelpTime => HelpPlaying ? _capital.time : -1f;

        /// <summary>The cheats' «Перемотать к концовке» (the user, 2026-11-16): the clock out now, the scene again even if
        /// it has played (scene 2 counted as seen, so it does not come first).</summary>
        public void CheatToHelp()
        {
            if (!_active || Current != Stage.Part1 || _capital != null) return;
            _helpDone = false;
            _prologueDone = false;
            _noHelp = false;
            _capitalDone = true;
            TimeLeft = 0.01f;
        }

        /// <summary>The last scene run to its end at once, as «Пропустить» does (the play-mode check).</summary>
        public void SkipHelp()
        {
            for (var i = 0; i < 10000 && HelpPlaying; i++) TickCapital(1f / 15f);
        }

        /// <summary>The last scene, once: true when it started (the win comes at its end), false = win now.</summary>
        private bool StartHelp()
        {
            if (_helpDone || _noHelp) return false;
            _helpDone = true;
            try
            {
                return StartFilm(helpScript, helpTakes, capitalFigures, capitalClips, capitalProps, helpSets,
                    helpMusic, helpMusic2, introHorn, true, AfterHelp);   // then the level-3 prologue (Prologue.cs), then the win
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                AbortCapital();
                return Current == Stage.Won;
            }
        }

        /// <summary>A place of the last scene in the town: its plan cell on the ground -- or, `anchor` "house", a step
        /// north of the north face of the standing house nearest it (the raze take's wall at its Blender y 1); that
        /// house burns, `fires` more with it, `ruins` houses are burnt shells.</summary>
        private Vector3 PlaceInTown(Place p, CapitalScript script)
        {
            var at = World(p.cell[0], p.cell[1]);
            if (p.anchor == "house")
            {
                BuildingInstance best = null;
                var bd = float.MaxValue;
                foreach (var h in _houses)
                {
                    if (h == null || h.CurrentHealth <= 0) continue;
                    var d = Flat(h.transform.position - at);
                    if (d < bd) { bd = d; best = h; }
                }
                if (best != null)
                {
                    var c = best.transform.position;
                    at = new Vector3(c.x, at.y, c.z + 2f);      // a 2x2 house: its face 1 north of its middle, + the step
                    Burn(best, script.fireSize);
                }
                SackTown(best, script.ruins, script.fires, script.fireSize);
            }
            at.y = GroundAt(at);
            return at;
        }

        /// <summary>The town sacked (the user, 2026-11-16: «город не похож на атакованный орками, нет пожаров и руин»):
        /// `ruins` houses left burnt shells (some still smoking), `fires` more burning -- never `best`'s row (the raze
        /// take needs its walls). The level-3 prologue played alone from the map does it too.</summary>
        private void SackTown(BuildingInstance best, int ruins, int fires, float fireSize)
        {
            var rnd = new System.Random(11);
            var pool = new System.Collections.Generic.List<BuildingInstance>();
            foreach (var h in _houses)
                if (h != null && h != best && h.CurrentHealth > 0 && (best == null || Flat(h.transform.position - best.transform.position) > 4f))
                    pool.Add(h);
            for (var i = pool.Count - 1; i > 0; i--)
            {
                var j = rnd.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            var n = 0;
            foreach (var h in pool)
            {
                if (n < ruins)
                {
                    var spot = h.transform.position;
                    Ruin(h);
                    if (n % 2 == 0) Fire(new Vector3(spot.x, GroundAt(spot) + 0.3f, spot.z), Mathf.Max(0.3f, fireSize * 0.7f));
                }
                else if (n < ruins + fires) Burn(h, fireSize);
                else break;
                n++;
            }
        }

        /// <summary>Flames out of a house's roof (Look.cs's fire; it burns on after the scene).</summary>
        private void Burn(BuildingInstance house, float size)
        {
            var rs = house.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            Fire(new Vector3(b.center.x, Mathf.Lerp(b.min.y, b.max.y, 0.75f), b.center.z), Mathf.Max(0.3f, size));
        }

        /// <summary>The town's living off while the scene shows its own: the orcs for good (they die with the win), the
        /// soldiers, Ардан and the townsfolk back at the end.</summary>
        private static void HideTheLiving(CapitalState s)
        {
            foreach (var orc in OrcUnit.All)
                if (orc != null) Hide(orc, null);
            foreach (var man in SoldierUnit.All)
                if (man != null) Hide(man, s);
            foreach (var who in FindObjectsByType<CitizenAgent>(FindObjectsSortMode.None))
                if (who != null) Hide(who, s);
        }

        private static void Hide(Component c, CapitalState back)
        {
            foreach (var r in c.GetComponentsInChildren<Renderer>())
            {
                if (r == null || !r.enabled) continue;
                r.enabled = false;
                back?.unseen.Add(r);
            }
        }
    }
}
