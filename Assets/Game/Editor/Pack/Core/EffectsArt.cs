using System;
using System.Collections.Generic;

namespace Lithostride.EditorTools
{
    /// <summary>
    /// Espada de cristal e as cinco sequências de efeito (8 quadros cada, em
    /// duas linhas de quatro). Os quadros se sobrepõem na prancha (arcos e
    /// brilhos invadem a célula vizinha): a divisão é por costuras de alfa
    /// mínimo, não por grade fixa, e cada quadro leva os fragmentos que caem
    /// do seu lado da costura.
    ///
    /// Efeitos têm transparência legítima (brilho, rastro): a redução é por
    /// média de área, e só o resíduo de alfa (&lt;= 8) é zerado.
    /// </summary>
    public static class EffectsArt
    {
        private const string Folder = "Assets/Art/Pack/Armas/";

        /// <summary>Divisões nominais entre colunas de quadros (1448 / 4), onde a costura começa a busca.</summary>
        private static readonly int[] ColumnGuides = { 362, 724, 1086 };

        private const int ColumnSearch = 110;

        public static List<DerivedSheet> Build(Func<string, PixelImage> load, List<string> notes)
        {
            List<DerivedSheet> result = new List<DerivedSheet> { Sword(load(PackSpec.Sword)) };
            for (int e = 0; e < PackSpec.Effects.Length; e++)
            {
                result.Add(Effect(load(PackSpec.Effects[e]), e, notes));
            }

            return result;
        }

        /// <summary>Ângulo do eixo da espada na fonte (cabo → ponta), em graus, y para cima.</summary>
        public static double SwordSourceAngle => Math.Atan2(PackSpec.SwordGrip[1] - PackSpec.SwordTip[1],
            PackSpec.SwordTip[0] - PackSpec.SwordGrip[0]) * 180.0 / Math.PI;

        private static double Distance(float[] a, float[] b)
        {
            return Math.Sqrt(((a[0] - b[0]) * (a[0] - b[0])) + ((a[1] - b[1]) * (a[1] - b[1])));
        }

        /// <summary>
        /// Espada empunhada: uma variante por ângulo de <see cref="PackSpec.SwordAngles"/>
        /// e a da orientação original (galeria). Cada uma é girada na resolução
        /// da fonte em torno do centro do cabo e só então reduzida (medoide),
        /// com o cabo no centro de um pixel lógico: o pivô é a empunhadura e a
        /// pixel art não serrilha como numa rotação feita no jogo.
        /// </summary>
        private static DerivedSheet Sword(PixelImage source)
        {
            source = source.Clone();
            source.CleanAlpha(9, 241);
            RectI bounds = source.OpaqueBounds(128);
            PixelImage cropped = source.Crop(bounds);
            double gripX = PackSpec.SwordGrip[0] - bounds.X, gripY = PackSpec.SwordGrip[1] - bounds.Y;
            double tipLength = Distance(PackSpec.SwordGrip, PackSpec.SwordTip);
            double length = tipLength + Distance(PackSpec.SwordGrip, PackSpec.SwordPommelEnd);
            double step = length / PackSpec.SwordLengthPixels;
            double sourceAngle = SwordSourceAngle;

            SheetPacker packer = new SheetPacker(512, 1, false);
            List<int> angles = new List<int> { 0 };
            angles.AddRange(PackSpec.SwordAngles);
            foreach (int angle in angles)
            {
                double target = angle == 0 ? sourceAngle : angle;
                PixelImage rotated = PixelImage.Rotate(cropped, gripX, gripY, target - sourceAngle, out double cx, out double cy);
                RectI b = rotated.OpaqueBounds(128);
                int left = (int)Math.Ceiling(((cx - b.X) / step) - 0.5), right = (int)Math.Ceiling(((b.Right - cx) / step) - 0.5);
                int top = (int)Math.Ceiling(((cy - b.Y) / step) - 0.5), bottom = (int)Math.Ceiling(((b.Bottom - cy) / step) - 0.5);
                int w = left + 1 + right, h = top + 1 + bottom;
                PixelImage art = PixelImage.ResampleMedoidAnchored(rotated, cx - ((left + 0.5) * step), cy - ((top + 0.5) * step), step,
                    w, h, 128);
                art.RemoveSpecks(128, 2);

                double radians = target * Math.PI / 180.0;
                DerivedSprite sprite = new DerivedSprite
                {
                    Name = angle == 0 ? "espada_cristal" : "espada_cristal_a" + angle.ToString("00"),
                    Source = PackSpec.Sword,
                    SourceRect = bounds,
                    Sequence = "espada",
                    Order = angle,
                    PivotX = (float)((left + 0.5) / w),
                    PivotY = (float)((bottom + 0.5) / h),
                    Role = angle == 0 ? "arma" : "arma:empunhada",
                    Adjustments = "girada " + (target - sourceAngle).ToString("0.0") + " graus na fonte em torno do cabo (" +
                                  PackSpec.SwordGrip[0] + "," + PackSpec.SwordGrip[1] + "); medoide 1/" + step.ToString("0.0") +
                                  " com o cabo no centro de um pixel; comprimento " + PackSpec.SwordLengthPixels + " px"
                };
                sprite.Anchors["ponta"] = new[] { (float)(Math.Cos(radians) * tipLength / step), (float)(Math.Sin(radians) * tipLength / step) };
                sprite.Anchors["angulo"] = new[] { (float)target };
                packer.Add(art, sprite);
            }

            return packer.Pack(Folder + "espada_cristal.png", PackSpec.LogicalPixelsPerUnit, "arma", false);
        }

