using System;
using System.Collections.Generic;
using Lithostride.World;

namespace Lithostride.EditorTools
{
    /// <summary>Parâmetros do mundo natural (em células).</summary>
    public sealed class NaturalWorldSettings
    {
        public int Seed = 1;
        public int Width = 720;
        public int Depth = 190;
        public int Sky = 70;

        /// <summary>Coluna do início (área segura, quase plana).</summary>
        public int SpawnX = 70;

        /// <summary>Maior desnível entre colunas vizinhas na superfície: o pulo sobe ~3,7 células.</summary>
        public int MaxStep = 3;

        /// <summary>Largura das falésias naturais nas bordas do mundo (a câmera para antes delas).</summary>
        public int EdgeRidge = 34;
    }

    /// <summary>
    /// Gera o terreno natural por semente: nada aqui é trecho fixo. Mesma
    /// semente, mesmo mundo (só <see cref="Random"/> com semente e ruído
    /// determinístico). Etapas:
    /// <list type="number">
    /// <item>perfil da superfície em quatro escalas (colinas largas, ombros,
    /// ondulação, detalhe), com deslocamento do domínio (encostas
    /// assimétricas), platôs com borda suave, depressões rasas e área do
    /// início quase plana; o desnível entre colunas é limitado ao que o pulo
    /// alcança, e as bordas do mundo sobem em falésias;</item>
    /// <item>rampas só em parte dos degraus de uma célula (não dominam o morro);</item>
    /// <item>solo de espessura variável (3 a 12), terra escura e cascalho em
    /// manchas, fronteira irregular com a pedra, pedra profunda embaixo;</item>
    /// <item>veios de minério por profundidade;</item>
    /// <item>cavernas: túneis curvos de raio variável, câmaras de tamanhos
    /// diferentes com pilares, entradas a partir da superfície; limpeza de
    /// paredes e tetos finos e de ilhas soltas;</item>
    /// <item>parede de fundo atrás de todo ar abaixo da superfície; umidade
    /// perto das cavernas e nas depressões; pedra com musgo em cavernas úmidas.</item>
    /// </list>
    /// </summary>
    public static class NaturalWorld
    {
        public const int Terra = 1, TerraEscura = 2, Pedra = 3, Cascalho = 4, Alvenaria = 5, PedraProfunda = 6, Cristal = 7,
            MinerioCristal = 8, Cobre = 9, Prata = 10, Ouro = 11, Vazio = 12, Areia = 13, Arenito = 14, Madeira = 15, Vulcanica = 16,
            PedraMusgosa = 17;

        /// <summary>Resultado: mapa, alturas da superfície natural e o que o layout precisa saber.</summary>
        public sealed class Result
        {
            public TerrainCellMap Map;
            public int[] Heights;
            public NaturalWorldSettings Settings;
            public readonly List<int[]> CaveEntrances = new List<int[]>();
            public readonly List<int[]> Chambers = new List<int[]>();
            public int MinX => Map.OriginX;
            public int MaxX => Map.OriginX + Map.Width - 1;
        }

        public static Result Generate(NaturalWorldSettings settings)
        {
            Result result = new Result { Settings = settings };
            TerrainCellMap map = new TerrainCellMap(0, -settings.Depth, settings.Width, settings.Depth + settings.Sky) { Seed = settings.Seed };
            result.Map = map;
            Random random = new Random(settings.Seed);

            int[] heights = Surface(settings, random);
            result.Heights = heights;
            Fill(map, heights, settings);
            Ores(map, heights, settings, new Random(settings.Seed * 7 + 1));
            Slopes(map, heights, settings);
            map.RecordSurface();

            bool[] cave = Caves(map, heights, settings, new Random(settings.Seed * 13 + 5), result);
            Walls(map, heights, cave);
            Humidity(map, heights, cave, settings);
            return result;
        }

        // ------------------------------------------------------------------ ruído determinístico

