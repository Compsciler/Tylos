Shader "Custom/PieChartShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _RedAmount ("Red Amount", Range(0, 1)) = 0
        _GreenAmount ("Green Amount", Range(0, 1)) = 0
        _BlueAmount ("Blue Amount", Range(0, 1)) = 0
    }

    SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 100

        CGPROGRAM
        #pragma surface surf Lambert

        sampler2D _MainTex;
        float _RedAmount;
        float _GreenAmount;
        float _BlueAmount;

        struct Input {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutput o) {
            o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
            o.Alpha = 1;

            float2 centeredUV = IN.uv_MainTex - float2(0.5, 0.5);
            float radius = length(centeredUV);
            if (radius > 0.5) {
                o.Alpha = 0;
                discard;
            } else {
                float angle = atan2(centeredUV.y, centeredUV.x) / 3.141592;
                angle = (angle + 1) * 0.5;

                if (angle <= _RedAmount) {
                    o.Albedo = float3(1, 0, 0);
                } else if (angle <= _RedAmount + _GreenAmount) {
                    o.Albedo = float3(0, 1, 0);
                } else if (angle <= _RedAmount + _GreenAmount + _BlueAmount) {
                    o.Albedo = float3(0, 0, 1);
                } else {
                    o.Albedo = o.Albedo * 0.5;
                }
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}