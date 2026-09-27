using System;
using Lithostride.Core;
using UnityEngine;

namespace Lithostride.Player
{
    /// <summary>
    /// Movimento do jogador: andar, correr (Shift), pular, rampas e degraus. A
    /// física fica no objeto raiz; o desenho é um filho, que não interfere no corpo.
    ///
    /// Chão: contato cuja normal aponta para cima, desde que o corpo não esteja
    /// se afastando dele. A lista de contatos lida no FixedUpdate é a do passo
    /// anterior; no passo seguinte ao impulso ela ainda traz o chão que o
    /// corpo acabou de deixar. Tratar esse contato como chão zerava a subida e
    /// o snap puxava o corpo de volta (pulo de altura zero, registrado em
    /// Documentation/Evidencias/testes).
    ///
    /// Pulo: o impulso sai no mesmo passo em que o comando é consumido (buffer
    /// e coyote cobrem apertar um pouco antes de pousar ou depois de sair da
    /// borda); a pose de preparação é só visual (<see cref="IsTakingOff"/>).
    /// Soltar o botão durante a subida corta a velocidade uma vez: toque curto,
    /// pulo baixo; segurar não repete o pulo.
    ///
    /// Rampas e degraus: no chão, a velocidade segue a tangente do piso e a
    /// gravidade fica desligada. Degrau de até uma célula é subido andando, e
    /// descido colado ao chão (snap) — o desenho suaviza os dois
    /// (<see cref="Stepped"/>).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private GameInput input;

        [SerializeField, Min(0f)] private float walkSpeed = 6f;
        [SerializeField, Min(0f)] private float runSpeed = 10f;

        /// <summary>15,65 u/s com gravidade 3,2x (31,4 u/s²): o pé sobe ~3,9 células.</summary>
        [SerializeField, Min(0f)] private float jumpVelocity = 15.65f;

        [SerializeField, Min(0.1f)] private float gravityScale = 3.2f;

        /// <summary>Soltar o pulo na subida multiplica a velocidade vertical por este fator (uma vez).</summary>
        [SerializeField, Range(0.1f, 1f)] private float jumpCutFactor = 0.45f;

        /// <summary>Janela visual da pose de preparação logo após o impulso.</summary>
        [SerializeField, Min(0f)] private float takeoffTime = 0.06f;

        [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;

        /// <summary>Contato cujo corpo se afasta da superfície mais rápido que isto não é chão (u/s).</summary>
        [SerializeField, Min(0f)] private float separatingSpeed = 0.5f;

        /// <summary>Degrau subido andando: uma célula.</summary>
        [SerializeField, Min(0f)] private float stepHeight = 1.05f;

        /// <summary>Descida colada ao chão ao sair de um degrau ou crista de rampa: até uma célula.</summary>
        [SerializeField, Min(0f)] private float groundSnapDistance = 1.05f;

        [SerializeField, Min(1f)] private float noclipSpeed = 18f;

        private const float ProbeDepth = 0.1f;

        private readonly Collider2D[] overlaps = new Collider2D[8];
        private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];
        private readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        private ContactFilter2D groundFilter;
        private ContactFilter2D solidFilter;

        private float moveAxis;
        private bool running;
        private bool jumpHeld;
        private float jumpBufferTimer = -1f;
        private float coyoteTimer;
        private float airTime;
        private bool jumpRising;
        private bool wasGrounded;
        private float lowestAirVelocity;
        private float knockbackTimer;
        private Vector2 groundNormal = Vector2.up;

        public bool IsGrounded { get; private set; }

        /// <summary>Logo depois do impulso (pose de preparação/impulso; não atrasa a física).</summary>
        public bool IsTakingOff => jumpRising && airTime < takeoffTime;

        public bool IsRunning => running && !Mathf.Approximately(moveAxis, 0f);
        public bool FacingLeft { get; private set; }
        public bool Noclip { get; private set; }
        public float MoveAxis => moveAxis;
        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;

        /// <summary>Controle desligado (morte, renascer): o corpo só cai.</summary>
        public bool ControlEnabled { get; set; } = true;

        /// <summary>Tocou o chão vindo do ar; o argumento é a menor velocidade vertical da queda.</summary>
        public event Action<float> Landed;

        public event Action Jumped;

        /// <summary>O corpo foi deslocado na vertical por um degrau (positivo) ou snap (negativo).</summary>
        public event Action<float> Stepped;

