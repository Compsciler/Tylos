Shader "Unlit/DevianceShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ArmyColor ("Army Color", Color) = (1,1,1,1)
        _HighlightColor ("Highlight Color", Color) = (1,0,0, 0.5)
    }
    SubShader
    {
        Tags {"RenderType" = "Transparent"}
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma alpha
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

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _ArmyColor;
            float4 _HighlightColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 dir = i.uv - float2(0.5f, 0.5f);
                float len = length(dir);
                float angle = atan2(dir.y, dir.x);
                float ref_len = 0.05f * sin(angle * 10) + 0.4f;
                // sample the texture
                fixed4 col = _ArmyColor;
                if (len > ref_len)
                {
                    col = _HighlightColor;
                };
                return col;
            }
            ENDCG
        }
    }
}
