using Lithostride.Core;
using Lithostride.World;
using UnityEngine;

namespace Lithostride.Player
{
    /// <summary>
    /// Mineração (modo 2): segurar o clique esquerdo sobre um bloco ao
    /// alcance. O bloco escurece enquanto quebra; trocar de bloco ou soltar
    /// zera o progresso. Não minera através de paredes: a linha do peito até
    /// o bloco não pode passar por outro bloco. Minério quebrado mostra o
    /// ícone de energia do material por um instante.
    /// </summary>
    public sealed class PlayerMining : MonoBehaviour
    {
        [SerializeField] private GameInput input;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private TerrainGrid terrain;
        [SerializeField] private PlayerToolMode toolMode;
        [SerializeField] private PlayerController controller;
        [SerializeField] private SpriteRenderer cursor;

        /// <summary>Alcance do peito ao centro do bloco, em unidades (blocos).</summary>
        [SerializeField, Min(0.5f)] private float reach = 5.5f;

        /// <summary>Altura do peito acima dos pés.</summary>
        [SerializeField] private float chestHeight = WorldScale.KaelChestHeight;

        private bool hasTarget;
        private Vector3Int target;
        private float progress;

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

        private void Update()
        {
            bool active = input != null && terrain != null && toolMode != null && toolMode.Mode == ToolMode.Mining &&
                          controller.ControlEnabled;
            if (!active)
            {
                ClearTarget();
                ShowCursor(false, Vector3Int.zero, false);
                return;
            }

            Vector3Int cell = PointerCell();
            bool valid = terrain.HasBlock(cell) && InReach(cell) && LineOfSight(cell);
            ShowCursor(!input.PointerOverUi, cell, valid);

            if (!input.PrimaryHeld || !valid)
            {
                ClearTarget();
                return;
            }

            if (!hasTarget || cell != target)
            {
                ClearTarget();
                hasTarget = true;
                target = cell;
            }

            TerrainMaterialDef material = terrain.Palette.Material(terrain.MaterialAt(cell));
            float breakTime = material != null ? material.BreakTime : 0.4f;
            progress += Time.deltaTime;
            if (progress >= breakTime)
            {
                Sprite drop = material != null ? material.DropIcon : null;
                terrain.Break(target);
                if (drop != null)
                {
                    FloatingIcon.Spawn(drop, terrain.CellCenter(target));
                }

                hasTarget = false;
                progress = 0f;
                return;
            }

            terrain.SetDamage(target, progress / breakTime);
        }

        private Vector3Int PointerCell()
        {
            Vector2 screen = input.PointerScreen;
            Vector3 world = targetCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -targetCamera.transform.position.z));
            return terrain.WorldToCell(world);
        }

        private bool InReach(Vector3Int cell)
        {
            Vector3 chest = transform.position + (Vector3.up * chestHeight);
            return Vector2.Distance(terrain.CellCenter(cell), chest) <= reach;
        }

        /// <summary>
        /// Percorre as células entre o peito e o alvo (amostragem a cada 1/4 de
        /// bloco): qualquer bloco no meio do caminho bloqueia.
        /// </summary>
        private bool LineOfSight(Vector3Int cell)
        {
            Vector2 from = transform.position + (Vector3.up * chestHeight);
            Vector2 to = terrain.CellCenter(cell);
            int steps = Mathf.CeilToInt(Vector2.Distance(from, to) * 4f);
            for (int i = 1; i < steps; i++)
            {
                Vector3Int probe = terrain.WorldToCell(Vector2.Lerp(from, to, (float)i / steps));
                if (probe != cell && terrain.HasBlock(probe))
                {
                    return false;
                }
            }

            return true;
        }

        private void ShowCursor(bool show, Vector3Int cell, bool valid)
        {
            if (cursor == null)
            {
                return;
            }

            cursor.enabled = show;
            if (show)
            {
                cursor.transform.position = terrain.CellCenter(cell);
                cursor.color = valid ? new Color(1f, 0.95f, 0.5f, 0.9f) : new Color(1f, 0.3f, 0.3f, 0.5f);
            }
        }

        private void ClearTarget()
        {
            if (hasTarget && terrain != null)
            {
                terrain.SetDamage(target, 0f);
            }

            hasTarget = false;
            progress = 0f;
        }
    }
}
