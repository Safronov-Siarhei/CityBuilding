using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// The King's Blessing (the user, 2026-10-23): for a while nothing of the player's takes damage
    /// -- buildings (BuildingInstance.TryDamage), soldiers and heroes (SoldierUnit.TakeDamage) and
    /// the people (OrcUnit's blows on a citizen). One clock for the whole settlement; the HUD's
    /// gold glow (HeroSkillBar) reads it.
    /// </summary>
    public static class HeroBlessing
    {
        private static float _until = -1f;

        public static bool Active => Time.time < _until;

        public static float Remaining => Mathf.Max(0f, _until - Time.time);

        public static void Begin(float seconds) => _until = Mathf.Max(_until, Time.time + Mathf.Max(0f, seconds));

        /// <summary>Over at once: a new run (InvasionMode) and the tests start clean.</summary>
        public static void End() => _until = -1f;
    }
}
