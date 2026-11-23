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
        [SerializeField] private GameObject[] prologueSets = new GameObject[0];    // Models/Cinematic/L3Glade*Set.fbx
        [SerializeField] private AudioClip prologueMusic;                          // Audio/Campaign/mus_gate.ogg (the square)
        [SerializeField] private AudioClip prologueMusic2;                         // Audio/Campaign/mus_portal.ogg (from the glade)

        private bool _prologueDone;

        /// <summary>After «Подкрепление»: the prologue, then the win (the win at once when it cannot play).</summary>
        private void AfterHelp()
        {
            if (!StartPrologue()) Win();
        }

        private bool StartPrologue()
        {
            if (_prologueDone) return false;
            _prologueDone = true;
            try
            {
                return StartFilm(prologueScript, prologueTakes, capitalFigures, capitalClips, capitalProps, prologueSets,
                    prologueMusic, prologueMusic2, null, true, Win);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                AbortCapital();
                return Current == Stage.Won;
            }
        }
    }
}
