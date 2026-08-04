using UnityEngine;

namespace CityBuilder.Buildings
{
    public class BuildingInstance : MonoBehaviour
    {
        public BuildingData Data { get; private set; }
        public Vector2Int OriginCell { get; private set; }

        public void Initialize(BuildingData data, Vector2Int originCell)
        {
            Data = data;
            OriginCell = originCell;
        }
    }
}
