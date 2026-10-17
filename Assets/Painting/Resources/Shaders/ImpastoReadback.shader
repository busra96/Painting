Shader "Hidden/Painting/Pigment Readback"
{
    Properties { _MainTex ("Premultiplied pigment", 2D) = "black" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 frag(v2f_img i) : SV_Target
            {
                float4 paint = tex2D(_MainTex, i.uv);
                // Scoring reads opaque pigment over white, without lighting or canvas artwork.
                return float4(paint.rgb + (1 - paint.a), 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
