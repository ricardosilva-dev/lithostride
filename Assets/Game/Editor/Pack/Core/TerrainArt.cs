using System;
using System.Collections.Generic;
using Lithostride.World;

namespace Lithostride.EditorTools
{
    /// <summary>
    /// Terreno: texturas contínuas por material, lidas pela posição no mundo.
    ///
    /// Por que assim: as amostras (pranchas 17 a 19 do lote 02 e blocos do
    /// pack) não encaixam entre si, e recortar uma por célula sempre deixa
    /// emenda ou período curto. Aqui cada material vira três camadas
    /// (variantes) de 256x256 px que repetem sem emenda (image quilting num
    /// toro, <see cref="Quilt"/>). O shader do terreno pinta cada pixel com a
    /// camada na coordenada do mundo: vizinhos são sempre pedaços contíguos
    /// da mesma textura, então não existe borda entre células do mesmo
    /// material para casar. As três camadas se alternam por um campo de ruído
    /// de baixa frequência avaliado por pixel (fronteiras irregulares, sem
    /// repetição a cada poucos blocos).
    ///
    /// O que é por célula (tiles): a máscara da forma (inteiro, com quinas
    /// arredondadas onde dois lados expostos se encontram; rampas; meio-bloco),
    /// cujo canal vermelho traz o índice da camada do material; o contorno só
    /// nos lados expostos ao ar; a franja irregular onde um material de
    /// prioridade maior encosta; a grama/musgo no topo exposto (faixa contínua
    /// lida pelo x do mundo); rachaduras de mineração.
    /// </summary>
    public static class TerrainArt
    {
        private const string Folder = "Assets/Art/Pack/Terreno/";

        private const int Cell = PackSpec.TileTexels;

        /// <summary>Lado de cada camada (16x16 células).</summary>
        public const int LayerSize = 256;

        public const int LayersPerMaterial = 3;

        /// <summary>Colunas da grade em que as camadas são empilhadas na folha (fatias do Texture2DArray em ordem de leitura).</summary>
        public const int LayerColumns = 8;

        /// <summary>Passo do quilting: todas as camadas crescem em blocos de 16 px.</summary>
        private const int QuiltStep = 16;

        /// <summary>Faixa de cobertura: 12 linhas, 4 acima do topo do bloco (tufos) e 8 dentro.</summary>
        public const int CapRows = 12;

        public const int CapAbove = 4;

        /// <summary>Sprite da cobertura: 4 px acima da célula, a célula e 4 px abaixo (franja em rampa).</summary>
        public const int CapSpriteHeight = 24;

        public const int CapLayersPerType = 2;

        public static readonly Rgba Outline = new Rgba(24, 17, 14, 255);
        private static readonly Rgba Shade = new Rgba(0, 0, 0, 64);
        private static readonly Rgba Highlight = new Rgba(255, 240, 220, 36);

        public static readonly string[] CapIds = { "grama", "musgo" };

        public static readonly TerrainMaterialInfo[] Materials =
        {
            Mat("terra", "Terra", "grama", 0.30f, -1, 1,
                Layer(B17(0, 0, 1, 2, 3, 4, 5), B17(1, 0, 1, 2, 3, 4, 5)),
                Layer(B17(0, 0, 1, 2, 3, 4, 5), B17(1, 0, 1, 2, 3, 4, 5), B17(3, 1, 2, 3, 5)),
                Layer(B17(3, 0, 4, 2))),
            Mat("terra_escura", "Terra escura", "grama", 0.40f, -1, 3,
                Layer(B18(1, 0, 1, 2, 3, 4, 5)),
                Layer(B18(1, 0, 1, 2, 3, 4, 5)),
                Layer(B18(2, 0, 1, 2, 3, 4, 5))),
            Mat("pedra", "Pedra", "musgo", 0.60f, -1, 6,
                Layer(B19(0, 0, 1, 2, 3, 4, 5)),
                Layer(B19(0, 0, 1, 2, 3, 4, 5), B19(1, 0, 1, 2, 3, 4, 5)),
                Layer(B19(1, 0, 1, 2, 3, 4, 5))),
            Mat("cascalho", "Cascalho", "musgo", 0.45f, 2, 4,
                Layer(B18(3, 0, 1, 2, 3, 4, 5)),
                Layer(B17(2, 0, 1, 2, 3, 4, 5)),
                Layer(B18(3, 0, 1, 2, 3, 4, 5), B17(2, 0, 1, 2, 3, 4, 5))),
            Mat("alvenaria", "Alvenaria", "", 0.70f, -1, 0,
                Pack(Row(2, 6, 4, 1), Row(2, 6, 7, 2), Row(3, 8, 0, 3))),
            Mat("pedra_profunda", "Pedra profunda", "", 0.80f, 3, 7,
                Layer(B19(3, 0, 1, 2, 3, 4, 5)),
                Layer(B19(3, 0, 1, 2, 3, 4, 5)),
                Layer(Row(2, 7, 0, 10))),
            Mat("cristal", "Cristal", "", 0.80f, 1, 8, Pack(Row(3, 0, 0, 1), Row(3, 0, 1, 2, 0.35f))),
            Mat("minerio_cristal", "Minério de cristal", "", 0.75f, 0, 8, Pack(Row(3, 0, 3, 7, 0.30f), Row(3, 0, 11, 1, 0.30f))),
            Mat("cobre", "Cobre", "", 0.70f, 4, 8, Pack(Row(3, 1, 0, 2), Row(3, 1, 2, 8, 0.30f), Row(3, 1, 11, 1, 0.30f))),
            Mat("prata", "Prata", "", 0.80f, 9, 8, Pack(Row(3, 2, 0, 2), Row(3, 2, 2, 8, 0.30f), Row(3, 2, 11, 1, 0.30f))),
            Mat("ouro", "Ouro", "", 0.90f, 5, 8, Pack(Row(3, 3, 0, 2), Row(3, 3, 2, 8, 0.30f), Row(3, 3, 11, 1, 0.30f))),
            Mat("vazio", "Pedra do vazio", "", 1.00f, 6, 8, Pack(Row(3, 4, 0, 2), Row(3, 4, 2, 8, 0.30f), Row(3, 4, 11, 1, 0.30f))),
            Mat("areia", "Areia", "", 0.25f, -1, 2, Pack(Row(3, 5, 0, 7, 0.10f), Row(3, 5, 8, 1, 0.10f), Row(3, 5, 11, 1, 0.10f))),
            Mat("arenito", "Arenito", "", 0.55f, -1, 0, Pack(Row(3, 6, 0, 10), Row(3, 6, 11, 1))),
            Mat("madeira", "Madeira", "", 0.35f, -1, 0, Pack(Row(3, 7, 0, 2), Row(3, 7, 5, 1), Row(3, 7, 7, 2), Row(3, 7, 11, 1))),
            Mat("vulcanica", "Rocha vulcânica", "", 0.90f, 8, 7, Pack(Row(3, 9, 0, 2), Row(3, 9, 2, 8, 0.30f), Row(3, 9, 11, 1, 0.30f))),
            Mat("pedra_musgosa", "Pedra com musgo", "musgo", 0.60f, -1, 5,
                Layer(B19(2, 0, 1, 2, 3, 4, 5)),
                Layer(B19(2, 0, 1, 2, 3, 4, 5)),
                Layer(B19(2, 0, 1, 2, 3, 4, 5), B19(0, 0, 1, 2, 3, 4, 5)))
        };

