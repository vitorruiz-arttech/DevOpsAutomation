using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace DevOpsAutomation.Core.Services
{
    /// <summary>Serviço para carregar e gerenciar configurações</summary>
    public static class ConfigurationService
    {
        public static Dictionary<string, string> CarregarConfiguracao(string caminhoArquivo)
        {
            if (!File.Exists(caminhoArquivo))
                throw new FileNotFoundException($"Arquivo de configuração não encontrado: {caminhoArquivo}");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(caminhoArquivo))
                .AddJsonFile(Path.GetFileName(caminhoArquivo), optional: false)
                .AddEnvironmentVariables();

            var config = builder.Build();

            return new Dictionary<string, string>
            {
                { "Azure:Organizacao", config["Azure:Organizacao"] ?? "" },
                { "Azure:Projeto", config["Azure:Projeto"] ?? "" },
                { "Azure:PersonalAccessToken",
                    Environment.GetEnvironmentVariable("AZDO_PAT") ??
                    config["Azure:PersonalAccessToken"] ?? "" },
                { "Paths:Documentos", config["Paths:Documentos"] ?? "" },
                { "Paths:Logs", config["Paths:Logs"] ?? "" },
                { "Service:IntervalMs", config["Service:IntervalMs"] ?? "1800000" },
                { "Service:DelayEntreArquivosMs", config["Service:DelayEntreArquivosMs"] ?? "2000" }
            };
        }
    }
}
