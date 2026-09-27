using Lithostride.Core;
using UnityEngine;

namespace Lithostride.Player
{
    /// <summary>
    /// Troca os quadros do Kael pelo estado da física. Sem Animator nem
    /// AnimationClip: são poucas sequências e o tempo depende da velocidade.
    ///
    /// Caminhada e corrida usam os quadros 1..8 das suas pranchas em laço; o
    /// quadro 0 de cada prancha é a pose parada (igual ao MASTER). O pulo não
    /// toca em laço: cada pose vale por uma faixa de velocidade vertical —
    /// preparação (só no instante do impulso, sem atrasar a física), subida,
    /// ápice, queda, queda longa — e, ao tocar o chão, aterrissagem e
    /// recuperação por instantes.
    ///
    /// Degraus: a física sobe ou desce uma célula de uma vez; o desenho parte
    /// do lugar antigo e alcança o corpo em poucos centésimos (deslocamento
    /// no <see cref="PixelSnap"/>), sem pulo visual de 16 px.
    /// </summary>
    public sealed class PlayerAnimator : MonoBehaviour
    {
        private const int JumpPrep = 1, JumpRise = 2, JumpApex = 3, JumpFall = 4, JumpFallLong = 5, JumpLand = 6, JumpRecover = 7;

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private PlayerController controller;
        [SerializeField] private PixelSnap visualSnap;

        [SerializeField] private KaelSequence idle;
        [SerializeField] private KaelSequence walk;
        [SerializeField] private KaelSequence run;
        [SerializeField] private KaelSequence jump;

        [SerializeField, Min(0.1f)] private float idleFramesPerSecond = 7f;
        [SerializeField, Min(0.1f)] private float walkFramesPerSecond = 11f;
        [SerializeField, Min(0.1f)] private float runFramesPerSecond = 15f;

        /// <summary>Faixa de velocidade vertical tratada como ápice (pose encolhida).</summary>
        [SerializeField, Min(0f)] private float apexBand = 3.5f;

        [SerializeField, Min(0f)] private float fallLongDelay = 0.2f;
        [SerializeField, Min(0f)] private float landTime = 0.09f;
        [SerializeField, Min(0f)] private float recoverTime = 0.07f;

        /// <summary>Velocidade com que o desenho alcança o corpo depois de um degrau (u/s).</summary>
        [SerializeField, Min(0.1f)] private float stepCatchUpSpeed = 14f;

        private KaelSequence current;
        private float timer;
        private float landTimer = -1f;
        private float fallTimer;
        private int frameIndex;
        private float stepOffset;
        private Vector3 baseOffset;

        /// <summary>Âncora da mão no quadro atual, em unidades locais (já espelhada quando virado à esquerda).</summary>
        public Vector2 HandAnchor { get; private set; }

        /// <summary>Ângulo da lâmina no quadro atual (graus, virado à direita).</summary>
        public int SwordAngle { get; private set; } = 55;

        /// <summary>O punho aparece no quadro atual (medido) ou está atrás do tronco (estimado).</summary>
        public bool HandVisible { get; private set; } = true;

        public KaelSequence Idle => idle;
        public KaelSequence Walk => walk;
        public KaelSequence Run => run;
        public KaelSequence Jump => jump;

        public bool FacingLeft => controller != null && controller.FacingLeft;

        /// <summary>O desenho do Kael (golpes presos à mão ficam na mesma grade de pixels dele).</summary>
        public SpriteRenderer BodyRenderer => spriteRenderer;

        /// <summary>Quadro atual (sprite) — o comparador de mão e a espada leem daqui.</summary>
        public Sprite CurrentSprite => spriteRenderer != null ? spriteRenderer.sprite : null;

        /// <summary>Congela a animação (visualizador e pausa).</summary>
        public bool Frozen { get; set; }

        public void Configure(SpriteRenderer renderer, PlayerController playerController, PixelSnap snap, KaelSequence idleFrames,
            KaelSequence walkFrames, KaelSequence runFrames, KaelSequence jumpFrames)
        {
            spriteRenderer = renderer;
            controller = playerController;
            visualSnap = snap;
            idle = idleFrames;
            walk = walkFrames;
            run = runFrames;
            jump = jumpFrames;
        }

