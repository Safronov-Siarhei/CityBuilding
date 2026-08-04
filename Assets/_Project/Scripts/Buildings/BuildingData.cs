using System.Collections.Generic;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Buildings
{
    [CreateAssetMenu(fileName = "NewBuilding", menuName = "CityBuilder/Building Data")]
    public class BuildingData : ScriptableObject
    {
        public string buildingName = "Building";
        public GameObject prefab;
        public Vector2Int footprintSize = Vector2Int.one;
        public List<ResourceAmount> cost = new List<ResourceAmount>();
    }
}
