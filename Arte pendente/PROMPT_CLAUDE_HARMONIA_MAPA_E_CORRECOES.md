# Prompt para o Claude — LITHOSTRIDE: escala, movimento, terreno natural e integração de arte

Copie este documento inteiro para o Claude que trabalha no projeto. Ele é uma nova tarefa sobre a implementação existente; substitui as metas visuais do prompt anterior. Os arquivos PNG citados são fontes novas para preparar, recortar e integrar, não sprites já homologados.

---

Trabalhe em C:\Users\Ricardo\Documents\Projetos\lithostride.

Quero que você implemente e teste as correções abaixo, usando o jogo existente. Não pare no plano nem entregue apenas um gerador que não foi executado. Preserve alterações locais e o trabalho já feito. Leia CLAUDE.md, os documentos de visão, arquitetura, decisões e o guia Documentation/Technical/PACK_COMPLETO_TESTE.md; confronte documentos com o código atual. Não reverta alterações de outras tarefas. Se algum arquivo tiver mudado desde este diagnóstico, adapte a solução ao estado real.

## 1. Resultado que quero ver

O jogo precisa parecer um mundo de exploração 2D coeso: Kael menor, árvores com proporções naturais, células pequenas, terra variada sem ladrilhos aparentes, morros assimétricos, depressões, cavernas e vegetação integrada ao solo. A referência de sensação é um sandbox como Terraria; mantenha a identidade do LITHOSTRIDE e as artes próprias.

Corrija também a espada que parece solta da mão e o pulo que está falhando. Quero jogar a demonstração pronta e acessar uma galeria de testes separada. Integre o Pack completo original e os dois novos lotes, com cobertura rastreável de cada imagem.

As capturas enviadas mostram zoom 0,33×. Meça as proporções também no zoom inicial real; não conclua que tamanho em tela equivale a tamanho no mundo. Não resolva a tarefa apenas mudando o zoom.

## 2. Fontes de arte disponíveis

Preserve estas pastas como originais, trabalhando com cópias derivadas:

- Pack completo — 28 PNGs, 25 conteúdos distintos no inventário anterior.
- Arte pendente\Codex_Lote_01_2026-09-26 — 16 pranchas complementares.
- Arte pendente\Codex_Lote_02_Harmonia_2026-09-27 — 6 pranchas para terreno e árvores.

Nos lotes novos, leia CATALOGO.md, manifesto.json, LEIA_PRIMEIRO.md e PROMPTS.md. As grades registradas são intenções de composição, não coordenadas finais de recorte. Todos esses PNGs foram gerados por image_gen; inspecione-os antes de importar.

| PNG novo | Conteúdo / destino |
|---|---|
| 01_arbustos_e_samambaias | Arbustos e samambaias; distribuição por agrupamentos |
| 02_flores_e_ervas | Flores e ervas; clareiras, bordas e manchas esparsas |
| 03_fungos_da_caverna | Cogumelos de cavernas e zonas úmidas |
| 04_cipos_e_raizes | Cipós e raízes; pendurados em suporte superior |
| 05_rochas_da_caverna | Rochas, estalagmites e estalactites; separar pivôs por orientação |
| 06_cristais_e_geodos | Cristais e geodos; veios e bolsões minerais |
| 07_ruinas_minerais | Props de ruína; não presumir peças modulares |
| 08_acampamento | Caixas, barris e objetos de acampamento |
| 09_baus_estados | Quatro tipos de baú com estados fechado, intermediário e aberto |
| 10_tochas_e_lanternas | Sequências de tocha, lanterna e luminária de cristal |
| 11_bancadas_e_estacoes | Estações e bancadas como props de teste |
| 12_recursos_coletaveis | Ícones de recursos para drops, inventário de teste e galeria |
| 13_morcego_cristal | Sequências de criatura voadora pequena |
| 14_besouro_mineral | Sequências de criatura terrestre |
| 15_gosma_de_musgo | Sequências de gosma e salto |
| 16_impactos_e_poeira | Efeitos curtos de terra, pedra, cristal e poeira |
| 17_terra_mineral_variacoes | 24 amostras visuais de terra para enriquecer o material |
| 18_terra_raizes_e_estratos | 24 amostras de terra, raízes, estratos e inclusões |
| 19_pedra_natural_variacoes | 24 amostras de pedra, fissuras, musgo e inclusões |
| 20_grama_bordas_naturais | 24 acabamentos de superfície; extrair grama e transições úteis |
| 21_arvores_bosque_medias | Oito árvores médias, com tronco e copa completos |
| 22_arvores_antigas_grandes | Oito árvores grandes, com copas altas |

