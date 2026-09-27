using System.Collections.Generic;
using Lithostride.Combat;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lithostride.UI
{
    /// <summary>
    /// Sobreposições de depuração desenhadas em linhas (GL) por cima do jogo:
    /// grade de células, pivôs, colisores (corpo, hurtboxes, contorno do
    /// terreno), hitbox do golpe ativo e, no visualizador, linha de base e
    /// limites de cada quadro. Tudo desligado por padrão; o painel liga.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class DebugOverlay : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform[] pivots;
        [SerializeField] private Collider2D[] colliders;
        [SerializeField] private CompositeCollider2D[] terrainColliders;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private AnimationViewer viewer;

        private readonly List<Vector2> path = new List<Vector2>();
        private Material material;

        public bool ShowGrid { get; set; }
        public bool ShowPivots { get; set; }
        public bool ShowColliders { get; set; }

        public void Configure(Camera cameraToDraw, Transform[] pivotTransforms, Collider2D[] bodyColliders,
            CompositeCollider2D[] terrain, PlayerCombat playerCombat, AnimationViewer animationViewer)
        {
            targetCamera = cameraToDraw;
            pivots = pivotTransforms;
            colliders = bodyColliders;
            terrainColliders = terrain;
            combat = playerCombat;
            viewer = animationViewer;
        }

        private void OnEnable()
        {
            Camera.onPostRender += OnCameraPostRender;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void OnDisable()
        {
            Camera.onPostRender -= OnCameraPostRender;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera cameraRendered)
        {
            OnCameraPostRender(cameraRendered);
        }

        private void OnCameraPostRender(Camera cameraRendered)
        {
            if (cameraRendered != targetCamera)
            {
                return;
            }

            bool viewerLines = viewer != null && (viewer.ShowBaseline || viewer.ShowBounds) && ViewerVisible();
            if (!ShowGrid && !ShowPivots && !ShowColliders && !viewerLines)
            {
                return;
            }

            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave };
            }

            material.SetPass(0);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(targetCamera.projectionMatrix);
            GL.modelview = targetCamera.worldToCameraMatrix;
            GL.Begin(GL.LINES);

            Rect view = ViewRect();
            if (ShowGrid)
            {
                DrawGrid(view);
            }

            if (ShowColliders)
            {
                DrawColliders(view);
            }

            if (ShowPivots && pivots != null)
            {
                GL.Color(new Color(1f, 0.2f, 0.9f));
                foreach (Transform pivot in pivots)
                {
                    if (pivot != null && pivot.gameObject.activeInHierarchy)
                    {
                        Cross(pivot.position, 0.25f);
                    }
                }
            }

            if (viewerLines)
            {
                DrawViewer();
            }

            GL.End();
            GL.PopMatrix();
        }

        private Rect ViewRect()
        {
            float h = targetCamera.orthographicSize * 2f, w = h * targetCamera.aspect;
            Vector3 c = targetCamera.transform.position;
            return new Rect(c.x - (w * 0.5f), c.y - (h * 0.5f), w, h);
        }

        private void DrawGrid(Rect view)
        {
            if (view.width > 160f)
            {
                return;
            }

            GL.Color(new Color(1f, 1f, 1f, 0.18f));
            for (int x = Mathf.FloorToInt(view.xMin); x <= Mathf.CeilToInt(view.xMax); x++)
            {
                Line(new Vector2(x, view.yMin), new Vector2(x, view.yMax));
            }

            for (int y = Mathf.FloorToInt(view.yMin); y <= Mathf.CeilToInt(view.yMax); y++)
            {
                Line(new Vector2(view.xMin, y), new Vector2(view.xMax, y));
            }
        }

        private void DrawColliders(Rect view)
        {
            GL.Color(new Color(0.2f, 1f, 0.3f));
            if (terrainColliders != null)
            {
                foreach (CompositeCollider2D composite in terrainColliders)
                {
                    if (composite == null || !composite.bounds.Intersects(new Bounds(view.center, view.size)))
                    {
                        continue;
                    }

                    for (int p = 0; p < composite.pathCount; p++)
                    {
                        path.Clear();
                        composite.GetPath(p, path);
                        for (int i = 0; i < path.Count; i++)
                        {
                            Line(path[i], path[(i + 1) % path.Count]);
                        }
                    }
                }
            }

            if (colliders != null)
            {
                foreach (Collider2D collider in colliders)
                {
                    if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    GL.Color(collider.isTrigger ? new Color(1f, 0.85f, 0.2f) : new Color(0.3f, 0.8f, 1f));
                    Box(collider.bounds);
                }
            }

            if (combat != null && combat.TryGetActiveHitbox(out Rect hitbox))
            {
                GL.Color(new Color(1f, 0.2f, 0.2f));
                Box(new Bounds(hitbox.center, hitbox.size));
            }
        }

        private void DrawViewer()
        {
            for (int i = 0; i < viewer.StationCount; i++)
            {
                SpriteRenderer renderer = viewer.StationRenderer(i);
                if (renderer == null || renderer.sprite == null)
                {
                    continue;
                }

                Vector3 pivot = renderer.transform.position;
                if (viewer.ShowBaseline)
                {
                    GL.Color(new Color(1f, 0.3f, 0.3f));
                    Line(new Vector2(pivot.x - 3f, pivot.y), new Vector2(pivot.x + 3f, pivot.y));
                    Cross(pivot, 0.2f);
                }

                if (viewer.ShowBounds)
                {
                    GL.Color(new Color(0.4f, 0.9f, 1f));
                    Box(renderer.bounds);
                }
            }
        }

        private bool ViewerVisible()
        {
            if (viewer.StationCount == 0 || viewer.StationRenderer(0) == null)
            {
                return false;
            }

            Rect view = ViewRect();
            return Mathf.Abs(viewer.StationRenderer(0).transform.position.x - view.center.x) < 200f;
        }

        private static void Line(Vector2 a, Vector2 b)
        {
            GL.Vertex3(a.x, a.y, 0f);
            GL.Vertex3(b.x, b.y, 0f);
        }

        private static void Cross(Vector3 p, float size)
        {
            Line(new Vector2(p.x - size, p.y), new Vector2(p.x + size, p.y));
            Line(new Vector2(p.x, p.y - size), new Vector2(p.x, p.y + size));
        }

        private static void Box(Bounds b)
        {
            Vector2 min = b.min, max = b.max;
            Line(new Vector2(min.x, min.y), new Vector2(max.x, min.y));
            Line(new Vector2(max.x, min.y), new Vector2(max.x, max.y));
            Line(new Vector2(max.x, max.y), new Vector2(min.x, max.y));
            Line(new Vector2(min.x, max.y), new Vector2(min.x, min.y));
        }
    }
}