        /// <summary>Ruído de valor 1D suave (-1..1), período de rede <paramref name="wavelength"/>.</summary>
        public static double Noise1(double x, double wavelength, int salt)
        {
            double f = x / wavelength;
            int i = (int)Math.Floor(f);
            double t = f - i;
            t = t * t * (3 - (2 * t));
            double a = Lattice(i, salt), b = Lattice(i + 1, salt);
            return a + ((b - a) * t);
        }

        /// <summary>Ruído de valor 2D suave (-1..1).</summary>
        public static double Noise2(double x, double y, double wavelength, int salt)
        {
            double fx = x / wavelength, fy = y / wavelength;
            int ix = (int)Math.Floor(fx), iy = (int)Math.Floor(fy);
            double tx = fx - ix, ty = fy - iy;
            tx = tx * tx * (3 - (2 * tx));
            ty = ty * ty * (3 - (2 * ty));
            double a = Lattice2(ix, iy, salt), b = Lattice2(ix + 1, iy, salt);
            double c = Lattice2(ix, iy + 1, salt), d = Lattice2(ix + 1, iy + 1, salt);
            return ((a + ((b - a) * tx)) * (1 - ty)) + ((c + ((d - c) * tx)) * ty);
        }

        private static double Lattice(int i, int salt)
        {
            return ((TerrainRules.Hash(i, 0, salt) % 20001) / 10000.0) - 1.0;
        }

        private static double Lattice2(int i, int j, int salt)
        {
            return ((TerrainRules.Hash(i, j, salt) % 20001) / 10000.0) - 1.0;
        }

        private static double Fbm2(double x, double y, double wavelength, int salt)
        {
            return (Noise2(x, y, wavelength, salt) * 0.6) + (Noise2(x, y, wavelength * 0.45, salt + 1) * 0.3) +
                   (Noise2(x, y, wavelength * 0.2, salt + 2) * 0.1);
        }

        // ------------------------------------------------------------------ superfície