O pack original continua necessário: MASTER/IDLE/WALK/RUN/JUMP do Kael; Blocos1/2/3 e todos os materiais; quatro pranchas de árvores; itensNoChao; três fundos; espada; cinco pranchas de ataques; boss/bossv2 e barraVida. Evite importar duas vezes os fundos idênticos de Blocos e Fundo. Mantenha as versões do boss selecionáveis no teste.

“Integrar tudo” significa não ignorar uma fonte silenciosamente: todas devem constar no manifesto e ter uso no mapa, na galeria ou no visualizador apropriado. Não significa colocar cada sprite no mesmo pedaço do mundo. Se uma célula gerada for inadequada, registre qual, por quê e como foi corrigida/substituída; não esconda perda de cobertura.

## 3. Diagnóstico existente: conferir antes de alterar

O guia atual declara a implementação incompleta. Registra pulo com altura medida zero na entrada simulada, boss ainda sem validação de voo/ataques, roteiro preso na galeria e teletransportes dentro de blocos. Não reutilize a frase “pulo ~4,2 unidades” como evidência de que funciona.

Pontos concretos encontrados:

- Assets/Game/Editor/Pack/Core/PackSpec.cs usa LogicalPixelsPerUnit = 48, EnvironmentPixelsPerUnit = 24, TileTexels = 24, TreeSourcePerTexel = 4 e SwordScale = 0.075.
- A documentação descreve Kael com altura visível de 3,44 unidades, bloco de 1 unidade e colisor de 0,95 × 3,1.
- VegetationArt.cs reduz árvores por um fator fixo. Isso não estabelece a altura visual correta de cada classe de árvore.
- KaelArt.cs usa coordenadas de mão por quadro para as sequências de chão, mas no pulo calcula a mão por um deslocamento aproximado do broche. Meça a mão real de cada pose.
- SwordHolder.cs já tenta acompanhar a âncora. PlayerAnimator.cs já espelha coordenadas e há PixelSnap no conjunto visual. Investigue os espaços de coordenadas e a ordem de atualização; não adicione um segundo sistema de espada nem espelhamento duplicado.
- TerrainRules.MacroBlocks é 4 e usa módulo das coordenadas. O quilting de 4×4 repete visualmente. Variar só os contornos não resolve o miolo repetitivo.
- PackLevelLayout.cs é um percurso de testes com trechos fixos, paredes, faixas de materiais e rampas. Não o apresente como geração natural.

Esses são pontos de investigação, não uma causa comprovada para cada bug. Preserve os recursos que já funcionam: TerrainCellMap/TerrainGrid, mineração e construção, suporte de props, seleção de materiais, controles e diagnósticos.

## 4. Uma escala coerente para tudo

Crie uma configuração central de escala e use medidas, não ajustes espalhados sem relação. Separe:

1. pixels do arquivo de origem;
2. pixels lógicos desejados da arte final;
3. unidade física / tamanho da célula;
4. escala de câmera e resolução de saída.

Use como ponto de partida células com aparência de 16×16 pixels lógicos e Kael com cerca de 40–48 pixels lógicos de altura visível em repouso (2,5–3 células). Esses números são metas propostas para este jogo, não especificações exatas de Terraria. Ajuste após comparar capturas, preservando o personagem e sua leitura.

Se o sprite fonte tiver mais pixels, normalize PPU/derivado/transform de forma explícita; não confunda altura do canvas com a altura opaca do personagem. Não reduza a arte repetidamente nem destrua rosto, mãos ou espada com filtros suaves.

