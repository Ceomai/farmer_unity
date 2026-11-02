// YALNIZCA DERLEME DENETİMİ İÇİN: Unity Editor kurulu olmayan makinede C# kodunun türlerini denetlemek üzere
// UnityEngine API'sinin kullanılan kısmının boş taslakları. Unity projesine DAHİL DEĞİLDİR (Assets dışında).
// Gerçek Unity API'sinde olmayan üye EKLEMEYİN. Eksik üye eklenecekse partial sınıflara ayrı dosyada eklenebilir.
#pragma warning disable 0067, 0649, 0414, 0169
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public partial class Object
    {
        public string name { get; set; }
        public HideFlags hideFlags { get; set; }
        public static void Destroy(Object o) { }
        public static void Destroy(Object o, float t) { }
        public static void DestroyImmediate(Object o) { }
        public static void DontDestroyOnLoad(Object o) { }
        public static T Instantiate<T>(T o) where T : Object => o;
        public static T Instantiate<T>(T o, Transform parent) where T : Object => o;
        public static T Instantiate<T>(T o, Vector3 p, Quaternion r) where T : Object => o;
        public static T FindObjectOfType<T>() where T : Object => null;
        public static T[] FindObjectsOfType<T>() where T : Object => null;
        public static T FindFirstObjectByType<T>() where T : Object => null;
        public int GetInstanceID() => 0;
        public static implicit operator bool(Object o) => o != null;
    }
    [Flags] public enum HideFlags { None = 0, HideInHierarchy = 1, DontSave = 52, HideAndDontSave = 61 }
    public partial class GameObject : Object
    {
        public GameObject() { } public GameObject(string n) { } public GameObject(string n, params Type[] c) { }
        public Transform transform => null; public int layer { get; set; } public string tag { get; set; }
        public bool activeSelf => true; public bool activeInHierarchy => true; public bool isStatic { get; set; }
        public void SetActive(bool v) { }
        public T AddComponent<T>() where T : Component => null; public Component AddComponent(Type t) => null;
        public T GetComponent<T>() => default(T); public T GetComponentInChildren<T>() => default(T); public T[] GetComponentsInChildren<T>() => null; public T[] GetComponentsInChildren<T>(bool i) => null;
        public T GetComponentInParent<T>() => default(T); public bool TryGetComponent<T>(out T c) { c = default(T); return false; }
        public static GameObject CreatePrimitive(PrimitiveType t) => null; public static GameObject Find(string n) => null;
        public void SendMessage(string m) { }
    }
    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public partial class Component : Object
    {
        public GameObject gameObject => null; public Transform transform => null; public string tag { get; set; }
        public T GetComponent<T>() => default(T); public Component GetComponent(Type t) => null; public T GetComponentInChildren<T>() => default(T); public T GetComponentInChildren<T>(bool i) => default(T);
        public T[] GetComponentsInChildren<T>() => null; public T[] GetComponentsInChildren<T>(bool i) => null; public T GetComponentInParent<T>() => default(T); public T[] GetComponents<T>() => null;
        public bool TryGetComponent<T>(out T c) { c = default(T); return false; }
        public bool CompareTag(string t) => false;
    }
    public partial class Behaviour : Component { public bool enabled { get; set; } public bool isActiveAndEnabled => true; }
    public partial class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator r) => null; public void StopCoroutine(Coroutine c) { } public void StopCoroutine(IEnumerator c) { } public void StopAllCoroutines() { }
        public void Invoke(string m, float t) { } public void CancelInvoke() { }
        public static void print(object o) { }
    }
    public sealed class Coroutine : YieldInstruction { }
    public class YieldInstruction { }
    public sealed class WaitForSeconds : YieldInstruction { public WaitForSeconds(float s) { } }
    public class WaitForSecondsRealtime : CustomYieldInstruction { public WaitForSecondsRealtime(float s) { } public override bool keepWaiting => false; }
    public sealed class WaitForEndOfFrame : YieldInstruction { }
    public abstract class CustomYieldInstruction : IEnumerator { public abstract bool keepWaiting { get; } public object Current => null; public bool MoveNext() => keepWaiting; public void Reset() { } }
    public class AsyncOperation : YieldInstruction { public bool isDone => true; public float progress => 1; public event Action<AsyncOperation> completed; }

    public partial class Transform : Component, IEnumerable
    {
        public Vector3 position { get; set; } public Vector3 localPosition { get; set; } public Quaternion rotation { get; set; } public Quaternion localRotation { get; set; }
        public Vector3 eulerAngles { get; set; } public Vector3 localEulerAngles { get; set; } public Vector3 localScale { get; set; } public Vector3 lossyScale => default(Vector3);
        public Transform parent { get; set; } public Transform root => null; public int childCount => 0;
        public Vector3 forward { get; set; } public Vector3 right { get; set; } public Vector3 up { get; set; }
        public Matrix4x4 localToWorldMatrix => default(Matrix4x4); public Matrix4x4 worldToLocalMatrix => default(Matrix4x4);
        public void SetParent(Transform p) { } public void SetParent(Transform p, bool w) { }
        public Transform GetChild(int i) => null; public Transform Find(string n) => null; public int GetSiblingIndex() => 0;
        public void SetSiblingIndex(int i) { } public void SetAsFirstSibling() { } public void SetAsLastSibling() { }
        public void LookAt(Vector3 p) { } public void LookAt(Vector3 p, Vector3 up) { } public void LookAt(Transform t) { }
        public void Rotate(Vector3 e) { } public void Rotate(float x, float y, float z) { } public void Rotate(Vector3 axis, float a) { } public void Rotate(Vector3 e, Space s) { } public void RotateAround(Vector3 p, Vector3 axis, float a) { }
        public void Translate(Vector3 v) { } public void Translate(Vector3 v, Space s) { }
        public void SetPositionAndRotation(Vector3 p, Quaternion r) { } public void SetLocalPositionAndRotation(Vector3 p, Quaternion r) { }
        public Vector3 TransformPoint(Vector3 p) => p; public Vector3 InverseTransformPoint(Vector3 p) => p; public Vector3 TransformDirection(Vector3 p) => p; public Vector3 InverseTransformDirection(Vector3 p) => p; public Vector3 TransformVector(Vector3 p) => p;
        public bool IsChildOf(Transform t) => false; public void DetachChildren() { }
        public IEnumerator GetEnumerator() => null;
    }
    public enum Space { World, Self }
    public sealed partial class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; } public Vector2 anchorMax { get; set; } public Vector2 pivot { get; set; } public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; } public Vector2 offsetMin { get; set; } public Vector2 offsetMax { get; set; } public Rect rect => default(Rect);
        public Vector3 anchoredPosition3D { get; set; }
        public void SetSizeWithCurrentAnchors(Axis a, float s) { } public void GetWorldCorners(Vector3[] c) { } public void GetLocalCorners(Vector3[] c) { }
        public void SetInsetAndSizeFromParentEdge(Edge e, float i, float s) { }
        public enum Axis { Horizontal, Vertical } public enum Edge { Left, Right, Top, Bottom }
    }
    public static class RectTransformUtility
    {
        public static bool ScreenPointToLocalPointInRectangle(RectTransform r, Vector2 sp, Camera c, out Vector2 lp) { lp = default(Vector2); return false; }
        public static bool RectangleContainsScreenPoint(RectTransform r, Vector2 sp) => false; public static bool RectangleContainsScreenPoint(RectTransform r, Vector2 sp, Camera c) => false;
        public static Vector2 WorldToScreenPoint(Camera c, Vector3 w) => default(Vector2);
    }

    public partial struct Vector2
    {
        public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero, one, up, down, left, right;
        public float magnitude => 0; public float sqrMagnitude => 0; public Vector2 normalized => this;
        public float this[int i] { get => 0; set { } }
        public static float Distance(Vector2 a, Vector2 b) => 0; public static float Dot(Vector2 a, Vector2 b) => 0; public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a; public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, float t) => a;
        public static Vector2 MoveTowards(Vector2 a, Vector2 b, float d) => a; public static Vector2 Scale(Vector2 a, Vector2 b) => a; public static float Angle(Vector2 a, Vector2 b) => 0; public static float SignedAngle(Vector2 a, Vector2 b) => 0;
        public static Vector2 Min(Vector2 a, Vector2 b) => a; public static Vector2 Max(Vector2 a, Vector2 b) => a; public static Vector2 ClampMagnitude(Vector2 a, float m) => a; public void Normalize() { } public void Set(float x, float y) { }
        public static Vector2 operator +(Vector2 a, Vector2 b) => a; public static Vector2 operator -(Vector2 a, Vector2 b) => a; public static Vector2 operator -(Vector2 a) => a;
        public static Vector2 operator *(Vector2 a, float b) => a; public static Vector2 operator *(float b, Vector2 a) => a; public static Vector2 operator /(Vector2 a, float b) => a; public static Vector2 operator *(Vector2 a, Vector2 b) => a; public static Vector2 operator /(Vector2 a, Vector2 b) => a;
        public static bool operator ==(Vector2 a, Vector2 b) => true; public static bool operator !=(Vector2 a, Vector2 b) => false;
        public static implicit operator Vector3(Vector2 v) => default(Vector3); public static implicit operator Vector2(Vector3 v) => default(Vector2);
        public override bool Equals(object o) => true; public override int GetHashCode() => 0;
    }
    public partial struct Vector2Int { public int x, y; public Vector2Int(int x, int y) { this.x = x; this.y = y; } }
    public partial struct Vector3
    {
        public float x, y, z; public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; } public Vector3(float x, float y) { this.x = x; this.y = y; z = 0; }
        public static Vector3 zero, one, up, down, left, right, forward, back, positiveInfinity, negativeInfinity;
        public float magnitude => 0; public float sqrMagnitude => 0; public Vector3 normalized => this;
        public float this[int i] { get => 0; set { } }
        public static float Distance(Vector3 a, Vector3 b) => 0; public static float Dot(Vector3 a, Vector3 b) => 0; public static Vector3 Cross(Vector3 a, Vector3 b) => a;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a; public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) => a; public static Vector3 Slerp(Vector3 a, Vector3 b, float t) => a;
        public static Vector3 MoveTowards(Vector3 a, Vector3 b, float d) => a; public static Vector3 Scale(Vector3 a, Vector3 b) => a; public static Vector3 Normalize(Vector3 a) => a;
        public static Vector3 Min(Vector3 a, Vector3 b) => a; public static Vector3 Max(Vector3 a, Vector3 b) => a; public static float Angle(Vector3 a, Vector3 b) => 0; public static float SignedAngle(Vector3 a, Vector3 b, Vector3 ax) => 0;
        public static Vector3 ClampMagnitude(Vector3 a, float m) => a; public static Vector3 Project(Vector3 a, Vector3 n) => a; public static Vector3 ProjectOnPlane(Vector3 a, Vector3 n) => a; public static Vector3 Reflect(Vector3 a, Vector3 n) => a;
        public static Vector3 SmoothDamp(Vector3 c, Vector3 t, ref Vector3 v, float s) => c; public static Vector3 SmoothDamp(Vector3 c, Vector3 t, ref Vector3 v, float s, float m, float dt) => c;
        public static Vector3 RotateTowards(Vector3 a, Vector3 b, float r, float m) => a;
        public void Normalize() { } public void Set(float x, float y, float z) { } public void Scale(Vector3 s) { }
        public static Vector3 operator +(Vector3 a, Vector3 b) => a; public static Vector3 operator -(Vector3 a, Vector3 b) => a; public static Vector3 operator -(Vector3 a) => a;
        public static Vector3 operator *(Vector3 a, float b) => a; public static Vector3 operator *(float b, Vector3 a) => a; public static Vector3 operator /(Vector3 a, float b) => a;
        public static bool operator ==(Vector3 a, Vector3 b) => true; public static bool operator !=(Vector3 a, Vector3 b) => false;
        public override bool Equals(object o) => true; public override int GetHashCode() => 0;
    }
    public partial struct Vector3Int { public int x, y, z; public Vector3Int(int x, int y, int z) { this.x = x; this.y = y; this.z = z; } }
    public partial struct Vector4
    {
        public float x, y, z, w; public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Vector4 zero, one; public static implicit operator Vector4(Vector3 v) => default(Vector4); public static implicit operator Vector3(Vector4 v) => default(Vector3); public static implicit operator Vector4(Vector2 v) => default(Vector4);
        public static Vector4 operator *(Vector4 a, float b) => a; public static Vector4 operator +(Vector4 a, Vector4 b) => a; public static Vector4 Lerp(Vector4 a, Vector4 b, float t) => a;
    }
    public partial struct Quaternion
    {
        public float x, y, z, w; public Quaternion(float x, float y, float z, float w) : this() { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity; public Vector3 eulerAngles { get; set; }
        public static Quaternion Euler(float x, float y, float z) => identity; public static Quaternion Euler(Vector3 e) => identity; public static Quaternion AngleAxis(float a, Vector3 ax) => identity;
        public static Quaternion LookRotation(Vector3 f) => identity; public static Quaternion LookRotation(Vector3 f, Vector3 u) => identity; public static Quaternion FromToRotation(Vector3 a, Vector3 b) => identity;
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => a; public static Quaternion Lerp(Quaternion a, Quaternion b, float t) => a; public static Quaternion RotateTowards(Quaternion a, Quaternion b, float d) => a;
        public static Quaternion Inverse(Quaternion a) => a; public static float Angle(Quaternion a, Quaternion b) => 0;
        public static Quaternion operator *(Quaternion a, Quaternion b) => a; public static Vector3 operator *(Quaternion a, Vector3 b) => b;
    }
    public partial struct Matrix4x4
    {
        public static Matrix4x4 identity; public static Matrix4x4 TRS(Vector3 p, Quaternion r, Vector3 s) => identity; public static Matrix4x4 Translate(Vector3 v) => identity; public static Matrix4x4 Rotate(Quaternion q) => identity; public static Matrix4x4 Scale(Vector3 v) => identity;
        public Vector3 MultiplyPoint(Vector3 p) => p; public Vector3 MultiplyPoint3x4(Vector3 p) => p; public Vector3 MultiplyVector(Vector3 p) => p; public Matrix4x4 inverse => this;
        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b) => a; public static Vector4 operator *(Matrix4x4 a, Vector4 b) => b;
        public float this[int r, int c] { get => 0; set { } }
    }
    public partial struct Color
    {
        public float r, g, b, a; public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1; } public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white, black, red, green, blue, yellow, cyan, magenta, gray, grey, clear;
        public float grayscale => 0; public Color linear => this; public Color gamma => this; public float maxColorComponent => 0;
        public static Color Lerp(Color a, Color b, float t) => a; public static Color LerpUnclamped(Color a, Color b, float t) => a; public static Color HSVToRGB(float h, float s, float v) => white; public static void RGBToHSV(Color c, out float h, out float s, out float v) { h = s = v = 0; }
        public static Color operator *(Color a, float b) => a; public static Color operator *(float b, Color a) => a; public static Color operator *(Color a, Color b) => a; public static Color operator +(Color a, Color b) => a; public static Color operator -(Color a, Color b) => a; public static Color operator /(Color a, float b) => a;
        public static bool operator ==(Color a, Color b) => true; public static bool operator !=(Color a, Color b) => false;
        public static implicit operator Color(Color32 c) => white; public static implicit operator Vector4(Color c) => default(Vector4); public static implicit operator Color(Vector4 c) => white;
        public float this[int i] { get => 0; set { } }
        public override bool Equals(object o) => true; public override int GetHashCode() => 0;
    }
    public partial struct Color32 { public byte r, g, b, a; public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; } public static implicit operator Color32(Color c) => default(Color32); public static Color32 Lerp(Color32 a, Color32 b, float t) => a; }
    public static class ColorUtility { public static bool TryParseHtmlString(string s, out Color c) { c = default(Color); return true; } public static string ToHtmlStringRGB(Color c) => ""; public static string ToHtmlStringRGBA(Color c) => ""; }
    public partial struct Rect
    {
        public Rect(float x, float y, float w, float h) : this() { this.x = x; this.y = y; width = w; height = h; } public Rect(Vector2 p, Vector2 s) : this() { x = y = width = height = 0; }
        public float x, y, width, height; public float xMin { get; set; } public float yMin { get; set; } public float xMax { get; set; } public float yMax { get; set; }
        public Vector2 center { get; set; } public Vector2 size { get; set; } public Vector2 position { get; set; } public Vector2 min { get; set; } public Vector2 max { get; set; }
        public bool Contains(Vector2 p) => false; public bool Contains(Vector3 p) => false; public bool Overlaps(Rect r) => false; public static Rect zero;
    }
    public partial struct RectInt { public int x, y, width, height; public RectInt(int x, int y, int w, int h) { this.x = x; this.y = y; width = w; height = h; } }
    public partial struct Bounds
    {
        public Bounds(Vector3 c, Vector3 s) : this() { } public Vector3 center { get; set; } public Vector3 size { get; set; } public Vector3 extents { get; set; } public Vector3 min { get; set; } public Vector3 max { get; set; }
        public void Encapsulate(Vector3 p) { } public void Encapsulate(Bounds b) { } public bool Contains(Vector3 p) => false; public void Expand(float a) { } public bool IntersectRay(Ray r) => false; public bool IntersectRay(Ray r, out float d) { d = 0; return false; } public Vector3 ClosestPoint(Vector3 p) => p;
    }
    public partial struct Ray { public Ray(Vector3 o, Vector3 d) : this() { } public Vector3 origin { get; set; } public Vector3 direction { get; set; } public Vector3 GetPoint(float d) => default(Vector3); }
    public partial struct Plane { public Plane(Vector3 n, Vector3 p) : this() { } public Plane(Vector3 n, float d) : this() { } public Plane(Vector3 a, Vector3 b, Vector3 c) : this() { } public bool Raycast(Ray r, out float e) { e = 0; return false; } public Vector3 normal { get; set; } public float distance { get; set; } public float GetDistanceToPoint(Vector3 p) => 0; public Vector3 ClosestPointOnPlane(Vector3 p) => p; }
    public partial struct RaycastHit { public Vector3 point { get; set; } public Vector3 normal { get; set; } public float distance { get; set; } public Collider collider => null; public Transform transform => null; public Rigidbody rigidbody => null; public int triangleIndex => 0; public Vector2 textureCoord => default(Vector2); }
    public static partial class Physics
    {
        public static bool Raycast(Ray r, out RaycastHit h) { h = default(RaycastHit); return false; } public static bool Raycast(Ray r, out RaycastHit h, float d) { h = default(RaycastHit); return false; }
        public static bool Raycast(Ray r, out RaycastHit h, float d, int mask) { h = default(RaycastHit); return false; } public static bool Raycast(Vector3 o, Vector3 d, out RaycastHit h, float m) { h = default(RaycastHit); return false; }
        public static RaycastHit[] RaycastAll(Ray r) => null; public static RaycastHit[] RaycastAll(Ray r, float d) => null; public static RaycastHit[] RaycastAll(Ray r, float d, int m) => null;
        public static bool autoSyncTransforms { get; set; } public static void SyncTransforms() { } public static Vector3 gravity { get; set; }
        public static Collider[] OverlapSphere(Vector3 p, float r) => null;
    }
    public partial class Collider : Component { public bool enabled { get; set; } public bool isTrigger { get; set; } public Bounds bounds => default(Bounds); public bool Raycast(Ray r, out RaycastHit h, float d) { h = default(RaycastHit); return false; } }
    public partial class BoxCollider : Collider { public Vector3 center { get; set; } public Vector3 size { get; set; } }
    public partial class SphereCollider : Collider { public Vector3 center { get; set; } public float radius { get; set; } }
    public partial class CapsuleCollider : Collider { public Vector3 center { get; set; } public float radius { get; set; } public float height { get; set; } public int direction { get; set; } }
    public partial class MeshCollider : Collider { public Mesh sharedMesh { get; set; } public bool convex { get; set; } }
    public partial class Rigidbody : Component { public bool isKinematic { get; set; } public bool useGravity { get; set; } public Vector3 velocity { get; set; } }
    public static partial class LayerMask { public static int GetMask(params string[] n) => 0; public static int NameToLayer(string n) => 0; }

    public static partial class Mathf
    {
        public const float PI = 3.14159265f, Infinity = float.PositiveInfinity, NegativeInfinity = float.NegativeInfinity, Deg2Rad = 0.0174533f, Rad2Deg = 57.29578f, Epsilon = 1.401298E-45f;
        public static float Sin(float f) => 0; public static float Cos(float f) => 0; public static float Tan(float f) => 0; public static float Asin(float f) => 0; public static float Acos(float f) => 0; public static float Atan(float f) => 0; public static float Atan2(float y, float x) => 0;
        public static float Sqrt(float f) => 0; public static float Abs(float f) => 0; public static int Abs(int f) => 0; public static float Min(float a, float b) => 0; public static float Min(params float[] a) => 0; public static int Min(int a, int b) => 0; public static int Min(params int[] a) => 0;
        public static float Max(float a, float b) => 0; public static float Max(params float[] a) => 0; public static int Max(int a, int b) => 0; public static int Max(params int[] a) => 0;
        public static float Pow(float a, float b) => 0; public static float Exp(float a) => 0; public static float Log(float a) => 0; public static float Log(float a, float b) => 0; public static float Log10(float a) => 0;
        public static float Ceil(float a) => 0; public static float Floor(float a) => 0; public static float Round(float a) => 0; public static int CeilToInt(float a) => 0; public static int FloorToInt(float a) => 0; public static int RoundToInt(float a) => 0;
        public static float Sign(float a) => 0; public static float Clamp(float v, float a, float b) => 0; public static int Clamp(int v, int a, int b) => 0; public static float Clamp01(float v) => 0;
        public static float Lerp(float a, float b, float t) => 0; public static float LerpUnclamped(float a, float b, float t) => 0; public static float LerpAngle(float a, float b, float t) => 0; public static float InverseLerp(float a, float b, float v) => 0;
        public static float MoveTowards(float a, float b, float d) => 0; public static float MoveTowardsAngle(float a, float b, float d) => 0; public static float SmoothStep(float a, float b, float t) => 0; public static float DeltaAngle(float a, float b) => 0;
        public static float SmoothDamp(float c, float t, ref float v, float s) => 0; public static float SmoothDamp(float c, float t, ref float v, float s, float m, float dt) => 0; public static float SmoothDampAngle(float c, float t, ref float v, float s) => 0;
        public static float Repeat(float t, float l) => 0; public static float PingPong(float t, float l) => 0; public static bool Approximately(float a, float b) => false; public static float PerlinNoise(float x, float y) => 0;
        public static int NextPowerOfTwo(int v) => 0; public static bool IsPowerOfTwo(int v) => false; public static float GammaToLinearSpace(float v) => 0; public static float LinearToGammaSpace(float v) => 0;
    }
    public static partial class Random
    {
        public static float value => 0; public static float Range(float a, float b) => 0; public static int Range(int a, int b) => 0; public static void InitState(int s) { }
        public static Vector3 insideUnitSphere => default(Vector3); public static Vector2 insideUnitCircle => default(Vector2); public static Vector3 onUnitSphere => default(Vector3); public static Quaternion rotation => default(Quaternion);
        public static Color ColorHSV() => default(Color); public static Color ColorHSV(float a, float b, float c, float d, float e, float f) => default(Color);
    }
    public static partial class Time
    {
        public static float time => 0; public static float deltaTime => 0; public static float unscaledDeltaTime => 0; public static float unscaledTime => 0; public static float realtimeSinceStartup => 0; public static double realtimeSinceStartupAsDouble => 0;
        public static double timeAsDouble => 0; public static float timeScale { get; set; } public static int frameCount => 0; public static float smoothDeltaTime => 0; public static float fixedDeltaTime { get; set; } public static float maximumDeltaTime { get; set; }
    }
    public static partial class Debug
    {
        public static void Log(object o) { } public static void LogWarning(object o) { } public static void LogError(object o) { } public static void LogException(Exception e) { } public static void LogFormat(string f, params object[] a) { }
        public static void Assert(bool c) { } public static void DrawLine(Vector3 a, Vector3 b, Color c) { } public static void DrawRay(Vector3 a, Vector3 b, Color c) { } public static bool isDebugBuild => false;
    }
    public delegate void LogCallback(string condition, string stackTrace, LogType type);
    public enum LogType { Error, Assert, Warning, Log, Exception }
    public enum RuntimePlatform { OSXEditor, OSXPlayer, WindowsPlayer, WindowsEditor, IPhonePlayer, Android, LinuxPlayer, LinuxEditor, WebGLPlayer }
    public enum NetworkReachability { NotReachable, ReachableViaCarrierDataNetwork, ReachableViaLocalAreaNetwork }
    public enum SystemLanguage { Turkish, English, Unknown }
    public static partial class Application
    {
        public static bool isMobilePlatform => false; public static bool isEditor => false; public static bool isPlaying => true; public static bool isFocused => true; public static RuntimePlatform platform => RuntimePlatform.Android;
        public static int targetFrameRate { get; set; } public static string persistentDataPath => ""; public static string temporaryCachePath => ""; public static string dataPath => ""; public static string streamingAssetsPath => "";
        public static string version => ""; public static string identifier => ""; public static string productName => ""; public static string companyName => ""; public static string unityVersion => "";
        public static bool runInBackground { get; set; } public static NetworkReachability internetReachability => NetworkReachability.ReachableViaLocalAreaNetwork; public static SystemLanguage systemLanguage => SystemLanguage.Turkish;
        public static void OpenURL(string u) { } public static void Quit() { } public static event LogCallback logMessageReceived; public static event Action<bool> focusChanged; public static event Action quitting; public static event Action lowMemory;
        public static bool genuine => true;
    }
    public enum ScreenOrientation { Portrait = 1, PortraitUpsideDown = 2, LandscapeLeft = 3, LandscapeRight = 4, AutoRotation = 5 }
    public partial struct Resolution { public int width, height; }
    public static partial class Screen
    {
        public static int width => 0; public static int height => 0; public static float dpi => 0; public static Rect safeArea => default(Rect); public static ScreenOrientation orientation { get; set; }
        public static bool fullScreen { get; set; } public static int sleepTimeout { get; set; } public static Resolution currentResolution => default(Resolution); public static void SetResolution(int w, int h, bool f) { }
        public static bool autorotateToPortrait { get; set; } public static bool autorotateToLandscapeLeft { get; set; } public static bool autorotateToLandscapeRight { get; set; } public static bool autorotateToPortraitUpsideDown { get; set; }
    }
    public static class SleepTimeout { public const int NeverSleep = -1, SystemSetting = -2; }
    public static partial class SystemInfo
    {
        public static int systemMemorySize => 0; public static int processorCount => 0; public static int graphicsMemorySize => 0; public static string deviceModel => ""; public static string deviceUniqueIdentifier => ""; public static string operatingSystem => "";
        public static string graphicsDeviceName => ""; public static bool supportsShadows => true; public static int maxTextureSize => 0; public static DeviceType deviceType => DeviceType.Handheld; public static bool supportsVibration => true; public static int graphicsShaderLevel => 0;
        public static bool SupportsRenderTextureFormat(RenderTextureFormat f) => true;
    }
    public enum DeviceType { Unknown, Handheld, Console, Desktop }
    public static partial class Handheld { public static void Vibrate() { } }
    public static partial class PlayerPrefs
    {
        public static bool HasKey(string k) => false; public static string GetString(string k) => ""; public static string GetString(string k, string d) => d; public static void SetString(string k, string v) { }
        public static int GetInt(string k, int d = 0) => d; public static void SetInt(string k, int v) { } public static float GetFloat(string k, float d = 0) => d; public static void SetFloat(string k, float v) { }
        public static void DeleteKey(string k) { } public static void DeleteAll() { } public static void Save() { }
    }
    public enum TouchPhase { Began, Moved, Stationary, Ended, Canceled }
    public partial struct Touch { public int fingerId { get; set; } public Vector2 position { get; set; } public Vector2 deltaPosition { get; set; } public TouchPhase phase { get; set; } public int tapCount { get; set; } public float deltaTime { get; set; } }
    public enum KeyCode { None, Backspace, Tab, Return, Escape, Space, Alpha0, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9, A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z, UpArrow, DownArrow, RightArrow, LeftArrow, Plus, Minus, Equals, KeypadPlus, KeypadMinus, LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt, F1, F2, F3, F4, F5, F10, F11, F12, Delete, Home, End, PageUp, PageDown, Mouse0, Mouse1, Mouse2 }
    public static partial class Input
    {
        public static Vector3 mousePosition => default(Vector3); public static Vector2 mouseScrollDelta => default(Vector2); public static int touchCount => 0; public static Touch[] touches => null; public static Touch GetTouch(int i) => default(Touch);
        public static bool GetMouseButton(int b) => false; public static bool GetMouseButtonDown(int b) => false; public static bool GetMouseButtonUp(int b) => false;
        public static bool GetKey(KeyCode k) => false; public static bool GetKeyDown(KeyCode k) => false; public static bool GetKeyUp(KeyCode k) => false; public static bool anyKeyDown => false; public static bool anyKey => false;
        public static float GetAxis(string a) => 0; public static bool multiTouchEnabled { get; set; } public static bool touchSupported => true; public static bool mousePresent => true; public static string inputString => "";
        public static Vector3 acceleration => default(Vector3); public static bool simulateMouseWithTouches { get; set; }
    }

    // ---------- Çizim ----------
    public enum CameraClearFlags { Skybox = 1, Color = 2, SolidColor = 2, Depth = 3, Nothing = 4 }
    public enum DepthTextureMode { None = 0, Depth = 1, DepthNormals = 2 }
    public partial class Camera : Behaviour
    {
        public static Camera main => null; public static Camera current => null; public static Camera[] allCameras => null;
        public float fieldOfView { get; set; } public float nearClipPlane { get; set; } public float farClipPlane { get; set; } public bool orthographic { get; set; } public float orthographicSize { get; set; } public float aspect { get; set; }
        public CameraClearFlags clearFlags { get; set; } public Color backgroundColor { get; set; } public int cullingMask { get; set; } public float depth { get; set; } public RenderTexture targetTexture { get; set; } public Rect rect { get; set; } public Rect pixelRect { get; set; }
        public int pixelWidth => 0; public int pixelHeight => 0; public bool allowHDR { get; set; } public bool allowMSAA { get; set; } public DepthTextureMode depthTextureMode { get; set; } public bool useOcclusionCulling { get; set; }
        public Matrix4x4 worldToCameraMatrix => default(Matrix4x4); public Matrix4x4 projectionMatrix { get; set; }
        public Ray ScreenPointToRay(Vector3 p) => default(Ray); public Vector3 WorldToScreenPoint(Vector3 p) => p; public Vector3 WorldToViewportPoint(Vector3 p) => p; public Vector3 ScreenToWorldPoint(Vector3 p) => p; public Vector3 ViewportToWorldPoint(Vector3 p) => p; public Ray ViewportPointToRay(Vector3 p) => default(Ray); public Vector3 ScreenToViewportPoint(Vector3 p) => p; public Vector3 ViewportToScreenPoint(Vector3 p) => p;
        public void Render() { } public void ResetProjectionMatrix() { }
    }
    public enum LightType { Spot, Directional, Point, Area }
    public enum LightShadows { None, Hard, Soft }
    public enum LightRenderMode { Auto, ForcePixel, ForceVertex }
    public partial class Light : Behaviour
    {
        public LightType type { get; set; } public Color color { get; set; } public float intensity { get; set; } public float range { get; set; } public float spotAngle { get; set; } public LightShadows shadows { get; set; }
        public float shadowStrength { get; set; } public float shadowBias { get; set; } public float shadowNormalBias { get; set; } public int shadowCustomResolution { get; set; } public UnityEngine.Rendering.LightShadowResolution shadowResolution { get; set; } public LightRenderMode renderMode { get; set; } public int cullingMask { get; set; } public float bounceIntensity { get; set; }
    }
    public partial class Renderer : Component
    {
        public bool enabled { get; set; } public Material material { get; set; } public Material sharedMaterial { get; set; } public Material[] materials { get; set; } public Material[] sharedMaterials { get; set; }
        public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode { get; set; } public bool receiveShadows { get; set; } public Bounds bounds => default(Bounds); public int sortingOrder { get; set; } public bool isVisible => true;
        public void SetPropertyBlock(MaterialPropertyBlock b) { } public void GetPropertyBlock(MaterialPropertyBlock b) { } public void SetPropertyBlock(MaterialPropertyBlock b, int i) { } public int sortingLayerID { get; set; } public string sortingLayerName { get; set; }
    }
    public partial class MeshRenderer : Renderer { }
    public partial class SkinnedMeshRenderer : Renderer { public Mesh sharedMesh { get; set; } }
    public partial class LineRenderer : Renderer { public int positionCount { get; set; } public float startWidth { get; set; } public float endWidth { get; set; } public float widthMultiplier { get; set; } public Color startColor { get; set; } public Color endColor { get; set; } public bool useWorldSpace { get; set; } public bool loop { get; set; } public void SetPosition(int i, Vector3 p) { } public void SetPositions(Vector3[] p) { } public int numCapVertices { get; set; } public int numCornerVertices { get; set; } }
    public partial class TrailRenderer : Renderer { public float time { get; set; } public float startWidth { get; set; } public float endWidth { get; set; } public void Clear() { } }
    public partial class MeshFilter : Component { public Mesh mesh { get; set; } public Mesh sharedMesh { get; set; } }
    public partial class Mesh : Object
    {
        public Mesh() { } public Vector3[] vertices { get; set; } public Vector3[] normals { get; set; } public Vector2[] uv { get; set; } public Vector2[] uv2 { get; set; } public Color[] colors { get; set; } public Color32[] colors32 { get; set; } public int[] triangles { get; set; } public Vector4[] tangents { get; set; }
        public int vertexCount => 0; public int subMeshCount { get; set; } public Bounds bounds { get; set; } public UnityEngine.Rendering.IndexFormat indexFormat { get; set; } public bool isReadable => true;
        public void SetVertices(List<Vector3> v) { } public void SetNormals(List<Vector3> v) { } public void SetUVs(int c, List<Vector2> v) { } public void SetColors(List<Color> c) { } public void SetColors(List<Color32> c) { } public void SetTriangles(List<int> t, int s) { } public void SetTriangles(int[] t, int s) { } public void SetIndices(int[] i, MeshTopology t, int s) { } public void SetTriangles(List<int> t, int s, bool cb) { }
        public void RecalculateNormals() { } public void RecalculateBounds() { } public void RecalculateTangents() { } public void Clear() { } public void UploadMeshData(bool l) { } public void Optimize() { } public void MarkDynamic() { }
        public void CombineMeshes(CombineInstance[] c) { } public void CombineMeshes(CombineInstance[] c, bool merge) { } public void CombineMeshes(CombineInstance[] c, bool merge, bool useMatrices) { } public int[] GetTriangles(int s) => null; public void GetVertices(List<Vector3> v) { }
    }
    public enum MeshTopology { Triangles = 0, Quads = 2, Lines = 3, LineStrip = 4, Points = 5 }
    public partial struct CombineInstance { public Mesh mesh { get; set; } public Matrix4x4 transform { get; set; } public int subMeshIndex { get; set; } }
    public partial class Shader : Object
    {
        public static Shader Find(string n) => null; public static int PropertyToID(string n) => 0; public bool isSupported => true; public int renderQueue => 0;
        public static void SetGlobalColor(string n, Color c) { } public static void SetGlobalFloat(string n, float f) { } public static void SetGlobalVector(string n, Vector4 v) { } public static void SetGlobalTexture(string n, Texture t) { } public static void SetGlobalColor(int n, Color c) { } public static void SetGlobalFloat(int n, float f) { } public static void SetGlobalVector(int n, Vector4 v) { }
    }
    public partial class Material : Object
    {
        public Material(Shader s) { } public Material(Material m) { } public Shader shader { get; set; } public Color color { get; set; } public Texture mainTexture { get; set; } public int renderQueue { get; set; } public Vector2 mainTextureScale { get; set; } public Vector2 mainTextureOffset { get; set; } public bool enableInstancing { get; set; } public string[] shaderKeywords { get; set; }
        public void SetColor(string n, Color c) { } public void SetColor(int n, Color c) { } public void SetFloat(string n, float v) { } public void SetFloat(int n, float v) { } public void SetInt(string n, int v) { } public void SetTexture(string n, Texture t) { } public void SetTexture(int n, Texture t) { } public void SetVector(string n, Vector4 v) { } public void SetVector(int n, Vector4 v) { } public void SetTextureScale(string n, Vector2 v) { } public void SetTextureOffset(string n, Vector2 v) { }
        public Color GetColor(string n) => default(Color); public float GetFloat(string n) => 0; public Texture GetTexture(string n) => null; public bool HasProperty(string n) => false; public bool HasProperty(int n) => false;
        public void EnableKeyword(string k) { } public void DisableKeyword(string k) { } public void SetOverrideTag(string t, string v) { } public void CopyPropertiesFromMaterial(Material m) { }
    }
    public sealed partial class MaterialPropertyBlock { public void SetColor(string n, Color c) { } public void SetColor(int n, Color c) { } public void SetFloat(string n, float f) { } public void SetFloat(int n, float f) { } public void SetVector(string n, Vector4 v) { } public void SetTexture(string n, Texture t) { } public void Clear() { } }
    public enum TextureFormat { Alpha8 = 1, RGB24 = 3, RGBA32 = 4, ARGB32 = 5, RGB565 = 7, R8 = 63, RGBAHalf = 17, RGBAFloat = 20 }
    public enum TextureWrapMode { Repeat, Clamp, Mirror, MirrorOnce }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum RenderTextureFormat { ARGB32 = 0, Depth = 1, ARGBHalf = 2, Shadowmap = 3, RGB565 = 4, Default = 7, R8 = 16 }
    public enum RenderTextureReadWrite { Default, Linear, sRGB }
    public partial class Texture : Object { public int width { get; set; } public int height { get; set; } public TextureWrapMode wrapMode { get; set; } public FilterMode filterMode { get; set; } public int anisoLevel { get; set; } public float mipMapBias { get; set; } public TextureWrapMode wrapModeU { get; set; } public TextureWrapMode wrapModeV { get; set; } }
    public partial class Texture2D : Texture
    {
        public Texture2D(int w, int h) { } public Texture2D(int w, int h, TextureFormat f, bool mip) { } public Texture2D(int w, int h, TextureFormat f, bool mip, bool linear) { }
        public static Texture2D whiteTexture => null; public static Texture2D blackTexture => null; public TextureFormat format => TextureFormat.RGBA32;
        public void SetPixel(int x, int y, Color c) { } public void SetPixels(Color[] c) { } public void SetPixels(int x, int y, int w, int h, Color[] c) { } public void SetPixels32(Color32[] c) { } public void SetPixels32(int x, int y, int w, int h, Color32[] c) { } public void SetPixelData<T>(T[] d, int mip, int off = 0) { }
        public Color GetPixel(int x, int y) => default(Color); public Color GetPixelBilinear(float u, float v) => default(Color); public Color[] GetPixels() => null; public Color32[] GetPixels32() => null; public byte[] GetRawTextureData() => null;
        public void Apply() { } public void Apply(bool mips) { } public void Apply(bool mips, bool nonReadable) { } public void ReadPixels(Rect r, int x, int y) { } public void ReadPixels(Rect r, int x, int y, bool m) { } public bool Reinitialize(int w, int h) => true; public void Compress(bool hq) { } public void LoadRawTextureData(byte[] d) { }
    }
    public static partial class ImageConversion { public static byte[] EncodeToPNG(this Texture2D t) => null; public static byte[] EncodeToJPG(this Texture2D t) => null; public static byte[] EncodeToJPG(this Texture2D t, int q) => null; public static bool LoadImage(this Texture2D t, byte[] d) => true; }
    public partial class RenderTexture : Texture
    {
        public RenderTexture(int w, int h, int d) { } public RenderTexture(int w, int h, int d, RenderTextureFormat f) { } public static RenderTexture active { get; set; } public int antiAliasing { get; set; } public bool useMipMap { get; set; } public int depth { get; set; }
        public bool Create() => true; public void Release() { } public bool IsCreated() => true; public static RenderTexture GetTemporary(int w, int h) => null; public static RenderTexture GetTemporary(int w, int h, int d) => null; public static void ReleaseTemporary(RenderTexture r) { } public void DiscardContents() { }
    }
    public partial class Sprite : Object
    {
        public static Sprite Create(Texture2D t, Rect r, Vector2 p) => null; public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu) => null; public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu, uint ex, SpriteMeshType m, Vector4 b) => null;
        public Texture2D texture => null; public Rect rect => default(Rect); public Vector4 border => default(Vector4); public float pixelsPerUnit => 100;
    }
    public enum SpriteMeshType { FullRect, Tight }
    public partial class Font : Object
    {
        public Font() { } public Font(string n) { } public static Font CreateDynamicFontFromOSFont(string n, int s) => null; public static Font CreateDynamicFontFromOSFont(string[] n, int s) => null; public static string[] GetOSInstalledFontNames() => null;
        public Material material { get; set; } public string[] fontNames { get; set; } public bool dynamic => true; public int fontSize => 0; public int lineHeight => 0;
        public void RequestCharactersInTexture(string c) { } public void RequestCharactersInTexture(string c, int s) { } public void RequestCharactersInTexture(string c, int s, FontStyle st) { } public bool GetCharacterInfo(char c, out CharacterInfo i) { i = default(CharacterInfo); return false; } public bool GetCharacterInfo(char c, out CharacterInfo i, int s) { i = default(CharacterInfo); return false; } public bool GetCharacterInfo(char c, out CharacterInfo i, int s, FontStyle st) { i = default(CharacterInfo); return false; } public bool HasCharacter(char c) => true;
        public static event Action<Font> textureRebuilt;
    }
    public partial struct CharacterInfo { public int advance { get; set; } public int glyphWidth { get; set; } public int glyphHeight { get; set; } public int minX { get; set; } public int maxX { get; set; } public int minY { get; set; } public int maxY { get; set; } public Vector2 uvBottomLeft { get; set; } public Vector2 uvBottomRight { get; set; } public Vector2 uvTopLeft { get; set; } public Vector2 uvTopRight { get; set; } }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum TextAlignment { Left, Center, Right }
    public partial class TextMesh : Component { public string text { get; set; } public Font font { get; set; } public int fontSize { get; set; } public float characterSize { get; set; } public float lineSpacing { get; set; } public TextAnchor anchor { get; set; } public TextAlignment alignment { get; set; } public Color color { get; set; } public FontStyle fontStyle { get; set; } public bool richText { get; set; } public float offsetZ { get; set; } public float tabSize { get; set; } }
    public static partial class Resources { public static T Load<T>(string p) where T : Object => null; public static Object Load(string p) => null; public static T GetBuiltinResource<T>(string p) where T : Object => null; public static AsyncOperation UnloadUnusedAssets() => null; public static T[] LoadAll<T>(string p) where T : Object => null; public static T[] FindObjectsOfTypeAll<T>() where T : Object => null; }
    public enum AmbientMode { Skybox = 0, Trilight = 1, Flat = 3, Custom = 4 }
    public enum FogMode { Linear = 1, Exponential = 2, ExponentialSquared = 3 }
    public static partial class RenderSettings
    {
        public static bool fog { get; set; } public static FogMode fogMode { get; set; } public static Color fogColor { get; set; } public static float fogDensity { get; set; } public static float fogStartDistance { get; set; } public static float fogEndDistance { get; set; }
        public static AmbientMode ambientMode { get; set; } public static Color ambientLight { get; set; } public static Color ambientSkyColor { get; set; } public static Color ambientEquatorColor { get; set; } public static Color ambientGroundColor { get; set; } public static float ambientIntensity { get; set; } public static Material skybox { get; set; } public static Light sun { get; set; }
    }
    public static partial class QualitySettings
    {
        public static int shadowResolution2 => 0; public static ShadowQuality shadows { get; set; } public static float shadowDistance { get; set; } public static int antiAliasing { get; set; } public static int vSyncCount { get; set; } public static int pixelLightCount { get; set; }
        public static UnityEngine.ShadowResolution shadowResolution { get; set; } public static int shadowCascades { get; set; } public static void SetQualityLevel(int i) { } public static float lodBias { get; set; } public static int masterTextureLimit { get; set; } public static int globalTextureMipmapLimit { get; set; }
        public static ShadowProjection shadowProjection { get; set; } public static float resolutionScalingFixedDPIFactor { get; set; }
    }
    public enum ShadowQuality { Disable, HardOnly, All }
    public enum ShadowResolution { Low, Medium, High, VeryHigh }
    public enum ShadowProjection { CloseFit, StableFit }
    public static partial class GL { public static void Clear(bool d, bool c, Color bg) { } public static void PushMatrix() { } public static void PopMatrix() { } public static void LoadOrtho() { } public static void Begin(int m) { } public static void End() { } public static void Vertex3(float x, float y, float z) { } public static void Color(Color c) { } public const int QUADS = 7, LINES = 1, TRIANGLES = 4; public static void LoadPixelMatrix() { } public static void TexCoord2(float x, float y) { } }
    public static partial class Graphics { public static void Blit(Texture s, RenderTexture d) { } public static void Blit(Texture s, RenderTexture d, Material m) { } public static void DrawTexture(Rect r, Texture t) { } public static void DrawMesh(Mesh m, Matrix4x4 x, Material mat, int l) { } public static void CopyTexture(Texture s, Texture d) { } public static void DrawMeshNow(Mesh m, Matrix4x4 x) { } }
    public static partial class ScreenCapture { public static Texture2D CaptureScreenshotAsTexture() => null; public static void CaptureScreenshot(string f) { } public static void CaptureScreenshotIntoRenderTexture(RenderTexture r) { } }
    public partial class ParticleSystem : Component { public void Play() { } public void Stop() { } public void Emit(int n) { } public bool isPlaying => false; public void Clear() { } }
    public partial class Animator : Behaviour { }
    public partial class LODGroup : Component { }
    public partial class Skybox : Behaviour { public Material material { get; set; } }

    // ---------- Ses ----------
    public partial class AudioClip : Object
    {
        public static AudioClip Create(string n, int len, int ch, int freq, bool stream) => null; public static AudioClip Create(string n, int len, int ch, int freq, bool stream, PCMReaderCallback r) => null;
        public bool SetData(float[] d, int off) => true; public bool GetData(float[] d, int off) => true; public float length => 0; public int samples => 0; public int channels => 1; public int frequency => 44100;
        public delegate void PCMReaderCallback(float[] data);
    }
    public partial class AudioSource : Behaviour
    {
        public AudioClip clip { get; set; } public float volume { get; set; } public float pitch { get; set; } public bool loop { get; set; } public bool playOnAwake { get; set; } public bool isPlaying => false; public float spatialBlend { get; set; } public float panStereo { get; set; } public bool mute { get; set; } public float time { get; set; } public int priority { get; set; } public bool ignoreListenerPause { get; set; } public bool bypassEffects { get; set; }
        public void Play() { } public void Play(ulong d) { } public void PlayDelayed(float d) { } public void PlayScheduled(double t) { } public void Stop() { } public void Pause() { } public void UnPause() { } public void PlayOneShot(AudioClip c) { } public void PlayOneShot(AudioClip c, float v) { } public void SetScheduledEndTime(double t) { }
    }
    public partial class AudioListener : Behaviour { public static float volume { get; set; } public static bool pause { get; set; } }
    public static partial class AudioSettings { public static double dspTime => 0; public static int outputSampleRate => 44100; }
    public partial class AudioLowPassFilter : Behaviour { public float cutoffFrequency { get; set; } }
    public partial class AudioReverbFilter : Behaviour { }

    // ---------- Öznitelikler ----------
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad, AfterAssembliesLoaded, BeforeSplashScreen, SubsystemRegistration }
    [AttributeUsage(AttributeTargets.Method)] public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute() { } public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }
    [AttributeUsage(AttributeTargets.Field)] public class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class HideInInspector : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class HeaderAttribute : Attribute { public HeaderAttribute(string h) { } }
    [AttributeUsage(AttributeTargets.Field)] public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    [AttributeUsage(AttributeTargets.Field)] public class TooltipAttribute : Attribute { public TooltipAttribute(string t) { } }
    [AttributeUsage(AttributeTargets.Class)] public class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)] public class RequireComponent : Attribute { public RequireComponent(Type t) { } }
    [AttributeUsage(AttributeTargets.Class)] public class ExecuteAlways : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int o) { } }
    public static partial class JsonUtility { public static string ToJson(object o) => ""; public static T FromJson<T>(string j) => default(T); }
    public partial class ScriptableObject : Object { public static T CreateInstance<T>() where T : ScriptableObject => null; }
    public partial struct Gradient { }
    public partial class AnimationCurve { public AnimationCurve(params Keyframe[] k) { } public float Evaluate(float t) => 0; public static AnimationCurve EaseInOut(float a, float b, float c, float d) => null; public static AnimationCurve Linear(float a, float b, float c, float d) => null; }
    public partial struct Keyframe { public Keyframe(float t, float v) { } }
}

