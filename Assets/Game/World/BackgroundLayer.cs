using UnityEngine;

namespace Lithostride.World
{
    /// <summary>
    /// Cenário pintado atrás do jogo. Acompanha a câmera e é escalado de forma
    /// uniforme para cobrir a vista inteira em qualquer zoom (cortando o que
    /// sobra, nunca esticando), e desliza devagar no sentido oposto ao
    /// movimento horizontal da câmera, o que dá a sensação de distância.
    ///
    /// As cópias vizinhas são a mesma imagem espelhada, para a emenda juntar
    /// uma borda com ela mesma; com o deslizamento lento, raramente aparecem.
    /// Roda depois da câmera e do snap de pixels.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class BackgroundLayer : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;

        /// <summary>Cópias em -2..2 larguras; as de índice ímpar são espelhadas.</summary>
        [SerializeField] private SpriteRenderer[] copies;

        /// <summary>
        /// Quanto a imagem desliza por unidade andada pela câmera, em unidades
        /// da própria imagem. 0,1: atravessar o mapa de 200 unidades desliza um
        /// quinto da largura do cenário.
        /// </summary>
        [SerializeField, Range(0f, 1f)] private float drift = 0.1f;

        private void LateUpdate()
        {
            if (targetCamera == null || copies == null || copies.Length == 0 || copies[0] == null ||
                copies[0].sprite == null || Screen.height <= 0)
            {
                return;
            }

            Vector2 size = copies[0].sprite.bounds.size;
            float viewHeight = 2f * targetCamera.orthographicSize;
            float viewWidth = viewHeight * targetCamera.aspect;
            float scale = Mathf.Max(viewHeight / size.y, viewWidth / size.x);
            transform.localScale = new Vector3(scale, scale, 1f);

            // O padrão imagem + espelho se repete a cada duas larguras.
            float period = 2f * size.x;
            float slide = Mathf.Repeat((-targetCamera.transform.position.x * drift) + size.x, period) - size.x;

            float step = viewHeight / Screen.height;
            Vector3 cameraPosition = targetCamera.transform.position;
            float x = cameraPosition.x + (slide * scale);
            transform.position = new Vector3(Mathf.Round(x / step) * step, cameraPosition.y, 0f);
        }
    }
}
