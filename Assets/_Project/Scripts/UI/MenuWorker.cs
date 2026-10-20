using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// A worker in the main menu's scene: the game's cube citizen (a body and a head, the same
    /// sizes and clothes) with an arm and a tool, so that what he does reads from the camera --
    /// an axe falling on a trunk, a pick on a boulder, a hoe in a furrow, a hammer on a beam --
    /// and a carrier walking a load to and fro. Animated in place by a few sines; nothing here
    /// knows about the game.
    /// </summary>
    public class MenuWorker : MonoBehaviour
    {
        public enum Tool { None, Axe, Pick, Hoe, Hammer, Log, Stone, Plank }
        public enum Motion { Chop, Hoe, Hammer, Carry }

        private static readonly Color Skin = new Color(0.85f, 0.68f, 0.55f);
        private static readonly Color Handle = new Color(0.45f, 0.3f, 0.16f);
        private static readonly Color Iron = new Color(0.55f, 0.56f, 0.6f);
        private static readonly Color StoneColour = new Color(0.6f, 0.58f, 0.55f);

        private Motion _motion;
        private Transform _arm;
        private Transform _body;
        private float _phase;
        private float _speed;
        private Vector3 _from;
        private Vector3 _to;
        private bool _walks;

        private static Material Lit(Color color) => new Material(RuntimeShaders.Lit) { color = color };

        public void Build(Color clothing, Tool tool, Motion motion, float phase)
        {
            _motion = motion;
            _phase = phase * 10f;
            _speed = motion == Motion.Hammer ? 7f : motion == Motion.Hoe ? 3.2f : 4.2f;
            transform.localScale = Vector3.one * UnitSize.Scale;

            _body = new GameObject("Figure").transform;
            _body.SetParent(transform, false);
            var cloth = Lit(clothing);
            Part(_body, new Vector3(0f, 0.25f, 0f), new Vector3(0.3f, 0.5f, 0.3f), cloth);
            Part(_body, new Vector3(0f, 0.61f, 0f), new Vector3(0.22f, 0.22f, 0.22f), Lit(Skin));

            // the arm hangs from the right shoulder; the tool is in its hand
            _arm = new GameObject("Arm").transform;
            _arm.SetParent(_body, false);
            _arm.localPosition = new Vector3(0.19f, 0.44f, 0f);
            Part(_arm, new Vector3(0f, -0.14f, 0f), new Vector3(0.09f, 0.3f, 0.09f), cloth);
            AddTool(tool);
        }

        /// <summary>Walks between two points, turning at each end (a carrier, a ploughman).</summary>
        public void Route(Vector3 from, Vector3 to)
        {
            _from = from;
            _to = to;
            _walks = true;
        }

        private void AddTool(Tool tool)
        {
            var hand = new Vector3(0f, -0.3f, 0.03f);
            switch (tool)
            {
                case Tool.Axe:
                    Part(_arm, hand + new Vector3(0f, 0f, 0.16f), new Vector3(0.04f, 0.04f, 0.42f), Lit(Handle));
                    Part(_arm, hand + new Vector3(0f, 0.05f, 0.34f), new Vector3(0.03f, 0.14f, 0.1f), Lit(Iron));
                    break;
                case Tool.Pick:
                    Part(_arm, hand + new Vector3(0f, 0f, 0.16f), new Vector3(0.04f, 0.04f, 0.42f), Lit(Handle));
                    Part(_arm, hand + new Vector3(0f, 0f, 0.36f), new Vector3(0.03f, 0.3f, 0.04f), Lit(Iron));
                    break;
                case Tool.Hoe:
                    Part(_arm, hand + new Vector3(0f, 0f, 0.2f), new Vector3(0.035f, 0.035f, 0.5f), Lit(Handle));
                    Part(_arm, hand + new Vector3(0f, -0.06f, 0.44f), new Vector3(0.12f, 0.12f, 0.03f), Lit(Iron));
                    break;
                case Tool.Hammer:
                    Part(_arm, hand + new Vector3(0f, 0f, 0.1f), new Vector3(0.035f, 0.035f, 0.26f), Lit(Handle));
                    Part(_arm, hand + new Vector3(0f, 0f, 0.23f), new Vector3(0.06f, 0.1f, 0.06f), Lit(Iron));
                    break;
                case Tool.Log:
                    Load(PrimitiveType.Cylinder, new Vector3(0f, 0.56f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.14f, 0.32f, 0.14f), Lit(Handle));
                    break;
                case Tool.Stone:
                    Load(PrimitiveType.Cube, new Vector3(0f, 0.62f, 0f), new Vector3(12f, 30f, 0f), new Vector3(0.22f, 0.18f, 0.2f), Lit(StoneColour));
                    break;
                case Tool.Plank:
                    Load(PrimitiveType.Cube, new Vector3(0.12f, 0.6f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0.06f, 0.05f, 0.8f), Lit(Handle));
                    break;
            }
        }

        private void Load(PrimitiveType type, Vector3 at, Vector3 euler, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(_body, false);
            go.transform.localPosition = at;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void Part(Transform parent, Vector3 at, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void Update()
        {
            var t = Time.time * _speed + _phase;

            if (_walks) Walk();

            switch (_motion)
            {
                case Motion.Chop:
                {
                    // raised behind the head, then brought down hard: a slow lift and a quick fall
                    var cycle = Mathf.Repeat(t / (Mathf.PI * 2f), 1f);
                    var swing = cycle < 0.7f ? Mathf.SmoothStep(0f, 1f, cycle / 0.7f) : 1f - Mathf.SmoothStep(0f, 1f, (cycle - 0.7f) / 0.3f);
                    _arm.localRotation = Quaternion.Euler(Mathf.Lerp(-35f, -170f, swing), 0f, 0f);
                    _body.localRotation = Quaternion.Euler(Mathf.Lerp(8f, -6f, swing), 0f, 0f);
                    break;
                }
                case Motion.Hammer:
                    _arm.localRotation = Quaternion.Euler(-60f - Mathf.Abs(Mathf.Sin(t)) * 70f, 0f, 0f);
                    break;
                case Motion.Hoe:
                    if (!_walks || _pausing) _arm.localRotation = Quaternion.Euler(Mathf.Lerp(40f, -50f, Mathf.Abs(Mathf.Sin(t))), 0f, 0f);
                    else _arm.localRotation = Quaternion.Euler(30f, 0f, 0f);
                    break;
                case Motion.Carry:
                    _arm.localRotation = Quaternion.Euler(-150f, 0f, 0f);
                    break;
            }
        }

        private float _along;
        private int _direction = 1;
        private bool _pausing;
        private float _pauseLeft;

        /// <summary>To and fro along the route; a ploughman stops every so often to work the soil.</summary>
        private void Walk()
        {
            var length = Vector3.Distance(_from, _to);
            if (length < 0.01f) return;

            if (_pausing)
            {
                _pauseLeft -= Time.deltaTime;
                if (_pauseLeft <= 0f) _pausing = false;
                return;
            }

            var pace = _motion == Motion.Carry ? 0.55f : 0.3f;
            _along += _direction * pace * Time.deltaTime / length;
            if (_along >= 1f || _along <= 0f)
            {
                _along = Mathf.Clamp01(_along);
                _direction = -_direction;
                if (_motion == Motion.Carry) { _pausing = true; _pauseLeft = 0.8f; }
            }
            else if (_motion == Motion.Hoe && Random.value < Time.deltaTime * 0.35f)
            {
                _pausing = true;
                _pauseLeft = 1.6f;
            }

            transform.position = Vector3.Lerp(_from, _to, _along) + Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.025f;
            var heading = (_direction > 0 ? _to - _from : _from - _to);
            heading.y = 0f;
            if (heading.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(heading);
        }
    }
}
