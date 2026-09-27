# 0002 — Menu principal

- **Data:** 2026-09-23
- **Estado:** aceita
- **Escopo:** `Assets/Game/UI`, `Assets/Game/Core/GameSettings.cs`, `Assets/Game/Editor`

## Contexto

Havia um mockup do menu principal: título, cinco entradas (Jogar, Configurações,
Conquistas, Extras, Sair), chamada no canto superior direito, epígrafe no canto
inferior direito e um cartão de "última partida" no canto inferior esquerdo,
tudo sobre uma ilustração do colosso.

O protótipo ainda não tem save, conquistas nem extras.

## Decisões

### 1. O fundo do menu é o jogo, não uma ilustração

A cena do menu usa um `ColossusBody` caminhando e as mesmas cinco camadas de
`ParallaxLayer` da cena de jogo. Nenhum código novo de fundo, nenhuma arte nova,
e o menu já comunica o conceito: o mundo se move enquanto o jogador decide.

Quando existir arte final, ela entra como mais uma camada ou substitui os PNGs
provisórios, sem mexer na estrutura.

### 2. Só entra no menu o que tem sistema por trás

- **Jogar** carrega a cena de jogo.
- **Configurações** abre um painel com três preferências reais.
- **Sair** encerra.
- **Conquistas** e **Extras** ficam visíveis, desativados e marcados como
  "em breve", porque o mockup os prevê e esconder a estrutura seria pior que
  mostrar o que falta.
- O **cartão de última partida** foi deixado de fora. Sem sistema de save, ele
  só poderia exibir dados falsos.

### 3. Preferências em PlayerPrefs, não em singleton de cena

`GameSettings` é uma classe estática fina sobre `PlayerPrefs`. Não guarda estado
próprio, não é `MonoBehaviour` e não precisa existir em cena, então atravessa a
troca de cenas sem `DontDestroyOnLoad` nem objeto global.

`ColossusStepImpulse` lê `GameSettings.ShakeScale` no momento do passo. A
preferência de acessibilidade continua tendo um único ponto de controle, agora
alcançável pelo menu.

O save da partida terá formato próprio (JSON versionado, conforme a seção 7 do
`TECH_STACK.md`); `PlayerPrefs` é só para preferências.

### 4. A cena do menu também é gerada por comando do editor

Mesma decisão do documento [0001](0001-prototipo-marcos-1-e-2.md), pelos mesmos
motivos. A montagem de uGUI por código ficou em `UiFactory`, compartilhada entre
a HUD do jogo e o menu, e o preenchimento de campos serializados virou
`SerializedWiring`, antes privado do construtor da cena de jogo.

### 5. As cenas entram na lista de build pelo construtor

`SceneManager.LoadScene` falha se a cena não estiver registrada. `SceneRegistration`
garante `MainMenu` em primeiro e `Prototype` em seguida a cada construção, para
que o fluxo menu → jogo → menu funcione no editor e no executável sem
configuração manual.

## Consequências

- O protótipo passa a ter um fluxo completo: menu → jogo → `Esc` → menu.
- A fonte é a padrão do TextMeshPro. A tipografia do mockup depende de uma fonte
  própria, que é tarefa de arte, não de código.
- Conquistas e Extras vão precisar de decisão própria quando existirem.
- Compilado e construído junto com o resto, na Unity 6000.6.2f1, sem erros.
