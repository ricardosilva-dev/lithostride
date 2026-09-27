using System;

namespace Lithostride.World
{
    /// <summary>
    /// Estado do terreno: material e forma por célula, o material da parede de
    /// fundo, a umidade (0..255, do gerador: cavernas e depressões são mais
    /// úmidas) e a altura da superfície original de cada coluna. É a fonte da
    /// verdade; os Tilemaps só desenham e colidem o que está aqui. Material 0
    /// = vazio; os demais são o índice + 1 na paleta de materiais.
    /// </summary>
    [Serializable]
    public sealed class TerrainCellMap
    {
        public int OriginX;
        public int OriginY;
        public int Width;
        public int Height;

        public byte[] Materials;
        public byte[] Shapes;
        public byte[] Walls;
        public byte[] Moisture;

        /// <summary>Linha da célula mais alta sólida de cada coluna quando o mundo foi gerado.</summary>
        public int[] Surface;

        /// <summary>Semente do gerador (0 = mapa montado à mão).</summary>
        public int Seed;

        /// <summary>Para o serializador da Unity.</summary>
        public TerrainCellMap()
        {
        }

        public TerrainCellMap(int originX, int originY, int width, int height)
        {
            OriginX = originX;
            OriginY = originY;
            Width = width;
            Height = height;
            Materials = new byte[width * height];
            Shapes = new byte[width * height];
            Walls = new byte[width * height];
            Moisture = new byte[width * height];
            Surface = new int[width];
        }

        public bool Contains(int x, int y)
        {
            return x >= OriginX && y >= OriginY && x < OriginX + Width && y < OriginY + Height;
        }

        private int Index(int x, int y)
        {
            return ((y - OriginY) * Width) + (x - OriginX);
        }

        public int MaterialAt(int x, int y)
        {
            return Contains(x, y) ? Materials[Index(x, y)] : 0;
        }

        /// <summary>Forma na célula; fora do mapa é vazio.</summary>
        public TerrainShape ShapeAt(int x, int y)
        {
            return Contains(x, y) ? (TerrainShape)Shapes[Index(x, y)] : TerrainShape.Empty;
        }

        public int WallAt(int x, int y)
        {
            return Contains(x, y) ? Walls[Index(x, y)] : 0;
        }

        public int MoistureAt(int x, int y)
        {
            return Contains(x, y) && Moisture != null && Moisture.Length == Materials.Length ? Moisture[Index(x, y)] : 0;
        }

        /// <summary>Superfície original da coluna (int.MinValue fora do mapa ou sem dado).</summary>
        public int SurfaceAt(int x)
        {
            int i = x - OriginX;
            return Surface != null && i >= 0 && i < Surface.Length ? Surface[i] : int.MinValue;
        }

        public void Set(int x, int y, int material, TerrainShape shape)
        {
            if (!Contains(x, y))
            {
                return;
            }

            int i = Index(x, y);
            bool empty = material == 0 || shape == TerrainShape.Empty;
            Materials[i] = empty ? (byte)0 : (byte)material;
            Shapes[i] = empty ? (byte)TerrainShape.Empty : (byte)shape;
        }

        public void SetWall(int x, int y, int material)
        {
            if (Contains(x, y))
            {
                Walls[Index(x, y)] = (byte)material;
            }
        }

        public void SetMoisture(int x, int y, int value)
        {
            if (Contains(x, y))
            {
                Moisture[Index(x, y)] = (byte)Math.Max(0, Math.Min(255, value));
            }
        }

        /// <summary>Guarda a superfície atual de cada coluna como a original (o gerador chama no fim).</summary>
        public void RecordSurface()
        {
            for (int i = 0; i < Width; i++)
            {
                int x = OriginX + i;
                Surface[i] = int.MinValue;
                for (int y = OriginY + Height - 1; y >= OriginY; y--)
                {
                    if (MaterialAt(x, y) != 0)
                    {
                        Surface[i] = y;
                        break;
                    }
                }
            }
        }

        public TerrainCellMap Clone()
        {
            TerrainCellMap copy = new TerrainCellMap(OriginX, OriginY, Width, Height) { Seed = Seed };
            Array.Copy(Materials, copy.Materials, Materials.Length);
            Array.Copy(Shapes, copy.Shapes, Shapes.Length);
            Array.Copy(Walls, copy.Walls, Walls.Length);
            if (Moisture != null && Moisture.Length == copy.Moisture.Length)
            {
                Array.Copy(Moisture, copy.Moisture, Moisture.Length);
            }

            if (Surface != null && Surface.Length == copy.Surface.Length)
            {
                Array.Copy(Surface, copy.Surface, Surface.Length);
            }

            return copy;
        }

        /// <summary>Copia o conteúdo de outro mapa do mesmo tamanho (reset sem alocar).</summary>
        public void CopyFrom(TerrainCellMap other)
        {
            Array.Copy(other.Materials, Materials, Materials.Length);
            Array.Copy(other.Shapes, Shapes, Shapes.Length);
            Array.Copy(other.Walls, Walls, Walls.Length);
            if (other.Moisture != null && Moisture != null && other.Moisture.Length == Moisture.Length)
            {
                Array.Copy(other.Moisture, Moisture, Moisture.Length);
            }
        }
    }
}