        private static int[] Surface(NaturalWorldSettings s, Random random)
        {
            int salt = s.Seed * 101;
            double[] h = new double[s.Width];
            for (int x = 0; x < s.Width; x++)
            {
                // Deslocamento do domínio: encostas de um lado mais íngremes que do outro.
                double warp = Noise1(x, 70, salt + 9) * 16;
                double wx = x + warp;
                double v = (Noise1(wx, 160, salt + 1) * 16) + (Noise1(wx, 58, salt + 2) * 7.5) + (Noise1(wx, 21, salt + 3) * 3) +
                           (Noise1(x, 8, salt + 4) * 1.0);

                // Platôs: onde a máscara é alta, a altura é puxada para o degrau de 5 mais próximo (topo quase plano, ombro suave).
                double plateau = Math.Max(0, Noise1(x, 90, salt + 5));
                if (plateau > 0.25)
                {
                    double stepped = Math.Round(v / 5.0) * 5.0 + (Noise1(x, 11, salt + 6) * 0.6);
                    double k = Math.Min(1, (plateau - 0.25) / 0.3);
                    v = (v * (1 - k)) + (stepped * k);
                }

                h[x] = v;
            }

            // Colinas assimétricas: cada lado com largura e curvatura próprias (ombro de um lado, encosta longa do outro).
            int hills = s.Width / 95;
            for (int k = 0; k < hills; k++)
            {
                int cx = random.Next(s.EdgeRidge + 25, s.Width - s.EdgeRidge - 25);
                if (Math.Abs(cx - s.SpawnX) < 50) continue;
                double height = 6 + (random.NextDouble() * 16);
                double left = (height * 1.6) + (random.NextDouble() * 28), right = (height * 1.6) + (random.NextDouble() * 28);
                double shapeL = 1.2 + random.NextDouble(), shapeR = 1.2 + random.NextDouble();
                for (int x = Math.Max(0, cx - (int)left); x < Math.Min(s.Width, cx + (int)right); x++)
                {
                    double t = x < cx ? 1 - ((cx - x) / left) : 1 - ((x - cx) / right);
                    t = Math.Max(0, t);
                    double p = x < cx ? shapeL : shapeR;
                    h[x] += height * Math.Pow(t * t * (3 - (2 * t)), p * 0.7);
                }
            }

            // Depressões rasas: vales curtos de 2 a 6 células.
            int dips = s.Width / 70;
            for (int d = 0; d < dips; d++)
            {
                int cx = random.Next(s.EdgeRidge + 20, s.Width - s.EdgeRidge - 20);
                if (Math.Abs(cx - s.SpawnX) < 40) continue;
                double width = 6 + (random.NextDouble() * 12), depth = 2 + (random.NextDouble() * 4);
                double skew = 0.6 + (random.NextDouble() * 0.8);
                for (int x = Math.Max(0, cx - 30); x < Math.Min(s.Width, cx + 30); x++)
                {
                    double t = (x - cx) / (x < cx ? width : width * skew);
                    h[x] -= depth * Math.Exp(-t * t);
                }
            }

            // Início: quase plano (±1) numa faixa larga, com transição suave.
            double baseSpawn = h[s.SpawnX];
            for (int x = 0; x < s.Width; x++)
            {
                double dist = Math.Abs(x - s.SpawnX);
                double k = dist < 18 ? 1 : dist < 34 ? 1 - ((dist - 18) / 16.0) : 0;
                k = k * k * (3 - (2 * k));
                h[x] = (h[x] * (1 - k)) + ((baseSpawn + (Noise1(x, 7, salt + 7) * 0.45)) * k);
            }

            // Bordas: falésias naturais sobem (a câmera para antes delas).
            for (int x = 0; x < s.Width; x++)
            {
                int edge = Math.Min(x, s.Width - 1 - x);
                if (edge < s.EdgeRidge)
                {
                    // Falésia rochosa: sobe forte perto da borda, com degraus e ressaltos irregulares.
                    double t = 1 - (edge / (double)s.EdgeRidge);
                    h[x] += (Math.Pow(t, 2.2) * 46) + (Noise1(x, 4, salt + 8) * 3 * t) + (Noise1(x, 11, salt + 10) * 4 * t);
                }
            }

            int[] heights = new int[s.Width];
            for (int x = 0; x < s.Width; x++)
            {
                heights[x] = (int)Math.Round(h[x]);
            }

            // Sem espetos nem poços de uma coluna; desnível que o pulo alcança (fora das falésias).
            for (int pass = 0; pass < 3; pass++)
            {
                for (int x = 1; x < s.Width - 1; x++)
                {
                    int left = heights[x - 1], right = heights[x + 1];
                    if (heights[x] - Math.Max(left, right) >= 2) heights[x] = Math.Max(left, right) + 1;
                    if (Math.Min(left, right) - heights[x] >= 2) heights[x] = Math.Min(left, right) - 1;
                }
            }

            for (int x = 1; x < s.Width; x++)
            {
                if (Math.Min(x, s.Width - 1 - x) < s.EdgeRidge - 6) continue;
                int diff = heights[x] - heights[x - 1];
                if (Math.Abs(diff) > s.MaxStep)
                {
                    heights[x] = heights[x - 1] + (Math.Sign(diff) * s.MaxStep);
                }
            }

            for (int x = s.Width - 2; x >= 0; x--)
            {
                if (Math.Min(x, s.Width - 1 - x) < s.EdgeRidge - 6) continue;
                int diff = heights[x] - heights[x + 1];
                if (Math.Abs(diff) > s.MaxStep)
                {
                    heights[x] = heights[x + 1] + (Math.Sign(diff) * s.MaxStep);
                }
            }

            return heights;
        }

        // ------------------------------------------------------------------ materiais