        /// <summary>Topos de musgo (prancha Blocos2, linha 1): a faixa de musgo sobre pedra.</summary>
        private static readonly TerrainSampleRef[] MossTops = Join(Row(2, 0, 0, 4), Row(2, 0, 6, 1), Row(2, 0, 8, 1), Row(2, 5, 0, 6));

        public static List<DerivedSheet> Build(Func<string, PixelImage> load, List<string> notes)
        {
            Dictionary<TerrainBoard, PixelImage> boards = new Dictionary<TerrainBoard, PixelImage>
            {
                { TerrainBoard.Blocos1, load(PackSpec.Blocks1) },
                { TerrainBoard.Blocos2, load(PackSpec.Blocks2) },
                { TerrainBoard.Blocos3, load(PackSpec.Blocks3) },
                { TerrainBoard.TerraMineral, load(HarmoniaSpec.TerraMineral) },
                { TerrainBoard.TerraRaizes, load(HarmoniaSpec.TerraRaizes) },
                { TerrainBoard.PedraNatural, load(HarmoniaSpec.PedraNatural) }
            };
            foreach (PixelImage board in boards.Values)
            {
                board.CleanAlpha(9, 241);
            }

            List<DerivedSheet> result = new List<DerivedSheet>
            {
                Icons(boards[TerrainBoard.Blocos1], 1, notes),
                Icons(boards[TerrainBoard.Blocos2], 2, notes),
                Icons(boards[TerrainBoard.Blocos3], 3, notes),
                Layers(boards, notes),
                CapStrips(load(HarmoniaSpec.GramaBordas), boards[TerrainBoard.Blocos2], notes),
                Regions(),
                Masks(),
                Fringes(),
                CapMasks(),
                Borders(),
                Cracks(),
                Decals(boards, notes)
            };
            return result;
        }

        // ------------------------------------------------------------------ detalhes (raízes, pedrinhas, musgo)

        public static readonly string[] DecalKinds = { "", "raiz", "pedrinha", "musgo" };

        /// <summary>
        /// Detalhes espalhados por densidade (as camadas ficam calmas; raiz e
        /// pedrinha não aparecem em toda célula): recortados das amostras das
        /// pranchas 17 e 18 por cor — raízes claras e musgo da linha 1 da 18,
        /// pedrinhas cinza da linha 3 da 17 e da linha 4 da 18. Cada detalhe
        /// cabe na célula (16x16), para nunca sobrar no ar ao minerar o vizinho.
        /// </summary>
        private static DerivedSheet Decals(Dictionary<TerrainBoard, PixelImage> boards, List<string> notes)
        {
            SheetPacker packer = new SheetPacker(16 * (Cell + 2), 1, false);
            int[] counts = new int[DecalKinds.Length];
            for (int kind = 1; kind < DecalKinds.Length; kind++)
            {
                List<TerrainSampleRef> sources = new List<TerrainSampleRef>();
                if (kind == 1 || kind == 3)
                {
                    sources.AddRange(B18(0, 0, 1, 2, 3, 4, 5));
                }
                else
                {
                    sources.AddRange(B17(2, 0, 1, 2, 3, 4, 5));
                    sources.AddRange(B18(3, 0, 1, 2, 3, 4, 5));
                }

                foreach (TerrainSampleRef s in sources)
                {
                    PixelImage sample = Sample(boards, s, NaturalPatch(s.Board));
                    double[] mean = Mean(sample);
                    double meanLuma = (mean[0] * 0.299) + (mean[1] * 0.587) + (mean[2] * 0.114);
                    Func<Rgba, bool> test;
                    if (kind == 1) test = c => c.Luma > meanLuma + 38 && c.R > c.G && c.G > c.B;
                    else if (kind == 3) test = c => c.G > c.R * 0.72 && c.G > c.B + 40 && c.Luma <= meanLuma + 38;
                    else test = c => Math.Max(c.R, Math.Max(c.G, c.B)) - Math.Min(c.R, Math.Min(c.G, c.B)) < 30 && c.Luma > 55;

                    foreach (PixelImage decal in Components(sample, test, kind == 2 ? 3 : 5, kind == 2 ? 40 : 200))
                    {
                        if (counts[kind] >= 10)
                        {
                            break;
                        }

                        packer.Add(decal, new DerivedSprite
                        {
                            Name = "detalhe_" + DecalKinds[kind] + "_" + counts[kind].ToString("00"),
                            Sequence = "terreno_detalhes_" + DecalKinds[kind],
                            Order = counts[kind],
                            Role = "tile:detalhe",
                            Source = BoardName(s.Board),
                            Adjustments = "componente de cor (" + DecalKinds[kind] + ") da amostra linha " + (s.Row + 1) + " coluna " + (s.Col + 1) +
                                          ", recortado em 16x16"
                        });
                        counts[kind]++;
                    }
                }
            }

            notes.Add("Detalhes do terreno: " + counts[1] + " raízes, " + counts[2] + " pedrinhas, " + counts[3] + " manchas de musgo");
            return packer.Pack(Folder + "terreno_detalhes.png", PackSpec.EnvironmentPixelsPerUnit, "terreno", false);
        }

        /// <summary>Componentes 8-conexos dos pixels que passam no teste, cada um num quadro 16x16 centrado (maiores primeiro).</summary>
        private static List<PixelImage> Components(PixelImage sample, Func<Rgba, bool> test, int minArea, int maxArea)
        {
            PixelImage mask = new PixelImage(sample.Width, sample.Height);
            for (int i = 0; i < sample.Pixels.Length; i++)
            {
                if (test(sample.Pixels[i]))
                {
                    mask.Pixels[i] = new Rgba(255, 255, 255, 255);
                }
            }

            mask.Islands(128, out int[] labels, out List<int> areas);
            List<int> order = new List<int>();
            for (int l = 1; l < areas.Count; l++)
            {
                if (areas[l] >= minArea && areas[l] <= maxArea) order.Add(l);
            }

            order.Sort((a, b) => areas[b].CompareTo(areas[a]));
            List<PixelImage> result = new List<PixelImage>();
            foreach (int label in order)
            {
                int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
                for (int i = 0; i < labels.Length; i++)
                {
                    if (labels[i] != label) continue;
                    int x = i % sample.Width, y = i / sample.Width;
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }

                int cx = (minX + maxX) / 2, cy = (minY + maxY) / 2;
                PixelImage decal = new PixelImage(Cell, Cell);
                for (int y = 0; y < Cell; y++)
                {
                    for (int x = 0; x < Cell; x++)
                    {
                        int sx = cx - (Cell / 2) + x, sy = cy - (Cell / 2) + y;
                        if (sample.Contains(sx, sy) && labels[(sy * sample.Width) + sx] == label)
                        {
                            decal[x, y] = sample[sx, sy];
                        }
                    }
                }

                result.Add(decal);
            }

            return result;
        }

        /// <summary>Índice da primeira camada do material (número do mapa 1..n).</summary>
        public static int LayerBase(int materialNumber)
        {
            return (materialNumber - 1) * LayersPerMaterial;
        }

        // ------------------------------------------------------------------ geometria das pranchas do pack

