Shader "Custom/FlatShading"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            v2f vert(appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 faceNormal = normalize(cross(ddy(i.worldPos), ddx(i.worldPos)));

                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float diff = max(0, dot(faceNormal, lightDir));

                fixed4 tex = tex2D(_MainTex, i.uv);
                return tex * _Color * diff;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
