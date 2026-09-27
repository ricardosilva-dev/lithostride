using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Lithostride.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Lithostride.EditorTools
{
    /// <summary>Resultado da preparação: sprites por nome, folhas e paleta do terreno.</summary>
    public sealed class PackImportResult
    {
        public readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        public readonly Dictionary<string, DerivedSprite> Meta = new Dictionary<string, DerivedSprite>();
        public List<DerivedSheet> Sheets;
        public TerrainPalette Palette;
        public readonly List<string> Notes = new List<string>();

        public Sprite Get(string name)
        {
            if (Sprites.TryGetValue(name, out Sprite sprite))
            {
                return sprite;
            }

            throw new InvalidOperationException("Pack completo: sprite " + name + " não encontrado após a importação.");
        }
    }

    /// <summary>
    /// Comando único e repetível: lê "Pack completo" (só leitura), gera os
    /// derivados em Assets/Art/Pack, configura a importação (pixel art: Point,
    /// sem compressão, sem mipmap; fundos e interface: bilinear), fatia com IDs
    /// estáveis, monta a paleta do terreno, escreve o manifesto e reconstrói a
    /// cena de teste. Rodar de novo não duplica nada: PNG igual não é
    /// reescrito, sprite de mesmo nome mantém o ID, tile de mesmo nome é
    /// atualizado no lugar.
    /// </summary>
    public static class PackPipeline
    {
        public const string ArtRoot = "Assets/Art/Pack";
        public const string ManifestPath = ArtRoot + "/pack_manifest.json";
        public const string PalettePath = ArtRoot + "/Terreno/TerrenoPaleta.asset";

        [MenuItem("Lithostride/Pack completo/Preparar e construir teste", false, 0)]
        public static void PrepareAndBuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            PrepareAndBuild();
        }

        [MenuItem("Lithostride/Pack completo/Só preparar a arte", false, 1)]
        public static void PrepareFromMenu()
        {
            Prepare();
        }

        /// <summary>Entrada para -executeMethod (modo batch): sai com código 1 se a preparação falhar.</summary>
        public static void BatchPrepareAndBuild()
        {
            if (!PrepareAndBuild() && Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Prepara a arte e reconstrói a cena. Falso (sem tocar na cena) se faltar arquivo.</summary>
        public static bool PrepareAndBuild()
        {
            PackImportResult result = Prepare();
            if (result == null)
            {
                return false;
            }

            PackSceneBuilder.Build(result);
            return true;
        }

        public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        /// <summary>
        /// Arquivo de origem: caminhos do "Pack completo" são relativos à pasta
        /// dele; os lotes novos já vêm com "Arte pendente/..." (relativos ao projeto).
        /// </summary>
        public static string SourcePath(string relative)
        {
            return relative.StartsWith(HarmoniaSpec.PendingFolder, StringComparison.Ordinal)
                ? Path.Combine(ProjectRoot, relative)
                : Path.Combine(ProjectRoot, PackSpec.PackFolder, relative);
        }

        public static PackImportResult Prepare()
        {
            List<string> missing = new List<string>();
            foreach (string source in PackBuilder.RequiredSources)
            {
                if (!File.Exists(SourcePath(source)))
                {
                    missing.Add(source);
                }
            }

            if (missing.Count > 0)
            {
                Debug.LogError("Pack completo: arquivos faltando em '" + PackSpec.PackFolder + "': " + string.Join(", ", missing) +
                               ". Nada foi alterado.");
                return null;
            }

            Debug.Log("Pack completo: pipeline de render ativo = " +
                      (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null
                          ? "Built-in"
                          : UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name));

            if (!EnsureTextMeshPro())
            {
                return null;
            }


            Dictionary<string, PixelImage> cache = new Dictionary<string, PixelImage>();
            PackImportResult result = new PackImportResult();
            result.Sheets = PackBuilder.Build(relative =>
            {
                if (!cache.TryGetValue(relative, out PixelImage image))
                {
                    image = LoadPng(SourcePath(relative));
                    cache[relative] = image;
                }

                return image.Clone();
            }, result.Notes);

            List<DerivedSheet> changed = new List<DerivedSheet>();
            foreach (DerivedSheet sheet in result.Sheets)
            {
                if (WritePng(sheet))
                {
                    changed.Add(sheet);
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            int reimported = 0;
            foreach (DerivedSheet sheet in result.Sheets)
            {
                if (ConfigureImporter(sheet, changed.Contains(sheet)))
                {
                    reimported++;
                }
            }

            RemoveObsolete(result);
            ReloadSprites(result);
            foreach (DerivedSheet sheet in result.Sheets)
            {
                foreach (DerivedSprite meta in sheet.Sprites)
                {
                    result.Meta[meta.Name] = meta;
                }
            }

            result.Palette = BuildPalette(result);
            WriteManifest(result);
            AssetDatabase.SaveAssets();
            Debug.Log("Pack completo: " + result.Sheets.Count + " folhas (" + changed.Count + " reescritas, " + reimported +
                      " reimportadas), " + result.Sprites.Count + " sprites.");
            return result;
        }

        /// <summary>
        /// Recarrega sprites e paleta do disco. Necessário depois de criar uma
        /// cena nova: a troca de cena descarrega assets referenciados só por
        /// código, e as referências antigas passam a valer nulo para a Unity.
        /// </summary>
        public static void ReloadAssets(PackImportResult result)
        {
            ReloadSprites(result);
            result.Palette = AssetDatabase.LoadAssetAtPath<TerrainPalette>(PalettePath);
        }

        private static void ReloadSprites(PackImportResult result)
        {
            result.Sprites.Clear();
            foreach (DerivedSheet sheet in result.Sheets)
            {
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(sheet.AssetPath))
                {
                    if (asset is Sprite sprite)
                    {
                        result.Sprites[sprite.name] = sprite;
                    }
                }
            }
        }

        // ------------------------------------------------------------------ PNG

        private static PixelImage LoadPng(string path)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            Color32[] pixels = texture.GetPixels32();
            PixelImage image = new PixelImage(texture.width, texture.height);
            for (int y = 0; y < texture.height; y++)
            {
                int row = (texture.height - 1 - y) * texture.width;
                for (int x = 0; x < texture.width; x++)
                {
                    Color32 c = pixels[row + x];
                    image[x, y] = new Rgba(c.r, c.g, c.b, c.a);
                }
            }

            Object.DestroyImmediate(texture);
            return image;
        }

        /// <summary>Grava o PNG só se o conteúdo mudou (reimportação e .meta intactos quando igual).</summary>
        private static bool WritePng(DerivedSheet sheet)
        {
            PixelImage image = sheet.Image;
            Texture2D texture = new Texture2D(image.Width, image.Height, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[image.Width * image.Height];
            for (int y = 0; y < image.Height; y++)
            {
                int row = (image.Height - 1 - y) * image.Width;
                for (int x = 0; x < image.Width; x++)
                {
                    Rgba c = image[x, y];
                    pixels[row + x] = new Color32(c.R, c.G, c.B, c.A);
                }
            }

            texture.SetPixels32(pixels);
            byte[] bytes = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            string path = Path.Combine(ProjectRoot, sheet.AssetPath);
            if (File.Exists(path) && Equal(File.ReadAllBytes(path), bytes))
            {
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, bytes);
            return true;
        }

        private static bool Equal(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        // ------------------------------------------------------------------ importação

        /// <summary>
        /// Configura a textura e os recortes. A assinatura (configuração +
        /// retângulos + pivôs + formas) fica no userData do importador: igual e
        /// PNG igual, não reimporta.
        /// </summary>
        private static bool ConfigureImporter(DerivedSheet sheet, bool pngChanged)
        {
            TextureImporter importer = AssetImporter.GetAtPath(sheet.AssetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("Pack completo: importador não encontrado para " + sheet.AssetPath);
                return false;
            }

            string signature = Signature(sheet);
            if (!pngChanged && importer.userData == signature)
            {
                return false;
            }

            if (sheet.TextureOnly)
            {
                // Textura de dados do terreno: array de fatias ou 2D, sem recortes.
                importer.textureType = TextureImporterType.Default;
                importer.textureShape = sheet.ArrayColumns > 0 ? TextureImporterShape.Texture2DArray : TextureImporterShape.Texture2D;
                importer.sRGBTexture = !sheet.Linear;
                importer.filterMode = sheet.PointFilter ? FilterMode.Point : FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.wrapMode = sheet.Repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.alphaIsTransparency = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 8192;
                importer.isReadable = false;
                if (sheet.ArrayColumns > 0)
                {
                    TextureImporterSettings arraySettings = new TextureImporterSettings();
                    importer.ReadTextureSettings(arraySettings);
                    arraySettings.flipbookColumns = sheet.ArrayColumns;
                    arraySettings.flipbookRows = sheet.ArrayRows;
                    importer.SetTextureSettings(arraySettings);
                }

                importer.userData = signature;
                importer.SaveAndReimport();
                return true;
            }

            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = !sheet.Linear;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = sheet.Single ? SpriteImportMode.Single : SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = sheet.PixelsPerUnit;
            importer.filterMode = sheet.PointFilter ? FilterMode.Point : FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = sheet.Mipmaps;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 8192;
            importer.isReadable = false;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            if (sheet.Single)
            {
                DerivedSprite single = sheet.Sprites[0];
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(single.PivotX, single.PivotY);
            }

            importer.SetTextureSettings(settings);
            importer.userData = signature;

            if (!sheet.Single)
            {
                ApplySpriteRects(importer, sheet);
            }

            importer.SaveAndReimport();
            return true;
        }

        private static void ApplySpriteRects(TextureImporter importer, DerivedSheet sheet)
        {
            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            // IDs estáveis: o que já existe com o mesmo nome continua com o mesmo ID.
            Dictionary<string, GUID> existing = new Dictionary<string, GUID>();
            foreach (SpriteRect rect in provider.GetSpriteRects())
            {
                existing[rect.name] = rect.spriteID;
            }

            int height = sheet.Image.Height;
            List<SpriteRect> rects = new List<SpriteRect>();
            List<SpriteNameFileIdPair> ids = new List<SpriteNameFileIdPair>();
            foreach (DerivedSprite sprite in sheet.Sprites)
            {
                GUID id = existing.TryGetValue(sprite.Name, out GUID old) ? old : StableGuid(sheet.AssetPath + "#" + sprite.Name);
                RectI r = sprite.Rect;
                rects.Add(new SpriteRect
                {
                    name = sprite.Name,
                    rect = new Rect(r.X, height - r.Y - r.H, r.W, r.H),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(sprite.PivotX, sprite.PivotY),
                    border = Vector4.zero,
                    spriteID = id
                });
                ids.Add(new SpriteNameFileIdPair(sprite.Name, id));
            }

            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(ids);

            ISpritePhysicsOutlineDataProvider physics = provider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
            for (int i = 0; i < sheet.Sprites.Count; i++)
            {
                List<Vector2[]> outlines = new List<Vector2[]>();
                if (sheet.Sprites[i].PhysicsShape != null)
                {
                    foreach (float[] polygon in sheet.Sprites[i].PhysicsShape)
                    {
                        Vector2[] points = new Vector2[polygon.Length / 2];
                        for (int p = 0; p < points.Length; p++)
                        {
                            points[p] = new Vector2(polygon[p * 2], polygon[(p * 2) + 1]);
                        }

                        outlines.Add(points);
                    }
                }

                physics.SetOutlines(rects[i].spriteID, outlines);
            }

            provider.Apply();
        }

        /// <summary>GUID determinístico a partir de um texto (MD5): o mesmo recorte ganha o mesmo ID em qualquer máquina.</summary>
        private static GUID StableGuid(string key)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
                StringBuilder sb = new StringBuilder(32);
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }

                return new GUID(sb.ToString());
            }
        }

        private static string Signature(DerivedSheet sheet)
        {
            StringBuilder sb = new StringBuilder("v4|");
            sb.Append(sheet.PixelsPerUnit).Append('|').Append(sheet.PointFilter).Append('|').Append(sheet.Mipmaps).Append('|')
                .Append(sheet.Single).Append('|').Append(sheet.TextureOnly).Append(sheet.Linear).Append(sheet.Repeat)
                .Append(sheet.ArrayColumns).Append('x').Append(sheet.ArrayRows).Append('|');
            foreach (DerivedSprite s in sheet.Sprites)
            {
                sb.Append(s.Name).Append(':').Append(s.Rect.ToString()).Append(':')
                    .Append(s.PivotX.ToString("0.#####")).Append(',').Append(s.PivotY.ToString("0.#####"));
                if (s.PhysicsShape != null)
                {
                    foreach (float[] polygon in s.PhysicsShape)
                    {
                        sb.Append('[').Append(string.Join(",", Array.ConvertAll(polygon, v => v.ToString("0.##")))).Append(']');
                    }
                }

                sb.Append(';');
            }

            using (MD5 md5 = MD5.Create())
            {
                return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()))).Replace("-", "");
            }
        }

        // ------------------------------------------------------------------ paleta do terreno

        public const string TerrainFolder = ArtRoot + "/Terreno/";
        public const string TerrainShaderName = "Lithostride/TerrenoContinuo";

        /// <summary>
        /// Paleta do terreno: tiles-máscara por material (16 quinas, rampas,
        /// meio-bloco), franjas, coberturas, bordas, rachaduras e detalhes
        /// como sub-assets (atualizados no lugar; os que não existem mais são
        /// removidos), e os três materiais do shader do terreno contínuo.
        /// </summary>
        private static TerrainPalette BuildPalette(PackImportResult result)
        {
            TerrainPalette palette = AssetDatabase.LoadAssetAtPath<TerrainPalette>(PalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<TerrainPalette>();
                AssetDatabase.CreateAsset(palette, PalettePath);
            }

            Dictionary<string, TerrainSpriteTile> tiles = new Dictionary<string, TerrainSpriteTile>();
            List<Object> stale = new List<Object>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(PalettePath))
            {
                if (asset is TerrainSpriteTile tile)
                {
                    tiles[tile.name] = tile;
                }
                else if (asset != null && asset != palette)
                {
                    stale.Add(asset);
                }
            }

            HashSet<string> used = new HashSet<string>();
            Tile.ColliderType none = Tile.ColliderType.None;

            TerrainSpriteTile Sub(string name, Tile.ColliderType collider)
            {
                used.Add(name);
                return SubTile(palette, tiles, name, result.Get(name), collider);
            }

            TerrainMaterialDef[] materials = new TerrainMaterialDef[TerrainArt.Materials.Length];
            for (int m = 0; m < TerrainArt.Materials.Length; m++)
            {
                TerrainMaterialInfo info = TerrainArt.Materials[m];
                TileBase[] full = new TileBase[16];
                for (int round = 0; round < 16; round++)
                {
                    full[round] = Sub(info.Id + "_cheio_" + round.ToString("00"), Tile.ColliderType.Grid);
                }

                TileBase[] fringes = new TileBase[0];
                if (info.Priority > 0)
                {
                    fringes = new TileBase[TerrainArt.FringeSides.Length * TerrainArt.FringeVariants];
                    for (int s = 0; s < TerrainArt.FringeSides.Length; s++)
                    {
                        for (int v = 0; v < TerrainArt.FringeVariants; v++)
                        {
                            fringes[(s * TerrainArt.FringeVariants) + v] = Sub(info.Id + "_franja_" + TerrainArt.FringeSides[s] + "_" + v, none);
                        }
                    }
                }

                int cap = info.Cap == "grama" ? TerrainVisualRules.Grass : info.Cap == "musgo" ? TerrainVisualRules.Moss : 0;
                Sprite drop = info.DropIcon >= 0 ? result.Get("energia_" + (info.DropIcon + 1).ToString("00")) : null;
                bool earthy = info.Id == "terra" || info.Id == "terra_escura";
                bool rocky = info.Id == "pedra" || info.Id == "pedra_musgosa" || info.Id == "pedra_profunda";
                materials[m] = new TerrainMaterialDef(info.Id, info.DisplayName, cap, info.Priority, earthy, rocky, info.BreakTime, drop, full,
                    Sub(info.Id + "_rampa_dir", Tile.ColliderType.Sprite), Sub(info.Id + "_rampa_esq", Tile.ColliderType.Sprite),
                    Sub(info.Id + "_meio", Tile.ColliderType.Sprite), fringes);
            }

            TerrainCapDef[] caps = new TerrainCapDef[TerrainArt.CapIds.Length];
            for (int c = 0; c < caps.Length; c++)
            {
                TileBase[] pieces = new TileBase[TerrainArt.CapPieces.Length];
                for (int p = 0; p < pieces.Length; p++)
                {
                    pieces[p] = Sub(TerrainArt.CapIds[c] + "_" + TerrainArt.CapPieces[p], none);
                }

                caps[c] = new TerrainCapDef(TerrainArt.CapIds[c], pieces);
            }

            TileBase[] borders = new TileBase[TerrainRules.BorderMaskCount];
            for (int mask = 1; mask < borders.Length; mask++)
            {
                borders[mask] = Sub("borda_" + mask.ToString("000"), none);
            }

            TileBase[] slopeRight = new TileBase[4], slopeLeft = new TileBase[4], half = new TileBase[8];
            for (int s = 0; s < 4; s++)
            {
                slopeRight[s] = Sub("borda_rampa_dir_" + s, none);
                slopeLeft[s] = Sub("borda_rampa_esq_" + s, none);
            }

            for (int s = 0; s < 8; s++)
            {
                half[s] = Sub("borda_meio_" + s, none);
            }

            TileBase[] cracks = new TileBase[3];
            for (int s = 0; s < 3; s++)
            {
                cracks[s] = Sub("rachadura_" + (s + 1), none);
            }

            TerrainDecalSet[] decals = new TerrainDecalSet[TerrainArt.DecalKinds.Length];
            decals[0] = new TerrainDecalSet(new TileBase[0]);
            for (int k = 1; k < decals.Length; k++)
            {
                List<TileBase> set = new List<TileBase>();
                for (int v = 0; result.Sprites.ContainsKey("detalhe_" + TerrainArt.DecalKinds[k] + "_" + v.ToString("00")); v++)
                {
                    set.Add(Sub("detalhe_" + TerrainArt.DecalKinds[k] + "_" + v.ToString("00"), none));
                }

                decals[k] = new TerrainDecalSet(set.ToArray());
            }

            foreach (KeyValuePair<string, TerrainSpriteTile> kv in tiles)
            {
                if (!used.Contains(kv.Key))
                {
                    stale.Add(kv.Value);
                }
            }

            // Sub-assets de versões anteriores (tiles de período 4x4, cipós...) saem da paleta.
            foreach (Object old in stale)
            {
                AssetDatabase.RemoveObjectFromAsset(old);
                Object.DestroyImmediate(old, true);
            }

            if (stale.Count > 0)
            {
                result.Notes.Add("Paleta do terreno: " + stale.Count + " sub-assets obsoletos removidos");
            }

            Texture layers = AssetDatabase.LoadAssetAtPath<Texture2DArray>(TerrainFolder + "terreno_camadas.png");
            Texture capStrips = AssetDatabase.LoadAssetAtPath<Texture2DArray>(TerrainFolder + "terreno_coberturas.png");
            Texture regions = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainFolder + "terreno_regioes.png");
            if (layers == null || capStrips == null || regions == null)
            {
                throw new InvalidOperationException("Pack completo: texturas do terreno não importadas como array (camadas " + (layers != null) +
                                                    ", coberturas " + (capStrips != null) + ", regiões " + (regions != null) + ").");
            }

            Material solid = TerrainMaterial("TerrenoSolido", 0f, Color.white, 1f, layers, regions);
            Material wall = TerrainMaterial("TerrenoParede", 0f, new Color(0.40f, 0.38f, 0.47f, 1f), 1f, layers, regions);
            Material capMaterial = TerrainMaterial("TerrenoCobertura", 1f, Color.white, 1f, capStrips, regions);

            palette.Assign(materials, caps, borders, slopeRight, slopeLeft, half, cracks, decals, solid, wall, capMaterial);
            EditorUtility.SetDirty(palette);
            AssetDatabase.SaveAssets();

            // Sem reimportar: reimportar recria os sub-assets e deixaria referências a objetos destruídos.
            return AssetDatabase.LoadAssetAtPath<TerrainPalette>(PalettePath);
        }

        /// <summary>Material do shader do terreno contínuo, criado ou atualizado no lugar (mesmo GUID).</summary>
        private static Material TerrainMaterial(string name, float mode, Color darken, float useTint, Texture layers, Texture regions)
        {
            Shader shader = Shader.Find(TerrainShaderName);
            if (shader == null)
            {
                throw new InvalidOperationException("Pack completo: shader " + TerrainShaderName + " não encontrado.");
            }

            string path = TerrainFolder + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetTexture("_Layers", layers);
            material.SetTexture("_Regions", regions);
            material.SetFloat("_Mode", mode);
            material.SetFloat("_LayersPerMaterial", mode < 0.5f ? TerrainArt.LayersPerMaterial : TerrainArt.CapLayersPerType);
            material.SetFloat("_LayerSize", TerrainArt.LayerSize);
            material.SetFloat("_StripRows", TerrainArt.CapRows);
            material.SetColor("_Darken", darken);
            material.SetFloat("_UseTint", useTint);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Tile como sub-asset da paleta: atualizado no lugar se já existe (referências preservadas).</summary>
        private static TerrainSpriteTile SubTile(TerrainPalette palette, Dictionary<string, TerrainSpriteTile> tiles, string name, Sprite sprite,
            Tile.ColliderType collider)
        {
            if (!tiles.TryGetValue(name, out TerrainSpriteTile tile))
            {
                tile = ScriptableObject.CreateInstance<TerrainSpriteTile>();
                tile.name = name;
                AssetDatabase.AddObjectToAsset(tile, palette);
                tiles[name] = tile;
            }

            tile.Configure(sprite, collider);
            EditorUtility.SetDirty(tile);
            return tile;
        }

        // ------------------------------------------------------------------ derivados obsoletos

        /// <summary>
        /// Apaga PNGs em Assets/Art/Pack que o pipeline não produz mais (versões
        /// anteriores dos derivados). Só derivados: as fontes ficam fora de Assets.
        /// </summary>
        private static void RemoveObsolete(PackImportResult result)
        {
            HashSet<string> current = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DerivedSheet sheet in result.Sheets)
            {
                current.Add(sheet.AssetPath.Replace('\\', '/'));
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Texture", new[] { ArtRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && !current.Contains(path))
                {
                    AssetDatabase.DeleteAsset(path);
                    result.Notes.Add("Derivado obsoleto removido: " + path);
                }
            }
        }

        // ------------------------------------------------------------------ manifesto

        private static void WriteManifest(PackImportResult result)
        {
            List<PackSourceInfo> sources = new List<PackSourceInfo>();
            // Pack completo (caminhos relativos à pasta dele) e os lotes de "Arte pendente" (relativos ao projeto).
            List<string[]> files = new List<string[]>();
            string packRoot = Path.Combine(ProjectRoot, PackSpec.PackFolder);
            foreach (string file in Directory.GetFiles(packRoot, "*.png", SearchOption.AllDirectories))
            {
                files.Add(new[] { file, file.Substring(packRoot.Length + 1).Replace(Path.DirectorySeparatorChar, '/') });
            }

            foreach (string batch in HarmoniaSpec.Batches)
            {
                string folder = Path.Combine(ProjectRoot, batch);
                if (!Directory.Exists(folder)) continue;
                foreach (string file in Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly))
                {
                    files.Add(new[] { file, batch + Path.GetFileName(file) });
                }
            }

            files.Sort((a, b) => string.CompareOrdinal(a[1], b[1]));
            foreach (string[] entry in files)
            {
                string file = entry[0];
                byte[] bytes = File.ReadAllBytes(file);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(bytes);
                string hash;
                using (SHA256 sha = SHA256.Create())
                {
                    hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
                }

                sources.Add(new PackSourceInfo
                {
                    Path = entry[1],
                    Sha256 = hash,
                    Width = texture.width,
                    Height = texture.height
                });
                Object.DestroyImmediate(texture);
            }

            string json = PackBuilder.Manifest(result.Sheets, sources, result.Notes);
            string path = Path.Combine(ProjectRoot, ManifestPath);
            if (!File.Exists(path) || File.ReadAllText(path) != json)
            {
                File.WriteAllText(path, json);
                AssetDatabase.ImportAsset(ManifestPath);
            }
        }

        // ------------------------------------------------------------------ TextMeshPro

        /// <summary>Importa os recursos essenciais do TextMeshPro (fonte padrão) se o projeto ainda não os tem.</summary>
        private static bool EnsureTextMeshPro()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) != null)
            {
                return true;
            }

            UnityEditor.PackageManager.PackageInfo ugui = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui/package.json");
            string package = ugui == null ? "" : Path.Combine(ugui.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            if (!File.Exists(package))
            {
                Debug.LogError("Pack completo: pacote de recursos do TextMeshPro não encontrado em " + package);
                return false;
            }

            // A API nova (AssetPackage.Package.Import) é assíncrona e não termina
            // antes do fim do modo batch; a antiga importa na hora.
#pragma warning disable CS0618
            AssetDatabase.ImportPackage(package, false);
#pragma warning restore CS0618
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            bool ok = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) != null;
            if (!ok)
            {
                // Em modo batch a importação por código pode ficar para depois; a linha de comando é síncrona.
                Debug.LogError("Pack completo: recursos do TextMeshPro ausentes. Importe uma vez (Window > TextMeshPro > Import TMP " +
                               "Essential Resources) ou rode a Unity com -importPackage \"" + package + "\". Nada foi alterado na cena.");
            }

            return ok;
        }

        public const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        public const string OutlineMaterialPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Outline.mat";

        public static TMP_FontAsset Font()
        {
            TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            return asset != null ? asset : TMP_Settings.defaultFontAsset;
        }

        /// <summary>Material de contorno da fonte, compartilhado por todos os rótulos (sem material por texto).</summary>
        public static Material OutlineMaterial()
        {
            return AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
        }
    }
}
