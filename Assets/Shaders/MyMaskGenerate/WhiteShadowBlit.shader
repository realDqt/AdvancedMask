Shader "Hidden/WhiteShadowBlit"
{
    Properties { _MainTex("Ignored", 2D) = "white" {} }

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

            fixed4 frag(v2f_img i) : SV_Target
            {
                half mask = tex2D(_ShadowMaskTex, i.uv).r;
                return mask > 0.001 ? fixed4(1,1,1,1) : fixed4(0,0,0,1);
            }
            ENDCG
        }
    }
}