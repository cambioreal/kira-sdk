# Goal — Kira Provider como serviço para o ecossistema

> Persistido em 2026-07-14 a pedido do usuário. O comando `/goal` rejeitou este conteúdo
> (`Goal condition is limited to 4000 characters (got 9893)`), então o texto integral foi
> salvo aqui como registro, sem que o loop autônomo tenha sido de fato iniciado pelo harness.
>
> **Nenhuma execução autônoma foi disparada a partir deste arquivo.** Ver a resposta do agente
> na sessão de 2026-07-14 para as dúvidas de esclarecimento levantadas antes de qualquer alteração.

## Objetivo final declarado

O objetivo é que o Provider Kira seja disponibilizado como serviço para ser consumido por
todas as plataformas do ecossistema!

## Missão (texto integral fornecido pelo usuário)

======================================================================
MISSÃO
======================================================================

Evoluir continuamente esta solução até que o objetivo definido seja completamente atingido.

A execução deverá ocorrer de forma iterativa, incremental, segura e orientada por evidências,
identificando continuamente oportunidades de evolução, inconsistências, riscos, lacunas e
melhorias.

A cada ciclo, o agente deverá aproximar a implementação do estado desejado até que não existam
pendências relevantes.

A definição de concluído somente será atingida quando todos os critérios de aceite estiverem
atendidos e comprovados.

Nunca assumir que algo está pronto apenas porque existe código.

Tudo deverá ser comprovado.

======================================================================
PRIMEIRA ETAPA (OBRIGATÓRIA)
ESCLARECIMENTO DA INTENÇÃO
======================================================================

ANTES de modificar absolutamente qualquer arquivo, executar obrigatoriamente:

1. Interpretar completamente o objetivo.
2. Levantar todas as dúvidas.
3. Identificar ambiguidades.
4. Identificar conflitos.
5. Identificar possíveis interpretações.
6. Identificar requisitos implícitos.
7. Identificar riscos técnicos.
8. Identificar informações ausentes.
9. Identificar dependências externas.
10. Identificar decisões arquiteturais que precisam do usuário.

NÃO executar nenhuma alteração enquanto existir qualquer ponto de dúvida.

Sempre interromper a execução para esclarecimento caso exista qualquer incerteza.

Nunca assumir.

Nunca inferir requisitos críticos.

Nunca criar comportamento baseado em hipótese.

======================================================================
FONTE DA VERDADE
======================================================================

Toda decisão deverá considerar continuamente: documentação, especificações, arquitetura,
código existente, convenções, histórico, decisões anteriores, ADRs, comentários relevantes,
testes, documentação técnica, documentação funcional, padrões do projeto, melhores práticas
do mercado, estado atual da implementação.

Quando houver conflito entre documentos: identificar, documentar, solicitar decisão.

Nunca escolher arbitrariamente.

======================================================================
MODO DE EXECUÇÃO
======================================================================

Executar continuamente o ciclo:

Descobrir → Analisar → Planejar → Implementar → Refatorar → Validar → Testar → Documentar →
Registrar aprendizados → Commit → Nova descoberta → Repetir

Até não existirem novas oportunidades relevantes.

======================================================================
A CADA CICLO
======================================================================

Executar obrigatoriamente: análise arquitetural, funcional, técnica, estrutural, de qualidade,
de segurança, de performance, de observabilidade, de documentação, de testes, de cobertura, de
código morto, de duplicação, de acoplamento, de coesão, de consistência, de nomenclaturas, de
configurações, de riscos, de dívidas técnicas, de dependências.

======================================================================
IDENTIFICAR AUTOMATICAMENTE
======================================================================

Bugs, inconsistências, funcionalidades incompletas, implementações parciais, TODOs, FIXMEs,
código morto, código duplicado, hardcodes, configurações inválidas, violações arquiteturais,
violações de convenções, violações de segurança, riscos operacionais, baixa cobertura,
documentação desatualizada, divergências entre documentação e implementação, gaps funcionais,
técnicos, arquiteturais, oportunidades de simplificação, otimização, desacoplamento,
automação.

======================================================================
IMPLEMENTAÇÃO
======================================================================

