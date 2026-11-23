using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 3b's hero LOOKS like the King on his horse from the opening scene (the user's playtest 2026-11-23: «мы
    /// играем за рыцаря на коне, а должны за короля»). Underneath he is still the kingdom's mounted knight (its fighting,
    /// steering, health); its own figure is hidden and the scene's two figures ride it, playing pieces of the APPROVED
    /// takes (Mission3Escort.json `king`, Tools/campaign/mission3b_town.py): the column's walk, the column halted, his
    /// charge back at a gallop, and his rear with the sword up for «За мной!». The rider sits where the take seats him
    /// on the horse, frame by frame. Sized like the cart (FitCart: the horse as long as a rider's horse).
    /// </summary>
    public partial class CityDefenceMode
    {
        [System.Serializable] private class KingRig
        {
            public string horse, rider;
            public float fps = 30f;
            public KingPiece walk, idle, gallop, cry;
            public float[] seatColumn, seatRally;
        }

        [System.Serializable] private class KingPiece { public string take; public float from, to, speed; }

        private Transform _kingFigure;
        private CineFigure _kingHorse, _kingRider;
        private KingPiece _kingNow;
        private float _kingT, _kingActLeft, _kingSpeed;
        private Vector3 _kingWas;

        private void DressKing()
        {
            var rig = _escort != null ? _escort.king : null;
            if (Arden == null || rig == null) return;
            var hi = System.Array.FindIndex(capitalFigures, m => m != null && m.name == rig.horse);
            var ri = System.Array.FindIndex(capitalFigures, m => m != null && m.name == rig.rider);
            if (hi < 0 || ri < 0 || hi >= capitalClips.Length || ri >= capitalClips.Length || capitalClips[hi] == null || capitalClips[ri] == null) return;
            foreach (var r in Arden.GetComponentsInChildren<Renderer>(true))
                if (r is MeshRenderer or SkinnedMeshRenderer) r.enabled = false;
            _kingFigure = new GameObject("KingFigure").transform;
            _kingFigure.SetParent(Arden.transform, false);
            _kingFigure.localScale = Vector3.one * _rigScale / Mathf.Max(0.01f, Arden.transform.lossyScale.x);
            _kingHorse = Figure(hi, "KingHorse");
            _kingRider = Figure(ri, "KingRider");
            _kingNow = null;
            _kingActLeft = 0f;
            _kingWas = Arden.transform.position;
            _kingSpeed = 0f;
            SkillUsed -= OnKingSkill;
            SkillUsed += OnKingSkill;

            CineFigure Figure(int i, string name)
            {
                var go = Instantiate(capitalFigures[i], _kingFigure, false);
                go.name = name;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                var fig = go.AddComponent<CineFigure>();
                fig.Init(capitalClips[i]);
                return fig;
            }
        }

        private void OnKingSkill(int skill)
        {
            var rig = _escort != null ? _escort.king : null;
            if (rig == null || rig.cry == null) return;
            // «За мной!»: the rear with the sword up; the charge goes at a gallop by itself
            if (skill != Cry) return;
            KingPlay(rig.cry);
            _kingActLeft = rig.cry.to - rig.cry.from;
        }

        private void KingPlay(KingPiece piece)
        {
            if (piece == null || piece == _kingNow) return;
            var fade = _kingNow != null && _kingNow.take != piece.take ? 0.2f : 0f;
            _kingNow = piece;
            _kingT = piece.from;
            _kingHorse.Play(piece.take, fade, 0f, piece.from);
            _kingRider.Play(piece.take, fade, 0f, piece.from);
            _kingHorse.SetTime(piece.from);
            _kingRider.SetTime(piece.from);
        }

        private void TickKingFigure(float dt)
        {
            if (_kingFigure == null || Arden == null || dt <= 0f) return;
            var rig = _escort.king;
            var at = Arden.transform.position;
            var d = at - _kingWas;
            d.y = 0f;
            _kingWas = at;
            _kingSpeed = Mathf.Lerp(_kingSpeed, d.magnitude / dt, 1f - Mathf.Exp(-dt * 8f));
            var scale = Mathf.Max(0.05f, _rigScale);
            if (_kingActLeft > 0f) _kingActLeft -= dt;
            else if (_kingSpeed < 0.12f * scale) KingPlay(rig.idle);
            else if (_kingSpeed < 1.4f * rig.walk.speed * scale) KingPlay(rig.walk);
            else KingPlay(rig.gallop);
            var piece = _kingNow;
            if (piece == null) return;
            var rate = piece.speed > 0f ? Mathf.Clamp(_kingSpeed / (piece.speed * scale), 0.4f, 2f) : 1f;
            _kingT += dt * rate;
            if (_kingT >= piece.to)
            {
                if (piece == rig.cry) { _kingActLeft = 0f; _kingT = piece.to; }
                else _kingT = piece.from + (_kingT - piece.to) % Mathf.Max(0.05f, piece.to - piece.from);
            }
            _kingHorse.SetTime(_kingT);
            _kingRider.SetTime(_kingT);
            _kingHorse.Tick(dt);
            _kingRider.Tick(dt);
            // the rider where the take seats him on the horse at this moment
            var seat = piece.take == "Rally" ? rig.seatRally : rig.seatColumn;
            if (seat != null && seat.Length >= 7)
            {
                var f = Mathf.Clamp(Mathf.FloorToInt(_kingT * rig.fps), 0, seat.Length / 7 - 1) * 7;
                _kingRider.transform.localPosition = new Vector3(seat[f], seat[f + 1], seat[f + 2]);
                _kingRider.transform.localRotation = new Quaternion(seat[f + 3], seat[f + 4], seat[f + 5], seat[f + 6]);
            }
        }
    }
}
