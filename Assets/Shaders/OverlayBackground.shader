Shader "Hidden/OverlayBackground"
{
    Properties
    {
        _MainTex     ("Scene",       2D) = "white" {}
        _BackgroundTex ("Background",2D) = "white" {}
        _OverlayColor ("Overlay Color", Color) = (0,0,0,1)

        // 默认取左下 1/4
        _Left   ("Left",   Float) = 0.0
        _Right  ("Right",  Float) = 0.5
        _Top    ("Top",    Float) = 0.0
        _Bottom ("Bottom", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _BackgroundTex;
            fixed4    _OverlayColor;

            float     _Left;
            float     _Right;
            float     _Top;
            float     _Bottom;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 把 0~1 的屏幕 UV 重映射到指定 1/4 区域的 UV
                float2 quarterUV = float2(
                    lerp(_Left, _Right,  i.uv.x),
                    lerp(_Top,  _Bottom, i.uv.y)
                );

                fixed4 backgroundColor = tex2D(_BackgroundTex, quarterUV);
                return backgroundColor;
            }
            ENDCG
        }
    }
}