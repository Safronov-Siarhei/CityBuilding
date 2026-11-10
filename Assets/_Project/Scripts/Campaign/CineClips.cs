using System;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// One cutscene figure's clips, baked from its FBX takes at setup (SetupProjectCampaign): every
    /// bone's rotation a frame, and the position of the few bones that move (the hips' bob, the rig
    /// root in the champion's leap). CineFigure plays them by writing the bones itself -- no Animator,
    /// so it works the same in the editor (photos) and in play, and costs a few dozen slerps a figure.
    /// </summary>
    public class CineClips : ScriptableObject
    {
        [Serializable]
        public class Clip
        {
            public string name;
            public float seconds;
            public bool loop;
            public int frames;          // samples, the last one at `seconds`
            public float[] rot;         // frames x bones x (x, y, z, w)
            public float[] pos;         // frames x moving bones x (x, y, z)
        }

        public const float FramesPerSecond = 15f;

        /// <summary>Bone paths under the model's root, as AnimationUtility names them.</summary>
        public string[] bones = new string[0];
        /// <summary>Indices into `bones` of the bones whose position is keyed.</summary>
        public int[] movingBones = new int[0];
        public Clip[] clips = new Clip[0];
        /// <summary>Metres a second at which Walk and Run keep the feet on the ground (OpeningCast.json).</summary>
        public float walkSpeed = 1f;
        public float runSpeed = 1f;

        public Clip Find(string clipName)
        {
            foreach (var c in clips)
            {
                if (c.name == clipName) return c;
            }
            return null;
        }
    }
}