No enquadramento normal de 1920×1080, comece buscando Kael em torno de 45–65 pixels de altura e células em torno de 16–22 pixels de largura, verificando a proporção combinada. Registre os valores finais de câmera, PPU, altura visível e colisor. Em 1280×720 e outra proporção, preserve a legibilidade e a proporção relativa.

Referências de tamanho em relação à altura visível de Kael:

- Mudas e árvores jovens: aproximadamente 1,5–2,5 alturas, usadas como vegetação jovem.
- Árvores comuns adultas: 3–5 alturas.
- Árvores antigas grandes: 6–9 alturas, menos frequentes.
- Ervas e pequenos tufos: aproximadamente 0,1–0,35 altura.
- Arbustos: aproximadamente 0,3–0,8 altura, conforme a espécie.

Não transforme toda árvore comum em uma planta do tamanho do personagem. Use as fontes antigas e novas por categoria, ajustando proporções a partir dos pixels visíveis. Varie escala moderadamente dentro da categoria, sem esticar só um eixo.

Recalibre colisor, alcance de interação, espada, efeitos, boss, velocidade, gravidade, altura de salto, autodegrau, distância de snap e posições de spawn em unidades coerentes. O colisor deve acompanhar tronco e pernas; capa/cabelo não definem a colisão do corpo. Não mude só o renderer deixando uma hitbox gigante invisível.

## 5. Preparação da arte: recorte e transparência

Estenda o pipeline existente em Assets/Game/Editor/Pack. Salve derivados em pastas identificáveis, preserve nomes/IDs estáveis e registre fonte, hash, retângulo, pivô, PPU, categoria, sequência, duração e âncoras por sprite.

As pranchas têm margens, alinhamentos e tamanhos diferentes. Detecte/inspecione cada região e salve coordenadas explícitas. Não divida cegamente largura e altura pelo número de colunas/linhas. Alfa residual pode unir componentes separados; agrupamento automático precisa de revisão visual.

Alguns PNGs possuem muitos pixels sólidos com alfa próximo de 251–253 e resíduos muito fracos no entorno. Faça limpeza em derivados de acordo com a categoria. Preserve transparência e brilho legítimos dos efeitos; não apague cores escuras do personagem, terra ou pedra como se fossem fundo. Verifique sobre fundo claro, escuro e quadriculado.

Use Point/Nearest, sem compressão destrutiva e sem mipmaps onde apropriado. Mantenha margens/extrusão no atlas para evitar vazamento. O filtro e o tamanho de exportação devem preservar a pixel art. Evite halos, sombras pintadas de apresentação e plataformas ornamentais na base das árvores.

Árvores e plantas devem tocar o chão na raiz, com pequena sobreposição controlada da grama. Cipós e estalactites precisam de pivô e suporte no teto. Remova apenas bases decorativas artificiais dos derivados, sem cortar raízes úteis.

## 6. Muito mais variedade de solo, com encaixe real

As 72 amostras dos arquivos 17–19 são matéria-prima visual. Elas NÃO são um conjunto Wang/autotile pronto e nem uma garantia de repetição sem emenda. O arquivo 20 também contém trechos que precisam de extração e adaptação: não use as pequenas plataformas desenhadas como solo completo repetido.

Prepare um sistema de materiais contínuos a partir dessas fontes e do pack:

- Terra com base predominante discreta; variantes secas, compactadas, úmidas, orgânicas, com raízes, cascalho e estratos. Evite que cada célula pareça uma pedra redonda com contorno preto.
- Pedra com massas e fissuras maiores, misturando áreas calmas e inclusões. Minério deve formar bolsões e veios coerentes.
- Variação em várias escalas: detalhes pequenos, manchas médias e regiões geológicas maiores. Não crie tabuleiro de cores nem uma mesma textura a cada quatro blocos.
- Transição irregular entre terra e pedra, espessura de solo variável e regiões úmidas associadas a cavernas/depressões.
- Contorno escuro somente nas superfícies expostas apropriadas. Entre duas células sólidas do mesmo material não pode aparecer moldura, sombra de borda ou fresta.
- Grama na superfície exposta; adaptar pontas, cantos internos/externos, pequenos degraus e trechos inclinados. Não pintar grama em cada bloco subterrâneo.
- Ramos de raízes, pequenas pedras e manchas não precisam existir em toda célula. Distribua por densidade e contexto.