        private static void Fill(TerrainCellMap map, int[] heights, NaturalWorldSettings s)
        {
            int salt = s.Seed * 211;
            for (int x = 0; x < s.Width; x++)
            {
                int top = heights[x];
                double soil = 7 + (Noise1(x, 45, salt + 1) * 3.5) + (Noise1(x, 13, salt + 2) * 1.5);
                if (Math.Min(x, s.Width - 1 - x) < s.EdgeRidge) soil *= 0.5; // falésias: rocha aflorando
                double deep = -95 + (Noise1(x, 60, salt + 3) * 18) + (Noise1(x, 19, salt + 11) * 5);
                // Afloramentos: trechos curtos em que a pedra chega à superfície.
                bool outcrop = Noise1(x, 7, salt + 20) > 0.62 && Math.Abs(x - s.SpawnX) > 22;
                double outcropDepth = 1.5 + (Noise1(x, 3, salt + 21) * 1.2);
                for (int y = map.OriginY; y <= top; y++)
                {
                    int below = top - y;
                    // Fronteira terra/pedra irregular: ruído 2D de alguns blocos sobre a espessura do solo.
                    double edge = soil + (Fbm2(x, y, 9, salt + 4) * 3.2);
                    int m;
                    if (outcrop && below < outcropDepth)
                    {
                        m = Pedra;
                    }
                    else if (below < edge)
                    {
                        m = Terra;
                        if (below >= 2 && Fbm2(x, y, 14, salt + 5) > 0.28) m = TerraEscura;
                        if (Fbm2(x, y, 11, salt + 6) > 0.52) m = Cascalho;
                    }
                    else
                    {
                        m = Pedra;
                        double pocket = Fbm2(x, y, 16, salt + 7);
                        if (y > deep + 40 && pocket > 0.5) m = TerraEscura;      // manchas de terra na pedra rasa
                        else if (pocket < -0.55) m = Cascalho;
                        if (y < deep + (Fbm2(x, y, 10, salt + 8) * 9)) m = PedraProfunda;
                        if (y < -165 + (Fbm2(x, y, 12, salt + 9) * 6)) m = Fbm2(x, y, 9, salt + 10) > 0.2 ? Vulcanica : PedraProfunda;
                    }

                    map.Set(x, y, m, TerrainShape.Full);
                }
            }
        }

        /// <summary>Veios: caminhantes curtos de raio 1-2, por profundidade, só dentro de rocha.</summary>
        private static void Ores(TerrainCellMap map, int[] heights, NaturalWorldSettings s, Random random)
        {
            (int material, int minDepth, int maxDepth, int count, int length)[] kinds =
            {
                (Cobre, 12, 70, 34, 9), (Prata, 40, 120, 26, 8), (Ouro, 80, 185, 18, 7), (MinerioCristal, 60, 185, 16, 7),
                (Cristal, 90, 185, 10, 5), (Vazio, 140, 188, 5, 5)
            };
            foreach ((int material, int minDepth, int maxDepth, int count, int length) in kinds)
            {
                for (int k = 0; k < count * s.Width / 720; k++)
                {
                    int x = random.Next(s.EdgeRidge, s.Width - s.EdgeRidge);
                    int y = heights[x] - random.Next(minDepth, maxDepth);
                    double angle = random.NextDouble() * Math.PI * 2;
                    double px = x, py = y;
                    for (int step = 0; step < length + random.Next(length); step++)
                    {
                        int r = random.Next(3) == 0 ? 2 : 1;
                        for (int dy = -r; dy <= r; dy++)
                        {
                            for (int dx = -r; dx <= r; dx++)
                            {
                                int cx = (int)Math.Round(px) + dx, cy = (int)Math.Round(py) + dy;
                                int current = map.MaterialAt(cx, cy);
                                if ((dx * dx) + (dy * dy) <= (r * r) + 1 &&
                                    (current == Pedra || current == PedraProfunda || current == TerraEscura || current == Vulcanica))
                                {
                                    map.Set(cx, cy, material, TerrainShape.Full);
                                }
                            }
                        }

                        angle += (random.NextDouble() - 0.5) * 1.2;
                        px += Math.Cos(angle) * 1.3;
                        py += Math.Sin(angle) * 0.9;
                    }
                }
            }
        }

