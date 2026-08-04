using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Core
{
    public static class CubeMeshBuilder
    {
        private static readonly Vector3[] UnitCubeVertices =
        {
            // Bottom
            new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1),
            // Left
            new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0),
            // Front
            new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 0, 0),
            // Back
            new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1), new Vector3(0, 0, 1),
            // Right
            new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1),
            // Top
            new Vector3(0, 1, 0), new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0),
        };

        private static readonly Vector3[] UnitCubeNormals =
        {
            Vector3.down, Vector3.down, Vector3.down, Vector3.down,
            Vector3.left, Vector3.left, Vector3.left, Vector3.left,
            Vector3.back, Vector3.back, Vector3.back, Vector3.back,
            Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
            Vector3.right, Vector3.right, Vector3.right, Vector3.right,
            Vector3.up, Vector3.up, Vector3.up, Vector3.up,
        };

        private static readonly int[] UnitCubeTriangles =
        {
            3, 1, 0, 3, 2, 1,
            7, 5, 4, 7, 6, 5,
            11, 9, 8, 11, 10, 9,
            15, 13, 12, 15, 14, 13,
            19, 17, 16, 19, 18, 17,
            23, 21, 20, 23, 22, 21,
        };

        /// <summary>
        /// Builds a single combined mesh made of one cube per grid cell, giving the terrain
        /// a blocky/voxel look while remaining a single draw call.
        /// </summary>
        public static Mesh BuildGrid(int cellsX, int cellsZ, float cellSize, float gap, float cubeHeight, Vector3 origin)
        {
            var cubeFootprint = cellSize - gap;
            var vertices = new List<Vector3>(cellsX * cellsZ * 24);
            var normals = new List<Vector3>(cellsX * cellsZ * 24);
            var triangles = new List<int>(cellsX * cellsZ * 36);

            for (var x = 0; x < cellsX; x++)
            {
                for (var z = 0; z < cellsZ; z++)
                {
                    var cellOrigin = origin + new Vector3(x * cellSize + gap * 0.5f, 0f, z * cellSize + gap * 0.5f);
                    AppendCube(vertices, normals, triangles, cellOrigin, new Vector3(cubeFootprint, cubeHeight, cubeFootprint));
                }
            }

            var mesh = new Mesh
            {
                indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private static void AppendCube(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, Vector3 originCorner, Vector3 size)
        {
            var indexOffset = vertices.Count;

            for (var i = 0; i < UnitCubeVertices.Length; i++)
            {
                vertices.Add(originCorner + Vector3.Scale(UnitCubeVertices[i], size));
                normals.Add(UnitCubeNormals[i]);
            }

            for (var i = 0; i < UnitCubeTriangles.Length; i++)
            {
                triangles.Add(indexOffset + UnitCubeTriangles[i]);
            }
        }
    }
}
