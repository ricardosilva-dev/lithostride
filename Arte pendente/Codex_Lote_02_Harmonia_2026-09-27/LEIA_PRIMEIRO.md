# LITHOSTRIDE — Lote 02: harmonia do terreno e árvores

Seis pranchas novas geradas com image_gen integrada. As fontes usam o pack original como referência de paleta e pixel art. Os prompts completos estão em PROMPTS.md.

Destino no projeto: `C:\Users\Ricardo\Documents\Projetos\lithostride\Arte pendente\Codex_Lote_02_Harmonia_2026-09-27`.

## Conteúdo

- 72 amostras visuais de materiais: 24 de terra mineral, 24 de terra com raízes/estratos e 24 de pedra.
- 24 acabamentos de grama e superfície.
- 16 árvores completas, em duas famílias: bosque e árvores antigas.

São contagens visuais de elementos nas pranchas, não quantidades de sprites já recortados/importados.

## Preparação necessária

As imagens têm canal alfa real, confirmado por leitura do PNG. Há resíduos muito fracos no entorno e pixels sólidos com alfa próximo do máximo; a preparação deve tratar isso por categoria, nos derivados. As fontes não foram alteradas.

As pranchas de terreno NÃO possuem encaixes matematicamente certificados. A variedade visual precisa passar por recorte, remoção de molduras, normalização de bordas e testes de vizinhança. O arquivo 20 contém pequenos trechos de plataforma: aproveite seus acabamentos de grama, sem repetir a peça inteira como chão.

As árvores precisam de calibração por altura visível e pivô na raiz. As classes “média” e “grande” descrevem o uso pretendido; o tamanho final é estabelecido no jogo. As copas têm margens apertadas em algumas regiões, portanto revise os recortes.

CATALOGO.md apresenta cada prancha. manifesto.json registra dimensões, hashes, grade pretendida e estatísticas amostrais de alfa. Os retângulos e pivôs finais ainda não foram definidos.

## Integração

O prompt PROMPT_CLAUDE_HARMONIA_MAPA_E_CORRECOES.md, entregue junto dos lotes, cobre a integração deste material e do lote 01, além de escala, espada, pulo e mapa natural. As imagens estão fora de Assets para que a integração ocorra nessa próxima tarefa do Claude.

Esta entrega não modificou código, cenas ou o pack original e não representa uma correção já aplicada ao jogo.

