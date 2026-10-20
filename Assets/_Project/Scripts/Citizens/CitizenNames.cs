using CityBuilder.Core;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// A citizen's own name (the user, 2026-10-20: "хочу чтобы они стали более одушевлёнными").
    ///
    /// Names are handed out in spawn order, so a town reloaded from a save -- which respawns its
    /// citizens in the same order -- calls them the same again without saving a word. The order
    /// is stepped through the list by a stride coprime with its length, so neighbours in the
    /// queue do not read as an alphabet; past the end the same names come back with a numeral.
    /// </summary>
    public static class CitizenNames
    {
        // { Russian, English } -- a name is a name, the English side only transliterates it.
        private static readonly string[,] Names =
        {
            { "Ларон", "Laron" }, { "Мирна", "Mirna" }, { "Борислав", "Borislav" }, { "Эльда", "Elda" },
            { "Тобин", "Tobin" }, { "Велена", "Velena" }, { "Гарет", "Garet" }, { "Ируна", "Iruna" },
            { "Олдрик", "Oldric" }, { "Сайра", "Saira" }, { "Брам", "Bram" }, { "Лиона", "Liona" },
            { "Кедрик", "Kedric" }, { "Ниса", "Nisa" }, { "Ульф", "Ulf" }, { "Тэйра", "Teira" },
            { "Данеш", "Danesh" }, { "Мэйв", "Maeve" }, { "Хальдор", "Haldor" }, { "Рина", "Rina" },
            { "Фенрик", "Fenric" }, { "Аделла", "Adella" }, { "Ивор", "Ivor" }, { "Зарина", "Zarina" },
            { "Освин", "Oswin" }, { "Лейса", "Leisa" }, { "Грим", "Grim" }, { "Ветта", "Vetta" },
            { "Эрвин", "Erwin" }, { "Майра", "Myra" }, { "Торвальд", "Torvald" }, { "Ильса", "Ilsa" },
            { "Родрик", "Rodric" }, { "Нэлла", "Nella" }, { "Бьорн", "Bjorn" }, { "Сельма", "Selma" },
            { "Кай", "Kai" }, { "Арвена", "Arvena" }, { "Мартен", "Marten" }, { "Ода", "Oda" },
            { "Виллем", "Willem" }, { "Тильда", "Tilda" }, { "Эйнар", "Einar" }, { "Грета", "Greta" },
            { "Леннарт", "Lennart" }, { "Фрея", "Freya" }, { "Остап", "Ostap" }, { "Злата", "Zlata" },
            { "Ратибор", "Ratibor" }, { "Ясна", "Yasna" }, { "Годрик", "Godric" }, { "Ровена", "Rowena" },
            { "Сверр", "Sverr" }, { "Агата", "Agata" }, { "Лукан", "Lukan" }, { "Белла", "Bella" },
            { "Микал", "Mikal" }, { "Дарина", "Darina" }, { "Ханс", "Hans" }, { "Эвелин", "Evelyn" },
            { "Яромир", "Yaromir" }, { "Ольва", "Olva" }, { "Корвин", "Corwin" }, { "Линнея", "Linnea" },
            { "Вейланд", "Wayland" }, { "Марта", "Marta" }, { "Стефан", "Stefan" }, { "Ингрид", "Ingrid" },
            { "Альрик", "Alric" }, { "Лада", "Lada" }, { "Тимон", "Timon" }, { "Кирра", "Kirra" },
        };

        private const int Stride = 29; // coprime with the list length (72)

        public static int Count => Names.GetLength(0);

        /// <summary>The name of the n-th citizen a town has had (0-based), in the current language.</summary>
        public static string Of(int index)
        {
            if (index < 0) index = 0;
            var count = Count;
            var slot = (int)((long)index * Stride % count);
            var name = Names[slot, Localization.Language == "ru" ? 0 : 1];
            var round = index / count;
            return round == 0 ? name : name + " " + Roman(round + 1);
        }

        private static string Roman(int n)
        {
            string[] tens = { "", "X", "XX", "XXX", "XL", "L", "LX", "LXX", "LXXX", "XC" };
            string[] ones = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };
            return n >= 100 ? n.ToString() : tens[n / 10] + ones[n % 10];
        }
    }
}
