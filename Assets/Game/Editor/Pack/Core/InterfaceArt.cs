using System;
using System.Collections.Generic;

namespace Lithostride.EditorTools
{
    /// <summary>
    /// Barra de vida do boss e fundos.
    ///
    /// A barra vem desenhada três vezes (cheia, parcial, vazia), com pequenas
    /// diferenças de posição. A vazia é a moldura canônica: as outras duas
    /// são alinhadas a ela por busca de deslocamento. O canal é onde cheia e
    /// vazia diferem em cor; o preenchimento é a barra cheia só nesse canal,
    /// e na interface ele é cortado pelo valor real da vida, sem esticar e sem
    /// cobrir cabeça nem moldura. A parcial dá o brilho da ponta do preenchimento.
    /// </summary>
    public static class InterfaceArt
    {
        private const string UiFolder = "Assets/Art/Pack/Interface/";
        private const string BackgroundFolder = "Assets/Art/Pack/Fundos/";

        /// <summary>Faixa x do canal na fonte (entre o medalhão da esquerda e a cabeça da direita).</summary>
        private const int ChannelLeft = 300;
        private const int ChannelRight = 1480;

        public static List<DerivedSheet> Build(Func<string, PixelImage> load, List<string> notes)
        {
            List<DerivedSheet> result = new List<DerivedSheet>();
            result.AddRange(HealthBar(load(PackSpec.HealthBar), notes));
            for (int i = 0; i < PackSpec.Backgrounds.Length; i++)
            {
                result.Add(Background(load(PackSpec.Backgrounds[i]), i));
            }

            return result;
        }

        private static List<DerivedSheet> HealthBar(PixelImage source, List<string> notes)
        {
            source = source.Clone();
            source.CleanAlpha(9, 241);
            int[][] rows = PackSpec.HealthBarRows;
            int[] seamA = Seams.Horizontal(source, 0, source.Width, rows[0][1], rows[1][0], (rows[0][1] + rows[1][0]) / 2, 1);
            int[] seamB = Seams.Horizontal(source, 0, source.Width, rows[1][1], rows[2][0], (rows[1][1] + rows[2][0]) / 2, 1);

            PixelImage full = Band(source, null, seamA);
            PixelImage mid = Band(source, seamA, seamB);
            PixelImage empty = Band(source, seamB, null);

            // Moldura canônica: a barra vazia, recortada rente.
            RectI frameBounds = empty.OpaqueBounds(24);
            int[] fullShift = Align(full, empty, frameBounds);
            int[] midShift = Align(mid, empty, frameBounds);
            notes.Add("Barra: cheia deslocada " + fullShift[0] + "," + fullShift[1] + "; parcial " + midShift[0] + "," +
                      midShift[1] + " em relação à vazia");

            PixelImage frame = empty.Crop(frameBounds);
            PixelImage fullAligned = full.Crop(new RectI(frameBounds.X + fullShift[0], frameBounds.Y + fullShift[1], frameBounds.W, frameBounds.H));
            PixelImage midAligned = mid.Crop(new RectI(frameBounds.X + midShift[0], frameBounds.Y + midShift[1], frameBounds.W, frameBounds.H));

            bool[] channel = ChannelMask(fullAligned, frame, ChannelLeft - frameBounds.X, ChannelRight - frameBounds.X);
            RectI channelRect = MaskBounds(channel, frame.Width, frame.Height);

            PixelImage fill = new PixelImage(channelRect.W, channelRect.H);
            for (int y = 0; y < channelRect.H; y++)
            {
                for (int x = 0; x < channelRect.W; x++)
                {
                    int fx = channelRect.X + x, fy = channelRect.Y + y;
                    if (channel[(fy * frame.Width) + fx])
                    {
                        fill[x, y] = fullAligned[fx, fy];
                    }
                }
            }

            PixelImage glow = LeadingGlow(midAligned, fullAligned, frame, channel, channelRect, out int boundary);
            notes.Add("Barra: canal " + channelRect + " (na moldura); ponta da parcial em x=" + boundary);

            float s = PackSpec.HealthBarScale;
            List<DerivedSheet> sheets = new List<DerivedSheet>();
            RectI channelScaled = new RectI((int)Math.Round(channelRect.X * s), (int)Math.Round(channelRect.Y * s),
                (int)Math.Round(channelRect.W * s), (int)Math.Round(channelRect.H * s));

            DerivedSheet frameSheet = Scaled(frame, UiFolder + "barra_moldura.png", "barra_moldura", frameBounds,
                "moldura canônica (barra vazia); inclui o canal vazio");
            frameSheet.Sprites[0].Anchors["canal"] = new float[] { channelScaled.X, channelScaled.Y, channelScaled.W, channelScaled.H };
            sheets.Add(frameSheet);

            DerivedSheet fillSheet = new DerivedSheet
            {
                AssetPath = UiFolder + "barra_preenchimento.png",
                Image = PixelImage.ResampleArea(fill, new RectI(0, 0, fill.Width, fill.Height), channelScaled.W, channelScaled.H),
                PixelsPerUnit = 100,
                PointFilter = false,
                Category = "interface",
                Single = true
            };
            fillSheet.Sprites.Add(Sprite(fillSheet, "barra_preenchimento",
                new RectI(frameBounds.X + fullShift[0] + channelRect.X, frameBounds.Y + fullShift[1] + channelRect.Y, channelRect.W, channelRect.H),
                "barra cheia só no canal (onde cheia e vazia diferem); alinhada à moldura"));
            sheets.Add(fillSheet);

            if (glow != null)
            {
                DerivedSheet glowSheet = Scaled(glow, UiFolder + "barra_brilho.png", "barra_brilho", new RectI(0, 0, glow.Width, glow.Height),
                    "brilho da ponta, recortado da barra parcial");
                sheets.Add(glowSheet);
            }

            sheets.Add(Scaled(full.Crop(full.OpaqueBounds(24)), UiFolder + "barra_ref_cheia.png", "barra_ref_cheia", full.OpaqueBounds(24),
                "referência: barra cheia original (comparação)"));
            sheets.Add(Scaled(mid.Crop(mid.OpaqueBounds(24)), UiFolder + "barra_ref_parcial.png", "barra_ref_parcial", mid.OpaqueBounds(24),
                "referência: barra parcial original (comparação)"));
            return sheets;
        }

