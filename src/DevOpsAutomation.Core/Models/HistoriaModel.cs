using System;

namespace DevOpsAutomation.Core.Models
{
    /// <summary>
    /// Modelo de dados para uma História de Usuário do Azure DevOps
    /// </summary>
    public class HistoriaModel
    {
        /// <summary>Identificador único da história</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Título da história (16pt bold)</summary>
        public string Titulo { get; set; } = string.Empty;

        /// <summary>Descrição completa em HTML</summary>
        public string Descricao { get; set; } = string.Empty;

        /// <summary>Critérios de aceite em HTML (cenários BDD)</summary>
        public string CriteriosAceite { get; set; } = string.Empty;

        /// <summary>Área/Projeto (GIS BR, Transportes BR, etc)</summary>
        public string Area { get; set; } = string.Empty;

        /// <summary>ID do Épico vinculado</summary>
        public int EpicoId { get; set; }

        /// <summary>Prioridade numérica (1=Alta, 2=Média, 3=Baixa)</summary>
        public int Prioridade { get; set; } = 2; // Padrão: Média

        /// <summary>Descrição da prioridade</summary>
        public string PrioridadeTexto { get; set; } = "Média";

        /// <summary>Data de criação</summary>
        public DateTime DataCriacao { get; set; } = DateTime.Now;

        /// <summary>Caminho original do arquivo .docx</summary>
        public string CaminhoOrigem { get; set; } = string.Empty;

        /// <summary>ID da PBI criada no Azure DevOps</summary>
        public int? PBIId { get; set; }

        /// <summary>Status de processamento</summary>
        public ProcessingStatus Status { get; set; } = ProcessingStatus.Pendente;

        /// <summary>Mensagem de erro (se houver)</summary>
        public string MensagemErro { get; set; } = string.Empty;

        public override string ToString() =>
            $"[{Status}] {Titulo} ({Area}) - PBI:{PBIId}";
    }

    /// <summary>Status de processamento da história</summary>
    public enum ProcessingStatus
    {
        /// <summary>Aguardando processamento</summary>
        Pendente = 0,

        /// <summary>Validado com sucesso</summary>
        Validado = 1,

        /// <summary>Enviado ao Azure DevOps</summary>
        EnviadoAoDevOps = 2,

        /// <summary>Processamento concluído</summary>
        Completo = 3,

        /// <summary>Erro durante processamento</summary>
        Erro = 4
    }
}