        private static int[][] RowsOf(int sheet)
        {
            return sheet == 1 ? PackSpec.Blocks1Rows : sheet == 2 ? PackSpec.Blocks2Rows : PackSpec.Blocks3Rows;
        }

        private static int[][][] ColsOf(int sheet)
        {
            return sheet == 1 ? PackSpec.Blocks1Cols : sheet == 2 ? PackSpec.Blocks2Cols : PackSpec.Blocks3Cols;
        }

        private static string SourceOf(int sheet)
        {
            return sheet == 1 ? PackSpec.Blocks1 : sheet == 2 ? PackSpec.Blocks2 : PackSpec.Blocks3;
        }

        private static float TexelOf(int sheet)
        {
            return sheet == 3 ? PackSpec.Blocks3SourcePerTexel : PackSpec.Blocks12SourcePerTexel;
        }

        /// <summary>Caixa medida do bloco (conteúdo com alfa &gt;= 200).</summary>
        public static RectI Body(int sheet, int row, int col)
        {
            int[] r = RowsOf(sheet)[row];
            int[] c = ColsOf(sheet)[row][col];
            return RectI.FromEdges(c[0], r[0], c[1] + 1, r[1] + 1);
        }

        /// <summary>Célula do bloco: do meio da lacuna anterior ao meio da seguinte, nas duas direções.</summary>
        public static RectI Cell_(int sheet, int row, int col, int imageW, int imageH)
        {
            int[][] rows = RowsOf(sheet);
            int[][] cols = ColsOf(sheet)[row];
            int top = row == 0 ? 0 : (rows[row - 1][1] + rows[row][0] + 1) / 2;
            int bottom = row == rows.Length - 1 ? imageH : (rows[row][1] + rows[row + 1][0] + 1) / 2;
            int left = col == 0 ? 0 : (cols[col - 1][1] + cols[col][0] + 1) / 2;
            int right = col == cols.Length - 1 ? imageW : (cols[col][1] + cols[col + 1][0] + 1) / 2;
            return RectI.FromEdges(left, top, right, bottom);
        }

        // ------------------------------------------------------------------ ícones (catálogo)

        private static DerivedSheet Icons(PixelImage source, int sheet, List<string> notes)
        {
            SheetPacker packer = new SheetPacker(1024, 2, false);
            int[][] rows = RowsOf(sheet);
            float texel = TexelOf(sheet);
            for (int r = 0; r < rows.Length; r++)
            {
                for (int c = 0; c < ColsOf(sheet)[r].Length; c++)
                {
                    RectI cell = Cell_(sheet, r, c, source.Width, source.Height);
                    int outW = (int)Math.Round(cell.W / texel), outH = (int)Math.Round(cell.H / texel);
                    PixelImage icon = PixelImage.ResampleMedoid(source, cell, outW, outH, 128);
                    icon.RemoveSpecks(128, 3);
                    RectI bounds = icon.OpaqueBounds(128);
                    if (bounds.IsEmpty)
                    {
                        notes.Add("Blocos" + sheet + " L" + (r + 1) + "C" + (c + 1) + ": vazio após a limpeza");
                        continue;
                    }

                    icon = icon.Crop(bounds);
                    bool energy = sheet == 3 && r == PackSpec.Blocks3EnergyRow;
                    string name = energy
                        ? "energia_" + (c + 1).ToString("00")
                        : "blocos" + sheet + "_l" + (r + 1).ToString("00") + "_c" + (c + 1).ToString("00");
                    packer.Add(icon, new DerivedSprite
                    {
                        Name = name,
                        Source = SourceOf(sheet),
                        SourceRect = cell,
                        Sequence = energy ? "energia" : "blocos" + sheet,
                        Order = (r * 100) + c,
                        PivotX = 0.5f,
                        PivotY = energy ? 0.5f : 0f,
                        Role = energy ? "icone:energia" : "icone:bloco",
                        Adjustments = "alfa <9 zerado, >=241 opaco; medoide a 1/" + texel.ToString("0.00") +
                                      " (grade da arte); ilhas < 3 texels removidas"
                    });
                }
            }

            return packer.Pack(Folder + "icones_blocos" + sheet + ".png", PackSpec.EnvironmentPixelsPerUnit, "icones", false);
        }

        // ------------------------------------------------------------------ amostras

        /// <summary>Tamanho natural (texels) da amostra de uma prancha.</summary>
        private static int NaturalPatch(TerrainBoard board)
        {
            switch (board)
            {
                case TerrainBoard.TerraMineral: return 22;
                case TerrainBoard.TerraRaizes:
                case TerrainBoard.PedraNatural: return 30;
                default: return 22;
            }
        }

        /// <summary>
        /// Amostra no tamanho <paramref name="patch"/>, na grade de pixels da
        /// própria prancha. Pranchas do lote 02: o miolo (sem as bordas
        /// borradas) é reduzido pelo tamanho aparente do pixel, com a fase da
        /// grade escolhida pela menor variação dentro de cada pixel. Pranchas
        /// do pack: o miolo do ícone, sem moldura nem chanfro.
        /// </summary>
        public static PixelImage Sample(Dictionary<TerrainBoard, PixelImage> boards, TerrainSampleRef s, int patch)
        {
            PixelImage board = boards[s.Board];
            if (s.Board == TerrainBoard.Blocos1 || s.Board == TerrainBoard.Blocos2 || s.Board == TerrainBoard.Blocos3)
            {
                int sheet = (int)s.Board;
                RectI body = Body(sheet, s.Row, s.Col);
                int dx = (int)Math.Round(body.W * PackSpec.InteriorInset);
                int dy = (int)Math.Round(body.H * PackSpec.InteriorInset);
                int extraTop = (int)Math.Round(body.H * s.TopInset);
                RectI interior = RectI.FromEdges(body.X + dx, body.Y + dy + extraTop, body.Right - dx, body.Bottom - dy);
                PixelImage p = PixelImage.ResampleMedoid(board, interior, 22, 22, 128);
                FillHoles(p);
                return CenterCrop(p, patch);
            }

            int b = (int)s.Board - 17;
            int[] cols = HarmoniaSpec.SampleCols[b][s.Col];
            int[] rows = HarmoniaSpec.SampleRows[b][s.Row];
            RectI area = RectI.FromEdges(cols[0] + HarmoniaSpec.SampleInset, rows[0] + HarmoniaSpec.SampleInset,
                cols[1] + 1 - HarmoniaSpec.SampleInset, rows[1] + 1 - HarmoniaSpec.SampleInset);
            double step = HarmoniaSpec.SampleTexel[b];
            double[] phase = BestPhase(board, area, step);
            int w = (int)Math.Floor((area.W - phase[0]) / step), h = (int)Math.Floor((area.H - phase[1]) / step);
            PixelImage sample = PixelImage.ResampleMedoidAnchored(board, area.X + phase[0], area.Y + phase[1], step, w, h, 128);
            FillHoles(sample);
            return CenterCrop(sample, patch);
        }

