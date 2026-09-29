using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DevOpsAutomation.Core.Clients;
using DevOpsAutomation.Core.Parsers;
using DevOpsAutomation.Core.Services;
using AppLoggerFactory = DevOpsAutomation.Core.Services.LoggerFactory;

namespace DevOpsAutomation.Service
{
    static class Program
    {
        /// <summary>Processa os documentos pendentes uma vez e encerra (executado pelo Task Scheduler)</summary>
        static async Task<int> Main()
        {
            var logger = AppLoggerFactory.CreateLogger<ProcessadorHistorias>();

            try
            {
                var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                var config = ConfigurationService.CarregarConfiguracao(configPath);

                using var devopsClient = new DevOpsApiClient(
                    config["Azure:Organizacao"].Trim(),
                    config["Azure:Projeto"].Trim(),
                    config["Azure:PersonalAccessToken"].Trim(),
                    AppLoggerFactory.CreateLogger<DevOpsApiClient>());

                var processador = new ProcessadorHistorias(
                    config["Paths:Documentos"],
                    new DocumentParser(AppLoggerFactory.CreateLogger<DocumentParser>()),
                    devopsClient,
                    logger);

                var resultado = await processador.ProcessarHistoriasAsync();
                logger.LogInformation($"📊 Resultado: {resultado}");
                return resultado.Erros == 0 ? 0 : 1;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Erro fatal na execução");
                return 2;
            }
            finally
            {
                Serilog.Log.CloseAndFlush();
            }
        }
    }
}
