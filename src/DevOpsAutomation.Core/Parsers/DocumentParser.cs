using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using DevOpsAutomation.Core.Models;

namespace DevOpsAutomation.Core.Parsers
{
    public class DocumentParser
    {
        private readonly ILogger<DocumentParser> logger;

        // Cabeçalhos definidos em INSTRUCOES_PREENCHIMENTO.md / PROMPT_PARA_LLA.md (comparados sem acentos)
        private static readonly (string Chave, string Cabecalho)[] Secoes =
        {
            ("titulo", "titulo"),
            ("historia", "historia"),
            ("objetivo", "objetivo da funcionalidade"),
            ("projeto", "projeto / epico vinculado"),
            ("regras", "regras de negocio"),
            ("criterios", "criterios de aceite"),
            ("fluxo", "fluxo resumido"),
            ("requisitos", "requisitos tecnicos"),
            ("prioridade", "prioridade"),
            ("resultado", "resultado esperado")
        };

        // AreaPath = nó de área existente em UonSolutions; null = projeto sem área correspondente no Azure DevOps
        private readonly Dictionary<string, (int EpicoId, string AreaPath)> areaMapeamento = new()
        {
            { "gis br", (26044, "GISBR") },
            { "gisbr", (26044, "GISBR") },
            { "hdi br", (27961, "HDI BR") },
            { "transportes br", (27573, "Transportes BR") },
            { "transporte br", (27573, "Transportes BR") },
            { "sinistros br", (26046, "Sinistros BR") },
            // Nome da área no Azure DevOps está grafado "Avalições BR"; ajustar aqui se for renomeada
            { "avaliacoes br", (26045, "Avalições BR") },
            { "aval br", (26045, "Avalições BR") },
            { "avaliacoes pt", (27038, null) },
            { "aval pt", (27038, null) },
            { "gis pt", (26019, null) },
            { "sinistros pt", (26098, null) }
        };

        private readonly Dictionary<string, int> prioridadeMap = new()
        {
            { "alta", 1 },
            { "media", 2 },
            { "baixa", 3 }
        };

