using System;
using System.Collections.Generic;

namespace Lithostride.EditorTools
{
    /// <summary>
    /// Boss voador, versões v2 (padrão: linha de ataque com seis quadros
    /// limpos) e v1 (alternativa, com o sopro longo invadindo a quinta célula
    /// da linha 4). Cinco linhas: pairar, avançar, mergulho, ataque e
    /// reação/queda/morte.
    ///
    /// Limpeza: a prancha tem névoa escura de alfa baixo atrás dos quadros.
    /// Fica o corpo (alfa &gt;= 200) e o brilho dos cristais (azul, claro,
    /// alfa &gt;= 24); a névoa cinza sai. O pivô é o centro do corpo sem as
    /// membranas das asas: é ele que o voo e a hitbox seguem, e o bater das
    /// asas não arrasta o corpo.
    /// </summary>
    public static class BossArt
    {
        private const string Folder = "Assets/Art/Pack/Boss/";

        public static List<DerivedSheet> Build(Func<string, PixelImage> load, List<string> notes)
        {
            List<DerivedSheet> result = new List<DerivedSheet>();
            for (int v = 0; v < PackSpec.BossSheets.Length; v++)
            {
                result.Add(Version(load(PackSpec.BossSheets[v]), v, notes));
            }

            return result;
        }

        private static DerivedSheet Version(PixelImage source, int version, List<string> notes)
        {
            PixelImage clean = Clean(source);
            string tag = PackSpec.BossVersionNames[version];
            int[][] rows = PackSpec.BossRows[version];
            SheetPacker packer = new SheetPacker(1600, 2, false);

            // Costuras entre linhas, na lacuna entre as faixas.
            int[][] rowSeams = new int[rows.Length + 1][];
            for (int r = 1; r < rows.Length; r++)
            {
                int y0 = rows[r - 1][1] - 6, y1 = rows[r][0] + 6;
                rowSeams[r] = Seams.Horizontal(clean, 0, clean.Width, y0, y1, (y0 + y1) / 2, 1);
            }

            for (int r = 0; r < rows.Length; r++)
            {
                int[][] cols = PackSpec.BossCols[version][r];
                int yTop = r == 0 ? 0 : rows[r - 1][1] - 6;
                int yBottom = r == rows.Length - 1 ? clean.Height : rows[r + 1][0] + 6;

                int[][] colSeams = new int[cols.Length + 1][];
                for (int c = 1; c < cols.Length; c++)
                {
                    int x0 = cols[c - 1][1] - 4, x1 = cols[c][0] + 4;
                    colSeams[c] = Seams.Vertical(clean, yTop, yBottom, Math.Min(x0, x1), Math.Max(x0, x1), (x0 + x1) / 2, 1);
                }

                for (int c = 0; c < cols.Length; c++)
                {
                    PixelImage frame = new PixelImage(clean.Width, clean.Height);
                    for (int y = yTop; y < yBottom; y++)
                    {
                        int xl = colSeams[c] == null ? 0 : colSeams[c][y - yTop] + 1;
                        int xr = colSeams[c + 1] == null ? clean.Width : colSeams[c + 1][y - yTop];
                        for (int x = xl; x < xr; x++)
                        {
                            bool below = rowSeams[r] == null || y > rowSeams[r][x];
                            bool above = rowSeams[r + 1] == null || y <= rowSeams[r + 1][x];
                            if (below && above)
                            {
                                frame[x, y] = clean[x, y];
                            }
                        }
                    }

                    RectI bounds = frame.OpaqueBounds(24);
                    PixelImage full = frame.Crop(bounds);
                    float[] body = BodyCenter(full);
                    PixelImage art = Scaled(full, body[0], body[1], out int left, out int bottom);
                    string rowName = PackSpec.BossRowNames[r];
                    DerivedSprite sprite = new DerivedSprite
                    {
                        Name = "boss_" + tag + "_" + rowName + "_" + c.ToString("00"),
                        Source = PackSpec.BossSheets[version],
                        SourceRect = bounds,
                        Sequence = "boss_" + tag + "_" + rowName,
                        Order = c,
                        PivotX = (float)left / art.Width,
                        PivotY = (float)bottom / art.Height,
                        Role = "boss",
                        Adjustments = "névoa removida (corpo alfa>=200, brilho azul alfa>=24); costuras entre quadros; " +
                                      "fator " + PackSpec.BossSourceToLogical + " (corpo por medoide, brilho por média de área) " +
                                      "ancorado no centro do corpo sem as asas"
                    };
                    sprite.Anchors["chao"] = new[] { 0f, -bottom };
                    packer.Add(art, sprite);

                    if (version == 1 && rowName == "ataque" && c == 3)
                    {
                        AddBreath(packer, frame, bounds);
                    }
                }
            }

            notes.Add("Boss " + tag + ": " + packer.Count + " recortes");
            return packer.Pack(Folder + "boss_" + tag + ".png", PackSpec.BossPixelsPerUnit, "boss", false);
        }

