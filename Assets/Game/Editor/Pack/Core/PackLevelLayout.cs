using System;
using System.Collections.Generic;
using Lithostride.World;

namespace Lithostride.EditorTools
{
    /// <summary>Sprite de vegetação disponível para o layout (medidas em texels do ambiente).</summary>
    public sealed class PackVegetationInfo
    {
        public string Name;
        public string Kind;
        public int Width;
        public int Height;

        /// <summary>Apoio (tronco/raízes), em texels relativos ao pivô.</summary>
        public float SupportLeft;
        public float SupportRight;

        /// <summary>Largura à esquerda e à direita do pivô, em texels (copa inteira).</summary>
        public float ExtentLeft;
        public float ExtentRight;
    }

    /// <summary>Objeto colocado no mundo: árvore, prop ou item do catálogo.</summary>
    public sealed class PackPlacement
    {
        public string Sprite;
        public string Kind;
        public float X;
        public float Y;
        public bool Flip;
        public string Label;

        /// <summary>Células que sustentam o objeto; ao perder uma, ele reage.</summary>
        public readonly List<int[]> Support = new List<int[]>();
    }

    /// <summary>Região nomeada do mapa, em células (inclusive).</summary>
    public sealed class PackZone
    {
        public string Name;
        public int X0;
        public int Y0;
        public int X1;
        public int Y1;
    }

    /// <summary>Resultado do layout: terreno, objetos e pontos nomeados.</summary>
    public sealed class PackLevel
    {
        public TerrainCellMap Map;
        public readonly List<PackPlacement> Vegetation = new List<PackPlacement>();
        public readonly List<PackPlacement> Catalog = new List<PackPlacement>();
        public readonly Dictionary<string, float[]> Points = new Dictionary<string, float[]>();
        public readonly List<PackZone> Zones = new List<PackZone>();

        /// <summary>Rótulos das bancadas de terreno: (texto, x, y).</summary>
        public readonly List<PackPlacement> Labels = new List<PackPlacement>();
    }

    /// <summary>
    /// Monta o mapa da demonstração. Coordenadas em células (1 célula = 1
    /// unidade); a linha 0 é o chão do início (topo do chão em y = 1).
    /// Tudo é determinístico: reconstruir gera o mesmo mapa e os mesmos objetos.
    /// </summary>
    public static class PackLevelLayout
    {
        // Materiais (índice + 1 em TerrainArt.Materials).
        public const int Terra = 1, TerraEscura = 2, Pedra = 3, Cascalho = 4, Alvenaria = 5, PedraProfunda = 6, Cristal = 7,
            MinerioCristal = 8, Cobre = 9, Prata = 10, Ouro = 11, Vazio = 12, Areia = 13, Arenito = 14, Madeira = 15, Vulcanica = 16;

        public const int MinX = -26, MaxX = 940, MinY = -48, MaxY = 64;

        /// <summary>Árvores e props afundam 2 texels (1/12 de unidade) na grama: a cobertura esconde a emenda.</summary>
        public const float Sink = 2f / PackSpec.EnvironmentPixelsPerUnit;

        private const float Texel = 1f / PackSpec.EnvironmentPixelsPerUnit;

        public static PackLevel Build(List<PackVegetationInfo> vegetation, List<PackVegetationInfo> props,
            List<string> blockIcons, List<string> energyIcons)
        {
            PackLevel level = new PackLevel { Map = new TerrainCellMap(MinX, MinY, MaxX - MinX + 1, MaxY - MinY + 1) };
            TerrainCellMap map = level.Map;

            Start(level);
            Course(level);
            Cave(level);
            Training(level);
            Arena(level);
            Benches(level);
            Gallery(level, vegetation, props, blockIcons, energyIcons);
            Viewer(level);

            // Parede de fundo atrás de tudo o que começa sólido (e do ar da caverna, marcado em Cave).
            for (int y = MinY; y <= MaxY; y++)
            {
                for (int x = MinX; x <= MaxX; x++)
                {
                    int m = map.MaterialAt(x, y);
                    if (m != 0 && map.WallAt(x, y) == 0 && (IsBelowSurface(map, x, y) || (y < 0 && map.ShapeAt(x, y) != TerrainShape.Full)))
                    {
                        map.SetWall(x, y, m);
                    }
                }
            }

            Scatter(level, vegetation, props);
            return level;
        }

