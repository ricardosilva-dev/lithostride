using System;
using System.Collections.Generic;

namespace Lithostride.EditorTools
{
    /// <summary>Cor RGBA de 8 bits por canal.</summary>
    public struct Rgba : IEquatable<Rgba>
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public Rgba(int r, int g, int b, int a)
        {
            R = (byte)Clamp(r);
            G = (byte)Clamp(g);
            B = (byte)Clamp(b);
            A = (byte)Clamp(a);
        }

        public static readonly Rgba Clear = new Rgba(0, 0, 0, 0);

        public int Luma => ((R * 299) + (G * 587) + (B * 114)) / 1000;

        public bool Equals(Rgba other)
        {
            return R == other.R && G == other.G && B == other.B && A == other.A;
        }

        public override bool Equals(object obj)
        {
            return obj is Rgba other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (R << 24) | (G << 16) | (B << 8) | A;
        }

        public static int DistanceSq(Rgba a, Rgba b)
        {
            int dr = a.R - b.R, dg = a.G - b.G, db = a.B - b.B;
            return (dr * dr) + (dg * dg) + (db * db);
        }

        public static Rgba Lerp(Rgba a, Rgba b, float t)
        {
            return new Rgba((int)Math.Round(a.R + ((b.R - a.R) * t)), (int)Math.Round(a.G + ((b.G - a.G) * t)),
                (int)Math.Round(a.B + ((b.B - a.B) * t)), (int)Math.Round(a.A + ((b.A - a.A) * t)));
        }

        /// <summary>Desenha <paramref name="top"/> sobre <paramref name="bottom"/> (alfa não pré-multiplicado).</summary>
        public static Rgba Over(Rgba top, Rgba bottom)
        {
            if (top.A == 255 || bottom.A == 0)
            {
                return top;
            }

            if (top.A == 0)
            {
                return bottom;
            }

            float ta = top.A / 255f, ba = bottom.A / 255f;
            float oa = ta + (ba * (1f - ta));
            float r = ((top.R * ta) + (bottom.R * ba * (1f - ta))) / oa;
            float g = ((top.G * ta) + (bottom.G * ba * (1f - ta))) / oa;
            float b = ((top.B * ta) + (bottom.B * ba * (1f - ta))) / oa;
            return new Rgba((int)Math.Round(r), (int)Math.Round(g), (int)Math.Round(b), (int)Math.Round(oa * 255f));
        }

