using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DevOpsAutomation.Core.Models;
using DevOpsAutomation.Core.Parsers;
using DevOpsAutomation.Core.Clients;

namespace DevOpsAutomation.Core.Services
{
    /// <summary>
    /// Serviço que orquestra o processamento de histórias
    /// </summary>
    public class ProcessadorHistorias
    {
        private readonly string pastaDocumentos;
        private readonly string pastaIntegrado;
        private readonly DocumentParser parser;
        private readonly DevOpsApiClient devopsClient;
        private readonly ILogger<ProcessadorHistorias> logger;

        public ProcessadorHistorias(
            string pastaDocumentos,
            DocumentParser parser,
            DevOpsApiClient devopsClient,
            ILogger<ProcessadorHistorias> logger)
        {
            this.pastaDocumentos = pastaDocumentos ??
                throw new ArgumentNullException(nameof(pastaDocumentos));
            this.pastaIntegrado = Path.Combine(pastaDocumentos, "Integrado");
            this.parser = parser ?? throw new ArgumentNullException(nameof(parser));
            this.devopsClient = devopsClient ?? throw new ArgumentNullException(nameof(devopsClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Directory.CreateDirectory(pastaIntegrado);
        }

        /// <summary>Processa todas as histórias pendentes</summary>
        public async Task<ProcessamentoResultado> ProcessarHistoriasAsync()
        {
            var resultado = new ProcessamentoResultado();

            try
            {
                logger.LogInformation("🔍 Iniciando varredura de histórias...");
                logger.LogInformation($"📁 Procurando documentos em: {pastaDocumentos}");
                logger.LogInformation($"📁 Pasta existe: {Directory.Exists(pastaDocumentos)}");

                // Diagnóstico: listar TODOS os arquivos
                try
                {
                    var todosArquivos = Directory.GetFiles(pastaDocumentos, "*");
                    logger.LogInformation($"📄 Arquivos totais na pasta: {todosArquivos.Length}");
                    foreach (var arquivo in todosArquivos.Take(5))
                    {
                        logger.LogInformation($"   Arquivo: {Path.GetFileName(arquivo)} (tipo: {Path.GetExtension(arquivo)})");
                    }
                }
                catch (Exception exDiag)
                {
                    logger.LogError(exDiag, "❌ Erro ao listar arquivos diagnóstico");
                }

                var arquivos = Directory.GetFiles(pastaDocumentos, "*.docx")
                    .Where(f => !f.Contains("\\Integrado\\"))
                    .ToList();

                logger.LogInformation($"📄 Total de arquivos .docx encontrados: {arquivos.Count}");

                if (arquivos.Count > 0)
                {
                    foreach (var arquivo in arquivos)
                    {
                        logger.LogInformation($"   - {Path.GetFileName(arquivo)}");
                    }
                }

                if (!arquivos.Any())
                {
                    logger.LogInformation("✅ Nenhuma história para processar");
                    return resultado;
                }

                logger.LogInformation($"📄 Encontrados {arquivos.Count} documento(s)");

                foreach (var arquivo in arquivos)
                {
                    await ProcessarArquivoAsync(arquivo, resultado);
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }

                logger.LogInformation(
                    $"✅ Processamento concluído: " +
                    $"{resultado.Sucesso} sucesso(s), {resultado.Erros} erro(s)");

                return resultado;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Erro geral no processamento");
                resultado.Erros++;
                resultado.MensagensErro.Add(ex.Message);
                return resultado;
            }
        }

        private async Task ProcessarArquivoAsync(string caminhoArquivo, ProcessamentoResultado resultado)
        {
            var nomeArquivo = Path.GetFileName(caminhoArquivo);

            try
            {
                logger.LogInformation($"📥 Processando: {nomeArquivo}");

                var historia = parser.ExtrairHistoria(caminhoArquivo);

                int pbiId = await devopsClient.CriarPBIAsync(historia);
                historia.PBIId = pbiId;
                historia.Status = ProcessingStatus.EnviadoAoDevOps;

                MoverParaIntegrado(caminhoArquivo, pbiId, historia.Area);

                logger.LogInformation($"✅ {nomeArquivo} → PBI #{pbiId}");
                resultado.Sucesso++;
                resultado.HistoriasProcessadas.Add(historia);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"❌ Erro ao processar {nomeArquivo}");
                resultado.Erros++;
                resultado.MensagensErro.Add($"{nomeArquivo}: {ex.Message}");
            }
        }

        private void MoverParaIntegrado(string caminhoOrigem, int pbiId, string area)
        {
            try
            {
                var nomeAtual = Path.GetFileName(caminhoOrigem);
                var novoNome = $"[{pbiId}]_{area}_{nomeAtual}";
                var caminhoDestino = Path.Combine(pastaIntegrado, novoNome);

                File.Move(caminhoOrigem, caminhoDestino, overwrite: false);

                logger.LogDebug($"📦 Movido para: {novoNome}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "⚠️ Erro ao mover arquivo");
                throw;
            }
        }
    }

    /// <summary>Resultado do processamento em lote</summary>
    public class ProcessamentoResultado
    {
        public int Sucesso { get; set; }
        public int Erros { get; set; }
        public List<HistoriaModel> HistoriasProcessadas { get; set; } = new();
        public List<string> MensagensErro { get; set; } = new();

        public override string ToString() =>
            $"Sucesso: {Sucesso}, Erros: {Erros}, Total: {Sucesso + Erros}";
    }
}