        /// <summary>
        /// Rampas em parte dos degraus de uma célula, escolhidas por ruído (não
        /// em todos): suavizam a silhueta sem virar escada de 45 graus.
        /// </summary>
        private static void Slopes(TerrainCellMap map, int[] heights, NaturalWorldSettings s)
        {
            int salt = s.Seed * 307;
            for (int x = 1; x < s.Width - 1; x++)
            {
                if (Math.Abs(x - s.SpawnX) < 14 || Noise1(x, 17, salt) < 0.05)
                {
                    continue;
                }

                int h = heights[x], left = heights[x - 1], right = heights[x + 1];
                // Degrau de 1 subindo para a direita: rampa sobre a coluna baixa, encostada no degrau.
                if (right == h + 1 && left <= h)
                {
                    map.Set(x, h + 1, map.MaterialAt(x, h), TerrainShape.SlopeUpRight);
                }
                else if (left == h + 1 && right <= h)
                {
                    map.Set(x, h + 1, map.MaterialAt(x, h), TerrainShape.SlopeUpLeft);
                }
            }
        }

        // ------------------------------------------------------------------ cavernas

        private static bool[] Caves(TerrainCellMap map, int[] heights, NaturalWorldSettings s, Random random, Result result)
        {
            int w = map.Width, hgt = map.Height;
            bool[] air = new bool[w * hgt];

            void Carve(double cx, double cy, double rx, double ry, int crust)
            {
                for (int y = (int)Math.Floor(cy - ry - 1); y <= (int)Math.Ceiling(cy + ry + 1); y++)
                {
                    for (int x = (int)Math.Floor(cx - rx - 1); x <= (int)Math.Ceiling(cx + rx + 1); x++)
                    {
                        if (!map.Contains(x, y) || x < 6 || x >= w - 6 || y < map.OriginY + 6) continue;
                        if (y > heights[x] - crust) continue;
                        double dx = (x - cx) / rx, dy = (y - cy) / ry;
                        double wobble = Noise2(x, y, 4, s.Seed + 77) * 0.22;
                        if ((dx * dx) + (dy * dy) <= 1 + wobble)
                        {
                            air[((y - map.OriginY) * w) + x] = true;
                        }
                    }
                }
            }

            // Túneis: caminhantes curvos, raio variável, descendo devagar.
            int tunnels = 13 * s.Width / 720;
            for (int t = 0; t < tunnels; t++)
            {
                double x = random.Next(s.EdgeRidge + 10, s.Width - s.EdgeRidge - 10);
                double y = heights[(int)x] - random.Next(18, 172);
                double angle = random.NextDouble() < 0.5 ? 0 : Math.PI;
                int length = 90 + random.Next(170);
                for (int step = 0; step < length; step++)
                {
                    double r = 1.6 + ((Noise1(step, 23, (t * 17) + s.Seed) + 1) * 1.1);
                    Carve(x, y, r * 1.25, r, 5);
                    if (random.NextDouble() < 0.025)
                    {
                        double rx = 5 + (random.NextDouble() * 8), ry = 3 + (random.NextDouble() * 4);
                        Carve(x, y, rx, ry, 6);
                        result.Chambers.Add(new[] { (int)x, (int)y, (int)rx, (int)ry });
                    }

                    angle += (Noise1(step, 15, (t * 31) + s.Seed) * 0.18) + ((random.NextDouble() - 0.5) * 0.12);
                    double dy = Math.Sin(angle) * 0.55 - 0.08;
                    x += Math.Cos(angle) * 1.0;
                    y += dy;
                    if (x < s.EdgeRidge || x > s.Width - s.EdgeRidge) angle = Math.PI - angle;
                    if (y > heights[Math.Max(0, Math.Min(s.Width - 1, (int)x))] - 12) angle = -Math.Abs(angle) - 0.3;
                    if (y < map.OriginY + 14) angle = Math.Abs(angle) + 0.3;
                }
            }

            // Câmaras grandes isoladas, com pilares.
            int halls = 9 * s.Width / 720;
            for (int k = 0; k < halls; k++)
            {
                int cx = random.Next(s.EdgeRidge + 20, s.Width - s.EdgeRidge - 20);
                int cy = heights[cx] - random.Next(35, 176);
                double rx = 9 + (random.NextDouble() * 10), ry = 5 + (random.NextDouble() * 5);
                Carve(cx, cy, rx, ry, 8);
                result.Chambers.Add(new[] { cx, cy, (int)rx, (int)ry });
            }

            // Entradas: poços inclinados da superfície até a rede de túneis.
            int entrances = 3 * s.Width / 720;
            for (int k = 0; k < entrances; k++)
            {
                int x0 = s.SpawnX + 90 + (k * (s.Width - s.SpawnX - 160) / Math.Max(1, entrances));
                x0 = Math.Min(s.Width - s.EdgeRidge - 20, x0 + random.Next(-20, 20));
                double x = x0, y = heights[x0] + 1;
                double side = random.NextDouble() < 0.5 ? -1 : 1;
                double phase = random.NextDouble() * 6;
                result.CaveEntrances.Add(new[] { x0, heights[x0] });
                for (int step = 0; step < 70; step++)
                {
                    // Poço sinuoso: o rumo oscila em volta da descida, com trechos quase horizontais.
                    double r = 1.9 + ((Noise1(step, 9, k + 99) + 1) * 0.7);
                    Carve(x, y, r * 1.2, r, -4);
                    double angle = (-Math.PI / 2) + (side * (0.55 + (Math.Sin((step / 8.0) + phase) * 0.75)));
                    x += Math.Cos(angle) * 0.9;
                    y += Math.Sin(angle) * 0.8;
                }
            }

            Smooth(air, map, heights, w, hgt);
            for (int i = 0; i < air.Length; i++)
            {
                if (air[i])
                {
                    int x = i % w, y = (i / w) + map.OriginY;
                    map.Set(x, y, 0, TerrainShape.Empty);
                }
            }

            RemoveFloatingIslands(map, w, hgt, 10);
            return air;
        }

