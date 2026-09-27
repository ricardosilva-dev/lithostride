using System;
using UnityEngine;

namespace Lithostride.Combat
{
    /// <summary>
    /// Vida, invulnerabilidade após dano e multiplicador de dano. Só guarda o
    /// estado e avisa por eventos; quem reage (piscar, recuar, morrer) é outro
    /// componente.
    /// </summary>
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        /// <summary>Tempo sem receber dano depois de um acerto (evita dano por quadro).</summary>
        [SerializeField, Min(0f)] private float invulnerabilityTime = 0.6f;

        private float current;
        private float invulnerableUntil = -1f;
        private bool initialized;

        public float Max => maxHealth;
        public float Current => EnsureInit();
        public float Fraction => Mathf.Clamp01(EnsureInit() / maxHealth);
        public bool IsDead => EnsureInit() <= 0f;
        public bool IsInvulnerable => Time.time < invulnerableUntil;

        /// <summary>Dano recebido é multiplicado por isto (janela de vulnerabilidade do boss).</summary>
        public float DamageMultiplier { get; set; } = 1f;

        /// <summary>Recebeu dano: (quantidade, posição de onde veio).</summary>
        public event Action<float, Vector2> Damaged;

        public event Action Died;

        public event Action Restored;

        public void Configure(float max, float invulnerability)
        {
            maxHealth = max;
            invulnerabilityTime = invulnerability;
            current = max;
            initialized = true;
        }

        private float EnsureInit()
        {
            if (!initialized)
            {
                current = maxHealth;
                initialized = true;
            }

            return current;
        }

        /// <summary>Aplica dano; falso se morto ou invulnerável.</summary>
        public bool TakeDamage(float amount, Vector2 source)
        {
            EnsureInit();
            if (current <= 0f || IsInvulnerable || amount <= 0f)
            {
                return false;
            }

            current = Mathf.Max(0f, current - (amount * DamageMultiplier));
            invulnerableUntil = Time.time + invulnerabilityTime;
            Damaged?.Invoke(amount * DamageMultiplier, source);
            if (current <= 0f)
            {
                Died?.Invoke();
            }

            return true;
        }

        /// <summary>Ferramenta de teste: põe a vida num valor exato (0 mata).</summary>
        public void SetCurrent(float value)
        {
            EnsureInit();
            bool wasAlive = current > 0f;
            current = Mathf.Clamp(value, 0f, maxHealth);
            if (wasAlive && current <= 0f)
            {
                Died?.Invoke();
            }
        }

        public void RestoreFull()
        {
            current = maxHealth;
            initialized = true;
            invulnerableUntil = -1f;
            DamageMultiplier = 1f;
            Restored?.Invoke();
        }
    }
}
