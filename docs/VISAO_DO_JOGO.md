# LITHOSTRIDE

## The Living Colossus

> *Construa sobre ele. Explore dentro dele. Sobreviva enquanto ele caminha.*

Documento de visão — primeira versão do documento oficial de conceito do jogo. Ele não fecha todas as decisões definitivamente, mas estabelece a identidade, os sistemas, o mundo, a progressão e a direção técnica do projeto.

---

## Sumário

1. [Conceito principal](#conceito-principal)
2. [Identidade do jogo](#identidade-do-jogo)
3. [Gênero](#gênero)
4. [Direção visual](#direção-visual)
5. [Os dois mapas principais](#os-dois-mapas-principais)
6. [Estrutura física do colosso](#estrutura-física-do-colosso)
7. [Camadas verticais do mundo](#camadas-verticais-do-mundo)
8. [Órgãos minerais](#órgãos-minerais)
9. [Movimento do colosso](#movimento-do-colosso)
10. [Dia e noite](#dia-e-noite)
11. [Biomas vivos](#biomas-vivos)
12. [Mundo exterior e eventos regionais](#mundo-exterior-e-eventos-regionais)
13. [Sistema de eventos combinados](#sistema-de-eventos-combinados)
14. [Ferimentos geológicos](#ferimentos-geológicos)
15. [Construção](#construção)
16. [Infraestrutura e transporte](#infraestrutura-e-transporte)
17. [Classes e estilos de jogo](#classes-e-estilos-de-jogo)
18. [Combate](#combate)
19. [Bosses](#bosses)
20. [NPCs e sociedades](#npcs-e-sociedades)
21. [Simulação durante a ausência](#simulação-durante-a-ausência)
22. [Exploração aérea](#exploração-aérea)
23. [Eventos do céu](#eventos-do-céu)
24. [Dimensões](#dimensões)
25. [Ecossistema](#ecossistema)
26. [Permanência e cicatrizes](#permanência-e-cicatrizes)
27. [Progressão](#progressão)
28. [Estrutura tecnológica](#estrutura-tecnológica)
29. [Arquitetura técnica do mundo](#arquitetura-técnica-do-mundo)
30. [Primeiro protótipo](#primeiro-protótipo)
31. [Primeira demonstração interna](#primeira-demonstração-interna)
32. [Escopo de uma versão comercial realista](#escopo-de-uma-versão-comercial-realista)
33. [Princípios de desenvolvimento](#princípios-de-desenvolvimento)
34. [Proteção do projeto](#proteção-do-projeto)
35. [Definição final do conceito](#definição-final-do-conceito)
36. [Melhorias aplicadas ao documento](#melhorias-aplicadas-ao-documento)

---

## Conceito principal

LITHOSTRIDE é um jogo sandbox survival 2D ambientado sobre um colosso mineral vivo, tão gigantesco que seu corpo funciona como um mundo completo.

Esse colosso não é um animal coberto por terra e não possui órgãos convencionais de carne. Ele é semelhante a um pequeno planeta vivo, formado por pedra, terra, cristais, minérios, magma, água subterrânea, energia e estruturas geológicas capazes de cumprir funções equivalentes às de órgãos.

Sobre seu corpo existem:

- florestas;
- montanhas;
- campos;
- rios;
- lagos;
- pântanos;
- desertos;
- cavernas;
- cidades;
- criaturas;
- civilizações;
- regiões aéreas;
- profundezas;
- estruturas minerais vivas.

Enquanto o jogador explora, constrói, luta e desenvolve assentamentos, o colosso continua caminhando por um mundo exterior ainda maior.

**O jogador vive sobre um mundo que está vivo, e esse mundo está viajando através de outro mundo.**

O colosso não é apenas o cenário. Seu movimento, seus ferimentos, seus órgãos, sua posição no mundo exterior e seu estado físico influenciam a jogabilidade constantemente.

---

## Identidade do jogo

O principal diferencial de LITHOSTRIDE é que o mapa jogável inteiro está em movimento.

Em outros jogos, o jogador atravessa um mundo estático. Em LITHOSTRIDE, o próprio mundo atravessa diferentes territórios enquanto o jogador vive sobre ele.

O jogador pode estar:

- construindo uma casa;
- minerando uma caverna;
- explorando uma floresta;
- defendendo uma cidade;
- atravessando uma montanha;
- investigando um órgão mineral;
- viajando pelo céu;
- explorando outra dimensão.

Enquanto isso, o colosso pode:

- caminhar;
- descansar;
- correr;
- subir uma montanha;
- atravessar um oceano;
- deitar;
- tropeçar;
- ficar ferido;
- aproximar-se de uma civilização;
- entrar em território inimigo;
- encontrar outro colosso.

**O mundo não espera pelo jogador.**

---

## Gênero

LITHOSTRIDE combina elementos de:

- sandbox 2D;
- survival;
- exploração;
- construção;
- mineração;
- combate;
- RPG;
- gerenciamento de assentamentos;
- simulação de ecossistemas;
- eventos dinâmicos;
- geração procedural;
- progressão baseada em equipamentos, conhecimento e exploração.

O jogo terá liberdade semelhante à de um sandbox, mas seus sistemas serão fortemente conectados ao estado e ao movimento do colosso.

---

## Direção visual

O jogo será apresentado em perspectiva lateral 2D.

O jogador enxerga:

- a superfície do colosso;
- as construções;
- os biomas;
- as cavernas;
- os personagens;
- as criaturas;
- o céu;
- o horizonte externo.

O diferencial visual estará nas várias camadas do fundo, que demonstrarão que o colosso está caminhando.

### Camadas de parallax

O cenário de fundo pode ser dividido em:

1. céu, estrelas e corpos celestes;
2. nuvens distantes;
3. cordilheiras e grandes formações;
4. cidades e civilizações externas;
5. florestas e estruturas próximas;
6. poeira, vegetação e criaturas externas;
7. superfície jogável do colosso.

Cada camada se movimenta em uma velocidade diferente.

As montanhas mais distantes passam lentamente. Árvores e estruturas próximas passam mais rápido. Isso cria uma sensação de profundidade e movimento sem precisar deslocar fisicamente todo o mapa jogável.

### Como o jogador percebe os passos

Durante uma caminhada comum, cada passo pode provocar:

- tremor leve na câmera;
- som grave e distante;
- pequenas ondas em lagos;
- folhas caindo;
- árvores balançando;
- poeira descendo do teto das cavernas;
- pequenas pedras rolando;
- cristais vibrando;
- animais reagindo;
- variações no fundo.

O efeito será sutil durante a caminhada normal para não tornar o jogo cansativo.

Quando o colosso corre, tropeça, é atacado ou perde o equilíbrio, os efeitos ficam muito mais intensos.

Uma opção de acessibilidade permitirá reduzir ou desligar o tremor da câmera, preservando os demais efeitos visuais e sonoros.

---

## Os dois mapas principais

LITHOSTRIDE possui duas escalas fundamentais.

### Mapa interno do colosso

Esse é o mapa jogável.

Ele representa o corpo do colosso e contém:

- biomas;
- cidades;
- cavernas;
- rios;
- montanhas;
- construções;
- estradas;
- ferrovias;
- teleféricos;
- postos avançados;
- órgãos minerais;
- ferimentos;
- áreas contaminadas;
- regiões desconhecidas;
- rotas subterrâneas.

O mapa não aparece completamente revelado desde o começo.

O jogador precisa preencher o mapa por meio de:

- exploração direta;
- torres de observação;
- mapas comprados;
- cartógrafos;
- NPCs exploradores;
- equipamentos de detecção;
- estruturas localizadas na cabeça;
- tecnologias desenvolvidas durante o jogo.

### Mapa do mundo exterior

Esse mapa mostra o colosso atravessando o mundo maior.

Nele, o jogador pode observar:

- posição atual do colosso;
- direção aproximada;
- velocidade;
- distância percorrida;
- território atual;
- regiões próximas;
- oceanos;
- desertos;
- florestas;
- reinos;
- cidades;
- exércitos;
- outros colossos;
- ameaças seguindo o colosso;
- eventos climáticos;
- possíveis destinos.

O mapa externo permite compreender a verdadeira escala do jogo.

Durante a jogabilidade comum, o movimento é percebido pelo fundo e pelas reações ambientais. Ao abrir o mapa externo, o jogador percebe o quanto o colosso realmente caminhou.

---

## Estrutura física do colosso

O colosso será tão grande que atravessá-lo da extremidade traseira até a cabeça será uma longa expedição.

Não será uma caminhada de poucos minutos. O jogador precisará construir infraestrutura, estabelecer postos e preparar recursos.

### Extremidade traseira

A extremidade traseira pode funcionar como uma das regiões iniciais.

Características:

- campos;
- florestas mais tranquilas;
- pequenas montanhas;
- cavernas superficiais;
- criaturas simples;
- primeiros assentamentos;
- menor quantidade de eventos extremos.

A região ainda será perigosa à noite, mas deverá introduzir os sistemas fundamentais do jogo.

### Terras baixas

São áreas que acumulam água e sedimentos.

Podem conter:

- pântanos;
- rios;
- lagos;
- florestas densas;
- campos alagados;
- criaturas venenosas;
- plantas raras;
- cidades construídas sobre plataformas.

Essas regiões são vulneráveis a inundações quando o colosso entra na água ou muda de inclinação.

### Costas

As costas formam uma das maiores regiões habitáveis.

Podem conter:

- grandes florestas;
- cordilheiras;
- cidades;
- rotas comerciais;
- plantações;
- ruínas;
- territórios amplos para construção;
- pontos de pouso para dirigíveis.

As costas são adequadas para grandes assentamentos, mas também ficam muito expostas a invasões e ameaças aéreas.

### Ombros

Os ombros possuem relevo inclinado e instável.

Características:

- penhascos;
- tempestades;
- fortes correntes de vento;
- criaturas voadoras;
- fortalezas;
- rotas estreitas;
- passagens para o pescoço.

Essa região exige equipamentos de mobilidade e construções mais resistentes.

### Pescoço

O pescoço funciona como um enorme bioma vertical.

Pode conter:

- paredões;
- cavernas;
- cachoeiras;
- plataformas naturais;
- teleféricos;
- correntes de vento;
- deslizamentos;
- passagens que se fecham com o movimento.

O pescoço é uma barreira natural entre as regiões comuns e a cabeça.

### Cabeça

A cabeça será uma das regiões mais perigosas e importantes.

Características:

- tempestades frequentes;
- grandes altitudes;
- pouco espaço estável;
- fortes movimentações;
- criaturas especiais;
- descargas de energia;
- proximidade com o Cérebro de Quartzo;
- visão privilegiada do caminho externo;
- acesso facilitado ao céu profundo.

A cabeça permite observar regiões exteriores antes que o restante do colosso chegue até elas.

---

## Camadas verticais do mundo

O mapa não será apenas horizontal. Também será dividido verticalmente.

### Céu

O céu será um conjunto completo de biomas e não apenas um espaço vazio acima do mapa.

Ele será dividido em níveis de altitude.

#### Baixa altitude

Área próxima da superfície.

Pode conter:

- aves;
- pequenos predadores;
- insetos;
- comerciantes em balões;
- ilhas flutuantes;
- correntes de ar;
- criaturas que pousam no colosso;
- recursos carregados pelo vento.

#### Alta altitude

Região acima das montanhas.

Perigos:

- frio;
- pouco oxigênio;
- tempestades;
- correntes violentas;
- criaturas maiores;
- raios;
- visibilidade reduzida.

Essa área exige planadores avançados, montarias ou veículos.

#### Céu profundo

Uma região quase celestial.

Características:

- gravidade reduzida;
- escuridão;
- fragmentos flutuantes;
- ruínas suspensas;
- tempestades elétricas;
- criaturas gigantescas;
- recursos raros;
- portais dimensionais.

O céu profundo será uma região avançada.

### Superfície

A superfície concentra:

- exploração;
- agricultura;
- cidades;
- construção;
- comércio;
- criaturas;
- eventos climáticos;
- invasões;
- guerras;
- desenvolvimento dos biomas.

É a camada mais afetada pelo território externo e pelo clima.

### Subsolo superficial

No começo, o subsolo parece relativamente comum.

Contém:

- cavernas;
- minas;
- lagos subterrâneos;
- fungos;
- ruínas;
- minérios;
- passagens escondidas;
- criaturas subterrâneas.

Conforme o jogador avança, percebe que as cavernas estão organizadas em sistemas e cumprem funções específicas.

### Subsolo funcional

Essa camada contém as estruturas minerais equivalentes aos órgãos do colosso.

Cada órgão é um bioma completo.

O jogador não entra em uma sala pequena e encontra um coração. Ele atravessa uma enorme região geológica e percebe gradualmente que todo aquele ambiente faz parte de um sistema vital.

### Profundezas

As profundezas são regiões de:

- calor;
- magma;
- pressão;
- terremotos;
- energia;
- gravidade irregular;
- estruturas vitais;
- criaturas antigas;
- minerais extremamente raros.

### Lado Sombrio

O Lado Sombrio funciona como o equivalente ao inferno, mas não será simplesmente uma região cheia de fogo e lava.

Ele representa uma parte enfraquecida, abandonada ou desconectada dos sistemas naturais do colosso.

Características:

- frio anormal;
- escuridão profunda;
- canais minerais mortos;
- terreno sem regeneração;
- ausência de circulação energética;
- gravidade instável;
- criaturas incomuns;
- silêncios prolongados;
- estruturas corrompidas;
- regiões que parecem não pertencer ao restante do colosso.

Enquanto o Coração Magmático representa energia e atividade, o Lado Sombrio representa ausência, isolamento e deterioração.

---

## Órgãos minerais

Os órgãos do colosso não são feitos de carne. São biomas geológicos vivos.

### Pulmões de Cristal

Os Pulmões de Cristal serão duas grandes redes de cavernas responsáveis pela circulação de ar e gases.

Elementos visuais:

- cristais que expandem e retraem;
- túneis com ventos intensos;
- bolsas de gás;
- pedras flutuantes;
- plantas aéreas;
- névoa brilhante.

Mecânicas:

- passagens abertas somente durante determinados ciclos de respiração;
- correntes capazes de lançar o jogador;
- gases perigosos;
- estruturas movidas pelo vento;
- turbinas;
- elevadores naturais;
- criaturas adaptadas à pressão.

Quando o colosso corre ou fica doente, os Pulmões entram em estado de respiração ofegante.

Isso pode provocar:

- tempestades internas;
- mudanças bruscas no vento;
- fechamento de passagens;
- liberação de gases;
- surgimento de novas rotas temporárias.

### Coração Magmático

O Coração Magmático bombeia magma, calor e energia pelo colosso.

Ele não é um pequeno objeto. É uma gigantesca formação de rocha rachada, cercada por rios de magma e estruturas minerais móveis.

Características:

- pulsações;
- terremotos;
- plataformas que aparecem e desaparecem;
- magma alterando de altura;
- minerais raros;
- temperaturas extremas;
- anéis minerais;
- canais energéticos.

Um batimento irregular pode:

- provocar erupções;
- esfriar algumas regiões;
- superaquecer outras;
- danificar canais;
- alterar o comportamento de criaturas;
- afetar construções próximas.

### Veias de Minério

As Veias de Minério atravessam quase todo o colosso.

Elas transportam:

- água;
- magma;
- calor;
- energia;
- cristais;
- minerais líquidos;
- partículas.

No início, são obstáculos naturais. Mais tarde, podem se transformar em rotas de transporte.

O jogador poderá construir:

- cápsulas;
- carrinhos;
- estações;
- filtros;
- desviadores;
- sistemas de coleta.

Uma veia bloqueada pode causar problemas em regiões distantes.

Por exemplo:

- um lago seca;
- uma floresta perde energia;
- cristais deixam de crescer;
- uma cidade perde iluminação;
- uma região congela;
- criaturas subterrâneas migram.

### Estômago de Erosão

O colosso pode consumir terra, montanhas, minerais e estruturas enquanto atravessa o mundo.

O Estômago de Erosão recebe e processa esses materiais.

Dentro dele podem surgir, temporariamente:

- pedaços de florestas externas;
- blocos de gelo;
- ruínas;
- construções;
- objetos;
- criaturas;
- minérios;
- tesouros;
- materiais desconhecidos.

O conteúdo ingerido é lentamente triturado ou dissolvido.

Isso permite criar biomas temporários.

Se o colosso consumir parte de uma região congelada, enormes blocos de gelo podem aparecer dentro do Estômago. Depois de certo tempo, tudo começa a derreter e desaparecer.

### Rins Minerais

Os Rins Minerais filtram água, venenos, sedimentos e resíduos.

A região possui:

- grandes lagos;
- filtros naturais;
- cristais formados por resíduos;
- piscinas contaminadas;
- canais de água pura;
- criaturas adaptadas a toxinas.

Quando os Rins são contaminados:

- rios da superfície ficam venenosos;
- plantações morrem;
- NPCs adoecem;
- animais sofrem mutações;
- pântanos se expandem;
- algumas cavernas ficam inabitáveis.

Nem todos os problemas serão resolvidos por combate. O jogador poderá precisar construir sistemas de filtragem, redirecionar canais ou remover substâncias perigosas.

### Esqueleto Montanhoso

A estrutura óssea do colosso será feita de minerais extremamente resistentes.

As montanhas da superfície podem ser partes expostas dessa estrutura.

Características:

- túneis verticais;
- cavernas estáveis;
- pontes naturais;
- minerais resistentes;
- fósseis;
- rotas profundas;
- baixa vulnerabilidade a desabamentos comuns.

Uma fratura no Esqueleto pode:

- inclinar uma região;
- abrir cavernas;
- destruir estradas;
- bloquear rotas;
- modificar o comportamento do colosso;
- afetar seu equilíbrio;
- provocar deslizamentos.

O jogador poderá reforçar fraturas com ligas minerais especiais.

### Cérebro de Quartzo

O Cérebro de Quartzo fica na cabeça.

É uma enorme rede de cristais capazes de transmitir impulsos e energia.

Características:

- descargas elétricas;
- ilusões;
- caminhos falsos;
- reflexos;
- estruturas flutuantes;
- mudanças de gravidade;
- tempestades energéticas.

Essa região poderá distorcer a percepção do jogador.

O mapa pode mostrar caminhos que não existem. Criaturas anteriores podem aparecer em versões cristalinas. Cenários podem se repetir ou alterar.

O jogador talvez consiga influenciar levemente a direção do colosso nessa região, mas nunca controlá-lo completamente.

### Núcleo Gravitacional

O Núcleo Gravitacional mantém a enorme estrutura do colosso unida.

É uma região avançada composta por:

- esfera central;
- rochas em órbita;
- magma flutuante;
- ilhas minerais;
- raios;
- ausência de chão fixo;
- gravidade variável.

Nessa região:

- paredes tornam-se pisos;
- criaturas caminham no teto;
- rios orbitam;
- projéteis mudam de direção;
- construções precisam ser ancoradas.

Uma instabilidade pode afetar toda a superfície:

- rios podem subir;
- pedras começam a flutuar;
- construções ficam mais pesadas;
- criaturas são lançadas;
- ilhas surgem temporariamente.

---

## Movimento do colosso

O colosso possuirá estados de movimento.

### Caminhando

Durante a caminhada comum:

- fundo se desloca lentamente;
- passos causam tremores leves;
- NPCs continuam trabalhando;
- construções permanecem estáveis;
- pequenas reações ambientais acontecem.

O personagem não escorrega constantemente. Todo o mapa compartilha o movimento do colosso.

### Descansando

Quando o colosso para:

- o fundo fica imóvel;
- tremores desaparecem;
- a poeira baixa;
- animais mudam o comportamento;
- comerciantes conseguem subir;
- invasores encontram mais facilidade;
- construção e mineração ficam mais estáveis;
- parasitas internos podem ficar mais ativos.

A ausência dos passos deve ser perceptível e até desconfortável depois de longos períodos de caminhada.

### Correndo

Quando o colosso corre:

- tremores aumentam;
- construções frágeis sofrem danos;
- objetos soltos são lançados;
- cavernas desabam;
- o fundo passa rapidamente;
- NPCs procuram abrigo;
- criaturas externas podem cair sobre o corpo;
- rios e lagos ficam instáveis.

O jogador poderá tentar descobrir do que o colosso está fugindo.

### Subindo

Quando o colosso sobe uma montanha externa:

- o corpo fica inclinado;
- rios mudam de direção;
- lagos transbordam;
- objetos escorregam;
- cachoeiras surgem;
- criaturas migram;
- algumas rotas ficam inacessíveis.

### Descendo

Durante a descida:

- a velocidade pode aumentar;
- pedras rolam;
- rios aceleram;
- construções sofrem tensão;
- o risco de queda aumenta.

### Deitando

Quando o colosso se deita:

- parte da superfície pode encostar no chão;
- regiões ficam bloqueadas;
- florestas podem ser esmagadas;
- cavernas são abertas;
- criaturas externas conseguem subir;
- um lado fica escuro;
- o lado oposto fica extremamente exposto.

### Entrando no oceano

Quando ele entra na água:

- regiões baixas são inundadas;
- cavernas acumulam água;
- criaturas marinhas aparecem;
- peixes entram em lagos;
- navios se aproximam;
- parasitas aquáticos se prendem ao colosso;
- regiões secas ficam férteis após a saída.

### Ferido

Quando sofre danos:

- surgem fraturas;
- o movimento muda;
- regiões ficam inclinadas;
- canais se rompem;
- órgãos entram em instabilidade;
- criaturas aproveitam as rachaduras;
- novos minerais ficam acessíveis.

---

## Dia e noite

O ciclo de tempo mudará o comportamento do mundo.

### Durante o dia

- exploração mais segura;
- criaturas pacíficas circulam;
- NPCs viajam;
- comerciantes trabalham;
- plantas florescem;
- construção fica mais previsível;
- ameaças noturnas permanecem escondidas;
- o horizonte externo fica visível.

### Durante a noite

- predadores saem das cavernas;
- criaturas externas sobem;
- algumas plantas ficam hostis;
- aldeias fecham os portões;
- luz e som atraem determinados monstros;
- criaturas aéreas descem;
- grandes seres circulam;
- regiões comuns mudam de comportamento.

### Escuridão viva

A escuridão pode funcionar como uma mecânica.

Quanto mais tempo o jogador permanece longe de fontes de luz:

1. sons estranhos começam;
2. formas aparecem ao fundo;
3. objetos parecem mudar de posição;
4. criaturas passam a observar;
5. a própria escuridão começa a produzir ameaças.

Isso torna a noite perigosa sem depender apenas de colocar muitos inimigos na tela.

---

## Biomas vivos

Os biomas não serão cenários estáticos.

Eles podem:

- crescer;
- recuar;
- migrar;
- contaminar áreas vizinhas;
- queimar;
- congelar;
- alagar;
- secar;
- regenerar;
- mudar após bosses;
- responder aos órgãos minerais.

Cada bioma poderá possuir um estado normal e um estado alterado.

### Floresta

**Estado normal:**

- árvores;
- animais;
- recursos;
- aldeias;
- plantas medicinais.

**Estado alterado:**

- raízes conectadas;
- caminhos fechados;
- plantas agressivas;
- animais controlados;
- núcleo vegetal;
- expansão para áreas vizinhas.

### Montanha

**Estado normal:**

- minérios;
- neve;
- cavernas;
- criaturas de altitude.

**Estado alterado:**

- atividade vulcânica;
- cristais crescendo;
- pedras se movendo;
- cavernas desabando;
- criatura enterrada despertando.

### Pântano

**Estado normal:**

- plantas raras;
- peixes;
- insetos;
- água parada.

**Estado alterado:**

- água venenosa;
- névoa;
- criaturas fossilizadas acordando;
- desaparecimento de NPCs;
- expansão da contaminação.

---

## Mundo exterior e eventos regionais

Os eventos dependem dos territórios pelos quais o colosso passa.

### Território goblin

Os goblins enxergam o colosso como uma fonte móvel de recursos.

Eles podem:

- escalar pelas pernas;
- utilizar balões;
- lançar ganchos;
- construir acampamentos;
- capturar NPCs;
- cortar árvores;
- instalar brocas;
- roubar minérios;
- ocupar cavernas.

Quanto mais tempo o colosso permanece no território, maior a ameaça.

Se o jogador não expulsar completamente os goblins, eles podem permanecer e formar uma comunidade própria.

### Deserto dos Vermes

No deserto, os passos atraem criaturas subterrâneas.

Eventos possíveis:

- vermes grudando nas pernas;
- tempestades de areia;
- novas cavernas;
- minerais trazidos do solo externo;
- criaturas seguindo o colosso;
- um verme gigantesco tentando derrubá-lo.

### Floresta de Esporos

Uma floresta externa libera esporos sobre o colosso.

Consequências:

- fungos surgem nas cavernas;
- animais sofrem mutações;
- NPCs adoecem;
- plantas mudam;
- uma rede coletiva começa a controlar regiões.

### Reinos humanos

Diferentes civilizações podem reagir de maneiras variadas.

Algumas podem:

- atacar;
- venerar;
- aprisionar;
- negociar;
- enviar refugiados;
- construir cidades;
- estudar o colosso;
- extrair recursos;
- tentar controlar sua direção.

### Oceano profundo

No oceano:

- regiões baixas alagam;
- monstros aquáticos aparecem;
- navios chegam;
- criaturas abissais se prendem;
- cavernas são inundadas;
- recursos marítimos ficam disponíveis.

---

## Sistema de eventos combinados

Os eventos não precisam ser completamente isolados.

O jogo poderá combinar:

- território externo;
- condição do colosso;
- bioma interno;
- horário;
- clima;
- ferimentos existentes;
- estado dos assentamentos.

### Exemplo 1

```
Território goblin
+ colosso descansando
+ noite
+ floresta
```

Resultado:

- goblins escalam;
- cortam árvores;
- constroem acampamentos;
- capturam NPCs;
- aproveitam a escuridão;
- podem permanecer após o colosso voltar a andar.

### Exemplo 2

```
Oceano
+ colosso ferido
+ tempestade
+ pântano
```

Resultado:

- pântano inundado;
- criaturas marinhas entrando pela fratura;
- raios atingindo regiões elevadas;
- parasitas aparecendo nas cavernas;
- água contaminada chegando aos Rins Minerais.

Eventos combinados aumentam muito a variedade sem exigir milhares de eventos totalmente independentes.

---

## Ferimentos geológicos

O colosso não possui uma simples barra de vida.

Seus ferimentos modificam fisicamente o mundo.

Tipos de ferimentos:

- fraturas;
- erosão;
- vazamentos magmáticos;
- canais rompidos;
- contaminação cristalina;
- desalinhamento gravitacional;
- perda de minerais;
- articulações danificadas;
- bloqueios internos;
- danos na cabeça.

Uma fratura pode:

- atravessar uma cidade;
- abrir cavernas;
- revelar minerais;
- permitir entrada de invasores;
- desviar rios;
- dividir um bioma;
- criar um desfiladeiro permanente.

O jogador pode:

- reparar;
- estabilizar;
- explorar;
- aproveitar recursos;
- ignorar;
- transformar o ferimento em parte da infraestrutura.

---

## Construção

As construções precisam considerar que o chão está vivo e em movimento.

O jogador poderá criar:

- casas;
- muralhas;
- torres;
- pontes;
- cidades;
- forjas;
- plantações;
- estações;
- abrigos;
- estradas;
- ferrovias;
- elevadores;
- teleféricos;
- dirigíveis;
- postos avançados.

### Estruturas adaptadas ao colosso

Algumas tecnologias:

- fundações flexíveis;
- cabos de sustentação;
- estabilizadores;
- para-raios;
- sistemas de drenagem;
- comportas;
- aquecedores;
- estruturas resistentes a terremotos;
- pontes retráteis;
- âncoras gravitacionais.

Uma casa comum pode funcionar na região inicial, mas não sobreviveria nas mesmas condições no pescoço ou perto do Coração Magmático.

A arquitetura precisa se adaptar ao ambiente.

---

## Infraestrutura e transporte

A distância entre as regiões será importante.

O jogador poderá construir:

- estradas;
- túneis;
- trilhos;
- trens;
- teleféricos;
- elevadores;
- tirolesas;
- portais limitados;
- estações;
- torres de descanso;
- rotas comerciais.

O teletransporte não será liberado cedo demais. Primeiro, o jogador deverá sentir a escala do colosso.

A infraestrutura construída contará a história da partida.

Uma ferrovia ligando a extremidade traseira à cabeça será uma conquista de longo prazo.

---

## Classes e estilos de jogo

As classes tradicionais serão reinterpretadas para funcionar dentro do conceito do colosso.

O jogador não ficará preso permanentemente a uma classe escolhida no começo.

A progressão poderá depender de:

- equipamentos;
- habilidades usadas;
- treinamentos;
- estruturas encontradas;
- especializações;
- interação com os órgãos minerais.

### Guardião Mineral

Equivalente ao guerreiro.

Especialidades:

- combate próximo;
- armas pesadas;
- proteção;
- resistência a tremores;
- destruição de armaduras;
- defesa de NPCs;
- escalada de criaturas.

Habilidades possíveis:

- Ancoragem;
- Impacto Sísmico;
- Muralha;
- Contra-ataque Tectônico;
- Escalada Brutal.

### Caçador dos Ventos

Equivalente ao arqueiro.

Especialidades:

- combate a distância;
- rastreamento;
- armadilhas;
- mobilidade;
- exploração aérea;
- planadores;
- montarias.

Habilidades possíveis:

- Flecha-Cabo;
- Rede Aérea;
- Marca do Predador;
- Passo Leve;
- Tiro Ascendente.

### Condutor do Colosso

Equivalente ao mago.

Utiliza forças minerais:

- magma;
- cristal;
- vento;
- água;
- gravidade;
- eletricidade;
- energia;
- escuridão.

O uso excessivo de magia próximo aos órgãos pode produzir reações ambientais.

### Construtor Nômade

Equivalente ao engenheiro.

Especialidades:

- máquinas;
- automação;
- defesa;
- veículos;
- trilhos;
- dirigíveis;
- filtros;
- estabilizadores;
- aproveitamento energético.

### Simbionte

Equivalente ao naturalista.

Especialidades:

- plantas;
- animais;
- cura;
- agricultura;
- venenos;
- domesticação;
- limpeza de contaminações;
- recuperação de biomas.

### Cartógrafo

Equivalente ao explorador.

Especialidades:

- mapeamento;
- previsão;
- rotas seguras;
- acampamentos;
- detecção de cavernas;
- observação;
- sobrevivência em ambientes extremos.

### Classes híbridas

O jogador poderá combinar especializações.

| Combinação | Classe híbrida |
|---|---|
| Guardião + Condutor | Cavaleiro Sísmico |
| Caçador + Construtor | Atirador Mecânico |
| Simbionte + Condutor | Druida Mineral |
| Guardião + Construtor | Guardião de Fortaleza |
| Cartógrafo + Caçador | Caçador Nômade |
| Construtor + Condutor | Artífice de Cristal |
| Cartógrafo + Condutor | Navegador Gravitacional |

As classes mudarão não apenas como o jogador causa dano, mas também como ele explora, constrói, protege e interage com o colosso.

---

## Combate

O combate poderá envolver:

- armas corpo a corpo;
- arcos;
- projéteis;
- poderes minerais;
- armadilhas;
- máquinas;
- montarias;
- veículos;
- ambiente.

O cenário deverá participar das batalhas.

Exemplos:

- usar um passo do colosso para derrubar inimigos;
- redirecionar magma;
- cortar uma ponte;
- provocar uma avalanche;
- usar correntes de vento;
- alterar gravidade;
- inundar um túnel;
- atrair criaturas para uma armadilha.

---

## Bosses

Os bosses não ficarão necessariamente presos em arenas.

Eles podem atravessar regiões, fugir, construir ninhos, contaminar biomas e alterar o mapa.

Ideias definidas:

- Rei Sucateiro Goblin;
- Devorador de Gigantes;
- Mente Micélio;
- Fortaleza Ambulante;
- Parasita Abissal;
- Guardião do Coração;
- Caçador de Colossos;
- Colônia Viva;
- Colosso Morto Controlado;
- Colosso Gêmeo;
- Tempestade Viva;
- Rei Parasita;
- Predador das Costas;
- Escavador;
- Sombra da Noite;
- criatura que tenta chegar à cabeça.

Alguns bosses produzirão consequências duradouras.

O cadáver de um boss pode:

- virar um bioma;
- gerar recursos;
- atrair criaturas;
- contaminar a região;
- servir como estrutura;
- ser ocupado por NPCs.

---

## NPCs e sociedades

Os NPCs não existirão apenas para vender itens.

Eles poderão criar:

- aldeias migratórias;
- cidades permanentes;
- fortalezas;
- acampamentos;
- comunidades subterrâneas;
- estações aéreas;
- postos comerciais.

Funções possíveis:

- caçadores;
- comerciantes;
- agricultores;
- mineradores;
- engenheiros;
- cartógrafos;
- exploradores;
- pesquisadores;
- guardas;
- construtores.

As sociedades poderão:

- crescer;
- migrar;
- especializar-se;
- entrar em conflito;
- abandonar regiões;
- construir defesas;
- sofrer invasões;
- desenvolver tecnologias;
- criar rotas comerciais.

Especializações de cidades:

- mineração;
- agricultura;
- comércio;
- defesa;
- pesquisa;
- exploração;
- transporte;
- construção;
- navegação aérea.

O jogador influencia essas comunidades, mas não controla cada cidadão diretamente.

---

## Simulação durante a ausência

O mundo continuará existindo quando o jogador estiver longe.

Regiões distantes serão simuladas de maneira simplificada.

O jogador poderá viajar para outra dimensão enquanto:

- o colosso continua andando;
- cidades consomem recursos;
- NPCs trabalham;
- invasões acontecem;
- construções são concluídas;
- biomas mudam;
- órgãos sofrem alterações;
- exploradores encontram regiões;
- o colosso pode ser ferido.

Antes de sair, o jogador pode escolher prioridades:

- **Defender**;
- **Preservar**;
- **Explorar**;
- **Reparar**;
- **Evacuar**;
- **Trabalhar**.

Ao retornar, receberá um relatório:

- tempo decorrido;
- distância percorrida;
- território atual;
- eventos;
- perdas;
- descobertas;
- recursos consumidos;
- condição dos órgãos;
- estado dos assentamentos.

O jogo não deve destruir tudo de maneira aleatória. O resultado dependerá das defesas, estoques, decisões e preparação do jogador.

---

## Exploração aérea

O jogador poderá utilizar:

- planadores;
- ganchos;
- montarias voadoras;
- balões;
- dirigíveis;
- navios celestes.

Quando estiver no ar, ele sairá parcialmente do referencial do colosso.

Se permanecer parado no céu, verá o colosso se afastando.

Isso cria uma mecânica importante:

**O mapa principal pode ir embora sem o jogador.**

O jogador precisará:

- acompanhar;
- pousar;
- alcançá-lo;
- seguir rastros;
- construir meios de retorno.

Do céu, será possível observar:

- cidades;
- montanhas;
- florestas;
- pernas;
- passos;
- pegadas;
- ferimentos;
- criaturas escalando;
- o tamanho real do colosso.

---

## Eventos do céu

### Tempestade capturada

Uma tempestade fica presa nas montanhas.

Consequências:

- raios;
- criaturas elétricas;
- cristais energizados;
- dirigíveis impedidos;
- estruturas elevadas ameaçadas.

### Migração celeste

Milhares de criaturas atravessam o céu.

Elas podem carregar:

- sementes;
- parasitas;
- ovos;
- predadores;
- comerciantes;
- novas espécies.

### Chuva de ilhas

Fragmentos flutuantes caem sobre o colosso.

Eles podem:

- destruir construções;
- abrir cavernas;
- trazer recursos;
- transportar criaturas;
- permanecer presos ao mapa.

### Noite sem estrelas

O céu fica completamente escuro.

Consequências:

- mapas imprecisos;
- criaturas aéreas descendo;
- luzes atraindo ameaças;
- portais aparecendo;
- navegação dificultada.

---

## Dimensões

As dimensões serão áreas de expedição.

O tempo do mundo principal continuará passando.

### Colosso Invertido

Uma versão distorcida do mapa:

- céu abaixo;
- profundezas acima;
- gravidade invertida;
- regiões em posições diferentes;
- áreas destruídas aparecendo intactas.

### Mar sem Chão

Uma dimensão oceânica:

- água aparentemente infinita;
- ilhas flutuantes;
- criaturas abissais;
- navegação;
- ausência de fundo conhecido.

### Reino das Tempestades

Uma dimensão formada por:

- nuvens;
- eletricidade;
- correntes;
- construções suspensas;
- criaturas energéticas.

### Mundo Imóvel

Uma dimensão onde:

- cachoeiras ficam paradas;
- criaturas permanecem congeladas;
- objetos não caem;
- o tempo funciona de maneira diferente;
- movimentos podem ser ativados pela aproximação.

### Dimensão Microscópica

O jogador fica extremamente pequeno.

Nesse espaço:

- cristais viram montanhas;
- gotas de água tornam-se oceanos;
- organismos minúsculos viram monstros;
- contaminações do colosso podem ser investigadas.

---

## Ecossistema

O mundo responderá às ações do jogador.

Se muitos predadores forem mortos:

- presas se multiplicam;
- plantações são atacadas;
- doenças se espalham;
- um novo predador ocupa a região.

As espécies podem:

- migrar;
- reproduzir-se;
- diminuir;
- desaparecer;
- invadir outros biomas;
- sofrer mutações.

O objetivo não é simular biologicamente cada criatura o tempo inteiro. Regiões distantes utilizarão cálculos estatísticos para manter o mundo viável.

---

## Permanência e cicatrizes

Os eventos deixam marcas.

Exemplos:

- uma cidade destruída permanece em ruínas;
- uma floresta queimada cresce lentamente;
- uma fratura continua aberta;
- um boss morto transforma a região;
- acampamentos goblins crescem;
- rios mudam de direção;
- uma ferrovia permanece na paisagem;
- montanhas quebradas não retornam imediatamente;
- cavernas abertas continuam existindo.

O mapa funciona como um registro da história daquela partida, mesmo sem uma narrativa principal definida.

---

## Progressão

A progressão não será apenas baseada em armas com números maiores.

Cada região apresenta perigos próprios:

- frio;
- calor;
- queda;
- vento;
- veneno;
- falta de oxigênio;
- pressão;
- escuridão;
- eletricidade;
- gravidade;
- contaminação;
- instabilidade.

O jogador precisará preparar:

- equipamentos;
- construções;
- ferramentas;
- consumíveis;
- veículos;
- rotas;
- abrigos;
- aliados.

Armaduras poderosas não resolverão todos os problemas ao mesmo tempo.

---

## Estrutura tecnológica

A estrutura recomendada para o desenvolvimento é:

| Área | Escolha |
|---|---|
| Engine | Unity 6 |
| Linguagem | C# |
| Renderização | URP 2D |
| Mundo | Tilemap dividido em chunks |
| Câmera | Cinemachine |
| Controles | Unity Input System |
| Dados | ScriptableObjects |
| Persistência | Saves locais versionados |
| Otimização | Burst e Job System quando necessário |
| Grandes simulações | ECS apenas quando realmente necessário |
| Editor de código | VS Code |
| Versionamento | Git |
| Repositório | GitHub privado |
| Arquivos grandes | Git LFS |
| Arte | Aseprite, Krita e Blender quando necessário |

O jogo single-player não precisará inicialmente de:

- NestJS;
- Node.js;
- Axios;
- Sequelize;
- banco SQL;
- servidor separado.

Tudo será executado localmente em C#.

---

## Arquitetura técnica do mundo

O mapa será dividido em chunks.

Cada chunk poderá armazenar:

- blocos;
- paredes;
- líquidos;
- vegetação;
- criaturas;
- construções;
- iluminação;
- temperatura;
- umidade;
- contaminação;
- bioma;
- modificações do jogador.

### Estados de simulação

#### Ativo

Região próxima ao jogador:

- física;
- inteligência artificial;
- animações;
- combate;
- partículas;
- líquidos;
- iluminação completa.

#### Simplificado

Região próxima, mas fora da tela:

- movimentação resumida;
- produção;
- crescimento;
- eventos menores;
- sem renderização detalhada.

#### Estatístico

Região distante:

- população;
- recursos;
- condição ambiental;
- danos;
- progresso de eventos;
- sem entidades físicas individuais.

Isso permite que o mundo continue funcionando sem processar cada elemento do colosso ao mesmo tempo.

---

## Primeiro protótipo

O primeiro protótipo não tentará criar o jogo inteiro.

Ele terá:

- personagem provisório;
- movimento;
- corrida;
- pulo;
- pequena floresta;
- montanha;
- caverna;
- fundo em parallax;
- colosso parado;
- colosso caminhando;
- tremores;
- água e vegetação reagindo;
- botão para alternar estados.

**Objetivo:** fazer o jogador sentir que está sobre algo gigantesco que está andando.

---

## Primeira demonstração interna

Depois, será criada uma fatia vertical contendo:

- mineração;
- madeira;
- pedra;
- inventário;
- barra rápida;
- crafting;
- construção;
- abrigo;
- dia e noite;
- três estilos básicos de combate;
- três tipos de inimigos;
- três NPCs;
- evento goblin;
- fratura geológica;
- veia mineral;
- mapa interno;
- mapa externo;
- salvamento.

### Fluxo esperado

1. O jogador desperta em um abrigo.
2. O colosso está caminhando.
3. Montanhas passam ao fundo.
4. O jogador coleta recursos.
5. Explora uma caverna.
6. Encontra uma veia mineral.
7. Melhora seu abrigo.
8. A noite começa.
9. O colosso para.
10. Goblins aparecem no horizonte.
11. A invasão começa.
12. Uma broca cria uma fratura.
13. A fratura revela uma caverna.
14. O jogador enfrenta os invasores.
15. O colosso volta a caminhar.
16. O mapa externo mostra a saída do território goblin.

Essa versão prova a identidade fundamental do LITHOSTRIDE.

---

## Escopo de uma versão comercial realista

Uma primeira versão completa poderá conter:

- quatro a sete biomas principais;
- extremidade traseira;
- costas;
- ombros;
- pescoço;
- cabeça;
- céu explorável;
- profundezas;
- Lado Sombrio;
- dois ou três órgãos minerais;
- quatro estilos de classe;
- especializações híbridas;
- 25 a 50 criaturas;
- seis a doze bosses;
- NPCs;
- assentamentos;
- eventos combinados;
- construção;
- crafting;
- mineração;
- ferimentos geológicos;
- mapa externo;
- dezenas de horas de conteúdo;
- single-player;
- lançamento inicial para Windows.

Dimensões, multiplayer, outros colossos e sistemas extremamente avançados podem ser adicionados posteriormente.

---

## Princípios de desenvolvimento

### Provar antes de expandir

Cada sistema será implementado em uma versão pequena e testável.

### Não construir o jogo inteiro de uma vez

O projeto será dividido em blocos.

### O colosso precisa afetar a jogabilidade

Se o colosso for apenas um fundo bonito, o conceito falhou.

### O mundo deve ser grande, mas não vazio

Cada região precisa ter identidade, recursos, perigos e eventos.

### Eventos precisam ser justos

O jogador deve receber sinais, previsões e formas de preparação.

### Construções não podem ser destruídas constantemente

A caminhada comum será segura. Eventos extremos gerarão riscos maiores.

### Classes oferecem soluções diferentes

Nenhuma classe deve bloquear permanentemente partes essenciais do conteúdo.

### A escala precisa ser sentida

O jogador deve atravessar, construir rotas e observar o colosso de diferentes perspectivas.

### O mundo não espera pelo jogador

Cidades, criaturas, biomas e eventos continuam existindo.

### Consequências precisam permanecer

O mapa deve guardar lembranças da partida.

---

## Proteção do projeto

O desenvolvimento será mantido inicialmente em sigilo.

Medidas planejadas:

- GitHub privado;
- autenticação em dois fatores;
- commits frequentes;
- documentação datada;
- backups externos;
- controle de acesso;
- contratos com colaboradores;
- organização de licenças;
- registro da marca;
- registro de versões relevantes do código;
- nenhum material sigiloso em repositórios públicos.

O projeto não precisa ser revelado antes de possuir uma execução sólida.

---

## Definição final do conceito

LITHOSTRIDE: The Living Colossus é um sandbox survival 2D em que o jogador vive sobre um colosso mineral gigantesco. O corpo do colosso é formado por biomas, cidades, cavernas, montanhas, organismos minerais e estruturas geológicas equivalentes a órgãos.

Enquanto o jogador explora e transforma esse mundo, o colosso caminha continuamente por um planeta muito maior.

O caminho externo altera:

- clima;
- recursos;
- criaturas;
- invasões;
- civilizações;
- biomas;
- eventos;
- disponibilidade de materiais.

O estado interno do colosso altera:

- terremotos;
- rios;
- gravidade;
- temperatura;
- cavernas;
- vegetação;
- assentamentos;
- circulação de energia.

O jogador poderá viver na superfície, explorar as profundezas, alcançar a cabeça, viajar pelo céu e entrar em outras dimensões. Porém, o colosso continuará caminhando durante sua ausência.

O diferencial não é apenas existir um gigante.

O diferencial é que:

- o gigante é o mapa;
- o mapa é vivo;
- o mapa está caminhando;
- seu movimento modifica o jogo;
- suas feridas transformam o cenário;
- seus órgãos são biomas;
- suas viagens trazem novos eventos;
- o jogador jamais controla tudo completamente.

### Frase central

> O jogador não vive apenas em um mundo. Ele vive sobre um mundo que está vivo, caminhando através de outro mundo.

### Frase comercial

> Construa sobre ele. Explore dentro dele. Sobreviva enquanto ele caminha.

### Nome oficial

**LITHOSTRIDE**

### Subtítulo

**The Living Colossus**

### Significado do nome

- **Litho** representa pedra, rocha e a natureza mineral do colosso.
- **Stride** representa uma passada longa, forte e imponente.

Juntos, formam a ideia de: **um mundo de pedra que caminha.**

---

## Melhorias aplicadas ao documento

### Organização

As ideias foram separadas em visão geral, mundo, sistemas, progressão, conteúdo, tecnologia e desenvolvimento. Isso transforma a conversa inicial em uma base utilizável para o projeto.

### Clareza

Foram distinguidos o mapa interno, o mundo exterior, o céu, a superfície, o subsolo e os órgãos minerais, evitando misturar escalas diferentes.

### Escopo

O documento diferencia a visão completa do jogo, o primeiro protótipo, a demonstração interna e uma versão comercial realista. Isso ajuda a preservar as ideias maiores sem comprometer o início do desenvolvimento.

### Identidade

Todo o conteúdo foi centralizado no diferencial mais importante: o colosso não é apenas cenário, ele é o próprio mundo jogável e continua caminhando enquanto tudo acontece sobre e dentro dele.
