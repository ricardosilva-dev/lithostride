using System.Collections.Generic;
using Lithostride.Core;
using Lithostride.Player;
using UnityEngine;

namespace Lithostride.Combat
{
    /// <summary>
    /// Ataques do Kael no modo combate. Clique esquerdo: golpe principal (Q/E
    /// alterna entre corte, corte energizado e golpe descendente); clique
    /// direito: projétil de cristal, que explode no impacto. O painel de
    /// teste dispara qualquer variante, inclusive a explosão sozinha.
    ///
    /// Cada golpe tem antecipação, janela ativa e recuperação tiradas dos
    /// quadros do efeito, e depois recarga. Dentro de um golpe, cada alvo é
    /// acertado uma vez (salvo ataque com intervalo de multi-acerto).
    /// </summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private GameInput input;
        [SerializeField] private PlayerController controller;
        [SerializeField] private PlayerAnimator animator;
        [SerializeField] private PlayerToolMode toolMode;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private EffectRegistry registry;
        [SerializeField] private AttackDefinition[] attacks;

        /// <summary>Índices dos golpes do clique esquerdo, na ordem do Q/E.</summary>
        [SerializeField] private int[] primaryCycle = { 0, 1, 2 };

        [SerializeField] private int secondaryAttack = 3;

        /// <summary>Quadros da explosão usados no impacto do projétil.</summary>
        [SerializeField] private Sprite[] impactFrames;

        [SerializeField, Min(0.1f)] private float impactScale = 0.6f;
        [SerializeField] private int effectSortingOrder = 20;

        private readonly Collider2D[] overlaps = new Collider2D[16];
        private readonly Dictionary<Health, float> struck = new Dictionary<Health, float>();
        private ContactFilter2D hitFilter;

        private int selected;
        private int currentAttack = -1;
        private float elapsed;
        private float cooldownUntil;
        private bool facingLeft;
        private bool projectileSpawned;
        private Vector2 burstCenter;

        public bool IsAttacking => currentAttack >= 0;

        /// <summary>O golpe atual já desenha a espada (a empunhada deve sumir).</summary>
        public bool CurrentShowsSword => currentAttack >= 0 && attacks[currentAttack].ContainsSword;

        public string SelectedName => attacks != null && attacks.Length > 0 ? attacks[primaryCycle[selected]].Name : "";
        public int AttackCount => attacks == null ? 0 : attacks.Length;

        public string AttackName(int index)
        {
            return attacks != null && index >= 0 && index < attacks.Length ? attacks[index].Name : "";
        }

        public void Configure(GameInput gameInput, PlayerController playerController, PlayerAnimator playerAnimator,
            PlayerToolMode mode, Rigidbody2D rigidbody, EffectRegistry effects, AttackDefinition[] definitions, Sprite[] impact)
        {
            input = gameInput;
            controller = playerController;
            animator = playerAnimator;
            toolMode = mode;
            body = rigidbody;
            registry = effects;
            attacks = definitions;
            impactFrames = impact;
        }

        private void Awake()
        {
            hitFilter = new ContactFilter2D { useTriggers = true };
        }

        private void Update()
        {
            if (input != null && toolMode != null && toolMode.Mode == ToolMode.Combat && controller.ControlEnabled)
            {
                if (input.Cycle != 0 && primaryCycle.Length > 0)
                {
                    selected = (selected + input.Cycle + primaryCycle.Length) % primaryCycle.Length;
                }

                if (input.PrimaryPressed)
                {
                    TriggerAttack(primaryCycle[selected]);
                }
                else if (input.SecondaryPressed)
                {
                    TriggerAttack(secondaryAttack);
                }
            }

            if (currentAttack < 0)
            {
                return;
            }

            elapsed += Time.deltaTime;
            AttackDefinition attack = attacks[currentAttack];
            if (attack.Kind == AttackKind.Projectile && !projectileSpawned && elapsed >= attack.ActiveStart)
            {
                SpawnProjectile(attack);
            }

            if (elapsed >= attack.Duration)
            {
                currentAttack = -1;
                cooldownUntil = Time.time + attack.Cooldown;
            }
        }

        private void FixedUpdate()
        {
            if (currentAttack < 0)
            {
                return;
            }

            AttackDefinition attack = attacks[currentAttack];
            if (attack.Kind == AttackKind.Projectile || elapsed < attack.ActiveStart || elapsed >= attack.ActiveEnd)
            {
                return;
            }

            Rect box = HitboxRect(attack);
            int count = Physics2D.OverlapBox(box.center, box.size, 0f, hitFilter, overlaps);
            for (int i = 0; i < count; i++)
            {
                Hurtbox hurtbox = overlaps[i].GetComponent<Hurtbox>();
                if (hurtbox == null || hurtbox.Team == Team.Player || hurtbox.Health == null || hurtbox.Health.IsDead)
                {
                    continue;
                }

                Health target = hurtbox.Health;
                if (struck.TryGetValue(target, out float last) &&
                    (attack.MultiHitInterval <= 0f || Time.time - last < attack.MultiHitInterval))
                {
                    continue;
                }

                struck[target] = Time.time;
                target.TakeDamage(attack.Damage, body.position);
            }
        }

