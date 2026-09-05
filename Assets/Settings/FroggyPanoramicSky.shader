Shader "Froggy/Panoramic Sky"
{
    Properties
    {
        _MainTex ("Sky Image", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Exposure ("Exposure", Range(0, 8)) = 1
        _Rotation ("Rotation", Range(0, 360)) = 0
        _SeamBlend ("Seam Blend Width", Range(0, 0.1)) = 0.025
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Tint;
            half _Exposure;
            float _Rotation;
            half _SeamBlend;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.direction = input.positionOS.xyz;
                return output;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                float rotation = radians(_Rotation);
                float sineRotation = sin(rotation);
                float cosineRotation = cos(rotation);
                direction.xz = float2(
                    direction.x * cosineRotation - direction.z * sineRotation,
                    direction.x * sineRotation + direction.z * cosineRotation);

                float2 uv;
                uv.x = atan2(direction.z, direction.x) / (2.0 * UNITY_PI) + 0.5;
                uv.y = asin(clamp(direction.y, -1.0, 1.0)) / UNITY_PI + 0.5;

                // atan2 wraps U from 1 back to 0. Automatic texture gradients see
                // that wrap as a full-width jump and select a very low mip level,
                // which creates a thin vertical seam. Wrap only the U gradients so
                // the sky remains sharp and continuous across the panorama edge.
                float2 uvDx = ddx(uv);
                float2 uvDy = ddy(uv);
                uvDx.x = frac(uvDx.x + 0.5) - 0.5;
                uvDy.x = frac(uvDy.x + 0.5) - 0.5;

                fixed4 skyColor = tex2Dgrad(_MainTex, uv, uvDx, uvDy);

                // The panorama's first and last columns are not perfectly equal.
                // Symmetrically blend both sides only near the wrap point so the
                // join stays continuous without softening the rest of the sky.
                float halfTexel = _MainTex_TexelSize.x * 0.5;
                float2 pairedUv = uv;
                pairedUv.x = uv.x < 0.5
                    ? 1.0 - uv.x - halfTexel
                    : 1.0 - uv.x + halfTexel;
                pairedUv.x = frac(pairedUv.x);

                float2 pairedDx = float2(-uvDx.x, uvDx.y);
                float2 pairedDy = float2(-uvDy.x, uvDy.y);
                fixed4 pairedColor = tex2Dgrad(_MainTex, pairedUv, pairedDx, pairedDy);

                float seamDistance = min(uv.x, 1.0 - uv.x);
                float seamAmount = 1.0 - smoothstep(0.0, max(_SeamBlend, 0.0001), seamDistance);
                skyColor = lerp(skyColor, (skyColor + pairedColor) * 0.5, seamAmount);

                return skyColor * _Tint * _Exposure;
            }
            ENDCG
        }
    }
}
