namespace Lithostride.EditorTools
{
    /// <summary>
    /// Medidas das pranchas de "Pack completo", tomadas por inspeção (faixas
    /// de alfa por linha e coluna, conferidas visualmente). Coordenadas em
    /// pixels da fonte, origem no canto de cima. Nada aqui é grade teórica:
    /// cada faixa é o intervalo [início, fim] medido de conteúdo, e as
    /// divisões ficam no meio das lacunas ou em costuras de alfa mínimo.
    /// </summary>
    public static class PackSpec
    {
        public const string PackFolder = "Pack completo";

        // ------------------------------------------------------------------ escala

        /// <summary>
        /// Pixel lógico do mundo (<see cref="Lithostride.Core.WorldScale"/>): toda
        /// a arte derivada fica nesta grade, 1 texel = 1 px lógico, 16 por unidade.
        /// Cada fonte tem o seu fator fonte → px lógico, registrado no manifesto.
        /// </summary>
        public const int LogicalPixelsPerUnit = Lithostride.Core.WorldScale.PixelsPerUnit;

        /// <summary>Ambiente (blocos, árvores, props, ícones): mesma grade do Kael.</summary>
        public const int EnvironmentPixelsPerUnit = Lithostride.Core.WorldScale.PixelsPerUnit;

        /// <summary>Boss: mesma grade.</summary>
        public const int BossPixelsPerUnit = Lithostride.Core.WorldScale.PixelsPerUnit;

        /// <summary>
        /// Boss: fonte → px lógico. Mantém a proporção boss/Kael da versão
        /// anterior (corpo de ~230 px da fonte ≈ 2,8 alturas visíveis do Kael):
        /// ~128 px lógicos, 8 unidades de envergadura.
        /// </summary>
        public const float BossSourceToLogical = 0.556f;

        /// <summary>Texels de um bloco na arte normalizada (16x16).</summary>
        public const int TileTexels = Lithostride.Core.WorldScale.PixelsPerUnit;

        // ------------------------------------------------------------------ personagem

        public const string KaelMaster = "Personagem/KAEL_MASTER.png";
        public const string KaelIdle = "Personagem/KAEL_IDLE_01.png";
        public const string KaelWalk = "Personagem/KAEL_WALK_01.png";
        public const string KaelRun = "Personagem/KAEL_RUN.png";
        public const string KaelJump = "Personagem/KAEL_JUMP.png";

        public const int KaelCellWidth = 128;
        public const int KaelCellHeight = 192;

        /// <summary>
        /// Pivô do Kael na célula 128x192: x no centro do corpo (sem a capa),
        /// y na base do pixel mais baixo das botas. Os pés estão na linha 180
        /// em todos os 27 quadros de parado, caminhada e corrida.
        /// </summary>
        public const int KaelPivotX = 72;
        public const int KaelFeetRow = 181;

        /// <summary>Broche do peito no quadro de referência (MASTER): âncora do tronco.</summary>
        public const float KaelBroochX = 82.4f;
        public const float KaelBroochY = 92.7f;

        /// <summary>
        /// Escala do pulo: a prancha foi desenhada 2,33x maior. Medida pela
        /// distância broche-pés da pose parada (203,5 px) contra a do MASTER (88,3).
        /// </summary>
        public const float KaelJumpScale = 88.3f / 203.5f;

        /// <summary>Poses do pulo em ordem de leitura; faixas medidas (x0, x1) por linha.</summary>
        public static readonly int[][] KaelJumpRows = { new[] { 9, 435 }, new[] { 461, 876 } };

        public static readonly int[][][] KaelJumpCols =
        {
            new[] { new[] { 78, 343 }, new[] { 484, 815 }, new[] { 928, 1253 }, new[] { 1385, 1699 } },
            new[] { new[] { 58, 353 }, new[] { 490, 803 }, new[] { 936, 1262 }, new[] { 1408, 1672 } }
        };

        /// <summary>
        /// Papel de cada pose do pulo (conferido na prancha): parado, preparação
        /// agachada, impulso, ápice encolhido, queda 1, queda 2, aterrissagem
        /// agachada, recuperação de pé. As poses no chão alinham pelos pés; as
        /// do ar, pelo broche.
        /// </summary>
        public static readonly string[] KaelJumpPoses =
            { "parado", "preparo", "impulso", "apice", "queda1", "queda2", "aterrissagem", "recuperacao" };

