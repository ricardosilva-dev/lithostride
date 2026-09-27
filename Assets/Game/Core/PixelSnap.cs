using UnityEngine;

namespace Lithostride.Core
{
    /// <summary>
    /// Mantém este objeto (o desenho) na grade de pixels da tela, seguindo a
    /// posição do pai (a física) mais o seu deslocamento local de origem. Com
    /// a câmera também presa à grade e escala inteira, as bordas dos pixels da
    /// arte caem sempre em bordas de pixels da tela: o sprite não tremula nem
    /// "respira" ao andar.
    ///
    /// Só a apresentação é arredondada; o corpo físico, no pai, não é tocado,
    /// e deslocamentos visuais (espada na mão, efeito) são preservados.
    /// Roda depois do <see cref="CameraFollow"/>.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class PixelSnap : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;

        /// <summary>Deslocamento local desejado em relação ao pai (atualizado por quem posiciona o desenho).</summary>
        [SerializeField] private Vector3 localOffset;

        public Vector3 LocalOffset
        {
            get => localOffset;
            set => localOffset = value;
        }

        public void Configure(Camera cameraToMatch)
        {
            targetCamera = cameraToMatch;
            localOffset = transform.localPosition;
        }

        private void LateUpdate()
        {
            Transform parent = transform.parent;
            if (targetCamera == null || parent == null || Screen.height <= 0)
            {
                return;
            }

            float step = 2f * targetCamera.orthographicSize / Screen.height;
            Vector3 position = parent.TransformPoint(localOffset);
            transform.position = new Vector3(Mathf.Round(position.x / step) * step,
                Mathf.Round(position.y / step) * step, position.z);
        }
    }
}
