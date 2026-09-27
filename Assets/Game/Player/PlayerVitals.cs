using System;
using Lithostride.Combat;
using Lithostride.Core;
using UnityEngine;

namespace Lithostride.Player
{
    /// <summary>
    /// Reação do Kael à vida: pisca e recua ao levar dano (a invulnerabilidade
    /// fica na <see cref="Health"/>), e ao morrer perde o controle, some e
    /// renasce no ponto de retorno atual com vida cheia. Não há quadros de
    /// dano nem de morte do Kael no pack: o retorno visual é cor e opacidade.
    /// </summary>
    public sealed class PlayerVitals : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private PlayerController controller;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private GameInput input;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private Vector2 knockback = new Vector2(7f, 8f);
        [SerializeField, Min(0f)] private float respawnDelay = 1.4f;

        private Vector2 respawnPoint;
        private float flashTimer;
        private float deathTimer = -1f;

        public bool IsDead => deathTimer >= 0f;

        /// <summary>Renasceu (a luta do boss escuta para reiniciar).</summary>
        public event Action Respawned;

        public void Configure(Health playerHealth, PlayerController playerController, PlayerCombat playerCombat,
            SpriteRenderer renderer, GameInput gameInput, CameraFollow follow, Vector2 spawn)
        {
            health = playerHealth;
            controller = playerController;
            combat = playerCombat;
            spriteRenderer = renderer;
            input = gameInput;
            cameraFollow = follow;
            respawnPoint = spawn;
        }

        public void SetRespawnPoint(Vector2 point)
        {
            respawnPoint = point;
        }

        private void Awake()
        {
            if (respawnPoint == Vector2.zero)
            {
                respawnPoint = transform.position;
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
        }

        private void OnDamaged(float amount, Vector2 source)
        {
            flashTimer = 0.35f;
            if (health.IsDead)
            {
                return;
            }

            float away = transform.position.x >= source.x ? 1f : -1f;
            controller.Knockback(new Vector2(knockback.x * away, knockback.y), 0.25f);
        }

        private void OnDied()
        {
            deathTimer = 0f;
            controller.ControlEnabled = false;
            if (combat != null)
            {
                combat.CancelAttack();
            }
        }

        private void Update()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (deathTimer >= 0f)
            {
                deathTimer += Time.deltaTime;
                float t = Mathf.Clamp01(deathTimer / respawnDelay);
                spriteRenderer.color = new Color(1f, 0.45f, 0.45f, 1f - t);
                if (deathTimer >= respawnDelay)
                {
                    Respawn();
                }

                return;
            }

            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                bool on = Mathf.Repeat(flashTimer, 0.1f) > 0.05f;
                spriteRenderer.color = on ? new Color(1f, 0.55f, 0.55f, 1f) : Color.white;
            }
            else if (health != null && health.IsInvulnerable)
            {
                spriteRenderer.color = new Color(1f, 1f, 1f, Mathf.Repeat(Time.time, 0.16f) > 0.08f ? 1f : 0.6f);
            }
            else
            {
                spriteRenderer.color = Color.white;
            }
        }

        /// <summary>Renasce no ponto de retorno, com vida cheia.</summary>
        public void Respawn()
        {
            deathTimer = -1f;
            controller.Teleport(respawnPoint);
            controller.ControlEnabled = true;
            health.RestoreFull();
            spriteRenderer.color = Color.white;
            if (cameraFollow != null)
            {
                cameraFollow.Recenter();
            }

            Respawned?.Invoke();
        }

        /// <summary>Vida cheia sem morrer (painel de teste).</summary>
        public void Heal()
        {
            if (deathTimer < 0f)
            {
                health.RestoreFull();
            }
        }
    }
}
