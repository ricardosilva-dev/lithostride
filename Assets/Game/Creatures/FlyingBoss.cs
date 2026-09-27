using System;
using Lithostride.Combat;
using Lithostride.Player;
using UnityEngine;

namespace Lithostride.Creatures
{
    public enum BossState
    {
        Dormant,
        Hover,
        Telegraph,
        Dive,
        Recover,
        Aim,
        Shoot,
        Hurt,
        Falling,
        Dead
    }

    /// <summary>
    /// Boss voador do pack (dragão de cristal). Voa de verdade, dentro da
    /// área de voo da arena, alternando dois ataques anunciados:
    /// <list type="bullet">
    /// <item>mergulho: sobe e pisca (aviso), mergulha no ponto onde o jogador
    /// estava e fica um tempo embaixo, lento e vulnerável (dano x1,5) — a
    /// janela para a espada;</item>
    /// <item>disparo: abre a boca (aviso) e lança três cristais em leque.</item>
    /// </list>
    /// Pairando, mantém distância do jogador: não fica colado nem fora de
    /// alcance para sempre (o projétil do jogador e as plataformas cobrem o
    /// ar). Morto, cai, fica no chão como cadáver sem dano, e os projéteis
    /// somem. A hurtbox é do corpo (não das asas) e segue o pivô do desenho.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class FlyingBoss : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Health health;
        [SerializeField] private BossVisual visual;
        [SerializeField] private Collider2D hurtbox;
        [SerializeField] private ContactDamage contact;
        [SerializeField] private Transform player;
        [SerializeField] private PlayerVitals playerVitals;
        [SerializeField] private EffectRegistry registry;
        [SerializeField] private Sprite crystalSprite;
        [SerializeField] private Sprite[] crystalImpact;

        [SerializeField] private string displayName = "Dragão de Cristal";
        [SerializeField] private Rect arena;
        [SerializeField] private Rect flightArea;
        [SerializeField] private Vector2 home;
        [SerializeField] private float groundTop = 1f;

        /// <summary>Distância do centro do corpo até o chão quando ele está deitado (morto).</summary>
        [SerializeField] private float corpseHeight = 1.0f;

        [SerializeField, Min(0f)] private float hoverSpeed = 5f;
        [SerializeField, Min(0f)] private float diveSpeed = 15f;
        [SerializeField, Min(0f)] private float hoverTime = 2.0f;
        [SerializeField, Min(0f)] private float telegraphTime = 0.8f;
        [SerializeField, Min(0f)] private float recoverTime = 1.6f;
        [SerializeField, Min(0f)] private float aimTime = 0.6f;
        [SerializeField, Min(0f)] private float shootTime = 0.8f;
        [SerializeField, Min(0f)] private float bodyDamage = 10f;
        [SerializeField, Min(0f)] private float diveDamage = 22f;
        [SerializeField, Min(0f)] private float crystalDamage = 12f;
        [SerializeField, Min(0f)] private float crystalSpeed = 9.5f;

        private BossState state = BossState.Dormant;
        private float stateTimer;
        private int attackCount;
        private Vector2 target;
        private Vector2 velocity;
        private bool shot;

        public BossState State => state;
        public string DisplayName => displayName;
        public Health Health => health;

        /// <summary>Luta em andamento (barra de vida visível).</summary>
        public bool Engaged => state != BossState.Dormant;

        public event Action<BossState> StateChanged;

        public void Configure(Rigidbody2D rigidbody, Health bossHealth, BossVisual bossVisual, Collider2D bodyHurtbox,
            ContactDamage contactDamage, Transform playerTransform, PlayerVitals vitals, EffectRegistry effects, Sprite crystal,
            Sprite[] impact, Rect arenaArea, Rect flight, Vector2 homePoint, float floorTop)
        {
            body = rigidbody;
            health = bossHealth;
            visual = bossVisual;
            hurtbox = bodyHurtbox;
            contact = contactDamage;
            player = playerTransform;
            playerVitals = vitals;
            registry = effects;
            crystalSprite = crystal;
            crystalImpact = impact;
            arena = arenaArea;
            flightArea = flight;
            home = homePoint;
            groundTop = floorTop;
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            if (playerVitals != null)
            {
                playerVitals.Respawned += OnPlayerRespawned;
            }
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            if (playerVitals != null)
            {
                playerVitals.Respawned -= OnPlayerRespawned;
            }
        }

