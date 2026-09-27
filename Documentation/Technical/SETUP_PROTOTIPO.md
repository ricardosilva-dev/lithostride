# LITHOSTRIDE — Como colocar o protótipo para rodar

> Este documento cobre os passos que **precisam ser feitos dentro da Unity** e que
> não podem ser feitos por edição de arquivos. O código do protótipo já está no
> repositório, em `Assets/Game/`.
>
> Escopo do protótipo: primeiro e segundo marcos técnicos do
> [`TECH_STACK.md`](TECH_STACK.md) e a seção "Primeiro protótipo" de
> [`docs/VISAO_DO_JOGO.md`](../../docs/VISAO_DO_JOGO.md).
>
> Última revisão: 2026-09-23 — validado na Unity 6000.6.2f1

---

## 0. Situação atual

**O projeto está montado e roda.** Validado em 2026-09-23 na
**Unity 6000.6.2f1**: compilação sem erros nem avisos, e as duas cenas geradas
em batchmode (terreno com 10.252 células).

Se você acabou de clonar o repositório, pule para o passo 3 — `Packages/` e
`ProjectSettings/` estão versionados e o projeto abre direto.

As seções 1 e 2 ficam registradas para quem precisar remontar do zero.

---

## 1. Instalar Unity

1. Instalar o **Unity Hub**.
2. Pelo Hub, instalar a **Unity 6000.6.2f1** (a versão registrada em
   `ProjectSettings/ProjectVersion.txt`).
3. **Nenhum módulo é necessário para apertar Play.** O *Windows Build Support
   (IL2CPP)* só faz falta na hora de gerar o `.exe`, e pode ser adicionado
   depois em Hub → Instalações → engrenagem → *Adicionar módulos*.

---

## 2. Remontar o projeto do zero (só se necessário)

O Unity Hub não cria projeto em pasta não vazia. Em vez do caminho da seção 17
do `TECH_STACK.md` (criar projeto temporário e copiar), dá para extrair o
template direto do editor instalado:

```
<editor>\Editor\Data\Resources\PackageManager\ProjectTemplates\
    com.unity.template.2d-cross-platform-2d-7.0.0.tgz
```

Dentro do `.tgz`, `package/ProjectData~/` contém `Packages/`, `ProjectSettings/`
e `Assets/Settings/`. Copiar para a raiz do repositório:

- `Packages/` e `ProjectSettings/` inteiros;
- de `Assets/Settings/`: `UniversalRP.asset`, `Renderer2D.asset`,
  `UniversalRenderPipelineGlobalSettings.asset`, `DefaultVolumeProfile.asset`
  — **sempre com os `.meta`**, senão os GUIDs referenciados em
  `GraphicsSettings.asset` quebram e o projeto volta para o pipeline padrão;
- **não** copiar `Assets/Welcome` nem `Assets/Scenes/SampleScene` (tutorial).

Depois, acrescentar `"com.unity.cinemachine": "3.1.7"` ao
`Packages/manifest.json` e criar `ProjectSettings/ProjectVersion.txt` com
`m_EditorVersion: 6000.6.2f1`.

> Ao editar esses arquivos por script, **não grave com BOM**. O
> `ProjectSettings.asset` precisa começar em `%YAML 1.1`. O `-Encoding utf8` do
> PowerShell 5.1 adiciona BOM; use `[System.IO.File]::WriteAllText` com
> `UTF8Encoding($false)`.

---

## 3. O que já vem configurado

Nada disso precisa ser feito à mão — veio do template e está versionado:

| Item | Estado |
|---|---|
| URP 2D | `17.6.0`, com `UniversalRP.asset` e `Renderer2D.asset` atribuídos |
| Input System | `1.19.0`, com `activeInputHandler: 1` (Input System Package) |
| Cinemachine | `3.1.7` (namespace `Unity.Cinemachine`) |
| Tilemap, 2D Animation, uGUI/TextMeshPro | do template 2D |
| Cenas no build | `MainMenu` primeiro, `Prototype` em seguida |

Único passo manual que pode aparecer: se a HUD e o menu abrirem sem texto,
rodar `Window → TextMeshPro → Import TMP Essential Resources`.

---

## 4. Gerar a arte provisória e as cenas

As cenas já estão construídas e versionadas. Reconstruir só é necessário depois
de mexer nos construtores ou no relevo.

**Um comando só:** `Lithostride → Protótipo → Construir tudo`.

Ele gera a arte provisória, constrói a cena de jogo, constrói o menu principal,
registra as duas cenas na lista de build (menu em primeiro) e deixa o menu
aberto, pronto para o Play.

Também roda sem abrir o editor:

```
"<editor>\Editor\Unity.exe" -batchmode -quit ^
  -projectPath "<repo>" ^
  -executeMethod Lithostride.EditorTools.PrototypeSceneBuilder.BuildAll ^
  -logFile "<log>"
```

Os comandos individuais existem para quando só uma parte mudou:

| Comando | O que faz |
|---|---|
| `Gerar arte provisória` | PNGs em `Assets/Art/Prototype/`: tiles, personagem, árvores, água, poeira, as cinco camadas de parallax e os elementos de UI |
| `Construir cena de jogo` | recria `Assets/Scenes/Prototype/Prototype.unity` |
| `Construir menu principal` | recria `Assets/Scenes/MainMenu/MainMenu.unity` |

> Construir **substitui a cena inteira**. Qualquer ajuste manual feito nela é
> perdido. Enquanto o protótipo estiver mudando muito, ajuste os campos
> serializados dos componentes e, quando gostar do resultado, leve os valores
> para o construtor.

---

## 5. Rodar

Abrir `Assets/Scenes/MainMenu/MainMenu.unity` e apertar Play. O fundo do menu é
o próprio colosso caminhando, com as mesmas camadas de parallax do jogo.

No menu: **JOGAR** carrega a cena de jogo, **CONFIGURAÇÕES** abre o painel de
preferências e **SAIR** encerra. *Conquistas* e *Extras* aparecem desativados
porque ainda não existem os sistemas por trás deles.

Dentro do jogo:

| Tecla | Ação |
|---|---|
| `A` / `D` ou setas | mover |
| `Shift` esquerdo | correr |
| `Espaço` | pular |
| `J`, clique esquerdo ou botão oeste do controle | atacar com a espada |
| `1` | colosso descansando |
| `2` | colosso caminhando |
| `3` | colosso correndo |
| `K` | liga/desliga o tremor de câmera |
| `Esc` | voltar ao menu principal |

Os mesmos comandos estão nos botões da HUD, no canto inferior esquerdo.

O primeiro boss, o Predador das Costas, dorme na arena à direita, depois da
montanha. Ver [decisão 0004](../Decisions/0004-primeiro-boss.md).

### Configurações disponíveis

Ficam em `PlayerPrefs` e valem entre execuções:

- **Tremor de câmera** — de 0% a 100%. Em 0% o tremor some, mas a poeira, o
  balanço da vegetação e as ondas continuam marcando os passos do colosso.
- **Volume geral**
- **Tela cheia**

---

## 6. Build para Windows

1. `File → Build Profiles` (ou `Build Settings`), plataforma **Windows**.
2. A lista de cenas já vem preenchida pelo comando *Construir tudo*:
   `MainMenu` primeiro, `Prototype` em seguida. Conferir a ordem.
3. Build para uma pasta fora do repositório, ou para `Build/`, que já está no
   `.gitignore`.