        /// <summary>Deslocamento da grade de pixels (0..passo) com menor variação de cor dentro de cada pixel.</summary>
        private static double[] BestPhase(PixelImage image, RectI area, double step)
        {
            double best = double.MaxValue;
            double[] result = { 0, 0 };
            int n = (int)Math.Ceiling(step);
            for (int py = 0; py < n; py++)
            {
                for (int px = 0; px < n; px++)
                {
                    double cost = 0;
                    int blocks = (int)Math.Floor((Math.Min(area.W, area.H) - n) / step);
                    for (int j = 0; j < blocks; j += 2)
                    {
                        for (int i = 0; i < blocks; i += 2)
                        {
                            cost += BlockVariance(image, area.X + px + (i * step), area.Y + py + (j * step), step);
                        }
                    }

                    if (cost < best)
                    {
                        best = cost;
                        result = new double[] { px, py };
                    }
                }
            }

            return result;
        }

        private static double BlockVariance(PixelImage image, double x0, double y0, double step)
        {
            int xa = (int)Math.Ceiling(x0 - 0.5), xb = (int)Math.Ceiling(x0 + step - 0.5);
            int ya = (int)Math.Ceiling(y0 - 0.5), yb = (int)Math.Ceiling(y0 + step - 0.5);
            double sr = 0, sg = 0, sb = 0, qr = 0, qg = 0, qb = 0;
            int count = 0;
            for (int y = ya; y < yb; y++)
            {
                for (int x = xa; x < xb; x++)
                {
                    Rgba c = image.GetOrClear(x, y);
                    sr += c.R;
                    sg += c.G;
                    sb += c.B;
                    qr += c.R * c.R;
                    qg += c.G * c.G;
                    qb += c.B * c.B;
                    count++;
                }
            }

            if (count == 0)
            {
                return 0;
            }

            return (qr - (sr * sr / count)) + (qg - (sg * sg / count)) + (qb - (sb * sb / count));
        }

        private static PixelImage CenterCrop(PixelImage image, int size)
        {
            if (image.Width < size || image.Height < size)
            {
                throw new InvalidOperationException("Terreno: amostra de " + image.Width + "x" + image.Height + " menor que o recorte " + size);
            }

            int x = Math.Max(0, (image.Width - size) / 2), y = Math.Max(0, (image.Height - size) / 2);
            return image.Crop(new RectI(x, y, size, size));
        }

        /// <summary>Buraco eventual no miolo (pixel transparente) recebe o vizinho opaco.</summary>
        private static void FillHoles(PixelImage patch)
        {
            for (int pass = 0; pass < 4; pass++)
            {
                for (int y = 0; y < patch.Height; y++)
                {
                    for (int x = 0; x < patch.Width; x++)
                    {
                        if (patch[x, y].A != 0)
                        {
                            continue;
                        }

                        Rgba near = patch.GetOrClear(x - 1, y);
                        if (near.A == 0) near = patch.GetOrClear(x + 1, y);
                        if (near.A == 0) near = patch.GetOrClear(x, y - 1);
                        if (near.A == 0) near = patch.GetOrClear(x, y + 1);
                        patch[x, y] = near;
                    }
                }
            }
        }

        private static double[] Mean(PixelImage image)
        {
            double r = 0, g = 0, b = 0;
            int n = 0;
            foreach (Rgba c in image.Pixels)
            {
                if (c.A == 0)
                {
                    continue;
                }

                r += c.R;
                g += c.G;
                b += c.B;
                n++;
            }

            return n == 0 ? new double[3] : new[] { r / n, g / n, b / n };
        }

        // ------------------------------------------------------------------ camadas

        /// <summary>
        /// Uma camada contínua (toro de 256x256): as amostras, niveladas na
        /// mesma cor média (sem remendos mais claros ou escuros), costuradas
        /// por quilting com corte de erro mínimo. Sem espelhar: a luz das
        /// amostras vem de cima e não pode trocar de lado.
        /// </summary>
        public static PixelImage BuildLayer(Dictionary<TerrainBoard, PixelImage> boards, TerrainSampleRef[] refs, int seed)
        {
            int patch = int.MaxValue;
            foreach (TerrainSampleRef s in refs)
            {
                patch = Math.Min(patch, NaturalPatch(s.Board));
            }

            List<PixelImage> samples = new List<PixelImage>();
            foreach (TerrainSampleRef s in refs)
            {
                samples.Add(Sample(boards, s, patch));
            }

            double[] mean = new double[3];
            foreach (PixelImage p in samples)
            {
                double[] m = Mean(p);
                for (int k = 0; k < 3; k++) mean[k] += m[k] / samples.Count;
            }

            foreach (PixelImage p in samples)
            {
                double[] m = Mean(p);
                for (int i = 0; i < p.Pixels.Length; i++)
                {
                    Rgba c = p.Pixels[i];
                    p.Pixels[i] = new Rgba((int)Math.Round(c.R + mean[0] - m[0]), (int)Math.Round(c.G + mean[1] - m[1]),
                        (int)Math.Round(c.B + mean[2] - m[2]), 255);
                }
            }

            return Quilt.Build(samples, LayerSize, patch - QuiltStep, seed, true, 0);
        }

        /// <summary>Todas as camadas empilhadas na vertical (material 1 camadas 0..2, material 2...).</summary>
        private static DerivedSheet Layers(Dictionary<TerrainBoard, PixelImage> boards, List<string> notes)
        {
            int slices = LayersPerMaterial * Materials.Length;
            int rows = (slices + LayerColumns - 1) / LayerColumns;
            PixelImage stack = new PixelImage(LayerSize * LayerColumns, LayerSize * rows);
            for (int m = 0; m < Materials.Length; m++)
            {
                TerrainMaterialInfo info = Materials[m];
                for (int l = 0; l < LayersPerMaterial; l++)
                {
                    TerrainSampleRef[] refs = info.Layers[Math.Min(l, info.Layers.Length - 1)];
                    PixelImage layer = BuildLayer(boards, refs, Seed(info.Id) + (l * 7919));
                    int slice = (m * LayersPerMaterial) + l;
                    stack.Blit(layer, (slice % LayerColumns) * LayerSize, (slice / LayerColumns) * LayerSize, false);
                }

                notes.Add("Material " + info.Id + ": 3 camadas contínuas de " + LayerSize + " px (" + Describe(info) + ")");
            }

            DerivedSheet sheet = new DerivedSheet
            {
                AssetPath = Folder + "terreno_camadas.png",
                Image = stack,
                PixelsPerUnit = PackSpec.EnvironmentPixelsPerUnit,
                Category = "terreno:camadas",
                TextureOnly = true,
                ArrayColumns = LayerColumns,
                ArrayRows = rows,
                Repeat = true
            };
            foreach (TerrainMaterialInfo info in Materials)
            {
                foreach (TerrainSampleRef[] layer in info.Layers)
                {
                    foreach (TerrainSampleRef s in layer)
                    {
                        string board = BoardName(s.Board);
                        if (!sheet.SheetSources.Contains(board)) sheet.SheetSources.Add(board);
                    }
                }
            }

            return sheet;
        }

        private static string Describe(TerrainMaterialInfo info)
        {
            List<string> parts = new List<string>();
            foreach (TerrainSampleRef[] layer in info.Layers)
            {
                Dictionary<string, int> counts = new Dictionary<string, int>();
                foreach (TerrainSampleRef s in layer)
                {
                    string key = System.IO.Path.GetFileNameWithoutExtension(BoardName(s.Board)) + (s.Board >= TerrainBoard.TerraMineral ? " linha " + (s.Row + 1) : "");
                    counts[key] = counts.TryGetValue(key, out int c) ? c + 1 : 1;
                }

                List<string> items = new List<string>();
                foreach (KeyValuePair<string, int> kv in counts) items.Add(kv.Key + " x" + kv.Value);
                parts.Add(string.Join(" + ", items));
            }

            return string.Join(" | ", parts);
        }