        public void Configure(Rigidbody2D rigidbody, Collider2D collider, GameInput gameInput)
        {
            body = rigidbody;
            bodyCollider = collider;
            input = gameInput;
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            // Chão é contato cuja normal aponta para cima (até 50 graus): rampa de 45 conta, parede não.
            groundFilter = new ContactFilter2D();
            groundFilter.SetNormalAngle(40f, 140f);
            groundFilter.useTriggers = false;

            solidFilter = new ContactFilter2D();
            solidFilter.useTriggers = false;
            body.gravityScale = gravityScale;
        }

        /// <summary>Leva o corpo a um ponto (teleporte, renascer), parado.</summary>
        public void Teleport(Vector2 feet)
        {
            body.position = feet;
            transform.position = feet;
            body.linearVelocity = Vector2.zero;
            jumpBufferTimer = -1f;
            knockbackTimer = 0f;
            jumpRising = false;
            wasGrounded = false;
        }

        /// <summary>Empurrão (dano): tira o controle por um instante.</summary>
        public void Knockback(Vector2 velocity, float duration)
        {
            if (Noclip)
            {
                return;
            }

            body.gravityScale = gravityScale;
            body.linearVelocity = velocity;
            knockbackTimer = duration;
            jumpRising = false;
        }

        public void SetNoclip(bool enabled)
        {
            Noclip = enabled;
            body.simulated = !enabled;
            body.linearVelocity = Vector2.zero;
        }

        private void Update()
        {
            if (input == null)
            {
                return;
            }

            if (input.NoclipTogglePressed)
            {
                SetNoclip(!Noclip);
            }

            bool control = ControlEnabled;
            moveAxis = control ? input.MoveAxis : 0f;
            running = control && input.RunHeld;
            jumpHeld = control && input.JumpHeld;
            if (control && input.JumpPressed)
            {
                jumpBufferTimer = jumpBufferTime;
            }

            if (!Mathf.Approximately(moveAxis, 0f) && knockbackTimer <= 0f)
            {
                FacingLeft = moveAxis < 0f;
            }

            if (Noclip)
            {
                Vector3 step = new Vector3(input.MoveAxis, input.VerticalAxis, 0f) * (noclipSpeed * (input.RunHeld ? 2f : 1f) * Time.deltaTime);
                transform.position += step;
            }
        }

        private void FixedUpdate()
        {
            if (Noclip)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;
            jumpBufferTimer -= dt;
            knockbackTimer -= dt;

            IsGrounded = ReadGround();

            // O coyote vale pelo que restava no passo anterior: o aperto lido no
            // Update só é consumido aqui, até um passo depois.
            bool coyote = coyoteTimer > 0f;
            coyoteTimer = IsGrounded ? coyoteTime : coyoteTimer - dt;
            airTime = IsGrounded ? 0f : airTime + dt;
            Vector2 velocity = body.linearVelocity;
            bool jumpedNow = false;

            if (!IsGrounded)
            {
                lowestAirVelocity = Mathf.Min(lowestAirVelocity, velocity.y);
            }
            else if (!wasGrounded)
            {
                Landed?.Invoke(lowestAirVelocity);
                lowestAirVelocity = 0f;
                jumpRising = false;
            }

            if (knockbackTimer > 0f)
            {
                body.gravityScale = gravityScale;
                wasGrounded = IsGrounded;
                return;
            }

            if (jumpBufferTimer > 0f && (IsGrounded || coyote))
            {
                jumpBufferTimer = -1f;
                coyoteTimer = 0f;
                velocity.y = jumpVelocity;
                IsGrounded = false;
                airTime = 0f;
                jumpRising = true;
                jumpedNow = true;
                Jumped?.Invoke();
            }
            else if (jumpRising && velocity.y > 0f && !jumpHeld)
            {
                // Toque curto: corta a subida uma vez.
                velocity.y *= jumpCutFactor;
                jumpRising = false;
            }

            if (velocity.y <= 0f && !jumpedNow)
            {
                jumpRising = false;
            }

            float speed = moveAxis * (running ? runSpeed : walkSpeed);
            if (IsGrounded && !jumpedNow)
            {
                body.gravityScale = 0f;
                if (Mathf.Approximately(moveAxis, 0f))
                {
                    velocity = Vector2.zero;
                }
                else
                {
                    Vector2 tangent = new Vector2(groundNormal.y, -groundNormal.x);
                    velocity = tangent * speed;
                    TryStepUp();
                }
            }
            else
            {
                body.gravityScale = gravityScale;
                velocity.x = speed;
            }

            body.linearVelocity = velocity;

            // Saiu do chão sem pular (borda de degrau, crista de rampa, cuja
            // tangente ainda sobe): acompanha o chão se ele está perto.
            if (wasGrounded && !IsGrounded && !jumpedNow && !jumpRising)
            {
                SnapToGround();
            }

            wasGrounded = IsGrounded;
        }

