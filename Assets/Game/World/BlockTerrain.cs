using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Lithostride.World
{
    /// <summary>
    /// Terreno de blocos de 1 unidade (16 px). O Tilemap é ao mesmo tempo o
    /// desenho e a colisão (TilemapCollider2D): tirar um tile tira o bloco do
    /// mundo físico.
    ///
    /// Ao quebrar um bloco, só a grama precisa ser refeita: um bloco de grama
    /// que perde o vizinho de um lado vira ponta de borda. Terra exposta
    /// continua terra, e a grama não cresce sobre ela.
    /// </summary>
    public sealed class BlockTerrain : MonoBehaviour
    {
        [SerializeField] private Tilemap blocks;

        [SerializeField] private TileBase[] grassTops;
        [SerializeField] private TileBase[] grassLeftEdges;
        [SerializeField] private TileBase[] grassRightEdges;

        /// <summary>Escurecimento do bloco quando está quase quebrando.</summary>
        [SerializeField] private Color damagedColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        private HashSet<TileBase> grass;

        public Vector3Int WorldToCell(Vector3 position)
        {
            return blocks.WorldToCell(position);
        }

        public Vector3 CellCenter(Vector3Int cell)
        {
            return blocks.GetCellCenterWorld(cell);
        }

        public bool HasBlock(Vector3Int cell)
        {
            return blocks.HasTile(cell);
        }

        /// <summary>Remove o bloco e refaz a grama dos vizinhos. Falso se não havia bloco.</summary>
        public bool Break(Vector3Int cell)
        {
            if (!blocks.HasTile(cell))
            {
                return false;
            }

            blocks.SetTile(cell, null);
            RefreshGrass(cell + Vector3Int.left);
            RefreshGrass(cell + Vector3Int.right);
            return true;
        }

        /// <summary>Tinge o bloco conforme o progresso da quebra (0 = intacto, 1 = quase quebrado).</summary>
        public void SetDamage(Vector3Int cell, float amount)
        {
            if (blocks.HasTile(cell))
            {
                blocks.SetColor(cell, Color.Lerp(Color.white, damagedColor, Mathf.Clamp01(amount)));
            }
        }

        /// <summary>
        /// Escolhe o tile de um bloco de grama pelos vizinhos: ar à esquerda,
        /// ponta esquerda; ar à direita, ponta direita; senão, topo comum. A
        /// variante sai de um hash da posição, igual a cada reconstrução.
        /// </summary>
        public void RefreshGrass(Vector3Int cell)
        {
            if (!IsGrass(blocks.GetTile(cell)))
            {
                return;
            }

            bool left = blocks.HasTile(cell + Vector3Int.left);
            bool right = blocks.HasTile(cell + Vector3Int.right);

            TileBase[] set = !left && right ? grassLeftEdges
                : left && !right ? grassRightEdges
                : grassTops;

            if (set == null || set.Length == 0)
            {
                return;
            }

            // A mesma variante que o vizinho da esquerda deixaria um par repetido lado a lado.
            int index = Hash(cell.x, cell.y, 1) % set.Length;
            TileBase leftTile = blocks.GetTile(cell + Vector3Int.left);
            if (set.Length > 1 && leftTile == set[index])
            {
                index = (index + 1) % set.Length;
            }

            blocks.SetTile(cell, set[index]);
        }

        private bool IsGrass(TileBase tile)
        {
            if (tile == null)
            {
                return false;
            }

            if (grass == null)
            {
                grass = new HashSet<TileBase>();
                AddAll(grassTops);
                AddAll(grassLeftEdges);
                AddAll(grassRightEdges);
            }

            return grass.Contains(tile);
        }

        private void AddAll(TileBase[] tiles)
        {
            if (tiles == null)
            {
                return;
            }

            foreach (TileBase tile in tiles)
            {
                if (tile != null)
                {
                    grass.Add(tile);
                }
            }
        }

        /// <summary>Ruído inteiro determinístico, não negativo.</summary>
        public static int Hash(int x, int y, int salt)
        {
            unchecked
            {
                int h = (x * 374761393) + (y * 668265263) + (salt * 1274126177);
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return h & 0x7FFFFFFF;
            }
        }
    }
}
