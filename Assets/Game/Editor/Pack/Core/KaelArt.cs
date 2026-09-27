using System;
using System.Collections.Generic;
using Lithostride.Core;

namespace Lithostride.EditorTools
{
    /// <summary>
    /// Kael: parado, caminhada e corrida (grades 3x3 de 128x192, pixel art
    /// nativa) e o pulo (8 poses numa prancha 2,33x maior), derivados na grade
    /// do mundo: 165 px opacos da fonte viram <see cref="WorldScale.KaelVisiblePixels"/>
    /// px lógicos.
    ///
    /// Redução por medoide (cada pixel de saída é uma cor real da área da
    /// fonte), com a grade de saída ancorada no pivô: os pés caem exatamente
    /// numa linha de pixels e o pivô é o mesmo em todos os quadros, então o
    /// balanço desenhado pelo artista fica e o corpo não "treme" entre quadros.
    /// O pulo alinha poses no chão pelos pés e poses no ar pelo broche do
    /// peito (prender o pivô ao pé mais baixo faria o corpo saltar a cada troca).
    ///
    /// Âncoras por quadro (px lógicos relativos ao pivô, y para cima): mão da
    /// espada (medida; ver <see cref="PackSpec.KaelHands"/>), se ela aparece no
    /// quadro, e o ângulo da lâmina.
    /// </summary>
    public static class KaelArt
    {
        private const string Folder = "Assets/Art/Pack/Personagem/";

        /// <summary>Pixels da fonte (grades 1:1) por pixel lógico.</summary>
        public static readonly double Step = 1.0 / WorldScale.KaelSourceToLogical;

        /// <summary>Pixels da prancha do pulo por pixel lógico.</summary>
        public static readonly double JumpStep = Step / PackSpec.KaelJumpScale;

        public static List<DerivedSheet> Build(Func<string, PixelImage> load, List<string> notes)
        {
            List<DerivedSheet> sheets = new List<DerivedSheet>();
            PixelImage master = load(PackSpec.KaelMaster);
            List<Rgba> palette = master.Palette(128);

            sheets.Add(Grid(load(PackSpec.KaelIdle), PackSpec.KaelIdle, "kael_parado", 0, 9));
            sheets.Add(Grid(load(PackSpec.KaelWalk), PackSpec.KaelWalk, "kael_caminhada", 9, 9));
            sheets.Add(Grid(load(PackSpec.KaelRun), PackSpec.KaelRun, "kael_corrida", 18, 9));
            sheets.Add(Grid(master, PackSpec.KaelMaster, "kael_master", 0, 1));
            sheets.Add(Jump(load(PackSpec.KaelJump), palette, notes));
            notes.Add("Kael: fator fonte->px lógico " + WorldScale.KaelSourceToLogical.ToString("0.0000") + " (" +
                      WorldScale.KaelSourceVisiblePixels + " px opacos -> " + WorldScale.KaelVisiblePixels + " px); pulo " +
                      (1.0 / JumpStep).ToString("0.0000"));
            return sheets;
        }

