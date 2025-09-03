Shader "Unlit/BGProcess"
{
    Properties
    {
        _BackgroundTex ("Background", 2D) = "white" {}
        _Multiplier    ("Multiplier", Range(0,1)) = 1
        
        _QuadrantIndex ("Quadrant (0~3)", Range(0,3)) = 0
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
            float     _SquareSize;
            float2    _Offset;
            int        _QuadrantIndex;

             fixed4 frag (v2f_img IN) : SV_Target
            {
                // 屏幕像素坐标
                float2 screenPos = IN.uv * _ScreenParams.xy;
                float2 local     = screenPos - _Offset;

                // 如果不在整个正方形外框内直接返回黑
                if (local.x < 0 || local.y < 0 ||
                    local.x >= _SquareSize || local.y >= _SquareSize)
                    return fixed4(0,0,0,1);

                // 归一化到 0~1
                float2 norm = local / _SquareSize;

                // 计算该象限在纹理中的起点
                float2 quadStart = 0;
                if (_QuadrantIndex == 0)      quadStart = float2(0.0, 0.5);   // 左上
                else if (_QuadrantIndex == 1) quadStart = float2(0.5, 0.5);   // 右上
                else if (_QuadrantIndex == 2) quadStart = float2(0.0, 0.0);   // 左下
                else                          quadStart = float2(0.5, 0.0);   // 右下

                // 把 0~1 的 norm 缩放到 0~0.5，再平移到对应象限
                float2 texUV = quadStart + norm * 0.5;

                fixed4 col = tex2D(_BackgroundTex, texUV);
                col.rgb *= _Multiplier;
                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
