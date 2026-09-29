using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DevOpsAutomation.Core.Models;

namespace DevOpsAutomation.Core.Clients
{
    /// <summary>
    /// Cliente para integração com Azure DevOps REST API
    /// </summary>
    public class DevOpsApiClient : IDisposable
    {
        private readonly string organizacao;
        private readonly string projeto;
        private readonly string pat;
        private readonly HttpClient httpClient;
        private readonly ILogger<DevOpsApiClient> logger;

        public DevOpsApiClient(
            string organizacao,
            string projeto,
            string personalAccessToken,
            ILogger<DevOpsApiClient> logger)
        {
            this.organizacao = organizacao ?? throw new ArgumentNullException(nameof(organizacao));
            this.projeto = projeto ?? throw new ArgumentNullException(nameof(projeto));
            this.pat = personalAccessToken ?? throw new ArgumentNullException(nameof(personalAccessToken));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.httpClient = new HttpClient();
            ConfigurarAutenticacao();
        }

        private void ConfigurarAutenticacao()
        {
            var auth = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($":{pat}"));

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", auth);

            httpClient.DefaultRequestHeaders.Add("User-Agent", "DevOpsAutomationService/1.0");
        }

        /// <summary>Cria um Product Backlog Item no Azure DevOps</summary>
        public async Task<int> CriarPBIAsync(HistoriaModel historia)
        {
            if (historia == null)
                throw new ArgumentNullException(nameof(historia));

            logger.LogInformation($"📤 Criando PBI: {historia.Titulo}");

            try
            {
                var url = $"https://dev.azure.com/{organizacao}/{projeto}/_apis/wit/workitems?api-version=7.1";

                var jsonPatch = new[]
                {
                    new {
                        op = "add",
                        path = "/fields/System.Title",
                        value = historia.Titulo
                    },
                    new {
                        op = "add",
                        path = "/fields/System.Description",
                        value = historia.Descricao
                    },
                    new {
                        op = "add",
                        path = "/fields/Microsoft.VSTS.Common.AcceptanceCriteria",
                        value = historia.CriteriosAceite
                    },
                    new {
                        op = "add",
                        path = "/fields/System.AreaPath",
                        value = $"{projeto}\\{historia.Area}"
                    },
                    new {
                        op = "add",
                        path = "/fields/Microsoft.VSTS.Common.Priority",
                        value = historia.Prioridade
                    }
                };

                var conteudo = new StringContent(
                    JsonSerializer.Serialize(jsonPatch),
                    Encoding.UTF8,
                    "application/json-patch+json");

                logger.LogDebug($"Enviando para: {url}");

                var resposta = await httpClient.PostAsync(url, conteudo);

                if (!resposta.IsSuccessStatusCode)
                {
                    var erroConteudo = await resposta.Content.ReadAsStringAsync();
                    logger.LogError($"❌ Erro HTTP {resposta.StatusCode}: {erroConteudo}");
                    throw new HttpRequestException(
                        $"Falha ao criar PBI. Status: {resposta.StatusCode}");
                }

                var respostaJson = JsonSerializer.Deserialize<JsonElement>(
                    await resposta.Content.ReadAsStringAsync());

                int pbiId = respostaJson.GetProperty("id").GetInt32();

                logger.LogInformation($"✅ PBI #{pbiId} criada com sucesso");

                return pbiId;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"❌ Erro ao criar PBI: {historia.Titulo}");
                throw;
            }
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}
