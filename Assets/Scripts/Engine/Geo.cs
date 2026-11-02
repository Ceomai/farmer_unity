// Three.js (r147) geometri üreticilerinin birebir karşılıkları → Unity Mesh.
// Tüm hesaplar Three koordinatında (sağ elli) yapılır, sonra Unity'ye çevrilir: z → -z, üçgen sırası ters çevrilir.
// Her ağın 3. UV kanalında (TEXCOORD3) dış çizgi için "yumuşatılmış normal" (JS smoothN → onormal) bulunur.
// Aynı parametreli ağlar önbelleğe alınır (paylaşılan Mesh; değiştirmeyin, Translate gibi yöntemler yeni ağ döndürür).
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Ciftlik
{
    public static class Geo
    {
        const float PI = (float)Math.PI, TAU = (float)(Math.PI * 2);
        static readonly Dictionary<string, Mesh> _cache = new Dictionary<string, Mesh>();
        static string K(string n, params object[] a)
        {
            var s = n;
            foreach (var o in a) s += "|" + (o is float f ? f.ToString("R", CultureInfo.InvariantCulture) : Convert.ToString(o, CultureInfo.InvariantCulture));
            return s;
        }
        static Mesh Cached(string key, Func<Mesh> make)
        {
            Mesh m;
            if (_cache.TryGetValue(key, out m) && m != null) return m;
            m = make(); m.name = key; _cache[key] = m; return m;
        }
        public static IEnumerable<Mesh> AllCached => _cache.Values;
        // UV3 kanalında yumuşatılmış normali (onormal) olan ağlar
        public static readonly HashSet<Mesh> WithONormal = new HashSet<Mesh>();

        // ---------- Three → Unity dönüşümü ----------
        // pos/nrm Three koordinatında; idx Three sarımıyla (CCW). uv boş olabilir.
        public static Mesh FromThree(List<Vector3> pos, List<Vector3> nrm, List<Vector2> uv, List<int> idx, string name = "geo")
        {
            int n = pos.Count;
            var on = SmoothNormals(pos, nrm);
            var up = new List<Vector3>(n); var un = new List<Vector3>(n); var uo = new List<Vector3>(n);
            for (int i = 0; i < n; i++)
            {
                up.Add(new Vector3(pos[i].x, pos[i].y, -pos[i].z));
                var q = nrm[i]; un.Add(new Vector3(q.x, q.y, -q.z));
                var o = on[i]; uo.Add(new Vector3(o.x, o.y, -o.z));
            }
            var tri = new List<int>(idx.Count);
            for (int i = 0; i + 2 < idx.Count; i += 3) { tri.Add(idx[i]); tri.Add(idx[i + 2]); tri.Add(idx[i + 1]); }
            var m = new Mesh { name = name };
            if (n > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(up); m.SetNormals(un);
            if (uv != null && uv.Count == n) m.SetUVs(0, uv);
            else { var z = new List<Vector2>(n); for (int i = 0; i < n; i++) z.Add(Vector2.zero); m.SetUVs(0, z); }
            m.SetUVs(3, uo); WithONormal.Add(m);
            m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            return m;
        }

        // JS smoothN: aynı konumdaki (3 ondalık) köşelerin normalleri toplanıp normalize edilir
        public static List<Vector3> SmoothNormals(List<Vector3> pos, List<Vector3> nrm)
        {
            var acc = new Dictionary<long, Vector3>();
            var keys = new long[pos.Count];
            for (int i = 0; i < pos.Count; i++)
            {
                var p = pos[i];
                long k = ((long)Math.Round(p.x * 1000.0) * 73856093L) ^ ((long)Math.Round(p.y * 1000.0) * 19349663L) ^ ((long)Math.Round(p.z * 1000.0) * 83492791L);
                keys[i] = k;
                Vector3 a; acc.TryGetValue(k, out a);
                acc[k] = new Vector3(a.x + nrm[i].x, a.y + nrm[i].y, a.z + nrm[i].z);
            }
            var r = new List<Vector3>(pos.Count);
            for (int i = 0; i < pos.Count; i++)
            {
                var a = acc[keys[i]];
                float l = (float)Math.Sqrt(a.x * a.x + a.y * a.y + a.z * a.z); if (l == 0) l = 1;
                r.Add(new Vector3(a.x / l, a.y / l, a.z / l));
            }
            return r;
        }

        // BufferGeometry.computeVertexNormals (indisli)
        public static List<Vector3> VertexNormals(List<Vector3> pos, List<int> idx)
        {
            var n = new Vector3[pos.Count];
            for (int i = 0; i + 2 < idx.Count; i += 3)
            {
                int a = idx[i], b = idx[i + 1], c = idx[i + 2];
                var cb = Sub(pos[c], pos[b]); var ab = Sub(pos[a], pos[b]);
                var f = Cross(cb, ab);
                n[a] = Add(n[a], f); n[b] = Add(n[b], f); n[c] = Add(n[c], f);
            }
            var r = new List<Vector3>(pos.Count);
            for (int i = 0; i < n.Length; i++) r.Add(Norm(n[i]));
            return r;
        }

        // ---------- küçük vektör yardımcıları (stub/çalışma zamanından bağımsız, kesin) ----------
        internal static Vector3 Add(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        internal static Vector3 Sub(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        internal static Vector3 Mul(Vector3 a, float s) => new Vector3(a.x * s, a.y * s, a.z * s);
        internal static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        internal static float Len(Vector3 a) => (float)Math.Sqrt(a.x * a.x + a.y * a.y + a.z * a.z);
        internal static Vector3 Norm(Vector3 a) { float l = Len(a); return l > 0 ? new Vector3(a.x / l, a.y / l, a.z / l) : new Vector3(0, 0, 0); }
        internal static Vector3 Lerp(Vector3 a, Vector3 b, float t) => new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        static float Sin(float a) => (float)Math.Sin(a);
        static float Cos(float a) => (float)Math.Cos(a);

        // ---------- BoxGeometry ----------
        public static Mesh Box(float w = 1, float h = 1, float d = 1, int ws = 1, int hs = 1, int ds = 1) => Cached(K("box", w, h, d, ws, hs, ds), () =>
        {
            var P = new List<Vector3>(); var N = new List<Vector3>(); var U = new List<Vector2>(); var I = new List<int>();
            // buildPlane(u, v, w, udir, vdir, width, height, depth, gridX, gridY)
            void BuildPlane(int u, int v, int ww, float udir, float vdir, float width, float height, float depth, int gx, int gy)
            {
                float sw = width / gx, sh = height / gy, wh = width / 2, hh = height / 2, dh = depth / 2;
                int gx1 = gx + 1, gy1 = gy + 1, start = P.Count;
                for (int iy = 0; iy < gy1; iy++)
                {
                    float y = iy * sh - hh;
                    for (int ix = 0; ix < gx1; ix++)
                    {
                        float x = ix * sw - wh;
                        var vec = new float[3]; vec[u] = x * udir; vec[v] = y * vdir; vec[ww] = dh;
                        P.Add(new Vector3(vec[0], vec[1], vec[2]));
                        var nn = new float[3]; nn[ww] = depth > 0 ? 1 : -1;
                        N.Add(new Vector3(nn[0], nn[1], nn[2]));
                        U.Add(new Vector2((float)ix / gx, 1 - (float)iy / gy));
                    }
                }
                for (int iy = 0; iy < gy; iy++) for (int ix = 0; ix < gx; ix++)
                    {
                        int a = start + ix + gx1 * iy, b = start + ix + gx1 * (iy + 1), c = start + (ix + 1) + gx1 * (iy + 1), e = start + (ix + 1) + gx1 * iy;
                        I.Add(a); I.Add(b); I.Add(e); I.Add(b); I.Add(c); I.Add(e);
                    }
            }
            const int X = 0, Y = 1, Z = 2;
            BuildPlane(Z, Y, X, -1, -1, d, h, w, ds, hs);
            BuildPlane(Z, Y, X, 1, -1, d, h, -w, ds, hs);
            BuildPlane(X, Z, Y, 1, 1, w, d, h, ws, ds);
            BuildPlane(X, Z, Y, 1, -1, w, d, -h, ws, ds);
            BuildPlane(X, Y, Z, 1, -1, w, h, d, ws, hs);
            BuildPlane(X, Y, Z, -1, -1, w, h, -d, ws, hs);
            return FromThree(P, N, U, I);
        });

        // ---------- SphereGeometry ----------
        public static Mesh Sphere(float r = 1, int ws = 32, int hs = 16, float phiStart = 0, float phiLength = TAU, float thetaStart = 0, float thetaLength = PI) =>
            Cached(K("sph", r, ws, hs, phiStart, phiLength, thetaStart, thetaLength), () =>
        {
            ws = Math.Max(3, ws); hs = Math.Max(2, hs);
            float thetaEnd = Math.Min(thetaStart + thetaLength, PI);
            var P = new List<Vector3>(); var N = new List<Vector3>(); var U = new List<Vector2>(); var I = new List<int>();
            var grid = new List<int[]>(); int index = 0;
            for (int iy = 0; iy <= hs; iy++)
            {
                var row = new int[ws + 1]; float v = (float)iy / hs;
                float uOff = 0;
                if (iy == 0 && thetaStart == 0) uOff = .5f / ws; else if (iy == hs && thetaEnd == PI) uOff = -.5f / ws;
                for (int ix = 0; ix <= ws; ix++)
                {
                    float u = (float)ix / ws;
                    var p = new Vector3(-r * Cos(phiStart + u * phiLength) * Sin(thetaStart + v * thetaLength), r * Cos(thetaStart + v * thetaLength), r * Sin(phiStart + u * phiLength) * Sin(thetaStart + v * thetaLength));
                    P.Add(p); N.Add(Norm(p)); U.Add(new Vector2(u + uOff, 1 - v));
                    row[ix] = index++;
                }
                grid.Add(row);
            }
            for (int iy = 0; iy < hs; iy++) for (int ix = 0; ix < ws; ix++)
                {
                    int a = grid[iy][ix + 1], b = grid[iy][ix], c = grid[iy + 1][ix], d = grid[iy + 1][ix + 1];
                    if (iy != 0 || thetaStart > 0) { I.Add(a); I.Add(b); I.Add(d); }
                    if (iy != hs - 1 || thetaEnd < PI) { I.Add(b); I.Add(c); I.Add(d); }
                }
            return FromThree(P, N, U, I);
        });

        // ---------- CylinderGeometry / ConeGeometry ----------
        public static Mesh Cylinder(float rt = 1, float rb = 1, float h = 1, int rs = 8, int hs = 1, bool openEnded = false, float thetaStart = 0, float thetaLength = TAU) =>
            Cached(K("cyl", rt, rb, h, rs, hs, openEnded, thetaStart, thetaLength), () =>
        {
            var P = new List<Vector3>(); var N = new List<Vector3>(); var U = new List<Vector2>(); var I = new List<int>();
            float halfH = h / 2; int index = 0;
            var grid = new List<int[]>();
            float slope = (rb - rt) / h;
            for (int y = 0; y <= hs; y++)
            {
                var row = new int[rs + 1]; float v = (float)y / hs; float radius = v * (rb - rt) + rt;
                for (int x = 0; x <= rs; x++)
                {
                    float u = (float)x / rs, th = u * thetaLength + thetaStart, s = Sin(th), c = Cos(th);
                    P.Add(new Vector3(radius * s, -v * h + halfH, radius * c));
                    N.Add(Norm(new Vector3(s, slope, c)));
                    U.Add(new Vector2(u, 1 - v));
                    row[x] = index++;
                }
                grid.Add(row);
            }
            for (int x = 0; x < rs; x++) for (int y = 0; y < hs; y++)
                {
                    int a = grid[y][x], b = grid[y + 1][x], c = grid[y + 1][x + 1], d = grid[y][x + 1];
                    I.Add(a); I.Add(b); I.Add(d); I.Add(b); I.Add(c); I.Add(d);
                }
            void Cap(bool top)
            {
                int centerStart = index; float radius = top ? rt : rb; float sign = top ? 1 : -1;
                for (int x = 1; x <= rs; x++) { P.Add(new Vector3(0, halfH * sign, 0)); N.Add(new Vector3(0, sign, 0)); U.Add(new Vector2(.5f, .5f)); index++; }
                int centerEnd = index;
                for (int x = 0; x <= rs; x++)
                {
                    float u = (float)x / rs, th = u * thetaLength + thetaStart, c = Cos(th), s = Sin(th);
                    P.Add(new Vector3(radius * s, halfH * sign, radius * c)); N.Add(new Vector3(0, sign, 0));
                    U.Add(new Vector2(c * .5f + .5f, s * .5f * sign + .5f)); index++;
                }
                for (int x = 0; x < rs; x++)
                {
                    int c = centerStart + x, i = centerEnd + x;
                    if (top) { I.Add(i); I.Add(i + 1); I.Add(c); } else { I.Add(i + 1); I.Add(i); I.Add(c); }
                }
            }
            if (!openEnded) { if (rt > 0) Cap(true); if (rb > 0) Cap(false); }
            return FromThree(P, N, U, I);
        });
        public static Mesh Cone(float r = 1, float h = 1, int rs = 8, int hs = 1, bool openEnded = false, float thetaStart = 0, float thetaLength = TAU) => Cylinder(0, r, h, rs, hs, openEnded, thetaStart, thetaLength);

        // ---------- Icosahedron (PolyhedronGeometry) ----------
        static readonly float T = (float)((1 + Math.Sqrt(5)) / 2);
        static readonly float[] ICO_V = { -1, T, 0, 1, T, 0, -1, -T, 0, 1, -T, 0, 0, -1, T, 0, 1, T, 0, -1, -T, 0, 1, -T, T, 0, -1, T, 0, 1, -T, 0, -1, -T, 0, 1 };
        static readonly int[] ICO_I = { 0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8, 3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1 };
        public static Mesh Icosahedron(float radius = 1, int detail = 0) => Cached(K("ico", radius, detail), () => Polyhedron(ICO_V, ICO_I, radius, detail));
        public static Mesh Polyhedron(float[] verts, int[] faces, float radius, int detail)
        {
            var V = new List<Vector3>();
            Vector3 Vt(int i) => new Vector3(verts[i * 3], verts[i * 3 + 1], verts[i * 3 + 2]);
            for (int f = 0; f < faces.Length; f += 3)
            {
                Vector3 a = Vt(faces[f]), b = Vt(faces[f + 1]), c = Vt(faces[f + 2]);
                int cols = detail + 1;
                var v = new List<Vector3[]>();
                for (int i = 0; i <= cols; i++)
                {
                    var aj = Lerp(a, c, (float)i / cols); var bj = Lerp(b, c, (float)i / cols); int rows = cols - i;
                    var row = new Vector3[rows + 1];
                    for (int j = 0; j <= rows; j++) row[j] = (j == 0 && i == cols) ? aj : Lerp(aj, bj, (float)j / rows);
                    v.Add(row);
                }
                for (int i = 0; i < cols; i++)
                    for (int j = 0; j < 2 * (cols - i) - 1; j++)
                    {
                        int k = j / 2;
                        if (j % 2 == 0) { V.Add(v[i][k + 1]); V.Add(v[i + 1][k]); V.Add(v[i][k]); }
                        else { V.Add(v[i][k + 1]); V.Add(v[i + 1][k + 1]); V.Add(v[i + 1][k]); }
                    }
            }
            var P = new List<Vector3>(V.Count); var N = new List<Vector3>(V.Count); var U = new List<Vector2>(V.Count); var I = new List<int>(V.Count);
            for (int i = 0; i < V.Count; i++) P.Add(Mul(Norm(V[i]), radius));
            if (detail == 0)
            {
                for (int i = 0; i < P.Count; i += 3) { var nn = Norm(Cross(Sub(P[i + 2], P[i + 1]), Sub(P[i], P[i + 1]))); N.Add(nn); N.Add(nn); N.Add(nn); }
            }
            else for (int i = 0; i < P.Count; i++) N.Add(Norm(P[i]));
            for (int i = 0; i < P.Count; i++)
            {
                var p = P[i];
                float u = (float)(Math.Atan2(p.z, -p.x) / TAU + .5), vv = (float)(Math.Atan2(-p.y, Math.Sqrt(p.x * p.x + p.z * p.z)) / PI + .5);
                U.Add(new Vector2(u, vv)); I.Add(i);
            }
            return FromThree(P, N, U, I);
        }

        // ---------- TorusGeometry ----------
        public static Mesh Torus(float radius = 1, float tube = .4f, int radialSegments = 12, int tubularSegments = 48, float arc = TAU) =>
            Cached(K("tor", radius, tube, radialSegments, tubularSegments, arc), () =>
        {
            var P = new List<Vector3>(); var N = new List<Vector3>(); var U = new List<Vector2>(); var I = new List<int>();
            for (int j = 0; j <= radialSegments; j++)
                for (int i = 0; i <= tubularSegments; i++)
                {
                    float u = (float)i / tubularSegments * arc, v = (float)j / radialSegments * TAU;
                    var p = new Vector3((radius + tube * Cos(v)) * Cos(u), (radius + tube * Cos(v)) * Sin(u), tube * Sin(v));
                    P.Add(p);
                    var c = new Vector3(radius * Cos(u), radius * Sin(u), 0);
                    N.Add(Norm(Sub(p, c)));
                    U.Add(new Vector2((float)i / tubularSegments, (float)j / radialSegments));
                }
            for (int j = 1; j <= radialSegments; j++)
                for (int i = 1; i <= tubularSegments; i++)
                {
                    int a = (tubularSegments + 1) * j + i - 1, b = (tubularSegments + 1) * (j - 1) + i - 1, c = (tubularSegments + 1) * (j - 1) + i, d = (tubularSegments + 1) * j + i;
                    I.Add(a); I.Add(b); I.Add(d); I.Add(b); I.Add(c); I.Add(d);
                }
            return FromThree(P, N, U, I);
        });

        // ---------- PlaneGeometry ----------
        public static Mesh Plane(float w = 1, float h = 1, int ws = 1, int hs = 1) => Cached(K("plane", w, h, ws, hs), () =>
        {
            var P = new List<Vector3>(); var N = new List<Vector3>(); var U = new List<Vector2>(); var I = new List<int>();
            float wh = w / 2, hh = h / 2, sw = w / ws, sh = h / hs; int gx1 = ws + 1, gy1 = hs + 1;
            for (int iy = 0; iy < gy1; iy++) { float y = iy * sh - hh; for (int ix = 0; ix < gx1; ix++) { float x = ix * sw - wh; P.Add(new Vector3(x, -y, 0)); N.Add(new Vector3(0, 0, 1)); U.Add(new Vector2((float)ix / ws, 1 - (float)iy / hs)); } }
            for (int iy = 0; iy < hs; iy++) for (int ix = 0; ix < ws; ix++)
                {
                    int a = ix + gx1 * iy, b = ix + gx1 * (iy + 1), c = (ix + 1) + gx1 * (iy + 1), d = (ix + 1) + gx1 * iy;
                    I.Add(a); I.Add(b); I.Add(d); I.Add(b); I.Add(c); I.Add(d);
                }
            return FromThree(P, N, U, I);
        });

        // ---------- CircleGeometry ----------
        public static Mesh Circle(float r = 1, int segments = 32, float thetaStart = 0, float thetaLength = TAU) => Cached(K("circle", r, segments, thetaStart, thetaLength), () =>
        {
            segments = Math.Max(3, segments);
            var P = new List<Vector3>(); var N = new List<Vector3>(); var U = new List<Vector2>(); var I = new List<int>();
            P.Add(new Vector3(0, 0, 0)); N.Add(new Vector3(0, 0, 1)); U.Add(new Vector2(.5f, .5f));
            for (int s = 0; s <= segments; s++)
            {
                float seg = thetaStart + (float)s / segments * thetaLength;
                var p = new Vector3(r * Cos(seg), r * Sin(seg), 0);
                P.Add(p); N.Add(new Vector3(0, 0, 1)); U.Add(new Vector2((p.x / r + 1) / 2, (p.y / r + 1) / 2));
            }
            for (int i = 1; i <= segments; i++) { I.Add(i); I.Add(i + 1); I.Add(0); }
            return FromThree(P, N, U, I);
        });

        // ---------- RingGeometry ----------
        public static Mesh Ring(float innerRadius = .5f, float outerRadius = 1, int thetaSegments = 32, int phiSegments = 1, float thetaStart = 0, float thetaLength = TAU) =>
            Cached(K("ring", innerRadius, outerRadius, thetaSegments, phiSegments, thetaStart, thetaLength), () =>
        {
            thetaSegments = Math.Max(3, thetaSegments); phiSegments = Math.Max(1, phiSegments);
            var P = new List<Vector3>(); var N = new List<Vector3>(); var U = new List<Vector2>(); var I = new List<int>();
            float radius = innerRadius, step = (outerRadius - innerRadius) / phiSegments;
            for (int j = 0; j <= phiSegments; j++)
            {
                for (int i = 0; i <= thetaSegments; i++)
                {
                    float seg = thetaStart + (float)i / thetaSegments * thetaLength;
                    var p = new Vector3(radius * Cos(seg), radius * Sin(seg), 0);
                    P.Add(p); N.Add(new Vector3(0, 0, 1)); U.Add(new Vector2((p.x / outerRadius + 1) / 2, (p.y / outerRadius + 1) / 2));
                }
                radius += step;
            }
            for (int j = 0; j < phiSegments; j++)
            {
                int lvl = j * (thetaSegments + 1);
                for (int i = 0; i < thetaSegments; i++)
                {
                    int seg = i + lvl, a = seg, b = seg + thetaSegments + 1, c = seg + thetaSegments + 2, d = seg + 1;
                    I.Add(a); I.Add(b); I.Add(d); I.Add(b); I.Add(c); I.Add(d);
                }
            }
            return FromThree(P, N, U, I);
        });

        // ---------- LatheGeometry ----------
        public static Mesh Lathe(Vector2[] points, int segments = 12, float phiStart = 0, float phiLength = TAU)
        {
            var key = "lathe"; foreach (var p in points) key += "|" + p.x.ToString("R", CultureInfo.InvariantCulture) + "," + p.y.ToString("R", CultureInfo.InvariantCulture);
            return Cached(K(key, segments, phiStart, phiLength), () =>
            {
                phiLength = Mathf.Clamp(phiLength, 0, TAU);
                var P = new List<Vector3>(); var U = new List<Vector2>(); var I = new List<int>();
                float inv = 1f / segments;
                for (int i = 0; i <= segments; i++)
                {
                    float phi = phiStart + i * inv * phiLength, s = Sin(phi), c = Cos(phi);
                    for (int j = 0; j < points.Length; j++)
                    {
                        P.Add(new Vector3(points[j].x * s, points[j].y, points[j].x * c));
                        U.Add(new Vector2((float)i / segments, (float)j / (points.Length - 1)));
                    }
                }
                for (int i = 0; i < segments; i++)
                    for (int j = 0; j < points.Length - 1; j++)
                    {
                        int b0 = j + i * points.Length, a = b0, b = b0 + points.Length, c = b0 + points.Length + 1, d = b0 + 1;
                        I.Add(a); I.Add(b); I.Add(d); I.Add(c); I.Add(d); I.Add(b);
                    }
                return FromThree(P, VertexNormals(P, I), U, I);
            });
        }

        // ---------- ExtrudeGeometry (bevel yok, steps=1) ----------
        // shape: kapalı çokgen noktaları (kapanış tekrarı olmadan). Ön yüz z=+depth/2... Three'deki gibi z=0..depth üretilir;
        // centerZ=true ise translate(0,0,-depth/2) uygulanmış olur (JS'te GE.tri/gam/arch böyle). Normaller düz (computeVertexNormals, indissiz).
        public static Mesh Extrude(Vector2[] shape, float depth = 1, bool centerZ = true, string key = null)
        {
            if (key == null) { key = "extr"; foreach (var p in shape) key += "|" + p.x.ToString("R", CultureInfo.InvariantCulture) + "," + p.y.ToString("R", CultureInfo.InvariantCulture); }
            return Cached(K(key, depth, centerZ), () =>
            {
                var pts = new List<Vector2>(shape);
                if (pts.Count > 1 && pts[0].x == pts[pts.Count - 1].x && pts[0].y == pts[pts.Count - 1].y) pts.RemoveAt(pts.Count - 1);
                // saat yönünün tersine (CCW) çevir
                float area = 0; for (int i = 0, j = pts.Count - 1; i < pts.Count; j = i++) area += pts[j].x * pts[i].y - pts[i].x * pts[j].y;
                if (area < 0) pts.Reverse();
                var tris = Triangulate(pts);
                float z0 = centerZ ? -depth / 2 : 0, z1 = z0 + depth;
                var P = new List<Vector3>(); var U = new List<Vector2>();
                // arka kapak (z0, normal -z): ters sarım
                for (int i = 0; i < tris.Count; i += 3)
                    foreach (var k in new[] { tris[i + 2], tris[i + 1], tris[i] }) { P.Add(new Vector3(pts[k].x, pts[k].y, z0)); U.Add(new Vector2(pts[k].x, pts[k].y)); }
                // ön kapak (z1, normal +z)
                for (int i = 0; i < tris.Count; i += 3)
                    foreach (var k in new[] { tris[i], tris[i + 1], tris[i + 2] }) { P.Add(new Vector3(pts[k].x, pts[k].y, z1)); U.Add(new Vector2(pts[k].x, pts[k].y)); }
                // yan duvarlar
                for (int i = 0; i < pts.Count; i++)
                {
                    var a = pts[i]; var b = pts[(i + 1) % pts.Count];
                    Vector3 A0 = new Vector3(a.x, a.y, z0), B0 = new Vector3(b.x, b.y, z0), A1 = new Vector3(a.x, a.y, z1), B1 = new Vector3(b.x, b.y, z1);
                    bool xMajor = Math.Abs(a.y - b.y) < Math.Abs(a.x - b.x);
                    Vector2 Uv(Vector3 p) => xMajor ? new Vector2(p.x, 1 - p.z) : new Vector2(p.y, 1 - p.z);
                    foreach (var p in new[] { A0, B0, B1, A0, B1, A1 }) { P.Add(p); U.Add(Uv(p)); }
                }
                var N = new List<Vector3>(P.Count); var I = new List<int>(P.Count);
                for (int i = 0; i < P.Count; i += 3) { var nn = Norm(Cross(Sub(P[i + 2], P[i + 1]), Sub(P[i], P[i + 1]))); N.Add(nn); N.Add(nn); N.Add(nn); }
                for (int i = 0; i < P.Count; i++) I.Add(i);
                return FromThree(P, N, U, I);
            });
        }

        // Basit kulak kırpma üçgenlemesi (CCW basit çokgen)
        public static List<int> Triangulate(List<Vector2> pts)
        {
            var res = new List<int>(); var V = new List<int>(); for (int i = 0; i < pts.Count; i++) V.Add(i);
            int guard = 0;
            while (V.Count > 3 && guard++ < 10000)
            {
                bool cut = false;
                for (int i = 0; i < V.Count; i++)
                {
                    int ia = V[(i + V.Count - 1) % V.Count], ib = V[i], ic = V[(i + 1) % V.Count];
                    Vector2 a = pts[ia], b = pts[ib], c = pts[ic];
                    float cr = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
                    if (cr <= 1e-9f) continue;
                    bool inside = false;
                    foreach (var o in V)
                    {
                        if (o == ia || o == ib || o == ic) continue;
                        if (InTri(pts[o], a, b, c)) { inside = true; break; }
                    }
                    if (inside) continue;
                    res.Add(ia); res.Add(ib); res.Add(ic); V.RemoveAt(i); cut = true; break;
                }
                if (!cut) break;
            }
            if (V.Count == 3) { res.Add(V[0]); res.Add(V[1]); res.Add(V[2]); }
            else if (V.Count > 3) for (int i = 1; i + 1 < V.Count; i++) { res.Add(V[0]); res.Add(V[i]); res.Add(V[i + 1]); }
            return res;
        }
        static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }

        // THREE.Path yardımcıları: absarc noktaları (EllipseCurve, getPoints(divisions*2) çözünürlüğü)
        public static void AbsArc(List<Vector2> path, float cx, float cy, float r, float a0, float a1, bool clockwise, int curveSegments = 12)
        {
            float delta = a1 - a0; bool same = Math.Abs(delta) < 1e-7f;
            while (delta < 0) delta += TAU; while (delta > TAU) delta -= TAU;
            if (delta < 1e-7f) delta = same ? 0 : TAU;
            if (clockwise && !same) delta = delta == TAU ? -TAU : delta - TAU;
            int res = curveSegments * 2;
            for (int i = 0; i <= res; i++)
            {
                float a = a0 + (float)i / res * delta;
                var p = new Vector2(cx + r * Cos(a), cy + r * Sin(a));
                if (path.Count > 0) { var l = path[path.Count - 1]; if (Math.Abs(l.x - p.x) < 1e-6f && Math.Abs(l.y - p.y) < 1e-6f) continue; }
                path.Add(p);
            }
        }

        // ---------- geometri dönüşümleri (yeni ağ döndürür) ----------
        // Three koordinatında öteleme (geometry.translate)
        public static Mesh Translate(Mesh src, float x, float y, float z)
        {
            var m = UnityEngine.Object.Instantiate(src);
            var v = m.vertices; var d = new Vector3(x, y, -z);
            for (int i = 0; i < v.Length; i++) v[i] = Add(v[i], d);
            m.vertices = v; m.RecalculateBounds(); m.name = src.name + "+t"; WithONormal.Add(m);
            return m;
        }
    }

    // THREE.CatmullRomCurve3 ('catmullrom', kapalı değil) — Three koordinatında çalışır
    public class CatmullRomCurve3
    {
        public readonly List<Vector3> points;
        public bool closed; public string curveType; public float tension;
        public int arcLengthDivisions = 200;
        float[] _lengths;
        public CatmullRomCurve3(List<Vector3> pts, bool closed = false, string curveType = "centripetal", float tension = .5f)
        { points = pts; this.closed = closed; this.curveType = curveType; this.tension = tension; }

        struct Cubic
        {
            public float c0, c1, c2, c3;
            public void Init(float x0, float x1, float t0, float t1) { c0 = x0; c1 = t0; c2 = -3 * x0 + 3 * x1 - 2 * t0 - t1; c3 = 2 * x0 - 2 * x1 + t0 + t1; }
            public void InitCatmullRom(float x0, float x1, float x2, float x3, float tension) => Init(x1, x2, tension * (x2 - x0), tension * (x3 - x1));
            public void InitNonuniform(float x0, float x1, float x2, float x3, float dt0, float dt1, float dt2)
            {
                float t1 = (x1 - x0) / dt0 - (x2 - x0) / (dt0 + dt1) + (x2 - x1) / dt1;
                float t2 = (x2 - x1) / dt1 - (x3 - x1) / (dt1 + dt2) + (x3 - x2) / dt2;
                t1 *= dt1; t2 *= dt1; Init(x1, x2, t1, t2);
            }
            public float Calc(float t) { float t2 = t * t, t3 = t2 * t; return c0 + c1 * t + c2 * t2 + c3 * t3; }
        }

        public Vector3 GetPoint(float t)
        {
            var pts = points; int l = pts.Count;
            float p = (l - (closed ? 0 : 1)) * t;
            int ip = (int)Math.Floor(p); float weight = p - ip;
            if (closed) ip += ip > 0 ? 0 : ((int)Math.Floor(Math.Abs(ip) / (float)l) + 1) * l;
            else if (weight == 0 && ip == l - 1) { ip = l - 2; weight = 1; }
            Vector3 p0, p3;
            if (closed || ip > 0) p0 = pts[(ip - 1 + l) % l];
            else p0 = Geo.Add(Geo.Sub(pts[0], pts[1]), pts[0]);
            var p1 = pts[ip % l]; var p2 = pts[(ip + 1) % l];
            if (closed || ip + 2 < l) p3 = pts[(ip + 2) % l];
            else p3 = Geo.Add(Geo.Sub(pts[l - 1], pts[l - 2]), pts[l - 1]);
            Cubic px = default(Cubic), py = default(Cubic), pz = default(Cubic);
            if (curveType == "centripetal" || curveType == "chordal")
            {
                float pw = curveType == "chordal" ? .5f : .25f;
                float dt0 = (float)Math.Pow(Sq(p0, p1), pw), dt1 = (float)Math.Pow(Sq(p1, p2), pw), dt2 = (float)Math.Pow(Sq(p2, p3), pw);
                if (dt1 < 1e-4f) dt1 = 1; if (dt0 < 1e-4f) dt0 = dt1; if (dt2 < 1e-4f) dt2 = dt1;
                px.InitNonuniform(p0.x, p1.x, p2.x, p3.x, dt0, dt1, dt2); py.InitNonuniform(p0.y, p1.y, p2.y, p3.y, dt0, dt1, dt2); pz.InitNonuniform(p0.z, p1.z, p2.z, p3.z, dt0, dt1, dt2);
            }
            else
            {
                px.InitCatmullRom(p0.x, p1.x, p2.x, p3.x, tension); py.InitCatmullRom(p0.y, p1.y, p2.y, p3.y, tension); pz.InitCatmullRom(p0.z, p1.z, p2.z, p3.z, tension);
            }
            return new Vector3(px.Calc(weight), py.Calc(weight), pz.Calc(weight));
        }
        static float Sq(Vector3 a, Vector3 b) { var d = Geo.Sub(a, b); return d.x * d.x + d.y * d.y + d.z * d.z; }

        public float[] GetLengths()
        {
            if (_lengths != null && _lengths.Length == arcLengthDivisions + 1) return _lengths;
            var cache = new float[arcLengthDivisions + 1]; var last = GetPoint(0); float sum = 0; cache[0] = 0;
            for (int p = 1; p <= arcLengthDivisions; p++) { var cur = GetPoint((float)p / arcLengthDivisions); sum += Geo.Len(Geo.Sub(cur, last)); cache[p] = sum; last = cur; }
            return _lengths = cache;
        }
        public float GetLength() { var l = GetLengths(); return l[l.Length - 1]; }
        public float GetUtoTmapping(float u)
        {
            var arc = GetLengths(); int il = arc.Length;
            float target = u * arc[il - 1];
            int low = 0, high = il - 1, i = 0;
            while (low <= high)
            {
                i = low + (high - low) / 2;
                float cmp = arc[i] - target;
                if (cmp < 0) low = i + 1; else if (cmp > 0) high = i - 1; else { high = i; break; }
            }
            i = high; if (i < 0) i = 0;
            if (arc[i] == target) return (float)i / (il - 1);
            float before = arc[i], after = arc[Math.Min(i + 1, il - 1)], seg = after - before;
            float frac = seg > 0 ? (target - before) / seg : 0;
            return (i + frac) / (il - 1);
        }
        public Vector3 GetPointAt(float u) => GetPoint(GetUtoTmapping(u));
        public Vector3 GetTangent(float t)
        {
            const float delta = .0001f;
            float t1 = Math.Max(0, t - delta), t2 = Math.Min(1, t + delta);
            return Geo.Norm(Geo.Sub(GetPoint(t2), GetPoint(t1)));
        }
        public Vector3 GetTangentAt(float u) => GetTangent(GetUtoTmapping(u));
    }
}
