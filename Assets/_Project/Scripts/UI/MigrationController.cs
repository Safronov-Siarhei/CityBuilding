using CityBuilder.Citizens;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The one line that makes migration a mechanic instead of a mystery: whether people are
    /// coming or going, and how long until the next one.
    ///
    /// Without it the whole system is invisible. The player would watch the population drift and
    /// have no way to connect it to the contentment score sitting in the panel above -- and a
    /// hidden rule that moves the only resource you cannot mine is the worst kind of hidden rule.
    /// The countdown is the part that teaches it: watching "0:47" shrink and a settler appear at
    /// zero explains the mechanic in one cycle, without a word of tutorial.
    ///
    /// Hidden entirely before the Town Hall goes up, when there is nothing to migrate to.
    /// </summary>
    public class MigrationController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text label;

        private void Start()
        {
            if (MigrationManager.Instance != null) MigrationManager.Instance.OnMigrationChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (MigrationManager.Instance != null) MigrationManager.Instance.OnMigrationChanged -= Refresh;
        }

        private void Refresh()
        {
            var migration = MigrationManager.Instance;
            if (migration == null) return;

            var visible = migration.State != MigrationManager.MigrationState.Dormant;
            if (panel != null && panel.activeSelf != visible) panel.SetActive(visible);
            if (!visible || label == null) return;

            label.text = Describe(migration);
            label.color = ColorFor(migration.State);
        }

        private static string Describe(MigrationManager migration)
        {
            switch (migration.State)
            {
                case MigrationManager.MigrationState.SettlingIn:
                    return Localization.Format("#migration_settling", Clock(migration.SettlingInRemaining));
                case MigrationManager.MigrationState.Arriving:
                    return Localization.Format("#migration_arriving", Clock(migration.SecondsUntilNextMove));
                case MigrationManager.MigrationState.Leaving:
                    return Localization.Format("#migration_leaving", Clock(migration.SecondsUntilNextMove));
                case MigrationManager.MigrationState.NoRoom:
                    return Localization.Get("#migration_no_room");
                case MigrationManager.MigrationState.Deserted:
                    return Localization.Get("#migration_deserted");
                default:
                    return Localization.Get("#migration_balanced");
            }
        }

        /// <summary>m:ss, rounded up so the label reads 0:01 for the last whole second instead of sitting on 0:00 while nothing happens.</summary>
        private static string Clock(float seconds)
        {
            var total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{total / 60}:{total % 60:00}";
        }

        private static Color ColorFor(MigrationManager.MigrationState state)
        {
            switch (state)
            {
                case MigrationManager.MigrationState.Arriving:
                    return new Color(0.55f, 0.85f, 0.45f);
                case MigrationManager.MigrationState.Leaving:
                case MigrationManager.MigrationState.Deserted:
                    return new Color(0.9f, 0.35f, 0.3f);
                case MigrationManager.MigrationState.NoRoom:
                    return new Color(0.95f, 0.85f, 0.35f);
                default:
                    return new Color(1f, 1f, 1f, 0.8f);
            }
        }
    }
}
