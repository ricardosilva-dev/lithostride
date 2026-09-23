# LITHOSTRIDE — Stack Técnica e Arquitetura Planejada

> Documento técnico interno. Descreve a stack, as responsabilidades de cada tecnologia e a arquitetura **planejada**. Nada descrito aqui como "planejado" ou "futuro" está implementado até que uma tarefa específica o implemente.
>
> Visão completa do jogo: [`docs/VISAO_DO_JOGO.md`](../../docs/VISAO_DO_JOGO.md)
>
> Última revisão: 2026-09-23

---

## 1. Identificação do projeto

| Campo | Valor |
|---|---|
| Nome | LITHOSTRIDE |
| Subtítulo | The Living Colossus |
| Gênero | Sandbox survival 2D (lateral) com exploração, construção, mineração, combate e elementos de RPG |
| Plataforma inicial | Windows (PC) |
| Modelo inicial | Single-player, offline, save local |
| Engine | Unity 6 LTS |
| Linguagem | C# |

**Descrição técnica resumida:** cliente Unity único, executado localmente, que simula um mundo 2D baseado em tiles sobre o corpo de um colosso mineral. O mapa jogável é dividido em regiões e chunks; o movimento do colosso é representado por um estado global abstrato e por efeitos visuais/sonoros (parallax, tremores, partículas), não pelo deslocamento físico do terreno. Dados de design ficam em ScriptableObjects; o estado da partida fica em classes C# serializáveis gravadas em arquivos locais. Não há backend, banco de dados nem rede.

---

## 2. Stack principal

### Unity 6 LTS

- **Função:** engine do jogo — editor, cenas, renderização, física, áudio, build.
- **Quando:** desde o primeiro dia.
- **Não fazer:** não atualizar a versão do editor no meio de uma etapa; não misturar versões diferentes entre máquinas.
- **Cuidados:** usar sempre uma versão **LTS** e registrar a versão exata em `ProjectSettings/ProjectVersion.txt` (versionado). Atualizações de versão devem ser uma tarefa isolada, com commit antes e depois.

### C#

- **Função:** toda a lógica do jogo.
- **Quando:** desde o primeiro script.
- **Não fazer:** não usar recursos de linguagem/.NET não suportados pela Unity; não usar `async void` fora de handlers de evento; não usar reflexão pesada em tempo de jogo.
- **Cuidados:** alocações em `Update`/`FixedUpdate` geram lixo e picos de GC; manter scripts pequenos e focados.

### URP 2D (Universal Render Pipeline com Renderer 2D)

- **Função:** renderização 2D, luzes 2D, sombras 2D, pós-processamento.
- **Quando:** desde a criação do projeto (template "Universal 2D").
- **Não fazer:** não trocar de pipeline depois que houver materiais e shaders; não usar o Built-in Render Pipeline em paralelo.
- **Cuidados:** luzes 2D em grande quantidade custam desempenho; os assets do pipeline (URP Asset, Renderer 2D) ficam em `Assets/Settings/` e não devem ser apagados.

### Input System

- **Função:** leitura de teclado, mouse e controle; rebinding.
- **Quando:** a partir do primeiro marco (movimentação).
- **Não fazer:** não usar a API antiga `UnityEngine.Input` em paralelo; não espalhar leitura de input por muitos scripts.
- **Cuidados:** configurar *Active Input Handling* em Project Settings → Player para **Input System Package (New)**; centralizar as ações em um Input Actions asset.

### Tilemap

- **Função:** terreno, paredes, cavernas e decoração baseados em grade.
- **Quando:** primeiro marco (chão provisório) e, depois, sistema de chunks.
- **Não fazer:** não usar um único Tilemap gigante para o colosso inteiro; não usar o Tilemap como fonte de verdade do estado do mundo (a fonte de verdade será o estado do chunk).
- **Cuidados:** `TilemapCollider2D` + `CompositeCollider2D` para colisão eficiente; atualizações em massa de tiles devem usar APIs em lote (`SetTiles`).

### 2D Animation

- **Função:** animação por ossos (rigging) de personagens e criaturas.
- **Quando:** após a validação da gameplay; no protótipo, sprites simples ou animação por frames bastam.
- **Não fazer:** não rigar arte provisória.
- **Cuidados:** criaturas com muitos ossos custam CPU; avaliar animação por frames para criaturas simples.

