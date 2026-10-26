# SimuladorEmolumentos — MVP V1

Simulador desktop Windows Forms, em português, offline e sem banco.

**Todos os atos e valores são fictícios, sem validade oficial.** Não há salvamento de simulações: os dados da sessão são descartados ao fechar. O único arquivo operacional é um diagnóstico técnico mínimo em `%LOCALAPPDATA%\SimuladorEmolumentos\diagnostico.log`, sem entradas monetárias ou dados pessoais.

## Decisões técnicas

- .NET 10 / C# 14: target atual com suporte a WinForms no Windows; SDK-base 10.0.100 em `global.json`, aceitando patches da mesma faixa. A versão efetivamente instalada deve ser registrada com `dotnet --info` na homologação.
- xUnit 2.9.3, runner Visual Studio 3.1.1 e Microsoft.NET.Test.Sdk 17.14.1, fixados nos projetos de testes.
- Domain contém contratos, catálogo em memória e regras. Presentation contém presenter e interface de view e pode ser testado fora do Windows. WinForms contém a view e a composição. Não há fórmulas no form.
- Infrastructure será introduzido quando houver persistência concreta. Banco e biblioteca de acesso serão escolhidos antes da V3.

## Regras e exemplos independentes

| UF | Ato | Identificador | Fórmula fictícia | Exemplo esperado |
| --- | --- | --- | --- | --- |
| SP | Certidão demonstrativa | DEMO-SP-CERT-1 | Fixo R$ 35,00 | Sem base → R$ 35,00 |
| RJ | Certidão demonstrativa | DEMO-RJ-CERT-1 | Fixo R$ 42,50 | Sem base → R$ 42,50 |
| SP | Escritura demonstrativa | DEMO-SP-ESCR-1 | Base × 1% | 10.000 × 0,01 = R$ 100,00 |
| RJ | Escritura demonstrativa | DEMO-RJ-ESCR-1 | Base × 1,2% | 10.000 × 0,012 = R$ 120,00 |

Cada regra produz um item. Arredondar cada item para duas casas com `MidpointRounding.AwayFromZero` e somar os itens arredondados. Exemplo de meio centavo: SP, base 100,50 × 0,01 = 1,005 → **R$ 1,01**; RJ, 100,50 × 0,012 = 1,206 → **R$ 1,21**. Esses exemplos são contas de referência, não tarifas oficiais.

As regras fixas não recebem parâmetros adicionais. As proporcionais exigem base maior que zero e até R$ 1.000.000.000,00. Para a entrada, use vírgula decimal, sem separadores de milhar nem símbolo monetário: `10000,00`. Não há quantidade na V1. UFs e atos ausentes no catálogo são rejeitados sem regra alternativa.

O resultado apresenta UF, ato, identificação da regra, fórmula, entradas, itens e total em pt-BR. Qualquer mudança de entrada limpa o resultado anterior. Ações disponíveis: calcular, limpar e copiar a memória completa; o texto do resultado também pode ser selecionado e copiado com Ctrl+C. Limpar retorna à primeira UF e primeiro ato.

## Preparar e executar no Windows

Pré-requisitos: Windows 10/11 com suporte ao .NET 10, SDK .NET 10 da faixa 10.0.1xx e acesso ao NuGet para restaurar dependências. Visual Studio é opcional; se usado, precisa oferecer suporte a .NET 10 e desenvolvimento desktop .NET.

Os comandos abaixo são o procedimento preparado para validação. **Não foram executados com sucesso neste ambiente**, que não tem SDK nem resolução de rede. Execute na raiz do projeto:

```powershell
dotnet --info
dotnet restore SimuladorEmolumentos.sln
dotnet build SimuladorEmolumentos.sln --configuration Release --no-restore
dotnet test tests/SimuladorEmolumentos.Domain.Tests/SimuladorEmolumentos.Domain.Tests.csproj --configuration Release --no-restore
dotnet test tests/SimuladorEmolumentos.Presentation.Tests/SimuladorEmolumentos.Presentation.Tests.csproj --configuration Release --no-restore
dotnet run --project src/SimuladorEmolumentos.WinForms/SimuladorEmolumentos.WinForms.csproj --configuration Release --no-build
```

Os dois projetos de testes usam `net10.0` e não dependem de WinForms ou banco. Para compilar a solução no Linux há `EnableWindowsTargeting`, mas a interface só executa no Windows e a restauração exige os pacotes de referência do Windows Desktop.

## Roteiro de demonstração e aceite pendente

1. Abrir a aplicação e verificar os avisos de dados fictícios e ausência de salvamento.
2. Simular certidão nas duas UFs e conferir R$ 35,00 e R$ 42,50.
3. Simular escritura nas duas UFs com `10000,00` e conferir R$ 100,00 e R$ 120,00. Conferir memória e copiar o resultado.
4. Usar `100,50` em SP e conferir R$ 1,01.
5. Alterar a base ou a UF e verificar que o total anterior desaparece.
6. Tentar base vazia, `abc`, `-1`, `0`, `10.00` e `1000000000,01`: deve exibir mensagem e nenhum total válido.
7. Limpar, navegar por Tab e atalhos, calcular novamente. Fechar e reabrir: não deve recuperar a simulação anterior.
8. Registrar Windows, SDK, saída de build/testes e capturas reais da tela inicial, resultado e erro em `docs/evidencias/`.

## Estado da validação

Implementação e testes escritos; revisão estática dos arquivos realizada no Linux em 26/10/2026. `dotnet --info` retornou `command not found`; a tentativa de obter o instalador falhou por resolução de DNS (`dot.net`). Portanto **build, execução dos testes, fluxo manual no Windows e capturas de tela permanecem pendentes**. Não há evidência visual simulada apresentada como captura real, nem aprovação final dos critérios de aceite.

O projeto está organizado em commits por funcionalidade na branch `main`. Após validar a entrega, identificar a V1 com tag/release e anexar este roteiro e as evidências.