namespace UnityEngine.Rendering
{
    public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }
    public enum LightShadowResolution { FromQualitySettings = -1, Low = 0, Medium = 1, High = 2, VeryHigh = 3 }
    public enum IndexFormat { UInt16, UInt32 }
    public enum CompareFunction { Disabled, Never, Less, Equal, LessEqual, Greater, NotEqual, GreaterEqual, Always }
    public enum CullMode { Off, Front, Back }
    public enum BlendMode { Zero, One, DstColor, SrcColor, OneMinusDstColor, SrcAlpha, OneMinusSrcColor, DstAlpha, OneMinusDstAlpha, SrcAlphaSaturate, OneMinusSrcAlpha }
    public static class GraphicsSettings { }
}

namespace UnityEngine.Networking
{
    public partial class UnityWebRequest : IDisposable
    {
        public enum Result { InProgress, Success, ConnectionError, ProtocolError, DataProcessingError }
        public UnityWebRequest(string url, string method) { } public UnityWebRequest(string url, string method, DownloadHandler d, UploadHandler u) { }
        public static UnityWebRequest Get(string u) => null; public static UnityWebRequest Post(string u, string data, string contentType) => null; public static UnityWebRequest Put(string u, string d) => null; public static UnityWebRequest Delete(string u) => null;
        public DownloadHandler downloadHandler { get; set; } public UploadHandler uploadHandler { get; set; } public string method { get; set; } public string url { get; set; } public int timeout { get; set; } public long responseCode => 0; public string error => null; public Result result => Result.Success; public bool isDone => true; public float downloadProgress => 0; public bool disposeUploadHandlerOnDispose { get; set; } public bool disposeDownloadHandlerOnDispose { get; set; }
        public void SetRequestHeader(string k, string v) { } public string GetResponseHeader(string k) => null; public UnityWebRequestAsyncOperation SendWebRequest() => null; public void Abort() { } public void Dispose() { }
        public static string EscapeURL(string s) => s;
    }
    public partial class UnityWebRequestAsyncOperation : AsyncOperation { public UnityWebRequest webRequest => null; }
    public partial class DownloadHandler : IDisposable { public string text => ""; public byte[] data => null; public bool isDone => true; public void Dispose() { } }
    public partial class DownloadHandlerBuffer : DownloadHandler { }
    public partial class UploadHandler : IDisposable { public string contentType { get; set; } public void Dispose() { } }
    public partial class UploadHandlerRaw : UploadHandler { public UploadHandlerRaw(byte[] d) { } }
}

