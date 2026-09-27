using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Lithostride.World
{
    /// <summary>
    /// Terreno em jogo. O estado (material, forma, parede, umidade) fica em
    /// <see cref="TerrainCellMap"/>; os Tilemaps só refletem esse estado:
    /// <list type="bullet">
    /// <item>sólido (máscaras de forma pintadas pelo shader do terreno
    /// contínuo, com colisão), dividido em seções de colunas: mudar um bloco
    /// refaz só o contorno de colisão da sua seção;</item>
    /// <item>franjas entre materiais (vertical e horizontal), detalhes
    /// (raízes, pedrinhas, musgo), bordas nos lados expostos, cobertura
    /// (grama/musgo) e rachaduras de mineração;</item>
    /// <item>parede de fundo, sem colisão, atrás do ar abaixo da superfície.</item>
    /// </list>
    /// Os tiles não são guardados na cena: são pintados a partir do mapa ao
    /// carregar (determinístico: o mesmo mapa pinta sempre o mesmo visual).
    /// Cada mudança refaz a célula e as oito vizinhas e avisa quem se apoia
    /// no terreno.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public sealed class TerrainGrid : MonoBehaviour
    {
        [SerializeField] private TerrainPalette palette;
        [SerializeField] private Grid grid;
        [SerializeField] private Tilemap[] solidSections;
        [SerializeField, Min(1)] private int sectionWidth = 32;
        [SerializeField] private Tilemap wallLayer;
        [SerializeField] private Tilemap fringeVLayer;
        [SerializeField] private Tilemap fringeHLayer;
        [SerializeField] private Tilemap decalLayer;
        [SerializeField] private Tilemap borderLayer;
        [SerializeField] private Tilemap capLayer;
        [SerializeField] private Tilemap crackLayer;
        [SerializeField] private TerrainCellMap initialMap;
        [SerializeField] private Color damagedColor = new Color(0.72f, 0.66f, 0.64f, 1f);

        private TerrainCellMap map;
        private TerrainRuleSet rules;
        private Texture2D tint;
        private bool painted;

        private static readonly int TintId = Shader.PropertyToID("_TerrainTint");
        private static readonly int TintRectId = Shader.PropertyToID("_TerrainTintRect");

        /// <summary>Uma célula mudou de forma ou material (x, y).</summary>
        public event Action<int, int> CellChanged;

        /// <summary>O terreno inteiro voltou ao estado inicial.</summary>
        public event Action TerrainReset;

        public TerrainPalette Palette => palette;
        public TerrainCellMap Map => EnsureMap();
        public int SectionCount => solidSections == null ? 0 : solidSections.Length;
        public Tilemap Section(int index) => solidSections[index];

        /// <summary>Tempo da última pintura completa, em ms (diagnóstico).</summary>
        public float LastRebuildMilliseconds { get; private set; }

        public void Initialize(TerrainPalette terrainPalette, Grid terrainGrid, Tilemap[] sections, int columnsPerSection, Tilemap walls,
            Tilemap fringeV, Tilemap fringeH, Tilemap decals, Tilemap borders, Tilemap caps, Tilemap cracks, TerrainCellMap initial)
        {
            palette = terrainPalette;
            grid = terrainGrid;
            solidSections = sections;
            sectionWidth = columnsPerSection;
            wallLayer = walls;
            fringeVLayer = fringeV;
            fringeHLayer = fringeH;
            decalLayer = decals;
            borderLayer = borders;
            capLayer = caps;
            crackLayer = cracks;
            initialMap = initial;
            map = null;
            rules = null;
        }

        private void Awake()
        {
            EnsureMap();
            if (!painted)
            {
                RebuildAll();
                RefreshCollision();
            }
        }

        private void OnDestroy()
        {
            if (tint != null)
            {
                Destroy(tint);
            }
        }

        private TerrainCellMap EnsureMap()
        {
            if (map == null && initialMap != null && initialMap.Materials != null)
            {
                map = initialMap.Clone();
            }

            if (rules == null && palette != null)
            {
                rules = palette.Rules();
            }

            return map;
        }

        // ------------------------------------------------------------------ consultas

        public Vector3Int WorldToCell(Vector3 position)
        {
            return grid.WorldToCell(position);
        }

        public Vector3 CellCenter(Vector3Int cell)
        {
            return grid.GetCellCenterWorld(cell);
        }

        public TerrainShape ShapeAt(Vector3Int cell)
        {
            return EnsureMap() == null ? TerrainShape.Empty : map.ShapeAt(cell.x, cell.y);
        }

        public int MaterialAt(Vector3Int cell)
        {
            return EnsureMap() == null ? 0 : map.MaterialAt(cell.x, cell.y);
        }

        public bool HasBlock(Vector3Int cell)
        {
            return MaterialAt(cell) != 0;
        }

        public bool InBounds(Vector3Int cell)
        {
            return EnsureMap() != null && map.Contains(cell.x, cell.y);
        }

        /// <summary>Linha da célula sólida mais alta da coluna agora (int.MinValue se não houver).</summary>
        public int TopSolid(int x)
        {
            if (EnsureMap() == null)
            {
                return int.MinValue;
            }

            for (int y = map.OriginY + map.Height - 1; y >= map.OriginY; y--)
            {
                if (map.MaterialAt(x, y) != 0)
                {
                    return y;
                }
            }

            return int.MinValue;
        }

        /// <summary>
        /// Ponto de pé livre perto de x: a primeira coluna (alternando para os
        /// lados) com chão inteiro e <paramref name="clearance"/> células de ar
        /// acima. Usado por teleporte e renascer: nunca dentro de sólido.
        /// </summary>
        public bool FindStandingSpot(float x, int clearance, int searchRadius, out Vector2 feet)
        {
            int cx = Mathf.FloorToInt(x);
            for (int d = 0; d <= searchRadius; d++)
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    int column = cx + (d * s);
                    int top = TopSolid(column);
                    if (top == int.MinValue || map.ShapeAt(column, top) != TerrainShape.Full)
                    {
                        continue;
                    }

                    bool free = true;
                    for (int y = top + 1; y <= top + clearance && free; y++)
                    {
                        free = map.MaterialAt(column, y) == 0 && map.MaterialAt(column + 1, y) == 0 && map.MaterialAt(column - 1, y) == 0;
                    }

                    if (free)
                    {
                        feet = new Vector2(column + 0.5f, top + 1f);
                        return true;
                    }

                    if (d == 0)
                    {
                        break;
                    }
                }
            }

            feet = new Vector2(x, 0f);
            return false;
        }

        // ------------------------------------------------------------------ mudanças

        /// <summary>Remove o bloco. Falso se não havia bloco.</summary>
        public bool Break(Vector3Int cell)
        {
            if (!HasBlock(cell))
            {
                return false;
            }

            map.Set(cell.x, cell.y, 0, TerrainShape.Empty);
            SetDamage(cell, 0f);
            RefreshAround(cell.x, cell.y);
            CellChanged?.Invoke(cell.x, cell.y);
            return true;
        }

        /// <summary>Põe um bloco numa célula vazia dentro do mapa. Falso se não coube.</summary>
        public bool Place(Vector3Int cell, int material, TerrainShape shape)
        {
            if (!InBounds(cell) || HasBlock(cell) || palette.Material(material) == null || shape == TerrainShape.Empty)
            {
                return false;
            }

            map.Set(cell.x, cell.y, material, shape);
            RefreshAround(cell.x, cell.y);
            CellChanged?.Invoke(cell.x, cell.y);
            return true;
        }

        /// <summary>Rachaduras (três estágios) e leve escurecimento conforme o progresso da quebra (0 = intacto).</summary>
        public void SetDamage(Vector3Int cell, float amount)
        {
            amount = Mathf.Clamp01(amount);
            int stage = amount <= 0.001f ? 0 : Mathf.Min(3, 1 + (int)(amount * 3f));
            if (crackLayer != null)
            {
                crackLayer.SetTile(cell, HasBlock(cell) ? palette.Crack(stage) : null);
            }

            Tilemap section = SectionFor(cell.x);
            if (section != null && section.HasTile(cell))
            {
                section.SetColor(cell, Color.Lerp(Color.white, damagedColor, amount));
            }
        }

        /// <summary>Volta ao terreno inicial.</summary>
        public void ResetTerrain()
        {
            EnsureMap();
            map.CopyFrom(initialMap);
            RebuildAll();
            TerrainReset?.Invoke();
        }

        // ------------------------------------------------------------------ desenho

        /// <summary>Pinta todas as camadas a partir do mapa, em blocos (usado ao carregar e no reset).</summary>
        public void RebuildAll()
        {
            if (EnsureMap() == null || palette == null)
            {
                return;
            }

            float started = Time.realtimeSinceStartup;
            BuildTint();
            int w = map.Width, h = map.Height;
            TileBase[] walls = new TileBase[w * h];
            TileBase[] fringeV = new TileBase[w * h];
            TileBase[] fringeH = new TileBase[w * h];
            TileBase[] decals = new TileBase[w * h];
            TileBase[] borders = new TileBase[w * h];
            TileBase[] caps = new TileBase[w * h];

            for (int s = 0; s < solidSections.Length; s++)
            {
                int x0 = map.OriginX + (s * sectionWidth);
                int width = Mathf.Min(sectionWidth, map.OriginX + w - x0);
                TileBase[] solids = new TileBase[width * h];
                for (int y = 0; y < h; y++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        int x = x0 + i, cy = map.OriginY + y;
                        int all = (y * w) + (x - map.OriginX);
                        walls[all] = WallTile(x, cy);
                        TerrainVisual visual = TerrainVisualRules.Resolve(map, x, cy, rules);
                        if (visual.Material == 0)
                        {
                            continue;
                        }

                        solids[(y * width) + i] = palette.Material(visual.Material)?.TileFor(visual.Shape, visual.Round);
                        fringeV[all] = FringeTile(visual.FringeVMaterial, visual.FringeV == 1 ? 0 : visual.FringeV == 2 ? 1 : 2,
                            visual.FringeV, visual.FringeVariant);
                        fringeH[all] = FringeTile(visual.FringeHMaterial, visual.FringeH == 1 ? 3 : visual.FringeH == 2 ? 4 : 5,
                            visual.FringeH, visual.FringeVariant);
                        decals[all] = visual.Decal != DecalKind.None && visual.Round == 0 ? palette.Decal(visual.Decal, visual.DecalVariant) : null;
                        borders[all] = palette.Border(visual);
                        caps[all] = palette.Cap(visual.CapType)?.TileFor(visual.Cap);
                    }
                }

                Tilemap section = solidSections[s];
                section.ClearAllTiles();
                section.SetTilesBlock(new BoundsInt(x0, map.OriginY, 0, width, h, 1), solids);
            }

            BoundsInt area = new BoundsInt(map.OriginX, map.OriginY, 0, w, h, 1);
            Fill(wallLayer, area, walls);
            Fill(fringeVLayer, area, fringeV);
            Fill(fringeHLayer, area, fringeH);
            Fill(decalLayer, area, decals);
            Fill(borderLayer, area, borders);
            Fill(capLayer, area, caps);
            if (crackLayer != null)
            {
                crackLayer.ClearAllTiles();
            }

            painted = true;
            LastRebuildMilliseconds = (Time.realtimeSinceStartup - started) * 1000f;
        }

        /// <summary>Apaga os tiles de todas as camadas (o construtor da cena salva sem eles).</summary>
        public void ClearTiles()
        {
            foreach (Tilemap section in solidSections)
            {
                section.ClearAllTiles();
            }

            foreach (Tilemap layer in new[] { wallLayer, fringeVLayer, fringeHLayer, decalLayer, borderLayer, capLayer, crackLayer })
            {
                if (layer != null)
                {
                    layer.ClearAllTiles();
                }
            }

            painted = false;
        }

        private static void Fill(Tilemap layer, BoundsInt area, TileBase[] tiles)
        {
            if (layer == null)
            {
                return;
            }

            layer.ClearAllTiles();
            layer.SetTilesBlock(area, tiles);
        }

        /// <summary>Força a colisão a refletir o mapa agora.</summary>
        public void RefreshCollision()
        {
            foreach (Tilemap section in solidSections)
            {
                TilemapCollider2D tilemapCollider = section.GetComponent<TilemapCollider2D>();
                CompositeCollider2D composite = section.GetComponent<CompositeCollider2D>();
                if (tilemapCollider != null)
                {
                    tilemapCollider.ProcessTilemapChanges();
                }

                if (composite != null)
                {
                    composite.GenerateGeometry();
                }
            }
        }

        /// <summary>
        /// Tinta por célula (textura global do shader): mais escuro e frio com
        /// a profundidade abaixo da superfície original, e com a umidade.
        /// </summary>
        private void BuildTint()
        {
            int w = map.Width, h = map.Height;
            if (tint == null || tint.width != w || tint.height != h)
            {
                if (tint != null)
                {
                    DestroyImmediate(tint);
                }

                tint = new Texture2D(w, h, TextureFormat.RGBA32, false, true)
                {
                    name = "Tinta do terreno",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.DontSave
                };
            }

            Color32[] pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int i = 0; i < w; i++)
                {
                    int x = map.OriginX + i, cy = map.OriginY + y;
                    int surface = map.SurfaceAt(x);
                    float depth = surface == int.MinValue ? 0f : Mathf.Clamp(surface - cy, 0, 120);
                    float moisture = map.MoistureAt(x, cy) / 255f;
                    float k = 1f - (depth / 120f * 0.2f) - (moisture * 0.1f);
                    float cool = depth / 120f * 0.05f;
                    pixels[(y * w) + i] = new Color32((byte)(255 * k * (1f - cool)), (byte)(255 * k), (byte)(255 * Mathf.Min(1f, k * (1f + cool))), 255);
                }
            }

            tint.SetPixels32(pixels);
            tint.Apply(false, false);
            Shader.SetGlobalTexture(TintId, tint);
            Shader.SetGlobalVector(TintRectId, new Vector4(map.OriginX, map.OriginY, w, h));
        }

        private TileBase WallTile(int x, int y)
        {
            int wall = map.WallAt(x, y);
            if (wall == 0 || map.ShapeAt(x, y) == TerrainShape.Full)
            {
                return null;
            }

            return palette.Material(wall)?.TileFor(TerrainShape.Full, 0);
        }

        private TileBase FringeTile(int material, int side, int sides, int variant)
        {
            return material == 0 || sides == 0 ? null : palette.Material(material)?.Fringe(side, variant);
        }

        private void RefreshAround(int cx, int cy)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    RefreshCell(cx + dx, cy + dy);
                }
            }
        }

        private void RefreshCell(int x, int y)
        {
            if (!map.Contains(x, y))
            {
                return;
            }

            Vector3Int cell = new Vector3Int(x, y, 0);
            TerrainVisual visual = TerrainVisualRules.Resolve(map, x, y, rules);
            Tilemap section = SectionFor(x);
            if (section != null)
            {
                TileBase tile = visual.Material == 0 ? null : palette.Material(visual.Material)?.TileFor(visual.Shape, visual.Round);
                if (section.GetTile(cell) != tile)
                {
                    section.SetTile(cell, tile);
                }

                // Bloco novo nunca herda a cor de dano do anterior.
                if (tile != null)
                {
                    section.SetColor(cell, Color.white);
                }
            }

            bool solid = visual.Material != 0;
            wallLayer.SetTile(cell, WallTile(x, y));
            fringeVLayer.SetTile(cell, solid ? FringeTile(visual.FringeVMaterial, visual.FringeV == 1 ? 0 : visual.FringeV == 2 ? 1 : 2,
                visual.FringeV, visual.FringeVariant) : null);
            fringeHLayer.SetTile(cell, solid ? FringeTile(visual.FringeHMaterial, visual.FringeH == 1 ? 3 : visual.FringeH == 2 ? 4 : 5,
                visual.FringeH, visual.FringeVariant) : null);
            decalLayer.SetTile(cell, solid && visual.Decal != DecalKind.None && visual.Round == 0 ? palette.Decal(visual.Decal, visual.DecalVariant) : null);
            borderLayer.SetTile(cell, solid ? palette.Border(visual) : null);
            capLayer.SetTile(cell, solid ? palette.Cap(visual.CapType)?.TileFor(visual.Cap) : null);
            if (!solid && crackLayer != null)
            {
                crackLayer.SetTile(cell, null);
            }
        }

        private Tilemap SectionFor(int x)
        {
            if (EnsureMap() == null || solidSections == null)
            {
                return null;
            }

            int index = (x - map.OriginX) / sectionWidth;
            return index >= 0 && index < solidSections.Length ? solidSections[index] : null;
        }
    }
}