        public DocumentParser(ILogger<DocumentParser> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public HistoriaModel ExtrairHistoria(string caminhoDocumento)
        {
            logger.LogInformation($"[*] Iniciando parsing: {caminhoDocumento}");

            try
            {
                using (WordprocessingDocument doc = WordprocessingDocument.Open(caminhoDocumento, false))
                {
                    var paragrafos = doc.MainDocumentPart.Document
                        .Descendants<Paragraph>()
                        .ToList();

                    var secoes = MapearSecoes(paragrafos);

                    var historia = new HistoriaModel
                    {
                        CaminhoOrigem = caminhoDocumento,
                        Titulo = ExtrairTitulo(paragrafos),
                        Area = ExtrairArea(secoes),
                        Descricao = SecaoHtml(secoes, "titulo"),
                        Historia = SecaoHtml(secoes, "historia"),
                        Objetivo = SecaoHtml(secoes, "objetivo"),
                        RegrasNegocio = SecaoHtml(secoes, "regras"),
                        CriteriosAceite = SecaoHtml(secoes, "criterios"),
                        FluxoResumido = SecaoHtml(secoes, "fluxo"),
                        RequisitosTecnicos = SecaoHtml(secoes, "requisitos"),
                        PrioridadeTexto = PrimeiroTexto(secoes, "prioridade") ?? "Media",
                        ResultadoEsperado = SecaoHtml(secoes, "resultado"),
                        Status = ProcessingStatus.Pendente
                    };

                    MapearArea(historia);
                    MapearPrioridade(historia);
                    ValidarHistoria(historia);

                    logger.LogInformation($"[OK] Parsing concluido: {historia.Titulo}");
                    return historia;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[ERRO] Erro ao fazer parsing de {caminhoDocumento}");
                throw;
            }
        }

        private string ExtrairTitulo(List<Paragraph> paragrafos)
        {
            string titulo = null;

            logger.LogInformation($"[*] Analisando {paragrafos.Count} paragrafos...");

            for (int i = 0; i < Math.Min(5, paragrafos.Count); i++)
            {
                var texto = paragrafos[i].InnerText?.Trim() ?? "[vazio]";
                var tamanho = TamanhoFonte(paragrafos[i]);
                var bold = EhBold(paragrafos[i]) ? "BOLD" : "norm";
                logger.LogInformation($"  P{i}: [{bold}, {tamanho}pt] {(texto.Length > 50 ? texto.Substring(0, 50) + "..." : texto)}");
            }

            // 1. Tentar 16pt bold
            titulo = paragrafos
                .FirstOrDefault(p => TamanhoFonte(p) == 16 && EhBold(p))
                ?.InnerText?.Trim();

            if (!string.IsNullOrWhiteSpace(titulo))
            {
                logger.LogInformation($"[OK] Titulo encontrado (16pt bold)");
                return titulo;
            }

            // 2. Tentar 16pt simples (para Spike)
            titulo = paragrafos
                .FirstOrDefault(p => TamanhoFonte(p) == 16 && !string.IsNullOrWhiteSpace(p.InnerText))
                ?.InnerText?.Trim();

            if (!string.IsNullOrWhiteSpace(titulo))
            {
                logger.LogInformation($"[OK] Titulo encontrado (16pt simples)");
                return titulo;
            }

            // 3. Tentar qualquer bold
            titulo = paragrafos
                .FirstOrDefault(p => EhBold(p) && !string.IsNullOrWhiteSpace(p.InnerText))
                ?.InnerText?.Trim();

            if (!string.IsNullOrWhiteSpace(titulo))
            {
                logger.LogInformation($"[OK] Titulo encontrado (bold)");
                return titulo;
            }

            // 4. Tentar primeiro com conteudo
            titulo = paragrafos
                .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.InnerText))
                ?.InnerText?.Trim();

            if (!string.IsNullOrWhiteSpace(titulo))
            {
                logger.LogInformation($"[OK] Titulo encontrado (primeiro paragrafo)");
                return titulo;
            }

            logger.LogError("[ERRO] Nenhum paragrafo com conteudo!");
            throw new InvalidOperationException("Titulo nao encontrado");
        }

        /// <summary>Agrupa os parágrafos de conteúdo sob o cabeçalho de seção mais recente.</summary>
        private Dictionary<string, List<string>> MapearSecoes(List<Paragraph> paragrafos)
        {
            var secoes = new Dictionary<string, List<string>>();
            List<string> atual = null;
            TableRow linhaCabecalho = null;

            foreach (var para in paragrafos)
            {
                var texto = para.InnerText?.Trim() ?? "";
                if (texto.Length == 0)
                    continue;

                var linhaTabela = para.Ancestors<TableRow>().FirstOrDefault();

                var (chave, valorInline) = IdentificarCabecalho(para, texto);
                if (chave != null)
                {
                    // Seção repetida no documento: mantém a primeira ocorrência
                    atual = secoes.ContainsKey(chave) ? null : secoes[chave] = new List<string>();
                    linhaCabecalho = linhaTabela;
                    if (atual != null && !string.IsNullOrWhiteSpace(valorInline))
                        atual.Add(valorInline);
                    continue;
                }

                // Cabeçalho em célula de tabela ("Projeto / Épico Vinculado | valor"): o valor é só o restante da linha
                if (linhaCabecalho != null && linhaTabela != linhaCabecalho)
                {
                    atual = null;
                    linhaCabecalho = null;
                }

                if (atual == null)
                    continue;

                atual.Add(para.ParagraphProperties?.NumberingProperties != null ? $"• {texto}" : texto);
            }

            logger.LogInformation($"[*] Secoes encontradas: {string.Join(", ", secoes.Select(s => $"{s.Key}({s.Value.Count})"))}");
            return secoes;
        }