        public static string BoardName(TerrainBoard board)
        {
            switch (board)
            {
                case TerrainBoard.Blocos1: return PackSpec.Blocks1;
                case TerrainBoard.Blocos2: return PackSpec.Blocks2;
                case TerrainBoard.Blocos3: return PackSpec.Blocks3;
                case TerrainBoard.TerraMineral: return HarmoniaSpec.TerraMineral;
                case TerrainBoard.TerraRaizes: return HarmoniaSpec.TerraRaizes;
                default: return HarmoniaSpec.PedraNatural;
            }
        }

        // ------------------------------------------------------------------ coberturas (faixas contínuas)

        /// <summary>
        /// Faixas de cobertura (256 x 12), repetíveis na horizontal: grama das
        /// plataformas planas da prancha 20 (só a faixa de cima: tufos, grama e
        /// franja; a plataforma inteira nunca vira chão) e musgo dos topos da
        /// prancha Blocos2. Duas variantes de cada.
        /// </summary>
        private static DerivedSheet CapStrips(PixelImage grassBoard, PixelImage blocks2, List<string> notes)
        {
            grassBoard = grassBoard.Clone();
            grassBoard.CleanAlpha(9, 241);
            List<PixelImage> grass = new List<PixelImage>();
            foreach (int[] p in HarmoniaSpec.GrassPlatforms)
            {
                grass.Add(GrassBand(grassBoard, p));
            }

            List<PixelImage> moss = new List<PixelImage>();
            foreach (TerrainSampleRef s in MossTops)
            {
                moss.Add(MossBand(blocks2, Body((int)s.Board, s.Row, s.Col)));
            }

            PixelImage stack = new PixelImage(LayerSize, CapRows * CapIds.Length * CapLayersPerType);
            for (int v = 0; v < CapLayersPerType; v++)
            {
                stack.Blit(Quilt.Build(grass, LayerSize, grass[0].Width - QuiltStep, Seed("grama") + (v * 131), false, CapRows), 0,
                    v * CapRows, false);
                stack.Blit(Quilt.Build(moss, LayerSize, moss[0].Width - QuiltStep, Seed("musgo") + (v * 131), false, CapRows), 0,
                    (CapLayersPerType + v) * CapRows, false);
            }

            notes.Add("Coberturas: grama de " + grass.Count + " plataformas da prancha 20 (faixa de cima), musgo de " + moss.Count +
                      " topos de Blocos2; 2 faixas contínuas de " + LayerSize + "x" + CapRows + " cada");
            DerivedSheet caps = new DerivedSheet
            {
                AssetPath = Folder + "terreno_coberturas.png",
                Image = stack,
                PixelsPerUnit = PackSpec.EnvironmentPixelsPerUnit,
                Category = "terreno:coberturas",
                TextureOnly = true,
                ArrayColumns = 1,
                ArrayRows = CapIds.Length * CapLayersPerType,
                Repeat = true
            };
            caps.SheetSources.Add(HarmoniaSpec.GramaBordas);
            caps.SheetSources.Add(PackSpec.Blocks2);
            return caps;
        }

        /// <summary>
        /// Faixa da grama de uma plataforma: acha a linha do topo (primeira
        /// linha quase toda opaca no meio) e recorta 4 texels acima e 8 abaixo,
        /// sem as pontas arredondadas, na grade de pixels da prancha.
        /// </summary>
        private static PixelImage GrassBand(PixelImage board, int[] box)
        {
            double step = HarmoniaSpec.GrassTexel;
            int x0 = box[0] + 34, x1 = box[2] - 34;
            int top = box[1];
            for (int y = box[1]; y < box[3]; y++)
            {
                int opaque = 0;
                for (int x = x0; x < x1; x++)
                {
                    if (board[x, y].A >= 200) opaque++;
                }

                if (opaque >= (x1 - x0) * 0.92)
                {
                    top = y;
                    break;
                }
            }

            int width = (int)Math.Floor((x1 - x0) / step);
            PixelImage band = PixelImage.ResampleMedoidAnchored(board, x0, top - (CapAbove * step), step, width, CapRows, 128);
            return CenterCropWidth(band, 28);
        }

        private static PixelImage MossBand(PixelImage board, RectI body)
        {
            double step = PackSpec.Blocks12SourcePerTexel;
            int dx = (int)Math.Round(body.W * PackSpec.InteriorInset);
            int width = (int)Math.Floor((body.W - (2 * dx)) / step);
            PixelImage band = PixelImage.ResampleMedoidAnchored(board, body.X + dx, body.Y - (CapAbove * step), step, width, CapRows, 128);
            // Abaixo das 3 primeiras linhas do bloco, só o que é musgo (verde) fica.
            for (int y = CapAbove + 3; y < CapRows; y++)
            {
                for (int x = 0; x < band.Width; x++)
                {
                    Rgba c = band[x, y];
                    if (!(c.A > 0 && c.G > c.R + 6 && c.G > c.B + 6))
                    {
                        band[x, y] = Rgba.Clear;
                    }
                }
            }

            return CenterCropWidth(band, 20);
        }

        private static PixelImage CenterCropWidth(PixelImage image, int width)
        {
            int x = Math.Max(0, (image.Width - width) / 2);
            return image.Crop(new RectI(x, 0, width, image.Height));
        }

        // ------------------------------------------------------------------ campo de regiões

        /// <summary>
        /// Ruído suave que repete sem emenda (256x256; R e G com sementes e
        /// frequências diferentes). O shader lê a 1 texel = 4 px lógicos e
        /// soma duas escalas: escolhe a camada de cada pixel, com fronteiras
        /// irregulares na grade de pixels e período combinado muito longo.
        /// </summary>
        private static DerivedSheet Regions()
        {
            PixelImage image = new PixelImage(LayerSize, LayerSize);
            double[,] a = TileableNoise(LayerSize, 8, 11);
            double[,] b = TileableNoise(LayerSize, 5, 23);
            for (int y = 0; y < LayerSize; y++)
            {
                for (int x = 0; x < LayerSize; x++)
                {
                    image[x, y] = new Rgba((int)Math.Round(a[x, y] * 255), (int)Math.Round(b[x, y] * 255), 0, 255);
                }
            }

            return new DerivedSheet
            {
                AssetPath = Folder + "terreno_regioes.png",
                Image = image,
                PixelsPerUnit = PackSpec.EnvironmentPixelsPerUnit,
                Category = "terreno:regioes",
                TextureOnly = true,
                Linear = true,
                Repeat = true,
                PointFilter = false
            };
        }

