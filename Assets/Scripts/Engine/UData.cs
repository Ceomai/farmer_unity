// THREE.Object3D.userData karşılığı (sahibi 06-engine). Başka modüller ihtiyaç duydukları alanları kendi dosyalarında
// "public partial class UData" ile ekler. Burada yalnızca 06-engine'in kullandığı/yazdığı alanlar ve PORTING.md'deki örnek alanlar var.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ciftlik
{
    public partial class UData : MonoBehaviour
    {
        // PORTING.md örneği (seçim/etiket bilgisi)
        public string kind, loc, unit;
        public int plot;

        // addFx: her kare (dt, t) ile çağrılan canlandırma işlevleri (07-world toplar, 25-desk worldTick çalıştırır)
        public List<Action<float, float>> fx;

        // hayvan / insan parçaları
        public Transform head, tail;
        public List<Transform> legs, arms;
        public float headRest;
        public float hop;                  // 11-input / 33-loop: sevilen hayvanın zıplaması (>0 iken dolaşma durur)

        // wanderFx durumu
        public float tx, tz, wait;
        public float? bs;                  // taban ölçek

        // ev bacası (Three koordinatı [x,y,z])
        public float[] chimney;

        // ribbon(): şeridin eğrisi
        public CatmullRomCurve3 curve;

        // toz bulutu (puff)
        public Vector3 vel;                // Three koordinatı
        public float life, sz;

        // Three Euler (XYZ, radyan) önbelleği — tek eksenli dönüş canlandırmalarında diğer eksenler korunur
        [NonSerialized] public Vector3 rot3;
        [NonSerialized] public Quaternion rot3q;
        [NonSerialized] public bool rot3ok;

        public static UData Get(Component c) => c == null ? null : c.GetComponent<UData>();
        public static UData Of(Component c)
        {
            if (c == null) return null;
            var u = c.GetComponent<UData>();
            if (u == null) u = c.gameObject.AddComponent<UData>();
            return u;
        }
    }
}