Escolha e implemente uma técnica determinística de continuidade: materiais/macrorregiões maiores construídos com bordas compatíveis, Wang tiles verificados ou outra solução de qualidade equivalente. Não sorteie recortes incompatíveis e apenas torça para as bordas coincidirem. Não espelhe detalhes direcionais, grama e iluminação aleatoriamente.

Prepare a topologia necessária para topo, base, lados, cantos, concavidades, blocos isolados, ilhas e conexões estreitas. Se usar máscaras de vizinhança, valide os padrões realmente suportados; não afirme que possui 256 formas apenas porque há 256 índices. Visual, colisão e célula lógica precisam concordar.

Use uma cena de inspeção com vizinhanças, transições e uma área extensa de material. Teste adjacências horizontal/vertical e encontro de quatro células em escala real e ampliada. Corrija bordas no derivado, mantendo originais. Zero gaps de geometria e zero linhas de fundo entre sólidos.

Mineração/construção devem atualizar material, bordas, grama e suportes na vizinhança local sem reconstruir o mundo inteiro. A seleção de variantes deve permanecer estável pela seed/coordenadas, inclusive após carregar a área de novo.

## 7. Um mapa natural e jogável

Crie uma cena de exploração, por exemplo Assets/Scenes/Tests/ExploracaoHarmonica.unity, e mantenha PackCompletoTest/galeria como ambiente de QA separado. O comando de testar/jogar deve abrir por padrão a exploração, com acesso explícito à galeria.

Gere um mundo determinístico por seed usando múltiplas escalas de relevo. Combine clareiras de tamanho variado, colinas assimétricas, vales, depressões rasas, platôs irregulares, passagens e cavernas. Quantize na grade mantendo o perfil natural. Não use uma onda senoidal repetida nem ruído independente por coluna.

As elevações não podem ser sempre triângulos perfeitos, escadas de ritmo constante ou rampas longas de 45 graus. Use mudanças de inclinação, trechos quase planos, ombros de colina, pequenos degraus e afloramentos. Rampas podem existir localmente quando fizerem sentido para o sistema, sem dominar todo morro.

Comece numa área segura e legível, com espaço para andar e testar salto. Garanta uma rota principal acessível e desafios opcionais. O relevo deve respeitar a capacidade real do personagem após corrigir o pulo. Posicione árvores grandes onde troncos e raízes tenham apoio suficiente; evite árvores equilibradas em um único pixel de quina.

Faça cavernas com túneis curvos/irregulares, câmaras de tamanhos diferentes, pilares, espessura mínima de paredes/tetos e conexões úteis. Não use apenas um retângulo cavado ou corredor diagonal perfeito.

Preencha o subsolo até além da área alcançável/visível da câmera ou use continuidade/chunks apropriados. Não deixar o mundo como uma placa rasa flutuando com céu aparecendo imediatamente sob a última linha de blocos. Cavernas precisam de camada de fundo/parede apropriada; a paisagem de céu distante não deve aparecer em todo buraco subterrâneo.

Remova a parede artificial gigante junto ao spawn da exploração. Limites de mundo e câmera devem impedir ver cortes laterais artificiais. Se houver falésia natural intencional, componha a continuidade visual da rocha.

Distribua flora por conjuntos e clareiras, sem fileira equidistante de props. Diferencie chão seco, úmido e cavernas; use pequenas pedras e fungos com parcimônia. O fundo deve ter menos contraste que o plano jogável. Preserve os três fundos do pack com transições coerentes; só use parallax independente onde existirem camadas reais, sem fingir que uma imagem achatada já foi separada.

Tijolos, madeira, arenito trabalhado e estações devem aparecer em ruínas/acampamentos construídos ou na galeria, não como faixas verticais aleatórias no solo natural. Integre a fantasia mineral e o colosso do projeto com afloramentos e ruínas coerentes.

## 8. Espada realmente presa à mão

