using System;
using System.Collections.Generic;

namespace Lithostride.EditorTools
{
    /// <summary>
    /// "Image quilting" (Efros e Freeman) sobre um toro: costura amostras de
    /// um material numa textura contínua que se repete sem emenda nas quatro
    /// direções.
    ///
    /// Por que existe: os blocos das pranchas são ícones soltos, cada um com
    /// seu desenho de pedras; lado a lado, formam uma grade. Aqui cada
    /// amostra nova sobrepõe as vizinhas em <c>overlap</c> pixels e entra
    /// por um corte de erro mínimo, que corre pelas frestas escuras entre as
    /// pedras, onde a troca de amostra não aparece. A textura final tem
    /// tamanho múltiplo do bloco, e cada célula do terreno mostra a fatia da
    /// sua posição: vizinhos sempre casam, porque são pedaços contíguos da
    /// mesma textura.
    /// </summary>
    public static class Quilt
    {
        /// <summary>
        /// Textura <paramref name="size"/> x <paramref name="size"/> (quadrada)
        /// ou faixa (<paramref name="height"/> &gt; 0: só repete na horizontal).
        /// Todas as amostras precisam ter o mesmo tamanho.
        /// </summary>
        public static PixelImage Build(IList<PixelImage> samples, int size, int overlap, int seed, bool wrapVertical,
            int height)
        {
            if (samples == null || samples.Count == 0)
            {
                throw new ArgumentException("Quilt: nenhuma amostra.");
            }

            int patchW = samples[0].Width, patchH = samples[0].Height;
            int stepX = patchW - overlap;
            int outW = size;
            int outH = wrapVertical ? size : height;
            int stepY = wrapVertical ? patchH - overlap : patchH;

            if (outW % stepX != 0 || (wrapVertical && outH % stepY != 0))
            {
                throw new ArgumentException("Quilt: tamanho " + size + " não é múltiplo do passo " + stepX + "/" + stepY);
            }

            int cellsX = outW / stepX;
            int cellsY = wrapVertical ? outH / stepY : 1;

            Rgba[] canvas = new Rgba[outW * outH];
            bool[] filled = new bool[outW * outH];
            Random random = new Random(seed);

            for (int gy = 0; gy < cellsY; gy++)
            {
                for (int gx = 0; gx < cellsX; gx++)
                {
                    int ox = gx * stepX, oy = gy * stepY;
                    PixelImage patch = Choose(samples, canvas, filled, outW, outH, ox, oy, random);

                    bool left = gx > 0, right = gx == cellsX - 1 && cellsX > 1;
                    bool top = wrapVertical && gy > 0, bottom = wrapVertical && gy == cellsY - 1 && cellsY > 1;

                    double[] error = new double[patchW * patchH];
                    for (int v = 0; v < patchH; v++)
                    {
                        for (int u = 0; u < patchW; u++)
                        {
                            int index = Index(ox + u, oy + v, outW, outH, wrapVertical);
                            if (index >= 0 && filled[index])
                            {
                                error[(v * patchW) + u] = Rgba.DistanceSq(canvas[index], patch[u, v]);
                            }
                        }
                    }

                    int[] cutLeft = left ? VerticalCut(error, patchW, patchH, 0, overlap) : null;
                    int[] cutRight = right ? VerticalCut(error, patchW, patchH, patchW - overlap, patchW) : null;
                    int[] cutTop = top ? HorizontalCut(error, patchW, patchH, 0, overlap) : null;
                    int[] cutBottom = bottom ? HorizontalCut(error, patchW, patchH, patchH - overlap, patchH) : null;

                    for (int v = 0; v < patchH; v++)
                    {
                        for (int u = 0; u < patchW; u++)
                        {
                            int index = Index(ox + u, oy + v, outW, outH, wrapVertical);
                            if (index < 0)
                            {
                                continue;
                            }

                            bool take = !filled[index] ||
                                        ((cutLeft == null || u > cutLeft[v]) && (cutRight == null || u < cutRight[v]) &&
                                         (cutTop == null || v > cutTop[u]) && (cutBottom == null || v < cutBottom[u]));
                            if (!take)
                            {
                                continue;
                            }

                            canvas[index] = patch[u, v];
                            filled[index] = true;
                        }
                    }
                }
            }

            PixelImage result = new PixelImage(outW, outH);
            for (int i = 0; i < canvas.Length; i++)
            {
                if (!filled[i])
                {
                    throw new InvalidOperationException("Quilt: pixel sem amostra em " + (i % outW) + "," + (i / outW));
                }

                result.Pixels[i] = canvas[i];
            }

            return result;
        }

