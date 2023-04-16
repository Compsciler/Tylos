Shader "Unlit/UnitMerge"
{
    Properties
    {
        UnitTex ("Texture", 2D) = "white" {}
        FillColor ("Fill Color", Color) = (1,1,1,1)
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

            sampler2D UnitTex;

            float4 UnitTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, UnitTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(UnitTex, i.uv);
                if(tex.x < 0.5){
                    discard;
                }
                return fixed4(tex.yzw, 1);
            }
            ENDCG
        }
    }
}
