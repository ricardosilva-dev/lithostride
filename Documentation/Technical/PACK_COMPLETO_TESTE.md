# Pack completo — demonstração de teste (estado em 2026-09-26)

> **Estado: incompleto.** A importação, a cena e o executável funcionam. A validação automática
> ainda tem falhas abertas, listadas em "Pendências" no fim deste guia.

## 1. Cena e comandos

- Cena: `Assets/Scenes/Tests/PackCompletoTest.unity`. A cena antiga `Teste.unity` não é tocada.
- Menu `Lithostride/Pack completo/Preparar e construir teste`: gera a arte derivada, a paleta do
  terreno e o manifesto, e reconstrói a cena.
- Menu `Lithostride/Pack completo/Reconstruir e gerar executável (Windows)`, ou `build_windows.bat`:
  faz o mesmo e depois gera `Build/Windows/LITHOSTRIDE.exe`.
- Modo batch: `-executeMethod Lithostride.EditorTools.PackPipeline.BatchPrepareAndBuild`.
- Se faltar arquivo em `Pack completo/`, o processo para antes de mexer na cena.
- O processo é idempotente. PNG igual não é regravado, um sprite com o mesmo nome mantém o ID
  (GUID determinístico para recortes novos) e os tiles são atualizados no lugar. Na segunda
  execução o log mostra "48 folhas (0 reescritas, 0 reimportadas)".
- Requisito: os recursos essenciais do TextMeshPro já foram importados em `Assets/TextMesh Pro`.

## 2. Controles

- A/D ou setas: andar. Shift: correr. Espaço ou W: pular.
- 1: combate. 2: mineração. 3: construção de teste.
- Clique esquerdo: ação do modo. Clique direito: projétil no combate, tirar bloco na construção.
- Q/E: troca o golpe ou o material. R: troca a forma do bloco. N: voo livre.
- Roda do mouse: zoom (1/4× a 3×). F1: abre o painel. Esc: fecha o painel ou pausa; "Sair" fica
  só na tela de pausa.
- No painel: teleportes (Início, Percurso, Caverna, Treino, Arena, Bancadas, Galeria, Ícones,
  Visualizador), escolha de fundo, versão do boss (v2/v1), reinício da luta, vida do boss em
  63%/7%/0, cura e dano no Kael, todos os ataques, material e forma de bloco, reset do terreno e
  dos alvos, sobreposições (grade, pivôs, colisores, rótulos) e visualizador (pausa, quadro a
  quadro, linha de base, limites).

## 3. Fonte → derivados

O mapa completo, com hash, retângulos, pivôs, âncoras e ajustes por sprite, está em
`Assets/Art/Pack/pack_manifest.json`. Ao todo são 28 PNGs de origem, 25 conteúdos distintos,
48 folhas derivadas e 1897 sprites.

| Fonte | Derivado | Uso |
|---|---|---|
| Personagem: MASTER, IDLE, WALK, RUN (9 quadros cada) | `Personagem/kael_*.png` 1:1 | Parado, caminhada e corrida. O quadro 0 é a pose parada. |
| `KAEL_JUMP.png` (8 poses) | `kael_pulo.png`, reduzido ×0,434 com a paleta do MASTER | Preparação, subida, ápice, queda, queda longa, aterrissagem e recuperação, escolhidos pela velocidade vertical. |
| Blocos1/2/3 | 16 materiais em textura contínua (quilting 4×4), 256 bordas, grama e musgo (topo, pontas, cipós, rampas), ícones de todos os blocos | Terreno, bancadas e painel de ícones |
| Blocos 3, linha 11 | `energia_01..10` | Ícone que aparece ao minerar minério e no painel de ícones |
| arvore 1–4 | 69 variações (árvores, tocos, troncos, galho), sem a plataforma ornamental | Vegetação natural e galeria |
| itensNoChao | 24 props | Espalhados pelo mapa e na galeria |
| Fundo/fundo1–3 (iguais aos de `Blocos/`) | `fundo_caverna`, `fundo_entardecer`, `fundo_dia` | Automático por região ou manual |
| arma1 | `espada_cristal`, pivô na empunhadura | Espada na mão do Kael |
| animacaoArma1 a v5 | Corte, energia, descendente, projétil, explosão (8 quadros cada) | Os 5 ataques |
| boss (v1) e bossv2 (v2, padrão) | 30 e 31 quadros; da v1 saem também o sopro e o cristal | Boss voador e comparador |
| barraVida | Moldura (barra vazia), preenchimento do canal, brilho da ponta, referências | Barra do boss |

## 4. Escala

