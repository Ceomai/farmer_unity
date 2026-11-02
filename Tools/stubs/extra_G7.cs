// G7 (içerik) için eksik gerçek Unity API taslakları
namespace UnityEngine
{
    public partial struct Rect { public static Rect MinMaxRect(float xmin, float ymin, float xmax, float ymax) => new Rect(xmin, ymin, xmax - xmin, ymax - ymin); }
}