### Cinemachine

- **Função:** câmera que segue o jogador, confinamento, tremor (impulse).
- **Quando:** primeiro marco (câmera seguindo) e segundo marco (tremor dos passos).
- **Não fazer:** não escrever uma câmera manual paralela; não aplicar tremor diretamente no Transform da câmera.
- **Cuidados:** o tremor precisa ser reduzível/desligável (acessibilidade) — usar um único ponto de controle de intensidade.

### TextMeshPro

- **Função:** texto da UI e do mundo.
- **Quando:** assim que houver UI.
- **Não fazer:** não usar o componente `Text` legado.
- **Cuidados:** importar os *TMP Essential Resources* uma vez; fontes com caracteres do português (acentos) precisam estar no atlas.

### ScriptableObjects

- **Função:** **definições estáticas** de design (itens, criaturas, biomas, eventos etc.).
- **Quando:** a partir do primeiro sistema que precisar de dados configuráveis.
- **Não fazer:** não guardar estado mutável da partida em ScriptableObjects; não usá-los como save.
- **Cuidados:** alterações feitas em ScriptableObjects durante o Play Mode persistem no editor — tratar como somente leitura em tempo de jogo.

### Visual Studio Code

- **Função:** editor de código.
- **Quando:** sempre.
- **Configuração:** extensão **Unity** (Microsoft) + **C# Dev Kit**; em Unity, Preferences → External Tools → External Script Editor = Visual Studio Code; pacote `com.unity.ide.visualstudio` presente no projeto.
- **Não fazer:** não versionar `.vscode/` pessoal por enquanto; não versionar `.sln`/`.csproj` (gerados pela Unity).
- **Cuidados:** se o IntelliSense falhar, usar *Regenerate project files* nas External Tools.

### Git e GitHub

- **Função:** versionamento e backup remoto.
- **Quando:** sempre.
- **Não fazer:** não versionar `Library/`, `Temp/`, `Logs/`, `obj/`, `Build(s)/`, `UserSettings/`; não commitar saves pessoais nem credenciais.
- **Cuidados:** sempre commitar os arquivos `.meta` junto com seus assets; Asset Serialization deve estar em **Force Text** (padrão) para permitir diffs.

---

## Estado atual dos pacotes

Inspeção realizada em 2026-09-23.

**O repositório ainda não contém um projeto Unity.** Não existem `Assets/` (além da estrutura de pastas criada nesta tarefa), `Packages/manifest.json` nem `ProjectSettings/ProjectVersion.txt`. Nenhum Unity Editor/Unity Hub foi encontrado nos caminhos padrão da máquina.

Portanto, **nenhuma versão de engine ou pacote pôde ser detectada**, e nenhuma versão é registrada aqui.

| Pacote desejado | Identificador esperado no manifest | Versão atual | Status | Ação futura recomendada |
|---|---|---|---|---|
| Universal Render Pipeline | `com.unity.render-pipelines.universal` | — | Ausente (sem projeto) | Criar projeto com o template **Universal 2D**, que já o inclui |
| Input System | `com.unity.inputsystem` | — | Ausente (sem projeto) | Verificar após criação; instalar pelo Package Manager se faltar |
| Tilemap | `com.unity.2d.tilemap` (+ `com.unity.2d.tilemap.extras`, opcional) | — | Ausente (sem projeto) | Incluído no template 2D; confirmar |
| 2D Animation | `com.unity.2d.animation` | — | Ausente (sem projeto) | Incluído via `com.unity.feature.2d`; confirmar |
| Cinemachine | `com.unity.cinemachine` | — | Ausente (sem projeto) | Instalar pelo Package Manager no primeiro marco |
| TextMeshPro | `com.unity.ugui` (no Unity 6 o TMP faz parte do uGUI) | — | Ausente (sem projeto) | Confirmar após criação; importar TMP Essential Resources |
| Integração VS Code | `com.unity.ide.visualstudio` | — | Ausente (sem projeto) | Confirmar após criação |

**Pendência principal:** criar o projeto Unity 6 LTS neste repositório (ver seção 17) e então atualizar esta tabela com as versões reais lidas de `Packages/manifest.json` e `ProjectSettings/ProjectVersion.txt`.

---

## 3. Responsabilidade da Unity

A Unity é responsável por:

- **cenas** — organização dos níveis e do fluxo (Bootstrap, MainMenu, Gameplay, Prototype);
- **GameObjects** e **componentes** — composição das entidades;
- **sprites** — importação, atlas, ordenação;
- **física** e **colisões** — Physics 2D (Rigidbody2D, Collider2D);
- **animações** — Animator, 2D Animation;
- **iluminação** — luzes 2D do URP;
- **partículas** — poeira, folhas, detritos;
- **áudio** — AudioSource, AudioMixer;
- **interface** — uGUI/TextMeshPro;
- **Tilemaps** — renderização e colisão da grade;
- **builds** — geração do executável Windows;
- **execução do jogo** — ciclo de vida, loop principal.

---

## 4. Responsabilidade do C#

C# implementa as regras do jogo:

- movimentação e controles;
- combate;
- itens, inventário e crafting;
- construção;
- chunks e biomas;
- criaturas e NPCs;
- eventos (incluindo eventos combinados);
- caminhada do colosso e seus estados;
- órgãos minerais;
- simulação distante (simplificada e estatística);
- salvamento e carregamento.

---

## 5. Ausência de backend tradicional

Na fase inicial, o jogo **não terá**:

- frontend web;
- backend NestJS (ou qualquer outro);
- API;
- banco de dados SQL (ou qualquer outro);
- Docker;
- servidor dedicado.

Toda a gameplay é executada **localmente no cliente Unity**. O estado da partida é gravado em arquivos no computador do jogador. Não existe conexão de rede na gameplay.

---

## 6. Estratégia de dados

Os dados são separados em duas categorias com regras diferentes.

### Definições estáticas (planejadas como ScriptableObjects)

Dados criados pelo design, que **não mudam durante a partida**:

| Definição planejada | Conteúdo previsto |
|---|---|
| `ItemDefinition` | id, nome, ícone, empilhamento, categoria |
| `WeaponDefinition` | dano, alcance, tipo, velocidade |
| `CreatureDefinition` | atributos, comportamento, biomas, drops |
| `BiomeDefinition` | tiles, vegetação, criaturas, estados normal/alterado |
| `EventDefinition` | condições (território, estado do colosso, horário, clima), efeitos |
| `RecipeDefinition` | ingredientes, resultado, estação necessária |
| `SkillDefinition` | habilidade de classe |
| `ClassDefinition` | estilo de jogo (Guardião Mineral, Caçador dos Ventos etc.) |
| `OrganDefinition` | órgão mineral e seus estados |
| `ExternalRegionDefinition` | territórios do mundo exterior |
| `WeatherDefinition` | clima e seus efeitos |

> **Não implementadas.** Serão criadas uma a uma, quando o sistema correspondente for implementado. Arquivos de dados ficarão em `Assets/Data/`; os scripts, em `Assets/Game/`.

### Estado da partida (planejado como classes C# serializáveis)

Dados que **mudam durante a partida** e precisam ser salvos:

- `PlayerState`
- `InventoryState`
- `WorldState`
- `ColossusState`
- `SettlementState`
- `EventState`
- `ChunkState`

> **Não implementadas.** Classes C# simples (não `MonoBehaviour`, não `ScriptableObject`), que referenciam definições por **id estável**, nunca por referência direta de objeto, para que o save sobreviva a mudanças nos assets.

---

## 7. Salvamento

Estratégia progressiva:

### Protótipo

- JSON local;
- campo de versão do formato em todo arquivo (`saveVersion`);
- fácil inspeção manual;
- backup simples (cópia do save anterior antes de sobrescrever).

### Mundo maior

- arquivos separados por chunk;
- formato binário;
- compactação;
- carregamento assíncrono;
- verificação de integridade (checksum);
- backups automáticos rotativos;
- migração entre versões de formato.

### Estrutura planejada

```
Saves/
└── World_001/
    ├── metadata.json
    ├── player.dat
    ├── world.dat
    ├── colossus.dat
    └── chunks/
        ├── chunk_-1_0.dat
        ├── chunk_0_0.dat
        └── chunk_0_1.dat
```

O local padrão será `Application.persistentDataPath` (fora do repositório). **Saves pessoais nunca são versionados no Git**; o `.gitignore` também ignora uma pasta `Saves/` na raiz caso seja usada durante o desenvolvimento.

---

## 8. Chunks

