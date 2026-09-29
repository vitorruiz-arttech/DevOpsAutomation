using System;
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
            if (personalAccessToken == null) throw new ArgumentNullException(nameof(personalAccessToken));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            httpClient = new HttpClient();
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($":{personalAccessToken}"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>Cria um Product Backlog Item no Azure DevOps</summary>
        public async Task<int> CriarPBIAsync(HistoriaModel historia)
        {
            if (historia == null)
                throw new ArgumentNullException(nameof(historia));

            logger.LogInformation($"📤 Criando PBI: {historia.Titulo}");

            var url = $"https://dev.azure.com/{Uri.EscapeDataString(organizacao)}/{Uri.EscapeDataString(projeto)}" +
                      "/_apis/wit/workitems/$Product%20Backlog%20Item?api-version=7.1";

            var patch = new object[]
            {
                new { op = "add", path = "/fields/System.Title", value = (object)historia.Titulo },
                new { op = "add", path = "/fields/System.Description", value = (object)ConstruirDescricaoCompleta(historia) },
                new { op = "add", path = "/fields/Microsoft.VSTS.Common.AcceptanceCriteria", value = (object)historia.CriteriosAceite },
                new { op = "add", path = "/fields/System.AreaPath", value = (object)$"{projeto}\\{historia.Area}" },
                new { op = "add", path = "/fields/Microsoft.VSTS.Common.Priority", value = (object)historia.Prioridade },
                new
                {
                    op = "add",
                    path = "/relations/-",
                    value = (object)new
                    {
                        rel = "System.LinkTypes.Hierarchy-Reverse",
                        url = $"https://dev.azure.com/{Uri.EscapeDataString(organizacao)}/_apis/wit/workItems/{historia.EpicoId}"
                    }
                }
            };

            using var conteudo = new StringContent(JsonSerializer.Serialize(patch), Encoding.UTF8);
            conteudo.Headers.ContentType = new MediaTypeHeaderValue("application/json-patch+json");

            using var resposta = await httpClient.PostAsync(url, conteudo);
            var corpo = await resposta.Content.ReadAsStringAsync();

            if (!resposta.IsSuccessStatusCode)
            {
                logger.LogError($"❌ HTTP {(int)resposta.StatusCode} ao criar PBI: {corpo}");
                throw new HttpRequestException($"Falha ao criar PBI. Status: {(int)resposta.StatusCode} {resposta.StatusCode}");
            }

            int pbiId = JsonDocument.Parse(corpo).RootElement.GetProperty("id").GetInt32();
            logger.LogInformation($"✅ PBI #{pbiId} criada com sucesso (parent: Epic #{historia.EpicoId})");
            return pbiId;
        }

        private string ConstruirDescricaoCompleta(HistoriaModel historia)
        {
            var secoes = new (string Titulo, string Html)[]
            {
                ("📝 Descrição", historia.Descricao),
                ("📖 História", historia.Historia),
                ("🎯 Objetivo da Funcionalidade", historia.Objetivo),
                ("📋 Regras de Negócio", historia.RegrasNegocio),
                ("🔄 Fluxo Resumido", historia.FluxoResumido),
                ("⚙️ Requisitos Técnicos", historia.RequisitosTecnicos),
                ("✅ Resultado Esperado", historia.ResultadoEsperado)
            };

            var sb = new StringBuilder();
            sb.AppendLine("<div style='font-family: Segoe UI, sans-serif; color: #333;'>");

            foreach (var (titulo, html) in secoes)
            {
                if (string.IsNullOrWhiteSpace(html))
                    continue;

                sb.AppendLine($"<h3>{titulo}</h3>");
                sb.AppendLine(html);
            }

            sb.AppendLine("</div>");
            return sb.ToString();
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }
    }
}