        // ------------------------------------------------------------------ regiões

        private static void Start(PackLevel level)
        {
            TerrainCellMap map = level.Map;
            Ground(map, MinX, 47, 0, 14);
            Box(map, MinX, 1, MinX + 2, 26, Pedra, TerrainShape.Full);
            level.Points["spawn"] = new[] { 4f, 1f };
            level.Points["destino:Início"] = new[] { 4f, 1f };
            level.Zones.Add(new PackZone { Name = "fundo:dia", X0 = MinX, Y0 = -4, X1 = 139, Y1 = MaxY });
            level.Zones.Add(new PackZone { Name = "livre:spawn", X0 = 0, Y0 = 0, X1 = 9, Y1 = 8 });
        }

        private static void Course(PackLevel level)
        {
            TerrainCellMap map = level.Map;
            level.Points["destino:Percurso"] = new[] { 44f, 1f };

            // Degraus de 1 bloco até o platô (y = 3), subidos andando.
            Ground(map, 44, 45, 1, 14);
            Ground(map, 46, 47, 2, 14);
            Ground(map, 48, 51, 3, 14);
            // Platô com transição de materiais na superfície: terra -> cascalho -> pedra.
            Ground(map, 52, 54, 3, 14, Cascalho);
            Ground(map, 55, 58, 3, 14, Pedra);

            // Rampa descendo à direita (sobe para a esquerda), de y = 3 a 0.
            Ground(map, 59, 59, 2, 14, Pedra);
            map.Set(59, 3, Pedra, TerrainShape.SlopeUpLeft);
            Ground(map, 60, 60, 1, 14);
            map.Set(60, 2, Terra, TerrainShape.SlopeUpLeft);
            Ground(map, 61, 61, 0, 14);
            map.Set(61, 1, Terra, TerrainShape.SlopeUpLeft);
            Ground(map, 62, 67, 0, 14);

            // Fosso de 4 x 4 com plataforma solta por cima.
            Ground(map, 68, 71, -4, 10);
            Box(map, 69, 3, 70, 3, Alvenaria, TerrainShape.Full);

            // Areia com monte de meios-blocos.
            Ground(map, 72, 81, 0, 14, Areia);
            map.Set(76, 1, Areia, TerrainShape.HalfBottom);
            Box(map, 77, 1, 79, 1, Areia, TerrainShape.Full);
            map.Set(80, 1, Areia, TerrainShape.HalfBottom);

            // Rampa subindo (arenito) até o platô de y = 4.
            for (int i = 0; i < 4; i++)
            {
                Ground(map, 82 + i, 82 + i, i, 14, Arenito);
                map.Set(82 + i, i + 1, Arenito, TerrainShape.SlopeUpRight);
            }

            Ground(map, 86, 93, 4, 14, Arenito);

            // Ponte de madeira sobre um vão, e descida em degraus.
            Ground(map, 94, 99, -2, 12);
            Box(map, 94, 4, 99, 4, Madeira, TerrainShape.Full);
            Ground(map, 100, 101, 4, 14);
            Ground(map, 102, 102, 3, 14);
            Ground(map, 103, 103, 2, 14);
            Ground(map, 104, 104, 1, 14);
            Ground(map, 105, 139, 0, 14);

            // Plataformas soltas em alturas de pulo (3 blocos).
            Box(map, 106, 3, 108, 3, Pedra, TerrainShape.Full);
            Box(map, 110, 6, 111, 6, Alvenaria, TerrainShape.Full);
            Box(map, 113, 9, 115, 9, Cristal, TerrainShape.Full);

            // Marquise solta: laje de pedra com base exposta; passa-se por baixo (4 blocos livres).
            Box(map, 117, 5, 121, 6, Pedra, TerrainShape.Full);

            // Morro 8x8 de terra com buraco interno; rampa de 45 graus subindo e descendo.
            Box(map, 130, 1, 137, 8, Terra, TerrainShape.Full);
            Clear(map, 133, 4, 134, 5);
            for (int i = 0; i < 8; i++)
            {
                if (i < 7) Box(map, 129 - i, 1, 129 - i, 7 - i, Terra, TerrainShape.Full);
                map.Set(129 - i, 8 - i, Terra, TerrainShape.SlopeUpRight);
            }

            for (int i = 0; i < 8; i++)
            {
                if (i < 7) Box(map, 138 + i, 1, 138 + i, 7 - i, Terra, TerrainShape.Full);
                map.Set(138 + i, 8 - i, Terra, TerrainShape.SlopeUpLeft);
            }
        }

