// Three.js → Unity dönüşümü.
// Three.js sağ elli (z ekrana doğru), Unity sol elli (z ileri). Kural: Three (x,y,z) → Unity (x,y,-z).
// Bu yansıma dönüşleri de değiştirir: x ve y eksenindeki açılar ters işaret alır, z ekseni aynı kalır.
// Three Euler sırası 'XYZ' (matris = Rx·Ry·Rz); ThreeRot bunu birebir uygular. Açılar RADYAN (JS ile aynı).
using UnityEngine;

namespace Ciftlik
{
    public static partial class Game
    {
        public static Vector3 V(double x, double y, double z) => new Vector3((float)x, (float)y, -(float)z);
        public static Quaternion ThreeRot(double x, double y, double z)
        {
            const float R = Mathf.Rad2Deg;
            return Quaternion.AngleAxis(-(float)x * R, Vector3.right) * Quaternion.AngleAxis(-(float)y * R, Vector3.up) * Quaternion.AngleAxis((float)z * R, Vector3.forward);
        }
        public static void SetPos(Transform t, double x, double y, double z) => t.localPosition = V(x, y, z);
        public static void SetRot(Transform t, double x, double y, double z) => t.localRotation = ThreeRot(x, y, z);
        public static void SetRotY(Transform t, double y) => t.localRotation = Quaternion.AngleAxis(-(float)y * Mathf.Rad2Deg, Vector3.up);
        // Unity konumu → Three koordinatı (kayıt/mantık JS koordinatlarıyla çalışır)
        public static Vector3 ToThree(Vector3 u) => new Vector3(u.x, u.y, -u.z);
        public static Color Hex3(int hex) => new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f);
    }
}

namespace Ciftlik
{
    // Editörde GetComponent<T>() bileşen yoksa "sahte null" nesne döndürür; C#'ın ?? işleci bunu null saymaz.
    // Bu yüzden "GetComponent<T>() ?? AddComponent<T>()" yerine bu yardımcı kullanılır.
    public static class UnityExt
    {
        public static T GetOrAdd<T>(this GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }
    }
}