Hierarquia planejada do mapa:

```
Colosso
└── Regiões
    └── Chunks
        └── Tiles
```

Cada chunk poderá futuramente armazenar:

- terreno;
- paredes;
- líquidos;
- vegetação;
- minérios;
- construções;
- criaturas persistentes;
- bioma;
- iluminação;
- temperatura;
- umidade;
- contaminação;
- modificações do jogador.

> **Não implementado.** O tamanho do chunk e o formato interno serão decididos na tarefa correspondente e registrados em `Documentation/Decisions/`.

---

## 9. Níveis de simulação

### Ativo

Região próxima ao jogador:

- física;
- inteligência artificial;
- combate;
- animação;
- partículas;
- iluminação completa.

### Simplificado

Regiões próximas, mas fora da câmera:

- crescimento;
- movimentação resumida;
- produção;
- eventos locais simplificados.

### Estatístico

Regiões distantes:

- população;
- recursos;
- condição ambiental;
- estado dos assentamentos;
- progresso de eventos;
- condição do colosso.

Um chunk muda de nível conforme a distância ao jogador. A transição deve ser determinística o suficiente para que o jogador não perceba perdas ao se aproximar.

---

## 10. Estratégia de desempenho

Ordem obrigatória:

1. GameObjects tradicionais.
2. Boas práticas e profiling (Unity Profiler).
3. Object Pooling.
4. Otimizações específicas.
5. C# Job System e Burst onde houver necessidade medida.
6. ECS somente após medição comprovar necessidade.

**ECS/DOTS não será usado no protótipo inicial.** Nenhuma otimização deve ser feita sem uma medição que a justifique.

---

## 11. Sistema do colosso

**O chão jogável não será fisicamente deslocado a cada passo.** O mapa inteiro compartilha o referencial do colosso; o jogador está parado em relação ao chão.

A sensação de movimento será criada por:

- posição global abstrata do colosso (coordenada no mundo exterior);
- fundos em parallax (camadas com velocidades diferentes);
- estados de movimento;
- eventos de passos (disparados em ritmo definido pelo estado);
- tremor de câmera (Cinemachine Impulse, com intensidade ajustável);
- partículas;
- áudio;
- reação da vegetação;
- ondas na água;
- poeira nas cavernas;
- inclinação variável apenas em eventos específicos.

Estados iniciais planejados:

- `Resting`
- `Walking`
- `Running`

Estados futuros:

- `Climbing`
- `Descending`
- `Falling`
- `Submerged`
- `Wounded`
- `LyingDown`

> **Não implementado.** Será introduzido no segundo marco técnico.

---

## 12. Áudio

### Inicialmente

- sistema de áudio nativo da Unity (AudioSource, AudioMixer);
- música;
- ambiente;
- efeitos;
- áudio espacial quando necessário.

### Futuramente

- avaliar FMOD **apenas** se o projeto exigir áudio adaptativo complexo.

FMOD não será instalado nesta fase.

---

## 13. Arte

Ferramentas planejadas:

- **Aseprite** — pixel art, sprites, animação por frames;
- **Krita** — pintura, fundos, conceitos;
- **Blender** — apenas se necessário (ex.: referências, renderizações para sprites);
- **sprites provisórios** durante o protótipo.

A arte definitiva **não tem prioridade** antes da validação da gameplay. Arquivos-fonte (`.aseprite`, `.kra`, `.psd`, `.blend`) só entrarão no repositório depois de configurado o Git LFS.

---

## 14. Versionamento

- Git;
- GitHub privado;
- GitHub Desktop;
- commits pequenos e frequentes, com mensagem descritiva;
- branches apenas quando necessárias;
- tags para marcos (ex.: `m1-prototype-movement`);
- Git LFS configurado **antes** de adicionar arquivos binários grandes.

Exemplos de arquivos que futuramente devem usar Git LFS:

- `.psd`
- `.blend`
- `.wav`
- `.aseprite`
- `.kra`
- arquivos grandes de arte ou áudio

Git LFS está instalado na máquina (3.7.1), mas **não foi configurado** neste repositório, pois ainda não há arquivos grandes.

---

## 15. Multiplayer

**Não faz parte da primeira fase.** Nenhum pacote de rede será instalado.

Possibilidades futuras:

