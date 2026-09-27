using UnityEngine;

namespace Lithostride.World
{
    /// <summary>
    /// Árvore ou prop apoiado no terreno. Guarda as células sob o tronco (ou
    /// sob a base do prop); se uma delas deixa de ser bloco inteiro, reage:
    /// prop pequeno cai um pouco e some, árvore tomba pela base e some. Nada
    /// fica flutuando. Voltam ao reset do terreno.
    /// </summary>
    public sealed class PropSupport : MonoBehaviour
    {
        [SerializeField] private TerrainGrid terrain;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Vector2Int[] supportCells;
        [SerializeField] private bool isTree;
        [SerializeField, Min(0.05f)] private float reactionTime = 0.7f;

        private Vector3 startPosition;
        private Quaternion startRotation;
        private float timer = -1f;
        private float fallDirection = 1f;

        public void Configure(TerrainGrid grid, SpriteRenderer renderer, Vector2Int[] cells, bool tree)
        {
            terrain = grid;
            spriteRenderer = renderer;
            supportCells = cells;
            isTree = tree;
        }

        public Vector2Int[] SupportCells => supportCells;

        private void Awake()
        {
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        private void OnEnable()
        {
            if (terrain != null)
            {
                terrain.CellChanged += OnCellChanged;
                terrain.TerrainReset += Restore;
            }
        }

        private void OnDisable()
        {
            if (terrain != null)
            {
                terrain.CellChanged -= OnCellChanged;
                terrain.TerrainReset -= Restore;
            }
        }

        private void OnCellChanged(int x, int y)
        {
            if (timer >= 0f || supportCells == null)
            {
                return;
            }

            foreach (Vector2Int cell in supportCells)
            {
                if (cell.x == x && cell.y == y && terrain.ShapeAt(new Vector3Int(x, y, 0)) != TerrainShape.Full)
                {
                    // Tomba para o lado do apoio perdido (o outro lado ainda segura).
                    float center = (supportCells[0].x + supportCells[supportCells.Length - 1].x) * 0.5f;
                    fallDirection = x < center ? 1f : -1f;
                    timer = 0f;
                    return;
                }
            }
        }

        private void Update()
        {
            if (timer < 0f || spriteRenderer == null)
            {
                return;
            }

            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / reactionTime);
            if (isTree)
            {
                transform.rotation = startRotation * Quaternion.Euler(0f, 0f, fallDirection * 80f * t * t);
            }
            else
            {
                transform.position = startPosition + (Vector3.down * 0.4f * t);
            }

            Color color = spriteRenderer.color;
            color.a = 1f - Mathf.Clamp01((t - 0.4f) / 0.6f);
            spriteRenderer.color = color;

            if (t >= 1f)
            {
                spriteRenderer.enabled = false;
                timer = -2f;
            }
        }

        /// <summary>Volta ao lugar, de pé e visível.</summary>
        public void Restore()
        {
            timer = -1f;
            transform.SetPositionAndRotation(startPosition, startRotation);
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                Color color = spriteRenderer.color;
                color.a = 1f;
                spriteRenderer.color = color;
            }
        }
    }
}
