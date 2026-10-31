using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Maps.Archipelago
{
    /// <summary>The storm's wave crests rolling in and out: each side's crests swing along its inward way. Four transforms a frame.</summary>
    public class StormSwell : MonoBehaviour
    {
        private const float Reach = 0.9f;     // metres in and out
        private const float Period = 3.2f;    // seconds a swing

        private readonly List<(Transform t, Vector3 way, Vector3 home, float phase)> _sides = new List<(Transform, Vector3, Vector3, float)>();

        public void Add(Transform side, Vector3 inward, float phase) => _sides.Add((side, inward, side.localPosition, phase));

        private void Update()
        {
            var time = Time.time * Mathf.PI * 2f / Period;
            foreach (var (t, way, home, phase) in _sides)
                if (t != null) t.localPosition = home + way * (Mathf.Sin(time + phase) * Reach);
        }
    }
}
