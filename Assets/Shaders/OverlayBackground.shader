Shader "Hidden/OverlayBackground"
{
    Properties
    {
        _MainTex ("Scene", 2D) = "white" {}
        _BackgroundTex ("Background (R)", 2D) = "white" {}
        _OverlayColor ("Overlay Color", Color) = (0,0,0,1)
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
            fixed4 _OverlayColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 backgroundColor   = tex2D(_BackgroundTex, i.uv);   
                return backgroundColor;
            }
            ENDCG
        }
    }
}