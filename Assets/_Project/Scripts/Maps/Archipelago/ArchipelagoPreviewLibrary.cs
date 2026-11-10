using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Maps.Archipelago
{
    /// <summary>What the generator reads of a library island: its cells and spots, none of its meshes.</summary>
    public interface IIslandCells
    {
        string IslandName { get; }
        ArchipelagoGenerator.Kind Kind { get; }
        int Variant { get; }
        int Frame { get; }
        byte Cell(int i, int j);
        Vector2[] StartZone { get; }
        bool HasMainBase { get; }
        Vector2 MainBase { get; }
        bool HasBot { get; }
        Vector2 Bot { get; }
    }

    /// <summary>One island's cells, copied out of its IslandAsset (no meshes, no props).</summary>
    [Serializable]
    public sealed class IslandCells : IIslandCells
    {
        [SerializeField] private string islandName;
        [SerializeField] private ArchipelagoGenerator.Kind kind;
        [SerializeField] private int frame;
        [SerializeField] private byte[] cells = new byte[0];
        [SerializeField] private Vector2[] startZone = new Vector2[0];
        [SerializeField] private bool hasMainBase;
        [SerializeField] private Vector2 mainBase;
        [SerializeField] private bool hasBot;
        [SerializeField] private Vector2 bot;

        public string IslandName => islandName;
        public ArchipelagoGenerator.Kind Kind => kind;
        public int Variant => IslandAsset.VariantOf(islandName);
        public int Frame => frame;
        public byte Cell(int i, int j) => cells[j * frame + i];
        public Vector2[] StartZone => startZone;
        public bool HasMainBase => hasMainBase;
        public Vector2 MainBase => mainBase;
        public bool HasBot => hasBot;
        public Vector2 Bot => bot;

#if UNITY_EDITOR
        public static IslandCells CopyOf(IslandAsset asset) => new IslandCells
        {
            islandName = asset.IslandName,
            kind = asset.Kind,
            frame = asset.Frame,
            cells = asset.EditorCells,
            startZone = asset.StartZone,
            hasMainBase = asset.HasMainBase,
            mainBase = asset.MainBase,
            hasBot = asset.HasBot,
            bot = asset.Bot,
        };
#endif
    }

    /// <summary>
    /// The library's islands as cells only, in the library's order (Resources/ArchipelagoPreview):
    /// what the main menu draws a map code's chart from (the sandbox board's ink map, 2026-11-10)
    /// without loading every island's meshes, as ArchipelagoLibrary would. Same islands, same order,
    /// so a seed lays out the same map from either.
    /// </summary>
    public class ArchipelagoPreviewLibrary : ScriptableObject
    {
        public const string ResourcePath = "ArchipelagoPreview";

        [SerializeField] private IslandCells[] islands = new IslandCells[0];

        public IReadOnlyList<IIslandCells> Islands => islands;

        private static ArchipelagoPreviewLibrary _loaded;

        /// <summary>The cells, or null when no island has been made yet.</summary>
        public static ArchipelagoPreviewLibrary Load()
        {
            if (_loaded == null) _loaded = UnityEngine.Resources.Load<ArchipelagoPreviewLibrary>(ResourcePath);
            return _loaded != null && _loaded.islands.Length > 0 ? _loaded : null;
        }

#if UNITY_EDITOR
        public void EditorInitialize(IslandAsset[] all)
        {
            var list = new List<IslandCells>();
            foreach (var asset in all)
            {
                if (asset != null) list.Add(IslandCells.CopyOf(asset));
            }
            islands = list.ToArray();
        }
#endif
    }
}
