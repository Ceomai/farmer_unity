// Gerçek Unity API'leri (taslakta eksik olanlar) — fix2
namespace UnityEngine
{
    public partial struct Matrix4x4
    {
        public static Matrix4x4 Perspective(float fov, float aspect, float zNear, float zFar) => default(Matrix4x4);
    }
}