        public static readonly bool[] KaelJumpGrounded = { true, true, false, false, false, false, true, true };

        /// <summary>
        /// Mão da espada (a direita do Kael, do lado de trás do corpo quando ele
        /// olha para a direita) por quadro: centro do punho (x, y) na célula
        /// 128x192 da fonte. Medida em 2026-09-27 por detecção dos pixels de pele
        /// (CC8B66/EBAB7F/F7CB9F) com rastreamento entre quadros, conferida em
        /// recortes ampliados. Onde o punho some atrás do tronco (visível =
        /// false) a posição é estimada pelo arco do braço entre os quadros
        /// vizinhos; a espada fica atrás do corpo, então só a lâmina aparece.
        /// Ordem: parado 0..8, caminhada 0..8, corrida 0..8.
        /// </summary>
        public static readonly float[][] KaelHands =
        {
            new[] { 93f, 134f }, new[] { 93f, 131f }, new[] { 93f, 128f }, new[] { 92f, 131f }, new[] { 91f, 133f },
            new[] { 92f, 134f }, new[] { 93f, 133f }, new[] { 93.5f, 135f }, new[] { 94f, 136f },
            new[] { 93f, 134f }, new[] { 93f, 133f }, new[] { 92f, 132f }, new[] { 90f, 131f }, new[] { 87f, 135.5f },
            new[] { 84f, 134f }, new[] { 83f, 133f }, new[] { 85f, 132f }, new[] { 89f, 133f },
            new[] { 93f, 134f }, new[] { 94f, 132.5f }, new[] { 102f, 129f }, new[] { 111f, 113f }, new[] { 111.5f, 104f },
            new[] { 111f, 105.5f }, new[] { 104f, 112f }, new[] { 94.5f, 118f }, new[] { 90f, 126f }
        };

        /// <summary>O punho aparece no quadro (medido) ou está atrás do tronco (estimado). Mesma ordem de <see cref="KaelHands"/>.</summary>
        public static readonly bool[] KaelHandVisible =
        {
            true, true, true, true, true, true, true, true, true,
            true, true, true, true, true, false, false, false, false,
            true, true, true, true, true, true, true, true, false
        };

        /// <summary>
        /// Mão da espada nas 8 poses do pulo, em pixels do recorte de cada pose
        /// (área = faixa medida ± 6/7 px, como em <see cref="KaelJumpCols"/>).
        /// Visíveis: 0, 3, 5, 7. Escondidas (estimadas pelo broche): 1, 2, 4, 6.
        /// </summary>
        public static readonly float[][] KaelJumpHands =
        {
            new[] { 227f, 322f }, new[] { 251f, 373f }, new[] { 271f, 305f }, new[] { 277f, 242f },
            new[] { 262f, 239f }, new[] { 287f, 246f }, new[] { 243f, 347f }, new[] { 227.5f, 312.5f }
        };

        public static readonly bool[] KaelJumpHandVisible = { true, false, false, true, false, true, false, true };

        /// <summary>
        /// Ângulo da lâmina por quadro (graus a partir do eixo +x, anti-horário,
        /// virado à direita). Parado e caminhada: lâmina para cima e para a
        /// frente; na corrida acompanha o balanço do braço (punho alto, lâmina
        /// mais deitada); no pulo segue a pose. Múltiplos de 5 entre 20 e 70:
        /// cada um é uma variante da espada derivada da fonte já girada.
        /// </summary>
        public static readonly int[] KaelSwordAngles =
        {
            55, 55, 55, 55, 55, 55, 55, 55, 55,
            55, 55, 55, 55, 60, 65, 65, 60, 55,
            55, 50, 45, 30, 20, 20, 30, 45, 60
        };

        public static readonly int[] KaelJumpSwordAngles = { 55, 35, 40, 40, 35, 25, 40, 55 };

        /// <summary>Variantes da espada empunhada (graus).</summary>
        public static readonly int[] SwordAngles = { 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70 };

        // ------------------------------------------------------------------ blocos

        public const string Blocks1 = "Blocos/Blocos1.png";
        public const string Blocks2 = "Blocos/Blocos2.png";
        public const string Blocks3 = "Blocos/Blocos 3.png";

