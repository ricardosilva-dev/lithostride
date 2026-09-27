using UnityEngine;
using UnityEngine.Tilemaps;

namespace Lithostride.World
{
    /// <summary>
    /// Tile do terreno: um sprite (máscara de forma, borda, franja, cobertura
    /// ou detalhe) e o tipo de colisão. A cor do terreno não vem daqui: o
    /// shader do terreno contínuo pinta pela posição no mundo. Sem LockColor:
    /// a cor por célula mostra o dano ao minerar.
    /// </summary>
    [CreateAssetMenu(menuName = "Lithostride/Tile de terreno")]
    public sealed class TerrainSpriteTile : TileBase
    {
        [SerializeField] private Sprite sprite;
        [SerializeField] private Tile.ColliderType colliderType = Tile.ColliderType.None;

        public Sprite Sprite => sprite;

        public void Configure(Sprite tileSprite, Tile.ColliderType collider)
        {
            sprite = tileSprite;
            colliderType = collider;
        }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = sprite;
            tileData.color = Color.white;
            tileData.transform = Matrix4x4.identity;
            tileData.colliderType = colliderType;
            tileData.flags = TileFlags.LockTransform;
        }
    }
}
