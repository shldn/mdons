Shader "Custom/Silhoutted Hatching" {
 
     Properties {
 
         _OutlineColor ("Outline Color", Color) = (0,0,0,1)
 
         _Outline ("Outline width", Range (0.0, 0.03)) = .005
 
         _ShinPower ("Shininess", Range(0,1)) = 0.5
 
         _GlossPower ("Gloss", Range(0.01,1)) = 0.5
 
         _MainTex ("Texture", 2D) = "white" {}
 
         _BumpMap ("Bumpmap", 2D) = "bump" {}
 
         _SpecularTex ("Specular Map", 2D) = "gray" {}
 
         _Hatch0 ("Hatch 0", 2D) = "white" {}
 
         _Hatch1 ("Hatch 1", 2D) = "gray" {}
 
         _Hatch2 ("Hatch 2", 2D) = "gray" {}
 
         _Hatch3 ("Hatch 3", 2D) = "black" {}
 
     }
 
  
 
  
 
  
 
  
 
  
 
     SubShader {
 
         Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
 
  
 
         Pass {
 
             Name "OUTLINE"
 
             Tags { "LightMode" = "Always" }
 
             Cull Off
 
             ZWrite Off
 
             Blend SrcAlpha OneMinusSrcAlpha
 
  
 
             CGPROGRAM
 
             #pragma vertex vert
 
             #pragma fragment frag
 
             #include "UnityCG.cginc"
 
  
 
             struct appdata {
 
                 float4 vertex : POSITION;
 
                 float3 normal : NORMAL;
 
             };
 
  
 
             struct v2f {
 
                 float4 pos : POSITION;
 
                 float4 color : COLOR;
 
             };
 
  
 
             uniform float _Outline;
 
             uniform float4 _OutlineColor;
 
  
 
             v2f vert(appdata v) {
 
                 v2f o;
 
                 o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
 
                 float3 norm   = mul ((float3x3)UNITY_MATRIX_IT_MV, v.normal);
 
                 float2 offset = TransformViewToProjection(norm.xy);
 
                 o.pos.xy += offset * o.pos.z * _Outline;
 
                 o.color = _OutlineColor;
 
                 return o;
 
             }
 
  
 
             half4 frag(v2f i) : COLOR {
 
                 return i.color;
 
             }
 
             ENDCG
 
         }
 
  
 
         CGPROGRAM
 
  
 
         #pragma surface surf Hatching noambient
 
         #pragma only_renderers d3d9
 
         #pragma target 3.0
 
  
 
         struct SurfaceOutputHatch {
 
             fixed3 Albedo;
 
             fixed3 Normal;
 
             fixed4 Hatch;
 
             fixed3 Emission;
 
             fixed Specular;
 
             fixed Gloss;
 
             float Alpha;
 
         };
 
  
 
         struct Input
 
         {
 
             float2 uv_Hatch0;
 
             float2 uv_MainTex;
 
         };
 
  
 
         sampler2D _Hatch0, _Hatch1, _Hatch2, _Hatch3;
 
         sampler2D _MainTex;
 
         sampler2D _BumpMap;
 
         sampler2D _SpecularTex;
 
         
  
 
         float _ShinPower;
 
         float _GlossPower;
 
  
 
         inline half4 LightingHatching (inout SurfaceOutputHatch s, half3 lightDir, half3 viewDir, half atten)
 
         {
 
             float3 h = normalize (lightDir + viewDir);
 
             float NdotL = dot (s.Normal, lightDir) * 0.5 + 0.5;
 
             float nh = max (0, dot (s.Normal, h));
 
             float spec = pow (nh, s.Gloss * 64) * s.Specular;
 
             
 
             float intensity = saturate((NdotL + spec) * atten);
 
             
 
             fixed hatch;
 
             hatch = lerp ( s.Hatch.r, 1.0, saturate((intensity - 0.75) * 4));
 
             hatch = lerp ( s.Hatch.g, hatch, saturate((intensity - 0.5) * 4));
 
             hatch = lerp ( s.Hatch.b, hatch, saturate((intensity - 0.25) * 4));
 
             hatch = lerp ( s.Hatch.a, hatch, saturate((intensity) * 4));
 
             
 
              
             half4 c;
 
             c.rgb = s.Albedo * _LightColor0.rgb * hatch;
 
             c.a = 0.5;
 
             return c;
 
         }
 
  
 
         void surf (Input IN, inout SurfaceOutputHatch o)
 
         {
 
             o.Albedo = tex2D (_MainTex, IN.uv_MainTex).rgb;
 
             o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
 
             fixed2 specGloss = tex2D(_SpecularTex, IN.uv_MainTex).rg;
 
             o.Specular = specGloss.r * _ShinPower;
 
             o.Gloss = specGloss.g * _GlossPower;
 
             o.Hatch.r = tex2D(_Hatch0, IN.uv_Hatch0);
 
             o.Hatch.g = tex2D(_Hatch1, IN.uv_Hatch0);
 
             o.Hatch.b = tex2D(_Hatch2, IN.uv_Hatch0);
 
             o.Hatch.a = tex2D(_Hatch3, IN.uv_Hatch0);
 
         }
 
         ENDCG
 
     }
 
     Fallback "Diffuse"
 
 }