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
            sampler2D _ObjectMaskTex;

            fixed4 frag(v2f_img i) : SV_Target
            {
                half shadowMask = tex2D(_ShadowMaskTex, i.uv).r;
                half objectMask = tex2D(_ObjectMaskTex, i.uv).r;
                return shadowMask > 0.001 || objectMask > 0.01 ? fixed4(1,1,1,1) : fixed4(0,0,0,1);
            }
            ENDCG
        }
    }
}