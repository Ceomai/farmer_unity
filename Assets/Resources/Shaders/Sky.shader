// Gökyüzü arka planı: web sürümünde tuval saydamdı ve arkada CSS "linear-gradient(180deg, top, low)" vardı
// (16-time-weather günün saatine göre değiştirir). Burada ekran-uzayı dikey gradyan olarak çizilen bir skybox.
// _Top/_Low sRGB değerleriyle verilir (CSS gibi sRGB'de karıştırılır, sonra doğrusala çevrilir).
Shader "Ciftlik/Sky"
{
    Properties
    {
        _Top ("Top (sRGB)", Vector) = (0.639, 0.769, 0.753, 1)
        _Low ("Low (sRGB)", Vector) = (0.937, 0.878, 0.714, 1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Top, _Low;
            struct v2f { float4 pos : SV_POSITION; float4 sp : TEXCOORD0; };
            v2f vert (appdata_base v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.sp = ComputeScreenPos(o.pos); return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                float y = saturate(i.sp.y / i.sp.w);
                float3 c = lerp(_Low.rgb, _Top.rgb, y);
                #ifndef UNITY_COLORSPACE_GAMMA
                c = GammaToLinearSpace(c);
                #endif
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
