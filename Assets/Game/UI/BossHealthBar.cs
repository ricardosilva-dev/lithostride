using Lithostride.Creatures;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lithostride.UI
{
    /// <summary>
    /// Barra de vida do boss com a arte do pack: moldura fixa (a barra vazia,
    /// com o canal escuro), preenchimento recortado dentro do canal pelo
    /// valor real (vida atual / máxima, contínuo, sem esticar nem encolher a
    /// moldura) e o brilho da ponta acompanhando o fim do preenchimento.
    /// Aparece com a luta e some depois da morte.
    /// </summary>
    public sealed class BossHealthBar : MonoBehaviour
    {
        [SerializeField] private FlyingBoss boss;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image fill;
        [SerializeField] private RectTransform glow;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text value;
        [SerializeField, Min(0f)] private float easeSpeed = 6f;
        [SerializeField, Min(0f)] private float hideAfterDeath = 3f;

        private float shown = 1f;
        private float deadTimer;

        public void Configure(FlyingBoss bossToShow, CanvasGroup canvasGroup, Image fillImage, RectTransform glowTransform,
            TMP_Text titleText, TMP_Text valueText)
        {
            boss = bossToShow;
            group = canvasGroup;
            fill = fillImage;
            glow = glowTransform;
            title = titleText;
            value = valueText;
        }

        private void Update()
        {
            if (boss == null || boss.Health == null)
            {
                return;
            }

            float real = boss.Health.Fraction;
            bool dead = boss.State == BossState.Dead || boss.State == BossState.Falling;
            deadTimer = dead ? deadTimer + Time.deltaTime : 0f;
            bool visible = boss.Engaged && deadTimer < hideAfterDeath;
            group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, Time.unscaledDeltaTime * 4f);

            // A barra corre até o valor real (a leitura numérica é sempre o valor exato).
            shown = real > shown ? real : Mathf.MoveTowards(shown, real, Time.deltaTime * easeSpeed * Mathf.Max(0.05f, shown - real + 0.05f));
            fill.fillAmount = shown;

            if (glow != null)
            {
                RectTransform fillRect = fill.rectTransform;
                glow.gameObject.SetActive(shown > 0.005f && shown < 0.995f);
                glow.anchoredPosition = new Vector2(fillRect.anchoredPosition.x - (fillRect.rect.width * fillRect.pivot.x) +
                                                    (fillRect.rect.width * shown), fillRect.anchoredPosition.y);
            }

            if (title != null)
            {
                title.text = boss.DisplayName;
            }

            if (value != null)
            {
                value.text = Mathf.CeilToInt(boss.Health.Current) + " / " + Mathf.CeilToInt(boss.Health.Max);
            }
        }
    }
}