        /// <summary>Quadros de uma grade 3x3 (ou só o primeiro), na grade do mundo, com o pivô (72, 181) numa quina de pixel.</summary>
        private static DerivedSheet Grid(PixelImage source, string sourcePath, string sequence, int handOffset, int count)
        {
            int left = (int)Math.Ceiling(PackSpec.KaelPivotX / Step);
            int right = (int)Math.Ceiling((PackSpec.KaelCellWidth - PackSpec.KaelPivotX) / Step);
            int top = (int)Math.Ceiling(PackSpec.KaelFeetRow / Step);
            int bottom = (int)Math.Ceiling((PackSpec.KaelCellHeight - PackSpec.KaelFeetRow) / Step);
            int w = left + right, h = top + bottom;

            SheetPacker packer = new SheetPacker(512, 1, false);
            for (int i = 0; i < count; i++)
            {
                int cellX = (i % 3) * PackSpec.KaelCellWidth, cellY = (i / 3) * PackSpec.KaelCellHeight;
                double ox = cellX + PackSpec.KaelPivotX - (left * Step);
                double oy = cellY + PackSpec.KaelFeetRow - (top * Step);
                PixelImage frame = PixelImage.ResampleMedoidAnchored(source, ox, oy, Step, w, h, 128);
                frame.RemoveSpecks(128, 2);

                string name = count == 1 ? sequence : sequence + "_" + i.ToString("00");
                DerivedSprite sprite = new DerivedSprite
                {
                    Name = name,
                    Source = sourcePath,
                    SourceRect = new RectI(cellX, cellY, PackSpec.KaelCellWidth, PackSpec.KaelCellHeight),
                    Sequence = count == 1 ? "referencia" : sequence,
                    Order = i,
                    PivotX = (float)left / w,
                    PivotY = (float)bottom / h,
                    Role = "personagem",
                    Adjustments = "medoide 1/" + Step.ToString("0.000") + " ancorado no pivô (72,181 da célula): centro do corpo, " +
                                  "base das botas; ilhas < 2 px removidas"
                };
                sprite.Anchors["broche"] = Relative(PackSpec.KaelBroochX, PackSpec.KaelBroochY, PackSpec.KaelPivotX,
                    PackSpec.KaelFeetRow, Step);

                int index = handOffset + i;
                float[] hand = PackSpec.KaelHands[index];
                sprite.Anchors["mao"] = Relative(hand[0], hand[1], PackSpec.KaelPivotX, PackSpec.KaelFeetRow, Step);
                sprite.Anchors["mao_visivel"] = new[] { PackSpec.KaelHandVisible[index] ? 1f : 0f };
                sprite.Anchors["espada_angulo"] = new[] { (float)PackSpec.KaelSwordAngles[index] };
                packer.Add(frame, sprite);
            }

            return packer.Pack(Folder + sequence + ".png", PackSpec.LogicalPixelsPerUnit, "personagem", true);
        }

        /// <summary>Ponto da fonte → px lógicos relativos ao pivô, y para cima.</summary>
        private static float[] Relative(double x, double y, double pivotX, double pivotY, double step)
        {
            return new[] { (float)((x - pivotX) / step), (float)((pivotY - y) / step) };
        }

