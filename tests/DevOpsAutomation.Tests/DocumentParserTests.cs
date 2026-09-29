using System;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using DevOpsAutomation.Core.Parsers;
using DevOpsAutomation.Core.Models;

namespace DevOpsAutomation.Tests
{
    public class DocumentParserTests
    {
        private readonly DocumentParser parser;
        private readonly Mock<ILogger<DocumentParser>> loggerMock;

        public DocumentParserTests()
        {
            loggerMock = new Mock<ILogger<DocumentParser>>();
            parser = new DocumentParser(loggerMock.Object);
        }

        [Fact]
        public void MapearArea_ComAreaValida_DeveRetornarEpicoCorreto()
        {
            // Arrange
            var historia = new HistoriaModel { Area = "Transportes BR" };

            // Act
            parser.MapearArea(historia);

            // Assert
            Assert.Equal(27573, historia.EpicoId);
            Assert.Equal("Transportes BR", historia.Area);
        }

        [Fact]
        public void MapearPrioridade_ComPrioridadeAlta_DeveRetornar1()
        {
            // Arrange
            var historia = new HistoriaModel { PrioridadeTexto = "Alta" };

            // Act
            parser.MapearPrioridade(historia);

            // Assert
            Assert.Equal(1, historia.Prioridade);
        }

        [Fact]
        public void MapearPrioridade_ComPrioridadeMedia_DeveRetornar2()
        {
            // Arrange
            var historia = new HistoriaModel { PrioridadeTexto = "Média" };

            // Act
            parser.MapearPrioridade(historia);

            // Assert
            Assert.Equal(2, historia.Prioridade);
        }

        [Fact]
        public void MapearArea_ComAreaInvalida_DeveThrowException()
        {
            // Arrange
            var historia = new HistoriaModel { Area = "Área Inexistente" };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                parser.MapearArea(historia));
        }
    }
}
