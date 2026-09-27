using System;
using System.Collections.Generic;

namespace Lithostride.EditorTools
{
    /// <summary>
    /// Árvores, tocos, troncos e itens do chão.
    ///
    /// Árvores: cada base (a plataforma de grama sob a árvore) marca uma
    /// variação; copas que se tocam são separadas por costura de alfa mínimo
    /// entre as bases. A plataforma ornamental é removida (linhas de baixo com
    /// cobertura contínua) e o apoio fica na base real do tronco, não no pixel
    /// mais baixo: folha solta ou tufo não servem de apoio.
    /// </summary>
    public static class VegetationArt
    {
        private const string Folder = "Assets/Art/Pack/Vegetacao/";

        /// <summary>Máximo de linhas de plataforma removidas (em texels).</summary>
        private const int MaxSlabRows = 8;

        public static List<DerivedSheet> Build(Func<string, PixelImage> load, List<string> notes)
        {
            List<DerivedSheet> result = new List<DerivedSheet>();
            for (int s = 0; s < PackSpec.TreeSheets.Length; s++)
            {
                result.Add(TreeSheet(load(PackSpec.TreeSheets[s]), s, notes));
            }

            result.Add(GroundItems(load(PackSpec.GroundItems), notes));
            return result;
        }

        private static DerivedSheet TreeSheet(PixelImage source, int sheetIndex, List<string> notes)
        {
            source = source.Clone();
            source.CleanAlpha(9, 241);
            SheetPacker packer = new SheetPacker(1024, 2, false);
            int[][] rows = PackSpec.TreeRows[sheetIndex];

            for (int r = 0; r < rows.Length; r++)
            {
                int top = r == 0 ? 0 : (rows[r - 1][1] + rows[r][0]) / 2;
                int bottom = r == rows.Length - 1 ? source.Height : (rows[r][1] + rows[r + 1][0]) / 2;
                int[][] bases = PackSpec.TreeBases[sheetIndex][r];

                // Costuras entre bases vizinhas, buscadas entre o fim de uma base e o início da outra.
                int[][] seams = new int[bases.Length + 1][];
                for (int b = 1; b < bases.Length; b++)
                {
                    int x0 = Math.Min(bases[b - 1][1], bases[b][0]);
                    int x1 = Math.Max(bases[b - 1][1], bases[b][0]);
                    int mid = (bases[b - 1][1] + bases[b][0]) / 2;
                    int span = Math.Max(24, (bases[b][0] - bases[b - 1][1]) / 2 + 24);
                    seams[b] = Seams.Vertical(source, top, bottom, Math.Max(0, Math.Min(x0, mid - span)),
                        Math.Min(source.Width - 1, Math.Max(x1, mid + span)), mid, 1);
                }

                for (int b = 0; b < bases.Length; b++)
                {
                    int[] leftSeam = seams[b], rightSeam = seams[b + 1];
                    PixelImage region = new PixelImage(source.Width, bottom - top);
                    for (int y = 0; y < region.Height; y++)
                    {
                        int xl = leftSeam == null ? 0 : leftSeam[y] + 1;
                        int xr = rightSeam == null ? source.Width : rightSeam[y];
                        for (int x = xl; x < xr; x++)
                        {
                            region[x, y] = source[x, top + y];
                        }
                    }

                    RectI bounds = region.OpaqueBounds(128);
                    RectI sourceRect = new RectI(bounds.X, bounds.Y + top, bounds.W, bounds.H);
                    PixelImage art = Downsample(region, bounds, PackSpec.TreeSourcePerTexel);
                    int slab = RemoveSlab(art);
                    RectI crop = art.OpaqueBounds(128);
                    art = art.Crop(crop);

                    float trunkX = TrunkBaseX(art);
                    float[] support = SupportSpan(art, trunkX);
                    string kind = Kind(sheetIndex, r);
                    string name = "arvore" + (sheetIndex + 1) + "_l" + (r + 1) + "_" + (b + 1).ToString("00");

                    DerivedSprite sprite = new DerivedSprite
                    {
                        Name = name,
                        Source = PackSpec.TreeSheets[sheetIndex],
                        SourceRect = sourceRect,
                        Sequence = "arvores" + (sheetIndex + 1),
                        Order = (r * 100) + b,
                        PivotX = trunkX / art.Width,
                        PivotY = 0f,
                        Role = kind,
                        Adjustments = "alfa <9 zerado, >=241 opaco; costura de alfa mínimo entre bases; medoide 1/" +
                                      PackSpec.TreeSourcePerTexel + "; plataforma ornamental removida (" + slab +
                                      " texels); apoio na base do tronco"
                    };
                    sprite.Anchors["apoio_esq"] = new[] { support[0] - trunkX, 0f };
                    sprite.Anchors["apoio_dir"] = new[] { support[1] - trunkX, 0f };
                    packer.Add(art, sprite);
                }
            }

            notes.Add("Árvores " + PackSpec.TreeSheets[sheetIndex] + ": " + packer.Count + " variações");
            return packer.Pack(Folder + "arvores" + (sheetIndex + 1) + ".png", PackSpec.EnvironmentPixelsPerUnit, "vegetacao",
                false);
        }

        /// <summary>Categoria pela linha da prancha (conferida visualmente).</summary>
        private static string Kind(int sheet, int row)
        {
            bool stumps = (sheet == 0 && row == 3) || (sheet == 1 && row == 2) || (sheet == 2 && row == 1) ||
                          (sheet == 3 && row == 2);
            if (stumps)
            {
                return "toco";
            }

            if (sheet == 0 && row == 0)
            {
                return "arvore:pequena";
            }

            if (sheet == 2)
            {
                return "arvore:alta";
            }

            if (sheet == 3)
            {
                return "arvore:antiga";
            }

            return "arvore:media";
        }

