using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// Crows circling over the orcs' main base (the user, 2026-11-06: crows instead of a storm cloud):
    /// one flock over the skull at the gate, one wider over the base. Each crow keeps its own circle,
    /// height and speed, banks into the turn and beats its wings (the static model squashed across).
    /// One Update for the whole flock, no physics; gone with the skeleton it is part of.
    /// </summary>
    public class CrowFlock : MonoBehaviour
    {
        private const int OverSkull = 7;
        private const int OverBase = 9;
        private const float Scale = 1.4f;

        private struct Crow
        {
            public Transform body;
            public Vector3 centre;
            public float radius, height, speed, angle, phase;
        }

        private readonly List<Crow> _crows = new List<Crow>();

        public void Initialize(GameObject model, Vector3 skull, Vector3 baseCentre)
        {
            var random = new System.Random(Mathf.RoundToInt(skull.x * 13f + skull.z * 7f));
            float Range(float a, float b) => a + (float)random.NextDouble() * (b - a);
            for (var i = 0; i < OverSkull + OverBase; i++)
            {
                var overSkull = i < OverSkull;
                var crow = Instantiate(model, transform);
                crow.name = "Crow";
                crow.transform.localScale = Vector3.one * Scale;
                _crows.Add(new Crow
                {
                    body = crow.transform,
                    centre = overSkull ? skull : baseCentre,
                    radius = overSkull ? Range(4f, 8f) : Range(7f, 15f),
                    height = overSkull ? Range(9f, 14f) : Range(12f, 18f),
                    speed = Range(0.35f, 0.6f) * (random.NextDouble() < 0.3 ? -1f : 1f),
                    angle = Range(0f, Mathf.PI * 2f),
                    phase = Range(0f, 10f),
                });
            }
            SceneryShadows.DisableCasting(gameObject);
        }

        private void Update()
        {
            var t = Time.time;
            var dt = Time.deltaTime;
            for (var i = 0; i < _crows.Count; i++)
            {
                var c = _crows[i];
                if (c.body == null) continue;
                c.angle += c.speed * dt;
                _crows[i] = c;
                var bob = Mathf.Sin(t * 0.7f + c.phase) * 0.8f;
                c.body.position = c.centre + new Vector3(Mathf.Cos(c.angle) * c.radius, c.height + bob, Mathf.Sin(c.angle) * c.radius);
                // flying along the circle, banked into it
                var heading = new Vector3(-Mathf.Sin(c.angle), 0f, Mathf.Cos(c.angle)) * Mathf.Sign(c.speed);
                var bank = 18f * Mathf.Sign(c.speed);
                c.body.rotation = Quaternion.LookRotation(heading, Vector3.up) * Quaternion.Euler(0f, 0f, bank);
                // the wings beat: the model squashed across, now and then a glide
                var beat = Mathf.Sin(t * 9f + c.phase * 3f);
                var glide = Mathf.Sin(t * 0.5f + c.phase) > 0.4f ? 0.15f : 1f;
                c.body.localScale = new Vector3(Scale * (1f - 0.35f * glide * (0.5f + 0.5f * beat)), Scale, Scale);
            }
        }
    }
}
