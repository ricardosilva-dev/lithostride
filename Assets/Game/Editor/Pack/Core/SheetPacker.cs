using System;
using System.Collections.Generic;

namespace Lithostride.EditorTools
{
    /// <summary>
    /// Monta uma folha a partir de imagens soltas, em prateleiras, com borda
    /// de segurança. Com <c>extrude</c>, a borda recebe a cópia dos pixels da
    /// beirada: amostragem que escorrega meio texel (comum em tiles lado a
    /// lado) pega a mesma cor, e não o vizinho da folha — é o que impede
    /// frestas entre blocos no Tilemap.
    /// </summary>
    public sealed class SheetPacker
    {
        private readonly int maxWidth;
        private readonly int padding;
        private readonly bool extrude;
        private readonly List<PixelImage> images = new List<PixelImage>();
        private readonly List<DerivedSprite> sprites = new List<DerivedSprite>();

        public SheetPacker(int maxWidth, int padding, bool extrude)
        {
            this.maxWidth = maxWidth;
            this.padding = padding;
            this.extrude = extrude;
        }

        public int Count => images.Count;

        /// <summary>Acrescenta uma imagem; o retângulo do sprite é preenchido em <see cref="Pack"/>.</summary>
        public void Add(PixelImage image, DerivedSprite sprite)
        {
            images.Add(image);
            sprites.Add(sprite);
        }

        /// <summary>
        /// Grade uniforme (todas as imagens do mesmo tamanho, na ordem de
        /// entrada), ou prateleiras por altura quando os tamanhos variam.
        /// </summary>
        public DerivedSheet Pack(string assetPath, int pixelsPerUnit, string category, bool uniformGrid)
        {
            int n = images.Count;
            int[] xs = new int[n], ys = new int[n];
            int width = 0, height = 0;

            if (uniformGrid && n > 0)
            {
                int cw = images[0].Width + (2 * padding), ch = images[0].Height + (2 * padding);
                int columns = Math.Max(1, Math.Min(n, maxWidth / cw));
                for (int i = 0; i < n; i++)
                {
                    xs[i] = ((i % columns) * cw) + padding;
                    ys[i] = ((i / columns) * ch) + padding;
                }

                width = columns * cw;
                height = ((n + columns - 1) / columns) * ch;
            }
            else
            {
                // Prateleiras na ordem de entrada: a folha fica legível (mesma ordem do manifesto).
                int x = 0, y = 0, shelf = 0;
                for (int i = 0; i < n; i++)
                {
                    int w = images[i].Width + (2 * padding), h = images[i].Height + (2 * padding);
                    if (x + w > maxWidth && x > 0)
                    {
                        x = 0;
                        y += shelf;
                        shelf = 0;
                    }

                    xs[i] = x + padding;
                    ys[i] = y + padding;
                    x += w;
                    shelf = Math.Max(shelf, h);
                    width = Math.Max(width, x);
                }

                height = y + shelf;
            }

            PixelImage atlas = new PixelImage(Math.Max(1, width), Math.Max(1, height));
            DerivedSheet sheet = new DerivedSheet
            {
                AssetPath = assetPath,
                Image = atlas,
                PixelsPerUnit = pixelsPerUnit,
                Category = category
            };

            for (int i = 0; i < n; i++)
            {
                PixelImage image = images[i];
                atlas.Blit(image, xs[i], ys[i], false);
                if (extrude)
                {
                    Extrude(atlas, xs[i], ys[i], image.Width, image.Height);
                }

                DerivedSprite sprite = sprites[i];
                sprite.Rect = new RectI(xs[i], ys[i], image.Width, image.Height);
                sheet.Sprites.Add(sprite);
            }

            return sheet;
        }

        private void Extrude(PixelImage atlas, int x0, int y0, int w, int h)
        {
            for (int p = 1; p <= padding; p++)
            {
                for (int x = 0; x < w; x++)
                {
                    atlas[x0 + x, y0 - p] = atlas[x0 + x, y0];
                    atlas[x0 + x, y0 + h - 1 + p] = atlas[x0 + x, y0 + h - 1];
                }

                for (int y = -p; y < h + p; y++)
                {
                    int yy = Math.Min(Math.Max(y, -padding), h - 1 + padding);
                    atlas[x0 - p, y0 + yy] = atlas[x0, y0 + yy];
                    atlas[x0 + w - 1 + p, y0 + yy] = atlas[x0 + w - 1, y0 + yy];
                }
            }
        }
    }
}
