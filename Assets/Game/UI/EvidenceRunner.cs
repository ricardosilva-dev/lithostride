using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lithostride.Combat;
using Lithostride.Core;
using Lithostride.Creatures;
using Lithostride.Player;
using Lithostride.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Lithostride.UI
{
    /// <summary>
    /// Roteiro de verificação do executável: só roda com o argumento
    /// "-evidencias &lt;pasta&gt;" (sem ele, desliga-se na hora). Percorre a
    /// demonstração com entrada simulada pelo Input System (o mesmo caminho
    /// do teclado real), grava capturas de tela e escreve um relatório com
    /// as verificações (APROVADO/REPROVADO) e as medidas.
    /// "-evidencias-curto" faz só as capturas de interface e fundo (para
    /// rodar em várias resoluções).
    /// </summary>
    public sealed class EvidenceRunner : MonoBehaviour
    {
        [SerializeField] private TestPanel panel;
        [SerializeField] private PlayerController controller;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerVitals vitals;
        [SerializeField] private Health playerHealth;
        [SerializeField] private FlyingBoss boss;
        [SerializeField] private BossVisual bossVisual;
        [SerializeField] private TerrainGrid terrain;
        [SerializeField] private TrainingDummy[] dummies;
        [SerializeField] private Health[] dummyHealth;
        [SerializeField] private CameraZoom zoom;
        [SerializeField] private CameraFollow follow;
        [SerializeField] private BackdropView backdrop;
        [SerializeField] private EffectRegistry registry;
        [SerializeField] private PropSupport[] props;

        private readonly StringBuilder report = new StringBuilder();
        private string folder;
        private int shot;
        private int passed;
        private int failed;

        public void Configure(TestPanel testPanel, PlayerController playerController, PlayerCombat playerCombat,
            PlayerVitals playerVitals, Health health, FlyingBoss flyingBoss, BossVisual flyingBossVisual, TerrainGrid terrainGrid,
            TrainingDummy[] trainingDummies, Health[] trainingHealth, CameraZoom cameraZoom, CameraFollow cameraFollow,
            BackdropView backdropView, EffectRegistry effects, PropSupport[] propSupports)
        {
            panel = testPanel;
            controller = playerController;
            combat = playerCombat;
            vitals = playerVitals;
            playerHealth = health;
            boss = flyingBoss;
            bossVisual = flyingBossVisual;
            terrain = terrainGrid;
            dummies = trainingDummies;
            dummyHealth = trainingHealth;
            zoom = cameraZoom;
            follow = cameraFollow;
            backdrop = backdropView;
            registry = effects;
            props = propSupports;
        }

        private void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-evidencias");
            if (index < 0 || index + 1 >= args.Length)
            {
                enabled = false;
                return;
            }

            folder = args[index + 1];
            Directory.CreateDirectory(folder);
            bool shortRun = Array.IndexOf(args, "-evidencias-curto") >= 0;
            StartCoroutine(shortRun ? ShortRun() : FullRun());
        }

        // ------------------------------------------------------------------ roteiros

        private IEnumerator ShortRun()
        {
            Line("Resolução " + Screen.width + "x" + Screen.height);
            yield return Wait(1.5f);
            yield return Shot("inicio");
            panel.Teleport(Destination("Arena"));
            yield return Wait(0.5f);
            yield return Walk(1f, 1.2f, false);
            yield return WaitFor(() => boss.Engaged, 4f);
            panel.SetBossPercent(63);
            yield return Wait(1.2f);
            yield return Shot("barra_63");
            for (int b = 0; b < backdrop.Count; b++)
            {
                panel.SetBackdrop(b);
                yield return Wait(0.8f);
                yield return Shot("fundo_" + backdrop.NameOf(b));
            }

            zoom.SetRelativeScale(0.25f);
            yield return Wait(0.5f);
            yield return Shot("zoom_minimo");
            zoom.SetRelativeScale(3f);
            yield return Wait(0.5f);
            yield return Shot("zoom_maximo");
            Finish();
        }

        private IEnumerator FullRun()
        {
            yield return Wait(1.5f);
            float ground = 1f;
            Check("Kael apoiado no chão ao nascer", controller.IsGrounded && Mathf.Abs(controller.transform.position.y - ground) < 0.05f,
                "y=" + controller.transform.position.y.ToString("0.000"));
            yield return Shot("inicio_zoom1");

            // Andar e correr no plano: sempre no chão, sem afundar nem flutuar.
            float minY = float.MaxValue, maxY = float.MinValue;
            int airborne = 0, frames = 0;
            yield return Walk(1.5f, 1f, false, () =>
            {
                frames++;
                float y = controller.transform.position.y;
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
                if (!controller.IsGrounded) airborne++;
            });
            Check("Caminhada no plano sem afundar/flutuar", maxY - minY < 0.02f && airborne == 0,
                "y " + minY.ToString("0.000") + ".." + maxY.ToString("0.000") + ", quadros no ar " + airborne + "/" + frames);
            yield return Shot("caminhada");
            float startX = controller.transform.position.x;
            yield return Walk(1f, 1f, true);
            Check("Corrida mais rápida que caminhada", controller.transform.position.x - startX > 8f,
                "andou " + (controller.transform.position.x - startX).ToString("0.00") + " u em 1 s");
            yield return Shot("corrida");

            // Pulo: altura e aterrissagem.
            yield return Wait(0.3f);
            float before = controller.transform.position.y, apex = before;
            Press(Key.Space);
            for (float t = 0f; t < 1.4f; t += Time.deltaTime)
            {
                apex = Mathf.Max(apex, controller.transform.position.y);
                if (t > 0.1f && t < 0.15f) yield return Shot("pulo_subida");
                if (t > 0.42f && t < 0.47f) yield return Shot("pulo_apice");
                if (t > 0.75f && t < 0.8f) yield return Shot("pulo_queda");
                if (t > 0.06f) Release();
                yield return null;
            }

            Check("Pulo de ~4 blocos e volta ao chão", apex - before > 3.6f && controller.IsGrounded,
                "altura " + (apex - before).ToString("0.00"));

            // Degraus de 1 bloco subidos andando.
            Teleport(41f, 1f);
            yield return Walk(1.4f, 1f, false);
            Check("Sobe degraus de 1 bloco andando", controller.transform.position.y > 3.9f,
                "y final " + controller.transform.position.y.ToString("0.00") + " (platô em 4)");
            yield return Shot("degraus");

            // Rampa descendo e rampa subindo.
            Teleport(56f, 4f);
            airborne = 0;
            yield return Walk(1.3f, 1f, false, () => { if (!controller.IsGrounded) airborne++; });
            Check("Desce a rampa colado ao chão", controller.transform.position.y < 1.1f && airborne < 6,
                "y final " + controller.transform.position.y.ToString("0.00") + ", quadros no ar " + airborne);
            Teleport(79f, 1f);
            yield return Walk(1.2f, 1f, false);
            yield return Shot("rampa_subida");
            Check("Sobe a rampa de arenito sem pular", controller.transform.position.y > 4.9f,
                "y final " + controller.transform.position.y.ToString("0.00") + " (platô em 5)");
            zoom.SetRelativeScale(3f);
            Teleport(83f, 3f);
            yield return Wait(0.8f);
            yield return Shot("rampa_zoom3");
            Teleport(106f, 1f);
            yield return Wait(0.8f);
            yield return Shot("degraus_plataformas_zoom3");
            zoom.SetRelativeScale(1f);

            // Morro 8x8 e túnel da caverna até o salão.
            Teleport(121f, 1f);
            yield return Wait(0.5f);
            yield return Walk(1.2f, 1f, true);
            yield return Shot("morro_rampa");
            Teleport(144f, 1f);
            yield return Walk(4.5f, 1f, true);
            Check("Desce o túnel inteiro até o salão", controller.transform.position.y < -24f,
                "y final " + controller.transform.position.y.ToString("0.00"));
            yield return Shot("caverna_salao");
            Check("Fundo automático da caverna", backdrop.NameOf(backdrop.CurrentBackdrop) == "caverna",
                backdrop.NameOf(backdrop.CurrentBackdrop));

            // Minerar sob uma árvore: ela reage e some; reset devolve.
            PropSupport tree = null;
            foreach (PropSupport p in props)
            {
                if (p != null && p.SupportCells.Length > 0 && p.transform.position.x < 40f) { tree = p; break; }
            }

            if (tree != null)
            {
                Vector2Int cell = tree.SupportCells[0];
                Teleport(tree.transform.position.x - 4f, 1f);
                yield return Wait(0.3f);
                yield return Shot("antes_de_minerar_sob_arvore");
                terrain.Break(new Vector3Int(cell.x, cell.y, 0));
                yield return Wait(0.35f);
                yield return Shot("arvore_perdendo_apoio");
                yield return Wait(1f);
                SpriteRenderer treeRenderer = tree.GetComponent<SpriteRenderer>();
                Check("Árvore sem apoio some (não flutua)", !treeRenderer.enabled, tree.name);
                panel.ResetTerrain();
                yield return Wait(0.3f);
                Check("Reset do terreno devolve bloco e árvore", terrain.HasBlock(new Vector3Int(cell.x, cell.y, 0)) && treeRenderer.enabled,
                    "célula " + cell);
            }

            // Colocar blocos (ferramenta de teste) e conectar.
            Vector3Int place = new Vector3Int(20, 2, 0);
            bool placed = terrain.Place(place, 3, TerrainShape.Full) && terrain.Place(place + Vector3Int.right, 3, TerrainShape.SlopeUpLeft);
            Check("Colocar bloco e rampa atualiza estado", placed && terrain.HasBlock(place), "pedra em " + place);
            Teleport(17f, 1f);
            zoom.SetRelativeScale(3f);
            yield return Wait(0.6f);
            yield return Shot("blocos_colocados_zoom3");
            zoom.SetRelativeScale(1f);
            panel.ResetTerrain();

            // Ataques no alvo de treino: um acerto por golpe.
            panel.Teleport(Destination("Treino"));
            panel.ResetDummies();
            Teleport(221.5f, 1f);
            yield return FaceRight();
            string[] shots = { "ataque_corte", "ataque_energia", "ataque_descendente", "ataque_projetil", "ataque_explosao" };
            for (int a = 0; a < combat.AttackCount; a++)
            {
                panel.ResetDummies();
                yield return Wait(0.35f);
                float hp = dummyHealth[0].Current;
                panel.TriggerAttack(a);
                yield return Wait(0.22f);
                yield return Shot(shots[a] + "_direita");
                yield return Wait(1.2f);
                float lost = hp - dummyHealth[0].Current;
                Check("Golpe '" + combat.AttackName(a) + "' causa dano no alvo", lost > 0f, "dano " + lost.ToString("0"));
            }

            panel.ResetDummies();
            yield return Wait(0.4f);
            float hpCorte = dummyHealth[0].Current;
            panel.TriggerAttack(0);
            yield return Wait(0.8f);
            Check("Corte acerta o alvo uma vez só", Mathf.Approximately(hpCorte - dummyHealth[0].Current, 18f),
                "dano " + (hpCorte - dummyHealth[0].Current).ToString("0"));

            // Virado para a esquerda: efeito, espada e hitbox espelhados.
            Teleport(226.5f, 1f);
            yield return FaceLeft();
            panel.ResetDummies();
            yield return Wait(0.35f);
            float hpLeft = dummyHealth[0].Current;
            panel.TriggerAttack(0);
            yield return Wait(0.22f);
            yield return Shot("ataque_corte_esquerda");
            yield return Wait(0.8f);
            Check("Corte virado à esquerda acerta", dummyHealth[0].Current < hpLeft, "dano " + (hpLeft - dummyHealth[0].Current).ToString("0"));
            yield return Shot("espada_empunhada_esquerda");
            yield return Wait(2f);
            Check("Efeitos e projéteis não acumulam", registry.Count == 0, "vivos " + registry.Count);

            // Boss: ativação, voo, ataques, barra e morte (duas vezes).
            for (int round = 1; round <= 2; round++)
            {
                panel.RestartFight();
                panel.HealPlayer();
                panel.Teleport(Destination("Arena"));
                yield return Walk(1.4f, 1f, false);
                yield return WaitFor(() => boss.Engaged, 4f);
                Check("Luta " + round + ": boss desperta com o jogador na arena", boss.Engaged, boss.State.ToString());
                for (int i = 0; i < 6 && round == 1; i++)
                {
                    yield return Wait(1.3f);
                    panel.HealPlayer();
                    yield return Shot("boss_" + boss.State.ToString().ToLowerInvariant() + "_" + i);
                }

                panel.SetBossPercent(100);
                yield return Wait(0.6f);
                if (round == 1) yield return Shot("barra_100");
                panel.SetBossPercent(63);
                yield return Wait(1.5f);
                if (round == 1) yield return Shot("barra_63");
                panel.SetBossPercent(7);
                yield return Wait(1.5f);
                if (round == 1) yield return Shot("barra_7");
                panel.SetBossPercent(0);
                yield return Wait(0.3f);
                if (round == 1) yield return Shot("boss_caindo");
                yield return WaitFor(() => boss.State == BossState.Dead, 4f);
                yield return Wait(0.8f);
                if (round == 1) yield return Shot("boss_morto_e_barra_0");
                Check("Luta " + round + ": boss morre, cai e para no chão", boss.State == BossState.Dead &&
                                                                          boss.transform.position.y < 4f, "y " + boss.transform.position.y.ToString("0.00"));
                Check("Luta " + round + ": projéteis limpos na morte", registry.Count == 0, "vivos " + registry.Count);
            }

            // Morte do jogador durante a luta e renascimento.
            panel.RestartFight();
            panel.Teleport(Destination("Arena"));
            yield return Walk(1.4f, 1f, false);
            yield return WaitFor(() => boss.Engaged, 4f);
            for (int i = 0; i < 8 && !vitals.IsDead; i++)
            {
                panel.HurtPlayer(25);
                yield return Wait(1f);
            }

            Check("Kael morre com vida zero", vitals.IsDead || playerHealth.IsDead, "vida " + playerHealth.Current);
            yield return Shot("kael_morrendo");
            yield return Wait(2f);
            Check("Kael renasce com vida cheia", !vitals.IsDead && Mathf.Approximately(playerHealth.Current, playerHealth.Max),
                "vida " + playerHealth.Current);
            Check("Boss volta a esperar após o renascimento", boss.State == BossState.Dormant &&
                                                             Mathf.Approximately(boss.Health.Current, boss.Health.Max), boss.State.ToString());

            // Fundos e zoom.
            panel.Teleport(Destination("Início"));
            for (int b = 0; b < backdrop.Count; b++)
            {
                panel.SetBackdrop(b);
                yield return Wait(0.8f);
                yield return Shot("fundo_" + backdrop.NameOf(b));
            }

            panel.SetBackdrop(-1);
            zoom.SetRelativeScale(0.25f);
            yield return Wait(0.6f);
            yield return Shot("zoom_minimo");
            zoom.SetRelativeScale(1f);

            // Catálogo, bancadas, galeria e visualizador.
            string[] places = { "Bancadas 1-4", "Bancadas 5-8", "Bancadas 9-12", "Bancadas 13-16", "Galeria 1", "Galeria 2", "Galeria 3", "Galeria 4", "Ícones", "Visualizador" };
            foreach (string place2 in places)
            {
                int d = Destination(place2);
                if (d < 0) continue;
                panel.Teleport(d);
                zoom.SetRelativeScale(place2.StartsWith("Galeria", StringComparison.Ordinal) || place2 == "Visualizador" ? 0.5f : 1f);
                yield return Wait(0.8f);
                yield return Shot("catalogo_" + place2.Replace(' ', '_'));
            }

            panel.ViewerBounds();
            yield return Wait(0.3f);
            yield return Shot("visualizador_limites");
            Finish();
        }

        // ------------------------------------------------------------------ ações

        private int Destination(string name)
        {
            for (int i = 0; i < panel.DestinationCount; i++)
            {
                if (panel.DestinationName(i) == name) return i;
            }

            return -1;
        }

        private void Teleport(float x, float y)
        {
            controller.Teleport(new Vector2(x, y));
            follow.Recenter();
        }

        private IEnumerator FaceRight()
        {
            yield return Walk(0.05f, 1f, false);
        }

        private IEnumerator FaceLeft()
        {
            yield return Walk(0.05f, -1f, false);
        }

        private IEnumerator Walk(float seconds, float direction, bool run, Action sample = null)
        {
            List<Key> keys = new List<Key> { direction > 0f ? Key.D : Key.A };
            if (run) keys.Add(Key.LeftShift);
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                Press(keys.ToArray());
                sample?.Invoke();
                yield return null;
            }

            Release();
            yield return null;
        }

        private static void Press(params Key[] keys)
        {
            if (Keyboard.current != null)
            {
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys));
            }
        }

        private static void Release()
        {
            if (Keyboard.current != null)
            {
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            }
        }

        private static IEnumerator Wait(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }

        private static IEnumerator WaitFor(Func<bool> condition, float timeout)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.deltaTime)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Captura o quadro final (jogo + interface) lendo o buffer depois de
        /// desenhado. O módulo ScreenCapture não faz parte do projeto; ler os
        /// pixels no fim do quadro dá o mesmo resultado sem pacote novo.
        /// </summary>
        private IEnumerator Shot(string name)
        {
            shot++;
            yield return new WaitForEndOfFrame();
            Texture2D image = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(folder, shot.ToString("00") + "_" + name + ".png"), image.EncodeToPNG());
            Destroy(image);
            yield return null;
        }

        private void Check(string name, bool ok, string detail)
        {
            if (ok) passed++;
            else failed++;
            Line((ok ? "APROVADO  " : "REPROVADO ") + name + " — " + detail);
        }

        private void Line(string text)
        {
            report.AppendLine(text);
            Debug.Log("[evidencias] " + text);
        }

        private void Finish()
        {
            Line("Total: " + passed + " aprovados, " + failed + " reprovados. Resolução " + Screen.width + "x" + Screen.height);
            File.WriteAllText(Path.Combine(folder, "relatorio.txt"), report.ToString());
            Application.Quit();
        }
    }
}