        private void Start()
        {
            ResetFight();
        }

        /// <summary>Volta ao começo: vida cheia, em casa, esperando o jogador entrar na arena.</summary>
        public void ResetFight()
        {
            health.RestoreFull();
            body.position = home;
            transform.position = home;
            velocity = Vector2.zero;
            attackCount = 0;
            if (registry != null)
            {
                registry.ClearAll();
            }

            hurtbox.enabled = true;
            contact.Active = false;
            visual.SetTint(Color.white);
            Enter(BossState.Dormant);
        }

        private void OnPlayerRespawned()
        {
            if (state != BossState.Dead && state != BossState.Falling)
            {
                ResetFight();
            }
        }

        private void OnDamaged(float amount, Vector2 source)
        {
            if (state == BossState.Hover || state == BossState.Aim)
            {
                Enter(BossState.Hurt);
            }
        }

        private void OnDied()
        {
            hurtbox.enabled = false;
            contact.Active = false;
            if (registry != null)
            {
                registry.ClearAll();
            }

            Enter(BossState.Falling);
        }

        private void Enter(BossState next)
        {
            state = next;
            stateTimer = 0f;
            shot = false;
            health.DamageMultiplier = next == BossState.Recover ? 1.5f : 1f;
            contact.Active = next != BossState.Dormant && next != BossState.Falling && next != BossState.Dead;
            contact.Damage = next == BossState.Dive ? diveDamage : bodyDamage;

            switch (next)
            {
                case BossState.Dormant:
                case BossState.Hover:
                    visual.Play(BossRow.Hover, 0, -1, true, 9f);
                    break;
                case BossState.Telegraph:
                    visual.Play(BossRow.Dive, 0, 1, true, 6f);
                    break;
                case BossState.Dive:
                    visual.Play(BossRow.Dive, 2, 3, true, 10f);
                    break;
                case BossState.Recover:
                    visual.Play(BossRow.Dive, 4, 5, false, 5f);
                    break;
                case BossState.Aim:
                    visual.Play(BossRow.Attack, 0, 1, false, 4f);
                    break;
                case BossState.Shoot:
                    visual.Play(BossRow.Attack, 2, -1, false, 8f);
                    break;
                case BossState.Hurt:
                    visual.Play(BossRow.React, 0, 2, false, 14f);
                    break;
                case BossState.Falling:
                    visual.Play(BossRow.React, 3, 4, false, 4f);
                    break;
                case BossState.Dead:
                    visual.Play(BossRow.React, 5, 5, false, 1f);
                    break;
            }

            StateChanged?.Invoke(next);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            stateTimer += dt;
            Vector2 position = body.position;
            Vector2 playerPosition = player != null ? (Vector2)player.position + new Vector2(0f, 1.35f) : position;
            bool playerAlive = playerVitals == null || !playerVitals.IsDead;

            switch (state)
            {
                case BossState.Dormant:
                    if (playerAlive && arena.Contains(playerPosition))
                    {
                        Enter(BossState.Hover);
                    }

                    MoveToward(home, hoverSpeed * 0.5f, dt);
                    break;

                case BossState.Hover:
                case BossState.Hurt:
                {
                    // Fica ao lado e acima do jogador, a uma distância de desvio.
                    float side = position.x >= playerPosition.x ? 1f : -1f;
                    Vector2 goal = ClampToFlight(new Vector2(playerPosition.x + (side * 5.5f), playerPosition.y + 6f));
                    MoveToward(goal, hoverSpeed, dt);
                    if (state == BossState.Hurt && stateTimer >= 0.25f)
                    {
                        Enter(BossState.Hover);
                    }
                    else if (state == BossState.Hover && stateTimer >= hoverTime && playerAlive)
                    {
                        attackCount++;
                        // Alterna: mergulho, disparo, mergulho...
                        Enter(attackCount % 2 == 1 ? BossState.Telegraph : BossState.Aim);
                    }

                    break;
                }

                case BossState.Telegraph:
                    // Sobe um pouco e pisca: aviso do mergulho.
                    MoveToward(ClampToFlight(position + new Vector2(0f, 1.7f)), 2.1f, dt);
                    visual.SetTint(Mathf.Repeat(stateTimer, 0.2f) > 0.1f ? new Color(0.6f, 0.9f, 1f) : Color.white);
                    if (stateTimer >= telegraphTime)
                    {
                        visual.SetTint(Color.white);
                        target = new Vector2(Mathf.Clamp(playerPosition.x, flightArea.xMin, flightArea.xMax),
                            Mathf.Max(groundTop + 1.5f, playerPosition.y));
                        Enter(BossState.Dive);
                    }

                    break;

                case BossState.Dive:
                    MoveToward(target, diveSpeed, dt);
                    if (Vector2.Distance(body.position, target) < 0.3f || stateTimer > 1.6f)
                    {
                        Enter(BossState.Recover);
                    }

                    break;

                case BossState.Recover:
                    // Embaixo, devagar e vulnerável: a janela da espada.
                    visual.SetTint(new Color(1f, 0.85f, 0.7f));
                    MoveToward(new Vector2(position.x, groundTop + 1.85f), 1.25f, dt);
                    if (stateTimer >= recoverTime)
                    {
                        visual.SetTint(Color.white);
                        Enter(BossState.Hover);
                    }

                    break;

                case BossState.Aim:
                    MoveToward(ClampToFlight(position), 1f, dt);
                    visual.SetTint(Mathf.Repeat(stateTimer, 0.16f) > 0.08f ? new Color(0.7f, 0.95f, 1f) : Color.white);
                    if (stateTimer >= aimTime)
                    {
                        visual.SetTint(Color.white);
                        Enter(BossState.Shoot);
                    }

                    break;

                case BossState.Shoot:
                    if (!shot && stateTimer >= 0.1f)
                    {
                        shot = true;
                        FireCrystals(playerPosition);
                    }

                    if (stateTimer >= shootTime)
                    {
                        Enter(BossState.Hover);
                    }

                    break;

                case BossState.Falling:
                {
                    velocity.y -= 30f * dt;
                    velocity.x = Mathf.MoveTowards(velocity.x, 0f, 6f * dt);
                    Vector2 next = position + (velocity * dt);
                    float rest = groundTop + corpseHeight;
                    if (next.y <= rest)
                    {
                        next.y = rest;
                        body.position = next;
                        Enter(BossState.Dead);
                        return;
                    }

                    body.MovePosition(next);
                    return;
                }

                case BossState.Dead:
                    return;
            }

            if (player != null && state != BossState.Dive)
            {
                visual.SetFacingLeft(playerPosition.x < body.position.x);
            }
        }

