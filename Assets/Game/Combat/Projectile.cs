using System.Collections.Generic;
using UnityEngine;

namespace Lithostride.Combat
{
    /// <summary>
    /// Projétil: anda em linha reta, e a cada passo de física varre o trecho
    /// percorrido com um círculo (CircleCast) — não atravessa alvo nem parede
    /// mesmo rápido. Acerta cada alvo uma vez, bate no terreno, e no impacto
    /// ou no fim da duração toca a dissipação e some.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private readonly RaycastHit2D[] hits = new RaycastHit2D[16];
        private readonly HashSet<Health> struck = new HashSet<Health>();
        private ContactFilter2D filter;

        private Team team;
        private Vector2 velocity;
        private float damage;
        private float radius;
        private float lifetime;
        private float age;
        private bool pierce;
        private Sprite[] flight;
        private Sprite[] fade;
        private Sprite[] impactFrames;
        private float impactScale;
        private EffectRegistry registry;
        private Rigidbody2D ownerBody;
        private float frameRate = 14f;
        private bool fading;
        private float fadeTimer;

        public void Launch(Team side, Rigidbody2D owner, Vector2 start, Vector2 direction, float speed, float hitDamage, float hitRadius,
            float duration, Sprite[] flightFrames, Sprite[] fadeFrames, Sprite[] impact, float impactSize, EffectRegistry effects,
            bool piercing)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            team = side;
            ownerBody = owner;
            transform.position = start;
            velocity = direction.normalized * speed;
            damage = hitDamage;
            radius = hitRadius;
            lifetime = duration;
            flight = flightFrames;
            fade = fadeFrames;
            impactFrames = impact;
            impactScale = impactSize;
            registry = effects;
            pierce = piercing;
            spriteRenderer.flipX = direction.x < 0f;
            // Sem máscara de camada; gatilhos incluídos (as hurtboxes são gatilhos).
            filter = new ContactFilter2D { useTriggers = true };
            if (flight != null && flight.Length > 0)
            {
                spriteRenderer.sprite = flight[0];
            }
        }

        private void Update()
        {
            if (fading)
            {
                fadeTimer += Time.deltaTime;
                int index = (int)(fadeTimer * frameRate);
                if (fade == null || index >= fade.Length)
                {
                    Destroy(gameObject);
                    return;
                }

                spriteRenderer.sprite = fade[index];
                return;
            }

            if (flight != null && flight.Length > 0)
            {
                spriteRenderer.sprite = flight[(int)(age * frameRate) % flight.Length];
            }
        }

        private void FixedUpdate()
        {
            if (fading)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;
            age += dt;
            Vector2 position = transform.position;
            Vector2 step = velocity * dt;
            int count = Physics2D.CircleCast(position, radius, step.normalized, filter, hits, step.magnitude);
            SortByDistance(count);

            for (int i = 0; i < count; i++)
            {
                Collider2D other = hits[i].collider;
                if (other == null || (ownerBody != null && other.attachedRigidbody == ownerBody))
                {
                    continue;
                }

                Hurtbox hurtbox = other.GetComponent<Hurtbox>();
                if (hurtbox != null)
                {
                    if (hurtbox.Team == team || hurtbox.Health == null || hurtbox.Health.IsDead || struck.Contains(hurtbox.Health))
                    {
                        continue;
                    }

                    struck.Add(hurtbox.Health);
                    hurtbox.Health.TakeDamage(damage, hits[i].point);
                    if (!pierce)
                    {
                        Impact(hits[i].centroid);
                        return;
                    }

                    continue;
                }

                if (!other.isTrigger)
                {
                    Impact(hits[i].centroid);
                    return;
                }
            }

            transform.position = position + step;
            if (age >= lifetime)
            {
                Impact(transform.position);
            }
        }

        private void SortByDistance(int count)
        {
            for (int i = 1; i < count; i++)
            {
                RaycastHit2D key = hits[i];
                int j = i - 1;
                while (j >= 0 && hits[j].distance > key.distance)
                {
                    hits[j + 1] = hits[j];
                    j--;
                }

                hits[j + 1] = key;
            }
        }

        private void Impact(Vector2 point)
        {
            fading = true;
            fadeTimer = 0f;
            transform.position = point;
            if (impactFrames != null && impactFrames.Length > 0)
            {
                GameObject burst = new GameObject("Impacto");
                burst.transform.position = point;
                burst.transform.localScale = Vector3.one * impactScale;
                SpriteRenderer renderer = burst.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = spriteRenderer.sortingOrder;
                burst.AddComponent<SpriteSequence>().Play(impactFrames, 18f, false, true);
                if (registry != null)
                {
                    registry.Track(burst);
                }
            }
        }
    }
}
