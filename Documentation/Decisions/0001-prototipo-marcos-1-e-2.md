# 0001 — Protótipo: primeiro e segundo marcos técnicos

- **Data:** 2026-09-23
- **Estado:** aceita
- **Escopo:** `Assets/Game/Core`, `Colossus`, `Player`, `World`, `UI`, `Editor`

## Contexto

O objetivo do primeiro protótipo, segundo a visão do jogo, é único: *fazer o
jogador sentir que está sobre algo gigantesco que está andando*. O primeiro e o
segundo marcos técnicos do `TECH_STACK.md` foram implementados juntos porque o
primeiro sozinho (personagem, chão, câmera) não prova nada sobre o conceito.

## Decisões

### 1. O colosso é uma coordenada abstrata, não um terreno que se move

`ColossusBody` guarda `TravelDistance`, uma distância acumulada no mundo
exterior, e dispara `StepTaken` no ritmo da marcha atual. O terreno jogável
nunca é deslocado. Parallax, tremor, vegetação, água e poeira leem esse estado.

Isso segue a seção 11 do `TECH_STACK.md` e evita o custo e os erros de
acumulação de mover fisicamente o mapa inteiro.

### 2. Estados de movimento limitados a três

Só `Resting`, `Walking` e `Running`. Os demais estados da visão (subindo,
descendo, deitado, submerso, ferido) ficam de fora até existir sistema que os
use — não há enum "preparado para o futuro".

### 3. Referências pelo Inspector, sem buscas globais nem singletons

Nenhum componente procura o `ColossusBody` em tempo de execução. Todos recebem
a referência por campo serializado, preenchido pelo construtor de cena.
`PrototypeInput` é o único ponto que lê o Input System.

### 4. A cena é gerada por um comando do editor

`PrototypeSceneBuilder` cria `Assets/Scenes/Prototype/Prototype.unity` inteira
por código, e `PrototypeArtGenerator` gera a arte provisória em PNG.

Motivos:

- cenas são assets serializados; escrevê-las à mão é arriscado;
- o protótipo fica reproduzível a partir do repositório, sem depender de arte
  externa nem de montagem manual no editor;
- o terreno provisório (relevo, montanha, bacia do lago, caverna) vem de uma
  função determinística, então dá para ajustá-lo alterando números em um só
  lugar.

**Custo aceito:** reconstruir a cena descarta ajustes manuais feitos nela.

### 5. Detecção de chão sem layer dedicada

`GroundSensor` usa `Physics2D.OverlapBox` com um `ContactFilter2D` sem máscara e
descarta os colisores do próprio `Rigidbody2D`. Evita criar uma layer "Ground" e,
com isso, evita uma etapa de configuração manual do projeto que precisaria ser
repetida a cada clone.

### 6. Cinemachine é dependência real do protótipo

O tremor dos passos usa `CinemachineImpulseSource` em vez de mexer no Transform
da câmera, conforme o `TECH_STACK.md`. O uso está isolado em dois arquivos
(`ColossusStepImpulse.cs` e `PrototypeSceneBuilder.cs`), então é possível
remover o pacote apagando esses dois pontos.

A intensidade do tremor tem um único ponto de controle, em `ColossusStepImpulse`,
para atender ao requisito de acessibilidade sem apagar os demais sinais do passo.

### 7. Sem Assembly Definitions ainda

Quatorze arquivos não justificam separar assemblies. A pasta
`Assets/Game/Editor/` já isola o código de editor pelo nome, que é o que a Unity
exige.

## Consequências

- O projeto Unity passou a existir no repositório no mesmo dia. Ver
  [`SETUP_PROTOTIPO.md`](../Technical/SETUP_PROTOTIPO.md).
- **Compilado e validado** na Unity 6000.6.2f1, sem erros nem avisos, e as duas
  cenas geradas em batchmode. Uma única correção de API foi necessária:
  `ContactFilter2D.NoFilter()` está obsoleto na Unity 6, trocado pela
  propriedade estática `ContactFilter2D.noFilter` em `GroundSensor`.
- Não há save, inventário, mineração, ciclo de dia e noite nem criaturas. Isso é
  a primeira demonstração interna, não o protótipo.
