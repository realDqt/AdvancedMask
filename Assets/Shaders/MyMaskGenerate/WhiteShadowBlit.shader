Shader "Hidden/WhiteShadowBlit"
{
    Properties
    {
        _MainTex       ("Ignored",     2D)   = "white" {}
        _ShadowMaskTex ("Shadow Mask", 2D)   = "white" {}
        _ObjectMaskTex ("Object Mask", 2D)   = "white" {}
        _Offset        ("Offset XY",   Vector) = (0,0,0,0)
        _Scale         ("Scale",       Vector) = (1,1,0,0)
        [Toggle] _FlipX ("Flip X", Float) = 0
        [Toggle] _FlipY ("Flip Y", Float) = 0
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
            half      _FlipX;
            half      _FlipY;

            fixed4 frag (v2f_img i) : SV_Target
            {
                // 1. 缩放 + 平移
                float2 uv = (i.uv - 0.5) / _Scale.xy + 0.5 + _Offset.xy;

                // 2. 翻转
                if (_FlipX > 0.5) uv.x = 1.0 - uv.x;
                if (_FlipY > 0.5) uv.y = 1.0 - uv.y;

                // 3. 边界
                half shadowMask = (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) ? 0
                                  : tex2D(_ShadowMaskTex, uv).r;
                half objectMask = (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) ? 0
                                  : tex2D(_ObjectMaskTex, uv).r;

                // 4. 输出
                return (shadowMask > 0.001 || objectMask > 0.001)
                       ? fixed4(0,0,0,1)
                       : fixed4(1,1,1,1);
            }
            ENDCG
        }
    }
}