        /// <summary>Começa um ataque (clique ou painel). Falso se já atacando ou em recarga.</summary>
        public bool TriggerAttack(int index)
        {
            if (attacks == null || index < 0 || index >= attacks.Length || currentAttack >= 0 || Time.time < cooldownUntil)
            {
                return false;
            }

            AttackDefinition attack = attacks[index];
            currentAttack = index;
            elapsed = 0f;
            projectileSpawned = false;
            struck.Clear();
            facingLeft = controller.FacingLeft;
            float sign = facingLeft ? -1f : 1f;

            GameObject effect = new GameObject("Efeito " + attack.Name);
            SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = effectSortingOrder;
            renderer.flipX = attack.SheetFacesLeft ? !facingLeft : facingLeft;

            Sprite[] frames = attack.Frames;
            int[] sorting = null;
            if (attack.Kind == AttackKind.Melee)
            {
                // Preso ao desenho do Kael (mesma grade de pixels), com o cabo
                // do quadro — o pivô — no pixel da mão do quadro atual.
                Transform parent = animator != null ? animator.transform : transform;
                SpriteRenderer body = animator != null ? animator.BodyRenderer : null;
                effect.transform.SetParent(body != null ? body.transform : parent, false);
                Vector2 hand = animator != null ? animator.HandAnchor : Vector2.zero;
                effect.transform.localPosition = SwordHolder.GripPosition(hand + new Vector2(attack.EffectOffset.x * sign, attack.EffectOffset.y));
                sorting = new int[frames.Length];
                int bodyOrder = body != null ? body.sortingOrder : effectSortingOrder;
                for (int i = 0; i < sorting.Length; i++)
                {
                    sorting[i] = attack.BehindBody(i) ? bodyOrder - 2 : effectSortingOrder;
                }
            }
            else if (attack.Kind == AttackKind.Burst)
            {
                burstCenter = body.position + new Vector2(attack.EffectOffset.x * sign, attack.EffectOffset.y);
                effect.transform.position = burstCenter;
            }
            else
            {
                // Projétil: na mão só o clarão de saída; o voo é do próprio projétil.
                Vector2 hand = animator != null ? animator.HandAnchor : Vector2.zero;
                effect.transform.position = body.position + hand + new Vector2(attack.EffectOffset.x * sign, attack.EffectOffset.y);
                frames = new[] { attack.Frames[0] };
            }

            effect.AddComponent<SpriteSequence>().Play(frames, attack.FramesPerSecond, false, true, 0, sorting);
            if (registry != null)
            {
                registry.Track(effect);
            }

            return true;
        }

        private void SpawnProjectile(AttackDefinition attack)
        {
            projectileSpawned = true;
            float sign = facingLeft ? -1f : 1f;
            Vector2 hand = animator != null ? animator.HandAnchor : Vector2.zero;
            Vector2 start = body.position + hand + new Vector2(attack.EffectOffset.x * sign, attack.EffectOffset.y);

            List<Sprite> flight = new List<Sprite>();
            List<Sprite> fade = new List<Sprite>();
            for (int i = 0; i < attack.Frames.Length; i++)
            {
                if (i >= attack.FlightFirst && i <= attack.FlightLast) flight.Add(attack.Frames[i]);
                else if (i > attack.FlightLast) fade.Add(attack.Frames[i]);
            }

            GameObject shot = new GameObject("Projétil");
            SpriteRenderer renderer = shot.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = effectSortingOrder;
            shot.AddComponent<Projectile>().Launch(Team.Player, body, start, new Vector2(sign, 0f), attack.ProjectileSpeed,
                attack.Damage, attack.ProjectileRadius, attack.ProjectileLifetime, flight.ToArray(), fade.ToArray(), impactFrames,
                impactScale, registry, false);
            if (registry != null)
            {
                registry.Track(shot);
            }
        }

        /// <summary>Hitbox do golpe atual em coordenadas do mundo (para o dano e para a sobreposição de depuração).</summary>
        public Rect HitboxRect(AttackDefinition attack)
        {
            float sign = facingLeft ? -1f : 1f;
            Vector2 origin = attack.Kind == AttackKind.Burst ? burstCenter : body.position;
            Vector2 center = origin + new Vector2(attack.HitboxOffset.x * sign, attack.HitboxOffset.y);
            return new Rect(center - (attack.HitboxSize * 0.5f), attack.HitboxSize);
        }

        public bool TryGetActiveHitbox(out Rect rect)
        {
            rect = default;
            if (currentAttack < 0)
            {
                return false;
            }

            AttackDefinition attack = attacks[currentAttack];
            if (attack.Kind == AttackKind.Projectile || elapsed < attack.ActiveStart || elapsed >= attack.ActiveEnd)
            {
                return false;
            }

            rect = HitboxRect(attack);
            return true;
        }

        /// <summary>Interrompe o golpe atual (morte, reset).</summary>
        public void CancelAttack()
        {
            currentAttack = -1;
            struck.Clear();
        }
    }
}