        /// <summary>
        /// Chão = contato com normal para cima em que o corpo não se afasta da
        /// superfície. O contato do passo em que o impulso saiu tem o corpo
        /// subindo a ~15 u/s em relação a ele: não conta.
        /// </summary>
        private bool ReadGround()
        {
            int count = body.GetContacts(groundFilter, contacts);
            Vector2 velocity = body.linearVelocity;
            bool found = false;
            Vector2 best = Vector2.up;
            for (int i = 0; i < count; i++)
            {
                Vector2 normal = contacts[i].normal;
                if (Vector2.Dot(velocity, normal) > separatingSpeed)
                {
                    continue;
                }

                if (!found || normal.y > best.y)
                {
                    best = normal;
                    found = true;
                }
            }

            groundNormal = found ? best : Vector2.up;
            return found;
        }

        /// <summary>
        /// Saindo de um degrau ou da crista de uma rampa andando: desce até o
        /// chão se ele estiver a até uma célula. Nunca na subida de um pulo.
        /// </summary>
        private void SnapToGround()
        {
            int count = body.Cast(Vector2.down, groundFilter, hits, groundSnapDistance);
            float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].normal.y > 0.6f && hits[i].distance < nearest)
                {
                    nearest = hits[i].distance;
                }
            }

            if (nearest == float.MaxValue)
            {
                return;
            }

            float drop = Mathf.Max(0f, nearest - 0.01f);
            body.position += Vector2.down * drop;
            body.linearVelocity = new Vector2(body.linearVelocity.x, 0f);
            body.gravityScale = 0f;
            IsGrounded = true;
            coyoteTimer = coyoteTime;
            if (drop > 0.05f)
            {
                Stepped?.Invoke(-drop);
            }
        }

        /// <summary>
        /// Sobe um degrau de até <see cref="stepHeight"/>: há obstáculo à frente
        /// na altura dos pés e espaço para o corpo inteiro um degrau acima.
        /// Parede mais alta falha na segunda consulta e continua pedindo pulo.
        /// </summary>
        private void TryStepUp()
        {
            if (bodyCollider == null || stepHeight <= 0f || groundNormal.y < 0.95f)
            {
                return;
            }

            Bounds bounds = bodyCollider.bounds;
            float direction = Mathf.Sign(moveAxis);
            float front = direction > 0f ? bounds.max.x + (ProbeDepth * 0.5f) : bounds.min.x - (ProbeDepth * 0.5f);

            Vector2 feetCenter = new Vector2(front, bounds.min.y + (stepHeight * 0.5f) + 0.02f);
            Vector2 feetSize = new Vector2(ProbeDepth, stepHeight - 0.1f);
            if (!Blocked(feetCenter, feetSize))
            {
                return;
            }

            Vector2 raisedCenter = new Vector2(bounds.center.x + (direction * ProbeDepth), bounds.center.y + stepHeight + 0.02f);
            Vector2 raisedSize = new Vector2(bounds.size.x, bounds.size.y - 0.04f);
            if (Blocked(raisedCenter, raisedSize))
            {
                return;
            }

            // Sobe só o necessário: do alto, desce o corpo até encostar no degrau (meio-bloco sobe meia célula).
            float lift = stepHeight + 0.02f;
            int count = Physics2D.BoxCast(raisedCenter, raisedSize, 0f, Vector2.down, solidFilter, hits, lift);
            float landing = lift;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider != bodyCollider && hits[i].collider.attachedRigidbody != body && hits[i].normal.y > 0.6f)
                {
                    landing = Mathf.Min(landing, hits[i].distance);
                }
            }

            float rise = lift - landing + 0.01f;
            if (rise <= 0.02f)
            {
                return;
            }

            body.position += new Vector2(0f, rise);
            Stepped?.Invoke(rise);
        }

        private bool Blocked(Vector2 center, Vector2 size)
        {
            int count = Physics2D.OverlapBox(center, size, 0f, solidFilter, overlaps);
            for (int i = 0; i < count; i++)
            {
                if (overlaps[i] != bodyCollider && overlaps[i].attachedRigidbody != body)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
