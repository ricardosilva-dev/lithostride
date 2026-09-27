using UnityEngine;

namespace Lithostride.Creatures
{
    public enum BossRow
    {
        Hover = 0,
        Fly = 1,
        Dive = 2,
        Attack = 3,
        React = 4
    }

    /// <summary>
    /// Desenho do boss: as cinco linhas de quadros das duas versões (v2 e v1,
    /// trocáveis pelo painel). O pivô de cada quadro é o centro do corpo, então
    /// trocar de quadro ou de versão não arrasta o corpo nem a hitbox.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BossVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] v2Frames;
        [SerializeField] private int[] v2RowStarts;
        [SerializeField] private Sprite[] v1Frames;
        [SerializeField] private int[] v1RowStarts;
        [SerializeField] private string[] versionNames = { "v2", "v1" };

        private int version;
        private BossRow row;
        private int first;
        private int last;
        private bool loop;
        private float fps = 10f;
        private float timer;

        public int Version => version;
        public string VersionName => versionNames[version];
        public int FrameIndex { get; private set; }

        public void Configure(SpriteRenderer renderer, Sprite[] versionTwo, int[] versionTwoRows, Sprite[] versionOne, int[] versionOneRows)
        {
            spriteRenderer = renderer;
            v2Frames = versionTwo;
            v2RowStarts = versionTwoRows;
            v1Frames = versionOne;
            v1RowStarts = versionOneRows;
        }

        public void SetVersion(int index)
        {
            version = Mathf.Clamp(index, 0, 1);
            Apply();
        }

        public int RowLength(BossRow which)
        {
            int[] starts = version == 0 ? v2RowStarts : v1RowStarts;
            Sprite[] frames = version == 0 ? v2Frames : v1Frames;
            int i = (int)which;
            int end = i + 1 < starts.Length ? starts[i + 1] : frames.Length;
            return end - starts[i];
        }

        /// <summary>Toca quadros [from..to] de uma linha (to &lt; 0 = até o fim).</summary>
        public void Play(BossRow which, int from, int to, bool looping, float framesPerSecond)
        {
            int length = RowLength(which);
            int clampedTo = to < 0 ? length - 1 : Mathf.Min(to, length - 1);
            int clampedFrom = Mathf.Clamp(from, 0, clampedTo);
            if (which == row && clampedFrom == first && clampedTo == last && looping == loop)
            {
                fps = framesPerSecond;
                return;
            }

            row = which;
            first = clampedFrom;
            last = clampedTo;
            loop = looping;
            fps = framesPerSecond;
            timer = 0f;
            Apply();
        }

        public void SetFacingLeft(bool left)
        {
            spriteRenderer.flipX = left;
        }

        public void SetTint(Color color)
        {
            spriteRenderer.color = color;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            Apply();
        }

        private void Apply()
        {
            Sprite[] frames = version == 0 ? v2Frames : v1Frames;
            int[] starts = version == 0 ? v2RowStarts : v1RowStarts;
            if (frames == null || starts == null || spriteRenderer == null)
            {
                return;
            }

            // As versões têm linhas de tamanhos diferentes (a v1 tem 5 quadros de ataque).
            int length = RowLength(row);
            int to = Mathf.Min(last, length - 1);
            int from = Mathf.Min(first, to);
            int span = to - from + 1;
            int step = (int)(timer * fps);
            int offset = loop ? step % span : Mathf.Min(step, span - 1);
            FrameIndex = from + offset;
            spriteRenderer.sprite = frames[starts[(int)row] + FrameIndex];
        }
    }
}