        /// <summary>Faixas de linha de cada prancha de blocos (alfa &gt;= 200).</summary>
        public static readonly int[][] Blocks1Rows =
        {
            new[] { 62, 182 }, new[] { 207, 323 }, new[] { 345, 470 }, new[] { 493, 621 }, new[] { 643, 749 },
            new[] { 771, 880 }, new[] { 902, 1014 }
        };

        public static readonly int[][][] Blocks1Cols =
        {
            Cols(30, 141, 164, 282, 305, 424, 447, 566, 589, 712, 734, 858, 880, 1000, 1022, 1142, 1164, 1284, 1306, 1418),
            Cols(30, 158, 180, 300, 321, 442, 465, 581, 603, 713, 733, 857, 880, 995, 1020, 1138, 1162, 1275, 1302, 1418),
            Cols(30, 153, 176, 295, 318, 436, 459, 581, 603, 718, 742, 857, 879, 998, 1022, 1140, 1161, 1279, 1303, 1418),
            Cols(30, 125, 180, 275, 319, 434, 471, 572, 612, 718, 748, 857, 885, 999, 1025, 1140, 1165, 1283, 1307, 1418),
            Cols(30, 140, 164, 280, 305, 422, 447, 565, 589, 709, 734, 855, 880, 998, 1022, 1140, 1164, 1283, 1306, 1418),
            Cols(30, 143, 166, 282, 305, 423, 447, 565, 589, 709, 733, 856, 880, 998, 1022, 1140, 1164, 1283, 1307, 1418),
            Cols(30, 142, 166, 282, 305, 423, 447, 565, 589, 709, 733, 856, 880, 998, 1022, 1140, 1164, 1283, 1306, 1418)
        };

        public static readonly int[][] Blocks2Rows =
        {
            new[] { 47, 158 }, new[] { 174, 287 }, new[] { 305, 426 }, new[] { 446, 569 }, new[] { 581, 691 },
            new[] { 701, 809 }, new[] { 820, 926 }, new[] { 937, 1043 }
        };

        public static readonly int[][][] Blocks2Cols =
        {
            Cols(29, 135, 161, 275, 300, 422, 443, 559, 581, 710, 729, 858, 881, 999, 1024, 1147, 1169, 1288, 1313, 1423),
            Cols(29, 153, 171, 288, 305, 423, 444, 559, 577, 696, 718, 839, 880, 998, 1024, 1145, 1169, 1283, 1306, 1426),
            Cols(30, 150, 171, 286, 305, 423, 443, 557, 578, 697, 719, 842, 863, 998, 1019, 1143, 1165, 1284, 1306, 1426),
            Cols(30, 146, 170, 278, 306, 423, 446, 556, 579, 697, 724, 842, 864, 985, 1010, 1128, 1153, 1277, 1302, 1417),
            Cols(29, 146, 167, 285, 306, 424, 448, 563, 587, 705, 729, 848, 873, 993, 1018, 1137, 1160, 1279, 1303, 1421),
            Cols(29, 145, 169, 286, 308, 424, 448, 563, 587, 705, 729, 848, 873, 992, 1018, 1136, 1160, 1279, 1304, 1421),
            Cols(29, 144, 170, 288, 310, 424, 449, 560, 585, 705, 728, 844, 873, 992, 1017, 1136, 1160, 1279, 1303, 1421),
            Cols(29, 144, 169, 288, 310, 425, 448, 561, 585, 705, 729, 848, 873, 993, 1017, 1136, 1160, 1279, 1303, 1421)
        };

        public static readonly int[][] Blocks3Rows =
        {
            new[] { 21, 127 }, new[] { 136, 226 }, new[] { 237, 323 }, new[] { 333, 425 }, new[] { 436, 521 },
            new[] { 531, 616 }, new[] { 628, 708 }, new[] { 720, 798 }, new[] { 810, 891 }, new[] { 902, 979 },
            new[] { 990, 1069 }
        };

        private static readonly int[][] Blocks3Grid = Cols(32, 121, 139, 228, 246, 336, 353, 442, 460, 550, 567, 659,
            676, 767, 783, 876, 893, 983, 1000, 1093, 1110, 1204, 1220, 1308, 1326, 1415);