        /// <summary>
        /// v1: o sopro longo do quadro "ataque 3" vira efeito à parte, e o
        /// maior cristal solto dele vira o projétil do boss.
        /// </summary>
        private static void AddBreath(SheetPacker packer, PixelImage frame, RectI frameBounds)
        {
            PixelImage breath = new PixelImage(frame.Width, frame.Height);
            for (int y = frameBounds.Y; y < frameBounds.Bottom; y++)
            {
                for (int x = PackSpec.BossV1BreathStartX; x < frameBounds.Right; x++)
                {
                    breath[x, y] = frame[x, y];
                }
            }

            RectI bounds = breath.OpaqueBounds(24);
            PixelImage art = breath.Crop(bounds);
            PixelImage breathScaled = Scaled(art, 0, art.Height * 0.5, out int breathLeft, out int breathBottom);
            packer.Add(breathScaled, new DerivedSprite
            {
                Name = "boss_v1_sopro",
                Source = PackSpec.BossSheets[1],
                SourceRect = bounds,
                Sequence = "boss_efeitos",
                Order = 0,
                PivotX = (float)breathLeft / breathScaled.Width,
                PivotY = (float)breathBottom / breathScaled.Height,
                Role = "efeito:boss",
                Adjustments = "parte do quadro ataque_03 da v1 à direita de x=" + PackSpec.BossV1BreathStartX + "; pivô na boca"
            });

            // Maior cristal solto do sopro (ilha própria, longe da boca): projétil.
            art.Islands(128, out int[] labels, out List<int> areas);
            int best = 0;
            for (int i = 1; i < areas.Count; i++)
            {
                if (areas[i] >= 20 && areas[i] <= 900 && (best == 0 || areas[i] > areas[best]))
                {
                    best = i;
                }
            }

            if (best == 0)
            {
                return;
            }

            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != best)
                {
                    continue;
                }

                int x = i % art.Width, y = i / art.Width;
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }

            RectI shard = RectI.FromEdges(minX - 3, minY - 3, maxX + 4, maxY + 4);
            PixelImage crystalFull = art.Crop(shard);
            PixelImage crystal = Scaled(crystalFull, crystalFull.Width * 0.5, crystalFull.Height * 0.5, out int crystalLeft, out int crystalBottom);
            packer.Add(crystal, new DerivedSprite
            {
                Name = "boss_cristal",
                Source = PackSpec.BossSheets[1],
                SourceRect = new RectI(bounds.X + shard.X, bounds.Y + shard.Y, shard.W, shard.H),
                Sequence = "boss_efeitos",
                Order = 1,
                PivotX = (float)crystalLeft / crystal.Width,
                PivotY = (float)crystalBottom / crystal.Height,
                Role = "projetil:boss",
                Adjustments = "maior cristal solto do sopro da v1 (ilha de alfa >= 128), com brilho em volta"
            });
        }

        /// <summary>
        /// Reduz um quadro para a grade do mundo com (cx, cy) numa quina de
        /// pixel: corpo opaco por medoide (cores reais, sem borrão) e o brilho
        /// azul semitransparente por média de área, por baixo do corpo.
        /// </summary>
        private static PixelImage Scaled(PixelImage full, double cx, double cy, out int left, out int bottom)
        {
            double step = 1.0 / PackSpec.BossSourceToLogical;
            left = (int)Math.Ceiling(cx / step);
            int right = (int)Math.Ceiling((full.Width - cx) / step);
            int top = (int)Math.Ceiling(cy / step);
            bottom = (int)Math.Ceiling((full.Height - cy) / step);
            int w = left + right, h = top + bottom;
            double ox = cx - (left * step), oy = cy - (top * step);
            PixelImage body = PixelImage.ResampleMedoidAnchored(full, ox, oy, step, w, h, 200);
            PixelImage glow = PixelImage.ResampleAreaAnchored(full, ox, oy, step, w, h);
            for (int i = 0; i < body.Pixels.Length; i++)
            {
                if (body.Pixels[i].A == 0 && glow.Pixels[i].A >= 24 && glow.Pixels[i].B > glow.Pixels[i].R + 40)
                {
                    body.Pixels[i] = glow.Pixels[i];
                }
            }

            body.RemoveSpecks(24, 2);
            return body;
        }

        private static PixelImage Clean(PixelImage source)
        {
            PixelImage clean = source.Clone();
            for (int i = 0; i < clean.Pixels.Length; i++)
            {
                Rgba c = clean.Pixels[i];
                bool body = c.A >= 200;
                bool glow = c.A >= 24 && c.B > c.R + 50 && c.B > 110;
                if (body)
                {
                    c.A = 255;
                    clean.Pixels[i] = c;
                }
                else if (!glow)
                {
                    clean.Pixels[i] = Rgba.Clear;
                }
            }

            clean.RemoveSpecks(24, 4);
            return clean;
        }

        /// <summary>Centroide do corpo: pixels opacos que não são membrana azul das asas.</summary>
        private static float[] BodyCenter(PixelImage art)
        {
            double sx = 0, sy = 0;
            int n = 0;
            for (int y = 0; y < art.Height; y++)
            {
                for (int x = 0; x < art.Width; x++)
                {
                    Rgba c = art[x, y];
                    if (c.A == 255 && c.B <= c.R + 20)
                    {
                        sx += x;
                        sy += y;
                        n++;
                    }
                }
            }

            return n == 0 ? new[] { art.Width * 0.5f, art.Height * 0.5f } : new[] { (float)Math.Round(sx / n), (float)Math.Round(sy / n) };
        }
    }
}
