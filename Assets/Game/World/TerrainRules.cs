namespace Lithostride.World
{
    /// <summary>Forma do bloco na célula. Rampas sobem para o lado indicado.</summary>
    public enum TerrainShape : byte
    {
        Empty = 0,
        Full = 1,
        SlopeUpRight = 2,
        SlopeUpLeft = 3,
        HalfBottom = 4
    }

    /// <summary>
    /// Regras de vizinhança do terreno, sem dependência da Unity: quais lados
    /// de um bloco ficam expostos ao ar, quais cantos internos aparecem e onde
    /// cresce cobertura (grama, musgo). O mesmo código decide o visual no
    /// editor, no jogo e na bancada de testes da arte.
    /// </summary>
    public static class TerrainRules
    {
        public const int North = 1;
        public const int East = 2;
        public const int South = 4;
        public const int West = 8;
        public const int CornerNorthEast = 16;
        public const int CornerSouthEast = 32;
        public const int CornerSouthWest = 64;
        public const int CornerNorthWest = 128;

        /// <summary>Máscaras possíveis de borda: 4 lados + 4 cantos internos.</summary>
        public const int BorderMaskCount = 256;

        /// <summary>
        /// O bloco nesta forma cobre por inteiro o lado dado? Rampas cobrem a
        /// base e o lado alto; meio-bloco só cobre a base. Um lado coberto
        /// esconde a borda do vizinho encostado nele.
        /// </summary>
        public static bool CoversSide(TerrainShape shape, int side)
        {
            switch (shape)
            {
                case TerrainShape.Full:
                    return true;
                case TerrainShape.SlopeUpRight:
                    return side == South || side == East;
                case TerrainShape.SlopeUpLeft:
                    return side == South || side == West;
                case TerrainShape.HalfBottom:
                    return side == South;
                default:
                    return false;
            }
        }

        public static int Opposite(int side)
        {
            switch (side)
            {
                case North: return South;
                case South: return North;
                case East: return West;
                default: return East;
            }
        }

        /// <summary>
        /// Máscara de borda de um bloco inteiro, a partir das formas dos oito
        /// vizinhos (ordem: N, NE, E, SE, S, SW, W, NW). Um lado fica exposto
        /// quando o vizinho não cobre o lado encostado. Canto interno: os dois
        /// lados vizinhos cobertos e a diagonal vazia — é onde, sem ele, a
        /// quina do terreno ficaria sem contorno.
        /// </summary>
        public static int BorderMask(TerrainShape n, TerrainShape ne, TerrainShape e, TerrainShape se, TerrainShape s,
            TerrainShape sw, TerrainShape w, TerrainShape nw)
        {
            int mask = 0;
            bool coveredN = CoversSide(n, South);
            bool coveredE = CoversSide(e, West);
            bool coveredS = CoversSide(s, North);
            bool coveredW = CoversSide(w, East);

            if (!coveredN) mask |= North;
            if (!coveredE) mask |= East;
            if (!coveredS) mask |= South;
            if (!coveredW) mask |= West;

            if (coveredN && coveredE && ne != TerrainShape.Full) mask |= CornerNorthEast;
            if (coveredS && coveredE && se != TerrainShape.Full) mask |= CornerSouthEast;
            if (coveredS && coveredW && sw != TerrainShape.Full) mask |= CornerSouthWest;
            if (coveredN && coveredW && nw != TerrainShape.Full) mask |= CornerNorthWest;
            return mask;
        }

        /// <summary>
        /// Recebe cobertura (grama) no topo: a forma tem topo exposto ao ar.
        /// Rampas e meios-blocos sempre têm; bloco inteiro, se o de cima não
        /// cobre a base.
        /// </summary>
        public static bool TopExposed(TerrainShape shape, TerrainShape above)
        {
            if (shape == TerrainShape.Empty)
            {
                return false;
            }

            return !CoversSide(above, South);
        }

        /// <summary>Ruído inteiro determinístico, não negativo: variantes estáveis a cada reconstrução.</summary>
        public static int Hash(int x, int y, int salt)
        {
            unchecked
            {
                int h = (x * 374761393) + (y * 668265263) + (salt * 1274126177);
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return h & 0x7FFFFFFF;
            }
        }
    }
}