        private static void Cave(PackLevel level)
        {
            TerrainCellMap map = level.Map;
            // Maciço da caverna: pedra, com pedra profunda embaixo.
            Ground(map, 140, 204, 0, 48);
            for (int x = 140; x <= 204; x++)
            {
                for (int y = MinY; y <= -8; y++)
                {
                    int depth = -y + (TerrainRules.Hash(x / 5, 0, 3) % 3);
                    map.Set(x, y, depth > 30 ? PedraProfunda : depth > 10 ? Pedra : map.MaterialAt(x, y) == Terra ? Cascalho : Pedra,
                        TerrainShape.Full);
                }
            }

            // Túnel em rampa de 45 graus: cada rampa ocupa a linha do piso da
            // coluna anterior, então a borda alta dela encosta no piso de trás.
            int floor = 0;
            for (int x = 147; x <= 172; x++)
            {
                int slopeRow = floor;
                ClearAir(level, x, slopeRow + 1, x, slopeRow + 7);
                int below = map.MaterialAt(x, slopeRow - 1);
                map.Set(x, slopeRow, below == 0 ? Pedra : below, TerrainShape.SlopeUpLeft);
                floor = slopeRow - 1;
            }

            // Salão: chão em y = -26, teto irregular.
            for (int x = 173; x <= 200; x++)
            {
                int ceiling = -14 - (TerrainRules.Hash(x, 1, 5) % 3);
                ClearAir(level, x, -25, x, ceiling);
            }

            // Estalagmites e degrau no salão.
            Box(map, 178, -25, 178, -23, Pedra, TerrainShape.Full);
            Box(map, 186, -25, 187, -24, PedraProfunda, TerrainShape.Full);
            map.Set(188, -24, PedraProfunda, TerrainShape.HalfBottom);

            // Veios de minério nas paredes do salão e do túnel.
            Vein(map, 168, -30, 3, Cobre);
            Vein(map, 176, -29, 3, Prata);
            Vein(map, 184, -30, 3, Ouro);
            Vein(map, 194, -29, 3, MinerioCristal);
            Vein(map, 200, -22, 3, Vazio);
            Vein(map, 196, -12, 3, Vulcanica);
            Vein(map, 172, -12, 3, Cristal);
            Vein(map, 180, -11, 3, TerraEscura);
            Vein(map, 158, -18, 3, Cobre);
            Vein(map, 152, -6, 2, Prata);

            level.Points["destino:Caverna"] = new[] { 180f, -25f };
            level.Zones.Add(new PackZone { Name = "fundo:caverna", X0 = 144, Y0 = MinY, X1 = 204, Y1 = -3 });
            level.Zones.Add(new PackZone { Name = "livre:caverna", X0 = 142, Y0 = -30, X1 = 204, Y1 = 12 });
        }

        private static void Training(PackLevel level)
        {
            TerrainCellMap map = level.Map;
            Ground(map, 205, 252, 0, 14);
            level.Points["destino:Treino"] = new[] { 212f, 1f };
            level.Points["alvo:1"] = new[] { 224f, 1f };
            level.Points["alvo:2"] = new[] { 232f, 1f };
            level.Points["alvo:3"] = new[] { 240f, 1f };
            level.Zones.Add(new PackZone { Name = "fundo:dia", X0 = 205, Y0 = -4, X1 = 252, Y1 = MaxY });
            level.Zones.Add(new PackZone { Name = "livre:treino", X0 = 218, Y0 = 0, X1 = 246, Y1 = 10 });
        }

