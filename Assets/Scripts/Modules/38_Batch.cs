// ================= STATİK BİRLEŞTİRME (draw call azaltma) (38-batch.js) =================
// Haritadaki binlerce küçük parça (çit, taş, çatı, çalı...) her karede ayrı ayrı çiziliyordu (~1800 çizim çağrısı).
// Bir kök kaydedilir; birkaç saniye boyunca hiç kıpırdamayan parçalar aynı malzemeye göre tek bir ağ (mesh) olarak birleştirilir.
// Hareket eden her şey (hayvanlar, bulutlar, yel değirmeni, duman...) olduğu gibi kalır. Tıklama için gereken etiketli
// gruplar (loc/unit/plot/region...) korunur: birleşen ağ o grubun içine eklenir.
// Unity: THREE.Mesh → MeshFilter+MeshRenderer, dış çizgi → OUT malzemeli "ol" çocukları (06-engine Outline), userData → UData.
// Seçim için Collider taşıyan parçalar birleştirilmez (ışın testi onlara dayanır).
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ciftlik
{
    // userData.baked=1 işareti
    // removed: birleştirilen asıl parçalar. JS onları ağaçtan çıkarır ama başka modüllerin elindeki başvurular (allFx vb.) canlı kalır;
    // Unity'de Destroy edilse MissingReferenceException olur → pasif "mezarlığa" taşınır, DisposeBaked'de yok edilir.
    public class Bt_Baked : MonoBehaviour { public List<GameObject> removed = new List<GameObject>(); }

    public static partial class Game
    {
        static readonly string[] BATCH_TAGS = { "plot", "plotBuy", "unit", "loc", "region", "farmer", "open", "act", "noBatch" };

        class Bt_Link { public Transform o; public Matrix4x4 s; }
        class Bt_Entry { public Transform m; public Transform anchor; public List<Bt_Link> chain; }
        class Bt_Queue { public Transform root; public List<Bt_Entry> list; public double at; }
        class Bt_Item { public Transform m; public Matrix4x4 rel; public bool ol; }
        class Bt_Group { public Material mat; public bool cs, rs; public List<Bt_Item> items = new List<Bt_Item>(); }
        static readonly List<Bt_Queue> _bq = new List<Bt_Queue>();

        // const isTagged=o=>{for(const k of BATCH_TAGS)if(u[k]!=null)return true;return false}
        static readonly Dictionary<Type, List<MemberInfo>> _btMembers = new Dictionary<Type, List<MemberInfo>>();
        static bool IsTagged(Transform o)
        {
            var u = o.GetComponent<UData>(); if (u == null) return false;
            if (u.hasPlot) return true;   // JS userData.plot != null (UData.plot bir int; atanmışlığı hasPlot tutar)
            var t = u.GetType(); List<MemberInfo> ms;
            if (!_btMembers.TryGetValue(t, out ms))
            {
                ms = new List<MemberInfo>();
                foreach (var k in BATCH_TAGS)
                {
                    var f = t.GetField(k, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); if (f != null) { ms.Add(f); continue; }
                    var p = t.GetProperty(k, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); if (p != null) ms.Add(p);
                }
                _btMembers[t] = ms;
            }
            foreach (var m in ms)
            {
                object v; Type vt;
                if (m is FieldInfo) { v = ((FieldInfo)m).GetValue(u); vt = ((FieldInfo)m).FieldType; } else { v = ((PropertyInfo)m).GetValue(u, null); vt = ((PropertyInfo)m).PropertyType; }
                if (Bt_Set(v, vt)) return true;
            }
            return false;
        }
        // JS "!= null": null / boş dizgi / false / değer tipinde (Nullable olmayan) sayılar "atanmamış" sayılır
        static bool Bt_Set(object v, Type t)
        {
            if (v == null) return false;
            if (v is string) return ((string)v).Length > 0;
            if (v is bool) return (bool)v;
            if (t.IsValueType && Nullable.GetUnderlyingType(t) == null) return false;
            return true;
        }
        static bool Bt_IsOut(Transform x) { var r = x.GetComponent<MeshRenderer>(); return r != null && r.sharedMaterial == OUT; }
        static bool Bt_Transparent(Material m) => m == null || Eng_IsTransparent(m) || m.renderQueue >= 3000;

        static List<Bt_Entry> BatchCandidates(Transform root, Func<Transform, bool> skip)
        {
            var outL = new List<Bt_Entry>();
            Action<Transform, Transform> walk = null;
            walk = (o, anchor) =>
            {
                for (int i = 0; i < o.childCount; i++)
                {
                    var c = o.GetChild(i);
                    if (skip != null && skip(c)) continue; if (Bt_IsOut(c)) continue;
                    var mr = c.GetComponent<MeshRenderer>(); var mf = c.GetComponent<MeshFilter>();
                    if (mr != null && mf != null && mf.sharedMesh != null && mf.sharedMesh.isReadable && mr.sharedMaterials != null && mr.sharedMaterials.Length == 1
                        && !Bt_Transparent(mr.sharedMaterial) && mr.sharedMaterial != OUT && !IsTagged(c) && c.GetComponent<Collider>() == null && c.GetComponent<Bt_Baked>() == null
                        && Bt_AllOut(c) && c.gameObject.activeSelf && mr.enabled)
                    { outL.Add(new Bt_Entry { m = c, anchor = anchor }); continue; }
                    if (c.gameObject.activeSelf && c.GetComponent<ParticleSystem>() == null && c.GetComponent<LineRenderer>() == null && c.GetComponent<SkinnedMeshRenderer>() == null)
                        walk(c, IsTagged(c) ? c : anchor);
                }
            };
            walk(root, root);
            return outL;
        }
        static Transform _btGrave;
        static Transform Bt_Grave()
        {
            if (_btGrave == null) { var go = new GameObject("batch-removed"); go.SetActive(false); _btGrave = go.transform; }
            return _btGrave;
        }
        static bool Bt_AllOut(Transform c) { for (int i = 0; i < c.childCount; i++) if (!Bt_IsOut(c.GetChild(i))) return false; return true; }
        static Matrix4x4 RelMat(Transform m, Transform anchor) => anchor.worldToLocalMatrix * m.localToWorldMatrix;

        // Kökü izlemeye al: belirtilen süre sonra kıpırdamamış olanları birleştir
        // Her parça için, etiketli gruba kadar tüm üst gruplara göre konumu kaydedilir; birleştirirken parçanın hiç kıpırdamadığı EN ÜST grup seçilir
        // (ör. süzülen bir bulut kümesinin parçaları kümenin içinde sabittir → küme başına tek ağ).
        public static void BatchWatch(Transform root, Func<Transform, bool> skip = null, double delay = 2600)
        {
            if (root == null) return;
            var list = BatchCandidates(root, skip);
            foreach (var e in list)
            {
                var ch = new List<Bt_Link>();
                for (var o = e.m.parent; o != null; o = o.parent) { ch.Add(new Bt_Link { o = o, s = RelMat(e.m, o) }); if (o == e.anchor) break; }
                e.chain = ch;
            }
            _bq.Add(new Bt_Queue { root = root, list = list, at = PerfNow() + delay });
        }
        public static void BatchCheck()
        {
            var t = PerfNow();
            for (int i = _bq.Count - 1; i >= 0; i--)
            {
                var q = _bq[i]; if (t < q.at) continue; _bq.RemoveAt(i);
                if (q.root == null) continue;
                if (q.root != World && q.root.parent == null) continue;
                BakeStatic(q);
            }
        }
        static bool InTree(Transform o, Transform root) { while (o != null) { if (o == root) return true; o = o.parent; } return false; }
        static int BakeStatic(Bt_Queue q)
        {
            var by = new List<KeyValuePair<Transform, Dictionary<string, Bt_Group>>>();
            Func<Transform, Dictionary<string, Bt_Group>> slot = a => { foreach (var kv in by) if (kv.Key == a) return kv.Value; var d = new Dictionary<string, Bt_Group>(); by.Add(new KeyValuePair<Transform, Dictionary<string, Bt_Group>>(a, d)); return d; };
            foreach (var e in q.list)
            {
                var m = e.m; if (m == null || !InTree(m, q.root)) continue; Transform anchor = null;
                for (int i = e.chain.Count - 1; i >= 0; i--)
                {
                    var c = e.chain[i]; if (c.o == null || !InTree(m, c.o)) continue; var r = RelMat(m, c.o); bool moved = false;
                    for (int k = 0; k < 16; k++) if (Math.Abs(r[k] - c.s[k]) > 1e-5) { moved = true; break; }
                    if (!moved) { anchor = c.o; break; }
                }
                if (anchor == null) continue;
                bool vis = true; for (var o = m; o != null && o != anchor; o = o.parent) if (!o.gameObject.activeSelf) vis = false;
                var mr = m.GetComponent<MeshRenderer>(); if (mr == null || !mr.enabled) vis = false;
                if (!vis) continue;
                var rel = RelMat(m, anchor); e.anchor = anchor;
                var a = slot(anchor); var mat = mr.sharedMaterial;
                bool cs = mr.shadowCastingMode != ShadowCastingMode.Off, rs = mr.receiveShadows;
                var key = mat.GetInstanceID() + (cs ? "s" : "") + (rs ? "r" : "");
                Bt_Group g; if (!a.TryGetValue(key, out g)) a[key] = g = new Bt_Group { mat = mat, cs = cs, rs = rs };
                g.items.Add(new Bt_Item { m = m, rel = rel, ol = m.childCount > 0 });
            }
            int saved = 0;
            foreach (var kv in by)
            {
                var anchor = kv.Key; var olItems = new List<Bt_Item>();
                foreach (var g in kv.Value.Values)
                {
                    if (g.items.Count < 2 && !g.items[0].ol) continue;
                    var mesh = BakeGeo(g.items, g.mat.mainTexture != null, false);
                    var go = new GameObject("baked"); go.layer = anchor.gameObject.layer; go.transform.SetParent(anchor, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = g.mat;
                    r.shadowCastingMode = g.cs ? ShadowCastingMode.On : ShadowCastingMode.Off; r.receiveShadows = g.rs; var bk = go.AddComponent<Bt_Baked>();
                    foreach (var it in g.items) { if (it.ol) olItems.Add(it); it.m.SetParent(Bt_Grave(), false); bk.removed.Add(it.m.gameObject); }
                    saved += g.items.Count - 1;
                }
                if (olItems.Count > 0)
                {
                    var o = new GameObject("baked-ol"); o.layer = anchor.gameObject.layer; o.transform.SetParent(anchor, false);
                    o.AddComponent<MeshFilter>().sharedMesh = BakeGeo(olItems, false, true); var r = o.AddComponent<MeshRenderer>(); r.sharedMaterial = OUT;
                    r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; o.AddComponent<Bt_Baked>();
                    saved += olItems.Count - 1;
                }
            }
            return saved;
        }
        // Parçaları üçgen üçgen (indekssiz) tek ağa yazar; ol=true ise dış çizgi normali (UV3 = onormal, yoksa normal) kullanılır
        static Mesh BakeGeo(List<Bt_Item> items, bool uv, bool ol)
        {
            int n = 0; foreach (var it in items) { var g = it.m.GetComponent<MeshFilter>().sharedMesh; n += g.triangles.Length; }
            var P = new List<Vector3>(n); var N = new List<Vector3>(n); var U = uv && !ol ? new List<Vector2>(n) : null;
            bool anyCol = false; foreach (var it in items) { var g = it.m.GetComponent<MeshFilter>().sharedMesh; if (g.colors != null && g.colors.Length > 0) anyCol = true; }
            var C = anyCol && !ol ? new List<Color>(n) : null;
            var tmp3 = new List<Vector3>();
            foreach (var it in items)
            {
                var g = it.m.GetComponent<MeshFilter>().sharedMesh; var pos = g.vertices; var idx = g.triangles;
                Vector3[] nor = g.normals; if (nor != null && nor.Length == 0) nor = null;
                if (ol) { tmp3.Clear(); g.GetUVs(3, tmp3); if (tmp3.Count == pos.Length) nor = tmp3.ToArray(); }
                var tuv = g.uv; if (tuv != null && tuv.Length == 0) tuv = null; var col = g.colors; if (col != null && col.Length == 0) col = null;
                var nm = it.rel.inverse.transpose; bool flip = it.rel.determinant < 0;
                for (int j = 0; j < idx.Length; j++)
                {
                    int jj = flip ? (j - j % 3) + new[] { 0, 2, 1 }[j % 3] : j, v = idx[jj];
                    P.Add(it.rel.MultiplyPoint3x4(pos[v]));
                    N.Add(nor != null ? nm.MultiplyVector(nor[v]).normalized : Vector3.up);
                    if (U != null) U.Add(tuv != null ? tuv[v] : Vector2.zero);
                    if (C != null) C.Add(col != null ? col[v] : Color.white);
                }
            }
            var geo = new Mesh { name = ol ? "baked-ol" : "baked" };
            if (n > 65535) geo.indexFormat = IndexFormat.UInt32;
            geo.SetVertices(P); geo.SetNormals(N); if (U != null) geo.SetUVs(0, U); if (C != null) geo.SetColors(C);
            if (ol) geo.SetUVs(3, N);   // dış çizgi gölgelendiricisi onormal'i UV3'ten okur
            var tri = new int[n]; for (int i = 0; i < n; i++) tri[i] = i; geo.SetTriangles(tri, 0);
            geo.RecalculateBounds();
            return geo;
        }
        public static void DisposeBaked(Transform root)
        {
            if (root == null) return;
            foreach (var b in root.GetComponentsInChildren<Bt_Baked>(true))
            {
                var mf = b.GetComponent<MeshFilter>(); if (mf != null && mf.sharedMesh != null) UnityEngine.Object.Destroy(mf.sharedMesh);
                foreach (var g in b.removed) if (g != null) UnityEngine.Object.Destroy(g);
                b.removed.Clear();
            }
        }
    }
}
