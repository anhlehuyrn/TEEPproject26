Shader "TEEP/Avatar Spawn Scan"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _MetallicGlossMap ("Metallic Smoothness", 2D) = "white" {}
        [HDR]_EdgeColor1 ("Lower Edge Color", Color) = (0.2, 1.15, 1.35, 1)
        [HDR]_EdgeColor2 ("Upper Edge Color", Color) = (0.15, 0.55, 2.2, 1)
        _Cutoff ("Cutoff", Range(0.01,1)) = 1
        _EdgeSizeBot ("Visible Edge Size", Range(0.005,0.5)) = 0.06
        _EdgeSizeTop ("Preview Edge Size", Range(0.005,0.5)) = 0.1
        _NoiseScale ("Noise Scale", Range(0.1,16)) = 7
        _NoiseStrength ("Noise Strength", Range(0,0.4)) = 0.04
        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.55
        _BoundsMinY ("Bounds Min Y", Float) = -1
        _BoundsMaxY ("Bounds Max Y", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
        Cull Off
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _MetallicGlossMap;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 worldPos;
        };

        half _Cutoff;
        half _EdgeSizeBot;
        half _EdgeSizeTop;
        half _NoiseScale;
        half _NoiseStrength;
        half _Metallic;
        half _Glossiness;
        float _BoundsMinY;
        float _BoundsMaxY;
        fixed4 _Color;
        fixed4 _EdgeColor1;
        fixed4 _EdgeColor2;

        float HashNoise(float2 p)
        {
            return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float heightRange = max(0.001, _BoundsMaxY - _BoundsMinY);
            float normalizedY = saturate((IN.worldPos.y - _BoundsMinY) / heightRange);
            float scan = _Cutoff;

            float noiseA = HashNoise(floor(float2(IN.worldPos.x, IN.worldPos.z) * _NoiseScale));
            float noiseB = HashNoise(floor(float2(IN.worldPos.y, IN.worldPos.x) * (_NoiseScale * 1.3)));
            float reveal = normalizedY + ((noiseA * noiseB) - 0.5) * _NoiseStrength;

            float signedDistance = scan - reveal;
            float visible = step(0.0, signedDistance);
            float visibleEdge = smoothstep(_EdgeSizeBot, 0.0, abs(signedDistance));
            float previewEdge = smoothstep(_EdgeSizeTop, 0.0, abs(signedDistance + _EdgeSizeTop * 0.35)) * (1.0 - visible);

            fixed4 albedo = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            fixed4 metalSmooth = tex2D(_MetallicGlossMap, IN.uv_MainTex);

            o.Albedo = albedo.rgb;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
            o.Metallic = metalSmooth.r * _Metallic;
            o.Smoothness = metalSmooth.a * _Glossiness;
            o.Emission = (_EdgeColor1.rgb * visibleEdge * 2.3) + (_EdgeColor2.rgb * previewEdge * 1.3);

            clip(visible + visibleEdge + previewEdge - 0.05);
        }
        ENDCG
    }

    FallBack "Diffuse"
}