        private static void Arena(PackLevel level)
        {
            TerrainCellMap map = level.Map;
            Ground(map, 253, 336, 0, 14);
            // Paredes: a da esquerda tem porta de 5 blocos de altura para o jogador de 3,4.
            Box(map, 256, 6, 258, 24, Alvenaria, TerrainShape.Full);
            Box(map, 332, 1, 334, 24, Alvenaria, TerrainShape.Full);
            // Plataformas para alcançar o boss no ar.
            Box(map, 272, 4, 276, 4, Alvenaria, TerrainShape.Full);
            Box(map, 290, 8, 294, 8, Alvenaria, TerrainShape.Full);
            Box(map, 308, 4, 312, 4, Alvenaria, TerrainShape.Full);

            level.Points["destino:Arena"] = new[] { 250f, 1f };
            level.Points["arena:inicio"] = new[] { 262f, 1f };
            level.Points["boss:casa"] = new[] { 300f, 12f };
            level.Zones.Add(new PackZone { Name = "arena", X0 = 260, Y0 = 1, X1 = 330, Y1 = 22 });
            level.Zones.Add(new PackZone { Name = "boss:voo", X0 = 264, Y0 = 5, X1 = 327, Y1 = 19 });
            level.Zones.Add(new PackZone { Name = "fundo:entardecer", X0 = 253, Y0 = -4, X1 = 338, Y1 = MaxY });
            level.Zones.Add(new PackZone { Name = "livre:arena", X0 = 253, Y0 = 0, X1 = 338, Y1 = 30 });
        }

        /// <summary>
        /// Bancadas de conexão: para cada material, bloco 8x8 com buraco
        /// (miolo, cantos internos), coluna, faixa solta, meio-bloco e morro
        /// com rampas nos dois lados, sobre chão de pedra (transição de material).
        /// </summary>
        private static void Benches(PackLevel level)
        {
            TerrainCellMap map = level.Map;
            int count = TerrainArt.Materials.Length;
            int start = 342;
            Ground(map, 337, start + (count * 20) + 4, 0, 10, Pedra);
            level.Zones.Add(new PackZone { Name = "fundo:dia", X0 = 337, Y0 = -4, X1 = start + (count * 20) + 4, Y1 = MaxY });

            for (int i = 0; i < count; i++)
            {
                int m = i + 1, x0 = start + (i * 20);
                Box(map, x0, 1, x0 + 7, 8, m, TerrainShape.Full);
                Clear(map, x0 + 3, 4, x0 + 4, 5);
                Box(map, x0 + 9, 1, x0 + 9, 5, m, TerrainShape.Full);
                Box(map, x0 + 10, 8, x0 + 14, 8, m, TerrainShape.Full);
                map.Set(x0 + 11, 1, m, TerrainShape.HalfBottom);

                // Morro: rampa sobe, topo de 2, rampa desce.
                map.Set(x0 + 13, 1, m, TerrainShape.SlopeUpRight);
                Box(map, x0 + 14, 1, x0 + 15, 1, m, TerrainShape.Full);
                map.Set(x0 + 14, 2, m, TerrainShape.SlopeUpRight);
                map.Set(x0 + 15, 2, m, TerrainShape.SlopeUpLeft);
                map.Set(x0 + 16, 1, m, TerrainShape.SlopeUpLeft);

                level.Labels.Add(new PackPlacement
                {
                    Label = TerrainArt.Materials[i].DisplayName + " (" + TerrainArt.Materials[i].Id + ")",
                    X = x0 + 4f,
                    Y = 10.2f,
                    Kind = "rotulo"
                });

                if (i % 4 == 0)
                {
                    level.Points["destino:Bancadas " + (i + 1) + "-" + (i + 4)] = new[] { x0 - 1.5f, 1f };
                }
            }
        }