        /// <summary>Ruído de valor em grade periódica (lattice x lattice), interpolação suave, 2 oitavas, normalizado 0..1.</summary>
        public static double[,] TileableNoise(int size, int lattice, int seed)
        {
            double[,] result = new double[size, size];
            Random random = new Random(seed);
            double[] weights = { 0.7, 0.3 };
            for (int octave = 0; octave < 2; octave++)
            {
                int n = lattice << octave;
                double[,] grid = new double[n, n];
                for (int j = 0; j < n; j++) for (int i = 0; i < n; i++) grid[i, j] = random.NextDouble();
                double cell = (double)size / n;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        double fx = x / cell, fy = y / cell;
                        int ix = (int)Math.Floor(fx), iy = (int)Math.Floor(fy);
                        double tx = Smooth(fx - ix), ty = Smooth(fy - iy);
                        double v00 = grid[ix % n, iy % n], v10 = grid[(ix + 1) % n, iy % n];
                        double v01 = grid[ix % n, (iy + 1) % n], v11 = grid[(ix + 1) % n, (iy + 1) % n];
                        double v = ((v00 * (1 - tx)) + (v10 * tx)) * (1 - ty) + (((v01 * (1 - tx)) + (v11 * tx)) * ty);
                        result[x, y] += v * weights[octave];
                    }
                }
            }

