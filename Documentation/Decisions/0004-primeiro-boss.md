# 0004 — Primeiro combate e primeiro boss

- **Data:** 2026-09-24
- **Estado:** aceita
- **Escopo:** `Assets/Game/Combat`, `Assets/Game/Creatures`, `Assets/Game/Player`,
  `Assets/Game/UI/CombatHud.cs`, `Assets/Game/Editor`, `Assets/Art/Creatures`,
  `ArtSource/World/extract_creatures.ps1`, `ArtSource/Kael/extract_pack1_player.ps1`

## Contexto

Pedido: criar o primeiro combate, contra o primeiro boss. O `TECH_STACK.md`
(seção 19) listava bosses como fora do escopo; o pedido explícito mudou isso.
O protótipo não tinha vida, dano nem ataque.

## Decisões

### 1. O boss é o Predador das Costas

Da lista de bosses da visão, o que cabe no terreno atual e na arte disponível:
uma fera de cristal que caça sobre o dorso do colosso. A arte é a criatura azul
com chifres da prancha do mundo (`world_board.png`), recortada por
`extract_creatures.ps1` para `Assets/Art/Creatures/back_predator.png`.

A prancha tem um quadro só da fera. A animação é de postura, não de quadros:
`BackPredatorVisual` inclina, achata, faz tremer e tinge um filho do objeto,
com pivô nos pés, sem tocar na física.

### 2. A luta é criar aberturas, e o colosso luta junto

A couraça recebe 20% do dano; aberta, 125%. Ela abre em três situações:

- a **investida** termina batendo no limite da arena — atordoada (2,6 s);
- depois do **salto**, cuja aterrissagem solta uma onda de choque que se evita
  pulando — recuperação (1,3 s);
- um **passo forte do colosso** (só a corrida chega à força necessária) tira a
  fera do chão — derrubada (2,2 s), com recarga de 8 s.

O terceiro ponto é a visão pedindo que o cenário participe das batalhas ("usar
um passo do colosso para derrubar inimigos"). A recarga existe porque, sem ela,
correr com o colosso prenderia o boss no chão a luta inteira.

Abaixo de 50% de vida, fúria: mais rápida, avisos mais curtos e duas investidas
seguidas.

### 3. Arena achatada no relevo

O boss é um corpo rígido largo; no relevo ondulado em degraus de um tile, ele
travaria. `TerrainPainter.SurfaceHeight` achata o vale depois da montanha
(x 78 a 115), com uma rampa de 6 unidades na entrada e um paredão no fim do mundo.
Nada de vegetação nem objetos ali. Os limites da arena são constantes do pintor,
fonte única para o terreno e para o boss.

### 4. `Health` compartilhada, reações separadas

`Lithostride.Combat.Health` só guarda a vida, a invulnerabilidade e o multiplicador
de dano, e avisa por eventos. Quem reage é outro componente: `PlayerVitals`
(recuo, pisca, morte, renascer), `BackPredator` (couraça, fúria, morte) e
`BackPredatorVisual` (clarão). Sem interface nem hierarquia: dois usos reais.

O golpe do jogador (`PlayerAttack`) procura `Health` no `Rigidbody2D` de quem
está na caixa de acerto, uma vez por golpe. A janela ativa são os três últimos
quadros da animação, onde o corte aparece.

### 5. Renascer no lugar, não recarregar a cena

Recarregar a cena obrigaria a refazer o caminho até a arena a cada morte. O
jogador renasce na encosta antes da rampa, com vida cheia; o boss, ao ver o
alvo morrer, volta a dormir no lugar de origem, com vida cheia, 2 s depois.

### 6. O corpo fica

Morto, o predador escurece e continua na arena, com colisor: dá para subir nele.
É o começo do "cadáver do boss vira estrutura" da visão, sem nenhum sistema a
mais.

### 7. Kael com ataque, dano e morte

`extract_pack1_player.ps1` passou a recortar ataque (8), dano (4) e morte (7)
da prancha Pack 1. O nono boneco do ataque ficou de fora: o arco de energia o
liga ao oitavo numa ilha só, e ele é só a volta à guarda. A célula cresceu de
72x71 para 108x77, para caber o arco e o corpo deitado; os quadros antigos só
ganharam margem (conferido pixel a pixel).

## Consequências

- Nova ação `Attack` no `LithostrideControls.inputactions`: `J`, botão
  esquerdo do mouse e botão oeste do controle.
- Validado em Play Mode por um teste temporário em batchmode: despertar,
  investida, atordoamento, salto com onda de choque, derrubada pelo passo do
  colosso, dano com couraça fechada (6) e aberta (38), morte e renascimento do
  jogador, morte do boss.
- Os impactos do boss não tremem a câmera: o tremor tem um único ponto de
  controle (`ColossusStepImpulse`), e abrir um segundo exigiria outra decisão.
- Sem som, sem partículas de impacto e sem recompensa por vencer.
- O clique esquerdo também ataca quando se clica nos botões da HUD.
