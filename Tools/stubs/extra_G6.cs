// G6 (arayüz) için eksik gerçek Unity API'leri (taslak). Hepsi UnityEngine.UI / UnityEngine'de gerçekten var.
using System.Collections.Generic;
namespace UnityEngine.UI
{
    public partial class LayoutGroup
    {
        protected List<RectTransform> rectChildren => null;
        protected RectTransform rectTransform => null;
        public virtual void CalculateLayoutInputHorizontal() { }
        public virtual void CalculateLayoutInputVertical() { }
        public virtual void SetLayoutHorizontal() { }
        public virtual void SetLayoutVertical() { }
        protected void SetLayoutInputForAxis(float totalMin, float totalPreferred, float totalFlexible, int axis) { }
        protected void SetChildAlongAxis(RectTransform rect, int axis, float pos, float size) { }
        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }
        protected void SetDirty() { }
    }
    public static partial class LayoutUtility
    {
        public static float GetMinSize(RectTransform r, int axis) => 0;
        public static float GetPreferredSize(RectTransform r, int axis) => 0;
        public static float GetFlexibleSize(RectTransform r, int axis) => 0;
    }
}
namespace UnityEngine
{
    public partial class Mesh
    {
        public void GetUVs(int channel, System.Collections.Generic.List<Vector3> uvs) { }
        public void GetUVs(int channel, System.Collections.Generic.List<Vector2> uvs) { }
    }
    public partial struct Matrix4x4
    {
        public Matrix4x4 transpose => this;
        public float determinant => 1;
        public float this[int index] { get => 0; set { } }
    }
}
