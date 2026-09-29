using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using DevOpsAutomation.Core.Models;

namespace DevOpsAutomation.Core.Parsers
{
    /// <summary>
    /// Parser para extrair dados de documentos .docx
    /// </summary>
    public class DocumentParser
    {
        private readonly ILogger<DocumentParser> logger;

        private readonly Dictionary<string, (int EpicoId, string NomeCanonico)> areaMapeamento = new()
        {
            { "gis br", (26044, "GISBR") },
            { "gisbr", (26044, "GISBR") },
            { "hdi br", (27961, "HDI BR") },
            { "avaliacoes br", (26045, "Avaliacoes BR") },
            { "aval br", (26045, "Avaliacoes BR") },
            { "transportes br", (27573, "Transportes BR") },
            { "transporte br", (27573, "Transportes BR") },
            { "sinistros br", (26046, "Sinistros BR") },
            { "avaliacoes pt", (27038, "Avaliacoes PT") },
            { "aval pt", (27038, "Avaliacoes PT") },
            { "gis pt", (26019, "GIS PT") },
            { "sinistros pt", (26098, "Sinistros PT") }
        };

        private readonly Dictionary<string, int> prioridadeMap = new()
        {
            { "alta", 1 },
            { "média", 2 },
            { "media", 2 },
            { "baixa", 3 }
        };

