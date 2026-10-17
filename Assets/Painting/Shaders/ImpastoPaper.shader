Shader "Painting/Impasto Paper"
{
    Properties
    {
        [PerRendererData] _MainTex ("Pigment", 2D) = "white" {}
        _PigmentTex ("Live pigment", 2D) = "white" {}
        _HeightTex ("Paint thickness", 2D) = "black" {}
        _ReservoirTex ("Paint on blade", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Relief ("Relief height", Float) = 0.075
        _Gloss ("Wet gloss", Float) = 0.32
        _ShadowStrength ("Contact shadows", Float) = 0.32
        _BladeVisible ("Blade visible", Float) = 0
        _BladeY ("Blade position", Float) = 1
        _BackdropCanvas ("Reveal canvas artwork", Float) = 0
        _PaperSize ("Paper size and cell size", Vector) = (8.51,12.61,0.022,0.022)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _PigmentTex, _HeightTex, _ReservoirTex;
            float4 _HeightTex_TexelSize, _Color, _PaperSize;
            float _Relief, _Gloss, _ShadowStrength, _BladeY, _BladeVisible, _BackdropCanvas;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o;
            }
            float noise(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float meniscus(float y, float volume)
            {
                float d=(y-_BladeY)*_PaperSize.y;
                float radius=0.04+min(sqrt(volume),2)*0.018;
                float offset=(d+0.025)/radius;
                return min(volume,2.8)*exp(-offset*offset);
            }
            float heightAt(float2 uv)
            {
                float h=max(0,tex2D(_HeightTex,uv).r);
                // Keep a readable wet edge on thin films without turning the whole canvas into a flat gloss.
                return h+sqrt(h)*0.55;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=i.uv, t=_HeightTex_TexelSize.xy*1.25;
                float rawHeight=max(0,tex2D(_HeightTex,uv).r);
                float h=rawHeight+sqrt(rawHeight)*0.55;
                float left=heightAt(uv-float2(t.x,0)), right=heightAt(uv+float2(t.x,0));
                float down=heightAt(uv-float2(0,t.y)), up=heightAt(uv+float2(0,t.y));
                float4 reservoir=0;
                float m=0, bladeShadow=0;
                // No blade is visible while drawing or on the result screen. Skip its texture
                // reads and exponential profile entirely on those frames, especially on phones.
                UNITY_BRANCH if (_BladeVisible>0.5)
                {
                    reservoir=tex2D(_ReservoirTex,float2(uv.x,0.5));
                    m=meniscus(uv.y,reservoir.a);
                    h+=m;
                    left+=meniscus(uv.y,tex2D(_ReservoirTex,float2(uv.x-t.x,0.5)).a);
                    right+=meniscus(uv.y,tex2D(_ReservoirTex,float2(uv.x+t.x,0.5)).a);
                    down+=meniscus(uv.y-t.y,reservoir.a);
                    up+=meniscus(uv.y+t.y,reservoir.a);
                    float bladeDistance=((uv.y-_BladeY)*_PaperSize.y+0.045)/0.08;
                    bladeShadow=0.18*exp(-bladeDistance*bladeDistance);
                }
                float gx=(right-left)*_Relief/(2.5*_PaperSize.z);
                float gy=(up-down)*_Relief/(2.5*_PaperSize.w);
                float3 n=normalize(float3(-gx,-gy,1));
                float3 light=normalize(float3(-0.5,0.65,0.7));
                float3 halfway=normalize(light+float3(0,0,1));
                float4 albedo=tex2D(_PigmentTex,uv);
                // The editor preview material has no live pigment texture yet.
                albedo*=step(0.000001,rawHeight);
                // Color already includes coverage, so bilinear filtering cannot add a white seam.
                albedo.rgb=lerp(albedo.rgb,reservoir.rgb,saturate(m*5));
                albedo.a=lerp(albedo.a,1,saturate(m*5));
                float coverage=lerp(1,albedo.a,_BackdropCanvas);
                albedo.rgb+= (1-albedo.a)*(1-_BackdropCanvas);
                albedo.rgb*=i.color.rgb;
                float painted=smoothstep(0.002,0.025,h);
                float diffuse=0.63+0.48*saturate(dot(n,light));
                float wetness=smoothstep(0.015,0.35,h);
                float broad=pow(saturate(dot(n,halfway)),lerp(14,24,wetness))*0.10;
                float sheen=pow(saturate(dot(n,halfway)),lerp(50,95,wetness))*_Gloss;
                float3 softbox=normalize(float3(0.09,0.13,1));
                float reflection=pow(saturate(dot(n,softbox)),80)*_Gloss*0.12;
                float shadow=0;
                [unroll] for(int k=1;k<=3;k++)
                {
                    float distance=k*0.03;
                    float2 offset=light.xy*distance/_PaperSize.xy;
                    float neighbor=heightAt(uv+offset)*_Relief;
                    float occlusion=saturate((neighbor-h*_Relief-distance*light.z)*25);
                    shadow=max(shadow,occlusion*(1-k*0.15));
                }
                float grain=(noise(floor(uv*_PaperSize.xy*120))-0.5)*0.008;
                float weave=sin(uv.x*3100)*sin(uv.y*4100)*0.003;
                float3 paperTint=float3(0.966,0.952,0.921)*(1+grain+weave);
                float3 base=albedo.rgb*lerp(paperTint,diffuse,painted);
                base*=1-shadow*_ShadowStrength;
                base*=1-bladeShadow;
                // Wet reflections retain pigment color and coverage instead of outlining every
                // boundary with additive white light, including where two paint colors meet.
                base+=albedo.rgb*(broad+sheen+reflection)*painted;
                return float4(max(base,0)*i.color.a,coverage*i.color.a);
            }
            ENDCG
        }
    }
}