        public static readonly int[][][] Blocks3Cols =
        {
            Blocks3Grid, Blocks3Grid, Blocks3Grid, Blocks3Grid, Blocks3Grid, Blocks3Grid, Blocks3Grid, Blocks3Grid,
            Blocks3Grid, Blocks3Grid,
            Cols(34, 116, 143, 222, 250, 332, 356, 436, 461, 543, 570, 651, 680, 763, 789, 870, 897, 978, 1003, 1084)
        };

        /// <summary>
        /// Pixels da fonte por pixel da arte, por prancha de blocos: o bloco de
        /// 108 a 117 px das pranchas 1 e 2 e o de 90 px da 3 têm o mesmo
        /// desenho em ~26 pixels de arte com a moldura.
        /// </summary>
        public const float Blocks12SourcePerTexel = 4.4f;
        public const float Blocks3SourcePerTexel = 3.46f;

        /// <summary>Linha 11 da prancha 3: ícones de energia (não são blocos).</summary>
        public const int Blocks3EnergyRow = 10;

        /// <summary>Recuo da moldura + chanfro, em fração do bloco, para tirar o interior.</summary>
        public const float InteriorInset = 0.09f;

        // ------------------------------------------------------------------ vegetação

        public static readonly string[] TreeSheets =
            { "Blocos/arvore 1.png", "Blocos/arvore2.png", "Blocos/arvore3.png", "Blocos/arvore4.png" };

        /// <summary>Faixas de linha de cada prancha de árvores.</summary>
        public static readonly int[][][] TreeRows =
        {
            new[] { new[] { 99, 337 }, new[] { 372, 614 }, new[] { 635, 904 }, new[] { 922, 1041 } },
            new[] { new[] { 92, 496 }, new[] { 526, 879 }, new[] { 901, 1028 } },
            new[] { new[] { 33, 875 }, new[] { 911, 1052 } },
            new[] { new[] { 21, 517 }, new[] { 525, 910 }, new[] { 923, 1064 } }
        };

        /// <summary>
        /// Bases (a plataforma de grama sob cada árvore), medidas nas 15 linhas
        /// de baixo de cada faixa. Cada base é uma árvore, toco ou tronco; as
        /// copas que se tocam são separadas por costura entre as bases.
        /// </summary>
        public static readonly int[][][][] TreeBases =
        {
            new[]
            {
                Cols(31, 143, 171, 292, 328, 453, 488, 650, 686, 826, 868, 1008, 1050, 1179, 1216, 1379),
                Cols(29, 190, 270, 420, 502, 665, 740, 908, 1017, 1159, 1218, 1379),
                Cols(60, 207, 295, 450, 563, 719, 752, 865, 887, 991, 1050, 1160, 1187, 1350),
                Cols(24, 174, 192, 368, 386, 561, 579, 719, 738, 965, 983, 1200, 1218, 1420)
            },
            new[]
            {
                Cols(72, 261, 306, 521, 608, 803, 887, 1101, 1194, 1372),
                Cols(69, 262, 325, 535, 622, 821, 907, 1142, 1190, 1381),
                Cols(25, 211, 226, 390, 406, 591, 607, 805, 820, 1090, 1105, 1221, 1236, 1415)
            },
            new[]
            {
                Cols(18, 194, 220, 401, 420, 596, 620, 828, 851, 1023, 1046, 1228, 1252, 1433),
                Cols(51, 278, 303, 515, 539, 727, 751, 951, 975, 1390)
            },
            new[]
            {
                Cols(44, 512, 529, 905, 921, 1369),
                Cols(13, 386, 460, 953, 984, 1435),
                Cols(16, 210, 217, 365, 377, 560, 570, 746, 755, 929, 938, 1430)
            }
        };

        /// <summary>Árvores: ~4 px da fonte por pixel da arte (período medido por autocorrelação).</summary>
        public const int TreeSourcePerTexel = 4;

        public const string GroundItems = "Blocos/itensNoChao.png";

        public static readonly int[][] GroundItemRows =
            { new[] { 80, 255 }, new[] { 330, 498 }, new[] { 592, 729 }, new[] { 804, 949 } };

        public static readonly int[][] GroundItemCols = Cols(40, 203, 288, 467, 540, 726, 785, 973, 1045, 1230, 1294, 1493);

