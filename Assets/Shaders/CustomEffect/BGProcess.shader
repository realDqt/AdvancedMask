Shader "Unlit/BGProcess"
{
    Properties
    {
        _BackgroundTex ("Background", 2D) = "white" {}
        _Multiplier    ("Multiplier", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _BackgroundTex;
            float     _Multiplier;

            // C# 会传进来
            float _SquareSize;      // 正方形边长（=屏幕高度）
            float2 _Offset;         // 正方形左上角在屏幕上的像素坐标

            fixed4 frag (v2f_img IN) : SV_Target
            {
                // 屏幕像素坐标 (0~w, 0~h)
                float2 screenPos = IN.uv * _ScreenParams.xy;

                // 相对正方形左上角
                float2 local = screenPos - _Offset;

                // 越界 → 黑
                if (local.x < 0 || local.y < 0 ||
                    local.x >= _SquareSize || local.y >= _SquareSize)
                    return fixed4(0,0,0,1);

                // 归一化到 [0,1] 采样纹理（保持1:1）
                float2 texUV = local / _SquareSize;
                fixed4 col = tex2D(_BackgroundTex, texUV);
                col.rgb *= _Multiplier;
                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
