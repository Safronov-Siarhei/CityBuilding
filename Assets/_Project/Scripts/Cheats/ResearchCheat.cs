using CityBuilder.Core;
using CityBuilder.Research;
using UnityEngine;

namespace CityBuilder.Cheats
{
    /// <summary>
    /// Testing tool: grants research without paying for it or waiting. Configure it in the Inspector
    /// on GameCheats/Research and press a button (added by ResearchCheatEditor) during Play.
    ///
    /// This exists because the Laboratory gates almost everything the game has: thirty-one of the
    /// forty-nine buildings and every upgrade level. Looking at a late settlement -- or testing the
    /// bakery, the furnace, the fortified towers -- would otherwise mean playing through the whole
    /// tech list first.
    ///
    /// Event-driven, with no Update: idle cost is zero, which matters because this ships in the
    /// generated scene like everything else.
    /// </summary>
    public class ResearchCheat : MonoBehaviour
    {
        [Tooltip("Id темы изучения, например level_building:Warehouse:2 или unlock_building:Smelter.")]
        [SerializeField] private string topicId = string.Empty;

        /// <summary>Grants the one topic named above, with its prerequisites. Bound to the Inspector's button.</summary>
        public void GrantOne()
        {
            var research = ResearchManager.Instance;
            if (research == null)
            {
                Debug.LogWarning("ResearchCheat: нет ResearchManager в сцене -- изучение выдать невозможно.");
                return;
            }

            if (!research.CompleteInstantly(topicId))
            {
                Debug.LogWarning($"ResearchCheat: тема '{topicId}' не найдена в каталоге или уже изучена.");
                return;
            }

            EventLogManager.Instance?.Log($"[Чит] изучено: {topicId}");
        }

        /// <summary>Grants every topic in the catalogue. Bound to the Inspector's button.</summary>
        public void GrantEverything()
        {
            var research = ResearchManager.Instance;
            if (research == null)
            {
                Debug.LogWarning("ResearchCheat: нет ResearchManager в сцене -- изучение выдать невозможно.");
                return;
            }

            var granted = research.CompleteEverything();
            EventLogManager.Instance?.Log($"[Чит] изучено тем: {granted}");
        }
    }
}
