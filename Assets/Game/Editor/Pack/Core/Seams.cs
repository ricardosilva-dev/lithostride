using System;

namespace Lithostride.EditorTools
{
    /// <summary>
    /// Costuras de custo mínimo pelo alfa: separam quadros, árvores e barras
    /// que se tocam na prancha. Em vez de uma linha reta num ponto fixo (que
    /// cortaria asas, copas e brilhos), a costura desvia do que é opaco e
    /// passa onde a prancha é mais transparente, inclusive em diagonal.
    /// </summary>
    public static class Seams
    {
        /// <summary>
        /// Costura vertical de cima a baixo dentro de [yTop, yBottom), com x
        /// limitado a [xMin, xMax]. Cada linha anda no máximo 1 px para o lado
        /// (<paramref name="slack"/> mais). Devolve o x da costura por linha
        /// (índice relativo a yTop). O viés puxa levemente para <paramref name="preferredX"/>.
        /// </summary>
        public static int[] Vertical(PixelImage image, int yTop, int yBottom, int xMin, int xMax, int preferredX,
            int slack)
        {
            int rows = yBottom - yTop, cols = xMax - xMin + 1;
            double[] cost = new double[rows * cols];
            int[] from = new int[rows * cols];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    double own = PixelCost(image, xMin + c, yTop + r) + (Math.Abs(xMin + c - preferredX) * 0.002);
                    if (r == 0)
                    {
                        cost[c] = own;
                        continue;
                    }

                    double best = double.MaxValue;
                    int bestC = c;
                    for (int d = -slack; d <= slack; d++)
                    {
                        int pc = c + d;
                        if (pc < 0 || pc >= cols)
                        {
                            continue;
                        }

                        double candidate = cost[((r - 1) * cols) + pc];
                        if (candidate < best)
                        {
                            best = candidate;
                            bestC = pc;
                        }
                    }

                    cost[(r * cols) + c] = best + own;
                    from[(r * cols) + c] = bestC;
                }
            }

            int end = 0;
            double endCost = double.MaxValue;
            for (int c = 0; c < cols; c++)
            {
                double v = cost[((rows - 1) * cols) + c];
                if (v < endCost)
                {
                    endCost = v;
                    end = c;
                }
            }

            int[] path = new int[rows];
            for (int r = rows - 1; r >= 0; r--)
            {
                path[r] = xMin + end;
                end = from[(r * cols) + end];
            }

            return path;
        }

        /// <summary>Costura horizontal: a transposta da vertical. Devolve o y por coluna.</summary>
        public static int[] Horizontal(PixelImage image, int xLeft, int xRight, int yMin, int yMax, int preferredY,
            int slack)
        {
            int cols = xRight - xLeft, rows = yMax - yMin + 1;
            double[] cost = new double[cols * rows];
            int[] from = new int[cols * rows];

            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    double own = PixelCost(image, xLeft + c, yMin + r) + (Math.Abs(yMin + r - preferredY) * 0.002);
                    if (c == 0)
                    {
                        cost[r] = own;
                        continue;
                    }

                    double best = double.MaxValue;
                    int bestR = r;
                    for (int d = -slack; d <= slack; d++)
                    {
                        int pr = r + d;
                        if (pr < 0 || pr >= rows)
                        {
                            continue;
                        }

                        double candidate = cost[((c - 1) * rows) + pr];
                        if (candidate < best)
                        {
                            best = candidate;
                            bestR = pr;
                        }
                    }

                    cost[(c * rows) + r] = best + own;
                    from[(c * rows) + r] = bestR;
                }
            }

            int end = 0;
            double endCost = double.MaxValue;
            for (int r = 0; r < rows; r++)
            {
                double v = cost[((cols - 1) * rows) + r];
                if (v < endCost)
                {
                    endCost = v;
                    end = r;
                }
            }

            int[] path = new int[cols];
            for (int c = cols - 1; c >= 0; c--)
            {
                path[c] = yMin + end;
                end = from[(c * rows) + end];
            }

            return path;
        }

        /// <summary>Pixels quase opacos custam muito; névoa residual, quase nada.</summary>
        private static double PixelCost(PixelImage image, int x, int y)
        {
            int a = image.GetOrClear(x, y).A;
            return a < 16 ? 0.0 : a < 128 ? a / 255.0 : 4.0 + (a / 255.0);
        }
    }
}