Meça e registre a posição da mão em CADA quadro de idle, walk, run e jump. O deslocamento aproximado a partir do broche nas poses aéreas deve ser substituído por dados revisados. O pivô da espada fica no centro da empunhadura que a mão segura.

Converta coordenadas fonte → recorte → pivô → PPU → transformação local de forma explícita. Verifique o espelhamento uma única vez, local/world space, parent visual versus root físico, escala do filho, ordem de animação e pixel snap. Não acumule correções mágicas por script.

Faça a arma acompanhar mão, orientação e pose; desenhe partes atrás/à frente conforme necessário para parecer segurada. Evite cabo sobre a capa ou lâmina passando pelo rosto. Quadros que já tiverem arma desenhada não devem receber uma segunda espada.

Aplique a mesma âncora consistente aos ataques, separando posição visual, direção e hitbox. Efeitos grandes não justificam dano atrás do personagem nem golpes múltiplos acidentais. Preserve as cinco famílias de efeitos originais.

Crie um comparador quadro a quadro com marcação de mão/empunhadura nos dois sentidos. A diferença visual deve ficar no máximo na ordem de 1–2 pixels lógicos nas poses avaliadas. Capture idle, corrida, subida, ápice, queda e ataque.

## 9. Corrigir o pulo pela causa real

Inspecione Assets/Game/Core/GameInput.cs, Player/PlayerController.cs e Player/PlayerAnimator.cs. Reproduza com entrada real e com o roteiro. Registre temporariamente tecla, consumo de buffer, coyote time, fase de preparo, contatos/normais, IsGrounded, velocidade vertical, deslocamento, snap e autodegrau por tick relevante.

Investigue sem presumir:

- WasPressedThisFrame em Update sendo perdido antes do FixedUpdate;
- foco/UI/pausa bloqueando a entrada;
- preparo consumindo o buffer ou reiniciando a cada tick;
- contato antigo mantendo grounded logo após aplicar impulso;
- ramo de chão zerando velocidade positiva;
- snap/autodegrau recolocando o corpo no chão durante a subida;
- colisores, camadas, gravidade ou constraints incorretos;
- animação aérea confundida com deslocamento físico;
- erro do próprio teste de entrada.

Corrija a menor causa comprovada e documente o antes/depois. Buffer e coyote na faixa de 80–120 ms são bons valores iniciais; ajuste ao jogo. Comece com salto alcançando aproximadamente 3–4,5 blocos e valide o arco após a nova escala. Não deixe a antecipação visual tornar o comando travado.

Manter o salto deve ter comportamento definido; um pressionamento não pode gerar saltos infinitos. Se implementar altura variável, teste toque curto e pressionamento longo. Subir, ápice, cair e aterrissar devem acompanhar estado físico, com aterrissagem emitida uma vez.

Snap deve servir para acompanhar o terreno na descida apropriada, sem prender a subida. Autodegrau deve ter altura máxima coerente, sem virar escalada automática de paredes ou teletransporte através do teto. Valide contato em ambos os sentidos de rampas e nos cantos.

Teste spawn/teleporte usando busca de espaço livre e chão válido. Analise a observação do CompositeCollider em Outlines antes de decidir por Polygons; confirme resultado e custo. Nunca gere o personagem dentro de sólidos para “testar” a colisão normal.

## 10. Uso dos novos props, criaturas, animações e efeitos

Recorte todos os lotes com IDs estáveis e faça visualizadores por família. Ajuste baseline/pivô das sequências antes de animar; geração por IA pode variar volume, centro e detalhes entre quadros. Não estique cada frame para uma bounding box diferente.

- Baús: estados fechado/intermediário/aberto, interação simples para teste e visualização de todos os tipos.
- Tochas/lanternas: sequência curta com emissivo/luz discretos, sem criar centenas de luzes caras; organizar por tipo.
- Bancadas/ruínas/acampamento: props coerentes e inspecionáveis; não inventar um sistema completo de crafting para usar suas imagens.
- Recursos: drops e inventário de teste simples, com ícones legíveis. O manifesto distingue conceito ilustrado e recurso efetivamente funcional.
- Morcego, besouro e gosma: protótipos leves de movimento/estados em área de testes, com dano/retorno de estado suficiente para ver as sequências; não trocar o boss por esses inimigos.
- Impactos: efeitos curtos disparados em mineração, passos/aterrissagem e interação adequada. Usar reutilização/limpeza e limite de instâncias.
- Plantas, fungos e minerais: apoio correto e atualização ao remover suporte. Ornamentos não devem bloquear o caminho com colisores acidentais.

