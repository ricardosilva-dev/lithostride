using Lithostride.Combat;
using Lithostride.Core;
using Lithostride.Creatures;
using Lithostride.Player;
using Lithostride.UI;
using Lithostride.World;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Lithostride.EditorTools
{
    /// <summary>
    /// Interface da demonstração: barra do boss com a arte do pack, vida do
    /// Kael, linha de estado/ajuda, painel de teste (F1, com rolagem) e tela
    /// de pausa. Botões ligados por listeners persistentes ao <see cref="TestPanel"/>.
    /// </summary>
    public static class PackUiBuilder
    {
        private static TMP_FontAsset font;
        private static Material outline;
        private static Sprite uiSprite;

        public static TestPanel Build(PackImportResult r, GameInput input, PlayerController controller, PlayerVitals vitals,
            Health playerHealth, PlayerToolMode toolMode, PlayerCombat combat, PlayerBuilder builder, CameraFollow follow,
            CameraZoom zoom, BackdropView backdrop, FlyingBoss boss, BossVisual bossVisual, TerrainGrid terrain,
            TrainingDummy[] dummies, DebugOverlay overlay, AnimationViewer viewer, GameObject labels, EffectRegistry registry,
            string[] destinationNames, Vector2[] destinationPoints)
        {
            font = PackPipeline.Font();
            outline = PackPipeline.OutlineMaterial();
            uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            GameObject canvasObject = new GameObject("Interface");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            RectTransform root = canvasObject.GetComponent<RectTransform>();

            BuildBossBar(r, root, boss);
            BuildPlayerHealth(root, playerHealth);

            TMP_Text status = Text(root, "Estado", "", 19, TextAlignmentOptions.BottomLeft);
            Place(status.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 12f), new Vector2(1500f, 90f));
            status.color = new Color(1f, 1f, 0.92f);

            GameObject pause = BuildPause(root, out Button resume, out Button quit);
            CanvasGroup panel = BuildPanelFrame(root, out RectTransform content);

            TestPanel test = canvasObject.AddComponent<TestPanel>();
            test.Configure(input, panel, pause, status, controller, vitals, playerHealth, toolMode, combat, builder, follow, zoom,
                backdrop, boss, bossVisual, terrain, dummies, overlay, viewer, labels, registry, destinationNames, destinationPoints);
            UnityEventTools.AddPersistentListener(resume.onClick, test.Resume);
            UnityEventTools.AddPersistentListener(quit.onClick, test.Quit);

            Section(content, "Destinos (teleporte)");
            RectTransform grid = Grid(content);
            for (int i = 0; i < destinationNames.Length; i++)
            {
                UnityEventTools.AddIntPersistentListener(MakeButton(grid, destinationNames[i]).onClick, test.Teleport, i);
            }

            Section(content, "Fundo");
            grid = Grid(content);
            UnityEventTools.AddIntPersistentListener(MakeButton(grid, "Automático").onClick, test.SetBackdrop, -1);
            for (int i = 0; i < backdrop.Count; i++)
            {
                UnityEventTools.AddIntPersistentListener(MakeButton(grid, backdrop.NameOf(i)).onClick, test.SetBackdrop, i);
            }

            Section(content, "Boss");
            grid = Grid(content);
            UnityEventTools.AddIntPersistentListener(MakeButton(grid, "Versão v2 (padrão)").onClick, test.SetBossVersion, 0);
            UnityEventTools.AddIntPersistentListener(MakeButton(grid, "Versão v1").onClick, test.SetBossVersion, 1);
            UnityEventTools.AddPersistentListener(MakeButton(grid, "Reiniciar luta").onClick, test.RestartFight);
            UnityEventTools.AddIntPersistentListener(MakeButton(grid, "Vida 63%").onClick, test.SetBossPercent, 63);
            UnityEventTools.AddIntPersistentListener(MakeButton(grid, "Vida 7%").onClick, test.SetBossPercent, 7);
            UnityEventTools.AddIntPersistentListener(MakeButton(grid, "Vida 0 (matar)").onClick, test.SetBossPercent, 0);

            Section(content, "Kael");
            grid = Grid(content);
            UnityEventTools.AddPersistentListener(MakeButton(grid, "Recuperar vida").onClick, test.HealPlayer);
            UnityEventTools.AddIntPersistentListener(MakeButton(grid, "Levar 25 de dano").onClick, test.HurtPlayer, 25);
            UnityEventTools.AddPersistentListener(MakeButton(grid, "Voo livre [N]").onClick, test.ToggleNoclip);

            Section(content, "Modo e ataques");
            grid = Grid(content);
            UnityEventTools.AddIntPersistentListener(MakeButton(grid, "1 Combate").onClick, test.SetMode, 1);
            UnityEventTools.AddIntPersistentListener(MakeButton(grid, "2 Mineração").onClick, test.SetMode, 2);
            UnityEventTools.AddIntPersistentListener(MakeButton(grid, "3 Construção").onClick, test.SetMode, 3);
            for (int i = 0; i < combat.AttackCount; i++)
            {
                UnityEventTools.AddIntPersistentListener(MakeButton(grid, combat.AttackName(i)).onClick, test.TriggerAttack, i);
            }

            UnityEventTools.AddPersistentListener(MakeButton(grid, "Limpar efeitos").onClick, test.ClearEffects);

            Section(content, "Material para colocar (modo 3)");
            grid = Grid(content);
            for (int m = 1; m <= terrain.Palette.MaterialCount; m++)
            {
                UnityEventTools.AddIntPersistentListener(MakeButton(grid, terrain.Palette.Material(m).DisplayName).onClick, test.SelectMaterial, m);
            }

            UnityEventTools.AddPersistentListener(MakeButton(grid, "Forma [R]").onClick, test.CycleShape);

            Section(content, "Área de teste");
            grid = Grid(content);
            UnityEventTools.AddPersistentListener(MakeButton(grid, "Resetar terreno").onClick, test.ResetTerrain);
            UnityEventTools.AddPersistentListener(MakeButton(grid, "Resetar alvos").onClick, test.ResetDummies);

            Section(content, "Sobreposições");
            grid = Grid(content);
            UnityEventTools.AddPersistentListener(MakeButton(grid, "Grade").onClick, test.ToggleGrid);
            UnityEventTools.AddPersistentListener(MakeButton(grid, "Pivôs").onClick, test.TogglePivots);
            UnityEventTools.AddPersistentListener(MakeButton(grid, "Colisores").onClick, test.ToggleColliders);
            UnityEventTools.AddPersistentListener(MakeButton(grid, "Rótulos").onClick, test.ToggleLabels);

            Section(content, "Visualizador de animações");
            grid = Grid(content);
            UnityEventTools.AddPersistentListener(MakeButton(grid, "Pausar / tocar").onClick, test.ViewerPause);
            UnityEventTools.AddIntPersistentListener(MakeButton(grid, "◀ Quadro").onClick, test.ViewerStep, -1);
            UnityEventTools.AddIntPersistentListener(MakeButton(grid, "Quadro ▶").onClick, test.ViewerStep, 1);
            UnityEventTools.AddPersistentListener(MakeButton(grid, "Linha de base").onClick, test.ViewerBaseline);
            UnityEventTools.AddPersistentListener(MakeButton(grid, "Limites").onClick, test.ViewerBounds);
            return test;
        }

        // ------------------------------------------------------------------ barras

        private static void BuildBossBar(PackImportResult r, RectTransform root, FlyingBoss boss)
        {
            Sprite frameSprite = r.Get("barra_moldura");
            GameObject container = new GameObject("Barra do boss", typeof(RectTransform));
            RectTransform box = container.GetComponent<RectTransform>();
            box.SetParent(root, false);
            Vector2 size = frameSprite.rect.size;
            Place(box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), size);
            CanvasGroup group = container.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            Image frame = MakeImage(box, "Moldura", frameSprite);
            Place(frame.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            frame.rectTransform.offsetMin = Vector2.zero;
            frame.rectTransform.offsetMax = Vector2.zero;

            // O canal (x, y, w, h a partir do topo da moldura) vem do manifesto.
            float[] channel = r.Meta["barra_moldura"].Anchors["canal"];
            Image fill = MakeImage(box, "Preenchimento", r.Get("barra_preenchimento"));
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 1f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 1f);
            fillRect.anchoredPosition = new Vector2(channel[0], -channel[1]);
            fillRect.sizeDelta = new Vector2(channel[2], channel[3]);

            RectTransform glowRect = null;
            if (r.Sprites.ContainsKey("barra_brilho"))
            {
                Image glow = MakeImage(box, "Brilho da ponta", r.Get("barra_brilho"));
                glowRect = glow.rectTransform;
                glowRect.anchorMin = new Vector2(0f, 1f);
                glowRect.anchorMax = new Vector2(0f, 1f);
                glowRect.pivot = new Vector2(0.5f, 1f);
                glowRect.sizeDelta = new Vector2(glow.sprite.rect.width, channel[3]);
                glowRect.anchoredPosition = new Vector2(channel[0] + channel[2], -channel[1]);
            }

            TMP_Text title = Text(box, "Nome", boss.DisplayName, 30, TextAlignmentOptions.Bottom);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 34f), new Vector2(700f, 40f));

            TMP_Text value = Text(box, "Valor", "", 22, TextAlignmentOptions.Center);
            value.rectTransform.anchorMin = new Vector2(0f, 1f);
            value.rectTransform.anchorMax = new Vector2(0f, 1f);
            value.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            value.rectTransform.anchoredPosition = new Vector2(channel[0] + (channel[2] * 0.5f), -channel[1] - (channel[3] * 0.5f));
            value.rectTransform.sizeDelta = new Vector2(channel[2], channel[3]);

            BossHealthBar bar = container.AddComponent<BossHealthBar>();
            bar.Configure(boss, group, fill, glowRect, title, value);
        }

        private static void BuildPlayerHealth(RectTransform root, Health health)
        {
            GameObject container = new GameObject("Vida do Kael", typeof(RectTransform));
            RectTransform box = container.GetComponent<RectTransform>();
            box.SetParent(root, false);
            Place(box, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(380f, 30f));
            box.pivot = new Vector2(0f, 1f);

            Image back = MakeImage(box, "Fundo", uiSprite);
            back.color = new Color(0f, 0f, 0f, 0.6f);
            back.type = UnityEngine.UI.Image.Type.Sliced;
            Stretch(back.rectTransform);

            Image fill = MakeImage(box, "Vida", uiSprite);
            fill.color = new Color(0.85f, 0.2f, 0.25f, 0.95f);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            Stretch(fill.rectTransform);
            fill.rectTransform.offsetMin = new Vector2(3f, 3f);
            fill.rectTransform.offsetMax = new Vector2(-3f, -3f);

            TMP_Text label = Text(box, "Texto", "", 20, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);

            container.AddComponent<PlayerHealthDisplay>().Configure(health, fill, label);
        }

        // ------------------------------------------------------------------ painel e pausa

        private static CanvasGroup BuildPanelFrame(RectTransform root, out RectTransform content)
        {
            GameObject panel = new GameObject("Painel de teste", typeof(RectTransform));
            RectTransform box = panel.GetComponent<RectTransform>();
            box.SetParent(root, false);
            box.anchorMin = new Vector2(1f, 0f);
            box.anchorMax = new Vector2(1f, 1f);
            box.pivot = new Vector2(1f, 0.5f);
            box.sizeDelta = new Vector2(470f, -120f);
            box.anchoredPosition = new Vector2(-10f, 0f);
            Image background = panel.AddComponent<Image>();
            background.sprite = uiSprite;
            background.type = UnityEngine.UI.Image.Type.Sliced;
            background.color = new Color(0.05f, 0.07f, 0.1f, 0.88f);
            CanvasGroup group = panel.AddComponent<CanvasGroup>();

            ScrollRect scroll = panel.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 30f;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport = new GameObject("Vista", typeof(RectTransform));
            RectTransform viewRect = viewport.GetComponent<RectTransform>();
            viewRect.SetParent(box, false);
            Stretch(viewRect);
            viewRect.offsetMin = new Vector2(8f, 8f);
            viewRect.offsetMax = new Vector2(-8f, -8f);
            viewport.AddComponent<RectMask2D>();

            GameObject list = new GameObject("Conteúdo", typeof(RectTransform));
            content = list.GetComponent<RectTransform>();
            content.SetParent(viewRect, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = list.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            list.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewRect;
            scroll.content = content;

            TMP_Text help = Text(content, "Ajuda",
                "<b>Painel de teste (F1)</b>\nA/D ou setas: andar · Shift: correr · Espaço/W: pular\n1 combate · 2 mineração · 3 construção\n" +
                "Clique esquerdo: ação do modo · Direito: projétil (combate) / tirar bloco (construção)\n" +
                "Q/E: golpe ou material · R: forma do bloco · N: voo livre\nRoda: zoom (fora do painel) · Esc: fecha o painel / pausa",
                17, TextAlignmentOptions.TopLeft);
            help.gameObject.AddComponent<LayoutElement>().preferredHeight = 150f;
            return group;
        }

        private static GameObject BuildPause(RectTransform root, out Button resume, out Button quit)
        {
            GameObject overlay = new GameObject("Pausa", typeof(RectTransform));
            RectTransform box = overlay.GetComponent<RectTransform>();
            box.SetParent(root, false);
            Stretch(box);
            Image dim = overlay.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);

            TMP_Text title = Text(box, "Título", "Pausado", 60, TextAlignmentOptions.Center);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(600f, 90f));

            resume = MakeButton(box, "Continuar (Esc)");
            Place((RectTransform)resume.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(320f, 56f));
            quit = MakeButton(box, "Sair do jogo");
            Place((RectTransform)quit.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(320f, 56f));
            return overlay;
        }

        private static void Section(RectTransform content, string title)
        {
            TMP_Text text = Text(content, title, "<b>" + title + "</b>", 20, TextAlignmentOptions.BottomLeft);
            text.color = new Color(0.75f, 0.9f, 1f);
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;
        }

        private static RectTransform Grid(RectTransform content)
        {
            GameObject grid = new GameObject("Botões", typeof(RectTransform));
            RectTransform rect = grid.GetComponent<RectTransform>();
            rect.SetParent(content, false);
            GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(144f, 34f);
            layout.spacing = new Vector2(5f, 5f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            grid.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        private static Button MakeButton(RectTransform parent, string label)
        {
            GameObject item = new GameObject("Botão " + label, typeof(RectTransform));
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Image image = item.AddComponent<Image>();
            image.sprite = uiSprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = new Color(0.2f, 0.28f, 0.38f, 1f);
            Button button = item.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.45f, 0.62f, 0.85f);
            colors.pressedColor = new Color(0.8f, 0.9f, 1f);
            button.colors = colors;

            TMP_Text text = Text(rect, "Texto", label, 16, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = 16f;
            return button;
        }

        // ------------------------------------------------------------------ utilitários

        private static Image MakeImage(RectTransform parent, string name, Sprite sprite)
        {
            GameObject item = new GameObject(name, typeof(RectTransform));
            item.GetComponent<RectTransform>().SetParent(parent, false);
            Image image = item.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = false;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text Text(RectTransform parent, string name, string text, float size, TextAlignmentOptions alignment)
        {
            GameObject item = new GameObject(name, typeof(RectTransform));
            item.GetComponent<RectTransform>().SetParent(parent, false);
            TextMeshProUGUI tmp = item.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                tmp.font = font;
            }

            if (outline != null)
            {
                tmp.fontSharedMaterial = outline;
            }

            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            tmp.color = Color.white;
            return tmp;
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2((anchorMin.x + anchorMax.x) * 0.5f, (anchorMin.y + anchorMax.y) * 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
