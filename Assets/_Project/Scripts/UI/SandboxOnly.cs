using CityBuilder.Invasion;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// A HUD piece that means nothing in an Invasion run and goes once one is under way -- the
    /// inventory's sack (the user, 2026-10-28: a run has wood, stone and gold, all on the bar).
    /// </summary>
    public class SandboxOnly : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (InvasionMode.IsActive) gameObject.SetActive(false);
        }
    }
}
