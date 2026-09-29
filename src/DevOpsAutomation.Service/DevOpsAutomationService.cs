using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Timers;
using System.IO;
using Microsoft.Extensions.Logging;
using DevOpsAutomation.Core.Services;
using DevOpsAutomation.Core.Parsers;
using DevOpsAutomation.Core.Clients;

namespace DevOpsAutomation.Service
{
    /// <summary>
    /// Serviço Windows que executa a automação em intervalos regulares
    /// </summary>
    public partial class DevOpsAutomationService : ServiceBase
    {
        private Timer timer;
        private ProcessadorHistorias processador;
        private DevOpsApiClient devopsClient;
        private DocumentParser parser;
        private ILogger<DevOpsAutomationService> logger;
        private readonly string configPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        public DevOpsAutomationService()
        {
            ServiceName = "DevOpsAutomationService";
            CanStop = true;
            CanPauseAndContinue = true;
            CanShutdown = true;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                EventLog.WriteEntry(ServiceName,
                    $"🚀 Serviço iniciando em {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    EventLogEntryType.Information);

                logger = DevOpsAutomation.Core.Services.LoggerFactory.CreateLogger<DevOpsAutomationService>();

                var config = ConfigurationService.CarregarConfiguracao(configPath);
                logger.LogInformation($"⚙️ Configuração carregada");

                parser = new DocumentParser(
                    DevOpsAutomation.Core.Services.LoggerFactory.CreateLogger<DocumentParser>());

                devopsClient = new DevOpsApiClient(
                    config["Azure:Organizacao"],
                    config["Azure:Projeto"],
                    config["Azure:PersonalAccessToken"],
                    DevOpsAutomation.Core.Services.LoggerFactory.CreateLogger<DevOpsApiClient>());

                processador = new ProcessadorHistorias(
                    config["Paths:Documentos"],
                    parser,
                    devopsClient,
                    DevOpsAutomation.Core.Services.LoggerFactory.CreateLogger<ProcessadorHistorias>());

                int intervaloMs = int.Parse(config["Service:IntervalMs"] ?? "1800000");
                timer = new Timer(intervaloMs);
                timer.Elapsed += OnTimerElapsed;
                timer.AutoReset = true;
                timer.Start();

                logger.LogInformation(
                    $"✅ Serviço iniciado. Intervalo: {intervaloMs / 1000 / 60} minutos");

                EventLog.WriteEntry(ServiceName,
                    "✅ Serviço iniciado com sucesso",
                    EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(ServiceName,
                    $"❌ Erro ao iniciar: {ex.Message}",
                    EventLogEntryType.Error);
                throw;
            }
        }

        protected override void OnStop()
        {
            try
            {
                logger?.LogInformation("🛑 Serviço parando...");

                timer?.Stop();
                timer?.Dispose();
                devopsClient?.Dispose();

                EventLog.WriteEntry(ServiceName,
                    "✅ Serviço parado com sucesso",
                    EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(ServiceName,
                    $"❌ Erro ao parar: {ex.Message}",
                    EventLogEntryType.Error);
                throw;
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();

            try
            {
                logger.LogInformation($"⏰ Executando processamento em {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                var resultado = processador.ProcessarHistoriasAsync().Result;

                logger.LogInformation($"📊 Resultado: {resultado}");

                EventLog.WriteEntry(ServiceName,
                    $"✅ Ciclo concluído: {resultado}",
                    EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Erro no processamento");

                EventLog.WriteEntry(ServiceName,
                    $"❌ Erro: {ex.Message}",
                    EventLogEntryType.Error);
            }
            finally
            {
                timer.Start();
            }
        }

        protected override void OnShutdown()
        {
            logger?.LogInformation("🔌 Sistema desligando");
            OnStop();
        }

        protected override void OnPause()
        {
            logger?.LogInformation("⏸️ Serviço pausado");
            timer?.Stop();
        }

        protected override void OnContinue()
        {
            logger?.LogInformation("▶️ Serviço retomado");
            timer?.Start();
        }
    }
}
