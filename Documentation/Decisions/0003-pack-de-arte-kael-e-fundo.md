# 0003 — Pack de arte no protótipo

- **Data:** 2026-09-24
- **Estado:** aceita
- **Escopo:** `Assets/Art/Characters/Kael`, `Assets/Art/Backgrounds`, `Assets/Art/Pack`,
  `Assets/Game/World/BackdropSequence.cs`, `Assets/Game/Editor`, `ArtSource/Pack`

## Contexto

Chegou um pack de arte em `pack/`: tiras do Kael, oito conceitos de fundo por
bioma, tiles 16x16 de terra e pedra ("environment"), um "starter map pack"
(tiles, props, árvores, nuvens, parallax) e pranchas de referência.

As fontes usadas foram copiadas para `ArtSource/Pack/`, e
`ArtSource/Pack/prepare_pack_art.ps1` gera tudo o que vai para `Assets/`.

## Decisões

### 1. Kael é o personagem jogável

A arte do jogo vem só das duas pranchas escolhidas — "Pack 1 - Player"
(`ArtSource/Kael/kael_pack1_player_board.png`) e a prancha do mundo
(`ArtSource/World/world_board.png`) — e, no que elas não cobrem, do pack. As
pranchas não têm transparência (o xadrez está pintado); os scripts de recorte
tiram o xadrez.

O jogador é o Kael da prancha Pack 1, recortado por
`ArtSource/Kael/extract_pack1_player.ps1` em `Assets/Art/Characters/Kael`:
parado (8), caminhada (8), corrida (8), pulo (6), queda (3) e aterrissagem (4).
Cada boneco é achado como ilha de pixels; o xadrez sai quando encosta na borda
ou forma bolsão grande, para os olhos e brilhos (claros e pequenos) ficarem.
Células de 72x71, pés na última linha e cabeça no centro; 40 px por unidade
(corpo de 1,72 unidade, a altura do colisor), filtro bilinear.

Os personagens anteriores (`Player` e `KaelV2`) saíram do jogo; as fontes
continuam em `ArtSource`.

### 2. Nove paisagens, uma por minuto

Os oito conceitos de bioma, recortados nas 864 linhas de cima (1536x864), mais as
quatro camadas de parallax do starter empilhadas numa imagem só, do mesmo
tamanho. A 50 px por unidade o recorte tem 17,3 unidades de altura, pouco mais
que a tela (15), e cada pixel da arte vira 1,44 pixel em 1080p.

Um recorte anterior, de 440 linhas a 22 px por unidade, tirava o primeiro plano
pintado, mas precisava ampliar a arte 3,3x para cobrir a tela, e a arte gerada
não tem pixels nítidos para isso: o fundo ficava borrado. Agora o primeiro
plano pintado entra no recorte e fica, em geral, atrás do terreno.

`BackdropSequence` troca de paisagem a cada 60 segundos de jogo, fundindo a
próxima por cima nos últimos 10. Antes a troca seguia a distância andada pelo
colosso, e parado ele nunca mudava de paisagem.

As cópias vizinhas do fundo são espelhadas, porque as imagens não repetem nas
bordas.

Com a imagem pouco mais larga que a tela, um fundo rolando sem fim mostraria
a emenda espelhada quase o tempo todo. Por isso o fundo acompanha a câmera e
cada paisagem só desliza 3,4 unidades enquanto está na tela; a próxima espera
no ponto de partida. As cópias espelhadas só aparecem em telas ultrawide.

O fundo pintado é o único fundo: as camadas geradas (nuvens, montanhas,
colinas, linha de árvores) saíram.

O zoom (`CameraZoom`: roda do mouse, `-` e `=`) vai de 4,5 a 11 de tamanho
ortográfico. O fundo cresce junto com o zoom, como se estivesse infinitamente
longe, e continua cobrindo a tela.

### 3. Terreno da prancha do mundo

`ArtSource/World/extract_terrain.ps1` monta os tiles em `Assets/Art/World/Terrain`
a partir dos blocos da prancha do mundo. Os blocos são ícones, com contorno
escuro nos quatro lados, então só o miolo é usado (26x26, reduzido para 24x24,
a 24 px por unidade); o contorno é redesenhado só nos lados expostos ao ar.
Terra, pedra e rocha profunda têm três interiores cada; a terra junta a metade
de baixo de blocos de terra com grama, porque a prancha não tem terra pura.

`TerrainArtGenerator.Load` prefere esses tiles; os do pack e os gerados ficam
como reserva para nomes que a prancha não cobre (as raízes penduradas). O que
vem abaixo descreve os tiles do pack, que o terreno usava antes.

Nenhum dos dois conjuntos do pack funciona sozinho:

- o "environment" tem bordas boas, mas furos transparentes do lado de dentro,
  uma grade pontilhada no miolo e uma fileira inferior que não é borda;
- o "starter" é opaco e completo, mas só tem topo gramado e miolo.

O script desenha cada borda do environment sobre um miolo opaco do starter —
só a partir de 5 px do lado exposto, para o ar continuar transparente — e usa
o miolo do starter na terra. Rocha usa o environment; rocha profunda e parede
de caverna, o starter.

`TerrainArtGenerator.Load` prefere o tile de `Assets/Art/Pack/Terrain` com o
mesmo nome do gerado. Os gerados continuam sendo escritos: cobrem as raízes
penduradas, que o pack não tem.

### 4. Props, árvores e nuvens do starter como cenário

- `props_small`: tufos, flores, cogumelo e pedras viram os tiles de decoração;
  o resto é espalhado sobre o chão.
- Árvores e arbustos vêm da prancha do mundo (`ArtSource/World/world_board.png`,
  recortada por `extract_vegetation.ps1` para `Assets/Art/World/Vegetation`), a 24 px
  por unidade e sem escala aleatória. Substituíram a árvore e o arbusto
  gerados e os arbustos de `props_medium`, que eram formas chapadas.
  Os blocos de terreno da mesma prancha não servem como tiles: cada um tem
  contorno escuro nos quatro lados, e lado a lado formariam uma grade.
- `props_medium` e `props_interactive` (pedras, troncos, caixa, cristais,
  tochas, placa, baús, escada) ficam sobre o chão, perto do início. Não são
  interativos.
- `clouds` formam uma camada de parallax na frente do fundo pintado.

### 5. O executável reconstrói as cenas

`WindowsBuild.RebuildAndBuild` (menu *Reconstruir cenas e gerar executável*, ou
`build_windows.bat`) reconstrói as duas cenas antes do build. Como as cenas são
geradas por código a partir da arte, sem isso arte nova não chega ao `.exe`.

## Consequências

- Três densidades de pixel convivem: fundo (50 px/unidade, filtro bilinear),
  personagem e arte gerada (32), pack (16).
- As nuvens e props do starter são formas chapadas e destoam do fundo pintado.
- O preto azulado das colinas geradas não combina com os fundos diurnos. O
  ideal é separar cada conceito em camadas, como o README do pack recomenda.
- Os tiles do environment têm alguns pixels roxos soltos na grama, herdados da
  remoção de fundo do gerador original.
