using System;
using UnityEngine;

namespace Lithostride.Player
{
    /// <summary>
    /// Uma sequência de quadros do Kael com os dados medidos de cada quadro:
    /// a mão da espada (unidades relativas ao pivô, virado à direita), se o
    /// punho aparece no quadro e o ângulo da lâmina. Vem do manifesto do
    /// pipeline de arte, preenchida pelo construtor da cena.
    /// </summary>
    [Serializable]
    public sealed class KaelSequence
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private Vector2[] hands;
        [SerializeField] private bool[] handVisible;
        [SerializeField] private int[] swordAngles;

        public KaelSequence(Sprite[] frames, Vector2[] hands, bool[] handVisible, int[] swordAngles)
        {
            this.frames = frames;
            this.hands = hands;
            this.handVisible = handVisible;
            this.swordAngles = swordAngles;
        }

        /// <summary>Para o serializador da Unity.</summary>
        public KaelSequence()
        {
        }

        public int Count => frames == null ? 0 : frames.Length;

        public Sprite Frame(int index)
        {
            return frames[index];
        }

        public Vector2 Hand(int index)
        {
            return hands != null && index < hands.Length ? hands[index] : Vector2.zero;
        }

        public bool HandVisible(int index)
        {
            return handVisible == null || index >= handVisible.Length || handVisible[index];
        }

        public int SwordAngle(int index)
        {
            return swordAngles != null && index < swordAngles.Length ? swordAngles[index] : 55;
        }
    }
}