namespace UnityEngine.EventSystems
{
    public partial class UIBehaviour : MonoBehaviour { }
    public partial class EventSystem : UIBehaviour { public static EventSystem current { get; set; } public bool IsPointerOverGameObject() => false; public bool IsPointerOverGameObject(int id) => false; public GameObject currentSelectedGameObject => null; public void SetSelectedGameObject(GameObject g) { } public int pixelDragThreshold { get; set; } public void RaycastAll(PointerEventData e, List<RaycastResult> r) { } }
    public partial class BaseInputModule : UIBehaviour { }
    public partial class StandaloneInputModule : BaseInputModule { }
    public partial class BaseEventData { public BaseEventData(EventSystem e) { } public void Use() { } public bool used => false; }
    public enum PointerEventButton { Left, Right, Middle }
    public partial class PointerEventData : BaseEventData
    {
        public enum InputButton { Left, Right, Middle }
        public PointerEventData(EventSystem e) : base(e) { } public Vector2 position { get; set; } public Vector2 delta { get; set; } public Vector2 pressPosition { get; set; } public int pointerId { get; set; } public InputButton button { get; set; } public int clickCount { get; set; } public bool dragging { get; set; } public GameObject pointerEnter { get; set; } public GameObject pointerPress { get; set; } public Vector2 scrollDelta { get; set; } public bool eligibleForClick { get; set; } public float clickTime { get; set; } public GameObject rawPointerPress { get; set; } public RaycastResult pointerCurrentRaycast { get; set; } public bool useDragThreshold { get; set; }
    }
    public partial struct RaycastResult { public GameObject gameObject { get; set; } public float distance; public Vector3 worldPosition; }
    public interface IEventSystemHandler { }
    public interface IPointerDownHandler : IEventSystemHandler { void OnPointerDown(PointerEventData e); }
    public interface IPointerUpHandler : IEventSystemHandler { void OnPointerUp(PointerEventData e); }
    public interface IPointerClickHandler : IEventSystemHandler { void OnPointerClick(PointerEventData e); }
    public interface IPointerEnterHandler : IEventSystemHandler { void OnPointerEnter(PointerEventData e); }
    public interface IPointerExitHandler : IEventSystemHandler { void OnPointerExit(PointerEventData e); }
    public interface IBeginDragHandler : IEventSystemHandler { void OnBeginDrag(PointerEventData e); }
    public interface IDragHandler : IEventSystemHandler { void OnDrag(PointerEventData e); }
    public interface IEndDragHandler : IEventSystemHandler { void OnEndDrag(PointerEventData e); }
    public interface IInitializePotentialDragHandler : IEventSystemHandler { void OnInitializePotentialDrag(PointerEventData e); }
    public interface IScrollHandler : IEventSystemHandler { void OnScroll(PointerEventData e); }
    public static class ExecuteEvents { }
}

