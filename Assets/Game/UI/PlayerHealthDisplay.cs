using Lithostride.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lithostride.UI
{
    /// <summary>Vida do jogador: texto e barra simples (o pack só tem a barra do boss).</summary>
    public sealed class PlayerHealthDisplay : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text label;

        public void Configure(Health playerHealth, Image fillImage, TMP_Text text)
        {
            health = playerHealth;
            fill = fillImage;
            label = text;
        }

        private void Update()
        {
            if (health == null)
            {
                return;
            }

            fill.fillAmount = health.Fraction;
            label.text = "Kael  " + Mathf.CeilToInt(health.Current) + " / " + Mathf.CeilToInt(health.Max);
        }
    }
}
