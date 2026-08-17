using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// Where the balance spreadsheet lives. Editor-only by design: the game never reads a URL,
    /// only the committed CSVs and the asset built from them (see BalanceImporter).
    ///
    /// Fill these from Google Sheets: Файл → Поделиться → Опубликовать в интернете → выбрать
    /// вкладку → формат CSV → скопировать ссылку. One link per tab; the link ends in
    /// output=csv and works without signing in, which is exactly why nothing here needs
    /// credentials or a Google Cloud project.
    /// </summary>
    public class BalanceSheetSettings : ScriptableObject
    {
        [Tooltip("Опубликованная как CSV вкладка units")]
        public string unitsCsvUrl = string.Empty;

        [Tooltip("Опубликованная как CSV вкладка economy")]
        public string economyCsvUrl = string.Empty;

        [Tooltip("Опубликованная как CSV вкладка buildings")]
        public string buildingsCsvUrl = string.Empty;

        [Tooltip("Опубликованная как CSV вкладка recipes")]
        public string recipesCsvUrl = string.Empty;
    }
}
