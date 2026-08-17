using CityBuilder.Core;

namespace CityBuilder.Resources
{
    /// <summary>
    /// What a resource is called in the UI.
    ///
    /// The name itself lives in the localization sheet under `resource.<Type>` -- the key is
    /// derived from the enum rather than stored anywhere, so a new resource needs one row in the
    /// sheet and no code at all. An untranslated one shows its key, which is the point: a blank
    /// label would be a bug nobody notices.
    /// </summary>
    public static class ResourceNames
    {
        public static string Of(ResourceType type) => Localization.Get(KeyFor(type));

        public static string KeyFor(ResourceType type) => "resource." + type;
    }
}
