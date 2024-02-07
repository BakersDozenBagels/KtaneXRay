Shader "Custom/XORay" {
	Properties {
		[NoScaleOffset]
		_MainTex ("Spritesheet", 2D) = "white" {}
		_BackgroundColor ("Background Color", Color) = (0,0,0,0)
		_Color ("Foreground Color", Color) = (1,1,1,1)
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		Lighting Off

		Pass {	
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float4 uvs : TEXCOORD0;
			};

			sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			
			uniform fixed4 _Color;
			uniform fixed4 _BackgroundColor;

			uniform float _progress;
			uniform float4x4 _matrix;

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uvs = mul(_matrix, float4(v.uv.x, _progress, 1, 0));
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				return lerp(_BackgroundColor, _Color, abs(step(tex2D(_MainTex, i.uvs.xy).a, 0.5) - step(tex2D(_MainTex, i.uvs.zw).a, 0.5)));
			}
			ENDCG 
		}
	}
}
