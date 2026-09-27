using System.Text;
using Lithostride.Combat;
using Lithostride.Core;
using Lithostride.Creatures;
using Lithostride.Player;
using Lithostride.World;
using TMPro;
using UnityEngine;

namespace Lithostride.UI
{
    /// <summary>
    /// Painel da demonstração (F1): destinos, fundo, versão do boss, luta,
    /// vida, terreno, alvos, sobreposições, voo livre, variantes de ataque e o
    /// visualizador de animações. Esc fecha o painel; sem painel aberto,
    /// pausa (e só então oferece "Sair"). São ferramentas de teste, não
    /// interface de jogo.
    /// </summary>
    public sealed class TestPanel : MonoBehaviour
    {
        [SerializeField] private GameInput input;
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private GameObject pauseOverlay;
        [SerializeField] private TMP_Text status;
        [SerializeField] private PlayerController controller;
        [SerializeField] private PlayerVitals vitals;
        [SerializeField] private Health playerHealth;
        [SerializeField] private PlayerToolMode toolMode;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerBuilder builder;
        [SerializeField] private CameraFollow follow;
        [SerializeField] private CameraZoom zoom;
        [SerializeField] private BackdropView backdrop;
        [SerializeField] private FlyingBoss boss;
        [SerializeField] private BossVisual bossVisual;
        [SerializeField] private TerrainGrid terrain;
        [SerializeField] private TrainingDummy[] dummies;
        [SerializeField] private DebugOverlay overlay;
        [SerializeField] private AnimationViewer viewer;
        [SerializeField] private GameObject labels;
        [SerializeField] private EffectRegistry registry;
        [SerializeField] private string[] destinationNames;
        [SerializeField] private Vector2[] destinationPoints;

        private float statusTimer;
        private bool paused;

        public int DestinationCount => destinationNames == null ? 0 : destinationNames.Length;

        public string DestinationName(int index)
        {
            return destinationNames[index];
        }

        public void Configure(GameInput gameInput, CanvasGroup panelGroup, GameObject pause, TMP_Text statusText,
            PlayerController playerController, PlayerVitals playerVitals, Health health, PlayerToolMode mode, PlayerCombat playerCombat,
            PlayerBuilder playerBuilder, CameraFollow cameraFollow, CameraZoom cameraZoom, BackdropView backdropView,
            FlyingBoss flyingBoss, BossVisual flyingBossVisual, TerrainGrid terrainGrid, TrainingDummy[] trainingDummies,
            DebugOverlay debugOverlay, AnimationViewer animationViewer, GameObject labelRoot, EffectRegistry effects, string[] names,
            Vector2[] points)
        {
            input = gameInput;
            panel = panelGroup;
            pauseOverlay = pause;
            status = statusText;
            controller = playerController;
            vitals = playerVitals;
            playerHealth = health;
            toolMode = mode;
            combat = playerCombat;
            builder = playerBuilder;
            follow = cameraFollow;
            zoom = cameraZoom;
            backdrop = backdropView;
            boss = flyingBoss;
            bossVisual = flyingBossVisual;
            terrain = terrainGrid;
            dummies = trainingDummies;
            overlay = debugOverlay;
            viewer = animationViewer;
            labels = labelRoot;
            registry = effects;
            destinationNames = names;
            destinationPoints = points;
        }

        private void Start()
        {
            SetPanel(false);
            pauseOverlay.SetActive(false);
        }

        private void Update()
        {
            if (input == null)
            {
                return;
            }

            if (input.PanelTogglePressed && !paused)
            {
                SetPanel(panel.alpha < 0.5f);
            }

            if (input.EscapePressed)
            {
                if (panel.alpha > 0.5f)
                {
                    SetPanel(false);
                }
                else
                {
                    SetPause(!paused);
                }
            }

            statusTimer -= Time.unscaledDeltaTime;
            if (statusTimer <= 0f && status != null)
            {
                statusTimer = 0.2f;
                status.text = StatusText();
            }
        }

        private void SetPanel(bool open)
        {
            panel.alpha = open ? 1f : 0f;
            panel.interactable = open;
            panel.blocksRaycasts = open;
        }

        private void SetPause(bool on)
        {
            paused = on;
            Time.timeScale = on ? 0f : 1f;
            input.GameplayBlocked = on;
            pauseOverlay.SetActive(on);
        }

