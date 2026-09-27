namespace Lithostride.Core
{
    /// <summary>
    /// Escala única do mundo. Separa quatro coisas que antes se misturavam:
    /// <list type="number">
    /// <item>pixels do arquivo de origem — cada prancha tem a sua densidade,
    /// e o pipeline converte com um fator por categoria (registrado no manifesto);</item>
    /// <item>pixels lógicos da arte final: toda arte de mundo é derivada nesta
    /// grade, 1 texel = 1 pixel lógico, <see cref="PixelsPerUnit"/> por unidade;</item>
    /// <item>unidade física: 1 unidade = 1 célula do terreno;</item>
    /// <item>câmera: pixels de tela por pixel lógico, inteiro, pela altura da
    /// tela (<see cref="BasePixelScale"/>).</item>
    /// </list>
    /// Só constantes, sem estado: o pipeline de arte (editor e bancada fora da
    /// Unity), o construtor de cena e o jogo leem os mesmos números.
    /// </summary>
    public static class WorldScale
    {
        /// <summary>Pixels lógicos por unidade (e por célula): a célula tem 16x16 pixels lógicos.</summary>
        public const int PixelsPerUnit = 16;

        /// <summary>Tamanho da célula do terreno em unidades.</summary>
        public const float CellSize = 1f;

        /// <summary>Um pixel lógico em unidades.</summary>
        public const float Pixel = 1f / PixelsPerUnit;

        /// <summary>Altura de tela em que a escala da câmera é 1 (um pixel de tela por pixel lógico).</summary>
        public const int ReferenceScreenHeight = 1080;

        // ------------------------------------------------------------------ Kael

        /// <summary>
        /// Altura visível do Kael em repouso, em pixels lógicos (dos pés à ponta
        /// do cabelo): 46 px = 2,875 células. O MASTER tem 165 px opacos; o
        /// derivado é reduzido por 46/165.
        /// </summary>
        public const int KaelVisiblePixels = 46;

        /// <summary>Altura opaca do MASTER na fonte (pixels), medida.</summary>
        public const int KaelSourceVisiblePixels = 165;

        /// <summary>Fator fonte → pixel lógico do Kael (parado, caminhada, corrida).</summary>
        public const float KaelSourceToLogical = (float)KaelVisiblePixels / KaelSourceVisiblePixels;

        /// <summary>
        /// Colisor do corpo: tronco e pernas, sem capa nem pontas do cabelo.
        /// Medido no MASTER: tronco de x 46 a 92 (46 px) e dos pés (linha 181)
        /// ao alto do crânio (linha 32, 149 px), vezes o fator do Kael.
        /// </summary>
        public const float KaelColliderWidth = 0.80f;

        public const float KaelColliderHeight = 2.60f;

        /// <summary>Folga entre a base da cápsula e os pés, para não nascer encostando no chão.</summary>
        public const float KaelColliderLift = 0.02f;

        /// <summary>Altura do peito (origem de mineração, construção e mira), dos pés.</summary>
        public const float KaelChestHeight = 1.65f;

        /// <summary>
        /// Centro do pixel lógico que contém a coordenada (em unidades, relativa
        /// a um pivô que está numa quina de pixel). Sprites com o pivô no centro
        /// de um pixel (espada, golpes) postos aqui ficam na mesma grade de
        /// pixels do Kael, sem meio pixel de desvio.
        /// </summary>
        public static float PixelCenter(float units)
        {
            return ((float)System.Math.Floor(units * PixelsPerUnit) + 0.5f) / PixelsPerUnit;
        }

        // ------------------------------------------------------------------ câmera

        /// <summary>
        /// Escala inteira da câmera (pixels de tela por pixel lógico) para uma
        /// altura de tela: 1 em 720p e 1080p, 2 em 2160p. Inteira para cada
        /// pixel da arte virar um bloco exato de pixels da tela.
        /// </summary>
        public static int BasePixelScale(int screenHeight)
        {
            int scale = (int)System.Math.Round((double)screenHeight / ReferenceScreenHeight);
            return scale < 1 ? 1 : scale;
        }
    }
}