Se um quadro não for adequado para determinada fase, ajuste a sequência e documente. Não apresente a grade inteira como um sprite único no jogo.

## 11. Testes e critérios de conclusão

Execute verificações que comprovem comportamento, sem testes artificiais que apenas repetem constantes:

1. Compilação sem novos erros e pipeline executado duas vezes, conferindo estabilidade de IDs, retângulos e referências.
2. Cobertura do inventário original e das 22 novas pranchas, com destino de cada família e pendências explícitas.
3. Escalas medidas: célula, Kael, colisor, árvore jovem/comum/grande, espada e props; screenshot com régua/grade e outro sem sobreposição.
4. Área extensa de terra/pedra e casos de borda: sem costuras visíveis, halos, contornos internos, grama subterrânea ou repetição gritante de 4×4.
5. Pelo menos três seeds fixas diferentes; comparar mesmas seeds entre execuções e percorrer rotas principais sem ficar preso.
6. Andar/correr, degraus, descidas, saltos parado e correndo, direção invertida no ar, teto baixo, bordas, coyote, buffer antes de aterrissar e respawn.
7. Altura do salto medida acima de zero e dentro da meta escolhida, em vários frame rates ou com um teste de simulação física reproduzível. Confirmar separadamente o evento de tecla real.
8. Espada alinhada nos quadros e direções pedidos; cinco ataques com origem correta e dano uma vez por janela apropriada.
9. Mineração, construção e perda de suporte funcionando com o terreno novo, sem reaparecimento aleatório de variantes.
10. Boss: entrada segura na arena, despertar, voo, mergulho, disparo, dano, morte e reset; capturas da barra em valores intermediários e zero.
11. Galeria e visualizador abrindo sem congelamento. Resolver/limitar o custo dos rótulos TMP; criar textos só para itens visíveis ou mostrar detalhes por seleção. Não ocultar o problema por timeout maior.
12. 1920×1080, 1280×720 e uma proporção diferente de 16:9; HUD legível, câmera dentro de limites e terreno com continuidade.
13. Imagens de evidência persistentes no projeto: visão geral, floresta, colina assimétrica, caverna, solo ampliado, mão/arma, sequência de salto, boss e galeria. Capturas de antes/depois devem usar parâmetros comparáveis.

Não declare aprovação de um teste que não executou. Se não puder interagir manualmente, explique isso e entregue os testes automáticos realmente executados e um roteiro manual curto. Não confunda imagem gerada com captura do jogo.

## 12. Ordem e entrega

Faça a integração em etapas verificáveis:

1. Inventariar estado atual e reproduzir problemas.
2. Estabelecer escala e corrigir entrada/pulo/âncoras da espada.
3. Preparar arte, transparência, recortes e materiais contínuos.
4. Construir e percorrer a exploração natural, calibrar árvores/solo/câmera.
5. Integrar os demais objetos, sequências, criaturas e galeria.
6. Executar validação, corrigir regressões e entregar build de teste.

Ao terminar, entregue:
- cena de exploração funcionando e forma direta de abri-la/jogá-la;
- build Windows atualizado, se a ferramenta Unity estiver disponível;
- galeria/visualizador acessíveis, com todos os grupos originais e novos;
- manifesto com fontes e derivados, sem perdas silenciosas;
- documentação dos números finais, causa real do pulo e correção da espada;
- evidências e resultados dos testes, incluindo qualquer limitação restante.

Use o pipeline e a arquitetura já presentes, evitando uma segunda implementação concorrente. Não destrua fontes nem sobrescreva mudanças alheias. Faça o trabalho autorizado até o resultado concreto; só peça informação se houver um bloqueio real que não possa resolver inspecionando o projeto.