        private void Awake()
        {
            if (visualSnap != null)
            {
                baseOffset = visualSnap.LocalOffset;
            }
        }

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.Landed += OnLanded;
                controller.Stepped += OnStepped;
            }
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.Landed -= OnLanded;
                controller.Stepped -= OnStepped;
            }
        }

        private void OnLanded(float impactVelocity)
        {
            // Só pouso de verdade (não degrau nem rampa) mostra a aterrissagem.
            if (impactVelocity < -6f)
            {
                landTimer = 0f;
            }
        }

        private void OnStepped(float rise)
        {
            // O corpo já está no degrau novo; o desenho começa onde estava.
            stepOffset = Mathf.Clamp(stepOffset - rise, -1.2f, 1.2f);
        }

        private void Update()
        {
            if (spriteRenderer == null || controller == null)
            {
                return;
            }

            UpdateStepOffset();
            if (Frozen)
            {
                return;
            }

            float dt = Time.deltaTime;
            spriteRenderer.flipX = controller.FacingLeft;
            bool moving = !Mathf.Approximately(controller.MoveAxis, 0f);

            if (controller.IsTakingOff)
            {
                Show(jump, JumpPrep);
                return;
            }

            if (!controller.IsGrounded && !controller.Noclip)
            {
                landTimer = -1f;
                float vy = controller.Velocity.y;
                fallTimer = vy < -apexBand ? fallTimer + dt : 0f;
                int pose = vy > apexBand ? JumpRise : vy >= -apexBand ? JumpApex : fallTimer < fallLongDelay ? JumpFall : JumpFallLong;
                Show(jump, pose);
                return;
            }

            if (landTimer >= 0f)
            {
                landTimer += dt;
                if (landTimer < landTime)
                {
                    Show(jump, JumpLand);
                    return;
                }

                if (!moving && landTimer < landTime + recoverTime)
                {
                    Show(jump, JumpRecover);
                    return;
                }

                landTimer = -1f;
            }

            if (moving)
            {
                bool running = controller.IsRunning;
                Loop(running ? run : walk, running ? runFramesPerSecond : walkFramesPerSecond, 1);
            }
            else
            {
                Loop(idle, idleFramesPerSecond, 0);
            }
        }

        private void UpdateStepOffset()
        {
            if (visualSnap == null)
            {
                return;
            }

            stepOffset = Mathf.MoveTowards(stepOffset, 0f, stepCatchUpSpeed * Time.deltaTime);
            visualSnap.LocalOffset = baseOffset + new Vector3(0f, stepOffset, 0f);
        }

        /// <summary>Laço de quadros a partir de <paramref name="first"/>; trocar de sequência reinicia.</summary>
        private void Loop(KaelSequence frames, float framesPerSecond, int first)
        {
            if (frames == null || frames.Count <= first)
            {
                return;
            }

            if (frames != current)
            {
                current = frames;
                timer = 0f;
            }

            timer += Time.deltaTime;
            int count = frames.Count - first;
            frameIndex = first + ((int)(timer * framesPerSecond) % count);
            Apply(frames, frameIndex);
        }

        private void Show(KaelSequence frames, int index)
        {
            if (frames == null || index >= frames.Count)
            {
                return;
            }

            current = frames;
            timer = 0f;
            frameIndex = index;
            Apply(frames, index);
        }

        /// <summary>
        /// Mostra um quadro exato, congelando a animação (comparador de mão e
        /// visualizador). O sentido vem do controle.
        /// </summary>
        public void ShowFrame(KaelSequence frames, int index)
        {
            Frozen = true;
            spriteRenderer.flipX = controller.FacingLeft;
            Show(frames, index);
        }

        private void Apply(KaelSequence frames, int index)
        {
            spriteRenderer.sprite = frames.Frame(index);
            Vector2 hand = frames.Hand(index);
            HandAnchor = controller.FacingLeft ? new Vector2(-hand.x, hand.y) : hand;
            SwordAngle = frames.SwordAngle(index);
            HandVisible = frames.HandVisible(index);
        }
    }
}