- cooperativo de 2 a 4 jogadores;
- Netcode for GameObjects;
- host autoritativo;
- lobby da Steam;
- save mantido pelo anfitrião.

---

## 16. Serviços futuros opcionais

Vue, NestJS, PostgreSQL e Docker **só** poderão aparecer no futuro, fora do cliente do jogo, para:

- site;
- painel administrativo;
- ranking;
- telemetria;
- contas;
- notícias;
- serviços online.

Eles **não fazem parte da gameplay principal** e o jogo deve continuar funcionando offline sem eles.

---

## 17. Primeiro marco técnico

Pré-requisito: criar o projeto Unity 6 LTS neste repositório (ainda não existe).

Sugestão segura, já que o Unity Hub não cria projeto em pasta não vazia:

1. No Unity Hub, criar um projeto com o template **Universal 2D** em uma pasta temporária.
2. Fechar o editor.
3. Copiar `Assets/`, `Packages/` e `ProjectSettings/` para a raiz deste repositório (as pastas de `Assets/` já existentes serão mescladas).
4. Abrir a pasta do repositório pelo Unity Hub (*Add project from disk*).
5. Conferir `git status` — `Library/` e similares não devem aparecer.

Conteúdo do marco:

- projeto Unity abrindo;
- VS Code configurado;
- cena `Prototype` (criada pelo editor em `Assets/Scenes/Prototype/`);
- chão provisório;
- personagem provisório;
- movimentação horizontal;
- pulo;
- detecção de chão;
- câmera seguindo;
- build para Windows;
- commit e push.

> Será implementado em uma tarefa posterior.

---

## 18. Segundo marco técnico

- fundo em parallax;
- estado parado;
- estado caminhando;
- passos do colosso;
- tremor suave;
- poeira;
- vegetação reagindo;
- água reagindo;
- transição entre movimento e descanso.

> Será implementado em uma tarefa posterior.

---

## 19. Itens fora do escopo atual

- mundo procedural completo;
- multiplayer;
- dimensões;
- órgãos minerais completos;
- assentamentos autônomos;
- classes completas;
- bosses;
- inventário;
- crafting;
- construção;
- ECS;
- banco de dados;
- Docker;
- API;
- servidor dedicado.

---

## 20. Critérios arquiteturais

- priorizar simplicidade;
- uma responsabilidade clara por componente;
- evitar classes gigantes;
- evitar abstrações sem uso real;
- evitar singletons globais desnecessários;
- não basear a arquitetura em buscas globais (`FindObjectOfType`, `FindFirstObjectByType`, `GameObject.Find`) — preferir referências pelo Inspector;
- evitar lógica pesada em `Update`;
- utilizar eventos apenas quando reduzirem acoplamento;
- usar ScriptableObjects para definições, não para estado mutável da partida;
- não usar ScriptableObjects como banco de dados de save;
- não introduzir dependências externas sem justificativa;
- medir desempenho antes de otimizar;
- manter o projeto compilando após cada alteração;
- implementar um bloco pequeno e testável por vez.

### Assembly Definitions

Nenhum arquivo `.asmdef` foi criado. Assembly Definitions poderão ser introduzidas quando o volume de scripts justificar a separação (redução do tempo de compilação e isolamento de dependências). Os testes em `Assets/Tests/EditMode` e `Assets/Tests/PlayMode` exigirão `.asmdef` próprios quando o primeiro teste for escrito.

### Estrutura de pastas

```
Assets/
├── Art/        sprites e arte (Characters, Creatures, Colossus, Biomes, Tiles, Backgrounds, Effects, UI)
├── Audio/      Music, Ambience, Creatures, Colossus, UI
├── Data/       instâncias de ScriptableObjects (definições)
├── Game/       scripts C#, um subdiretório por namespace (Lithostride.<Pasta>)
├── Materials/
├── Prefabs/
├── Scenes/     Bootstrap, MainMenu, Gameplay, Prototype, Tests
├── Settings/   assets do URP e configurações
├── Shaders/
└── Tests/      EditMode, PlayMode

Documentation/
├── GameDesign/
├── Technical/
├── Roadmap/
└── Decisions/
```

Pastas vazias contêm `.gitkeep` apenas para serem versionadas. A Unity ignora arquivos iniciados por `.`, portanto eles não geram `.meta`. Os `.meta` das pastas serão gerados pela Unity na primeira importação e devem ser commitados.
