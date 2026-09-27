using System.Globalization;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Lithostride.Tests
{
    /// <summary>
    /// Movimento do Kael medido numa simulação física reproduzível (ver
    /// <see cref="KaelPhysicsHarness"/>): pulo em vários ritmos de quadro,
    /// toque curto e longo, segurar, buffer, coyote, inversão no ar, teto
    /// baixo, degraus e rampas nos dois sentidos. Os registros por passo e o
    /// resumo das medidas ficam em Documentation/Evidencias/testes.
    /// </summary>
    public sealed class KaelJumpTests
    {
        private const float GroundTop = 1f;
        private const float Dt60 = 1f / 60f;

        public static string EvidenceFolder
        {
            get
            {
                string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Documentation", "Evidencias", "testes");
                Directory.CreateDirectory(folder);
                return folder;
            }
        }

        [OneTimeSetUp]
        public void NewSummary()
        {
            File.WriteAllText(Path.Combine(EvidenceFolder, "movimento_resumo.txt"),
                "Movimento do Kael — simulação física (Physics2D.Simulate, passo fixo 0,02 s). Unidades: células.\n");
        }

        private static void Summary(string line)
        {
            File.AppendAllText(Path.Combine(EvidenceFolder, "movimento_resumo.txt"), line + "\n");
        }

        private static string F(float v)
        {
            return v.ToString("0.000", CultureInfo.InvariantCulture);
        }

        private static KaelPhysicsHarness FlatGround(bool log, float x = 0.5f)
        {
            KaelPhysicsHarness h = new KaelPhysicsHarness(new Vector2(x, GroundTop), log);
            h.Fill(-40, -6, 40, 0);
            h.Commit();
            return h;
        }

        private static void Run(KaelPhysicsHarness h, float dt, float seconds, float move, bool held, bool run = false)
        {
            for (float t = 0f; t < seconds; t += dt)
            {
                h.Frame(dt, move, false, held, run);
            }
        }

        // ------------------------------------------------------------------ pulo

        /// <summary>Parado no plano, pulo segurado: sobe a altura-alvo e volta ao chão, com uma aterrissagem.</summary>
        [TestCase(30f)]
        [TestCase(60f)]
        [TestCase(144f)]
        public void PuloParado_AlcancaAlturaAlvo(float framesPerSecond)
        {
            using (KaelPhysicsHarness h = FlatGround(true))
            {
                float dt = 1f / framesPerSecond;
                Run(h, dt, 0.5f, 0f, false);
                Assert.IsTrue(h.Controller.IsGrounded, "Kael deveria estar apoiado antes do pulo");

                float start = h.Body.position.y, apex = start;
                h.AfterFixedStep = x => apex = Mathf.Max(apex, x.Body.position.y);
                int landedBefore = h.LandedCount;

                h.Frame(dt, 0f, true, true);
                Run(h, dt, 1.6f, 0f, true);
                Run(h, dt, 0.4f, 0f, false);

                float height = apex - start;
                File.WriteAllText(Path.Combine(EvidenceFolder, "pulo_parado_" + framesPerSecond + "fps.txt"),
                    "altura=" + F(height) + " aterrissagens=" + (h.LandedCount - landedBefore) + " pulos=" + h.JumpedCount + "\n" + h.Log);
                Summary("pulo parado " + framesPerSecond + " fps: altura " + F(height) + " células, aterrissagens " +
                        (h.LandedCount - landedBefore) + ", y final " + F(h.Body.position.y));

                Assert.GreaterOrEqual(height, 3.0f, "altura do pulo (blocos)");
                Assert.LessOrEqual(height, 4.5f, "altura do pulo (blocos)");
                Assert.IsTrue(h.Controller.IsGrounded, "volta ao chão");
                Assert.AreEqual(GroundTop, h.Body.position.y, 0.05f, "pousa no topo do chão");
                Assert.AreEqual(1, h.LandedCount - landedBefore, "uma aterrissagem por pulo");
                Assert.AreEqual(1, h.JumpedCount, "um pulo por toque");
            }
        }

        /// <summary>Toque curto (solta no mesmo quadro) dá pulo baixo; o longo, o pulo inteiro.</summary>
        [Test]
        public void ToqueCurtoEToqueLongo_AlturasDiferentes()
        {
            float Jump(bool hold)
            {
                using (KaelPhysicsHarness h = FlatGround(false))
                {
                    Run(h, Dt60, 0.4f, 0f, false);
                    float start = h.Body.position.y, apex = start;
                    h.AfterFixedStep = x => apex = Mathf.Max(apex, x.Body.position.y);
                    h.Frame(Dt60, 0f, true, hold);
                    Run(h, Dt60, 1.6f, 0f, hold);
                    Assert.IsTrue(h.Controller.IsGrounded, "volta ao chão");
                    return apex - start;
                }
            }

            float shortHop = Jump(false), longJump = Jump(true);
            Summary("toque curto: " + F(shortHop) + " células; toque longo: " + F(longJump) + " células");
            Assert.Greater(shortHop, 0.5f, "toque curto ainda sai do chão");
            Assert.Less(shortHop, 2.2f, "toque curto é baixo");
            Assert.Greater(longJump - shortHop, 1.5f, "segurar sobe bem mais");
        }

        /// <summary>Segurar o pulo por 3 s gera um pulo só (não repete ao pousar).</summary>
        [Test]
        public void SegurarPulo_NaoRepete()
        {
            using (KaelPhysicsHarness h = FlatGround(false))
            {
                Run(h, Dt60, 0.4f, 0f, false);
                int landedBefore = h.LandedCount;
                h.Frame(Dt60, 0f, true, true);
                Run(h, Dt60, 3f, 0f, true);
                Summary("segurar 3 s: pulos " + h.JumpedCount + ", aterrissagens " + (h.LandedCount - landedBefore));
                Assert.AreEqual(1, h.JumpedCount);
                Assert.AreEqual(1, h.LandedCount - landedBefore);
                Assert.IsTrue(h.Controller.IsGrounded);
            }
        }

        /// <summary>Apertar pouco antes de pousar (buffer) pula de novo ao tocar o chão.</summary>
        [Test]
        public void Buffer_ApertarAntesDePousar()
        {
            using (KaelPhysicsHarness h = FlatGround(true))
            {
                Run(h, Dt60, 0.4f, 0f, false);
                h.Frame(Dt60, 0f, true, true);
                // Cai até faltar ~0,5 célula para o chão (menos de 0,1 s de queda).
                float guard = 0f;
                while ((h.JumpedCount < 1 || h.Body.linearVelocity.y > 0f || h.Body.position.y > GroundTop + 0.55f) && guard < 3f)
                {
                    h.Frame(Dt60, 0f, false, false);
                    guard += Dt60;
                }

                float landedAt = -1f, secondJumpAt = -1f;
                int landed = h.LandedCount;
                h.AfterFixedStep = x =>
                {
                    if (landedAt < 0f && x.LandedCount > landed) landedAt = x.Time;
                    if (secondJumpAt < 0f && x.JumpedCount >= 2) secondJumpAt = x.Time;
                };
                h.Frame(Dt60, 0f, true, true);
                Run(h, Dt60, 0.5f, 0f, true);
                File.WriteAllText(Path.Combine(EvidenceFolder, "pulo_buffer.txt"), h.Log.ToString());
                Summary("buffer: pulos " + h.JumpedCount + ", segundo pulo " + F(secondJumpAt - landedAt) + " s após pousar");
                Assert.AreEqual(2, h.JumpedCount, "o aperto no ar ficou guardado e virou pulo");
                Assert.GreaterOrEqual(landedAt, 0f, "pousou");
                Assert.LessOrEqual(secondJumpAt - landedAt, 0.021f, "pulo sai no passo do pouso ou no seguinte");
            }
        }

        /// <summary>Sai da borda andando e aperta logo depois (coyote): pula. Tarde demais: cai.</summary>
        [TestCase(0.05f, true)]
        [TestCase(0.25f, false)]
        public void Coyote_DepoisDaBorda(float delay, bool expectJump)
        {
            using (KaelPhysicsHarness h = new KaelPhysicsHarness(new Vector2(-2f, GroundTop), false))
            {
                h.Fill(-30, -20, -1, 0);
                h.Commit();
                Run(h, Dt60, 0.3f, 0f, false);
                float guard = 0f;
                while (h.Controller.IsGrounded && guard < 2f)
                {
                    h.Frame(Dt60, 1f, false, false);
                    guard += Dt60;
                }

                Run(h, Dt60, delay, 1f, false);
                float y0 = h.Body.position.y, apex = y0;
                h.AfterFixedStep = x => apex = Mathf.Max(apex, x.Body.position.y);
                h.Frame(Dt60, 1f, true, true);
                Run(h, Dt60, 0.4f, 1f, true);
                Summary("coyote " + F(delay) + " s: pulos " + h.JumpedCount + ", subida " + F(apex - y0));
                Assert.AreEqual(expectJump ? 1 : 0, h.JumpedCount);
                if (expectJump)
                {
                    Assert.Greater(apex - y0, 2.5f, "o pulo de coyote sobe de verdade");
                }
            }
        }

        /// <summary>Correndo para a direita, inverte no ar: a velocidade horizontal troca de sinal na hora.</summary>
        [Test]
        public void InverterDirecaoNoAr()
        {
            using (KaelPhysicsHarness h = FlatGround(false, -20f))
            {
                Run(h, Dt60, 0.3f, 1f, false, true);
                h.Frame(Dt60, 1f, true, true, true);
                Run(h, Dt60, 0.25f, 1f, true, true);
                float xTurn = h.Body.position.x;
                Run(h, Dt60, 0.1f, -1f, true, true);
                float vx = h.Body.linearVelocity.x;
                Run(h, Dt60, 1f, -1f, true, true);
                Summary("inversão no ar: vx depois " + F(vx) + ", x virada " + F(xTurn) + ", x final " + F(h.Body.position.x));
                Assert.Less(vx, -5f, "controle no ar responde à inversão");
                Assert.Less(h.Body.position.x, xTurn, "voltou para a esquerda");
                Assert.IsTrue(h.Controller.IsGrounded);
            }
        }

        /// <summary>Teto baixo: a cabeça bate, o corpo não atravessa nem é teleportado, e volta ao chão.</summary>
        [Test]
        public void TetoBaixo_CabecaBateSemAtravessar()
        {
            using (KaelPhysicsHarness h = FlatGround(false))
            {
                h.Fill(-10, 5, 10, 8); // teto com face de baixo em y = 5 (4 células livres acima do chão)
                h.Commit();
                Run(h, Dt60, 0.4f, 0f, false);
                float maxTop = 0f;
                h.AfterFixedStep = x => maxTop = Mathf.Max(maxTop, x.Capsule.bounds.max.y);
                h.Frame(Dt60, 0f, true, true);
                Run(h, Dt60, 1.5f, 0f, true);
                Summary("teto baixo (face em 5): topo máximo da cápsula " + F(maxTop) + ", y final " + F(h.Body.position.y));
                Assert.LessOrEqual(maxTop, 5.05f, "não atravessa o teto");
                Assert.AreEqual(GroundTop, h.Body.position.y, 0.05f, "volta ao chão");
            }
        }

        // ------------------------------------------------------------------ degraus e rampas

        [Test]
        public void DegrauDeUmaCelula_SobeAndando()
        {
            using (KaelPhysicsHarness h = FlatGround(false, 0f))
            {
                h.Fill(3, 1, 20, 1); // degrau de 1 célula a partir de x = 3
                h.Commit();
                Run(h, Dt60, 0.3f, 0f, false);
                int air = 0;
                h.AfterFixedStep = x => { if (!x.Controller.IsGrounded) air++; };
                Run(h, Dt60, 1.2f, 1f, false);
                Summary("degrau 1: y final " + F(h.Body.position.y) + ", x " + F(h.Body.position.x) + ", passos no ar " + air);
                Assert.AreEqual(2f, h.Body.position.y, 0.06f, "está em cima do degrau");
                Assert.Greater(h.Body.position.x, 5f, "passou do degrau");
                Assert.LessOrEqual(air, 3, "subiu sem voar");
            }
        }

        [Test]
        public void ParedeDeDuasCelulas_PedePulo()
        {
            using (KaelPhysicsHarness h = FlatGround(false, 0f))
            {
                h.Fill(3, 1, 20, 2);
                h.Commit();
                Run(h, Dt60, 0.3f, 0f, false);
                Run(h, Dt60, 1.2f, 1f, false);
                Summary("parede 2: x final " + F(h.Body.position.x) + ", y " + F(h.Body.position.y));
                Assert.Less(h.Body.position.x, 3f, "não atravessa nem escala a parede");
                Assert.AreEqual(GroundTop, h.Body.position.y, 0.06f);

                h.Frame(Dt60, 1f, true, true);
                Run(h, Dt60, 1.2f, 1f, true);
                Assert.AreEqual(3f, h.Body.position.y, 0.06f, "com pulo, sobe a parede de 2");
            }
        }

        /// <summary>Escada descendo (1 célula a cada 3): acompanha o chão, sem voo.</summary>
        [Test]
        public void EscadaDescendo_ColadoAoChao()
        {
            using (KaelPhysicsHarness h = new KaelPhysicsHarness(new Vector2(0.5f, 6f), false))
            {
                h.Fill(-10, -6, 2, 5);
                for (int s = 0; s < 5; s++)
                {
                    h.Fill(3 + (s * 3), -6, 5 + (s * 3), 4 - s);
                }

                h.Fill(18, -6, 40, 0);
                h.Commit();
                Run(h, Dt60, 0.3f, 0f, false);
                int air = 0, ticks = 0;
                float worstImpact = 0f;
                h.Controller.Landed += v => worstImpact = Mathf.Min(worstImpact, v);
                h.AfterFixedStep = x => { ticks++; if (!x.Controller.IsGrounded) air++; };
                Run(h, Dt60, 3.2f, 1f, false);
                Summary("escada descendo: y final " + F(h.Body.position.y) + ", passos no ar " + air + "/" + ticks +
                        ", pior impacto " + F(worstImpact));
                Assert.AreEqual(1f, h.Body.position.y, 0.06f, "chegou embaixo");
                Assert.LessOrEqual(air, ticks / 10, "no chão em pelo menos 90% dos passos");
                Assert.Greater(worstImpact, -6f, "sem queda que mostre aterrissagem");
            }
        }

        /// <summary>Rampa de 45 graus nos dois sentidos: sobe e desce colado, nas duas direções.</summary>
        [TestCase(true)]
        [TestCase(false)]
        public void Rampa_SobeEDesceNosDoisSentidos(bool upRight)
        {
            float dir = upRight ? 1f : -1f;
            using (KaelPhysicsHarness h = new KaelPhysicsHarness(new Vector2(-6f * dir, GroundTop), true))
            {
                h.Fill(-40, -6, 40, 0);
                // Rampa de 3 células subindo no sentido 'dir' a partir de x = 0, platô no alto.
                for (int i = 0; i < 3; i++)
                {
                    int x = upRight ? i : -1 - i;
                    h.Fill(x, 1, x, i);
                    h.Slope(x, 1 + i, upRight);
                }

                if (upRight) h.Fill(3, 1, 40, 3);
                else h.Fill(-40, 1, -4, 3);
                h.Commit();

                Run(h, Dt60, 0.3f, 0f, false);
                int air = 0;
                float slopeNormal = 1f;
                h.AfterFixedStep = x =>
                {
                    if (!x.Controller.IsGrounded) air++;
                    else slopeNormal = Mathf.Min(slopeNormal, x.GroundNormal.y);
                };
                Run(h, Dt60, 2f, dir, false);
                float top = h.Body.position.y;
                int airUp = air;
                air = 0;
                Run(h, Dt60, 2.2f, -dir, false);
                File.WriteAllText(Path.Combine(EvidenceFolder, "rampa_" + (upRight ? "dir" : "esq") + ".txt"), h.Log.ToString());
                Summary("rampa " + (upRight ? "↗" : "↖") + ": alto " + F(top) + " (no ar " + airUp + "), volta " +
                        F(h.Body.position.y) + " (no ar " + air + ")");
                Assert.Less(slopeNormal, 0.8f, "andou sobre a rampa (normal inclinada), não sobre degraus");
                Assert.AreEqual(4f, top, 0.08f, "subiu ao platô");
                Assert.LessOrEqual(airUp, 3, "subida colada");
                Assert.AreEqual(GroundTop, h.Body.position.y, 0.08f, "desceu ao chão");
                Assert.LessOrEqual(air, 6, "descida colada");
            }
        }
    }
}
