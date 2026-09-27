namespace Lithostride.EditorTools
{
    /// <summary>
    /// Medidas dos lotes novos de "Arte pendente" (lote 01, complementos, e
    /// lote 02, harmonia), tomadas por inspeção: ilhas de alfa &gt;= 200,
    /// autocorrelação para o tamanho aparente do pixel e recortes ampliados.
    /// As grades pedidas nos prompts dos lotes eram só intenção de
    /// composição; aqui ficam os retângulos medidos. Coordenadas em pixels da
    /// fonte, origem no topo.
    /// </summary>
    public static class HarmoniaSpec
    {
        public const string PendingFolder = "Arte pendente/";

        /// <summary>Pastas dos lotes (fontes somente leitura), relativas ao projeto.</summary>
        public static readonly string[] Batches = { Lote01, Lote02 };

        public const string Lote01 = "Arte pendente/Codex_Lote_01_2026-09-26/";
        public const string Lote02 = "Arte pendente/Codex_Lote_02_Harmonia_2026-09-27/";

        public const string TerraMineral = Lote02 + "17_terra_mineral_variacoes.png";
        public const string TerraRaizes = Lote02 + "18_terra_raizes_e_estratos.png";
        public const string PedraNatural = Lote02 + "19_pedra_natural_variacoes.png";
        public const string GramaBordas = Lote02 + "20_grama_bordas_naturais.png";
        public const string ArvoresMedias = Lote02 + "21_arvores_bosque_medias.png";
        public const string ArvoresAntigas = Lote02 + "22_arvores_antigas_grandes.png";

        // ------------------------------------------------------------------ amostras de terreno (17, 18, 19)

        /// <summary>Faixas (início, fim inclusive) das colunas e linhas de amostras, por prancha 17, 18, 19.</summary>
        public static readonly int[][][] SampleCols =
        {
            new[] { new[] { 42, 255 }, new[] { 289, 503 }, new[] { 538, 751 }, new[] { 784, 997 }, new[] { 1033, 1246 }, new[] { 1281, 1494 } },
            new[] { new[] { 48, 243 }, new[] { 297, 492 }, new[] { 545, 741 }, new[] { 795, 990 }, new[] { 1044, 1240 }, new[] { 1294, 1488 } },
            new[] { new[] { 45, 259 }, new[] { 289, 507 }, new[] { 538, 752 }, new[] { 783, 998 }, new[] { 1028, 1246 }, new[] { 1276, 1491 } }
        };

        public static readonly int[][][] SampleRows =
        {
            new[] { new[] { 50, 253 }, new[] { 287, 492 }, new[] { 526, 730 }, new[] { 765, 968 } },
            new[] { new[] { 51, 241 }, new[] { 293, 484 }, new[] { 536, 723 }, new[] { 777, 964 } },
            new[] { new[] { 45, 255 }, new[] { 284, 494 }, new[] { 522, 725 }, new[] { 753, 965 } }
        };

        /// <summary>
        /// Pixels da fonte por texel (tamanho aparente do pixel, pico da
        /// autocorrelação): 17 tem pixel de 8 px; 18 e 19, de ~5,33 px.
        /// </summary>
        public static readonly double[] SampleTexel = { 8.0, 16.0 / 3.0, 16.0 / 3.0 };

        /// <summary>Recuo das bordas borradas de cada amostra, em px da fonte.</summary>
        public const int SampleInset = 10;

        /// <summary>
        /// O que há em cada linha (conferido na prancha): 17 — 1 e 2 terra
        /// mineral lisa com pontos laranja, 3 com pedrinhas cinza, 4 seca
        /// (mais clara e amarelada nas colunas 1 e 5); 18 — 1 raízes e musgo,
        /// 2 terra escura compacta, 3 estratos em zigue-zague, 4 cascalho;
        /// 19 — 1 pedra lisa, 2 pedra com fissuras, 3 pedra com musgo, 4
        /// pedra azul profunda com pontos dourados.
        /// </summary>
        public static readonly string[][] SampleRowNames =
        {
            new[] { "terra_lisa_a", "terra_lisa_b", "terra_pedrinhas", "terra_seca" },
            new[] { "terra_raizes", "terra_escura", "terra_estratos", "cascalho" },
            new[] { "pedra_lisa", "pedra_fissuras", "pedra_musgo", "pedra_profunda" }
        };

        // ------------------------------------------------------------------ grama (20)

        /// <summary>
        /// Plataformas planas das linhas 1 e 2 da prancha 20 (caixa medida por
        /// ilha, alfa &gt;= 128 com união de 3 px). Delas sai só a faixa de
        /// cima (tufos + grama + franja), o trecho do meio, sem as pontas
        /// arredondadas nem a terra de baixo.
        /// </summary>
        public static readonly int[][] GrassPlatforms =
        {
            new[] { 27, 139, 248, 276 }, new[] { 271, 145, 504, 277 }, new[] { 529, 139, 756, 265 },
            new[] { 781, 149, 1005, 273 }, new[] { 1028, 145, 1263, 285 }, new[] { 1285, 145, 1511, 269 },
            new[] { 30, 347, 248, 476 }, new[] { 273, 338, 505, 473 }, new[] { 529, 340, 756, 465 },
            new[] { 781, 331, 1005, 465 }, new[] { 1029, 347, 1263, 472 }, new[] { 1279, 342, 1506, 472 }
        };

        /// <summary>Linhas 3 e 4 da prancha 20 (degraus e pontas): fonte de referência, não repetidas como chão.</summary>
        public static readonly int[][] GrassEdgePieces =
        {
            new[] { 29, 561, 248, 713 }, new[] { 273, 547, 505, 698 }, new[] { 530, 548, 757, 707 },
            new[] { 781, 561, 1009, 707 }, new[] { 1038, 549, 1263, 698 }, new[] { 1282, 549, 1511, 701 },
            new[] { 28, 776, 213, 938 }, new[] { 290, 776, 473, 939 }, new[] { 529, 780, 739, 938 },
            new[] { 793, 779, 999, 926 }, new[] { 1063, 775, 1232, 922 }, new[] { 1321, 774, 1501, 958 }
        };

        public const double GrassTexel = 16.0 / 3.0;
    }
}