        /// <summary>Itens do chão: ~6 px da fonte por pixel da arte.</summary>
        public const int GroundItemSourcePerTexel = 6;

        public static readonly string[] GroundItemNames =
        {
            "grama_baixa_a", "grama_baixa_b", "grama_media_a", "grama_media_b", "grama_alta_a", "grama_alta_b",
            "samambaia_a", "samambaia_b", "broto_a", "broto_b", "arbusto_a", "arbusto_b",
            "flores_a", "flores_b", "cogumelo_a", "cogumelo_b", "pedra_musgo_a", "pedra_musgo_b",
            "raizes_a", "raizes_b", "galho_a", "galho_b", "pedrinha_a", "pedrinha_b"
        };

        // ------------------------------------------------------------------ fundos

        /// <summary>Fundos únicos (os de Blocos/ são cópias idênticas, conferidas por SHA-256).</summary>
        public static readonly string[] Backgrounds = { "Fundo/fundo1.png", "Fundo/fundo2.png", "Fundo/fundo3.png" };

        public static readonly string[] BackgroundAliases = { "Blocos/fundo1.png", "Blocos/fundo2.png", "Blocos/fundo3.png" };

        /// <summary>Tema de cada fundo, conferido na imagem.</summary>
        public static readonly string[] BackgroundThemes = { "caverna", "entardecer", "dia" };

        // ------------------------------------------------------------------ arma e efeitos

        public const string Sword = "Armas/arma1.png";

        public static readonly string[] Effects =
        {
            "Armas/animacaoArma1.png", "Armas/animacaoArma1v2.png", "Armas/animacaoArma1v3.png",
            "Armas/animacaoArma1v4.png", "Armas/animacaoArma1v5.png"
        };

        public static readonly string[] EffectNames = { "corte", "energia", "descendente", "projetil", "explosao" };

        /// <summary>Janela (y0, y1) da costura entre as duas linhas de quadros de cada efeito.</summary>
        public static readonly int[][] EffectRowSeam =
        {
            new[] { 530, 640 }, new[] { 555, 610 }, new[] { 470, 640 }, new[] { 556, 702 }, new[] { 550, 598 }
        };

        /// <summary>Espada na fonte (arma1): centro do cabo, ponta da lâmina e fim do pomo, medidos em recortes ampliados.</summary>
        public static readonly float[] SwordGrip = { 322f, 1192f };

        public static readonly float[] SwordTip = { 1050f, 23f };
        public static readonly float[] SwordPommelEnd = { 190f, 1425f };

        /// <summary>Comprimento da espada derivada, do pomo à ponta, em px lógicos (~0,65 da altura visível do Kael).</summary>
        public const int SwordLengthPixels = 30;

        /// <summary>
        /// Comprimento da espada desenhada em cada efeito (pomo à ponta, quadro
        /// 0), em px da fonte. Projétil e explosão não têm espada e usam a
        /// escala do corte. Cada efeito é reduzido para a espada dele ter o
        /// tamanho da espada empunhada.
        /// </summary>
        public static readonly float[] EffectSwordSourceLength = { 428f, 446f, 519f, 428f, 428f };

        public static float EffectScale(int effect)
        {
            return SwordLengthPixels / EffectSwordSourceLength[effect];
        }

        /// <summary>
        /// Centro do cabo da espada em cada quadro dos golpes que a desenham
        /// (corte, energia, descendente), em px da prancha (origem no topo),
        /// medido em recortes ampliados (meio do trecho escuro entre a gema da
        /// guarda e a caixa do pomo). É o pivô do quadro derivado: preso à mão,
        /// o golpe gira em volta do punho. Nulo = efeito sem espada.
        /// </summary>
        public static readonly float[][][] EffectGrips =
        {
            new[]
            {
                new[] { 89f, 438f }, new[] { 578f, 459f }, new[] { 980f, 454f }, new[] { 1386f, 459f },
                new[] { 336f, 717f }, new[] { 696f, 700f }, new[] { 1067f, 699f }, new[] { 1384f, 705f }
            },
            new[]
            {
                new[] { 94f, 442f }, new[] { 459f, 457f }, new[] { 780f, 471f }, new[] { 1109f, 416f },
                new[] { 93.5f, 934f }, new[] { 427.7f, 959f }, new[] { 799f, 969f }, new[] { 1167f, 972f }
            },
            new[]
            {
                new[] { 143.4f, 405.8f }, new[] { 461.5f, 438.4f }, new[] { 826f, 211f }, new[] { 1203f, 195f },
                new[] { 184.4f, 733f }, new[] { 521f, 763f }, new[] { 813f, 918f }, new[] { 1228f, 866f }
            },
            null,
            null
        };

