namespace CityBuilder.Resources
{
    /// <summary>
    /// What a resource is called in the UI. The HUD says everything with icons, so this did not
    /// exist until a building had to explain a conversion ("Мука из зерна") -- an icon pair alone
    /// cannot say which of the two is the input.
    /// </summary>
    public static class ResourceNames
    {
        public static string Of(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Wood: return "дерево";
                case ResourceType.Stone: return "камень";
                case ResourceType.Food: return "еда";
                case ResourceType.Gold: return "золото";
                case ResourceType.Population: return "жители";
                case ResourceType.Iron: return "железо";
                case ResourceType.Coal: return "уголь";
                case ResourceType.Coins: return "монеты";
                case ResourceType.Grain: return "пшеница";
                case ResourceType.Flour: return "мука";
                case ResourceType.Bread: return "хлеб";
                default: return type.ToString();
            }
        }
    }
}