        /// <summary>
        /// Galeria: prateleiras de terra com todas as árvores e props (em pé, no
        /// chão, com rótulo) e painéis com todos os ícones de blocos e de energia.
        /// </summary>
        private static void Gallery(PackLevel level, List<PackVegetationInfo> vegetation, List<PackVegetationInfo> props,
            List<string> blockIcons, List<string> energyIcons)
        {
            TerrainCellMap map = level.Map;
            int x0 = 680;
            int[] shelves = { 0, 16, 32, 48 };
            string[] sheets = { "arvore1_", "arvore2_", "arvore3_", "arvore4_" };
            Ground(map, 676, 800, 0, 8);
            level.Zones.Add(new PackZone { Name = "fundo:dia", X0 = 666, Y0 = -4, X1 = MaxX, Y1 = MaxY });

            for (int s = 0; s < sheets.Length; s++)
            {
                float x = x0;
                List<PackVegetationInfo> row = new List<PackVegetationInfo>();
                foreach (PackVegetationInfo v in vegetation)
                {
                    if (v.Name.StartsWith(sheets[s], StringComparison.Ordinal)) row.Add(v);
                }

                if (s == 3)
                {
                    row.AddRange(props);
                }

                float width = 0f;
                foreach (PackVegetationInfo v in row) width += ((v.ExtentLeft + v.ExtentRight) * Texel) + 1.5f;
                int shelfY = shelves[s];
                if (s > 0)
                {
                    Box(map, x0 - 2, shelfY, x0 + (int)Math.Ceiling(width) + 2, shelfY, Terra, TerrainShape.Full);
                }

                foreach (PackVegetationInfo v in row)
                {
                    x += (v.ExtentLeft * Texel) + 0.75f;
                    float px = (float)Math.Round(x * PackSpec.EnvironmentPixelsPerUnit) / PackSpec.EnvironmentPixelsPerUnit;
                    PackPlacement p = new PackPlacement
                    {
                        Sprite = v.Name,
                        Kind = v.Kind,
                        X = px,
                        Y = shelfY + 1 - Sink,
                        Label = v.Name
                    };
                    AddSupport(p, v, px, shelfY);
                    level.Catalog.Add(p);
                    x += (v.ExtentRight * Texel) + 0.75f;
                }

                level.Points["destino:Galeria " + (s + 1)] = new[] { x0 - 1f, shelfY + 1f };
            }

            // Painéis de ícones: blocos na grade da prancha (linha/coluna do nome), energia numa fileira.
            float panelX = 820f;
            foreach (string icon in blockIcons)
            {
                // Nome: blocosS_lLL_cCC.
                int sheet = icon[6] - '0';
                int row = int.Parse(icon.Substring(9, 2));
                int col = int.Parse(icon.Substring(13, 2));
                level.Catalog.Add(new PackPlacement
                {
                    Sprite = icon,
                    Kind = "icone",
                    X = panelX + ((sheet - 1) * 16f) + (col * 1.2f),
                    Y = 30f - (row * 1.6f),
                    Label = icon
                });
            }

            for (int i = 0; i < energyIcons.Count; i++)
            {
                level.Catalog.Add(new PackPlacement
                {
                    Sprite = energyIcons[i],
                    Kind = "icone",
                    X = panelX + (i * 1.6f),
                    Y = 8f,
                    Label = energyIcons[i]
                });
            }

            Ground(map, 805, 870, 0, 8, Pedra);
            level.Points["destino:Ícones"] = new[] { 832f, 1f };
        }

        private static void Viewer(PackLevel level)
        {
            Ground(level.Map, 871, MaxX, 0, 8, Pedra);
            Box(level.Map, MaxX - 2, 1, MaxX, 26, Pedra, TerrainShape.Full);
            level.Points["destino:Visualizador"] = new[] { 876f, 1f };
            level.Points["visualizador:origem"] = new[] { 880f, 1f };
        }

        // ------------------------------------------------------------------ vegetação natural