        private static PixelImage Downsample(PixelImage region, RectI bounds, int factor)
        {
            // Área alinhada à caixa, arredondada para múltiplo do fator (sem texel pela metade).
            int w = (bounds.W + factor - 1) / factor, h = (bounds.H + factor - 1) / factor;
            RectI area = new RectI(bounds.X, bounds.Bottom - (h * factor), w * factor, h * factor);
            PixelImage art = PixelImage.ResampleMedoid(region, area, w, h, 128);
            art.RemoveSpecks(128, 3);
            return art;
        }

        /// <summary>
        /// A plataforma ornamental: as linhas de baixo com cobertura contínua
        /// (&gt;= 95% da largura da base, sem falhas). Tufos e raízes acima dela
        /// têm falhas e ficam. Devolve quantas linhas saíram.
        /// </summary>
        private static int RemoveSlab(PixelImage art)
        {
            RectI bounds = art.OpaqueBounds(128);
            int bottom = bounds.Bottom - 1;
            int left = int.MaxValue, right = -1;
            for (int x = 0; x < art.Width; x++)
            {
                if (art[x, bottom].A >= 128)
                {
                    left = Math.Min(left, x);
                    right = Math.Max(right, x);
                }
            }

            if (right < 0)
            {
                return 0;
            }

            int removed = 0;
            for (int y = bottom; y > bottom - MaxSlabRows && y >= 0; y--)
            {
                int covered = 0;
                for (int x = left; x <= right; x++)
                {
                    if (art[x, y].A >= 128)
                    {
                        covered++;
                    }
                }

                if (covered < (right - left + 1) * 0.95f)
                {
                    break;
                }

                for (int x = 0; x < art.Width; x++)
                {
                    art[x, y] = Rgba.Clear;
                }

                removed++;
            }

            return removed;
        }

        /// <summary>x da base do tronco: centro dos pixels marrons nas linhas de baixo (sem a folhagem verde).</summary>
        private static float TrunkBaseX(PixelImage art)
        {
            int rows = Math.Max(3, art.Height / 5);
            double sx = 0;
            int n = 0;
            for (int y = art.Height - rows; y < art.Height; y++)
            {
                for (int x = 0; x < art.Width; x++)
                {
                    Rgba c = art[x, y];
                    if (c.A >= 128 && c.R > c.G + 8 && c.G >= c.B)
                    {
                        sx += x;
                        n++;
                    }
                }
            }

            return n == 0 ? art.Width * 0.5f : (float)Math.Round(sx / n);
        }

        /// <summary>Largura de apoio: onde há tronco/raiz marrom na última faixa, em x relativo à imagem.</summary>
        private static float[] SupportSpan(PixelImage art, float trunkX)
        {
            int rows = Math.Max(2, art.Height / 12);
            int left = int.MaxValue, right = -1;
            for (int y = art.Height - rows; y < art.Height; y++)
            {
                for (int x = 0; x < art.Width; x++)
                {
                    Rgba c = art[x, y];
                    if (c.A >= 128 && c.R > c.G + 8 && c.G >= c.B)
                    {
                        left = Math.Min(left, x);
                        right = Math.Max(right, x);
                    }
                }
            }

            if (right < 0)
            {
                return new[] { trunkX - 2f, trunkX + 2f };
            }

            return new[] { (float)left, (float)(right + 1) };
        }

        private static DerivedSheet GroundItems(PixelImage source, List<string> notes)
        {
            source = source.Clone();
            // O brilho verde em volta dos itens tem alfa baixo: só o desenho (>= 200) fica.
            source.BinarizeAlpha(200);
            SheetPacker packer = new SheetPacker(1024, 2, false);
            int[][] rows = PackSpec.GroundItemRows;
            int[][] cols = PackSpec.GroundItemCols;
            int factor = PackSpec.GroundItemSourcePerTexel;

            for (int r = 0; r < rows.Length; r++)
            {
                for (int c = 0; c < cols.Length; c++)
                {
                    RectI area = RectI.FromEdges(cols[c][0] - 4, rows[r][0] - 4, cols[c][1] + 5, rows[r][1] + 5);
                    PixelImage cell = source.Crop(area);
                    RectI bounds = cell.OpaqueBounds(128);
                    int w = (bounds.W + factor - 1) / factor, h = (bounds.H + factor - 1) / factor;
                    RectI grid = new RectI(bounds.X, bounds.Bottom - (h * factor), w * factor, h * factor);
                    PixelImage art = PixelImage.ResampleMedoid(cell, grid, w, h, 128);
                    art.RemoveSpecks(128, 2);
                    art = art.Crop(art.OpaqueBounds(128));

                    int index = (r * cols.Length) + c;
                    DerivedSprite sprite = new DerivedSprite
                    {
                        Name = "chao_" + PackSpec.GroundItemNames[index],
                        Source = PackSpec.GroundItems,
                        SourceRect = new RectI(area.X + bounds.X, area.Y + bounds.Y, bounds.W, bounds.H),
                        Sequence = "itens_chao",
                        Order = index,
                        PivotX = 0.5f,
                        PivotY = 0f,
                        Role = "prop",
                        Adjustments = "brilho de fundo removido (alfa < 200 zerado); medoide 1/" + factor +
                                      "; apoio na linha de baixo"
                    };
                    sprite.Anchors["apoio_esq"] = new[] { -art.Width * 0.5f, 0f };
                    sprite.Anchors["apoio_dir"] = new[] { art.Width * 0.5f, 0f };
                    packer.Add(art, sprite);
                }
            }

            notes.Add("Itens do chão: " + packer.Count);
            return packer.Pack(Folder + "itens_chao.png", PackSpec.EnvironmentPixelsPerUnit, "vegetacao", false);
        }
    }
}
