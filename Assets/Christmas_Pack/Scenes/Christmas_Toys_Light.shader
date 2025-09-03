// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "AE/Christmas_Toys_Light"
{
	Properties
	{
		_Christmas_Toys_E("Christmas_Toys_E", 2D) = "white" {}
		_Speed1("Speed", Range( 0 , 5)) = 2.57
		_Christmas_Toys_A("Christmas_Toys_A", 2D) = "white" {}
		[HDR]_Emmisive_Power("Emmisive_Power", Range( 0 , 300)) = 3
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _Christmas_Toys_A;
		uniform float4 _Christmas_Toys_A_ST;
		uniform sampler2D _Christmas_Toys_E;
		uniform float4 _Christmas_Toys_E_ST;
		uniform float _Speed1;
		uniform float _Emmisive_Power;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_Christmas_Toys_A = i.uv_texcoord * _Christmas_Toys_A_ST.xy + _Christmas_Toys_A_ST.zw;
			o.Albedo = tex2D( _Christmas_Toys_A, uv_Christmas_Toys_A ).rgb;
			float2 uv_Christmas_Toys_E = i.uv_texcoord * _Christmas_Toys_E_ST.xy + _Christmas_Toys_E_ST.zw;
			o.Emission = ( ( tex2D( _Christmas_Toys_E, uv_Christmas_Toys_E ) * frac( ( _Speed1 * _Time.y ) ) ) * _Emmisive_Power ).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18921
0;18;1906;1001;2502.15;466.8857;1.486494;True;False
Node;AmplifyShaderEditor.TimeNode;11;-1942.791,253.5911;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;19;-2241.465,-239.5133;Inherit;False;Property;_Speed1;Speed;1;0;Create;True;0;0;0;False;0;False;2.57;5;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;12;-1606.791,109.5908;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;3;-1154.551,-23.23791;Inherit;True;Property;_Christmas_Toys_E;Christmas_Toys_E;0;0;Create;True;0;0;0;False;0;False;-1;dce6c595cfa9f3f43abe57959fe541c6;dce6c595cfa9f3f43abe57959fe541c6;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FractNode;13;-1318.791,205.5908;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;20;-751.6216,166.0442;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;17;-754.3745,579.3181;Inherit;False;Property;_Emmisive_Power;Emmisive_Power;3;1;[HDR];Create;True;0;0;0;False;0;False;3;0.4;0;300;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;8;-482.6416,-164.1849;Inherit;True;Property;_Christmas_Toys_A;Christmas_Toys_A;2;0;Create;True;0;0;0;False;0;False;-1;d4f2dfecf75d4a64ea87d88e6077a9e6;d4f2dfecf75d4a64ea87d88e6077a9e6;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;18;-358.791,93.59081;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;AE/Christmas_Toys_Light;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;12;0;19;0
WireConnection;12;1;11;2
WireConnection;13;0;12;0
WireConnection;20;0;3;0
WireConnection;20;1;13;0
WireConnection;18;0;20;0
WireConnection;18;1;17;0
WireConnection;0;0;8;0
WireConnection;0;2;18;0
ASEEND*/
//CHKSM=2AAEDB850DE4B7751A9F8B3F0032E459055B2E63