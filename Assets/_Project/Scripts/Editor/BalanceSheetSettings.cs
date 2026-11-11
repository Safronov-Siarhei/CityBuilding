using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// Where the balance spreadsheet lives. Editor-only by design: the game never reads a URL,
    /// only the committed CSVs and the asset built from them (see BalanceImporter).
    ///
    /// One link for the whole document (Sheets: Файл → Поделиться → Опубликовать в интернете →
    /// весь документ → CSV), cut after /pub; which tabs are read is Balance/sheet_tabs.csv. It works
    /// without signing in, which is why nothing here needs credentials or a Google Cloud project.
    /// </summary>
    public class BalanceSheetSettings : ScriptableObject
    {
        [Tooltip("Ссылка на опубликованную таблицу, до /pub включительно")]
        public string publishedUrl = string.Empty;
    }
}