        private void MoveToward(Vector2 goal, float speed, float dt)
        {
            Vector2 next = Vector2.MoveTowards(body.position, goal, speed * dt);
            velocity = (next - body.position) / Mathf.Max(dt, 0.0001f);
            body.MovePosition(next);
        }

        private Vector2 ClampToFlight(Vector2 point)
        {
            return new Vector2(Mathf.Clamp(point.x, flightArea.xMin, flightArea.xMax),
                Mathf.Clamp(point.y, flightArea.yMin, flightArea.yMax));
        }

        /// <summary>Três cristais em leque da boca para o jogador.</summary>
        private void FireCrystals(Vector2 playerPosition)
        {
            bool left = playerPosition.x < body.position.x;
            Vector2 mouth = body.position + new Vector2(left ? -2.7f : 2.7f, 0.75f);
            Vector2 aim = (playerPosition - mouth).normalized;
            for (int i = -1; i <= 1; i++)
            {
                Vector2 direction = Quaternion.Euler(0f, 0f, i * 12f) * aim;
                GameObject shotObject = new GameObject("Cristal do boss");
                SpriteRenderer renderer = shotObject.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 18;
                shotObject.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, Mathf.Abs(direction.x)) * Mathf.Rad2Deg * (direction.x < 0f ? -1f : 1f));
                shotObject.AddComponent<Projectile>().Launch(Team.Enemy, body, mouth, direction, crystalSpeed, crystalDamage, 0.3f,
                    2.5f, new[] { crystalSprite }, null, crystalImpact, 0.4f, registry, false);
                if (registry != null)
                {
                    registry.Track(shotObject);
                }
            }
        }
    }
}
