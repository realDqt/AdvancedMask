Shader "Hidden/WhiteShadowBlit"
{
    Properties
    {
        _MainTex        ("Ignored", 2D)   = "white" {}
        _ShadowMaskTex  ("Shadow Mask", 2D)= "white" {}
        _ObjectMaskTex  ("Object Mask", 2D)= "white" {}
        _Offset         ("Offset XY", Vector) = (0,0,0,0)
        _Scale          ("Scale",     Vector) = (1,1,0,0)
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _ShadowMaskTex;
            sampler2D _ObjectMaskTex;
            float4    _Offset;
            float4    _Scale;

            fixed4 frag (v2f_img i) : SV_Target
            {
                // 用 _Scale.xy 缩放、_Offset.xy 平移 UV
                float2 uv = (i.uv - 0.5) / _Scale.xy + 0.5 + _Offset.xy;

                // 采样并做边界检测
                half shadowMask = (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) ? 0
                                  : tex2D(_ShadowMaskTex, uv).r;
                half objectMask = (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) ? 0
                                  : tex2D(_ObjectMaskTex, uv).r;

                // 规则不变：两者至少一个大于阈值 → 黑；否则白
                return (shadowMask > 0.001 || objectMask > 0.001)
                       ? fixed4(0,0,0,1)
                       : fixed4(1,1,1,1);
            }
            ENDCG
        }
    }
}