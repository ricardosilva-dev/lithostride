using Lithostride.Combat;
using Lithostride.Core;
using UnityEngine;

namespace Lithostride.Player
{
    /// <summary>
    /// Espada de cristal na mão do Kael, no modo combate.
    ///
    /// O Kael não tem quadros próprios com espada: cada quadro traz a mão da
    /// espada medida (a direita, do lado de trás do corpo quando ele olha
    /// para a direita) e o ângulo da lâmina. A espada é uma das variantes
    /// derivadas já girada nesse ângulo, com o pivô no cabo; fica atrás do
    /// corpo (ordem de desenho menor), então os dedos do próprio quadro
    /// cobrem o cabo, e a lâmina nunca passa por cima do rosto ou da capa.
    ///
    /// Espaços de coordenadas: a espada é filha do desenho (que já está na
    /// grade de pixels da tela, <see cref="PixelSnap"/>); a âncora vem em
    /// unidades locais, já espelhada pelo animador; o cabo vai no centro do
    /// pixel lógico da mão, na mesma grade do quadro. O espelhamento é um só:
    /// flipX da espada, em volta do próprio cabo. Roda depois do animador
    /// (Update) e do PixelSnap (LateUpdate, ordem 100).
    ///
    /// Durante um golpe cujo efeito já desenha a espada, esta some: nunca duas espadas.
    /// </summary>
    [DefaultExecutionOrder(110)]
    public sealed class SwordHolder : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer swordRenderer;
        [SerializeField] private PlayerAnimator animator;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerToolMode toolMode;
        [SerializeField] private PlayerVitals vitals;

        /// <summary>Variantes da espada empunhada e o ângulo (graus) de cada uma.</summary>
        [SerializeField] private Sprite[] variants;
        [SerializeField] private int[] variantAngles;

        public void Configure(SpriteRenderer renderer, PlayerAnimator playerAnimator, PlayerCombat playerCombat,
            PlayerToolMode mode, PlayerVitals playerVitals, Sprite[] swordVariants, int[] swordAngles)
        {
            swordRenderer = renderer;
            animator = playerAnimator;
            combat = playerCombat;
            toolMode = mode;
            vitals = playerVitals;
            variants = swordVariants;
            variantAngles = swordAngles;
        }

        /// <summary>Posição local do cabo (unidades) para uma âncora de mão: centro do pixel lógico da mão.</summary>
        public static Vector3 GripPosition(Vector2 hand)
        {
            return new Vector3(WorldScale.PixelCenter(hand.x), WorldScale.PixelCenter(hand.y), 0f);
        }

        private void LateUpdate()
        {
            if (swordRenderer == null || animator == null)
            {
                return;
            }

            bool visible = toolMode != null && toolMode.Mode == ToolMode.Combat && (combat == null || !combat.CurrentShowsSword) &&
                           (vitals == null || !vitals.IsDead);
            swordRenderer.enabled = visible;
            if (!visible)
            {
                return;
            }

            swordRenderer.sprite = Variant(animator.SwordAngle);
            swordRenderer.flipX = animator.FacingLeft;
            swordRenderer.transform.localPosition = GripPosition(animator.HandAnchor);
        }

        private Sprite Variant(int angle)
        {
            if (variants == null || variants.Length == 0)
            {
                return swordRenderer.sprite;
            }

            int best = 0;
            for (int i = 1; i < variants.Length; i++)
            {
                if (Mathf.Abs(variantAngles[i] - angle) < Mathf.Abs(variantAngles[best] - angle))
                {
                    best = i;
                }
            }

            return variants[best];
        }
    }
}