        private (string Chave, string ValorInline) IdentificarCabecalho(Paragraph para, string texto)
        {
            var tamanho = TamanhoFonte(para);
            if (tamanho >= 16)
                return (null, null);

            var normalizado = Normalizar(texto);
            var partes = normalizado.Split(new[] { ':' }, 2);
            var nome = partes[0].Trim();

            foreach (var (chave, cabecalho) in Secoes)
            {
                if (nome != cabecalho)
                    continue;

                // "Projeto / Épico Vinculado: Transportes BR" — valor na mesma linha
                string valorInline = null;
                if (partes.Length == 2)
                {
                    var idx = texto.IndexOf(':');
                    valorInline = texto.Substring(idx + 1).Trim();
                }

                return (chave, valorInline);
            }

            return (null, null);
        }

        private string SecaoHtml(Dictionary<string, List<string>> secoes, string chave)
        {
            if (!secoes.TryGetValue(chave, out var linhas) || linhas.Count == 0)
                return string.Empty;

            var html = new StringBuilder();
            foreach (var linha in linhas)
            {
                var encoded = WebUtility.HtmlEncode(linha);
                var normalizado = Normalizar(linha);

                if (normalizado.StartsWith("cenario") || Regex.IsMatch(linha, @"^RN\d+\s*[—–-]"))
                    html.AppendLine($"<p><b>{encoded}</b></p>");
                else
                    html.AppendLine($"<p>{encoded}</p>");
            }

            return html.ToString();
        }

        private static string PrimeiroTexto(Dictionary<string, List<string>> secoes, string chave) =>
            secoes.TryGetValue(chave, out var linhas) ? linhas.FirstOrDefault() : null;

        private string ExtrairArea(Dictionary<string, List<string>> secoes)
        {
            var valor = PrimeiroTexto(secoes, "projeto");
            if (string.IsNullOrWhiteSpace(valor))
            {
                logger.LogError("[ERRO] Secao 'Projeto / Épico Vinculado' nao encontrada");
                throw new InvalidOperationException("Area nao encontrada");
            }

            // "27573 - Transportes BR" → "Transportes BR"
            var area = Regex.Replace(valor, @"^\d+\s*[—–-]\s*", "").Trim();
            logger.LogInformation($"[OK] Area encontrada: {area}");
            return area;
        }

        private void ValidarHistoria(HistoriaModel historia)
        {
            var erros = new List<string>();

            if (string.IsNullOrWhiteSpace(historia.Titulo))
                erros.Add("Titulo obrigatorio");

            if (string.IsNullOrWhiteSpace(historia.Area))
                erros.Add("Area obrigatoria");

            if (string.IsNullOrWhiteSpace(historia.CriteriosAceite))
                erros.Add("Critérios de Aceite obrigatórios (secao 'Critérios de Aceite' nao encontrada ou vazia)");

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

            if (!areaMapeamento.TryGetValue(chave, out var mapeamento))
                throw new InvalidOperationException($"Area '{historia.Area}' nao mapeada");

            if (mapeamento.AreaPath == null)
                throw new InvalidOperationException(
                    $"Projeto '{historia.Area}' ainda nao possui area correspondente no Azure DevOps (Epic #{mapeamento.EpicoId}). " +
                    "Cadastre a area em DocumentParser.areaMapeamento.");

            historia.EpicoId = mapeamento.EpicoId;
            historia.Area = mapeamento.AreaPath;
        }

        public void MapearPrioridade(HistoriaModel historia)
        {
            // "Alta — A equipe..." → "alta"
            var chave = Normalizar(historia.PrioridadeTexto)
                .Split(new[] { ' ', '-', '—', '–', ':', '.', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? "";

            if (prioridadeMap.TryGetValue(chave, out var prioridade))
            {
                historia.Prioridade = prioridade;
            }
            else
            {
                logger.LogWarning($"Prioridade '{historia.PrioridadeTexto}' nao reconhecida");
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
            // <w:b/> sem atributo val significa negrito ligado
            return paragrafo.Descendants<Run>()
                .Any(r => r.RunProperties?.Bold is Bold b && (b.Val == null || b.Val.Value));
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