        private static int Index(int x, int y, int w, int h, bool wrapVertical)
        {
            x = ((x % w) + w) % w;
            if (wrapVertical)
            {
                y = ((y % h) + h) % h;
            }
            else if (y < 0 || y >= h)
            {
                return -1;
            }

            return (y * w) + x;
        }

        /// <summary>A amostra com menor erro na sobreposição, sorteada entre as quase tão boas.</summary>
        private static PixelImage Choose(IList<PixelImage> samples, Rgba[] canvas, bool[] filled, int w, int h, int ox,
            int oy, Random random)
        {
            double[] errors = new double[samples.Count];
            double best = double.MaxValue;
            for (int s = 0; s < samples.Count; s++)
            {
                PixelImage patch = samples[s];
                double sum = 0;
                int n = 0;
                for (int v = 0; v < patch.Height; v++)
                {
                    for (int u = 0; u < patch.Width; u++)
                    {
                        int index = Index(ox + u, oy + v, w, h, h == w);
                        if (index >= 0 && filled[index])
                        {
                            sum += Rgba.DistanceSq(canvas[index], patch[u, v]);
                            n++;
                        }
                    }
                }

                errors[s] = n == 0 ? 0 : sum / n;
                best = Math.Min(best, errors[s]);
            }

            List<int> good = new List<int>();
            for (int s = 0; s < samples.Count; s++)
            {
                if (errors[s] <= (best * 1.25) + 1.0)
                {
                    good.Add(s);
                }
            }

            return samples[good[random.Next(good.Count)]];
        }

        private static int[] VerticalCut(double[] error, int w, int h, int u0, int u1)
        {
            int width = u1 - u0;
            double[] cost = new double[width * h];
            int[] from = new int[width * h];
            for (int v = 0; v < h; v++)
            {
                for (int c = 0; c < width; c++)
                {
                    double own = error[(v * w) + u0 + c];
                    if (v == 0)
                    {
                        cost[c] = own;
                        continue;
                    }

                    double best = double.MaxValue;
                    int bestC = c;
                    for (int d = -1; d <= 1; d++)
                    {
                        int pc = c + d;
                        if (pc < 0 || pc >= width)
                        {
                            continue;
                        }

                        if (cost[((v - 1) * width) + pc] < best)
                        {
                            best = cost[((v - 1) * width) + pc];
                            bestC = pc;
                        }
                    }

                    cost[(v * width) + c] = best + own;
                    from[(v * width) + c] = bestC;
                }
            }

            int end = 0;
            for (int c = 1; c < width; c++)
            {
                if (cost[((h - 1) * width) + c] < cost[((h - 1) * width) + end])
                {
                    end = c;
                }
            }

            int[] path = new int[h];
            for (int v = h - 1; v >= 0; v--)
            {
                path[v] = u0 + end;
                end = from[(v * width) + end];
            }

            return path;
        }

        private static int[] HorizontalCut(double[] error, int w, int h, int v0, int v1)
        {
            int height = v1 - v0;
            double[] cost = new double[height * w];
            int[] from = new int[height * w];
            for (int u = 0; u < w; u++)
            {
                for (int r = 0; r < height; r++)
                {
                    double own = error[((v0 + r) * w) + u];
                    if (u == 0)
                    {
                        cost[r] = own;
                        continue;
                    }

                    double best = double.MaxValue;
                    int bestR = r;
                    for (int d = -1; d <= 1; d++)
                    {
                        int pr = r + d;
                        if (pr < 0 || pr >= height)
                        {
                            continue;
                        }

                        if (cost[((u - 1) * height) + pr] < best)
                        {
                            best = cost[((u - 1) * height) + pr];
                            bestR = pr;
                        }
                    }

                    cost[(u * height) + r] = best + own;
                    from[(u * height) + r] = bestR;
                }
            }

            int end = 0;
            for (int r = 1; r < height; r++)
            {
                if (cost[((w - 1) * height) + r] < cost[((w - 1) * height) + end])
                {
                    end = r;
                }
            }

            int[] path = new int[w];
            for (int u = w - 1; u >= 0; u--)
            {
                path[u] = v0 + end;
                end = from[(u * height) + end];
            }

            return path;
        }
    }
}
