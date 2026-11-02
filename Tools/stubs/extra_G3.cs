// G3 (motor) için eksik gerçek Unity API'leri (taslak). Hepsi UnityEngine'de gerçekten var.
using System.Collections.Generic;
namespace UnityEngine
{
    public partial class Mesh
    {
        public void SetUVs(int channel, List<Vector3> uvs) { }
        public void SetUVs(int channel, List<Vector4> uvs) { }
    }
}