Toda implementação deverá: alterar apenas o necessário. Nunca reescrever sem necessidade.
Sempre reutilizar. Sempre seguir os padrões existentes. Sempre preservar compatibilidade
quando necessário. Sempre reduzir complexidade. Sempre aumentar clareza. Sempre privilegiar
soluções simples.

======================================================================
VALIDAÇÃO
======================================================================

Toda implementação deverá ser validada. Executar sempre que possível: testes unitários, de
integração, funcionais, end-to-end, validações arquiteturais, estáticas, de qualidade, de
segurança, de performance.

Nenhuma implementação poderá ser considerada concluída sem evidência.

======================================================================
DOCUMENTAÇÃO
======================================================================

Toda alteração deverá atualizar automaticamente: documentação técnica, funcional, diagramas,
ADRs, changelog, READMEs, guias, exemplos, comentários relevantes.

Sempre manter documentação sincronizada com a implementação.

======================================================================
COMMITS
======================================================================

Ao concluir um ciclo consistente: gerar commit pequeno, atômico, descritivo, coeso.

Nunca misturar múltiplos objetivos.

======================================================================
APRENDIZADO CONTÍNUO
======================================================================

Ao final de cada ciclo registrar: descobertas, decisões, justificativas, trade-offs, riscos,
limitações, melhorias futuras, novos padrões encontrados, novas convenções identificadas.

Esses aprendizados deverão ser utilizados nas próximas iterações.

======================================================================
AUTO-REAVALIAÇÃO
======================================================================

Após cada ciclo perguntar continuamente: Existe algo melhor? Existe algo mais simples? Existe
alguma inconsistência? Existe algum risco? Existe algum gap? Existe código desnecessário?
Existe documentação divergente? Existe arquitetura melhor? Existe funcionalidade parcialmente
implementada? Existe oportunidade de reduzir complexidade? Existe algo que deveria ser
configurável? Existe algo excessivamente acoplado? Existe oportunidade de generalização?
Existe oportunidade de reutilização?

Se qualquer resposta for SIM: iniciar novo ciclo.

======================================================================
CRITÉRIO DE PARADA
======================================================================

Somente encerrar quando TODOS forem verdadeiros:

✓ objetivo completamente atendido
✓ arquitetura consistente
✓ documentação sincronizada
✓ testes aprovados
✓ sem gaps conhecidos
✓ sem inconsistências conhecidas
✓ sem dúvidas remanescentes
✓ sem pendências técnicas relevantes
✓ sem violações arquiteturais
✓ sem dívida técnica crítica
✓ implementação considerada pronta para produção dentro do escopo definido

======================================================================
RELATÓRIO DE CADA EXECUÇÃO
======================================================================

Ao final de CADA execução gerar automaticamente um arquivo:

`docs/audits/YYYY-MM-DD_HH-mm-ss_EXECUTION_REPORT.md`

contendo obrigatoriamente: Cabeçalho (Data/Hora, Objetivo atual, Versão, Branch, Commit
inicial, Commit final), Resumo Executivo (objetivo, resultado, status, percentual estimado de
evolução), O que foi implementado, O que foi corrigido, O que foi validado, Testes executados,
Problemas encontrados, Pendências restantes (por prioridade), Gaps identificados (funcionais,
arquiteturais, técnicos, documentais, operacionais), Riscos, Decisões tomadas (justificativa),
Aprendizados, Evidências (arquivos alterados, métricas, resultados, cobertura, logs
relevantes), Handoff (contexto atual, onde a execução terminou, decisões importantes,
cuidados, próximos passos, riscos, dependências, recomendações).

======================================================================
CONTINUIDADE
======================================================================

Ao finalizar o relatório gerar automaticamente um novo objetivo para continuidade, que deverá:
considerar tudo que foi aprendido, os gaps remanescentes, novas descobertas, melhorias
identificadas, priorizar maior impacto, servir como ponto de partida da próxima execução.

O próximo objetivo deverá ser registrado ao final do relatório no formato `# NEXT GOAL`.

======================================================================
PRINCÍPIO MÁXIMO
======================================================================

Nunca evoluir baseado em suposições. Sempre evoluir baseado em evidências. Sempre perguntar
quando existir dúvida. Sempre preservar consistência. Sempre deixar a solução melhor do que
foi encontrada.

A cada ciclo a solução deve tornar-se mais completa, mais simples, mais consistente, mais
documentada, mais testada e mais próxima do estado final desejado.