        private string StatusText()
        {
            StringBuilder sb = new StringBuilder();
            string mode = toolMode.Mode == ToolMode.Combat ? "Combate" : toolMode.Mode == ToolMode.Mining ? "Mineração" : "Construção";
            sb.Append("Modo [1/2/3]: ").Append(mode);
            if (toolMode.Mode == ToolMode.Combat)
            {
                sb.Append("   Golpe [Q/E]: ").Append(combat.SelectedName);
            }
            else if (toolMode.Mode == ToolMode.Build)
            {
                TerrainMaterialDef material = terrain.Palette.Material(builder.SelectedMaterial);
                sb.Append("   Material [Q/E]: ").Append(material != null ? material.DisplayName : "?")
                    .Append("   Forma [R]: ").Append(builder.SelectedShapeName);
            }

            sb.Append("\nFundo: ").Append(backdrop.CurrentBackdrop >= 0 ? backdrop.NameOf(backdrop.CurrentBackdrop) : "?")
                .Append(backdrop.ManualBackdrop >= 0 ? " (manual)" : " (automático)")
                .Append("   Boss: ").Append(bossVisual.VersionName).Append(" ").Append(boss.State)
                .Append("   Zoom: ").Append(zoom.PixelScale.ToString("0.##")).Append("x")
                .Append(controller.Noclip ? "   VOO LIVRE [N]" : "")
                .Append("   Efeitos vivos: ").Append(registry.Count)
                .Append("   FPS: ").Append((1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime)).ToString("0"));
            sb.Append("\nF1 painel · Esc pausa · A/D anda · Shift corre · Espaço pula · clique: ação do modo · direito: projétil · roda: zoom");
            return sb.ToString();
        }

        // ------------------------------------------------------------------ botões

        public void Teleport(int index)
        {
            if (index < 0 || index >= DestinationCount)
            {
                return;
            }

            Vector2 point = destinationPoints[index];
            controller.Teleport(point);
            vitals.SetRespawnPoint(point);
            zoom.ResetZoom();
            follow.Recenter();
        }

        public void SetBackdrop(int index)
        {
            backdrop.SetManual(index);
        }

        public void SetBossVersion(int version)
        {
            bossVisual.SetVersion(version);
        }

        public void RestartFight()
        {
            boss.ResetFight();
        }

        /// <summary>Teste da barra: põe a vida do boss numa porcentagem exata.</summary>
        public void SetBossPercent(int percent)
        {
            if (boss.State == BossState.Dormant)
            {
                return;
            }

            boss.Health.SetCurrent(boss.Health.Max * percent / 100f);
        }

        public void HealPlayer()
        {
            vitals.Heal();
        }

        public void HurtPlayer(int amount)
        {
            playerHealth.TakeDamage(amount, (Vector2)controller.transform.position + Vector2.right);
        }

        public void ResetTerrain()
        {
            terrain.ResetTerrain();
        }

        public void ResetDummies()
        {
            foreach (TrainingDummy dummy in dummies)
            {
                dummy.ResetDummy();
            }
        }

        public void ClearEffects()
        {
            registry.ClearAll();
        }

        public void ToggleGrid()
        {
            overlay.ShowGrid = !overlay.ShowGrid;
        }

        public void TogglePivots()
        {
            overlay.ShowPivots = !overlay.ShowPivots;
        }

        public void ToggleColliders()
        {
            overlay.ShowColliders = !overlay.ShowColliders;
        }

        public void ToggleLabels()
        {
            labels.SetActive(!labels.activeSelf);
        }

        public void ToggleNoclip()
        {
            controller.SetNoclip(!controller.Noclip);
        }

        public void SetMode(int mode)
        {
            toolMode.SetMode((ToolMode)mode);
        }

        public void TriggerAttack(int index)
        {
            toolMode.SetMode(ToolMode.Combat);
            combat.TriggerAttack(index);
        }

        public void SelectMaterial(int number)
        {
            toolMode.SetMode(ToolMode.Build);
            builder.SelectMaterial(number);
        }

        public void CycleShape()
        {
            builder.CycleShape();
        }

        public void ViewerPause()
        {
            viewer.TogglePause();
        }

        public void ViewerStep(int direction)
        {
            viewer.Step(direction);
        }

        public void ViewerBaseline()
        {
            viewer.ToggleBaseline();
        }

        public void ViewerBounds()
        {
            viewer.ToggleBounds();
        }

        public void Resume()
        {
            SetPause(false);
        }

        public void Quit()
        {
            Application.Quit();
        }
    }
}