namespace UnityEngine.Events
{
    public partial class UnityEventBase { public void RemoveAllListeners() { } }
    public partial class UnityEvent : UnityEventBase { public void AddListener(UnityAction a) { } public void RemoveListener(UnityAction a) { } public void Invoke() { } }
    public partial class UnityEvent<T0> : UnityEventBase { public void AddListener(UnityAction<T0> a) { } public void RemoveListener(UnityAction<T0> a) { } public void Invoke(T0 a) { } }
    public delegate void UnityAction(); public delegate void UnityAction<T0>(T0 a);
}

namespace UnityEngine.UI
{
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    public partial class Graphic : UIBehaviour { public Color color { get; set; } public bool raycastTarget { get; set; } public RectTransform rectTransform => null; public Material material { get; set; } public Canvas canvas => null; public void SetAllDirty() { } public void SetVerticesDirty() { } public void CrossFadeAlpha(float a, float d, bool i) { } public void CrossFadeColor(Color c, float d, bool i, bool a) { } public Vector4 raycastPadding { get; set; } }
    public partial class MaskableGraphic : Graphic { public bool maskable { get; set; } }
    public partial class Image : MaskableGraphic
    {
        public enum Type { Simple, Sliced, Tiled, Filled } public enum FillMethod { Horizontal, Vertical, Radial90, Radial180, Radial360 } public enum Origin360 { Bottom, Right, Top, Left }
        public Sprite sprite { get; set; } public Type type { get; set; } public float fillAmount { get; set; } public FillMethod fillMethod { get; set; } public int fillOrigin { get; set; } public bool fillClockwise { get; set; } public bool preserveAspect { get; set; } public float pixelsPerUnitMultiplier { get; set; } public bool fillCenter { get; set; } public void SetNativeSize() { } public float alphaHitTestMinimumThreshold { get; set; }
    }
    public partial class RawImage : MaskableGraphic { public Texture texture { get; set; } public Rect uvRect { get; set; } }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }
    public partial class Text : MaskableGraphic
    {
        public string text { get; set; } public Font font { get; set; } public int fontSize { get; set; } public FontStyle fontStyle { get; set; } public TextAnchor alignment { get; set; } public bool supportRichText { get; set; } public HorizontalWrapMode horizontalOverflow { get; set; } public VerticalWrapMode verticalOverflow { get; set; } public float lineSpacing { get; set; }
        public bool resizeTextForBestFit { get; set; } public int resizeTextMinSize { get; set; } public int resizeTextMaxSize { get; set; } public float preferredWidth => 0; public float preferredHeight => 0; public bool alignByGeometry { get; set; }
    }
    public partial class BaseMeshEffect : UIBehaviour { }
    public partial class Shadow : BaseMeshEffect { public Color effectColor { get; set; } public Vector2 effectDistance { get; set; } public bool useGraphicAlpha { get; set; } }
    public partial class Outline : Shadow { }
    public partial class Selectable : UIBehaviour
    {
        public enum Transition { None, ColorTint, SpriteSwap, Animation }
        public bool interactable { get; set; } public Graphic targetGraphic { get; set; } public ColorBlock colors { get; set; } public Transition transition { get; set; } public Navigation navigation { get; set; } public bool IsInteractable() => true; public void Select() { }
    }
    public partial struct Navigation { public enum Mode { None, Horizontal, Vertical, Automatic, Explicit } public Mode mode { get; set; } public static Navigation defaultNavigation => default(Navigation); }
    public partial struct ColorBlock { public Color normalColor { get; set; } public Color highlightedColor { get; set; } public Color pressedColor { get; set; } public Color selectedColor { get; set; } public Color disabledColor { get; set; } public float colorMultiplier { get; set; } public float fadeDuration { get; set; } public static ColorBlock defaultColorBlock => default(ColorBlock); }
    public partial class Button : Selectable, IPointerClickHandler { public partial class ButtonClickedEvent : UnityEvent { } public ButtonClickedEvent onClick { get; set; } public void OnPointerClick(PointerEventData e) { } }
    public partial class Toggle : Selectable { public partial class ToggleEvent : UnityEvent<bool> { } public bool isOn { get; set; } public Graphic graphic { get; set; } public ToggleGroup group { get; set; } public ToggleEvent onValueChanged { get; set; } public void SetIsOnWithoutNotify(bool v) { } }
    public partial class ToggleGroup : UIBehaviour { public bool allowSwitchOff { get; set; } }
    public partial class Slider : Selectable
    {
        public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom } public partial class SliderEvent : UnityEvent<float> { }
        public float value { get; set; } public float minValue { get; set; } public float maxValue { get; set; } public bool wholeNumbers { get; set; } public RectTransform fillRect { get; set; } public RectTransform handleRect { get; set; } public Direction direction { get; set; } public SliderEvent onValueChanged { get; set; } public void SetValueWithoutNotify(float v) { } public float normalizedValue { get; set; }
    }
    public partial class Scrollbar : Selectable { public float value { get; set; } public float size { get; set; } }
    public partial class InputField : Selectable
    {
        public enum ContentType { Standard, Autocorrected, IntegerNumber, DecimalNumber, Alphanumeric, Name, EmailAddress, Password, Pin, Custom } public enum LineType { SingleLine, MultiLineSubmit, MultiLineNewline }
        public partial class SubmitEvent : UnityEvent<string> { } public partial class OnChangeEvent : UnityEvent<string> { }
        public string text { get; set; } public Text textComponent { get; set; } public Graphic placeholder { get; set; } public int characterLimit { get; set; } public ContentType contentType { get; set; } public LineType lineType { get; set; } public SubmitEvent onEndEdit { get; set; } public OnChangeEvent onValueChanged { get; set; } public bool isFocused => false; public void ActivateInputField() { } public void DeactivateInputField() { } public bool readOnly { get; set; } public void SetTextWithoutNotify(string t) { } public TouchScreenKeyboardType keyboardType { get; set; }
    }
    public partial class ScrollRect : UIBehaviour
    {
        public enum MovementType { Unrestricted, Elastic, Clamped } public enum ScrollbarVisibility { Permanent, AutoHide, AutoHideAndExpandViewport }
        public partial class ScrollRectEvent : UnityEvent<Vector2> { }
        public RectTransform content { get; set; } public RectTransform viewport { get; set; } public bool horizontal { get; set; } public bool vertical { get; set; } public MovementType movementType { get; set; } public float elasticity { get; set; } public bool inertia { get; set; } public float decelerationRate { get; set; } public float scrollSensitivity { get; set; }
        public Vector2 normalizedPosition { get; set; } public float verticalNormalizedPosition { get; set; } public float horizontalNormalizedPosition { get; set; } public Vector2 velocity { get; set; } public void StopMovement() { } public ScrollRectEvent onValueChanged { get; set; } public Scrollbar verticalScrollbar { get; set; } public Scrollbar horizontalScrollbar { get; set; }
    }
    public partial class Mask : UIBehaviour { public bool showMaskGraphic { get; set; } }
    public partial class RectMask2D : UIBehaviour { public Vector4 padding { get; set; } public Vector2Int softness { get; set; } }
    public partial class LayoutGroup : UIBehaviour { public RectOffset padding { get; set; } public TextAnchor childAlignment { get; set; } }
    public partial class HorizontalOrVerticalLayoutGroup : LayoutGroup { public float spacing { get; set; } public bool childForceExpandWidth { get; set; } public bool childForceExpandHeight { get; set; } public bool childControlWidth { get; set; } public bool childControlHeight { get; set; } public bool childScaleWidth { get; set; } public bool childScaleHeight { get; set; } public bool reverseArrangement { get; set; } }
    public partial class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public partial class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public partial class GridLayoutGroup : LayoutGroup
    {
        public enum Corner { UpperLeft, UpperRight, LowerLeft, LowerRight } public enum Axis { Horizontal, Vertical } public enum Constraint { Flexible, FixedColumnCount, FixedRowCount }
        public Vector2 cellSize { get; set; } public Vector2 spacing { get; set; } public Constraint constraint { get; set; } public int constraintCount { get; set; } public Corner startCorner { get; set; } public Axis startAxis { get; set; }
    }
    public partial class LayoutElement : UIBehaviour { public float minWidth { get; set; } public float minHeight { get; set; } public float preferredWidth { get; set; } public float preferredHeight { get; set; } public float flexibleWidth { get; set; } public float flexibleHeight { get; set; } public bool ignoreLayout { get; set; } public int layoutPriority { get; set; } }
    public partial class ContentSizeFitter : UIBehaviour { public enum FitMode { Unconstrained, MinSize, PreferredSize } public FitMode horizontalFit { get; set; } public FitMode verticalFit { get; set; } }
    public partial class AspectRatioFitter : UIBehaviour { public enum AspectMode { None, WidthControlsHeight, HeightControlsWidth, FitInParent, EnvelopeParent } public AspectMode aspectMode { get; set; } public float aspectRatio { get; set; } }
    public partial class CanvasScaler : UIBehaviour { public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize } public enum ScreenMatchMode { MatchWidthOrHeight, Expand, Shrink } public ScaleMode uiScaleMode { get; set; } public float scaleFactor { get; set; } public Vector2 referenceResolution { get; set; } public ScreenMatchMode screenMatchMode { get; set; } public float matchWidthOrHeight { get; set; } public float referencePixelsPerUnit { get; set; } }
    public partial class GraphicRaycaster : UIBehaviour { }
    public static partial class LayoutRebuilder { public static void ForceRebuildLayoutImmediate(RectTransform r) { } public static void MarkLayoutForRebuild(RectTransform r) { } }
    public static partial class LayoutUtility { public static float GetPreferredHeight(RectTransform r) => 0; public static float GetPreferredWidth(RectTransform r) => 0; }
}