- Pixel lógico = pixel do Kael = 48 px por unidade. O Kael desenhado tem 3,44 unidades.
- Bloco = 1 unidade = 24 texels da arte de ambiente (24 PPU, cada pixel da arte vira 2 px
  lógicos). O boss também usa 24 PPU.
- Colisor do Kael: 0,95 × 3,1 unidades, sem capa e sem cabelo. Degrau de 1 bloco sobe andando.
  O pulo chega a cerca de 4,2 unidades.
- Pivôs:
  - Kael: centro do corpo, na base das botas. Nas poses no ar, o alinhamento é pelo broche.
  - Árvores: base do tronco, com as raízes afundadas 2 texels na grama.
  - Boss: centro do corpo, sem as asas.

## 5. Ajustes de arte

- Alfa residual (1 a 8) é zerado. Pixel com alfa ≥ 241 vira opaco.
- Nos efeitos o brilho parcial é mantido. Na prancha do boss, a névoa escura sai e o brilho azul
  fica.
- Divisão entre quadros e entre árvores: costuras de alfa mínimo, em vez de grade fixa.
- Blocos: o miolo sem moldura é costurado numa textura contínua. O contorno só aparece em lados
  expostos ao ar.

## 6. Arquivos

- Editor: `Assets/Game/Editor/Pack/` (`PackPipeline`, `PackSceneBuilder`, `PackUiBuilder`,
  `PackDiagnostics`) e `Pack/Core/` (processamento de imagem sem Unity).
- Runtime:
  - `World/Terrain*`, `PropSupport`, `BackdropView`;
  - `Core/GameInput` (única leitura de entrada);
  - `Player/*`, `Combat/*`;
  - `Creatures/FlyingBoss`, `BossVisual`;
  - `UI/*`, incluindo o roteiro `EvidenceRunner`.
- Removidos para a Lixeira: `TestSceneBuilder.cs` e `TerrainBuilder.cs`, que apontavam para arte
  antiga que não existe mais.
- Configuração: o pipeline de render ativo é **Built-in**, confirmado no log. A documentação cita
  URP, mas não há asset de URP atribuído. A física 2D está em FixedUpdate.
- Decisão 0004: o predador terrestre antigo não existe no código atual. Este teste usa o boss
  voador do pack.

## 7. Testes executados (executável, 1920×1080, D3D11, entrada simulada)

**Aprovados:**
- Kael nasce apoiado no chão.
- Anda no plano sem afundar (0 quadros no ar em 278).
- A corrida é mais rápida que a caminhada.
- Sobe degraus de 1 bloco andando.
- Desce rampa colado ao chão.
- Árvore sem apoio some, e o reset devolve o bloco e a árvore.
- Colocar bloco e rampa funciona.
- Os 5 ataques causam dano no alvo (18, 28, 40, 20, 28).
- O corte acerta uma vez por golpe, também virado à esquerda.
- Efeitos não acumulam.
- Kael morre e renasce com vida cheia, e o boss volta a esperar.
- Projéteis são limpos quando o boss morre.

Capturas: pasta temporária da sessão. Não foram copiadas para o projeto.

## 8. Pendências (parte ainda incompleta)

1. **Pulo pela entrada simulada: REPROVADO.** A altura medida foi 0. Ainda não se sabe se o erro
   está no roteiro (evento de tecla) ou no controle. O pulo não foi testado manualmente.
2. **Boss: despertar, voo, mergulho e disparo NÃO VALIDADOS.** O roteiro não andou o suficiente
   para entrar na arena (a zona começa em x = 260; é preciso andar cerca de 2,5 s). A barra em
   63%, 7% e 0 também não foi capturada, porque o botão ignora o boss adormecido.
3. **Roteiro travou/excedeu 5 min ao chegar na Galeria** (zoom 0,5). Galeria, ícones e
   visualizador ficaram sem captura. É preciso investigar o desempenho com os 383 rótulos
   TextMeshPro.
4. Teleportes do roteiro caem dentro de blocos. É um erro do roteiro, mas mostra que a colisão
   em Outlines deixa um corpo que entra no sólido cair até o fundo do mapa. Considerar
   `GeometryType.Polygons`.
5. Rampa de arenito: o teste terminou em y = 4,66 porque o tempo de caminhada foi curto. Precisa
   ser refeito.
6. Não executados: 1280×720 e proporção diferente de 16:9 (`-evidencias-curto`), segunda
   reimportação comparando IDs, testes de EditMode.
7. A fonte do painel não tem ◀ ▶: aparece "□" nos botões de quadro.
8. Disco C com cerca de 4 GB livres. A Unity avisa que está com pouco espaço.