        /// <summary>
        /// Espalha árvores e props nas áreas abertas: só em topo gramado plano,
        /// com toda a largura de apoio sobre blocos inteiros da mesma altura,
        /// espaço livre acima da copa, distância mínima e semente fixa.
        /// </summary>
        private static void Scatter(PackLevel level, List<PackVegetationInfo> vegetation, List<PackVegetationInfo> props)
        {
            TerrainCellMap map = level.Map;
            Random random = new Random(20260926);
            List<float[]> taken = new List<float[]>();
            List<PackVegetationInfo> trees = vegetation.FindAll(v => v.Kind.StartsWith("arvore", StringComparison.Ordinal));
            List<PackVegetationInfo> stumps = vegetation.FindAll(v => v.Kind == "toco");

            int[][] ranges = { new[] { -20, 42 }, new[] { 62, 67 }, new[] { 105, 120 }, new[] { 205, 216 }, new[] { 244, 252 } };
            foreach (int[] range in ranges)
            {
                for (float x = range[0]; x <= range[1]; x += 2.5f + (float)(random.NextDouble() * 4.0))
                {
                    bool stump = random.NextDouble() < 0.18;
                    List<PackVegetationInfo> pool = stump ? stumps : trees;
                    TryPlace(level, pool[random.Next(pool.Count)], x, random, taken, 1.0f);
                }
            }

            for (int i = 0; i < 260; i++)
            {
                int[] range = ranges[random.Next(ranges.Length)];
                float x = range[0] + (float)(random.NextDouble() * (range[1] - range[0]));
                TryPlace(level, props[random.Next(props.Count)], x, random, taken, 0.2f);
            }
        }

        private static void TryPlace(PackLevel level, PackVegetationInfo v, float x, Random random, List<float[]> taken, float gap)
        {
            TerrainCellMap map = level.Map;
            // O espelho é decidido antes: apoio e copa espelham em volta do pivô.
            bool flip = random.Next(2) == 0;
            v = flip ? Mirror(v) : v;
            float px = (float)Math.Round(x * PackSpec.EnvironmentPixelsPerUnit) / PackSpec.EnvironmentPixelsPerUnit;
            int cell = (int)Math.Floor(px);
            int surface = Surface(map, cell);
            if (surface == int.MinValue || map.MaterialAt(cell, surface) != Terra)
            {
                return;
            }

            float left = px + (v.SupportLeft * Texel), right = px + (v.SupportRight * Texel);
            for (int c = (int)Math.Floor(left); c <= (int)Math.Floor(right - 0.001f); c++)
            {
                if (map.ShapeAt(c, surface) != TerrainShape.Full || map.ShapeAt(c, surface + 1) != TerrainShape.Empty)
                {
                    return;
                }
            }

            float extentL = px - (v.ExtentLeft * Texel), extentR = px + (v.ExtentRight * Texel);
            float top = surface + 1 + (v.Height * Texel);
            for (int c = (int)Math.Floor(extentL); c <= (int)Math.Floor(extentR); c++)
            {
                for (int y = surface + 1; y <= (int)Math.Ceiling(top); y++)
                {
                    if (map.MaterialAt(c, y) != 0)
                    {
                        return;
                    }
                }
            }

            foreach (PackZone zone in level.Zones)
            {
                if (zone.Name.StartsWith("livre:", StringComparison.Ordinal) && extentR >= zone.X0 && extentL <= zone.X1 + 1 &&
                    surface >= zone.Y0 && surface <= zone.Y1)
                {
                    return;
                }
            }

            bool tree = v.Kind.StartsWith("arvore", StringComparison.Ordinal) || v.Kind == "toco";
            foreach (float[] t in taken)
            {
                bool otherTree = t[3] > 0.5f;
                float needed = tree && otherTree ? gap : 0.1f;
                if (extentL < t[1] + needed && extentR > t[0] - needed && (tree == otherTree || !tree))
                {
                    if (tree || Math.Abs(t[2] - px) < 1.2f || (otherTree && Math.Abs(t[2] - px) < 0.6f))
                    {
                        return;
                    }
                }
            }

            PackPlacement p = new PackPlacement
            {
                Sprite = v.Name,
                Kind = v.Kind,
                X = px,
                Y = surface + 1 - Sink,
                Flip = flip,
                Label = v.Name
            };
            AddSupport(p, v, px, surface);
            level.Vegetation.Add(p);
            taken.Add(new[] { extentL, extentR, px, tree ? 1f : 0f });
        }

