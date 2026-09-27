using System.Text;
using Lithostride.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Lithostride.EditorTools
{
    /// <summary>Relatório de diagnóstico da cena de teste (tiles e colisão do terreno), para o modo batch.</summary>
    public static class PackDiagnostics
    {
        public static void Report()
        {
            EditorSceneManager.OpenScene(PackSceneBuilder.ScenePath);
            StringBuilder sb = new StringBuilder("Pack completo: diagnóstico\n");

            TerrainPalette palette = AssetDatabase.LoadAssetAtPath<TerrainPalette>(PackPipeline.PalettePath);
            sb.Append("paleta: ").Append(palette != null).Append(" materiais=").Append(palette != null ? palette.MaterialCount : -1);
            if (palette != null && palette.MaterialCount > 0)
            {
                TileBase full = palette.Material(1).TileFor(TerrainShape.Full);
                sb.Append(" terra cheio=").Append(full != null ? full.name : "null");
            }

            sb.Append('\n');

            TerrainGrid grid = Object.FindAnyObjectByType<TerrainGrid>();
            sb.Append("TerrainGrid: ").Append(grid != null);
            if (grid != null)
            {
                sb.Append(" paleta=").Append(grid.Palette != null).Append(" mapa=").Append(grid.Map != null);
                if (grid.Map != null)
                {
                    int solid = 0;
                    foreach (byte m in grid.Map.Materials) if (m != 0) solid++;
                    sb.Append(" células sólidas no estado=").Append(solid);
                }
            }

            sb.Append('\n');
            foreach (TilemapCollider2D tilemapCollider in Object.FindObjectsByType<TilemapCollider2D>())
            {
                Tilemap tilemap = tilemapCollider.GetComponent<Tilemap>();
                CompositeCollider2D composite = tilemapCollider.GetComponent<CompositeCollider2D>();
                sb.Append(tilemap.name).Append(": tiles=").Append(tilemap.GetUsedTilesCount()).Append(" formas=")
                    .Append(tilemapCollider.shapeCount).Append(" caminhos=").Append(composite.pathCount).Append('\n');
            }

            if (grid != null && grid.Palette != null && grid.Map != null)
            {
                grid.RebuildAll();
                grid.RefreshCollision();
                sb.Append("depois de RebuildAll:\n");
                foreach (TilemapCollider2D tilemapCollider in Object.FindObjectsByType<TilemapCollider2D>())
                {
                    Tilemap tilemap = tilemapCollider.GetComponent<Tilemap>();
                    sb.Append(tilemap.name).Append(": tiles=").Append(tilemap.GetUsedTilesCount()).Append(" formas=")
                        .Append(tilemapCollider.shapeCount).Append(" caminhos=")
                        .Append(tilemapCollider.GetComponent<CompositeCollider2D>().pathCount).Append('\n');
                }
            }

            Debug.Log(sb.ToString());
        }
    }
}
