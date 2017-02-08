using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class Triangulator
{
    private List<Vector2> m_points = new List<Vector2>();

    public Triangulator(Vector2[] points, bool duplicatedClosePoint = true)
    {
        m_points = new List<Vector2>(points);

        // Remove the duplicated point which is present to close the
        // polygon in the geoJSON
        if (duplicatedClosePoint)
            m_points.RemoveAt(m_points.Count - 1);
    }

    public int[] Triangulate()
    {
        List<int> indices = new List<int>();

        int n = m_points.Count;
        if (n < 3)
            return indices.ToArray();

        int[] V = new int[n];
        if (Area() > 0)
        {
            for (int v = 0; v < n; v++)
                V[v] = v;
        }
        else
        {
            for (int v = 0; v < n; v++)
                V[v] = (n - 1) - v;
        }

        int nv = n;
        int count = 2 * nv;
        for (int m = 0, v = nv; nv > 2;)
        {
            if ((count--) <= 0)
                return indices.ToArray();

            int u = v;
            if (nv <= u)
                u = 0;
            v = u + 1;
            if (nv <= v)
                v = 0;
            int w = v + 1;
            if (nv <= w)
                w = 0;

            if (Snip(u, v, w, nv, V))
            {
                int a, b, c, s, t;
                a = V[u];
                b = V[v];
                c = V[w];
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);
                m++;
                for (s = v, t = v + 1; t < nv; s++, t++)
                    V[s] = V[t];
                nv--;
                count = 2 * nv;
            }
        }

        indices.Reverse();
        return indices.ToArray();
    }

    private float Area()
    {
        int n = m_points.Count;
        float A = 0.0f;
        for (int p = n - 1, q = 0; q < n; p = q++)
        {
            Vector2 pval = m_points[p];
            Vector2 qval = m_points[q];
            A += pval.x * qval.y - qval.x * pval.y;
        }
        return (A * 0.5f);
    }

    private bool Snip(int u, int v, int w, int n, int[] V)
    {
        int p;
        Vector2 A = m_points[V[u]];
        Vector2 B = m_points[V[v]];
        Vector2 C = m_points[V[w]];
        if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
            return false;
        for (p = 0; p < n; p++)
        {
            if ((p == u) || (p == v) || (p == w))
                continue;
            Vector2 P = m_points[V[p]];
            if (InsideTriangle(A, B, C, P))
                return false;
        }
        return true;
    }

    private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
    {
        float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
        float cCROSSap, bCROSScp, aCROSSbp;

        ax = C.x - B.x; ay = C.y - B.y;
        bx = A.x - C.x; by = A.y - C.y;
        cx = B.x - A.x; cy = B.y - A.y;
        apx = P.x - A.x; apy = P.y - A.y;
        bpx = P.x - B.x; bpy = P.y - B.y;
        cpx = P.x - C.x; cpy = P.y - C.y;

        aCROSSbp = ax * bpy - ay * bpx;
        cCROSSap = cx * apy - cy * apx;
        bCROSScp = bx * cpy - by * cpx;

        return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
    }

    public static Mesh CreateMesh(Vector2[] poly)
    {
        // convert polygon to triangles
        Triangulator triangulator = new Triangulator(poly, false);

        int[] tris = triangulator.Triangulate();
        Mesh m = new Mesh();
        Vector3[] vertices = new Vector3[poly.Length];

        for (int i = 0; i < poly.Length; i++)
        {
            vertices[i].x = poly[i].x;
            vertices[i].y = 0.0f;
            vertices[i].z = poly[i].y; // front vertex
        }

        m.vertices = vertices;
        m.triangles = tris;

        //texture coordinate
        Vector2[] uvs = new Vector2[vertices.Length];

        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
        }

        m.uv = uvs;
        m.RecalculateNormals();
        m.RecalculateBounds();

        return m;
    }

    public static Mesh CreateMesh(Vector2[] poly, float extrusion)
    {
        Array.Resize<Vector2>(ref poly, poly.Length - 1);

        // convert polygon to triangles
        Triangulator triangulator = new Triangulator(poly, false);
        //Triangulator triangulator = new Triangulator(poly);

        int[] tris = triangulator.Triangulate();

        Mesh m = new Mesh();
        Vector3[] vertices = new Vector3[poly.Length * 2];

        for (int i = 0; i < poly.Length; i++)
        {
            vertices[i].x = poly[i].x;
            vertices[i].y = 0.0f;
            vertices[i].z = poly[i].y; // front vertex
            vertices[i + poly.Length].x = poly[i].x;
            vertices[i + poly.Length].y = extrusion;
            vertices[i + poly.Length].z = poly[i].y;  // back vertex     
        }

        int[] triangles = new int[tris.Length];

        int count_tris = 0;

        // If we want to render the underneath
        //for (int i = 0; i < tris.Length; i += 3)
        //{
        //    triangles[i] = tris[i];
        //    triangles[i + 1] = tris[i + 2];
        //    triangles[i + 2] = tris[i + 1];
        //} // front vertices

        //count_tris += tris.Length;

        for (int i = 0; i < tris.Length; i += 3)
        {
            triangles[count_tris + i] = tris[i + 1] + poly.Length;
            triangles[count_tris + i + 1] = tris[i + 2] + poly.Length;
            triangles[count_tris + i + 2] = tris[i] + poly.Length;
        }

        //texture coordinate
        Vector2[] uvs = new Vector2[vertices.Length];

        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
        }

        m.vertices = vertices;

        m.triangles = triangles;
        m.uv = uvs;
        m = Triangulator.SideExtrusion(m, IsClockwise(poly));
        m.RecalculateNormals();
        m.RecalculateBounds();

        return m;
    }

    static bool IsClockwise(Vector2[] poly)
    {
        float cnt = 0;
        for (int i = 0;i<poly.Length-1;i++)
        {
            cnt += (poly[i + 1].x - poly[i].x) * (poly[i + 1].y - poly[i].y);
        }
        return cnt >= 0;
    }

    private static Mesh SideExtrusion(Mesh mesh, bool Clockwise)
    {
        List<int> indices = new List<int>(mesh.triangles);
        int count = (mesh.vertices.Length / 2);
        for (int i = 0; i < count; i++)
        {
            int i1 = i;
            int i2 = (i1 + 1) % count;
            int i3 = i1 + count;
            int i4 = i2 + count;

            // Draw the polygons for this double-sided as some
            // of the buildings appear to follow a different winding
            // rule..
            //if (Clockwise)
            //{
                indices.Add(i4);
                indices.Add(i1);
                indices.Add(i3);
                indices.Add(i2);
                indices.Add(i1);
                indices.Add(i4);
            //}
            //else
            //{
                indices.Add(i4);
                indices.Add(i3);
                indices.Add(i1);
                indices.Add(i2);
                indices.Add(i4);
                indices.Add(i1);
            //}
        }
        mesh.triangles = indices.ToArray();
        return mesh;
    }
}