            double min = double.MaxValue, max = double.MinValue;
            foreach (double v in result)
            {
                min = Math.Min(min, v);
                max = Math.Max(max, v);
            }

            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) result[x, y] = (result[x, y] - min) / (max - min);
            return result;
        }

        private static double Smooth(double t)
        {
            return t * t * (3 - (2 * t));
        }

        // ------------------------------------------------------------------ máscaras de forma

        /// <summary>Bits de quina arredondada (lados expostos que se encontram).</summary>
        public const int RoundNW = 1, RoundNE = 2, RoundSE = 4, RoundSW = 8;

        /// <summary>
        /// Por material: bloco inteiro nas 16 combinações de quinas
        /// arredondadas, rampas e meio-bloco. Vermelho = índice da primeira
        /// camada do material; alfa = forma. Forma física igual ao desenho
        /// nas rampas e no meio-bloco (o inteiro usa a célula da grade).
        /// </summary>
        private static DerivedSheet Masks()
        {
            SheetPacker packer = new SheetPacker(20 * (Cell + 2), 1, false);
            for (int m = 0; m < Materials.Length; m++)
            {
                int layer = LayerBase(m + 1);
                string id = Materials[m].Id;
                for (int round = 0; round < 16; round++)
                {
                    packer.Add(ShapeMask(TerrainShape.Full, round, layer), MaskSprite(id + "_cheio_" + round.ToString("00"), id, TerrainShape.Full,
                        "máscara do bloco inteiro; quinas arredondadas " + round));
                }

                packer.Add(ShapeMask(TerrainShape.SlopeUpRight, 0, layer), MaskSprite(id + "_rampa_dir", id, TerrainShape.SlopeUpRight, "rampa ↗"));
                packer.Add(ShapeMask(TerrainShape.SlopeUpLeft, 0, layer), MaskSprite(id + "_rampa_esq", id, TerrainShape.SlopeUpLeft, "rampa ↖"));
                packer.Add(ShapeMask(TerrainShape.HalfBottom, 0, layer), MaskSprite(id + "_meio", id, TerrainShape.HalfBottom, "meio-bloco"));
            }

            DerivedSheet sheet = packer.Pack(Folder + "terreno_mascaras.png", PackSpec.EnvironmentPixelsPerUnit, "terreno:mascaras", false);
            sheet.Linear = true;
            return sheet;
        }

        private static DerivedSprite MaskSprite(string name, string material, TerrainShape shape, string what)
        {
            return new DerivedSprite
            {
                Name = name,
                Sequence = "mascaras_" + material,
                Role = "tile:mascara",
                Source = "(gerado: índice da camada no vermelho, forma no alfa)",
                Adjustments = what,
                PhysicsShape = Physics(shape)
            };
        }

        public static PixelImage ShapeMask(TerrainShape shape, int round, int layer)
        {
            PixelImage mask = new PixelImage(Cell, Cell);
            Rgba on = new Rgba(layer, 0, 0, 255);
            int last = Cell - 1;
            for (int y = 0; y < Cell; y++)
            {
                for (int x = 0; x < Cell; x++)
                {
                    if (y >= SurfaceRow(shape, x))
                    {
                        mask[x, y] = on;
                    }
                }
            }

            if ((round & RoundNW) != 0) mask[0, 0] = Rgba.Clear;
            if ((round & RoundNE) != 0) mask[last, 0] = Rgba.Clear;
            if ((round & RoundSE) != 0) mask[last, last] = Rgba.Clear;
            if ((round & RoundSW) != 0) mask[0, last] = Rgba.Clear;
            return mask;
        }

        /// <summary>Linha (do topo) do primeiro texel sólido da coluna x na forma.</summary>
        public static int SurfaceRow(TerrainShape shape, int x)
        {
            switch (shape)
            {
                case TerrainShape.SlopeUpRight:
                    return Cell - 1 - x;
                case TerrainShape.SlopeUpLeft:
                    return x;
                case TerrainShape.HalfBottom:
                    return Cell / 2;
                default:
                    return 0;
            }
        }

        /// <summary>Forma física em pixels relativos ao centro do tile, y para cima. Nulo = célula inteira.</summary>
        private static List<float[]> Physics(TerrainShape shape)
        {
            float h = Cell / 2f;
            switch (shape)
            {
                case TerrainShape.SlopeUpRight:
                    return new List<float[]> { new[] { -h, -h, h, -h, h, h } };
                case TerrainShape.SlopeUpLeft:
                    return new List<float[]> { new[] { -h, -h, h, -h, -h, h } };
                case TerrainShape.HalfBottom:
                    return new List<float[]> { new[] { -h, -h, h, -h, h, 0f, -h, 0f } };
                default:
                    return null;
            }
        }

        // ------------------------------------------------------------------ franjas entre materiais

        /// <summary>Lados da franja: norte, sul, os dois; leste, oeste, os dois.</summary>
        public static readonly string[] FringeSides = { "n", "s", "ns", "l", "o", "lo" };

        public const int FringeVariants = 2;

        /// <summary>
        /// Franja irregular (2 a 5 px) com o índice da camada do material que
        /// avança: desenhada sobre a célula vizinha de prioridade menor, na
        /// coordenada do mundo, ela continua a textura do material que avança —
        /// a fronteira entre terra e pedra fica irregular, sem emenda.
        /// </summary>
        private static DerivedSheet Fringes()
        {
            SheetPacker packer = new SheetPacker(24 * (Cell + 2), 1, false);
            for (int m = 0; m < Materials.Length; m++)
            {
                if (Materials[m].Priority <= 0)
                {
                    continue;
                }

                int layer = LayerBase(m + 1);
                for (int s = 0; s < FringeSides.Length; s++)
                {
                    for (int v = 0; v < FringeVariants; v++)
                    {
                        packer.Add(FringeMask(FringeSides[s], v, layer), new DerivedSprite
                        {
                            Name = Materials[m].Id + "_franja_" + FringeSides[s] + "_" + v,
                            Sequence = "franjas_" + Materials[m].Id,
                            Role = "tile:franja",
                            Source = "(gerado: perfil irregular determinístico)",
                            Adjustments = "franja " + FringeSides[s] + " variante " + v
                        });
                    }
                }
            }

            DerivedSheet sheet = packer.Pack(Folder + "terreno_franjas.png", PackSpec.EnvironmentPixelsPerUnit, "terreno:franjas", false);
            sheet.Linear = true;
            return sheet;
        }

        public static PixelImage FringeMask(string side, int variant, int layer)
        {
            PixelImage mask = new PixelImage(Cell, Cell);
            Rgba on = new Rgba(layer, 0, 0, 255);
            foreach (char c in side)
            {
                int[] depth = Profile(c * 31 + (variant * 7));
                for (int i = 0; i < Cell; i++)
                {
                    for (int d = 0; d < depth[i]; d++)
                    {
                        switch (c)
                        {
                            case 'n': mask[i, d] = on; break;
                            case 's': mask[i, Cell - 1 - d] = on; break;
                            case 'l': mask[Cell - 1 - d, i] = on; break;
                            default: mask[d, i] = on; break;
                        }
                    }
                }
            }

            return mask;
        }

        /// <summary>Profundidade 1..5 px ao longo do lado, em degraus de no máximo 1 px (irregular, sem serrilha fina).</summary>
        private static int[] Profile(int seed)
        {
            Random random = new Random(seed);
            int[] depth = new int[Cell];
            int d = 2 + random.Next(3);
            for (int i = 0; i < Cell; i++)
            {
                int r = random.Next(10);
                if (r < 3) d--;
                else if (r > 6) d++;
                d = Math.Max(1, Math.Min(5, d));
                depth[i] = d;
            }

            // As pontas voltam a 1-2 px para a franja casar com a da célula ao lado.
            depth[0] = Math.Min(depth[0], 2);
            depth[Cell - 1] = Math.Min(depth[Cell - 1], 2);
            return depth;
        }

        // ------------------------------------------------------------------ coberturas: máscaras das peças

        public static readonly string[] CapPieces = { "topo", "ponta_esq", "ponta_dir", "isolado", "rampa_dir", "rampa_esq", "meio" };

        /// <summary>
        /// Peças da cobertura (16 x 24: 4 px acima da célula, a célula e 4
        /// abaixo). Vermelho = primeira camada da faixa; verde = linha da faixa
        /// (0..11). A faixa é lida pelo x do mundo, então a grama continua de
        /// uma célula para a outra, também em degraus e rampas.
        /// </summary>
        private static DerivedSheet CapMasks()
        {
            SheetPacker packer = new SheetPacker(16 * (Cell + 2), 1, false);
            for (int c = 0; c < CapIds.Length; c++)
            {
                int layer = c * CapLayersPerType;
                foreach (string piece in CapPieces)
                {
                    packer.Add(CapMask(piece, layer), new DerivedSprite
                    {
                        Name = CapIds[c] + "_" + piece,
                        Sequence = "cobertura_" + CapIds[c],
                        Role = "tile:cobertura",
                        PivotX = 0.5f,
                        PivotY = 0.5f,
                        Source = c == 0 ? HarmoniaSpec.GramaBordas : PackSpec.Blocks2,
                        Adjustments = "máscara da peça " + piece + " (linha da faixa no verde)"
                    });
                }
            }

            DerivedSheet sheet = packer.Pack(Folder + "terreno_coberturas_mascaras.png", PackSpec.EnvironmentPixelsPerUnit, "terreno:coberturas",
                false);
            sheet.Linear = true;
            return sheet;
        }

        public static PixelImage CapMask(string piece, int layer)
        {
            PixelImage mask = new PixelImage(Cell, CapSpriteHeight);
            TerrainShape shape = piece == "rampa_dir" ? TerrainShape.SlopeUpRight : piece == "rampa_esq" ? TerrainShape.SlopeUpLeft
                : piece == "meio" ? TerrainShape.HalfBottom : TerrainShape.Full;
            bool left = piece == "ponta_esq" || piece == "isolado", right = piece == "ponta_dir" || piece == "isolado";
            for (int x = 0; x < Cell; x++)
            {
                int surface = SurfaceRow(shape, x);
                int first = 0, last = CapRows - 1;
                int edge = Math.Min(left ? x : int.MaxValue, right ? Cell - 1 - x : int.MaxValue);
                if (edge < CapAbove)
                {
                    // Ponta aberta: os tufos somem e a faixa afina até a borda.
                    first = CapAbove - edge;
                    last = CapAbove + 2 + (edge * 2);
                }

                for (int r = first; r <= Math.Min(last, CapRows - 1); r++)
                {
                    int y = CapAbove + surface - CapAbove + r;
                    if (y >= 0 && y < CapSpriteHeight)
                    {
                        mask[x, y] = new Rgba(layer, r, 0, 255);
                    }
                }
            }

            return mask;
        }

        // ------------------------------------------------------------------ bordas (contorno nos lados expostos)

        /// <summary>
        /// Uma sobreposição por máscara (4 lados + 4 cantos internos): contorno
        /// escuro no lado exposto, sombra por dentro nos lados e na base,
        /// brilho no topo. Onde dois lados expostos se encontram, a quina fica
        /// aberta (a máscara do bloco a arredonda). Mais as bordas de rampas e
        /// do meio-bloco.
        /// </summary>
        private static DerivedSheet Borders()
        {
            SheetPacker packer = new SheetPacker(16 * (Cell + 2), 1, false);
            for (int mask = 0; mask < TerrainRules.BorderMaskCount; mask++)
            {
                packer.Add(BorderTile(mask), new DerivedSprite
                {
                    Name = "borda_" + mask.ToString("000"),
                    Sequence = "terreno_bordas",
                    Order = mask,
                    Role = "tile:borda",
                    Source = "(gerado a partir da cor da moldura das pranchas de blocos)",
                    Adjustments = "máscara " + mask
                });
            }

            for (int sides = 0; sides < 4; sides++)
            {
                packer.Add(SlopeBorder(TerrainShape.SlopeUpRight, sides), BorderSprite("borda_rampa_dir_" + sides, "rampa ↗, lados " + sides));
                packer.Add(SlopeBorder(TerrainShape.SlopeUpLeft, sides), BorderSprite("borda_rampa_esq_" + sides, "rampa ↖, lados " + sides));
            }

            for (int sides = 0; sides < 8; sides++)
            {
                packer.Add(HalfBorder(sides), BorderSprite("borda_meio_" + sides, "meio-bloco, lados " + sides));
            }

            return packer.Pack(Folder + "terreno_bordas.png", PackSpec.EnvironmentPixelsPerUnit, "terreno", false);
        }

        private static DerivedSprite BorderSprite(string name, string what)
        {
            return new DerivedSprite
            {
                Name = name,
                Sequence = "terreno_bordas",
                Role = "tile:borda",
                Source = "(gerado)",
                Adjustments = what
            };
        }

        public static PixelImage BorderTile(int mask)
        {
            PixelImage tile = new PixelImage(Cell, Cell);
            int last = Cell - 1;
            bool n = (mask & TerrainRules.North) != 0, e = (mask & TerrainRules.East) != 0;
            bool s = (mask & TerrainRules.South) != 0, w = (mask & TerrainRules.West) != 0;

            for (int i = 0; i < Cell; i++)
            {
                if (n) tile[i, 1] = Rgba.Over(Highlight, tile[i, 1]);
                if (s) tile[i, last - 1] = Rgba.Over(Shade, tile[i, last - 1]);
                if (w) tile[1, i] = Rgba.Over(new Rgba(0, 0, 0, 40), tile[1, i]);
                if (e) tile[last - 1, i] = Rgba.Over(Shade, tile[last - 1, i]);
            }

            for (int i = 0; i < Cell; i++)
            {
                if (n) tile[i, 0] = Outline;
                if (s) tile[i, last] = Outline;
                if (w) tile[0, i] = Outline;
                if (e) tile[last, i] = Outline;
            }

            // Quina aberta onde dois lados expostos se encontram (a máscara do bloco tira o pixel).
            if (n && w) tile[0, 0] = Rgba.Clear;
            if (n && e) tile[last, 0] = Rgba.Clear;
            if (s && e) tile[last, last] = Rgba.Clear;
            if (s && w) tile[0, last] = Rgba.Clear;
            if (n && w) tile[1, 1] = Rgba.Over(new Rgba(24, 17, 14, 150), tile[1, 1]);
            if (n && e) tile[last - 1, 1] = Rgba.Over(new Rgba(24, 17, 14, 150), tile[last - 1, 1]);

            InnerCorner(tile, mask, TerrainRules.CornerNorthEast, last, 0, -1, 1);
            InnerCorner(tile, mask, TerrainRules.CornerSouthEast, last, last, -1, -1);
            InnerCorner(tile, mask, TerrainRules.CornerSouthWest, 0, last, 1, -1);
            InnerCorner(tile, mask, TerrainRules.CornerNorthWest, 0, 0, 1, 1);
            return tile;
        }

        private static void InnerCorner(PixelImage tile, int mask, int bit, int x, int y, int dx, int dy)
        {
            if ((mask & bit) == 0)
            {
                return;
            }

            tile[x + dx, y] = Rgba.Over(Shade, tile[x + dx, y]);
            tile[x, y + dy] = Rgba.Over(Shade, tile[x, y + dy]);
            tile[x, y] = Outline;
        }

        /// <summary>Borda de rampa: contorno na diagonal; bit 1 = base exposta, bit 2 = lado alto exposto.</summary>
        private static PixelImage SlopeBorder(TerrainShape shape, int sides)
        {
            PixelImage tile = new PixelImage(Cell, Cell);
            int last = Cell - 1;
            for (int x = 0; x < Cell; x++)
            {
                int row = SurfaceRow(shape, x);
                tile[x, row] = Outline;
                if (row + 1 < Cell) tile[x, row + 1] = Rgba.Over(Shade, tile[x, row + 1]);
                if ((sides & 1) != 0) tile[x, last] = Outline;
            }

            if ((sides & 2) != 0)
            {
                int column = shape == TerrainShape.SlopeUpRight ? last : 0;
                for (int y = 0; y < Cell; y++) tile[column, y] = Outline;
            }

            return tile;
        }

        /// <summary>Borda do meio-bloco: topo sempre; bit 1 = leste, 2 = sul, 4 = oeste.</summary>
        private static PixelImage HalfBorder(int sides)
        {
            PixelImage tile = new PixelImage(Cell, Cell);
            int last = Cell - 1, top = Cell / 2;
            for (int x = 0; x < Cell; x++)
            {
                tile[x, top] = Outline;
                tile[x, top + 1] = Rgba.Over(Highlight, tile[x, top + 1]);
                if ((sides & 2) != 0) tile[x, last] = Outline;
            }

            for (int y = top; y < Cell; y++)
            {
                if ((sides & 1) != 0) tile[last, y] = Outline;
                if ((sides & 4) != 0) tile[0, y] = Outline;
            }

            return tile;
        }

        // ------------------------------------------------------------------ rachaduras (mineração)

        private static DerivedSheet Cracks()
        {
            SheetPacker packer = new SheetPacker(8 * (Cell + 2), 1, false);
            Random random = new Random(4242);
            PixelImage crack = new PixelImage(Cell, Cell);
            Rgba dark = new Rgba(20, 14, 12, 210);
            for (int stage = 1; stage <= 3; stage++)
            {
                // Cada estágio acrescenta ramos ao anterior: a rachadura cresce.
                for (int branch = 0; branch < 2 + stage; branch++)
                {
                    int x = 5 + random.Next(6), y = 5 + random.Next(6);
                    int length = 3 + (stage * 2) + random.Next(3);
                    int dx = random.Next(2) == 0 ? -1 : 1, dy = random.Next(2) == 0 ? -1 : 1;
                    for (int i = 0; i < length; i++)
                    {
                        if (x < 0 || y < 0 || x >= Cell || y >= Cell) break;
                        crack[x, y] = dark;
                        if (random.Next(3) == 0) x += dx; else y += dy;
                        if (random.Next(5) == 0) x += dx;
                    }
                }

                packer.Add(crack.Clone(), new DerivedSprite
                {
                    Name = "rachadura_" + stage,
                    Sequence = "terreno_rachaduras",
                    Order = stage,
                    Role = "tile:dano",
                    Source = "(gerado)",
                    Adjustments = "estágio " + stage + " de 3 da quebra"
                });
            }

            return packer.Pack(Folder + "terreno_rachaduras.png", PackSpec.EnvironmentPixelsPerUnit, "terreno", false);
        }

        // ------------------------------------------------------------------ utilitários

        private static TerrainMaterialInfo Mat(string id, string display, string cap, float breakTime, int drop, int priority,
            params TerrainSampleRef[][] layers)
        {
            return new TerrainMaterialInfo
            {
                Id = id,
                DisplayName = display,
                Cap = cap,
                BreakTime = breakTime,
                DropIcon = drop,
                Priority = priority,
                Layers = layers
            };
        }

        /// <summary>Material do pack: as mesmas amostras, três arranjos diferentes (sementes).</summary>
        private static TerrainSampleRef[] Pack(params TerrainSampleRef[][] parts)
        {
            return Join(parts);
        }

        private static TerrainSampleRef[] Layer(params TerrainSampleRef[][] parts)
        {
            return Join(parts);
        }

        private static TerrainSampleRef[] B17(int row, params int[] cols) => Board(TerrainBoard.TerraMineral, row, cols);
        private static TerrainSampleRef[] B18(int row, params int[] cols) => Board(TerrainBoard.TerraRaizes, row, cols);
        private static TerrainSampleRef[] B19(int row, params int[] cols) => Board(TerrainBoard.PedraNatural, row, cols);

        private static TerrainSampleRef[] Board(TerrainBoard board, int row, int[] cols)
        {
            TerrainSampleRef[] result = new TerrainSampleRef[cols.Length];
            for (int i = 0; i < cols.Length; i++) result[i] = new TerrainSampleRef(board, row, cols[i]);
            return result;
        }

        /// <summary>Colunas consecutivas de uma linha de prancha do pack (1..3).</summary>
        private static TerrainSampleRef[] Row(int sheet, int row, int firstCol, int count, float topInset = 0f)
        {
            TerrainSampleRef[] result = new TerrainSampleRef[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = new TerrainSampleRef((TerrainBoard)sheet, row, firstCol + i, topInset);
            }

            return result;
        }

        private static TerrainSampleRef[] Join(params TerrainSampleRef[][] parts)
        {
            List<TerrainSampleRef> all = new List<TerrainSampleRef>();
            foreach (TerrainSampleRef[] part in parts)
            {
                all.AddRange(part);
            }

            return all.ToArray();
        }

        public static int Seed(string id)
        {
            int h = 17;
            foreach (char c in id)
            {
                h = unchecked((h * 31) + c);
            }

            return h & 0x7FFFFFFF;
        }
    }
}
