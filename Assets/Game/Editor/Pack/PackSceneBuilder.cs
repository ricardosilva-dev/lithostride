using System;
using System.Collections.Generic;
using System.IO;
using Lithostride.Combat;
using Lithostride.Core;
using Lithostride.Creatures;
using Lithostride.Player;
using Lithostride.UI;
using Lithostride.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Lithostride.EditorTools
{
    /// <summary>
    /// Monta a cena da demonstração do Pack completo por código, a partir do
    /// resultado da importação e do layout (<see cref="PackLevelLayout"/>).
    /// A cena é um asset serializado: gerá-la por comando evita YAML à mão e
    /// deixa o teste reconstruível. Não toca na cena antiga (Teste.unity).
    /// </summary>
    public static class PackSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Tests/PackCompletoTest.unity";

        public const int SectionWidth = 32;
        private const float Ppu = PackSpec.LogicalPixelsPerUnit;
        private const float EnvPpu = PackSpec.EnvironmentPixelsPerUnit;

        /// <summary>Agrupa as peças do jogador que o resto da cena referencia.</summary>
        private sealed class PlayerRig
        {
            public Transform Root;
            public Rigidbody2D Body;
            public PlayerController Controller;
            public PlayerAnimator Animator;
            public PlayerToolMode ToolMode;
            public PlayerCombat Combat;
            public PlayerMining Mining;
            public PlayerBuilder Builder;
            public PlayerVitals Vitals;
            public Health Health;
            public SpriteRenderer Visual;
            public SpriteRenderer Sword;
            public Collider2D BodyCollider;
            public Collider2D Hurtbox;
        }

        public static void Build(PackImportResult r)
        {
            PackLevel level = PackLevelLayout.Build(Vegetation(r, false), Vegetation(r, true), Names(r, "icone:bloco"),
                Names(r, "icone:energia"));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PackPipeline.ReloadAssets(r);

            GameObject system = new GameObject("Sistema");
            GameInput input = system.AddComponent<GameInput>();
            EventSystem eventSystem = system.AddComponent<EventSystem>();
            InputSystemUIInputModule module = system.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
            input.Configure(eventSystem);
            EffectRegistry registry = system.AddComponent<EffectRegistry>();

            TerrainGrid terrain = BuildTerrain(r, level);
            GameObject labels = new GameObject("Rótulos");
            List<PropSupport> props = BuildVegetation(r, level, terrain, labels.transform);

            Camera camera = BuildCamera();
            PlayerRig player = BuildPlayer(r, level, input, terrain, camera, registry);
            CameraFollow follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.Configure(player.Root, new Vector2(0f, 1.5f));
            CameraZoom zoom = camera.gameObject.AddComponent<CameraZoom>();
            zoom.Configure(camera, follow, input, PackSpec.LogicalPixelsPerUnit);
            player.Vitals.Configure(player.Health, player.Controller, player.Combat, player.Visual, input, follow, Point(level, "spawn"));
            camera.transform.position = new Vector3(player.Root.position.x, player.Root.position.y + 1.5f, -10f);

            BackdropView backdrop = BuildBackdrop(r, level, camera);
            TrainingDummy[] dummies = BuildDummies(r, level, labels.transform);
            FlyingBoss boss = BuildBoss(r, level, player, registry, out BossVisual bossVisual);
            AnimationViewer viewer = BuildViewer(r, level, labels.transform);

            List<Transform> pivots = new List<Transform> { player.Visual.transform, player.Sword.transform, bossVisual.transform };
            List<Collider2D> colliders = new List<Collider2D> { player.BodyCollider, player.Hurtbox };
            foreach (Collider2D c in boss.GetComponentsInChildren<Collider2D>()) colliders.Add(c);
            foreach (TrainingDummy dummy in dummies)
            {
                pivots.Add(dummy.transform);
                foreach (Collider2D c in dummy.GetComponentsInChildren<Collider2D>()) colliders.Add(c);
            }

            foreach (PropSupport prop in props)
            {
                pivots.Add(prop.transform);
            }

            List<CompositeCollider2D> composites = new List<CompositeCollider2D>(terrain.GetComponentsInChildren<CompositeCollider2D>());
            DebugOverlay overlay = camera.gameObject.AddComponent<DebugOverlay>();
            overlay.Configure(camera, pivots.ToArray(), colliders.ToArray(), composites.ToArray(), player.Combat, viewer);

            List<string> destinationNames = new List<string>();
            List<Vector2> destinationPoints = new List<Vector2>();
            List<KeyValuePair<string, float[]>> destinations = new List<KeyValuePair<string, float[]>>();
            foreach (KeyValuePair<string, float[]> point in level.Points)
            {
                if (point.Key.StartsWith("destino:", StringComparison.Ordinal)) destinations.Add(point);
            }

            destinations.Sort((a, b) => a.Value[0].CompareTo(b.Value[0]));
            foreach (KeyValuePair<string, float[]> d in destinations)
            {
                destinationNames.Add(d.Key.Substring("destino:".Length));
                destinationPoints.Add(new Vector2(d.Value[0], d.Value[1]));
            }

            TestPanel panel = PackUiBuilder.Build(r, input, player.Controller, player.Vitals, player.Health, player.ToolMode, player.Combat,
                player.Builder, follow, zoom, backdrop, boss, bossVisual, terrain, dummies, overlay, viewer, labels, registry,
                destinationNames.ToArray(), destinationPoints.ToArray());

            Health[] dummyHealth = new Health[dummies.Length];
            for (int i = 0; i < dummies.Length; i++) dummyHealth[i] = dummies[i].GetComponent<Health>();
            system.AddComponent<EvidenceRunner>().Configure(panel, player.Controller, player.Combat, player.Vitals, player.Health, boss,
                bossVisual, terrain, dummies, dummyHealth, zoom, follow, backdrop, registry, props.ToArray());

            terrain.RefreshCollision();
            int solidTiles = 0, paths = 0;
            foreach (CompositeCollider2D composite in terrain.GetComponentsInChildren<CompositeCollider2D>())
            {
                Tilemap section = composite.GetComponent<Tilemap>();
                foreach (Vector3Int cell in section.cellBounds.allPositionsWithin)
                {
                    if (section.HasTile(cell)) solidTiles++;
                }

                paths += composite.pathCount;
            }

            Debug.Log("Pack completo: terreno com " + solidTiles + " blocos sólidos e " + paths + " contornos de colisão.");
            if (solidTiles == 0 || paths == 0)
            {
                Debug.LogError("Pack completo: terreno sem tiles ou sem colisão na cena gerada.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Pack completo: cena construída em " + ScenePath + " (" + level.Vegetation.Count + " plantas, " +
                      level.Catalog.Count + " itens de catálogo, " + destinationNames.Count + " destinos).");
        }

        // ------------------------------------------------------------------ dados do layout

        private static List<PackVegetationInfo> Vegetation(PackImportResult r, bool props)
        {
            List<PackVegetationInfo> list = new List<PackVegetationInfo>();
            foreach (DerivedSheet sheet in r.Sheets)
            {
                foreach (DerivedSprite s in sheet.Sprites)
                {
                    bool isProp = s.Role == "prop";
                    bool isTree = s.Role.StartsWith("arvore", StringComparison.Ordinal) || s.Role == "toco";
                    if ((props && !isProp) || (!props && !isTree))
                    {
                        continue;
                    }

                    list.Add(new PackVegetationInfo
                    {
                        Name = s.Name,
                        Kind = s.Role,
                        Width = s.Rect.W,
                        Height = s.Rect.H,
                        SupportLeft = s.Anchors["apoio_esq"][0],
                        SupportRight = s.Anchors["apoio_dir"][0],
                        ExtentLeft = s.PivotX * s.Rect.W,
                        ExtentRight = (1f - s.PivotX) * s.Rect.W
                    });
                }
            }

            return list;
        }

        private static List<string> Names(PackImportResult r, string role)
        {
            List<string> list = new List<string>();
            foreach (DerivedSheet sheet in r.Sheets)
            {
                foreach (DerivedSprite s in sheet.Sprites)
                {
                    if (s.Role == role) list.Add(s.Name);
                }
            }

            return list;
        }

        private static Vector2 Point(PackLevel level, string name)
        {
            float[] p = level.Points[name];
            return new Vector2(p[0], p[1]);
        }

        private static Rect Zone(PackLevel level, string name)
        {
            foreach (PackZone z in level.Zones)
            {
                if (z.Name == name) return new Rect(z.X0, z.Y0, z.X1 - z.X0 + 1, z.Y1 - z.Y0 + 1);
            }

            throw new InvalidOperationException("Zona " + name + " não existe no layout.");
        }

        // ------------------------------------------------------------------ terreno

        private static TerrainGrid BuildTerrain(PackImportResult r, PackLevel level)
        {
            return BuildTerrain(r.Palette, level.Map);
        }

        /// <summary>
        /// Terreno completo: grade, parede de fundo, seções sólidas com colisão
        /// (composite por seção), franjas, detalhes, bordas, cobertura e
        /// rachaduras. Os tiles são pintados ao carregar a cena (não ficam no
        /// arquivo): aqui só se pinta para conferir e se apaga antes de salvar.
        /// </summary>
        public static TerrainGrid BuildTerrain(TerrainPalette palette, TerrainCellMap map)
        {
            GameObject root = new GameObject("Terreno");
            Grid grid = root.AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            Tilemap walls = Layer(root.transform, "Parede", -30, palette.WallMaterial);
            int sectionCount = Mathf.CeilToInt((float)map.Width / SectionWidth);
            Tilemap[] sections = new Tilemap[sectionCount];
            Transform solidRoot = new GameObject("Sólido").transform;
            solidRoot.SetParent(root.transform, false);
            for (int s = 0; s < sectionCount; s++)
            {
                Tilemap section = Layer(solidRoot, "Seção " + (map.OriginX + (s * SectionWidth)), 0, palette.SolidMaterial);
                Rigidbody2D body = section.gameObject.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Static;
                TilemapCollider2D tilemapCollider = section.gameObject.AddComponent<TilemapCollider2D>();
                tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
                CompositeCollider2D composite = section.gameObject.AddComponent<CompositeCollider2D>();
                composite.geometryType = CompositeGeometry;
                composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
                sections[s] = section;
            }

            Tilemap fringeV = Layer(root.transform, "Franjas verticais", 1, palette.SolidMaterial);
            Tilemap fringeH = Layer(root.transform, "Franjas horizontais", 2, palette.SolidMaterial);
            Tilemap decals = Layer(root.transform, "Detalhes", 3, null);
            Tilemap borders = Layer(root.transform, "Bordas", 4, null);
            Tilemap caps = Layer(root.transform, "Cobertura", 5, palette.CapMaterial);
            Tilemap cracks = Layer(root.transform, "Rachaduras", 6, null);

            TerrainGrid terrain = root.AddComponent<TerrainGrid>();
            terrain.Initialize(palette, grid, sections, SectionWidth, walls, fringeV, fringeH, decals, borders, caps, cracks, map);
            return terrain;
        }

        /// <summary>
        /// Contorno de colisão do terreno. Polygons: um corpo que entre no
        /// sólido (teleporte, bug) é empurrado para fora, em vez de cair por
        /// dentro de um contorno oco (o que acontecia com Outlines).
        /// </summary>
        public const CompositeCollider2D.GeometryType CompositeGeometry = CompositeCollider2D.GeometryType.Polygons;

        private static Tilemap Layer(Transform parent, string name, int order, Material material)
        {
            GameObject layer = new GameObject(name);
            layer.transform.SetParent(parent, false);
            Tilemap tilemap = layer.AddComponent<Tilemap>();
            TilemapRenderer renderer = layer.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            renderer.mode = TilemapRenderer.Mode.Chunk;
            renderer.detectChunkCullingBounds = TilemapRenderer.DetectChunkCullingBounds.Auto;
            return tilemap;
        }

        // ------------------------------------------------------------------ vegetação e catálogo

        private static List<PropSupport> BuildVegetation(PackImportResult r, PackLevel level, TerrainGrid terrain, Transform labels)
        {
            List<PropSupport> props = new List<PropSupport>();
            Transform natural = new GameObject("Vegetação").transform;
            foreach (PackPlacement p in level.Vegetation)
            {
                props.Add(Plant(r, p, terrain, natural, null));
            }

            Transform catalog = new GameObject("Catálogo").transform;
            foreach (PackPlacement p in level.Catalog)
            {
                if (p.Kind == "icone")
                {
                    GameObject icon = new GameObject(p.Sprite);
                    icon.transform.SetParent(catalog, false);
                    icon.transform.position = new Vector3(p.X, p.Y, 0f);
                    SpriteRenderer renderer = icon.AddComponent<SpriteRenderer>();
                    renderer.sprite = r.Get(p.Sprite);
                    renderer.sortingOrder = 3;
                    Label(labels, ShortLabel(p.Sprite), new Vector2(p.X, p.Y - 0.25f), 0.16f);
                    continue;
                }

                props.Add(Plant(r, p, terrain, catalog, labels));
            }

            foreach (PackPlacement p in level.Labels)
            {
                Label(labels, p.Label, new Vector2(p.X, p.Y), 0.5f);
            }

            return props;
        }

        private static PropSupport Plant(PackImportResult r, PackPlacement p, TerrainGrid terrain, Transform parent, Transform labels)
        {
            GameObject plant = new GameObject(p.Sprite);
            plant.transform.SetParent(parent, false);
            plant.transform.position = new Vector3(p.X, p.Y, 0f);
            SpriteRenderer renderer = plant.AddComponent<SpriteRenderer>();
            renderer.sprite = r.Get(p.Sprite);
            renderer.flipX = p.Flip;
            bool tree = p.Kind.StartsWith("arvore", StringComparison.Ordinal) || p.Kind == "toco";

            // Atrás do sólido: a parte das raízes afundada na grama fica escondida, e a cobertura passa na frente.
            renderer.sortingOrder = tree ? -6 : -5;

            Vector2Int[] cells = new Vector2Int[p.Support.Count];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = new Vector2Int(p.Support[i][0], p.Support[i][1]);
            }

            PropSupport support = plant.AddComponent<PropSupport>();
            support.Configure(terrain, renderer, cells, tree);
            if (labels != null)
            {
                Label(labels, p.Label, new Vector2(p.X, p.Y - 0.45f), 0.22f);
            }

            return support;
        }

        private static string ShortLabel(string name)
        {
            return name.Replace("blocos", "B").Replace("_l", " L").Replace("_c", "C").Replace("energia_", "E");
        }

        private static void Label(Transform parent, string text, Vector2 position, float size)
        {
            GameObject label = new GameObject("Rótulo " + text);
            label.transform.SetParent(parent, false);
            label.transform.position = new Vector3(position.x, position.y, 0f);
            TextMeshPro tmp = label.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = size * 10f;
            tmp.alignment = TextAlignmentOptions.Top;
            tmp.font = PackPipeline.Font();
            Material outline = PackPipeline.OutlineMaterial();
            if (outline != null)
            {
                tmp.fontSharedMaterial = outline;
            }

            tmp.color = new Color(1f, 1f, 0.9f, 0.95f);
            tmp.rectTransform.sizeDelta = new Vector2(Mathf.Max(4f, text.Length * size * 0.7f), size * 3f);
            tmp.rectTransform.pivot = new Vector2(0.5f, 1f);
            tmp.GetComponent<MeshRenderer>().sortingOrder = 50;
        }

        // ------------------------------------------------------------------ câmera e fundo

        private static Camera BuildCamera()
        {
            GameObject root = new GameObject("Câmera Principal");
            root.tag = "MainCamera";
            Camera camera = root.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 11.25f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.11f, 0.15f);
            camera.allowMSAA = false;
            camera.allowHDR = false;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            root.AddComponent<AudioListener>();
            return camera;
        }

        private static BackdropView BuildBackdrop(PackImportResult r, PackLevel level, Camera camera)
        {
            GameObject root = new GameObject("Fundo");
            SpriteRenderer front = Child(root.transform, "Frente", -100);
            SpriteRenderer back = Child(root.transform, "Transição", -101);
            back.enabled = false;

            string[] themes = PackSpec.BackgroundThemes;
            Sprite[] images = new Sprite[themes.Length];
            string[] names = new string[themes.Length];
            for (int i = 0; i < themes.Length; i++)
            {
                images[i] = r.Get("fundo_" + themes[i]);
                names[i] = themes[i];
            }

            BackdropView view = root.AddComponent<BackdropView>();
            view.Configure(camera, front, back, images, names, Array.IndexOf(themes, "dia"));

            List<Rect> areas = new List<Rect>();
            List<int> indices = new List<int>();
            foreach (PackZone zone in level.Zones)
            {
                if (!zone.Name.StartsWith("fundo:", StringComparison.Ordinal)) continue;
                areas.Add(new Rect(zone.X0, zone.Y0, zone.X1 - zone.X0 + 1, zone.Y1 - zone.Y0 + 1));
                indices.Add(Array.IndexOf(themes, zone.Name.Substring("fundo:".Length)));
            }

            view.SetZones(areas.ToArray(), indices.ToArray());
            front.sprite = images[Array.IndexOf(themes, "dia")];
            return view;
        }

        private static SpriteRenderer Child(Transform parent, string name, int order)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            return renderer;
        }

        // ------------------------------------------------------------------ jogador

        private static PlayerRig BuildPlayer(PackImportResult r, PackLevel level, GameInput input, TerrainGrid terrain, Camera camera,
            EffectRegistry registry)
        {
            PlayerRig rig = new PlayerRig();
            GameObject root = new GameObject("Kael");
            root.transform.position = Point(level, "spawn");
            rig.Root = root.transform;

            CapsuleCollider2D capsule = AddKaelBody(root, out rig.Body);
            rig.BodyCollider = capsule;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            rig.Visual = visual.AddComponent<SpriteRenderer>();
            rig.Visual.sortingOrder = 10;
            PixelSnap visualSnap = visual.AddComponent<PixelSnap>();
            visualSnap.Configure(camera);

            GameObject sword = new GameObject("Espada");
            sword.transform.SetParent(visual.transform, false);
            rig.Sword = sword.AddComponent<SpriteRenderer>();
            rig.Sword.sprite = r.Get("espada_cristal_a55");
            // Atrás do corpo: os dedos do quadro cobrem o cabo; a lâmina não passa sobre o rosto.
            rig.Sword.sortingOrder = rig.Visual.sortingOrder - 1;

            GameObject hurt = new GameObject("Hurtbox");
            hurt.transform.SetParent(root.transform, false);
            CapsuleCollider2D hurtCollider = hurt.AddComponent<CapsuleCollider2D>();
            hurtCollider.isTrigger = true;
            hurtCollider.size = new Vector2(WorldScale.KaelColliderWidth - 0.05f, WorldScale.KaelColliderHeight - 0.1f);
            hurtCollider.offset = new Vector2(0f, WorldScale.KaelColliderHeight * 0.5f);
            rig.Hurtbox = hurtCollider;

            rig.Health = root.AddComponent<Health>();
            rig.Health.Configure(100f, 0.9f);
            hurt.AddComponent<Hurtbox>().Configure(rig.Health, Team.Player);

            rig.Controller = root.AddComponent<PlayerController>();
            rig.Controller.Configure(rig.Body, capsule, input);
            rig.ToolMode = root.AddComponent<PlayerToolMode>();
            rig.ToolMode.Configure(input);

            rig.Animator = root.AddComponent<PlayerAnimator>();
            rig.Animator.Configure(rig.Visual, rig.Controller, visualSnap, Sequence(r, "kael_parado_"), Sequence(r, "kael_caminhada_"),
                Sequence(r, "kael_corrida_"), Sequence(r, "kael_pulo_"));
            rig.Visual.sprite = Frames(r, "kael_parado_")[0];

            SpriteRenderer miningCursor = Cursor(r, "Cursor mineração");
            SpriteRenderer buildCursor = Cursor(r, "Cursor construção");
            rig.Mining = root.AddComponent<PlayerMining>();
            rig.Mining.Configure(input, camera, terrain, rig.ToolMode, rig.Controller, miningCursor);
            rig.Builder = root.AddComponent<PlayerBuilder>();
            rig.Builder.Configure(input, camera, terrain, rig.ToolMode, rig.Controller, buildCursor);

            rig.Combat = root.AddComponent<PlayerCombat>();
            rig.Combat.Configure(input, rig.Controller, rig.Animator, rig.ToolMode, rig.Body, registry, Attacks(r),
                Frames(r, "efeito_explosao_"));

            rig.Vitals = root.AddComponent<PlayerVitals>();
            SwordVariants(r, out Sprite[] swordSprites, out int[] swordAngles);
            root.AddComponent<SwordHolder>().Configure(rig.Sword, rig.Animator, rig.Combat, rig.ToolMode, rig.Vitals, swordSprites, swordAngles);
            return rig;
        }

        /// <summary>
        /// Corpo físico do Kael (corpo rígido e cápsula). Público para que os
        /// testes de simulação usem exatamente o mesmo corpo da cena.
        /// </summary>
        public static CapsuleCollider2D AddKaelBody(GameObject root, out Rigidbody2D body)
        {
            body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 3.2f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Tronco e pernas, sem capa nem pontas do cabelo (medidas em WorldScale).
            CapsuleCollider2D capsule = root.AddComponent<CapsuleCollider2D>();
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.size = new Vector2(WorldScale.KaelColliderWidth, WorldScale.KaelColliderHeight);
            capsule.offset = new Vector2(0f, (WorldScale.KaelColliderHeight * 0.5f) + WorldScale.KaelColliderLift);
            capsule.sharedMaterial = Frictionless();
            return capsule;
        }

        /// <summary>
        /// Os cinco ataques, montados das cinco sequências do pack. Tempo dos
        /// golpes pelos quadros; hitbox em unidades a partir dos pés, virado à
        /// direita (espelhada ao virar), do tamanho do alcance visível da
        /// espada (30 px = 1,9 u a partir do punho, que fica a ~0,4 u à frente
        /// e ~0,8 u acima dos pés). O efeito com espada tem o pivô no cabo e
        /// vai na mão, sem deslocamento; os quadros de preparação do corte
        /// ficam atrás do corpo.
        /// </summary>
        private static AttackDefinition[] Attacks(PackImportResult r)
        {
            return new[]
            {
                new AttackDefinition("Corte", AttackKind.Melee, Frames(r, "efeito_corte_"), 20f, 2, 5, 0.12f, 18f,
                    new Vector2(1.35f, 1.35f), new Vector2(2.3f, 2.5f), Vector2.zero, true, true, 0f, Behind(r, "efeito_corte_")),
                new AttackDefinition("Corte energizado", AttackKind.Melee, Frames(r, "efeito_energia_"), 16f, 2, 5, 0.3f, 28f,
                    new Vector2(1.45f, 1.5f), new Vector2(2.5f, 2.8f), Vector2.zero, false, true, 0f, Behind(r, "efeito_energia_")),
                new AttackDefinition("Golpe descendente", AttackKind.Melee, Frames(r, "efeito_descendente_"), 16f, 3, 6, 0.5f, 40f,
                    new Vector2(1.5f, 0.9f), new Vector2(3.0f, 2.0f), Vector2.zero, false, true, 0f, Behind(r, "efeito_descendente_")),
                new AttackDefinition("Projétil de cristal", AttackKind.Projectile, Frames(r, "efeito_projetil_"), 14f, 1, 1, 0.35f,
                    20f, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.1f), false, false, 0f),
                new AttackDefinition("Explosão de cristais", AttackKind.Burst, Frames(r, "efeito_explosao_"), 14f, 1, 4, 0.6f, 14f,
                    new Vector2(0f, 0f), new Vector2(2.5f, 2.6f), new Vector2(2.4f, 1.3f), false, false, 0.25f)
            };
        }

        /// <summary>Quadros do efeito marcados "atras_do_corpo" no manifesto.</summary>
        private static bool[] Behind(PackImportResult r, string prefix)
        {
            Sprite[] frames = Frames(r, prefix);
            bool[] behind = new bool[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                behind[i] = r.Meta[frames[i].name].Anchors.TryGetValue("atras_do_corpo", out float[] flag) && flag[0] > 0.5f;
            }

            return behind;
        }

        /// <summary>Quadros de uma sequência do Kael com mão (px → unidades), visibilidade do punho e ângulo da lâmina.</summary>
        private static KaelSequence Sequence(PackImportResult r, string prefix)
        {
            Sprite[] frames = Frames(r, prefix);
            Vector2[] hands = new Vector2[frames.Length];
            bool[] visible = new bool[frames.Length];
            int[] angles = new int[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                DerivedSprite meta = r.Meta[frames[i].name];
                float[] hand = meta.Anchors["mao"];
                hands[i] = new Vector2(hand[0], hand[1]) / Ppu;
                visible[i] = meta.Anchors["mao_visivel"][0] > 0.5f;
                angles[i] = Mathf.RoundToInt(meta.Anchors["espada_angulo"][0]);
            }

            return new KaelSequence(frames, hands, visible, angles);
        }

        /// <summary>Variantes da espada empunhada ("espada_cristal_aNN") e seus ângulos.</summary>
        private static void SwordVariants(PackImportResult r, out Sprite[] sprites, out int[] angles)
        {
            sprites = new Sprite[PackSpec.SwordAngles.Length];
            angles = new int[PackSpec.SwordAngles.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                angles[i] = PackSpec.SwordAngles[i];
                sprites[i] = r.Get("espada_cristal_a" + angles[i].ToString("00"));
            }
        }

        private static Sprite[] Frames(PackImportResult r, string prefix)
        {
            List<string> names = new List<string>();
            foreach (string name in r.Sprites.Keys)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal)) names.Add(name);
            }

            names.Sort(StringComparer.Ordinal);
            List<Sprite> frames = new List<Sprite>();
            foreach (string name in names) frames.Add(r.Sprites[name]);
            return frames.ToArray();
        }

        private static SpriteRenderer Cursor(PackImportResult r, string name)
        {
            GameObject cursor = new GameObject(name);
            SpriteRenderer renderer = cursor.AddComponent<SpriteRenderer>();
            renderer.sprite = r.Get("borda_015");
            renderer.sortingOrder = 40;
            renderer.enabled = false;
            return renderer;
        }

        private static PhysicsMaterial2D Frictionless()
        {
            const string path = "Assets/Physics/JogadorSemAtrito.physicsMaterial2D";
            PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (material == null)
            {
                Directory.CreateDirectory("Assets/Physics");
                material = new PhysicsMaterial2D("JogadorSemAtrito");
                AssetDatabase.CreateAsset(material, path);
            }

            material.friction = 0f;
            material.bounciness = 0f;
            EditorUtility.SetDirty(material);
            return material;
        }

        // ------------------------------------------------------------------ alvos e boss

        private static TrainingDummy[] BuildDummies(PackImportResult r, PackLevel level, Transform labels)
        {
            string[] sprites = { "arvore1_l4_01", "arvore2_l3_03", "arvore3_l2_01" };
            TrainingDummy[] dummies = new TrainingDummy[3];
            Transform parent = new GameObject("Treino").transform;
            for (int i = 0; i < dummies.Length; i++)
            {
                Vector2 at = Point(level, "alvo:" + (i + 1));
                GameObject dummy = new GameObject("Alvo " + (i + 1));
                dummy.transform.SetParent(parent, false);
                dummy.transform.position = new Vector3(at.x, at.y - PackLevelLayout.Sink, 0f);
                SpriteRenderer renderer = dummy.AddComponent<SpriteRenderer>();
                renderer.sprite = r.Get(sprites[i]);
                renderer.sortingOrder = 8;
                Rigidbody2D body = dummy.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;

                Vector2 size = renderer.sprite.bounds.size;
                GameObject hurt = new GameObject("Hurtbox");
                hurt.transform.SetParent(dummy.transform, false);
                BoxCollider2D box = hurt.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(Mathf.Max(1.2f, size.x * 0.7f), Mathf.Max(2f, size.y));
                box.offset = new Vector2(0f, box.size.y * 0.5f);

                Health health = dummy.AddComponent<Health>();
                health.Configure(300f, 0f);
                hurt.AddComponent<Hurtbox>().Configure(health, Team.Enemy);

                GameObject text = new GameObject("Vida");
                text.transform.SetParent(dummy.transform, false);
                text.transform.localPosition = new Vector3(0f, Mathf.Max(2.2f, size.y + 0.9f), 0f);
                TextMeshPro tmp = text.AddComponent<TextMeshPro>();
                tmp.font = PackPipeline.Font();
                if (PackPipeline.OutlineMaterial() != null) tmp.fontSharedMaterial = PackPipeline.OutlineMaterial();
                tmp.fontSize = 3.2f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.rectTransform.sizeDelta = new Vector2(8f, 1.6f);
                tmp.GetComponent<MeshRenderer>().sortingOrder = 50;

                TrainingDummy component = dummy.AddComponent<TrainingDummy>();
                component.Configure(health, renderer, tmp);
                dummies[i] = component;
            }

            return dummies;
        }

        private static FlyingBoss BuildBoss(PackImportResult r, PackLevel level, PlayerRig player, EffectRegistry registry,
            out BossVisual visual)
        {
            Vector2 home = Point(level, "boss:casa");
            GameObject root = new GameObject("Dragão de Cristal");
            root.transform.position = home;
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            GameObject art = new GameObject("Visual");
            art.transform.SetParent(root.transform, false);
            SpriteRenderer renderer = art.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 15;
            visual = art.AddComponent<BossVisual>();
            int[] v2Starts, v1Starts;
            Sprite[] v2 = BossFrames(r, "v2", out v2Starts);
            Sprite[] v1 = BossFrames(r, "v1", out v1Starts);
            visual.Configure(renderer, v2, v2Starts, v1, v1Starts);
            renderer.sprite = v2[0];

            // Hurtbox do corpo (sem as asas) e área de contato um pouco menor.
            GameObject hurt = new GameObject("Hurtbox");
            hurt.transform.SetParent(root.transform, false);
            CapsuleCollider2D hurtCollider = hurt.AddComponent<CapsuleCollider2D>();
            hurtCollider.isTrigger = true;
            hurtCollider.direction = CapsuleDirection2D.Horizontal;
            hurtCollider.size = new Vector2(3.7f, 2.0f);
            hurtCollider.offset = new Vector2(0f, 0.17f);

            GameObject touch = new GameObject("Contato");
            touch.transform.SetParent(root.transform, false);
            BoxCollider2D touchCollider = touch.AddComponent<BoxCollider2D>();
            touchCollider.isTrigger = true;
            touchCollider.size = new Vector2(2.85f, 1.42f);
            touchCollider.offset = new Vector2(0f, 0.08f);
            ContactDamage contact = touch.AddComponent<ContactDamage>();
            contact.Configure(Team.Enemy, 10f);

            Health health = root.AddComponent<Health>();
            health.Configure(600f, 0.12f);
            hurt.AddComponent<Hurtbox>().Configure(health, Team.Enemy);

            FlyingBoss boss = root.AddComponent<FlyingBoss>();
            boss.Configure(body, health, visual, hurtCollider, contact, player.Root, player.Vitals, registry, r.Get("boss_cristal"),
                Frames(r, "efeito_explosao_"), Zone(level, "arena"), Zone(level, "boss:voo"), home, 1f);
            return boss;
        }

        /// <summary>Quadros do boss na ordem das linhas; <paramref name="starts"/> recebe o início de cada linha.</summary>
        private static Sprite[] BossFrames(PackImportResult r, string version, out int[] starts)
        {
            List<Sprite> frames = new List<Sprite>();
            starts = new int[PackSpec.BossRowNames.Length];
            for (int row = 0; row < PackSpec.BossRowNames.Length; row++)
            {
                starts[row] = frames.Count;
                frames.AddRange(Frames(r, "boss_" + version + "_" + PackSpec.BossRowNames[row] + "_"));
            }

            return frames.ToArray();
        }

        // ------------------------------------------------------------------ visualizador

        private static AnimationViewer BuildViewer(PackImportResult r, PackLevel level, Transform labels)
        {
            Vector2 origin = Point(level, "visualizador:origem");
            GameObject root = new GameObject("Visualizador");
            AnimationViewer viewer = root.AddComponent<AnimationViewer>();
            List<SpriteRenderer> renderers = new List<SpriteRenderer>();
            List<Sprite[]> sequences = new List<Sprite[]>();
            List<float> rates = new List<float>();

            void Station(string name, Sprite[] frames, float fps, Vector2 at)
            {
                GameObject station = new GameObject(name);
                station.transform.SetParent(root.transform, false);
                station.transform.position = at;
                SpriteRenderer renderer = station.AddComponent<SpriteRenderer>();
                renderer.sprite = frames[0];
                renderer.sortingOrder = 12;
                renderers.Add(renderer);
                sequences.Add(frames);
                rates.Add(fps);
                Label(labels, name + " (" + frames.Length + ")", at + new Vector2(0f, -0.35f), 0.35f);
            }

            float x = origin.x;
            Station("Kael parado", Frames(r, "kael_parado_"), 7f, new Vector2(x, origin.y));
            Station("Kael caminhada", Frames(r, "kael_caminhada_"), 11f, new Vector2(x + 5f, origin.y));
            Station("Kael corrida", Frames(r, "kael_corrida_"), 15f, new Vector2(x + 10f, origin.y));
            Station("Kael pulo (8 poses)", Frames(r, "kael_pulo_"), 3f, new Vector2(x + 15f, origin.y));
            Station("Kael MASTER", Frames(r, "kael_master"), 1f, new Vector2(x + 20f, origin.y));

            string[] effects = PackSpec.EffectNames;
            for (int i = 0; i < effects.Length; i++)
            {
                Station("Efeito " + effects[i], Frames(r, "efeito_" + effects[i] + "_"), 10f, new Vector2(x + 2f + (i * 6f), origin.y + 7f));
            }

            Station("Espada", new[] { r.Get("espada_cristal") }, 1f, new Vector2(x + 32f, origin.y + 7f));

            for (int row = 0; row < PackSpec.BossRowNames.Length; row++)
            {
                string rowName = PackSpec.BossRowNames[row];
                Station("Boss v2 " + rowName, Frames(r, "boss_v2_" + rowName + "_"), 8f, new Vector2(x + 4f + (row * 11f), origin.y + 16f));
                Station("Boss v1 " + rowName, Frames(r, "boss_v1_" + rowName + "_"), 8f, new Vector2(x + 4f + (row * 11f), origin.y + 26f));
            }

            Station("Sopro v1", new[] { r.Get("boss_v1_sopro") }, 1f, new Vector2(x + 58f, origin.y + 26f));
            Station("Cristal do boss", new[] { r.Get("boss_cristal") }, 1f, new Vector2(x + 58f, origin.y + 18f));

            viewer.SetStations(renderers.ToArray(), sequences.ToArray(), rates.ToArray());
            return viewer;
        }
    }
}
