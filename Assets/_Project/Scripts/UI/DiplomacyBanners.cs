using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Diplomacy;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// The neighbour's banners (the user, 2026-11-17: «как баннеры чумы и пожаров»; art UI/Diplomacy/Banner*
    /// and Razed): «просит помощи», «подарок от соседа», «отношения на грани», «сосед собирает войско» and
    /// «ратуша соседа пала». Each pauses the game like the disaster banners; one at a time, the rest wait in
    /// a queue. Every one is logged in the notice panel too.
    /// </summary>
    public class DiplomacyBanners : MonoBehaviour
    {
        private const float RefreshSeconds = 0.25f;

        [SerializeField] private BakedUi help;
        [SerializeField] private BakedUi gift;
        [SerializeField] private BakedUi edge;
        [SerializeField] private BakedUi war;
        [SerializeField] private BakedUi razed;

        private readonly Queue<DiplomacyNotice> _waiting = new Queue<DiplomacyNotice>();
        private DiplomacyNotice _current;
        private BakedUi _shown;
        private float _timeScale = 1f;
        private float _refreshIn;

        public bool IsOpen => _shown != null;

        private void Awake()
        {
            if (help != null)
            {
                help.OnTap("Send", () =>
                {
                    if (DiplomacyRules.SendHelp(_current)) Done();
                });
                help.OnTap("Refuse", () =>
                {
                    DiplomacyRules.RefuseHelp();
                    Done();
                });
            }
            if (gift != null)
                gift.OnTap("Take", () =>
                {
                    DiplomacyRules.TakeGift(_current);
                    Done();
                });
            if (edge != null)
            {
                edge.OnTap("Gift", () =>
                {
                    Done();
                    if (DiplomacyGiftWindow.Instance != null) DiplomacyGiftWindow.Instance.Show();
                });
                edge.OnTap("Later", Done);
            }
            if (war != null)
            {
                war.OnTap("ToHall", () =>
                {
                    Done();
                    if (DiplomacyWindow.Instance != null) DiplomacyWindow.Instance.Show(null);
                });
                war.OnTap("Ok", Done);
            }
            if (razed != null)
                razed.OnTap("Take", () =>
                {
                    if (_current != null && _current.Items.Count > 0) Wallet.UpgradeCrystals += _current.Items[0].amount;
                    Done();
                });
        }

        private void OnEnable() => DiplomacyRules.Raised += Handle;

        private void OnDisable() => DiplomacyRules.Raised -= Handle;

        private void Handle(DiplomacyNotice notice)
        {
            Log(notice);
            _waiting.Enqueue(notice);
            if (_shown == null) Next();
        }

        private static void Log(DiplomacyNotice notice)
        {
            var log = EventLogManager.Instance;
            if (log == null) return;
            var (key, severity, glyph) = notice.Kind switch
            {
                DiplomacyNoticeKind.Help => ("#log_diplomacy_help", EventSeverity.Warning, EventGlyph.Alert),
                DiplomacyNoticeKind.BotGift => ("#log_diplomacy_gift", EventSeverity.Good, EventGlyph.Star),
                DiplomacyNoticeKind.Edge => ("#log_diplomacy_edge", EventSeverity.Danger, EventGlyph.Sad),
                DiplomacyNoticeKind.War => ("#log_diplomacy_war", EventSeverity.Danger, EventGlyph.Swords),
                _ => ("#log_diplomacy_razed", EventSeverity.Good, EventGlyph.Check),
            };
            log.Post(GameEvent.Nowhere(Localization.Get(key), severity, glyph, BuildingIds.DiplomacyHall));
        }

        private BakedUi For(DiplomacyNoticeKind kind) => kind switch
        {
            DiplomacyNoticeKind.Help => help,
            DiplomacyNoticeKind.BotGift => gift,
            DiplomacyNoticeKind.Edge => edge,
            DiplomacyNoticeKind.War => war,
            _ => razed,
        };

        private void Next()
        {
            while (_waiting.Count > 0)
            {
                var notice = _waiting.Dequeue();
                var banner = For(notice.Kind);
                if (banner == null) continue;
                _current = notice;
                _shown = banner;
                _timeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                transform.SetAsLastSibling();
                banner.Open();
                Fill();
                return;
            }
        }

        private void Done()
        {
            if (_shown == null) return;
            _shown.Hide();
            _shown = null;
            _current = null;
            Time.timeScale = _timeScale;
            Next();
        }

        private void Update()
        {
            if (_shown == null) return;
            _refreshIn -= Time.unscaledDeltaTime;
            if (_refreshIn > 0f) return;
            _refreshIn = RefreshSeconds;
            Fill();
        }

        private void Fill()
        {
            var config = BalanceConfig.Instance;
            if (_current == null || config == null) return;
            switch (_current.Kind)
            {
                case DiplomacyNoticeKind.Help:
                {
                    var item = _current.Items.Count > 0 ? _current.Items[0] : default;
                    help.SetText("Body", _current.Row != null ? Localization.Get("#diplomacy_ask_" + _current.Row.id) : string.Empty);
                    help.SetIcon("AskIcon", item.resource);
                    help.SetText("AskN", item.amount.ToString());
                    help.SetText("SendTail", "+" + config.DiplomacyHelpSendRelations);
                    help.SetText("RefuseTail", "-" + config.DiplomacyHelpRefuseRelations);
                    help.Interactable("Send", DiplomacyRules.Have(item.resource) >= item.amount);
                    break;
                }
                case DiplomacyNoticeKind.BotGift:
                    for (var i = 0; i < 3; i++)
                    {
                        var on = i < _current.Items.Count;
                        gift.Show("Item" + i, on);
                        if (!on) continue;
                        gift.SetIcon("Item" + i + "Icon", _current.Items[i].resource);
                        gift.SetText("Item" + i + "N", "+" + _current.Items[i].amount);
                    }
                    break;
                case DiplomacyNoticeKind.Edge:
                    edge.SetText("Value", Mathf.RoundToInt(BotRelations.Score).ToString("+0;-0;0"));
                    DiplomacyWindow.PlaceNeedle(edge, BotRelations.Score);
                    break;
                case DiplomacyNoticeKind.War:
                {
                    var left = Mathf.Max(0, Mathf.CeilToInt(config.BotWarRaidFirstSeconds - BotRelations.WarSeconds));
                    war.SetText("Timer", Shop.OfferShopWindow.Clock(left));
                    break;
                }
                default:
                    razed.SetText("Amount", "+" + (_current.Items.Count > 0 ? _current.Items[0].amount : 0));
                    break;
            }
        }
    }
}
