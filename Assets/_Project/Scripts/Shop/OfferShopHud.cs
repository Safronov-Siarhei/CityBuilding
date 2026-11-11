using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Shop
{
    /// <summary>
    /// The shop's place on the HUD (mock-up Design/store/OfferShop_Hud.png): a stall icon in the left
    /// column under the sack, the next change's timer under it, a red dot while something can be
    /// taken, a warm glow while a free bundle waits; and the loud gold notice to its right for
    /// shop_notice_seconds when a free bundle comes up (only then -- ordinary changes are quiet). A tap
    /// on either opens the shop. Sits on an always-active object, so it can hide and show the
    /// button as the mode changes (a dungeon entered from the sandbox).
    /// </summary>
    public class OfferShopHud : MonoBehaviour
    {
        [SerializeField] private GameObject button;
        [SerializeField] private Button open;
        [SerializeField] private Text timer;
        [SerializeField] private GameObject dot;
        [SerializeField] private RectTransform glow;
        [SerializeField] private GameObject notice;
        [SerializeField] private Button noticeButton;
        [SerializeField] private RectTransform noticeBar;

        private OfferShop _shop;
        private int _shownSeconds = -1;
        private float _noticeLeft;
        private float _noticeTotal = 1f;
        private float _barWidth;

        private void Awake()
        {
            open.onClick.AddListener(OpenShop);
            noticeButton.onClick.AddListener(OpenShop);
            notice.SetActive(false);
            _barWidth = noticeBar.sizeDelta.x;
        }

        private void OpenShop()
        {
            _noticeLeft = 0f;
            notice.SetActive(false);
            OfferShopWindow.Show();
        }

        private void OnDestroy()
        {
            if (_shop != null) _shop.FreeArrived -= OnFree;
        }

        private void OnFree()
        {
            _noticeTotal = Mathf.Max(1f, Core.BalanceConfig.Sandbox.ShopNoticeSeconds);
            _noticeLeft = _noticeTotal;
        }

        private void Update()
        {
            if (_shop == null && OfferShop.Instance != null)
            {
                _shop = OfferShop.Instance;
                _shop.FreeArrived += OnFree;
            }
            var on = _shop != null && _shop.Rotation != null && OfferShop.Allowed;
            if (button.activeSelf != on) button.SetActive(on);
            if (!on)
            {
                if (notice.activeSelf) notice.SetActive(false);
                return;
            }
            var rotation = _shop.Rotation;

            var seconds = Mathf.Max(0, Mathf.CeilToInt((float)(rotation.NextChangeAt - _shop.Now)));
            if (seconds != _shownSeconds)
            {
                _shownSeconds = seconds;
                timer.text = OfferShopWindow.Clock(seconds);
            }
            var free = rotation.FreeWaiting;
            var any = free;
            if (!any)
                foreach (var slot in rotation.Slots)
                    if (slot.Bundle != null && !slot.Taken && !slot.IsPaid) { any = true; break; }
            if (dot.activeSelf != any) dot.SetActive(any);
            if (glow.gameObject.activeSelf != free) glow.gameObject.SetActive(free);
            if (free)
            {
                // a slow breath so the eye finds it
                var k = 0.92f + 0.12f * Mathf.Sin(Time.unscaledTime * 3.2f);
                glow.localScale = new Vector3(k, k, 1f);
            }

            var showNotice = _noticeLeft > 0f && !OfferShopWindow.IsOpen;
            if (notice.activeSelf != showNotice) notice.SetActive(showNotice);
            if (_noticeLeft > 0f)
            {
                _noticeLeft -= Time.unscaledDeltaTime;
                var size = noticeBar.sizeDelta;
                size.x = _barWidth * Mathf.Clamp01(_noticeLeft / _noticeTotal);
                noticeBar.sizeDelta = size;
            }
        }
    }
}
