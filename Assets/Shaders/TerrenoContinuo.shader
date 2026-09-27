// Terreno contínuo: a cor de cada pixel vem das camadas do material lidas
// pela posição no mundo (16 px lógicos por unidade), não do sprite do tile.
// O sprite do tile é só uma máscara: alfa = forma, vermelho = índice da
// primeira camada do material, verde = linha da faixa (coberturas).
// Vizinhos mostram pedaços contíguos da mesma textura: não há emenda entre
// células do mesmo material. Três camadas por material se alternam por um
// campo de ruído suave lido por pixel (duas escalas).
Shader "Lithostride/TerrenoContinuo"
{
    Properties
    {
        [PerRendererData] _MainTex ("Máscara (sprite do tile)", 2D) = "white" {}
        _Layers ("Camadas dos materiais", 2DArray) = "" {}
        _Regions ("Campo de regiões (R, G)", 2D) = "gray" {}
        [Enum(Solido,0,Cobertura,1)] _Mode ("Modo", Float) = 0
        _LayersPerMaterial ("Camadas por material", Float) = 3
        _LayerSize ("Lado da camada (px)", Float) = 256
        _StripRows ("Linhas da faixa de cobertura", Float) = 12
        _Darken ("Multiplicador de cor (parede de fundo)", Color) = (1,1,1,1)
        _UseTint ("Usar tinta do mapa", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma require 2darray
            #pragma target 3.5
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            UNITY_DECLARE_TEX2DARRAY(_Layers);
            sampler2D _Regions;
            float _Mode;
            float _LayersPerMaterial;
            float _LayerSize;
            float _StripRows;
            fixed4 _Darken;
            float _UseTint;

            // Tinta por célula (umidade, profundidade), montada pelo TerrainGrid.
            sampler2D _TerrainTint;
            float4 _TerrainTintRect;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 world : TEXCOORD1;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                o.color = v.color;
                return o;
            }

            float Region(float2 p, float salt)
            {
                // Duas leituras em escalas diferentes (1 texel = 4 px e 5,6 px): período combinado muito longo.
                float a = tex2Dlod(_Regions, float4((p + 0.5 + salt) / (4.0 * _LayerSize), 0, 0)).r;
                float b = tex2Dlod(_Regions, float4((p * 0.71 + 0.5 + float2(97, 41) + salt * 0.5) / (4.0 * _LayerSize), 0, 0)).g;
                return a * 0.6 + b * 0.4;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 mask = tex2D(_MainTex, i.uv);
                clip(mask.a - 0.5);

                float2 px = floor(i.world * 16.0);
                float baseLayer = floor(mask.r * 255.0 + 0.5);
                float3 uvw;
                if (_Mode < 0.5)
                {
                    float n = Region(px, baseLayer * 37.0);
                    float variant = n < 0.52 ? 0.0 : (n < 0.8 ? 1.0 : 2.0);
                    variant = min(variant, _LayersPerMaterial - 1.0);
                    float2 t = frac((px + 0.5) / _LayerSize);
                    uvw = float3(t, baseLayer + variant);
                }
                else
                {
                    // Cobertura: a linha vem da máscara; a coluna, do x do mundo (contínua entre células e degraus).
                    float row = floor(mask.g * 255.0 + 0.5);
                    float n = Region(float2(px.x, 3.0), baseLayer * 13.0 + 200.0);
                    float variant = n < 0.5 ? 0.0 : 1.0;
                    uvw = float3(frac((px.x + 0.5) / _LayerSize), (_StripRows - 1.0 - row + 0.5) / _StripRows, baseLayer + variant);
                }

                fixed4 col = UNITY_SAMPLE_TEX2DARRAY_LOD(_Layers, uvw, 0);
                if (_UseTint > 0.5)
                {
                    float2 cell = floor(i.world) - _TerrainTintRect.xy;
                    float2 tuv = (cell + 0.5) / max(_TerrainTintRect.zw, 1.0);
                    col.rgb *= tex2Dlod(_TerrainTint, float4(tuv, 0, 0)).rgb;
                }

                col.rgb *= i.color.rgb * _Darken.rgb;
                col.a *= i.color.a;
                return col;
            }
            ENDCG
        }
    }
}