namespace UnityEngine
{
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public partial class Canvas : Behaviour { public RenderMode renderMode { get; set; } public int sortingOrder { get; set; } public float scaleFactor { get; set; } public Camera worldCamera { get; set; } public bool overrideSorting { get; set; } public bool pixelPerfect { get; set; } public float planeDistance { get; set; } public static void ForceUpdateCanvases() { } public Canvas rootCanvas => null; public float referencePixelsPerUnit { get; set; } }
    public partial class CanvasGroup : Behaviour { public float alpha { get; set; } public bool interactable { get; set; } public bool blocksRaycasts { get; set; } public bool ignoreParentGroups { get; set; } }
    public partial class CanvasRenderer : Component { }
    public partial class RectOffset { public RectOffset() { } public RectOffset(int l, int r, int t, int b) { } public int left { get; set; } public int right { get; set; } public int top { get; set; } public int bottom { get; set; } public int horizontal => 0; public int vertical => 0; }
    public enum TouchScreenKeyboardType { Default, ASCIICapable, NumbersAndPunctuation, URL, NumberPad, PhonePad, NamePhonePad, EmailAddress }
    public partial class TouchScreenKeyboard { public static TouchScreenKeyboard Open(string t) => null; public static bool isSupported => true; }
    public static partial class GUIUtility { public static string systemCopyBuffer { get; set; } }
}
