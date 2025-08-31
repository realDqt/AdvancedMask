Shader "Hidden/WhiteShadowCaster"
{
    SubShader
    {
        Tags { "LightMode" = "ShadowCaster" }
        Pass
        {
            ZWrite On ZTest LEqual Cull Back
            ColorMask R       // 只写 R 通道即可
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
            };

            v2f vert(appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return i.color.a > 0.001 ?fixed4(1,0,0,0) : fixed4(0, 0, 0, 0);   // 任意非 0 值都行，反正只写 R
            }
            ENDCG
        }
    }
}