        private static DerivedSheet Scaled(PixelImage image, string path, string name, RectI sourceRect, string adjustments)
        {
            float s = PackSpec.HealthBarScale;
            int w = Math.Max(1, (int)Math.Round(image.Width * s)), h = Math.Max(1, (int)Math.Round(image.Height * s));
            DerivedSheet sheet = new DerivedSheet
            {
                AssetPath = path,
                Image = PixelImage.ResampleArea(image, new RectI(0, 0, image.Width, image.Height), w, h),
                PixelsPerUnit = 100,
                PointFilter = false,
                Category = "interface",
                Single = true
            };
            sheet.Sprites.Add(Sprite(sheet, name, sourceRect, adjustments + "; redução por área x" + s));
            return sheet;
        }

        private static DerivedSprite Sprite(DerivedSheet sheet, string name, RectI sourceRect, string adjustments)
        {
            return new DerivedSprite
            {
                Name = name,
                Rect = new RectI(0, 0, sheet.Image.Width, sheet.Image.Height),
                Source = PackSpec.HealthBar,
                SourceRect = sourceRect,
                Sequence = "barra_vida",
                Role = "interface",
                Adjustments = adjustments
            };
        }

        /// <summary>Pixels entre duas costuras horizontais (nula = borda da imagem).</summary>
        private static PixelImage Band(PixelImage source, int[] topSeam, int[] bottomSeam)
        {
            PixelImage band = new PixelImage(source.Width, source.Height);
            for (int x = 0; x < source.Width; x++)
            {
                int y0 = topSeam == null ? 0 : topSeam[x] + 1;
                int y1 = bottomSeam == null ? source.Height : bottomSeam[x] + 1;
                for (int y = y0; y < y1; y++)
                {
                    band[x, y] = source[x, y];
                }
            }

            return band;
        }

        /// <summary>Deslocamento de <paramref name="image"/> que melhor casa o alfa com a moldura canônica.</summary>
        private static int[] Align(PixelImage image, PixelImage reference, RectI referenceBounds)
        {
            RectI imageBounds = image.OpaqueBounds(24);
            int baseDx = imageBounds.X - referenceBounds.X, baseDy = imageBounds.Y - referenceBounds.Y;
            int bestDx = baseDx, bestDy = baseDy;
            long best = long.MaxValue;
            for (int dy = baseDy - 8; dy <= baseDy + 8; dy++)
            {
                for (int dx = baseDx - 8; dx <= baseDx + 8; dx++)
                {
                    long sum = 0;
                    for (int y = referenceBounds.Y; y < referenceBounds.Bottom; y += 2)
                    {
                        for (int x = referenceBounds.X; x < referenceBounds.Right; x += 2)
                        {
                            // Só a moldura (fora do canal) decide o alinhamento.
                            if (x >= ChannelLeft + 40 && x <= ChannelRight - 40 && reference[x, y].A > 0 && reference[x, y].Luma < 90)
                            {
                                continue;
                            }

                            Rgba a = reference[x, y], b = image.GetOrClear(x + dx, y + dy);
                            sum += Math.Abs(a.A - b.A) + (Math.Abs(a.Luma - b.Luma) / 2);
                        }
                    }

                    if (sum < best)
                    {
                        best = sum;
                        bestDx = dx;
                        bestDy = dy;
                    }
                }
            }

            return new[] { bestDx, bestDy };
        }

