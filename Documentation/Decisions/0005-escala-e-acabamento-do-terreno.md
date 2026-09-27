# 0005 — Escala e acabamento do terreno

- **Data:** 2026-09-24
- **Estado:** aceita
- **Escopo:** `TerrainPainter`, `PrototypeSceneBuilder`, `PrototypeArtGenerator`, `PlayerMotor`

## Contexto

Na avaliação do jogo, o visual foi rejeitado: blocos enormes, terreno chapado,
lago de vidro, objetos soltos sem sentido. Capturas em 1080p confirmaram:

- cada bloco tinha uma unidade, ou 72 px de tela, e o Kael só 1,7 bloco de altura;
- a cor por célula nunca funcionou — o `Tile` vem com `LockColor`, que descarta
  a cor ao carregar a cena;
- o lago era um retângulo semitransparente na frente do terreno;
- os objetos do starter pack eram formas chapadas.

## Decisões

### 1. Blocos de meia unidade

`TerrainPainter.CellsPerUnit = 2`. O relevo, a água e tudo o que é posicionado
continuam em unidades do mundo; só a grade fica mais fina, com o objeto da
grade em escala 0,5 (arte e colisão juntas). O Kael passa a ter três blocos e
meio de altura, como em Terraria. `TerrainPainter.GroundTop(x)` é o único ponto
que diz onde algo pousa.

### 2. Subida automática de degrau

Com blocos de meia unidade, o relevo virou degraus de 0,5, mais altos que o
raio do colisor do Kael. `PlayerMotor` sobe andando degraus de até 0,55 quando
há espaço para o corpo em cima; parede alta continua pedindo pulo. Conferido em
Play Mode: a encosta do início sobe a 4,75 u/s, sem pular.

### 3. Luz de cima, manchas e fronteira irregular

`TintTerrain` escurece o terreno nos primeiros 7 m abaixo do relevo e esfria
para o azul no fundo, com manchas de ruído suave e pouca variação por célula. A
fronteira entre terra e rocha ondula com ruído. Os tiles gerados passaram a ter
`TileFlags.None`, sem o que nada disso aparece.

### 4. Lago opaco atrás do terreno

A água ficou opaca e foi para trás do terreno (ordem -2): o retângulo cobre a
bacia e o terreno recorta a forma dela.

### 5. Sem objetos do starter pack nem pedrinhas

Tochas apagadas (liam como setas), baús, caixa, troncos, placa e cristais
chapados saíram da cena; as pedrinhas da decoração, que liam como gravetos,
também. O terreno volta a ganhar objetos quando houver arte no estilo da
prancha do mundo.

## Consequências

- O terreno tem 4 vezes mais células (41 mil sólidas); a reconstrução continua
  levando segundos.
- Continua uma escada: blocos sem rampas. Rampas e meios-blocos são o próximo
  passo para suavizar as encostas.
- O fundo pintado segue mais detalhado que o primeiro plano. Só se resolve com
  arte feita num estilo só.