        private static DerivedSheet Jump(PixelImage source, List<Rgba> palette, List<string> notes)
        {
            // Offsets do broche em relação ao pivô, em px da prancha do pulo.
            double broochDx = (PackSpec.KaelBroochX - PackSpec.KaelPivotX) / PackSpec.KaelJumpScale;
            double broochDy = (PackSpec.KaelFeetRow - PackSpec.KaelBroochY) / PackSpec.KaelJumpScale;
            double bootsOffset = 0;
            SheetPacker packer = new SheetPacker(512, 1, false);

            for (int i = 0; i < 8; i++)
            {
                int row = i / 4, col = i % 4;
                int[] rows = PackSpec.KaelJumpRows[row];
                int[] cols = PackSpec.KaelJumpCols[row][col];
                RectI area = RectI.FromEdges(cols[0] - 6, rows[0] - 6, cols[1] + 7, rows[1] + 7);
                PixelImage pose = source.Crop(area);
                pose.CleanAlpha(9, 241);

                float[] brooch = Brooch(pose);
                RectI bounds = pose.OpaqueBounds(128);
                double feetRow = bounds.Bottom;
                double bootsX = BootsCenter(pose);
                if (i == 0)
                {
                    bootsOffset = bootsX - (brooch[0] - broochDx);
                }

                double pivotX, pivotY;
                if (PackSpec.KaelJumpGrounded[i])
                {
                    pivotY = feetRow;
                    // Em pé, o broche dá o centro do corpo; agachado, o tronco
                    // inclina e quem não pode escorregar são as botas.
                    bool crouched = PackSpec.KaelJumpPoses[i] == "preparo" || PackSpec.KaelJumpPoses[i] == "aterrissagem";
                    pivotX = crouched ? bootsX - bootsOffset : brooch[0] - broochDx;
                }
                else
                {
                    pivotX = brooch[0] - broochDx;
                    pivotY = brooch[1] + broochDy;
                }

                int left = (int)Math.Ceiling((pivotX - bounds.X) / JumpStep) + 1;
                int right = (int)Math.Ceiling((bounds.Right - pivotX) / JumpStep) + 1;
                int top = (int)Math.Ceiling((pivotY - bounds.Y) / JumpStep) + 1;
                int bottom = Math.Max(1, (int)Math.Ceiling((bounds.Bottom - pivotY) / JumpStep) + 1);
                PixelImage frame = PixelImage.ResampleMedoidAnchored(pose, pivotX - (left * JumpStep), pivotY - (top * JumpStep),
                    JumpStep, left + right, top + bottom, 128);
                frame.Quantize(palette);
                frame.RemoveSpecks(128, 2);

                DerivedSprite sprite = new DerivedSprite
                {
                    Name = "kael_pulo_" + i.ToString("00") + "_" + PackSpec.KaelJumpPoses[i],
                    Source = PackSpec.KaelJump,
                    SourceRect = area,
                    Sequence = "kael_pulo",
                    Order = i,
                    PivotX = (float)left / (left + right),
                    PivotY = (float)bottom / (top + bottom),
                    Role = "personagem",
                    Adjustments = "medoide 1/" + JumpStep.ToString("0.00") + " (broche-pés = MASTER) ancorado no pivô; paleta do MASTER (" +
                                  palette.Count + " cores); pivô por " + (PackSpec.KaelJumpGrounded[i] ? "pés" : "broche")
                };
                sprite.Anchors["broche"] = Relative(brooch[0], brooch[1], pivotX, pivotY, JumpStep);
                float[] hand = PackSpec.KaelJumpHands[i];
                sprite.Anchors["mao"] = Relative(hand[0], hand[1], pivotX, pivotY, JumpStep);
                sprite.Anchors["mao_visivel"] = new[] { PackSpec.KaelJumpHandVisible[i] ? 1f : 0f };
                sprite.Anchors["espada_angulo"] = new[] { (float)PackSpec.KaelJumpSwordAngles[i] };
                packer.Add(frame, sprite);

                notes.Add("Kael pulo " + i + " (" + PackSpec.KaelJumpPoses[i] + "): " + frame.Width + "x" + frame.Height +
                          " broche " + brooch[0].ToString("0.0") + "," + brooch[1].ToString("0.0"));
            }

            return packer.Pack(Folder + "kael_pulo.png", PackSpec.LogicalPixelsPerUnit, "personagem", false);
        }

        /// <summary>Centroide dos pixels do broche (azul-ciano claro): a âncora do tronco.</summary>
        public static float[] Brooch(PixelImage image)
        {
            double sx = 0, sy = 0;
            int n = 0;
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Rgba c = image[x, y];
                    if (c.A >= 128 && c.B > 180 && c.G > 120 && c.R < 110 && c.B > c.R + 90)
                    {
                        sx += x;
                        sy += y;
                        n++;
                    }
                }
            }

            if (n == 0)
            {
                return new[] { image.Width * 0.5f, image.Height * 0.5f };
            }

            return new[] { (float)(sx / n) + 0.5f, (float)(sy / n) + 0.5f };
        }

        /// <summary>Centro horizontal das botas: pixels opacos nas 12% de linhas de baixo.</summary>
        private static float BootsCenter(PixelImage image)
        {
            RectI bounds = image.OpaqueBounds(128);
            int top = bounds.Bottom - Math.Max(3, (int)(bounds.H * 0.12f));
            double sx = 0;
            int n = 0;
            for (int y = top; y < bounds.Bottom; y++)
            {
                for (int x = bounds.X; x < bounds.Right; x++)
                {
                    if (image[x, y].A >= 128)
                    {
                        sx += x;
                        n++;
                    }
                }
            }

            return n == 0 ? bounds.X + (bounds.W * 0.5f) : (float)(sx / n) + 0.5f;
        }
    }
}