        private static DerivedSheet Effect(PixelImage source, int index, List<string> notes)
        {
            source = source.Clone();
            source.CleanAlpha(9, 250);
            int[] rowSeamWindow = PackSpec.EffectRowSeam[index];
            int rowMid = (rowSeamWindow[0] + rowSeamWindow[1]) / 2;
            int[] rowSeam = Seams.Horizontal(source, 0, source.Width, rowSeamWindow[0], rowSeamWindow[1], rowMid, 1);

            SheetPacker packer = new SheetPacker(1024, 2, false);
            string name = PackSpec.EffectNames[index];

            for (int row = 0; row < 2; row++)
            {
                int[][] colSeams = new int[5][];
                for (int c = 0; c < ColumnGuides.Length; c++)
                {
                    int guide = ColumnGuides[c];
                    int yTop = row == 0 ? 0 : rowSeamWindow[0];
                    int yBottom = row == 0 ? rowSeamWindow[1] : source.Height;
                    int[] partial = Seams.Vertical(source, yTop, yBottom, guide - ColumnSearch, guide + ColumnSearch, guide, 1);
                    colSeams[c + 1] = new int[source.Height];
                    for (int y = 0; y < source.Height; y++)
                    {
                        colSeams[c + 1][y] = partial[Math.Min(Math.Max(y - yTop, 0), partial.Length - 1)];
                    }
                }

                for (int col = 0; col < 4; col++)
                {
                    PixelImage frame = new PixelImage(source.Width, source.Height);
                    for (int y = 0; y < source.Height; y++)
                    {
                        int xl = colSeams[col] == null ? 0 : colSeams[col][y] + 1;
                        int xr = colSeams[col + 1] == null ? source.Width : colSeams[col + 1][y];
                        for (int x = xl; x < xr; x++)
                        {
                            bool inRow = row == 0 ? y <= rowSeam[x] : y > rowSeam[x];
                            if (inRow)
                            {
                                frame[x, y] = source[x, y];
                            }
                        }
                    }

                    RectI bounds = frame.OpaqueBounds(24);
                    int order = (row * 4) + col;
                    double step = 1.0 / PackSpec.EffectScale(index);

                    // Com espada: a grade é ancorada no cabo (centro de um pixel), que vira o pivô.
                    // Sem espada: centro do quadro.
                    float[] grip = PackSpec.EffectGrips[index] != null ? PackSpec.EffectGrips[index][order] : null;
                    double cx = grip != null ? grip[0] : bounds.X + (bounds.W * 0.5);
                    double cy = grip != null ? grip[1] : bounds.Y + (bounds.H * 0.5);
                    int left = (int)Math.Ceiling(((cx - bounds.X) / step) - 0.5), right = (int)Math.Ceiling(((bounds.Right - cx) / step) - 0.5);
                    int top = (int)Math.Ceiling(((cy - bounds.Y) / step) - 0.5), bottom = (int)Math.Ceiling(((bounds.Bottom - cy) / step) - 0.5);
                    left = Math.Max(0, left);
                    right = Math.Max(0, right);
                    top = Math.Max(0, top);
                    bottom = Math.Max(0, bottom);
                    int outW = left + 1 + right, outH = top + 1 + bottom;
                    PixelImage art = PixelImage.ResampleAreaAnchored(frame, cx - ((left + 0.5) * step), cy - ((top + 0.5) * step), step,
                        outW, outH);
                    art.CleanAlpha(12, 250);

                    DerivedSprite sprite = new DerivedSprite
                    {
                        Name = "efeito_" + name + "_" + order.ToString("00"),
                        Source = PackSpec.Effects[index],
                        SourceRect = bounds,
                        Sequence = "efeito_" + name,
                        Order = order,
                        PivotX = (float)((left + 0.5) / outW),
                        PivotY = (float)((bottom + 0.5) / outH),
                        Role = "efeito",
                        Adjustments = "separado por costuras de alfa mínimo (linha e colunas); resíduo de alfa <9 zerado; " +
                                      "redução por área 1/" + step.ToString("0.0") + " (espada do quadro = espada empunhada; preserva brilho); " +
                                      "alfa <12 zerado após a redução; pivô " + (grip != null ? "no cabo (" + grip[0] + "," + grip[1] + ")" : "no centro")
                    };
                    if (grip != null)
                    {
                        sprite.Anchors["cabo"] = new[] { 0f, 0f };
                    }

                    bool[] behind = PackSpec.EffectBehindBody[index];
                    sprite.Anchors["atras_do_corpo"] = new[] { behind != null && behind[order] ? 1f : 0f };

                    packer.Add(art, sprite);
                }
            }

            notes.Add("Efeito " + name + ": 8 quadros");
            return packer.Pack(Folder + "efeito_" + name + ".png", PackSpec.LogicalPixelsPerUnit, "efeito", false);
        }
    }
}