        /// <summary>
        /// Quadros de golpe em que a lâmina está atrás da cabeça ou do tronco
        /// (preparação do corte: lâmina para trás e para cima). São desenhados
        /// atrás do Kael, para a lâmina não passar por cima do rosto.
        /// </summary>
        public static readonly bool[][] EffectBehindBody =
        {
            new[] { true, true, false, false, false, false, false, false },
            null,
            null,
            null,
            null
        };

        // ------------------------------------------------------------------ boss

        public static readonly string[] BossSheets = { "Boss/bossv2.png", "Boss/boss.png" };

        public static readonly string[] BossVersionNames = { "v2", "v1" };

        public static readonly int[][][] BossRows =
        {
            new[] { new[] { 53, 228 }, new[] { 263, 412 }, new[] { 430, 620 }, new[] { 652, 795 }, new[] { 822, 987 } },
            new[] { new[] { 6, 196 }, new[] { 221, 369 }, new[] { 381, 587 }, new[] { 618, 765 }, new[] { 807, 993 } }
        };

        /// <summary>
        /// Faixas das colunas por linha (alfa &gt;= 200). Na v1, a linha 4 tem o
        /// sopro do quadro 4 ocupando a quinta célula: são 5 quadros de dragão,
        /// e o quadro 4 inclui o sopro inteiro.
        /// </summary>
        public static readonly int[][][][] BossCols =
        {
            new[]
            {
                Cols(21, 239, 279, 504, 530, 757, 780, 1011, 1048, 1273, 1307, 1521),
                Cols(16, 263, 268, 507, 525, 765, 784, 1017, 1040, 1275, 1291, 1521),
                Cols(23, 240, 287, 507, 554, 764, 801, 1034, 1044, 1253, 1304, 1523),
                Cols(19, 247, 268, 495, 520, 757, 789, 1014, 1042, 1267, 1301, 1517),
                Cols(20, 257, 274, 511, 545, 752, 800, 984, 1042, 1253, 1289, 1515)
            },
            new[]
            {
                Cols(10, 239, 266, 495, 529, 750, 785, 1017, 1040, 1279, 1314, 1524),
                Cols(9, 255, 268, 504, 517, 764, 782, 1028, 1041, 1278, 1291, 1529),
                Cols(14, 227, 272, 506, 546, 764, 818, 1038, 1065, 1267, 1308, 1525),
                Cols(8, 242, 261, 471, 507, 764, 775, 1280, 1297, 1516),
                Cols(17, 252, 269, 510, 541, 756, 785, 965, 1024, 1245, 1271, 1515)
            }
        };

        public static readonly string[] BossRowNames = { "pairar", "avancar", "mergulho", "ataque", "reacao" };

        /// <summary>Na v1, o sopro do quadro "ataque 4" começa aqui (x da fonte): recorte à parte como efeito.</summary>
        public const int BossV1BreathStartX = 1000;

        // ------------------------------------------------------------------ interface

        public const string HealthBar = "Boss/barraVida.png";

        /// <summary>Barras cheia, parcial e vazia: faixas medidas no trecho do canal (x 330..1400).</summary>
        public static readonly int[][] HealthBarRows = { new[] { 56, 307 }, new[] { 348, 597 }, new[] { 641, 891 } };

        /// <summary>A barra da interface fica na metade da resolução da fonte (~815 px de largura em 1080p).</summary>
        public const float HealthBarScale = 0.5f;

        // ------------------------------------------------------------------ utilitário

        /// <summary>Pares (início, fim) a partir de uma lista plana.</summary>
        private static int[][] Cols(params int[] flat)
        {
            int[][] result = new int[flat.Length / 2][];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new[] { flat[i * 2], flat[(i * 2) + 1] };
            }

            return result;
        }
    }
}
