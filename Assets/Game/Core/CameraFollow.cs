using UnityEngine;

namespace Lithostride.Core
{
    /// <summary>
    /// Única autoridade sobre a posição da câmera: acompanha o alvo com
    /// suavização. A posição suavizada fica guardada à parte; a câmera em si
    /// é colocada na grade de pixels da tela, para a pixel art não tremular
    /// com movimentos de fração de pixel.
    /// </summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 offset = new Vector2(0f, 2.5f);
        [SerializeField, Min(0f)] private float smoothTime = 0.12f;

        private Camera attachedCamera;
        private Vector3 position;
        private Vector3 velocity;
        private Vector2 baseOffset;

        public void Configure(Transform followTarget, Vector2 framingOffset)
        {
            target = followTarget;
            offset = framingOffset;
        }

        /// <summary>
        /// Desloca o enquadramento em unidades do mundo, de imediato e de forma
        /// permanente: a câmera continua seguindo o alvo, com o novo deslocamento.
        /// Usado pelo zoom para manter o ponto sob o cursor parado na tela.
        /// </summary>
        public void Pan(Vector2 delta)
        {
            Vector3 shift = new Vector3(delta.x, delta.y, 0f);
            offset += delta;
            position += shift;
            transform.position += shift;
        }

        /// <summary>Volta ao enquadramento original, já sobre o alvo (teleporte, renascer).</summary>
        public void Recenter()
        {
            offset = baseOffset;
            velocity = Vector3.zero;
            if (target != null)
            {
                position = new Vector3(target.position.x + offset.x, target.position.y + offset.y, position.z);
                transform.position = SnapToScreenPixels(position);
            }
        }

        private void Awake()
        {
            attachedCamera = GetComponent<Camera>();
            position = transform.position;
            baseOffset = offset;
        }

        private void LateUpdate()
        {
            if (target != null)
            {
                Vector3 goal = new Vector3(target.position.x + offset.x, target.position.y + offset.y, position.z);
                position = Vector3.SmoothDamp(position, goal, ref velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            }

            transform.position = SnapToScreenPixels(position);
        }

        /// <summary>
        /// Arredonda para a grade de pixels da tela. Com largura ou altura
        /// ímpar, o centro da tela cai no meio de um pixel: a grade anda meio passo.
        /// </summary>
        private Vector3 SnapToScreenPixels(Vector3 point)
        {
            if (attachedCamera == null || Screen.height <= 0)
            {
                return point;
            }

            float step = 2f * attachedCamera.orthographicSize / Screen.height;
            float halfX = Screen.width % 2 == 1 ? 0.5f : 0f;
            float halfY = Screen.height % 2 == 1 ? 0.5f : 0f;
            return new Vector3((Mathf.Round((point.x / step) - halfX) + halfX) * step,
                (Mathf.Round((point.y / step) - halfY) + halfY) * step, point.z);
        }
    }
}
