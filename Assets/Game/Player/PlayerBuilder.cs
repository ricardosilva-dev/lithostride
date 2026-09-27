using Lithostride.Core;
using Lithostride.World;
using UnityEngine;

namespace Lithostride.Player
{
    /// <summary>
    /// Ferramenta de teste (modo 3): põe o material escolhido (Q/E) na forma
    /// escolhida (R: inteiro, rampa para a direita, rampa para a esquerda,
    /// meio-bloco) com o clique esquerdo; o direito tira o bloco na hora.
    /// Não é inventário: serve para testar conexões e colisão em qualquer
    /// lugar. Não coloca dentro do jogador, do boss nem dos alvos.
    /// </summary>
    public sealed class PlayerBuilder : MonoBehaviour
    {
        private static readonly TerrainShape[] Shapes =
            { TerrainShape.Full, TerrainShape.SlopeUpRight, TerrainShape.SlopeUpLeft, TerrainShape.HalfBottom };

        private static readonly string[] ShapeNames = { "inteiro", "rampa ↗", "rampa ↖", "meio-bloco" };

        [SerializeField] private GameInput input;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private TerrainGrid terrain;
        [SerializeField] private PlayerToolMode toolMode;
        [SerializeField] private PlayerController controller;
        [SerializeField] private SpriteRenderer cursor;
        [SerializeField, Min(0.5f)] private float reach = 7f;
        [SerializeField] private float chestHeight = WorldScale.KaelChestHeight;

        private readonly Collider2D[] overlaps = new Collider2D[8];
        private ContactFilter2D bodies;
        private int material = 1;
        private int shape;

        public int SelectedMaterial => material;
        public string SelectedShapeName => ShapeNames[shape];

        public void Configure(GameInput gameInput, Camera cameraToUse, TerrainGrid grid, PlayerToolMode mode,
            PlayerController playerController, SpriteRenderer cellCursor)
        {
            input = gameInput;
            targetCamera = cameraToUse;
            terrain = grid;
            toolMode = mode;
            controller = playerController;
            cursor = cellCursor;
        }

        public void SelectMaterial(int number)
        {
            if (terrain != null && number >= 1 && number <= terrain.Palette.MaterialCount)
            {
                material = number;
            }
        }

        public void CycleShape()
        {
            shape = (shape + 1) % Shapes.Length;
        }

        private void Awake()
        {
            bodies = new ContactFilter2D { useTriggers = true };
        }

        private void Update()
        {
            bool active = input != null && terrain != null && toolMode != null && toolMode.Mode == ToolMode.Build &&
                          controller.ControlEnabled;
            if (!active)
            {
                if (cursor != null)
                {
                    cursor.enabled = false;
                }

                return;
            }

            int count = terrain.Palette.MaterialCount;
            if (input.Cycle != 0 && count > 0)
            {
                material = ((material - 1 + input.Cycle + count) % count) + 1;
            }

            if (input.ShapeCyclePressed)
            {
                CycleShape();
            }

            Vector2 screen = input.PointerScreen;
            Vector3 world = targetCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -targetCamera.transform.position.z));
            Vector3Int cell = terrain.WorldToCell(world);
            bool inReach = Vector2.Distance(terrain.CellCenter(cell), transform.position + (Vector3.up * chestHeight)) <= reach;
            bool free = !terrain.HasBlock(cell) && terrain.InBounds(cell) && !Occupied(cell);

            if (cursor != null)
            {
                cursor.enabled = !input.PointerOverUi;
                cursor.transform.position = terrain.CellCenter(cell);
                cursor.color = inReach && free ? new Color(0.5f, 1f, 0.6f, 0.9f) : new Color(1f, 0.3f, 0.3f, 0.5f);
            }

            if (input.PrimaryHeld && inReach && free)
            {
                terrain.Place(cell, material, Shapes[shape]);
            }
            else if (input.SecondaryPressed && inReach)
            {
                terrain.Break(cell);
            }
        }

        /// <summary>Célula ocupada por um corpo (jogador, boss, alvo): lá não entra bloco.</summary>
        private bool Occupied(Vector3Int cell)
        {
            Vector2 center = terrain.CellCenter(cell);
            int found = Physics2D.OverlapBox(center, new Vector2(0.96f, 0.96f), 0f, bodies, overlaps);
            for (int i = 0; i < found; i++)
            {
                Collider2D other = overlaps[i];
                if (other.attachedRigidbody != null && other.attachedRigidbody.bodyType != RigidbodyType2D.Static)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
