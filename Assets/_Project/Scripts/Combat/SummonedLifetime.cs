using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>A soldier the Queen's Призыв called up: gone when its time is up (ArmyManager.Dismiss).</summary>
    public class SummonedLifetime : MonoBehaviour
    {
        public float Remaining { get; private set; }

        public void Begin(float seconds) => Remaining = Mathf.Max(0.1f, seconds);

        private void Update()
        {
            Remaining -= Time.deltaTime;
            if (Remaining > 0f) return;
            enabled = false;
            ArmyManager.Instance?.Dismiss(GetComponent<SoldierUnit>());
        }
    }
}
