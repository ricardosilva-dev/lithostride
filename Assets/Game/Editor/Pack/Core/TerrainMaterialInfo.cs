namespace Lithostride.EditorTools
{
    /// <summary>De onde vem uma amostra de terreno.</summary>
    public enum TerrainBoard
    {
        Blocos1 = 1,
        Blocos2 = 2,
        Blocos3 = 3,
        TerraMineral = 17,
        TerraRaizes = 18,
        PedraNatural = 19
    }

    /// <summary>Uma amostra: prancha, linha e coluna (base 0) e recuo extra do topo (fração, só nas pranchas do pack).</summary>
    public sealed class TerrainSampleRef
    {
        public TerrainBoard Board;
        public int Row;
        public int Col;
        public float TopInset;

        public TerrainSampleRef(TerrainBoard board, int row, int col, float topInset = 0f)
        {
            Board = board;
            Row = row;
            Col = col;
            TopInset = topInset;
        }
    }

    /// <summary>Definição de um material de terreno: de onde vêm as amostras de cada camada e como se comporta.</summary>
    public sealed class TerrainMaterialInfo
    {
        /// <summary>Id estável (nome de arquivo e de tile).</summary>
        public string Id;

        public string DisplayName;

        /// <summary>Cobertura que cresce no topo exposto ("grama", "musgo") ou vazio.</summary>
        public string Cap = "";

        /// <summary>Segundos segurando o botão para quebrar.</summary>
        public float BreakTime = 0.4f;

        /// <summary>Ícone de energia (0..9 da linha 11 de Blocos 3) mostrado ao quebrar; -1 = nenhum.</summary>
        public int DropIcon = -1;

        /// <summary>
        /// Prioridade na fronteira entre materiais: o de prioridade maior
        /// avança com uma franja irregular sobre o vizinho. 0 = material
        /// construído (alvenaria, madeira, arenito): fronteira reta, sem franja.
        /// </summary>
        public int Priority;

        /// <summary>Amostras de cada uma das camadas (variantes contínuas) do material.</summary>
        public TerrainSampleRef[][] Layers;
    }
}
