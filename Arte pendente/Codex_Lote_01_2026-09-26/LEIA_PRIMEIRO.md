# LITHOSTRIDE — Arte complementar, lote 01

Lote criado a pedido de Ricardo para integração posterior. Esta pasta contém fontes visuais novas e um catálogo; não contém implementação de gameplay nem configurações de importação da Unity.

Destino no projeto: `C:\Users\Ricardo\Documents\Projetos\lithostride\Arte pendente\Codex_Lote_01_2026-09-26`.

## Separação do trabalho atual

- Este lote foi mantido fora de `Assets` e fora de `Pack completo`, para não mudar o conjunto de arquivos que o Claude pode estar importando.
- Nenhum script, cena, prefab, `.meta`, pacote, configuração, documento existente ou imagem do pack original foi alterado por esta entrega.
- Esta tarefa entrega fontes de arte. A integração dos dois lotes pelo Claude já foi solicitada por Ricardo e está descrita no novo prompt PROMPT_CLAUDE_HARMONIA_MAPA_E_CORRECOES.md, entregue junto do material.
- Os novos desenhos não definem novas mecânicas obrigatórias. Bancadas, recursos e criaturas podem começar como elementos de teste.

## Como usar depois

1. Conferir o catálogo e escolher as pranchas desejadas.
2. Copiar os derivados necessários para `Assets` somente na tarefa de integração, preservando estas fontes.
3. Inspecionar cada PNG com fundo claro/escuro e medir seus recortes. As grades pedidas nos prompts são orientação de composição, NÃO retângulos de corte já validados.
4. Ajustar escala, pivôs e contato com o terreno. Plantas foram solicitadas sem blocos de suporte, mas o encaixe exato nos tiles precisa ser conferido no jogo.
5. Nas sequências animadas, revisar ordem, anatomia, baseline/centro do corpo e duração. Frames gerados não equivalem a uma animação testada.
6. Verificar resíduos de alfa baixo antes de empilhar sprites. Preservar transparência legítima de efeitos; não remover cores ou aplicar um limiar agressivo global.
7. Tratar ruínas e rochas como props independentes. Este lote não adiciona tiles com repetição ou encaixe modular garantidos.

## Conteúdo e rastreabilidade

`CATALOGO.md` descreve o conteúdo observado e os cuidados por prancha. `manifesto.json` registra tamanho real, hash, grupo e grade solicitada. `PROMPTS.md` registra o pedido exato de cada geração. A ferramenta usada foi image_gen integrada.

As imagens são material para preparação e teste posterior, não sprites já recortados, prefabs ou animações prontos para a Unity. Não houve teste de jogo nem compilação, pois esta tarefa foi exclusivamente de arte.
