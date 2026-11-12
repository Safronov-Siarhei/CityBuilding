using CityBuilder.Combat;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 2's hero LOOKS like Ардан with his sword (the user's second playtest: «почему мы играем за
    /// Латника?»; the model and its poses approved 2026-11-12). He is still the Латник's SoldierUnit
    /// underneath -- its fighting, steering and health stay -- only its own figure is hidden and the
    /// cinematic figure (Tools/blender/cinematic/export_ardan_sword.py) rides it: Idle or Run by how fast
    /// he moves, Attack on every blow, and a clip for each of the four skills.
    /// </summary>
    public partial class CityDefenceMode
    {
        [SerializeField] private GameObject ardenModel;    // CineArdanSword.fbx
        [SerializeField] private CineClips ardenClips;     // its baked clips

        private static readonly string[] SkillClip = { "Cry", "Whirl", "Dash", "Breath" };   // by skill index
        // three strikes in turn (approved 2026-11-12): from the right, the backhand, the overhead -- BlowFx tilts its crescent to match
        private static readonly string[] StrikeClips = { "Attack", "Attack2", "Attack3" };
        private const float RunFrom = 0.15f;               // m/s: slower than this he stands
        private const float LookFade = 0.12f;

        private CineFigure _ardenFig;
        private Vector3 _ardenWas;
        private float _ardenSpeed;
        private bool _ardenActs;

        private void DressArden()
        {
            if (Arden == null || ardenModel == null || ardenClips == null) return;
            // as tall as the soldier he stands in for (the user's third playtest: at the village's 1/1.6 he looked a child)
            var feet = Arden.transform.position.y;
            var soldierTop = feet;
            foreach (var r in Arden.GetComponentsInChildren<Renderer>(true))
            {
                if (r.enabled && r is MeshRenderer or SkinnedMeshRenderer) soldierTop = Mathf.Max(soldierTop, r.bounds.max.y);
                r.enabled = false;
            }
            var go = Instantiate(ardenModel, Arden.transform, false);
            go.name = "ArdenSword";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = ardenModel.transform.localScale * CineScale / Mathf.Max(0.01f, Arden.transform.lossyScale.x);
            _ardenFig = go.AddComponent<CineFigure>();
            _ardenFig.Init(ardenClips);
            var figTop = feet;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) figTop = Mathf.Max(figTop, r.bounds.max.y);
            if (soldierTop - feet > 0.1f && figTop - feet > 0.05f)
                go.transform.localScale *= (soldierTop - feet) / (figTop - feet);
            _ardenFig.Play("Idle", 0f);
            _ardenWas = Arden.transform.position;
            Arden.Struck += () => { Act(StrikeClips[_blow % StrikeClips.Length]); BlowFx(); };
            SkillUsed -= OnArdenSkill;
            SkillUsed += OnArdenSkill;
        }

        private void OnArdenSkill(int skill)
        {
            if (skill >= 0 && skill < SkillClip.Length) Act(SkillClip[skill]);
            SkillFx(skill);
        }

        private void Act(string clip)
        {
            if (_ardenFig == null) return;
            _ardenFig.Play(clip, LookFade * 0.5f);
            _ardenActs = true;
        }

        private void TickArdenFigure(float dt)
        {
            if (_ardenFig == null || Arden == null || dt <= 0f) return;
            var at = Arden.transform.position;
            var d = at - _ardenWas;
            d.y = 0f;
            _ardenWas = at;
            _ardenSpeed = Mathf.Lerp(_ardenSpeed, d.magnitude / dt, 1f - Mathf.Exp(-dt * 10f));
            if (_ardenActs && _ardenFig.Finished) _ardenActs = false;
            if (!_ardenActs)
            {
                var run = _ardenSpeed > RunFrom;
                var want = run ? "Run" : "Idle";
                var full = Mathf.Max(0.05f, ardenClips.runSpeed * CineScale);
                var rate = run ? Mathf.Clamp(_ardenSpeed / full, 0.5f, 1.8f) : 1f;
                _ardenFig.Play(want, LookFade, rate);   // the same clip again only takes the new rate
            }
            _ardenFig.Tick(dt);
        }
    }
}
