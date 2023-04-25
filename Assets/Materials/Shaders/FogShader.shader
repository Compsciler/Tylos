Shader "Unlit/FogShader"
{
    Properties
    {
        FogTex ("Texture", 2D) = "white" {}
        CloudTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags {"RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D FogTex;
            sampler2D CloudTex;

            float4 FogTex_ST;
            float4 CloudTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, FogTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 fixed_uv = i.uv*2-0.5;
                fixed4 fog = tex2D(FogTex, fixed_uv);
                fixed4 color;
                if(fixed_uv.x > 1 || fixed_uv.x < 0 || fixed_uv.y > 1 || fixed_uv.y < 0){
                    color = tex2D(CloudTex, i.uv);
                    color.w = 1;
                    return color;
                }
                if(fog.x < 0.5){
                    discard;
                    return color;
                } else {
                    color = tex2D(CloudTex, i.uv);
                    color.w = min((fog.x - 0.5) * 5, 1);
                    return color;
                }
            }
            ENDCG
        }
    }
}
