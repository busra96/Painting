Shader "Painting/Steel Spatula"
{
    Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(float4 vertex:POSITION,float2 uv:TEXCOORD0) { v2f o; o.pos=UnityObjectToClipPos(vertex);o.uv=uv;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=abs(i.uv-0.5)-float2(0.487,0.38);
                float edge=length(max(p,0))-0.055;
                float a=1-smoothstep(-0.004,0.003,edge);
                float shine=exp(-pow((i.uv.y-0.32)*12,2));
                float top=exp(-pow((i.uv.y-0.74)*25,2));
                float3 metal=lerp(float3(0.24,0.31,0.37),float3(0.82,0.88,0.92),shine*0.75+top*0.2);
                metal+=exp(-pow((i.uv.y-0.11)*90,2))*0.45;
                float brush=sin(i.uv.x*3200)*0.008;
                metal+=brush;
                return float4(metal,a);
            }
            ENDCG
        }
    }
}
