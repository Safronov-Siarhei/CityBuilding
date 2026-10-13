using CityBuilder.Buildings;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// How loud an event is on screen -- the colour of its icon's rim and badge, and how long the
    /// icon stays. <see cref="LogOnly"/> is written to the text history and nowhere else: the
    /// player's own actions (built, demolished, research started) are things they just watched
    /// themselves do, and an icon for each was exactly the noise the old log panel was made of.
    /// </summary>
    public enum EventSeverity { LogOnly, Good, Warning, Danger }

    /// <summary>
    /// The small picture in an event icon's corner badge, which says WHAT happened -- the big
    /// picture in the middle says to what (a building's own icon, a resource). An event with no
    /// big picture shows this one large instead. Each value is a file in Assets/_Project/UI/Glyphs
    /// of the same name, drawn by Tools/ui/make_event_glyphs.py; the order is the order of
    /// EventIconLibrary's glyph array, so a new value goes at the end.
    /// </summary>
    public enum EventGlyph { None, Fire, Skull, Sick, Heal, Water, Upgrade, Ruin, Swords, Portal, Star, Alert, Sad, Happy, Check }

    /// <summary>
    /// One thing that happened, as the event feed shows it: what, how loud, and where. The text is
    /// what the history keeps and what the icon shows when held; everything else is for the icon.
    ///
    /// Where is two things: a position taken when the event happened, and optionally a transform to
    /// follow -- a fire is reported once but the camera should go to the building, and an orc raid
    /// should send the camera to where the orcs are NOW, not to the portal they left. A destroyed
    /// follow target falls back to the position.
    /// </summary>
    public class GameEvent
    {
        public string Text;
        public EventSeverity Severity;
        public EventGlyph Glyph;

        /// <summary>The icon name for the middle of the circle (a file in Assets/_Project/UI/Icons, e.g. "Baker" or "Res_Food"), or null to show the glyph large.</summary>
        public string Picture;

        public bool HasPosition;
        public Vector3 Position;
        public Transform Follow;

        /// <summary>Where the camera should go for this event right now, if anywhere.</summary>
        public bool TryGetTarget(out Vector3 target)
        {
            // Unity's null: a destroyed transform compares equal to null here.
            if (Follow != null)
            {
                target = Follow.position;
                return true;
            }

            target = Position;
            return HasPosition;
        }

        public static GameEvent At(string text, EventSeverity severity, EventGlyph glyph, string picture, Vector3 position, Transform follow = null)
        {
            return new GameEvent
            {
                Text = text, Severity = severity, Glyph = glyph, Picture = picture,
                HasPosition = true, Position = position, Follow = follow,
            };
        }

        public static GameEvent Nowhere(string text, EventSeverity severity, EventGlyph glyph, string picture)
        {
            return new GameEvent { Text = text, Severity = severity, Glyph = glyph, Picture = picture };
        }

        /// <summary>Something that happened to this building: its own icon in the middle, and the camera goes to it.</summary>
        public static GameEvent About(BuildingInstance building, string text, EventSeverity severity, EventGlyph glyph)
        {
            if (building == null) return Nowhere(text, severity, glyph, null);

            var picture = building.Data != null ? building.Data.buildingName : null;
            return At(text, severity, glyph, picture, building.transform.position, building.transform);
        }

        /// <summary>
        /// Something that happened to the whole settlement -- an epidemic, hunger, a mood -- which
        /// has no single place of its own. The camera goes to the building that answers it (the
        /// healer's house for illness), else to the Town Hall, else nowhere.
        ///
        /// A scan over the buildings, which is fine for what calls it: these are once-a-day
        /// events at most.
        /// </summary>
        public static GameEvent ForSettlement(string text, EventSeverity severity, EventGlyph glyph, string picture, string placeBuildingName = null)
        {
            var place = FindBuilding(placeBuildingName) ?? FindBuilding(BuildingIds.TownHall);
            return place != null
                ? At(text, severity, glyph, picture, place.transform.position, place.transform)
                : Nowhere(text, severity, glyph, picture);
        }

        private static BuildingInstance FindBuilding(string buildingName)
        {
            if (string.IsNullOrEmpty(buildingName) || !BuildingInstance.HasAny(buildingName)) return null;

            foreach (var instance in Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (instance.Data != null && instance.Data.buildingName == buildingName) return instance;
            }
            return null;
        }

        /// <summary>
        /// Two events are the same icon when they would draw the same picture in the same colours:
        /// three fences decaying is one icon counting to three, a bakery and a mill catching fire
        /// are two.
        /// </summary>
        public bool SameIconAs(GameEvent other)
        {
            return other != null && other.Severity == Severity && other.Glyph == Glyph && other.Picture == Picture;
        }
    }
}
