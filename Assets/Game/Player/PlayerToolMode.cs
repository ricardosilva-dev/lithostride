using System;
using Lithostride.Core;
using UnityEngine;

namespace Lithostride.Player
{
    public enum ToolMode
    {
        Combat = 1,
        Mining = 2,
        Build = 3
    }

    /// <summary>
    /// Modo da mão do jogador (teclas 1, 2 e 3): combate, mineração ou
    /// colocação de blocos de teste. O clique esquerdo faz uma coisa só,
    /// conforme o modo — nunca ataca e minera ao mesmo tempo.
    /// </summary>
    public sealed class PlayerToolMode : MonoBehaviour
    {
        [SerializeField] private GameInput input;
        [SerializeField] private ToolMode mode = ToolMode.Combat;

        public ToolMode Mode => mode;

        public event Action<ToolMode> Changed;

        public void Configure(GameInput gameInput)
        {
            input = gameInput;
        }

        public void SetMode(ToolMode next)
        {
            if (next == mode)
            {
                return;
            }

            mode = next;
            Changed?.Invoke(mode);
        }

        private void Update()
        {
            if (input != null && input.ModeKey != 0)
            {
                SetMode((ToolMode)input.ModeKey);
            }
        }
    }
}
