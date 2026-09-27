using System;
using UnityEngine;

namespace Lithostride.World
{
    /// <summary>
    /// Fundo pintado atrás do jogo. Os três fundos do pack são imagens
    /// inteiras (não camadas de um parallax): um de cada vez, escolhido pela
    /// região onde está a câmera (dia, entardecer, caverna) ou à mão pelo
    /// painel de teste.
    ///
    /// A imagem acompanha a câmera, sempre cobrindo a vista com sobra
    /// (overscan) em qualquer zoom e proporção, e desliza pouco no sentido
    /// oposto à câmera — no máximo a sobra, então a borda nunca aparece e não
    /// há cópia espelhada. Troca de fundo com transição curta.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class BackdropView : MonoBehaviour
    {
        [Serializable]
        private sealed class Zone
        {
            [SerializeField] private Rect area;
            [SerializeField] private int backdrop;

            public Zone(Rect area, int backdrop)
            {
                this.area = area;
                this.backdrop = backdrop;
            }

            public Rect Area => area;
            public int Backdrop => backdrop;
        }

        [SerializeField] private Camera targetCamera;
        [SerializeField] private SpriteRenderer front;
        [SerializeField] private SpriteRenderer back;
        [SerializeField] private Sprite[] backdrops;
        [SerializeField] private string[] backdropNames;
        [SerializeField] private Zone[] zones;
        [SerializeField] private int defaultBackdrop;

        /// <summary>Sobra em volta da vista: a imagem cobre 112% dela.</summary>
        [SerializeField, Range(1f, 1.5f)] private float overscan = 1.12f;

        /// <summary>Deslize por unidade andada pela câmera (limitado à sobra).</summary>
        [SerializeField, Range(0f, 0.2f)] private float drift = 0.03f;

        [SerializeField, Min(0.01f)] private float fadeTime = 0.6f;

        private int current = -1;
        private int manual = -1;
        private float fade = 1f;

        /// <summary>Fundo escolhido à mão (-1 = automático pela região).</summary>
        public int ManualBackdrop => manual;

        public int CurrentBackdrop => current;

        public int Count => backdrops == null ? 0 : backdrops.Length;

        public string NameOf(int index)
        {
            return backdropNames != null && index >= 0 && index < backdropNames.Length ? backdropNames[index] : "?";
        }

        public void Configure(Camera cameraToFollow, SpriteRenderer frontRenderer, SpriteRenderer backRenderer, Sprite[] images,
            string[] names, int fallback)
        {
            targetCamera = cameraToFollow;
            front = frontRenderer;
            back = backRenderer;
            backdrops = images;
            backdropNames = names;
            defaultBackdrop = fallback;
        }

        public void SetZones(Rect[] areas, int[] indices)
        {
            zones = new Zone[areas.Length];
            for (int i = 0; i < areas.Length; i++)
            {
                zones[i] = new Zone(areas[i], indices[i]);
            }
        }

        /// <summary>Escolhe o fundo à mão; -1 volta ao automático.</summary>
        public void SetManual(int index)
        {
            manual = index >= 0 && index < Count ? index : -1;
        }

        private void LateUpdate()
        {
            if (targetCamera == null || front == null || backdrops == null || backdrops.Length == 0 || Screen.height <= 0)
            {
                return;
            }

            Vector3 cameraPosition = targetCamera.transform.position;
            int wanted = manual >= 0 ? manual : ZoneAt(cameraPosition);
            if (wanted != current)
            {
                if (current >= 0 && back != null)
                {
                    back.sprite = backdrops[current];
                    back.enabled = true;
                    fade = 0f;
                }

                current = wanted;
                front.sprite = backdrops[current];
            }

            fade = Mathf.Min(1f, fade + (Time.unscaledDeltaTime / fadeTime));
            Color color = front.color;
            color.a = fade;
            front.color = color;
            if (back != null && fade >= 1f)
            {
                back.enabled = false;
            }

            Place(front, cameraPosition);
            if (back != null && back.enabled)
            {
                Place(back, cameraPosition);
            }
        }

        private void Place(SpriteRenderer target, Vector3 cameraPosition)
        {
            Vector2 size = target.sprite.bounds.size;
            float viewHeight = 2f * targetCamera.orthographicSize;
            float viewWidth = viewHeight * targetCamera.aspect;
            float scale = overscan * Mathf.Max(viewHeight / size.y, viewWidth / size.x);
            target.transform.localScale = new Vector3(scale, scale, 1f);

            // O deslize é uma onda triangular dentro da sobra: contínuo pelo
            // mundo inteiro, sem salto, e a borda da imagem nunca entra na vista.
            float slackX = Mathf.Max(0f, ((size.x * scale) - viewWidth) * 0.5f);
            float slackY = Mathf.Max(0f, ((size.y * scale) - viewHeight) * 0.5f);
            float offsetX = slackX <= 0f ? 0f : Mathf.PingPong((-cameraPosition.x * drift) + slackX, 2f * slackX) - slackX;
            float offsetY = slackY <= 0f ? 0f : Mathf.PingPong((-cameraPosition.y * drift * 0.5f) + slackY, 2f * slackY) - slackY;
            target.transform.position = new Vector3(cameraPosition.x + offsetX, cameraPosition.y + offsetY, 0f);
        }

        private int ZoneAt(Vector3 position)
        {
            if (zones != null)
            {
                // A última zona que contém o ponto vence: zonas específicas vêm depois das gerais.
                for (int i = zones.Length - 1; i >= 0; i--)
                {
                    if (zones[i].Area.Contains(position))
                    {
                        return Mathf.Clamp(zones[i].Backdrop, 0, backdrops.Length - 1);
                    }
                }
            }

            return Mathf.Clamp(defaultBackdrop, 0, backdrops.Length - 1);
        }
    }
}