        /// <summary>
        /// Canal: onde cheia e vazia diferem. Os cristais da moldura também
        /// mudam de brilho entre as duas; por isso o canal cresce, coluna a
        /// coluna, a partir da linha central (a de mais diferenças), só
        /// enquanto a diferença continua — tolerando falhas de até 2 px.
        /// </summary>
        private static bool[] ChannelMask(PixelImage full, PixelImage empty, int left, int right)
        {
            int w = empty.Width, h = empty.Height;
            bool[] differs = new bool[w * h];
            int[] perRow = new int[h];
            for (int x = Math.Max(0, left); x < Math.Min(w, right); x++)
            {
                for (int y = 0; y < h; y++)
                {
                    Rgba a = full[x, y], b = empty[x, y];
                    if (a.A > 200 && b.A > 200 && Rgba.DistanceSq(a, b) > 45 * 45 && a.B > b.B + 30)
                    {
                        differs[(y * w) + x] = true;
                        perRow[y]++;
                    }
                }
            }

            int center = 0;
            for (int y = 1; y < h; y++)
            {
                if (perRow[y] > perRow[center])
                {
                    center = y;
                }
            }

            bool[] mask = new bool[w * h];
            for (int x = Math.Max(0, left); x < Math.Min(w, right); x++)
            {
                if (!differs[(center * w) + x])
                {
                    continue;
                }

                int top = Extend(differs, w, h, x, center, -1);
                int bottom = Extend(differs, w, h, x, center, 1);
                for (int y = top; y <= bottom; y++)
                {
                    mask[(y * w) + x] = true;
                }
            }

            return mask;
        }

        private static int Extend(bool[] differs, int w, int h, int x, int start, int step)
        {
            int last = start, gap = 0;
            for (int y = start + step; y >= 0 && y < h; y += step)
            {
                if (differs[(y * w) + x])
                {
                    last = y;
                    gap = 0;
                }
                else if (++gap > 2)
                {
                    break;
                }
            }

            return last;
        }

        private static RectI MaskBounds(bool[] mask, int w, int h)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
            for (int i = 0; i < mask.Length; i++)
            {
                if (!mask[i])
                {
                    continue;
                }

                int x = i % w, y = i / w;
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }

            return RectI.FromEdges(minX, minY, maxX + 1, maxY + 1);
        }

        /// <summary>
        /// Brilho da ponta: na barra parcial, a coluna onde o canal deixa de
        /// parecer a cheia; recorta ali os pixels mais claros que as duas barras.
        /// </summary>
        private static PixelImage LeadingGlow(PixelImage mid, PixelImage full, PixelImage empty, bool[] channel, RectI rect,
            out int boundary)
        {
            // O preenchimento da parcial é contínuo a partir da esquerda: a
            // ponta fica depois de tantas colunas quantas se parecem com a cheia.
            int w = empty.Width;
            int fullColumns = 0;
            for (int x = rect.X; x < rect.Right; x++)
            {
                int likeFull = 0, likeEmpty = 0;
                for (int y = rect.Y; y < rect.Bottom; y++)
                {
                    if (!channel[(y * w) + x])
                    {
                        continue;
                    }

                    if (Rgba.DistanceSq(mid[x, y], full[x, y]) < Rgba.DistanceSq(mid[x, y], empty[x, y])) likeFull++;
                    else likeEmpty++;
                }

                if (likeFull > likeEmpty)
                {
                    fullColumns++;
                }
            }

            boundary = rect.X + fullColumns;

            RectI area = RectI.FromEdges(boundary - 8, rect.Y, boundary + 8, rect.Bottom);
            PixelImage glow = new PixelImage(area.W, area.H);
            int kept = 0;
            for (int y = 0; y < area.H; y++)
            {
                for (int x = 0; x < area.W; x++)
                {
                    int fx = area.X + x, fy = area.Y + y;
                    if (fx < 0 || fx >= w || !channel[(fy * w) + fx])
                    {
                        continue;
                    }

                    Rgba c = mid[fx, fy];
                    if (c.Luma > Math.Max(full[fx, fy].Luma, empty[fx, fy].Luma) + 25)
                    {
                        glow[x, y] = c;
                        kept++;
                    }
                }
            }

            return kept > 4 ? glow : null;
        }

        private static DerivedSheet Background(PixelImage source, int index)
        {
            DerivedSheet sheet = new DerivedSheet
            {
                AssetPath = BackgroundFolder + "fundo_" + PackSpec.BackgroundThemes[index] + ".png",
                Image = source.Clone(),
                PixelsPerUnit = PackSpec.LogicalPixelsPerUnit,
                PointFilter = false,
                Mipmaps = true,
                Category = "fundo",
                Single = true
            };
            sheet.Sprites.Add(new DerivedSprite
            {
                Name = "fundo_" + PackSpec.BackgroundThemes[index],
                Rect = new RectI(0, 0, source.Width, source.Height),
                Source = PackSpec.Backgrounds[index] + " (= " + PackSpec.BackgroundAliases[index] + ")",
                SourceRect = new RectI(0, 0, source.Width, source.Height),
                Sequence = "fundos",
                Order = index,
                Role = "fundo:" + PackSpec.BackgroundThemes[index],
                Adjustments = "cópia integral (sem alfa na fonte); filtro bilinear com mipmaps: pintura de fundo em escala não inteira"
            });
            return sheet;
        }
    }
}
