using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Core
{
    public static class RoofMeshBuilder
    {
        /// <summary>
        /// Builds a simple gable (tent) roof: a ridge running along Z, sloping down to the X
        /// edges, with triangular end caps front and back. Centered on X/Z, base at y=0.
        /// </summary>
        public static Mesh BuildGableRoof(float width, float depth, float height)
        {
            var hw = width * 0.5f;
            var hd = depth * 0.5f;

            var a = new Vector3(-hw, 0f, -hd);
            var b = new Vector3(hw, 0f, -hd);
            var c = new Vector3(hw, 0f, hd);
            var d = new Vector3(-hw, 0f, hd);
            var r1 = new Vector3(0f, height, -hd);
            var r2 = new Vector3(0f, height, hd);

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            AddQuad(vertices, normals, triangles, a, d, r2, r1); // left slope
            AddQuad(vertices, normals, triangles, b, r1, r2, c); // right slope
            AddTriangle(vertices, normals, triangles, a, r1, b); // front gable end
            AddTriangle(vertices, normals, triangles, d, c, r2); // back gable end

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private static void AddQuad(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            AddTriangle(vertices, normals, triangles, p0, p1, p2);
            AddTriangle(vertices, normals, triangles, p0, p2, p3);
        }

        private static void AddTriangle(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            var normal = Vector3.Cross(p1 - p0, p2 - p0).normalized;
            var start = vertices.Count;
            vertices.Add(p0);
            vertices.Add(p1);
            vertices.Add(p2);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }
    }
}
