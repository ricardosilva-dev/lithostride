using UnityEngine;
using UnityEngine.InputSystem;

namespace Lithostride.Core
{
    /// <summary>Esc fecha o jogo: o mapa de teste não tem menu.</summary>
    public sealed class QuitOnEscape : MonoBehaviour
    {
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Application.Quit();
            }
        }
    }
}
