using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Lithostride.World
{
    /// <summary>Material de terreno: tiles-máscara por forma, franjas e comportamento ao minerar.</summary>
    [Serializable]
    public sealed class TerrainMaterialDef
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private int capType;
        [SerializeField] private int priority;
        [SerializeField] private bool earthy;
        [SerializeField] private bool rocky;
        [SerializeField] private float breakTime = 0.4f;
        [SerializeField] private Sprite dropIcon;

        /// <summary>Bloco inteiro nas 16 combinações de quinas arredondadas.</summary>
        [SerializeField] private TileBase[] full;

        [SerializeField] private TileBase slopeUpRight;
        [SerializeField] private TileBase slopeUpLeft;
        [SerializeField] private TileBase half;

        /// <summary>Franjas: 6 lados (n, s, ns, l, o, lo) x 2 variantes; vazio se o material não avança sobre vizinhos.</summary>
        [SerializeField] private TileBase[] fringes;

        /// <summary>Para o serializador da Unity.</summary>
        public TerrainMaterialDef()
        {
        }

        public TerrainMaterialDef(string id, string displayName, int capType, int priority, bool earthy, bool rocky, float breakTime,
            Sprite dropIcon, TileBase[] full, TileBase slopeUpRight, TileBase slopeUpLeft, TileBase half, TileBase[] fringes)
        {
            this.id = id;
            this.displayName = displayName;
            this.capType = capType;
            this.priority = priority;
            this.earthy = earthy;
            this.rocky = rocky;
            this.breakTime = breakTime;
            this.dropIcon = dropIcon;
            this.full = full;
            this.slopeUpRight = slopeUpRight;
            this.slopeUpLeft = slopeUpLeft;
            this.half = half;
            this.fringes = fringes;
        }

        public string Id => id;
        public string DisplayName => displayName;
        public int CapType => capType;
        public int Priority => priority;
        public bool Earthy => earthy;
        public bool Rocky => rocky;
        public float BreakTime => breakTime;
        public Sprite DropIcon => dropIcon;

        public TileBase TileFor(TerrainShape shape, int round = 0)
        {
            switch (shape)
            {
                case TerrainShape.Full: return full != null && full.Length > 0 ? full[Mathf.Clamp(round, 0, full.Length - 1)] : null;
                case TerrainShape.SlopeUpRight: return slopeUpRight;
                case TerrainShape.SlopeUpLeft: return slopeUpLeft;
                case TerrainShape.HalfBottom: return half;
                default: return null;
            }
        }

        /// <summary>Franja: lado 0..5 (n, s, ns, l, o, lo), variante 0..1.</summary>
        public TileBase Fringe(int side, int variant)
        {
            int index = (side * 2) + variant;
            return fringes != null && index >= 0 && index < fringes.Length ? fringes[index] : null;
        }
    }

    /// <summary>Cobertura (grama, musgo): uma peça por situação do topo.</summary>
    [Serializable]
    public sealed class TerrainCapDef
    {
        [SerializeField] private string id;

        /// <summary>Peças na ordem: topo, ponta esquerda, ponta direita, isolado, rampa ↗, rampa ↖, meio-bloco.</summary>
        [SerializeField] private TileBase[] pieces;

        /// <summary>Para o serializador da Unity.</summary>
        public TerrainCapDef()
        {
        }

        public TerrainCapDef(string id, TileBase[] pieces)
        {
            this.id = id;
            this.pieces = pieces;
        }

        public string Id => id;

        public TileBase TileFor(CapPiece piece)
        {
            int index;
            switch (piece)
            {
                case CapPiece.Top: index = 0; break;
                case CapPiece.EndLeft: index = 1; break;
                case CapPiece.EndRight: index = 2; break;
                case CapPiece.Isolated: index = 3; break;
                case CapPiece.SlopeRight: index = 4; break;
                case CapPiece.SlopeLeft: index = 5; break;
                case CapPiece.Half: index = 6; break;
                default: return null;
            }

            return pieces != null && index < pieces.Length ? pieces[index] : null;
        }
    }

    /// <summary>Variantes de um tipo de detalhe espalhado (raiz, pedrinha, musgo).</summary>
    [Serializable]
    public sealed class TerrainDecalSet
    {
        [SerializeField] private TileBase[] tiles;

        public TerrainDecalSet(TileBase[] tiles)
        {
            this.tiles = tiles;
        }

        /// <summary>Para o serializador da Unity.</summary>
        public TerrainDecalSet()
        {
        }

        public int Count => tiles == null ? 0 : tiles.Length;

        public TileBase Tile(int variant)
        {
            return tiles != null && variant >= 0 && variant < tiles.Length ? tiles[variant] : null;
        }
    }

    /// <summary>
    /// Definição estática do terreno: materiais, coberturas, bordas, franjas,
    /// detalhes e os materiais de render (shader do terreno contínuo). Gerada
    /// pelo pipeline do pack; os tiles ficam como sub-assets deste arquivo.
    /// </summary>
    public sealed class TerrainPalette : ScriptableObject
    {
        [SerializeField] private TerrainMaterialDef[] materials;
        [SerializeField] private TerrainCapDef[] caps;
        [SerializeField] private TileBase[] borders;
        [SerializeField] private TileBase[] slopeRightBorders;
        [SerializeField] private TileBase[] slopeLeftBorders;
        [SerializeField] private TileBase[] halfBorders;
        [SerializeField] private TileBase[] cracks;
        [SerializeField] private TerrainDecalSet[] decals;
        [SerializeField] private Material solidMaterial;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material capMaterial;

        public int MaterialCount => materials == null ? 0 : materials.Length;
        public Material SolidMaterial => solidMaterial;
        public Material WallMaterial => wallMaterial;
        public Material CapMaterial => capMaterial;

        public void Assign(TerrainMaterialDef[] materialDefs, TerrainCapDef[] capDefs, TileBase[] borderTiles, TileBase[] slopeRight,
            TileBase[] slopeLeft, TileBase[] half, TileBase[] crackTiles, TerrainDecalSet[] decalSets, Material solid, Material wall,
            Material cap)
        {
            materials = materialDefs;
            caps = capDefs;
            borders = borderTiles;
            slopeRightBorders = slopeRight;
            slopeLeftBorders = slopeLeft;
            halfBorders = half;
            cracks = crackTiles;
            decals = decalSets;
            solidMaterial = solid;
            wallMaterial = wall;
            capMaterial = cap;
        }

        /// <summary>Material pelo número usado no mapa (1..n); nulo para 0.</summary>
        public TerrainMaterialDef Material(int number)
        {
            return number >= 1 && materials != null && number <= materials.Length ? materials[number - 1] : null;
        }

        /// <summary>Número do material pelo id (0 se não existe).</summary>
        public int NumberOf(string id)
        {
            for (int i = 0; i < MaterialCount; i++)
            {
                if (materials[i].Id == id)
                {
                    return i + 1;
                }
            }

            return 0;
        }

        public TerrainCapDef Cap(int capType)
        {
            return capType >= 1 && caps != null && capType <= caps.Length ? caps[capType - 1] : null;
        }

        /// <summary>Borda do visual resolvido de uma célula (bloco inteiro, rampa ou meio-bloco).</summary>
        public TileBase Border(TerrainVisual visual)
        {
            switch (visual.Shape)
            {
                case TerrainShape.Full:
                    return borders != null && visual.Border > 0 && visual.Border < borders.Length ? borders[visual.Border] : null;
                case TerrainShape.SlopeUpRight:
                    return Pick(slopeRightBorders, visual.ShapeSides);
                case TerrainShape.SlopeUpLeft:
                    return Pick(slopeLeftBorders, visual.ShapeSides);
                case TerrainShape.HalfBottom:
                    return Pick(halfBorders, visual.ShapeSides);
                default:
                    return null;
            }
        }

        public TileBase BorderMask(int mask)
        {
            return borders != null && mask > 0 && mask < borders.Length ? borders[mask] : null;
        }

        private static TileBase Pick(TileBase[] set, int index)
        {
            return set != null && index >= 0 && index < set.Length ? set[index] : null;
        }

        /// <summary>Rachadura do estágio 1..3 (0 = nenhuma).</summary>
        public TileBase Crack(int stage)
        {
            return cracks != null && stage >= 1 && stage <= cracks.Length ? cracks[stage - 1] : null;
        }

        public TileBase Decal(DecalKind kind, int variant)
        {
            int index = (int)kind;
            return decals != null && index > 0 && index < decals.Length ? decals[index].Tile(variant) : null;
        }

        /// <summary>Regras de vizinhança (sem Unity) a partir das definições.</summary>
        public TerrainRuleSet Rules()
        {
            int n = MaterialCount + 1;
            TerrainRuleSet rules = new TerrainRuleSet
            {
                CapOfMaterial = new int[n],
                PriorityOfMaterial = new int[n],
                Earthy = new bool[n],
                Rocky = new bool[n],
                DecalVariants = new int[4]
            };
            for (int i = 1; i < n; i++)
            {
                TerrainMaterialDef m = materials[i - 1];
                rules.CapOfMaterial[i] = m.CapType;
                rules.PriorityOfMaterial[i] = m.Priority;
                rules.Earthy[i] = m.Earthy;
                rules.Rocky[i] = m.Rocky;
            }

            for (int k = 1; k < rules.DecalVariants.Length && decals != null && k < decals.Length; k++)
            {
                rules.DecalVariants[k] = decals[k].Count;
            }

            return rules;
        }
    }
}