        public static int Clamp(int value)
        {
            return value < 0 ? 0 : value > 255 ? 255 : value;
        }
    }

    /// <summary>Retângulo inteiro com origem no canto de cima (como se lê a imagem).</summary>
    public struct RectI
    {
        public int X;
        public int Y;
        public int W;
        public int H;

        public RectI(int x, int y, int w, int h)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
        }

        public int Right => X + W;
        public int Bottom => Y + H;
        public bool IsEmpty => W <= 0 || H <= 0;

        public static RectI FromEdges(int left, int top, int right, int bottom)
        {
            return new RectI(left, top, right - left, bottom - top);
        }

        public RectI Inset(int dx, int dy)
        {
            return new RectI(X + dx, Y + dy, W - (2 * dx), H - (2 * dy));
        }

        public override string ToString()
        {
            return X + "," + Y + "," + W + "," + H;
        }
    }

    /// <summary>
    /// Imagem RGBA em memória, linha 0 no topo. Todo o recorte e a limpeza da
    /// arte trabalham nela, sem depender da Unity: o mesmo código roda no
    /// editor e numa bancada de testes fora dele.
    /// </summary>
    public sealed class PixelImage
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Rgba[] Pixels;

        public PixelImage(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new Rgba[width * height];
        }

        public Rgba this[int x, int y]
        {
            get => Pixels[(y * Width) + x];
            set => Pixels[(y * Width) + x] = value;
        }

        public bool Contains(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        public Rgba GetOrClear(int x, int y)
        {
            return Contains(x, y) ? Pixels[(y * Width) + x] : Rgba.Clear;
        }

        public PixelImage Clone()
        {
            PixelImage copy = new PixelImage(Width, Height);
            Array.Copy(Pixels, copy.Pixels, Pixels.Length);
            return copy;
        }

        /// <summary>Recorte; o que cai fora da imagem vira transparente.</summary>
        public PixelImage Crop(RectI area)
        {
            PixelImage result = new PixelImage(Math.Max(1, area.W), Math.Max(1, area.H));
            for (int y = 0; y < area.H; y++)
            {
                for (int x = 0; x < area.W; x++)
                {
                    result[x, y] = GetOrClear(area.X + x, area.Y + y);
                }
            }

            return result;
        }

        /// <summary>Copia <paramref name="source"/> para (dx, dy); com <paramref name="over"/>, compõe por alfa.</summary>
        public void Blit(PixelImage source, int dx, int dy, bool over)
        {
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    int tx = dx + x, ty = dy + y;
                    if (!Contains(tx, ty))
                    {
                        continue;
                    }

                    Rgba c = source[x, y];
                    this[tx, ty] = over ? Rgba.Over(c, this[tx, ty]) : c;
                }
            }
        }

        public PixelImage FlipX()
        {
            PixelImage result = new PixelImage(Width, Height);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    result[Width - 1 - x, y] = this[x, y];
                }
            }

            return result;
        }

        public PixelImage FlipY()
        {
            PixelImage result = new PixelImage(Width, Height);
            for (int y = 0; y < Height; y++)
            {
                Array.Copy(Pixels, y * Width, result.Pixels, (Height - 1 - y) * Width, Width);
            }

            return result;
        }

        /// <summary>Caixa dos pixels com alfa &gt;= limiar; vazia se não houver nenhum.</summary>
        public RectI OpaqueBounds(int threshold)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (this[x, y].A < threshold)
                    {
                        continue;
                    }

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            return maxX < 0 ? new RectI(0, 0, 0, 0) : RectI.FromEdges(minX, minY, maxX + 1, maxY + 1);
        }

        /// <summary>
        /// Resíduo de alfa: abaixo de <paramref name="floor"/> vira transparente;
        /// a partir de <paramref name="ceiling"/>, opaco. O meio fica como está.
        /// </summary>
        public void CleanAlpha(int floor, int ceiling)
        {
            for (int i = 0; i < Pixels.Length; i++)
            {
                Rgba c = Pixels[i];
                if (c.A < floor)
                {
                    Pixels[i] = Rgba.Clear;
                }
                else if (c.A >= ceiling)
                {
                    c.A = 255;
                    Pixels[i] = c;
                }
            }
        }

        /// <summary>Alfa binário: pixel art de contorno nítido, sem névoa nem meio-tom.</summary>
        public void BinarizeAlpha(int threshold)
        {
            for (int i = 0; i < Pixels.Length; i++)
            {
                Rgba c = Pixels[i];
                if (c.A < threshold)
                {
                    Pixels[i] = Rgba.Clear;
                }
                else
                {
                    c.A = 255;
                    Pixels[i] = c;
                }
            }
        }

        /// <summary>
        /// Redução para a grade de pixels da arte: cada pixel de saída é o
        /// medoide (a cor real mais próxima da mediana por canal) dos pixels
        /// opacos da sua área na fonte. Não inventa cores novas nem borra, ao
        /// contrário da média. A área é opaca se a maioria dos pixels for.
        /// </summary>
        public static PixelImage ResampleMedoid(PixelImage source, RectI area, int outWidth, int outHeight,
            int alphaThreshold)
        {
            PixelImage result = new PixelImage(outWidth, outHeight);
            List<Rgba> samples = new List<Rgba>(64);
            int[] rs = new int[256], gs = new int[256], bs = new int[256];
            double sx = (double)area.W / outWidth, sy = (double)area.H / outHeight;

            for (int oy = 0; oy < outHeight; oy++)
            {
                int y0 = area.Y + (int)Math.Floor(oy * sy);
                int y1 = Math.Max(y0 + 1, area.Y + (int)Math.Floor((oy + 1) * sy));
                for (int ox = 0; ox < outWidth; ox++)
                {
                    int x0 = area.X + (int)Math.Floor(ox * sx);
                    int x1 = Math.Max(x0 + 1, area.X + (int)Math.Floor((ox + 1) * sx));

                    samples.Clear();
                    int total = 0;
                    for (int y = y0; y < y1; y++)
                    {
                        for (int x = x0; x < x1; x++)
                        {
                            total++;
                            Rgba c = source.GetOrClear(x, y);
                            if (c.A >= alphaThreshold)
                            {
                                samples.Add(c);
                            }
                        }
                    }

                    if (samples.Count * 2 < total || samples.Count == 0)
                    {
                        result[ox, oy] = Rgba.Clear;
                        continue;
                    }

                    result[ox, oy] = Medoid(samples, rs, gs, bs);
                }
            }

            return result;
        }

        /// <summary>
        /// Medoide com grade de saída ancorada num ponto da fonte: o pixel de
        /// saída (i, j) cobre a fonte em [ox + i*step, ox + (i+1)*step) x
        /// [oy + j*step, ...). Serve para pôr o pivô (pés, empunhadura) exatamente
        /// numa quina ou no centro de um pixel de saída, com fator fracionário. Um
        /// pixel da fonte entra na área se o seu centro cai nela.
        /// </summary>
        public static PixelImage ResampleMedoidAnchored(PixelImage source, double ox, double oy, double step, int outWidth,
            int outHeight, int alphaThreshold)
        {
            PixelImage result = new PixelImage(outWidth, outHeight);
            List<Rgba> samples = new List<Rgba>(256);
            int capacity = (int)Math.Ceiling(step + 1) * (int)Math.Ceiling(step + 1);
            int[] rs = new int[capacity], gs = new int[capacity], bs = new int[capacity];
            for (int j = 0; j < outHeight; j++)
            {
                int y0 = (int)Math.Ceiling(oy + (j * step) - 0.5), y1 = (int)Math.Ceiling(oy + ((j + 1) * step) - 0.5);
                for (int i = 0; i < outWidth; i++)
                {
                    int x0 = (int)Math.Ceiling(ox + (i * step) - 0.5), x1 = (int)Math.Ceiling(ox + ((i + 1) * step) - 0.5);
                    samples.Clear();
                    int total = 0;
                    for (int y = y0; y < y1; y++)
                    {
                        for (int x = x0; x < x1; x++)
                        {
                            total++;
                            Rgba c = source.GetOrClear(x, y);
                            if (c.A >= alphaThreshold)
                            {
                                samples.Add(c);
                            }
                        }
                    }

                    if (samples.Count == 0 || samples.Count * 2 < total)
                    {
                        continue;
                    }

                    if (samples.Count > rs.Length)
                    {
                        rs = new int[samples.Count];
                        gs = new int[samples.Count];
                        bs = new int[samples.Count];
                    }

                    result[i, j] = Medoid(samples, rs, gs, bs);
                }
            }

            return result;
        }

        /// <summary>
        /// Gira a imagem em torno de (cx, cy) por <paramref name="degrees"/>
        /// (anti-horário, como na tela com y para cima), por amostragem bilinear
        /// com alfa pré-multiplicado, na resolução da fonte. A saída tem o
        /// tamanho da diagonal; (<paramref name="newCx"/>, <paramref name="newCy"/>)
        /// é onde o centro de rotação ficou.
        /// </summary>
        public static PixelImage Rotate(PixelImage source, double cx, double cy, double degrees, out double newCx, out double newCy)
        {
            double r = degrees * Math.PI / 180.0, cos = Math.Cos(r), sin = Math.Sin(r);
            int size = (int)Math.Ceiling(Math.Sqrt((source.Width * source.Width) + (source.Height * source.Height))) * 2;
            PixelImage result = new PixelImage(size, size);
            newCx = size * 0.5;
            newCy = size * 0.5;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Saída -> fonte: gira de volta (-graus). y da imagem cresce para baixo.
                    double dx = x + 0.5 - newCx, dyUp = -(y + 0.5 - newCy);
                    double sx = (dx * cos) + (dyUp * sin), syUp = (-dx * sin) + (dyUp * cos);
                    result[x, y] = Bilinear(source, cx + sx - 0.5, cy - syUp - 0.5);
                }
            }

            return result;
        }

        private static Rgba Bilinear(PixelImage source, double x, double y)
        {
            int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
            double fx = x - x0, fy = y - y0;
            double r = 0, g = 0, b = 0, a = 0;
            for (int k = 0; k < 4; k++)
            {
                int px = x0 + (k & 1), py = y0 + (k >> 1);
                double w = ((k & 1) == 1 ? fx : 1 - fx) * ((k >> 1) == 1 ? fy : 1 - fy);
                Rgba c = source.GetOrClear(px, py);
                double ca = c.A / 255.0 * w;
                r += c.R * ca;
                g += c.G * ca;
                b += c.B * ca;
                a += ca;
            }

            return a <= 1e-6 ? Rgba.Clear : new Rgba((int)Math.Round(r / a), (int)Math.Round(g / a), (int)Math.Round(b / a), (int)Math.Round(a * 255.0));
        }

        private static Rgba Medoid(List<Rgba> samples, int[] rs, int[] gs, int[] bs)
        {
            int n = samples.Count;
            for (int i = 0; i < n; i++)
            {
                rs[i] = samples[i].R;
                gs[i] = samples[i].G;
                bs[i] = samples[i].B;
            }

            Array.Sort(rs, 0, n);
            Array.Sort(gs, 0, n);
            Array.Sort(bs, 0, n);
            Rgba median = new Rgba(rs[n / 2], gs[n / 2], bs[n / 2], 255);

            Rgba best = samples[0];
            int bestDistance = int.MaxValue;
            for (int i = 0; i < n; i++)
            {
                int d = Rgba.DistanceSq(samples[i], median);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = samples[i];
                }
            }

            best.A = 255;
            return best;
        }

        /// <summary>
        /// Redução por média de área com alfa pré-multiplicado, para efeitos e
        /// brilhos, em que a transparência parcial é legítima. Aceita fator
        /// fracionário: cada pixel de fonte contribui pela área que cobre.
        /// </summary>
        public static PixelImage ResampleArea(PixelImage source, RectI area, int outWidth, int outHeight)
        {
            PixelImage result = new PixelImage(outWidth, outHeight);
            double sx = (double)area.W / outWidth, sy = (double)area.H / outHeight;
            for (int oy = 0; oy < outHeight; oy++)
            {
                double fy0 = area.Y + (oy * sy), fy1 = fy0 + sy;
                for (int ox = 0; ox < outWidth; ox++)
                {
                    double fx0 = area.X + (ox * sx), fx1 = fx0 + sx;
                    double r = 0, g = 0, b = 0, a = 0, weight = 0;
                    for (int y = (int)Math.Floor(fy0); y < (int)Math.Ceiling(fy1); y++)
                    {
                        double wy = Math.Min(fy1, y + 1) - Math.Max(fy0, y);
                        for (int x = (int)Math.Floor(fx0); x < (int)Math.Ceiling(fx1); x++)
                        {
                            double wx = Math.Min(fx1, x + 1) - Math.Max(fx0, x);
                            double w = wx * wy;
                            Rgba c = source.GetOrClear(x, y);
                            double ca = c.A / 255.0;
                            r += c.R * ca * w;
                            g += c.G * ca * w;
                            b += c.B * ca * w;
                            a += ca * w;
                            weight += w;
                        }
                    }

                    if (a <= 1e-6)
                    {
                        result[ox, oy] = Rgba.Clear;
                        continue;
                    }

                    result[ox, oy] = new Rgba((int)Math.Round(r / a), (int)Math.Round(g / a), (int)Math.Round(b / a),
                        (int)Math.Round(255.0 * a / weight));
                }
            }

            return result;
        }

        /// <summary>
        /// Média de área (alfa pré-multiplicado) com grade ancorada: o pixel de
        /// saída (i, j) cobre [ox + i*step, ox + (i+1)*step) x [oy + j*step, ...).
        /// Para efeitos com brilho cujo pivô precisa cair num ponto exato.
        /// </summary>
        public static PixelImage ResampleAreaAnchored(PixelImage source, double ox, double oy, double step, int outWidth, int outHeight)
        {
            PixelImage result = new PixelImage(outWidth, outHeight);
            for (int j = 0; j < outHeight; j++)
            {
                double fy0 = oy + (j * step), fy1 = fy0 + step;
                for (int i = 0; i < outWidth; i++)
                {
                    double fx0 = ox + (i * step), fx1 = fx0 + step;
                    double r = 0, g = 0, b = 0, a = 0, weight = 0;
                    for (int y = (int)Math.Floor(fy0); y < (int)Math.Ceiling(fy1); y++)
                    {
                        double wy = Math.Min(fy1, y + 1) - Math.Max(fy0, y);
                        for (int x = (int)Math.Floor(fx0); x < (int)Math.Ceiling(fx1); x++)
                        {
                            double w = (Math.Min(fx1, x + 1) - Math.Max(fx0, x)) * wy;
                            Rgba c = source.GetOrClear(x, y);
                            double ca = c.A / 255.0;
                            r += c.R * ca * w;
                            g += c.G * ca * w;
                            b += c.B * ca * w;
                            a += ca * w;
                            weight += w;
                        }
                    }

                    if (a > 1e-6)
                    {
                        result[i, j] = new Rgba((int)Math.Round(r / a), (int)Math.Round(g / a), (int)Math.Round(b / a),
                            (int)Math.Round(255.0 * a / weight));
                    }
                }
            }

            return result;
        }

        /// <summary>Cada cor opaca vira a cor mais próxima da paleta.</summary>
        public void Quantize(IList<Rgba> palette)
        {
            if (palette == null || palette.Count == 0)
            {
                return;
            }

            Dictionary<int, Rgba> cache = new Dictionary<int, Rgba>();
            for (int i = 0; i < Pixels.Length; i++)
            {
                Rgba c = Pixels[i];
                if (c.A == 0)
                {
                    continue;
                }

                int key = (c.R << 16) | (c.G << 8) | c.B;
                if (!cache.TryGetValue(key, out Rgba mapped))
                {
                    int best = int.MaxValue;
                    mapped = palette[0];
                    for (int p = 0; p < palette.Count; p++)
                    {
                        int d = Rgba.DistanceSq(c, palette[p]);
                        if (d < best)
                        {
                            best = d;
                            mapped = palette[p];
                        }
                    }

                    cache[key] = mapped;
                }

                Pixels[i] = new Rgba(mapped.R, mapped.G, mapped.B, c.A);
            }
        }

        /// <summary>Cores distintas dos pixels opacos (alfa &gt;= limiar).</summary>
        public List<Rgba> Palette(int alphaThreshold)
        {
            HashSet<int> seen = new HashSet<int>();
            List<Rgba> result = new List<Rgba>();
            foreach (Rgba c in Pixels)
            {
                if (c.A < alphaThreshold)
                {
                    continue;
                }

                int key = (c.R << 16) | (c.G << 8) | c.B;
                if (seen.Add(key))
                {
                    result.Add(new Rgba(c.R, c.G, c.B, 255));
                }
            }

            return result;
        }

        /// <summary>Apaga os pixels fora da máscara (true = mantém).</summary>
        public void Mask(bool[] keep)
        {
            for (int i = 0; i < Pixels.Length; i++)
            {
                if (!keep[i])
                {
                    Pixels[i] = Rgba.Clear;
                }
            }
        }

        /// <summary>
        /// Rótulos de ilhas 8-conexas de alfa &gt;= limiar. Devolve o número de
        /// ilhas; <paramref name="labels"/> recebe 0 para fundo e 1..n.
        /// </summary>
        public int Islands(int threshold, out int[] labels, out List<int> areas)
        {
            labels = new int[Pixels.Length];
            areas = new List<int> { 0 };
            Stack<int> stack = new Stack<int>();
            int count = 0;
            for (int start = 0; start < Pixels.Length; start++)
            {
                if (labels[start] != 0 || Pixels[start].A < threshold)
                {
                    continue;
                }

                count++;
                int area = 0;
                labels[start] = count;
                stack.Push(start);
                while (stack.Count > 0)
                {
                    int p = stack.Pop();
                    area++;
                    int px = p % Width, py = p / Width;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = px + dx, ny = py + dy;
                            if (!Contains(nx, ny))
                            {
                                continue;
                            }

                            int q = (ny * Width) + nx;
                            if (labels[q] != 0 || Pixels[q].A < threshold)
                            {
                                continue;
                            }

                            labels[q] = count;
                            stack.Push(q);
                        }
                    }
                }

                areas.Add(area);
            }

            return count;
        }

        /// <summary>Remove ilhas menores que <paramref name="minArea"/> (poeira de pixels soltos).</summary>
        public int RemoveSpecks(int threshold, int minArea)
        {
            int count = Islands(threshold, out int[] labels, out List<int> areas);
            int removed = 0;
            for (int i = 0; i < Pixels.Length; i++)
            {
                int label = labels[i];
                if (label != 0 && areas[label] < minArea)
                {
                    Pixels[i] = Rgba.Clear;
                    removed++;
                }
            }

            return count == 0 ? 0 : removed;
        }
    }
}
