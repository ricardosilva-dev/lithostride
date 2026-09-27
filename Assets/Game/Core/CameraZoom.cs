using UnityEngine;

namespace Lithostride.Core
{
    /// <summary>
    /// Zoom pela roda do mouse, centrado no cursor. Só muda o tamanho
    /// ortográfico e a posição da câmera: sprites, tiles e física não são tocados.
    ///
    /// O zoom é medido em pixels de tela por pixel lógico (16 por unidade,
    /// <see cref="WorldScale"/>). A escala inicial é inteira e vem da altura da
    /// tela (<see cref="WorldScale.BasePixelScale"/>): 1 em 720p e 1080p.
    /// Durante a rolagem ele varia de forma contínua; parada a roda, assenta
    /// na escala inteira mais próxima (ou 1/2, 1/3, 1/4 ao afastar), onde
    /// cada pixel da arte vira um bloco exato de pixels da tela.
    /// </summary>
    public sealed class CameraZoom : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private CameraFollow follow;
        [SerializeField] private GameInput input;

        [SerializeField, Min(1)] private int pixelsPerUnit = WorldScale.PixelsPerUnit;

        /// <summary>Limites úteis: 1/4 (visão geral de uma região) a 4x (inspeção de pixels).</summary>
        [SerializeField, Min(0.001f)] private float minScale = 0.25f;
        [SerializeField, Min(0.001f)] private float maxScale = 4f;

        [SerializeField, Min(1.01f)] private float stepFactor = 1.12f;
        [SerializeField, Min(0f)] private float settleDelay = 0.15f;
        [SerializeField, Min(0.1f)] private float settleSpeed = 14f;

        private float basePixelScale = 1f;
        private float pixelScale = 1f;
        private float targetScale = 1f;
        private float lastScrollTime = float.NegativeInfinity;
        private Vector2 anchor;

        /// <summary>Pixels de tela por pixel lógico agora.</summary>
        public float PixelScale => pixelScale;

        public void Configure(Camera cameraToZoom, CameraFollow cameraFollow, GameInput gameInput, int unitsPixels)
        {
            targetCamera = cameraToZoom;
            follow = cameraFollow;
            input = gameInput;
            pixelsPerUnit = unitsPixels;
        }

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            basePixelScale = WorldScale.BasePixelScale(Screen.height);
            pixelScale = basePixelScale;
            targetScale = basePixelScale;
            anchor = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            ApplySize();
        }

        /// <summary>Escala exata (roteiro de evidências): relativa ao zoom inicial, sem transição.</summary>
        public void SetRelativeScale(float relative)
        {
            targetScale = Mathf.Clamp(basePixelScale * relative, minScale, maxScale);
            pixelScale = targetScale;
            lastScrollTime = float.NegativeInfinity;
            ApplySize();
        }

        /// <summary>Volta ao zoom inicial (usado pelo teleporte do painel).</summary>
        public void ResetZoom()
        {
            targetScale = basePixelScale;
            pixelScale = basePixelScale;
            ApplySize();
        }

        private void Update()
        {
            if (targetCamera == null)
            {
                return;
            }

            ReadScroll();

            if (Time.unscaledTime - lastScrollTime > settleDelay)
            {
                targetScale = Snap(targetScale);
            }

            if (!Mathf.Approximately(pixelScale, targetScale))
            {
                // Interpolação em escala logarítmica: aproximar e afastar no mesmo ritmo.
                float t = 1f - Mathf.Exp(-settleSpeed * Time.unscaledDeltaTime);
                float next = Mathf.Exp(Mathf.Lerp(Mathf.Log(pixelScale), Mathf.Log(targetScale), t));
                if (Mathf.Abs(Mathf.Log(next / targetScale)) < 0.002f)
                {
                    next = targetScale;
                }

                ZoomAround(anchor, next);
            }
            else
            {
                // Mantém a escala se a janela mudar de tamanho.
                ApplySize();
            }
        }

        private void ReadScroll()
        {
            float steps = input != null ? input.ScrollSteps : 0f;
            if (Mathf.Approximately(steps, 0f))
            {
                return;
            }

            targetScale = Mathf.Clamp(targetScale * Mathf.Pow(stepFactor, steps), minScale, maxScale);
            lastScrollTime = Time.unscaledTime;

            Vector2 screenPoint = input.PointerScreen;
            bool insideScreen = screenPoint.x >= 0f && screenPoint.y >= 0f &&
                                screenPoint.x <= Screen.width && screenPoint.y <= Screen.height;
            anchor = insideScreen ? screenPoint : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        /// <summary>
        /// Muda a escala mantendo parado o ponto do mundo sob <paramref name="screenPoint"/>:
        /// tela → mundo antes e depois; a diferença é quanto a câmera anda.
        /// </summary>
        private void ZoomAround(Vector2 screenPoint, float scale)
        {
            Vector3 before = ScreenToWorld(screenPoint);
            pixelScale = scale;
            ApplySize();
            Vector3 after = ScreenToWorld(screenPoint);

            Vector2 delta = before - after;
            if (follow != null)
            {
                follow.Pan(delta);
            }
            else
            {
                transform.position += new Vector3(delta.x, delta.y, 0f);
            }
        }

        private void ApplySize()
        {
            if (Screen.height > 0)
            {
                targetCamera.orthographicSize = Screen.height / (2f * pixelsPerUnit * pixelScale);
            }
        }

        /// <summary>Escala inteira mais próxima; abaixo de 1, a fração 1/n mais próxima.</summary>
        private static float Snap(float scale)
        {
            return scale >= 1f
                ? Mathf.Max(1f, Mathf.Round(scale))
                : 1f / Mathf.Max(1f, Mathf.Round(1f / scale));
        }

        private Vector3 ScreenToWorld(Vector2 screenPoint)
        {
            return targetCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y,
                -targetCamera.transform.position.z));
        }
    }
}