        public DocumentParser(ILogger<DocumentParser> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Extrai todos os dados de uma história a partir do .docx</summary>
        public HistoriaModel ExtrairHistoria(string caminhoDocumento)
        {
            logger.LogInformation($"📄 Iniciando parsing: {caminhoDocumento}");

            try
            {
                using (WordprocessingDocument doc =
                       WordprocessingDocument.Open(caminhoDocumento, false))
                {
                    var paragrafos = doc.MainDocumentPart.Document
                        .Descendants<Paragraph>()
                        .ToList();

                    var historia = new HistoriaModel
                    {
                        CaminhoOrigem = caminhoDocumento,
                        Titulo = ExtrairTitulo(paragrafos),
                        Area = ExtrairArea(paragrafos),
                        Descricao = ExtrairDescricao(paragrafos),
                        CriteriosAceite = ExtrairCriteriosAceite(paragrafos),
                        PrioridadeTexto = ExtrairPrioridadeTexto(paragrafos),
                        Status = ProcessingStatus.Pendente
                    };

                    MapearArea(historia);
                    MapearPrioridade(historia);
                    ValidarHistoria(historia);

                    logger.LogInformation($"✅ Parsing concluído: {historia.Titulo}");
                    return historia;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"❌ Erro ao fazer parsing de {caminhoDocumento}");
                throw;
            }
        }

        private string ExtrairTitulo(List<Paragraph> paragrafos)
        {
            var titulo = paragrafos
                .FirstOrDefault(p => TamanhoFonte(p) == 16 && EhBold(p))
                ?.InnerText;

            if (string.IsNullOrWhiteSpace(titulo))
                throw new InvalidOperationException("Título não encontrado (16pt bold)");

            return titulo.Trim();
        }

        private string ExtrairArea(List<Paragraph> paragrafos)
        {
            for (int i = 0; i < paragrafos.Count; i++)
            {
                var paragrafo = paragrafos[i];

                if (paragrafo.InnerText.Contains("Projeto / Épico Vinculado") &&
                    TamanhoFonte(paragrafo) == 14 && EhBold(paragrafo))
                {
                    if (i + 1 < paragrafos.Count)
                    {
                        var prox = paragrafos[i + 1];
                        if (TamanhoFonte(prox) == 11 && !EhBold(prox))
                        {
                            return prox.InnerText.Trim();
                        }
                    }
                }
            }

            throw new InvalidOperationException("Área não encontrada");
        }

        private string ExtrairDescricao(List<Paragraph> paragrafos)
        {
            var html = new StringBuilder();

            for (int i = 0; i < paragrafos.Count; i++)
            {
                var para = paragrafos[i];
                var texto = para.InnerText.Trim();

                if (texto.Contains("Critérios de Aceite") && TamanhoFonte(para) == 14)
                    break;

                if (TamanhoFonte(para) == 16 ||
                    texto.Contains("Projeto / Épico Vinculado"))
                    continue;

                if (string.IsNullOrWhiteSpace(texto))
                    continue;

                if (TamanhoFonte(para) == 14 && EhBold(para))
                {
                    html.AppendLine($"<h2>{System.Net.WebUtility.HtmlEncode(texto)}</h2>");
                }
                else if (TamanhoFonte(para) == 11)
                {
                    html.AppendLine($"<p>{System.Net.WebUtility.HtmlEncode(texto)}</p>");
                }
            }

            if (html.Length == 0)
                throw new InvalidOperationException("Descrição vazia");

            return html.ToString();
        }

        private string ExtrairCriteriosAceite(List<Paragraph> paragrafos)
        {
            var html = new StringBuilder();
            var emSecaoCriterios = false;

            for (int i = 0; i < paragrafos.Count; i++)
            {
                var para = paragrafos[i];
                var texto = para.InnerText.Trim();

                if (texto.Contains("Critérios de Aceite") && TamanhoFonte(para) == 14)
                {
                    emSecaoCriterios = true;
                    html.AppendLine($"<h2>{System.Net.WebUtility.HtmlEncode(texto)}</h2>");
                    continue;
                }

                if (!emSecaoCriterios)
                    continue;

                if (TamanhoFonte(para) == 14 && EhBold(para) &&
                    !texto.StartsWith("Cenário"))
                    break;

                if (string.IsNullOrWhiteSpace(texto))
                    continue;

                if ((TamanhoFonte(para) == 11 || TamanhoFonte(para) == 12) &&
                    EhBold(para) && texto.StartsWith("Cenário"))
                {
                    html.AppendLine($"<h4>{System.Net.WebUtility.HtmlEncode(texto)}</h4>");
                }
                else if (TamanhoFonte(para) == 11 && !EhBold(para))
                {
                    html.AppendLine($"<p>{System.Net.WebUtility.HtmlEncode(texto)}</p>");
                }
            }

            return html.Length > 0 ? html.ToString() : "<h2>Critérios de Aceite</h2>";
        }

        private string ExtrairPrioridadeTexto(List<Paragraph> paragrafos)
        {
            for (int i = 0; i < paragrafos.Count; i++)
            {
                var para = paragrafos[i];

                if (para.InnerText.Contains("Prioridade") &&
                    TamanhoFonte(para) == 14 && EhBold(para))
                {
                    if (i + 1 < paragrafos.Count)
                    {
                        return paragrafos[i + 1].InnerText.Trim();
                    }
                }
            }

            return "Média";
        }

        private void ValidarHistoria(HistoriaModel historia)
        {
            var erros = new List<string>();

            if (string.IsNullOrWhiteSpace(historia.Titulo))
                erros.Add("Título obrigatório");

            if (string.IsNullOrWhiteSpace(historia.Descricao))
                erros.Add("Descrição obrigatória");

            if (string.IsNullOrWhiteSpace(historia.Area))
                erros.Add("Área obrigatória");

            if (string.IsNullOrWhiteSpace(historia.CriteriosAceite))
                erros.Add("Critérios de Aceite obrigatórios");

            if (erros.Any())
            {
                historia.Status = ProcessingStatus.Erro;
                historia.MensagemErro = string.Join("; ", erros);
                throw new InvalidOperationException(historia.MensagemErro);
            }

            historia.Status = ProcessingStatus.Validado;
        }

        public void MapearArea(HistoriaModel historia)
        {
            var chave = Normalizar(historia.Area);

            if (areaMapeamento.TryGetValue(chave, out var mapeamento))
            {
                historia.EpicoId = mapeamento.EpicoId;
                historia.Area = mapeamento.NomeCanonico;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Área '{historia.Area}' não mapeada");
            }
        }

        public void MapearPrioridade(HistoriaModel historia)
        {
            var chave = Normalizar(historia.PrioridadeTexto.Split('—')[0]);

            if (prioridadeMap.TryGetValue(chave, out var prioridade))
            {
                historia.Prioridade = prioridade;
            }
            else
            {
                logger.LogWarning($"Prioridade '{historia.PrioridadeTexto}' não reconhecida");
                historia.Prioridade = 2;
            }
        }

        private int TamanhoFonte(Paragraph paragrafo)
        {
            try
            {
                var run = paragrafo.Descendants<Run>().FirstOrDefault();
                if (run == null) return 0;

                var sizeStr = run.RunProperties?.FontSize?.Val?.ToString();
                if (string.IsNullOrEmpty(sizeStr) || !int.TryParse(sizeStr, out int sizeValue))
                    return 0;

                return sizeValue / 2;
            }
            catch
            {
                return 0;
            }
        }

        private bool EhBold(Paragraph paragrafo)
        {
            try
            {
                return paragrafo.Descendants<Run>()
                    .Any(r => r.RunProperties?.Bold?.Val ?? false);
            }
            catch
            {
                return false;
            }
        }

        private string Normalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return "";

            var nfd = texto.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in nfd)
            {
                var unicat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicat != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().ToLowerInvariant().Trim();
        }
    }
}
