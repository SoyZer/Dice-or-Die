Shader "Custom/Pixelation"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Pixelation"
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // Size of each pixel block in screen pixels.
            int _PixelSize;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                // _BlitTexture_TexelSize.zw = (width, height) in pixels.
                float2 resolution = _BlitTexture_TexelSize.zw;

                // Snap UV to the nearest pixel-block corner.
                float2 pixelUV = floor(uv * resolution / _PixelSize) * _PixelSize / resolution;

                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, pixelUV);
            }
            ENDHLSL
        }
    }
}
