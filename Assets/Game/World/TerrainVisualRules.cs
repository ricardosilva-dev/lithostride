namespace Lithostride.World
{
    /// <summary>Peça da cobertura (grama/musgo) de uma célula.</summary>
    public enum CapPiece : byte
    {
        None = 0,
        Top = 1,
        EndLeft = 2,
        EndRight = 3,
        Isolated = 4,
        SlopeRight = 6,
        SlopeLeft = 7,
        Half = 8
    }

    /// <summary>Tipos de detalhe espalhado por densidade (0 = nenhum).</summary>
    public enum DecalKind : byte
    {
        None = 0,
        Root = 1,
        Pebble = 2,
        Moss = 3
    }

    /// <summary>
    /// Dados estáticos das regras, sem Unity: cobertura e prioridade de cada
    /// material (índice = número do material no mapa) e quantas variantes de
    /// cada detalhe existem.
    /// </summary>
    public sealed class TerrainRuleSet
    {
        public int[] CapOfMaterial;
        public int[] PriorityOfMaterial;
        public int[] DecalVariants = new int[4];

        /// <summary>Materiais que recebem raízes e pedrinhas (terra) e musgo úmido (terra escura, pedra).</summary>
        public bool[] Earthy;
        public bool[] Rocky;

        public int Cap(int material)
        {
            return CapOfMaterial != null && material > 0 && material < CapOfMaterial.Length ? CapOfMaterial[material] : 0;
        }

        public int Priority(int material)
        {
            return PriorityOfMaterial != null && material > 0 && material < PriorityOfMaterial.Length ? PriorityOfMaterial[material] : 0;
        }

        public bool IsEarthy(int material)
        {
            return Earthy != null && material > 0 && material < Earthy.Length && Earthy[material];
        }

        public bool IsRocky(int material)
        {
            return Rocky != null && material > 0 && material < Rocky.Length && Rocky[material];
        }
    }

    /// <summary>O que desenhar numa célula, decidido só pelo mapa: nada aqui depende da Unity.</summary>
    public struct TerrainVisual
    {
        public int Material;
        public TerrainShape Shape;

        /// <summary>Quinas arredondadas do bloco inteiro (bits NO=1, NE=2, SE=4, SO=8).</summary>
        public int Round;

        /// <summary>Máscara de borda do bloco inteiro (0 = sem borda).</summary>
        public int Border;

        /// <summary>Lados expostos de rampa (1 = base, 2 = lado alto) ou meio-bloco (1 = leste, 2 = sul, 4 = oeste).</summary>
        public int ShapeSides;

        /// <summary>Tipo de cobertura (0 = nenhuma) e peça.</summary>
        public int CapType;

        public CapPiece Cap;

        /// <summary>Franja vertical (vizinho de cima/baixo de prioridade maior): material e lados (1 = norte, 2 = sul, 3 = ambos).</summary>
        public int FringeVMaterial;
        public int FringeV;

        /// <summary>Franja horizontal: material e lados (1 = leste, 2 = oeste, 3 = ambos).</summary>
        public int FringeHMaterial;
        public int FringeH;

        public int FringeVariant;

        public DecalKind Decal;
        public int DecalVariant;

        public int Moisture;

        /// <summary>Células abaixo da superfície original da coluna (0 na superfície; negativo acima).</summary>
        public int Depth;
    }

    /// <summary>
    /// Decide o visual de cada célula pelos vizinhos:
    /// <list type="bullet">
    /// <item>contorno só nos lados expostos ao ar; quinas arredondadas onde
    /// dois lados expostos se encontram; nunca entre dois sólidos;</item>
    /// <item>franja irregular do vizinho de prioridade maior (pedra avança
    /// sobre terra, minério sobre pedra); materiais construídos não têm franja;</item>
    /// <item>grama só no topo exposto ao céu aberto e na superfície original
    /// (não em cavernas nem no fundo de um poço cavado); musgo no topo exposto
    /// de pedra, ao ar livre ou em lugar úmido;</item>
    /// <item>detalhes (raízes perto da superfície, pedrinhas, musgo úmido) por
    /// densidade, sempre no mesmo lugar para a mesma célula.</item>
    /// </list>
    /// </summary>
    public static class TerrainVisualRules
    {
        public const int Grass = 1;
        public const int Moss = 2;

        /// <summary>Umidade a partir da qual pedra e terra escura criam musgo.</summary>
        public const int HumidMoisture = 130;

        /// <param name="topmostSolid">
        /// Linha da célula sólida mais alta da coluna agora (para "céu aberto");
        /// int.MinValue = procurar subindo pela coluna.
        /// </param>
        public static TerrainVisual Resolve(TerrainCellMap map, int x, int y, TerrainRuleSet rules, int topmostSolid = int.MinValue)
        {
            TerrainVisual visual = new TerrainVisual();
            visual.Material = map.MaterialAt(x, y);
            visual.Shape = map.ShapeAt(x, y);
            if (visual.Material == 0)
            {
                return visual;
            }

            visual.Moisture = map.MoistureAt(x, y);
            int surface = map.SurfaceAt(x);
            visual.Depth = surface == int.MinValue ? 0 : surface - y;

            TerrainShape n = map.ShapeAt(x, y + 1), s = map.ShapeAt(x, y - 1), e = map.ShapeAt(x + 1, y), w = map.ShapeAt(x - 1, y);
            switch (visual.Shape)
            {
                case TerrainShape.Full:
                    visual.Border = TerrainRules.BorderMask(n, map.ShapeAt(x + 1, y + 1), e, map.ShapeAt(x + 1, y - 1), s,
                        map.ShapeAt(x - 1, y - 1), w, map.ShapeAt(x - 1, y + 1));
                    visual.Round = RoundCorners(visual.Border);
                    Fringes(map, x, y, rules, ref visual);
                    visual.Decal = ChooseDecal(map, x, y, rules, visual, out visual.DecalVariant);
                    break;
                case TerrainShape.SlopeUpRight:
                    visual.ShapeSides = (!TerrainRules.CoversSide(s, TerrainRules.North) ? 1 : 0) |
                                        (!TerrainRules.CoversSide(e, TerrainRules.West) ? 2 : 0);
                    break;
                case TerrainShape.SlopeUpLeft:
                    visual.ShapeSides = (!TerrainRules.CoversSide(s, TerrainRules.North) ? 1 : 0) |
                                        (!TerrainRules.CoversSide(w, TerrainRules.East) ? 2 : 0);
                    break;
                case TerrainShape.HalfBottom:
                    visual.ShapeSides = (!TerrainRules.CoversSide(e, TerrainRules.West) ? 1 : 0) |
                                        (!TerrainRules.CoversSide(s, TerrainRules.North) ? 2 : 0) |
                                        (!TerrainRules.CoversSide(w, TerrainRules.East) ? 4 : 0);
                    break;
            }

            ResolveCap(map, x, y, rules, topmostSolid, n, e, w, ref visual);
            return visual;
        }

        /// <summary>Quina arredondada onde dois lados expostos (norte/sul com leste/oeste) se encontram.</summary>
        public static int RoundCorners(int border)
        {
            bool bn = (border & TerrainRules.North) != 0, be = (border & TerrainRules.East) != 0;
            bool bs = (border & TerrainRules.South) != 0, bw = (border & TerrainRules.West) != 0;
            return (bn && bw ? 1 : 0) | (bn && be ? 2 : 0) | (bs && be ? 4 : 0) | (bs && bw ? 8 : 0);
        }

        private static void ResolveCap(TerrainCellMap map, int x, int y, TerrainRuleSet rules, int topmostSolid, TerrainShape n,
            TerrainShape e, TerrainShape w, ref TerrainVisual visual)
        {
            int cap = rules.Cap(visual.Material);
            if (cap == 0 || !TerrainRules.TopExposed(visual.Shape, n))
            {
                return;
            }

            bool openSky = OpenSky(map, x, y, topmostSolid);
            if (cap == Grass)
            {
                int surface = map.SurfaceAt(x);
                bool onSurface = surface == int.MinValue || y >= surface;
                if (!openSky || !onSurface)
                {
                    return;
                }
            }
            else if (!openSky && visual.Moisture < HumidMoisture)
            {
                return;
            }

            visual.CapType = cap;
            switch (visual.Shape)
            {
                case TerrainShape.SlopeUpRight:
                    visual.Cap = CapPiece.SlopeRight;
                    return;
                case TerrainShape.SlopeUpLeft:
                    visual.Cap = CapPiece.SlopeLeft;
                    return;
                case TerrainShape.HalfBottom:
                    visual.Cap = CapPiece.Half;
                    return;
            }

            bool openLeft = !TerrainRules.CoversSide(w, TerrainRules.East);
            bool openRight = !TerrainRules.CoversSide(e, TerrainRules.West);
            visual.Cap = openLeft && openRight ? CapPiece.Isolated : openLeft ? CapPiece.EndLeft : openRight ? CapPiece.EndRight : CapPiece.Top;
        }

        /// <summary>Nada sólido acima da célula na coluna (céu aberto).</summary>
        public static bool OpenSky(TerrainCellMap map, int x, int y, int topmostSolid)
        {
            if (topmostSolid != int.MinValue)
            {
                return y >= topmostSolid;
            }

            for (int yy = y + 1; yy < map.OriginY + map.Height; yy++)
            {
                if (map.MaterialAt(x, yy) != 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static void Fringes(TerrainCellMap map, int x, int y, TerrainRuleSet rules, ref TerrainVisual visual)
        {
            int own = rules.Priority(visual.Material);
            if (own <= 0)
            {
                return;
            }

            int north = Invader(map, x, y + 1, visual.Material, own, rules);
            int south = Invader(map, x, y - 1, visual.Material, own, rules);
            int east = Invader(map, x + 1, y, visual.Material, own, rules);
            int west = Invader(map, x - 1, y, visual.Material, own, rules);
            Pick(north, south, rules, out visual.FringeVMaterial, out visual.FringeV);
            Pick(east, west, rules, out visual.FringeHMaterial, out visual.FringeH);
            visual.FringeVariant = TerrainRules.Hash(x, y, 31) % 2;
        }

        /// <summary>Material do vizinho que avança sobre esta célula (prioridade maior, bloco inteiro), ou 0.</summary>
        private static int Invader(TerrainCellMap map, int x, int y, int own, int ownPriority, TerrainRuleSet rules)
        {
            int m = map.MaterialAt(x, y);
            if (m == 0 || m == own || map.ShapeAt(x, y) != TerrainShape.Full)
            {
                return 0;
            }

            return rules.Priority(m) > ownPriority ? m : 0;
        }

        /// <summary>Dois lados opostos: o mesmo material nos dois vira franja dupla; senão vence a maior prioridade.</summary>
        private static void Pick(int first, int second, TerrainRuleSet rules, out int material, out int sides)
        {
            if (first != 0 && first == second)
            {
                material = first;
                sides = 3;
            }
            else if (first != 0 && (second == 0 || rules.Priority(first) >= rules.Priority(second)))
            {
                material = first;
                sides = 1;
            }
            else if (second != 0)
            {
                material = second;
                sides = 2;
            }
            else
            {
                material = 0;
                sides = 0;
            }
        }

        private static DecalKind ChooseDecal(TerrainCellMap map, int x, int y, TerrainRuleSet rules, TerrainVisual visual, out int variant)
        {
            variant = 0;
            int roll = TerrainRules.Hash(x, y, 53) % 1000;
            DecalKind kind = DecalKind.None;
            bool earthy = rules.IsEarthy(visual.Material), rocky = rules.IsRocky(visual.Material);

            // Raízes: terra logo abaixo da superfície original (1 a 4 células), com densidade que cai com a profundidade.
            if (earthy && visual.Depth >= 1 && visual.Depth <= 4 && roll < 160 - (visual.Depth * 30))
            {
                kind = DecalKind.Root;
            }
            else if ((earthy || rocky) && visual.Moisture >= HumidMoisture && roll >= 800 && roll < 800 + (visual.Moisture / 3))
            {
                kind = DecalKind.Moss;
            }
            else if (earthy && roll >= 600 && roll < 628)
            {
                kind = DecalKind.Pebble;
            }

            int count = rules.DecalVariants != null && (int)kind < rules.DecalVariants.Length ? rules.DecalVariants[(int)kind] : 0;
            if (kind == DecalKind.None || count == 0)
            {
                return DecalKind.None;
            }

            variant = TerrainRules.Hash(x, y, 59) % count;
            return kind;
        }
    }
}
