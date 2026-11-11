using System;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 1 «Бегство» (the user, 2026-11-11): the film ends and play begins on the spot. The
    /// camera eases from the film's last shot into the main game's view -- its pitch, its lens, no
    /// zoom, never turning -- and stays on Ардан, who is always in the middle and cannot be
    /// deselected. The floating stick runs him; the houses, props and trees stop him. The village
    /// goes on burning round him (the director keeps the world stepping).
    ///
    /// Stage 1: the handover and the controls in the film's village. The route beyond it (wheat,
    /// forest, bridge, the mage, the gates) waits for its models; see Design/mission1_map_v1.png.
    /// Every number is in Campaign/Mission1.json.
    /// </summary>
    public class MissionOne : MonoBehaviour
    {
        [Serializable] private class Config
        {
            public CameraConfig camera;
            public StickConfig joystick;
            public RunnerConfig runner;
            public string[] solidPieces;
            public string hintKey;
            public CalmConfig calm;
        }

        [Serializable] private class CameraConfig { public float pitch, yaw, distance, fov, aimHeight, blend; }
        [Serializable] private class StickConfig { public float radius, knob, deadzone, walkBelow, hintDelay, hintFadeAfter; }
        [Serializable] private class CalmConfig { public float from, to; }
        [Serializable] private class RunnerConfig { public float radius, height, turnRate, trunkRadius, fade; }

        [SerializeField] private OpeningDirector director;
        [SerializeField] private OpeningVillage village;
        [SerializeField] private Camera cam;
        [SerializeField] private TextAsset configFile;
        [SerializeField] private OpeningFx fx;

        private Config _c;
        private CineFigure _ardan;
        private CharacterController _body;
        private FloatingJoystick _stick;
        private Vector3 _fromPos;
        private Quaternion _fromRot;
        private float _fromFov, _blend;

        public bool Running { get; private set; }
        public Transform Ardan => _ardan != null ? _ardan.transform : null;

        private void Awake()
        {
            if (director != null) director.Finished += Begin;
        }

        private void OnDestroy()
        {
            if (director != null) director.Finished -= Begin;
        }

        /// <summary>Play begins: Ардан is the player's, the camera eases into the game's view.</summary>
        public void Begin()
        {
            if (Running) return;
            _c = JsonUtility.FromJson<Config>(configFile.text);
            director.KeepAlive = true;
            director.CameraReleased = true;
            _ardan = director.Release("ardan");
            if (_ardan == null) return;
            var r = _c.runner;
            village.MakeSolid(_c.solidPieces, r.trunkRadius, r.height * 3f);
            _body = _ardan.gameObject.AddComponent<CharacterController>();
            _body.radius = r.radius;
            _body.height = Mathf.Max(r.height, r.radius * 2f);
            _body.center = new Vector3(0f, _body.height * 0.5f, 0f);
            _body.skinWidth = r.radius * 0.1f;
            _body.minMoveDistance = 0f;
            _body.stepOffset = 0f;

            // from the film's physical lens to the game's plain field of view, without a jump
            _fromPos = cam.transform.position;
            _fromRot = cam.transform.rotation;
            _fromFov = cam.usePhysicalProperties ? Camera.FocalLengthToFieldOfView(cam.focalLength, cam.sensorSize.y) : cam.fieldOfView;
            cam.usePhysicalProperties = false;
            cam.fieldOfView = _fromFov;
            _blend = 0f;

            var j = _c.joystick;
            _stick = FloatingJoystick.Create(j.radius, j.knob, j.deadzone, j.hintDelay, j.hintFadeAfter, Localization.Get(_c.hintKey));
            _stick.transform.SetParent(transform, false);
            Running = true;
        }

        private void Update()
        {
            if (Running) Step(Time.deltaTime, null);
        }

        private void LateUpdate()
        {
            if (Running) StepCamera(Time.deltaTime);
        }

        /// <summary>One frame of play; `steer` stands in for the stick (the batchmode check).</summary>
        public void Step(float dt, Vector2? steer)
        {
            if (steer.HasValue) _stick.Feed(steer.Value, dt);
            var v = _stick.Value;
            var m = v.magnitude;
            var t = _ardan.transform;
            if (m > 0f)
            {
                // screen up is where the camera looks, flattened; it never turns, so neither does this
                var yaw = _c.camera.yaw * Mathf.Deg2Rad;
                var up = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
                var right = new Vector3(Mathf.Cos(yaw), 0f, -Mathf.Sin(yaw));
                var dir = (right * v.x + up * v.y).normalized;
                var walk = m < _c.joystick.walkBelow;
                var clips = _ardan.Clips;
                var speed = walk ? clips.walkSpeed : clips.runSpeed;
                _body.Move(dir * speed * dt);
                var p = t.position;
                p.y = village.HeightAt(new Vector2(p.x, p.z));    // flat ground, or the bridge's deck
                t.position = p;
                t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(dir), Mathf.Clamp01(_c.runner.turnRate * dt));
                _ardan.Play(walk ? "Walk" : "Run", _c.runner.fade);
            }
            else _ardan.Play("Idle", _c.runner.fade);
            _ardan.Tick(dt);
            // the storm lets go as he gets away from the portal (the square, the village's origin)
            if (fx != null && _c.calm != null)
            {
                var d = new Vector2(t.position.x, t.position.z).magnitude;
                fx.Calm = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_c.calm.from, _c.calm.to, d));
            }
        }

        /// <summary>The game's view, Ардан in the middle of it; eased into from the film's last shot.</summary>
        public void StepCamera(float dt)
        {
            var c = _c.camera;
            var aim = _ardan.transform.position + Vector3.up * c.aimHeight;
            var rot = Quaternion.Euler(c.pitch, c.yaw, 0f);
            var pos = aim - rot * Vector3.forward * c.distance;
            _blend = c.blend > 0f ? Mathf.Min(1f, _blend + dt / c.blend) : 1f;
            var w = Mathf.SmoothStep(0f, 1f, _blend);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(_fromPos, pos, w), Quaternion.Slerp(_fromRot, rot, w));
            cam.fieldOfView = Mathf.Lerp(_fromFov, c.fov, w);
            director.SetCameraAim(cam.transform.position, aim);
        }
    }
}
