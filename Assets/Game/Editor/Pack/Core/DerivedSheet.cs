using System.Collections.Generic;

namespace Lithostride.EditorTools
{
    /// <summary>Um sprite recortado de uma folha derivada, com tudo o que o manifesto registra.</summary>
    public sealed class DerivedSprite
    {
        /// <summary>Nome estável: identifica o sprite entre reconstruções.</summary>
        public string Name;

        /// <summary>Retângulo na folha derivada, origem no topo (como a imagem).</summary>
        public RectI Rect;

        /// <summary>Pivô normalizado, origem embaixo à esquerda (convenção da Unity).</summary>
        public float PivotX = 0.5f;
        public float PivotY = 0.5f;

        /// <summary>Arquivo de origem, relativo a "Pack completo".</summary>
        public string Source;

        /// <summary>Retângulo na fonte, origem no topo; vazio quando o sprite é composto (terreno).</summary>
        public RectI SourceRect;

        public string Sequence = "";
        public int Order;

        /// <summary>Função no jogo: "tile:solido", "tile:borda", "arvore", "efeito", ...</summary>
        public string Role = "";

        /// <summary>Ajustes aplicados ao derivado, em texto (limpeza de alfa, redução, plataforma removida...).</summary>
        public string Adjustments = "";

        /// <summary>Âncoras especiais em pixels relativos ao pivô, y para cima (ex.: "mao", "broche", "apoio").</summary>
        public readonly Dictionary<string, float[]> Anchors = new Dictionary<string, float[]>();

        /// <summary>Forma física (polígonos) em pixels relativos ao centro do retângulo, y para cima.</summary>
        public List<float[]> PhysicsShape;
    }

    /// <summary>Uma textura derivada, escrita em Assets, e os sprites recortados dela.</summary>
    public sealed class DerivedSheet
    {
        /// <summary>Caminho do PNG a partir da raiz do projeto (Assets/...).</summary>
        public string AssetPath;

        public PixelImage Image;

        public int PixelsPerUnit;

        /// <summary>Filtro Point (pixel art) ou bilinear (fundos pintados, interface reduzida).</summary>
        public bool PointFilter = true;

        public bool Mipmaps;

        /// <summary>Um sprite só (a imagem inteira) ou vários recortes.</summary>
        public bool Single;

        public string Category;

        /// <summary>
        /// Textura de dados (camadas do terreno, ruído de regiões): sem
        /// recortes, legível pelo editor para virar Texture2DArray, e com
        /// <see cref="Linear"/> os valores de cor chegam exatos ao shader.
        /// </summary>
        public bool TextureOnly;

        /// <summary>Sem conversão sRGB: o shader lê os canais como números (índice de camada, linha da faixa).</summary>
        public bool Linear;

        /// <summary>Importar como Texture2DArray: fatias em grade (colunas x linhas), da esquerda para a direita, de cima para baixo.</summary>
        public int ArrayColumns;

        public int ArrayRows;

        /// <summary>Repetir nas bordas (texturas contínuas lidas pela posição no mundo).</summary>
        public bool Repeat;

        /// <summary>Fontes usadas pela folha inteira (texturas de dados sem recortes, como as camadas do terreno).</summary>
        public readonly List<string> SheetSources = new List<string>();

        public readonly List<DerivedSprite> Sprites = new List<DerivedSprite>();

        public DerivedSprite Find(string name)
        {
            foreach (DerivedSprite sprite in Sprites)
            {
                if (sprite.Name == name)
                {
                    return sprite;
                }
            }

            return null;
        }
    }
}
