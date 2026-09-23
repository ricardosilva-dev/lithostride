# CLAUDE.md

## Projeto

- **LITHOSTRIDE: The Living Colossus**
- Unity 6 (LTS), C#
- Sandbox survival 2D (visão lateral)
- Single-player, offline, save local
- Windows inicialmente

> Estado em 2026-09-23: o repositório tem a estrutura de pastas e a documentação, mas **o projeto Unity ainda não foi criado** (sem `Packages/` e `ProjectSettings/`). Ver `Documentation/Technical/TECH_STACK.md`, seção 17.

## Documentos obrigatórios

Antes de qualquer alteração, ler:

- `docs/VISAO_DO_JOGO.md` — visão completa do jogo (não reescrever nem resumir sem pedido);
- `Documentation/Technical/TECH_STACK.md` — stack, arquitetura e escopo;
- `README.md`;
- decisões relevantes em `Documentation/Decisions/`.

## Regras de edição

- Analisar antes de alterar.
- Não editar `Library/`, `Temp/`, `Logs/`, `obj/`, `Build/`, `Builds/`, `UserSettings/`, `MemoryCaptures/`, `Recordings/`.
- Não alterar arquivos `.meta` sem necessidade; nunca criá-los manualmente.
- Não editar cenas, prefabs ou assets serializados por texto quando isso puder corrompê-los.
- Não adicionar ou remover pacotes sem autorização.
- Não criar sistemas futuros antecipadamente — uma funcionalidade por vez.
- Fornecer arquivos completos ao alterar código.
- Listar arquivos criados e modificados.
- Explicar configurações necessárias no editor.
- Preservar a compilação; nunca afirmar que compilou sem ter validado.
- Evitar complexidade prematura (sem interfaces, services, repositories, factories ou singletons sem necessidade concreta).
- Não basear a arquitetura em `FindObjectOfType`, `FindFirstObjectByType`, `GameObject.Find` ou buscas globais.
- Não usar banco de dados, backend separado, Docker nem networking nesta fase.
- Não fazer commit ou push sem pedido.

## Padrão C#

- PascalCase para classes, métodos, propriedades e enums.
- camelCase para parâmetros e variáveis locais.
- Campos privados em camelCase.
- Campos do Inspector: `private` com `[SerializeField]`.
- Uma classe pública principal por arquivo; nome do arquivo igual ao da classe.
- Usar namespaces do projeto.
- Evitar `#region` excessivo.
- Comentar decisões, não sintaxe; evitar comentários óbvios.
- `sealed` quando a herança não for necessária.
- Preferir composição.
- Evitar alocações em `Update` e `FixedUpdate`.
- `Update` para entrada, `FixedUpdate` para física, quando aplicável.
- Não usar `async void`, exceto em handlers de eventos compatíveis.
- Null checks apropriados (lembrar que objetos Unity destruídos comparam `== null`; não usar `?.`/`??` em `UnityEngine.Object`).
- Scripts pequenos e focados.
- ScriptableObjects só para definições estáticas; estado da partida em classes C# serializáveis.

### Namespaces

Raiz: `Lithostride`. Cada namespace corresponde a uma pasta em `Assets/Game/`:

- `Lithostride.Core`
- `Lithostride.Player`
- `Lithostride.World`
- `Lithostride.Colossus`
- `Lithostride.Combat`
- `Lithostride.Creatures`
- `Lithostride.NPCs`
- `Lithostride.Settlements`
- `Lithostride.Building`
- `Lithostride.Items`
- `Lithostride.Crafting`
- `Lithostride.Events`
- `Lithostride.Sky`
- `Lithostride.Underground`
- `Lithostride.Saving`
- `Lithostride.UI`

## Formato das respostas

Ao concluir uma alteração, responder com:

1. Resumo do que foi feito.
2. Arquivos criados.
3. Arquivos alterados.
4. Passos manuais necessários na Unity.
5. Como testar.
6. Riscos ou limitações.
7. Próxima tarefa recomendada.
