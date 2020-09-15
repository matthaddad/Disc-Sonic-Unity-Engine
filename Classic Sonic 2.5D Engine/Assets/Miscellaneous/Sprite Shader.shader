Shader "Sprites/SpriteShader" {
    Properties {
        [PerRendererData]_MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Cutoff("Shadow alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {

        LOD 200
        Cull Off

        CGPROGRAM
        #pragma surface surf Lambert addshadow

        sampler2D _MainTex;
        fixed4 _Color;
        fixed _Cutoff;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutput o) {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = tex.rgb;
            o.Emission = tex.rgb * 0.5;
            o.Alpha = tex.a;
            clip(o.Alpha - _Cutoff);
        }
        ENDCG
    }
    FallBack "Diffuse"
}