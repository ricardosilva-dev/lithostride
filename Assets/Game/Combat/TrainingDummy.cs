using TMPro;
using UnityEngine;

namespace Lithostride.Combat
{
    /// <summary>
    /// Alvo de treino: um toco do pack com vida, que pisca ao apanhar, mostra
    /// vida e último dano, tomba ao zerar e volta sozinho depois de um tempo
    /// (ou pelo painel). Serve para testar cada ataque sem a luta do boss.
    /// </summary>
    public sealed class TrainingDummy : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TMP_Text label;
        [SerializeField, Min(0f)] private float restoreDelay = 2.5f;

        private float flash;
        private float lastDamage;
        private int hits;
        private float deadTimer = -1f;

        public void Configure(Health dummyHealth, SpriteRenderer renderer, TMP_Text text)
        {
            health = dummyHealth;
            spriteRenderer = renderer;
            label = text;
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
            flash = 0.15f;
            lastDamage = amount;
            hits++;
        }

        private void OnDied()
        {
            deadTimer = 0f;
        }

        private void Update()
        {
            if (spriteRenderer == null || health == null)
            {
                return;
            }

            if (deadTimer >= 0f)
            {
                deadTimer += Time.deltaTime;
                spriteRenderer.color = new Color(0.6f, 0.6f, 0.6f, 0.6f);
                if (deadTimer >= restoreDelay)
                {
                    ResetDummy();
                }
            }
            else
            {
                flash -= Time.deltaTime;
                spriteRenderer.color = flash > 0f ? new Color(1f, 0.5f, 0.5f, 1f) : Color.white;
            }

            if (label != null)
            {
                label.text = "Alvo " + Mathf.CeilToInt(health.Current) + "/" + Mathf.CeilToInt(health.Max) +
                             (hits > 0 ? "\núltimo: " + lastDamage.ToString("0") + "  acertos: " + hits : "");
            }
        }

        public void ResetDummy()
        {
            deadTimer = -1f;
            hits = 0;
            lastDamage = 0f;
            health.RestoreFull();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
        }
    }
}
