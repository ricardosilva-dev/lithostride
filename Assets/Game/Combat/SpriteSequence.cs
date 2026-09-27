using UnityEngine;

namespace Lithostride.Combat
{
    /// <summary>
    /// Toca uma sequência de sprites (efeito, projétil) e, no fim, some ou se
    /// destrói. Usa tempo de jogo: pausa congela junto.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteSequence : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(0.1f)] private float framesPerSecond = 16f;
        [SerializeField] private bool loop;
        [SerializeField] private bool destroyAtEnd = true;
        [SerializeField] private int loopFirst;

        /// <summary>Ordem de desenho por quadro (opcional): um golpe pode passar para trás do corpo em alguns quadros.</summary>
        [SerializeField] private int[] sortingOrders;

        private float timer;

        public int FrameIndex { get; private set; }
        public bool Finished { get; private set; }

        public void Play(Sprite[] sequence, float fps, bool looping, bool destroyWhenDone, int firstLoopFrame = 0, int[] frameSortingOrders = null)
        {
            sortingOrders = frameSortingOrders;
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            frames = sequence;
            framesPerSecond = fps;
            loop = looping;
            destroyAtEnd = destroyWhenDone;
            loopFirst = firstLoopFrame;
            timer = 0f;
            Finished = false;
            FrameIndex = 0;
            spriteRenderer.enabled = true;
            if (frames != null && frames.Length > 0)
            {
                spriteRenderer.sprite = frames[0];
                ApplySorting(0);
            }
        }

        private void ApplySorting(int index)
        {
            if (sortingOrders != null && index < sortingOrders.Length)
            {
                spriteRenderer.sortingOrder = sortingOrders[index];
            }
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0 || Finished)
            {
                return;
            }

            timer += Time.deltaTime;
            int index = (int)(timer * framesPerSecond);
            if (index >= frames.Length)
            {
                if (loop)
                {
                    int span = Mathf.Max(1, frames.Length - loopFirst);
                    index = loopFirst + ((index - loopFirst) % span);
                }
                else
                {
                    Finished = true;
                    if (destroyAtEnd)
                    {
                        Destroy(gameObject);
                    }
                    else
                    {
                        spriteRenderer.enabled = false;
                    }

                    return;
                }
            }

            FrameIndex = index;
            spriteRenderer.sprite = frames[index];
            ApplySorting(index);
        }
    }
}