        private static PackVegetationInfo Mirror(PackVegetationInfo v)
        {
            return new PackVegetationInfo
            {
                Name = v.Name,
                Kind = v.Kind,
                Width = v.Width,
                Height = v.Height,
                SupportLeft = -v.SupportRight,
                SupportRight = -v.SupportLeft,
                ExtentLeft = v.ExtentRight,
                ExtentRight = v.ExtentLeft
            };
        }

        private static void AddSupport(PackPlacement p, PackVegetationInfo v, float px, int surface)
        {
            float left = px + (v.SupportLeft * Texel), right = px + (v.SupportRight * Texel);
            for (int c = (int)Math.Floor(left); c <= (int)Math.Floor(right - 0.001f); c++)
            {
                p.Support.Add(new[] { c, surface });
            }
        }

        /// <summary>Linha da célula mais alta sólida na coluna (int.MinValue se não houver).</summary>
        private static int Surface(TerrainCellMap map, int x)
        {
            for (int y = MaxY; y >= MinY; y--)
            {
                if (map.MaterialAt(x, y) != 0)
                {
                    return y;
                }
            }

            return int.MinValue;
        }

        // ------------------------------------------------------------------ primitivas

        /// <summary>Chão de x0 a x1 com topo em <paramref name="top"/>: terra por cima, pedra a partir de 5 abaixo.</summary>
        private static void Ground(TerrainCellMap map, int x0, int x1, int top, int depth, int surfaceMaterial = Terra)
        {
            for (int x = x0; x <= x1; x++)
            {
                for (int y = top - depth + 1; y <= top; y++)
                {
                    int below = top - y;
                    int boundary = 5 + (TerrainRules.Hash(x / 4, 0, 2) % 2);
                    int m = below < boundary ? surfaceMaterial : surfaceMaterial == Terra ? Pedra : surfaceMaterial;
                    if (surfaceMaterial == Terra && below >= boundary && below < boundary + 2)
                    {
                        m = TerraEscura;
                    }

                    map.Set(x, y, m, TerrainShape.Full);
                }
            }
        }

        private static void Box(TerrainCellMap map, int x0, int y0, int x1, int y1, int material, TerrainShape shape)
        {
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    map.Set(x, y, material, shape);
                }
            }
        }

        private static void Clear(TerrainCellMap map, int x0, int y0, int x1, int y1)
        {
            Box(map, x0, y0, x1, y1, 0, TerrainShape.Empty);
        }

        /// <summary>Abre ar dentro do maciço e põe parede atrás (não mostra céu pela caverna).</summary>
        private static void ClearAir(PackLevel level, int x0, int y0, int x1, int y1)
        {
            TerrainCellMap map = level.Map;
            for (int x = Math.Min(x0, x1); x <= Math.Max(x0, x1); x++)
            {
                for (int y = Math.Min(y0, y1); y <= Math.Max(y0, y1); y++)
                {
                    int m = map.MaterialAt(x, y);
                    if (m != 0 && y < 0)
                    {
                        map.SetWall(x, y, m);
                    }

                    map.Set(x, y, 0, TerrainShape.Empty);
                }
            }
        }

        /// <summary>Veio de minério: disco irregular de raio ~r, só onde já há rocha.</summary>
        private static void Vein(TerrainCellMap map, int cx, int cy, int r, int material)
        {
            for (int x = cx - r - 1; x <= cx + r + 1; x++)
            {
                for (int y = cy - r - 1; y <= cy + r + 1; y++)
                {
                    float d = (float)Math.Sqrt(((x - cx) * (x - cx)) + ((y - cy) * (y - cy) * 1.4f));
                    float wobble = (TerrainRules.Hash(x, y, 9) % 100) / 100f;
                    if (d <= r + (wobble * 0.8f) - 0.3f && map.ShapeAt(x, y) == TerrainShape.Full)
                    {
                        map.Set(x, y, material, TerrainShape.Full);
                    }
                }
            }
        }

        private static bool IsBelowSurface(TerrainCellMap map, int x, int y)
        {
            // Sólido com sólido acima em até 2 células: o miolo do chão (não plataformas nem colunas soltas no céu).
            return y <= 0 && map.MaterialAt(x, y + 1) != 0;
        }
    }
}
