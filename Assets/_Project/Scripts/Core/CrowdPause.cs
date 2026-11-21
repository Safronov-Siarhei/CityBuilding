using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// A walker pressed into a crowd stops pushing for a moment (the user, 2026-11-21, «разбери»): in a 120-a-side fight
    /// in a town the back ranks of orcs and soldiers called CharacterController.Move every frame against the backs in
    /// front -- ~80-100 us a step on the Redmi Pad, growing with the crowd's density, the reason a battle's cost rose
    /// faster than its size. One that got under a quarter of its step for `StuckSeconds` waits `WaitSeconds` (jittered)
    /// before trying again; on the screen it is the same standing in the back rank.
    /// </summary>
    public struct CrowdPause
    {
        private const float StuckSeconds = 0.4f;
        private const float WaitSeconds = 0.4f;
        private float _stuck;
        private float _wait;

        /// <summary>True while it waits: skip the step this frame.</summary>
        public bool Waiting(float dt)
        {
            if (_wait <= 0f) return false;
            _wait -= dt;
            return true;
        }

        /// <summary>After a step: `meant` metres asked for, the position before and after it.</summary>
        public void After(Vector3 before, Vector3 after, float meant, float dt)
        {
            if (meant <= 0.0001f) return;
            var moved = after - before;
            moved.y = 0f;
            if (moved.magnitude >= meant * 0.25f) { _stuck = 0f; return; }
            _stuck += dt;
            if (_stuck < StuckSeconds) return;
            _stuck = 0f;
            _wait = WaitSeconds * (0.75f + 0.5f * Random.value);
        }
    }
}