        /// <summary>
        /// Autômato celular em cima do ar das cavernas: tira pontas soltas e
        /// fecha buracos de uma célula; paredes e tetos com menos de 2 células
        /// entre duas cavidades são abertos (sem lâminas de 1 bloco).
        /// </summary>
        private static void Smooth(bool[] air, TerrainCellMap map, int[] heights, int w, int h)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                bool[] next = (bool[])air.Clone();
                for (int y = 1; y < h - 1; y++)
                {
                    for (int x = 1; x < w - 1; x++)
                    {
                        int n = 0;
                        for (int dy = -1; dy <= 1; dy++)
                            for (int dx = -1; dx <= 1; dx++)
                                if ((dx != 0 || dy != 0) && air[((y + dy) * w) + x + dx]) n++;
                        int i = (y * w) + x;
                        if (air[i] && n <= 2) next[i] = false;
                        else if (!air[i] && n >= 6 && (y + map.OriginY) < heights[x] - 4) next[i] = true;
                    }
                }

                Array.Copy(next, air, air.Length);
            }

            // Lâminas de 1 célula entre ar e ar (horizontal ou vertical) viram ar.
            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    int i = (y * w) + x;
                    if (air[i] || (y + map.OriginY) >= heights[x] - 3) continue;
                    bool vertical = air[i - w] && air[i + w];
                    bool horizontal = air[i - 1] && air[i + 1];
                    if (vertical || horizontal) air[i] = true;
                }
            }
        }

        /// <summary>Ilhas sólidas pequenas soltas no ar (pedras flutuando) somem.</summary>
        private static void RemoveFloatingIslands(TerrainCellMap map, int w, int h, int minCells)
        {
            int[] label = new int[w * h];
            int current = 0;
            Stack<int> stack = new Stack<int>();
            List<int> cells = new List<int>();
            for (int start = 0; start < label.Length; start++)
            {
                int sx = start % w, sy = (start / w) + map.OriginY;
                if (label[start] != 0 || map.MaterialAt(sx, sy) == 0) continue;
                current++;
                cells.Clear();
                stack.Push(start);
                label[start] = current;
                bool touchesEdge = false;

                // Percorre a região inteira (parar cedo deixaria pedaços de uma região grande sem rótulo, apagados depois).
                while (stack.Count > 0)
                {
                    int p = stack.Pop();
                    cells.Add(p);
                    int px = p % w, py = p / w;
                    if (px == 0 || py == 0 || px == w - 1 || py == h - 1) touchesEdge = true;
                    int[] nx = { px - 1, px + 1, px, px };
                    int[] ny = { py, py, py - 1, py + 1 };
                    for (int k = 0; k < 4; k++)
                    {
                        if (nx[k] < 0 || ny[k] < 0 || nx[k] >= w || ny[k] >= h) continue;
                        int q = (ny[k] * w) + nx[k];
                        if (label[q] != 0 || map.MaterialAt(nx[k], ny[k] + map.OriginY) == 0) continue;
                        label[q] = current;
                        stack.Push(q);
                    }
                }

                if (cells.Count <= minCells && !touchesEdge)
                {
                    foreach (int p in cells) map.Set(p % w, (p / w) + map.OriginY, 0, TerrainShape.Empty);
                }
            }
        }

        // ------------------------------------------------------------------ paredes e umidade

        /// <summary>
        /// Parede de fundo em toda célula abaixo da superfície natural (terra
        /// perto do topo, pedra embaixo): aparece no ar das cavernas e onde o
        /// jogador cavar; o céu nunca aparece num buraco subterrâneo.
        /// </summary>
        private static void Walls(TerrainCellMap map, int[] heights, bool[] cave)
        {
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = map.OriginY; y < heights[x]; y++)
                {
                    int depth = heights[x] - y;
                    int m = depth < 6 ? TerraEscura : y < -95 ? PedraProfunda : Pedra;
                    map.SetWall(x, y, m);
                }
            }
        }

        /// <summary>
        /// Umidade: alta perto do ar das cavernas (decai em 4 células) e nas
        /// depressões da superfície; pedra na parede de caverna muito úmida
        /// vira pedra com musgo, em manchas.
        /// </summary>
        private static void Humidity(TerrainCellMap map, int[] heights, bool[] cave, NaturalWorldSettings s)
        {
            int w = map.Width, h = map.Height;
            int[] distance = new int[w * h];
            Queue<int> queue = new Queue<int>();
            for (int i = 0; i < distance.Length; i++)
            {
                distance[i] = cave[i] && map.MaterialAt(i % w, (i / w) + map.OriginY) == 0 ? 0 : 99;
                if (distance[i] == 0) queue.Enqueue(i);
            }

            while (queue.Count > 0)
            {
                int p = queue.Dequeue();
                int d = distance[p];
                if (d >= 5) continue;
                int px = p % w, py = p / w;
                int[] nx = { px - 1, px + 1, px, px };
                int[] ny = { py, py, py - 1, py + 1 };
                for (int k = 0; k < 4; k++)
                {
                    if (nx[k] < 0 || ny[k] < 0 || nx[k] >= w || ny[k] >= h) continue;
                    int q = (ny[k] * w) + nx[k];
                    if (distance[q] <= d + 1) continue;
                    distance[q] = d + 1;
                    queue.Enqueue(q);
                }
            }

            int salt = s.Seed * 401;
            for (int y = map.OriginY; y < map.OriginY + h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = ((y - map.OriginY) * w) + x;
                    double wet = distance[i] <= 5 ? (6 - distance[i]) * 34 : 0;
                    // Depressões: a superfície mais baixa que a média local fica úmida perto do topo.
                    double local = 0;
                    for (int k = -12; k <= 12; k += 4) local += heights[Math.Max(0, Math.Min(w - 1, x + k))];
                    local /= 7;
                    double hollow = Math.Max(0, local - heights[x]) * 28;
                    if (heights[x] - y < 6) wet += hollow;
                    wet += (Noise2(x, y, 30, salt) + 1) * 25;
                    map.SetMoisture(x, y, (int)Math.Min(255, wet));

                    if (map.MaterialAt(x, y) == Pedra && distance[i] <= 2 && wet > 150 && Fbm2(x, y, 7, salt + 1) > -0.1)
                    {
                        map.Set(x, y, PedraMusgosa, TerrainShape.Full);
                    }
                }
            }
        }
    }
}
