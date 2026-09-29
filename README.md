# 🚀 DevOps Historia Automation Service

Serviço Windows em C# para automatizar a criação de Product Backlog Items (PBIs) no Azure DevOps a partir de documentos .docx estruturados.

## 📋 Visão Geral

Este projeto substitui a solução anterior em Python + PowerShell por uma alternativa moderna e robusta em C# .NET 6.

**Funcionalidades:**
- ✅ Leitura e parsing de histórias em formato .docx
- ✅ Validação automática de estrutura de documentos
- ✅ Criação de PBIs no Azure DevOps via REST API
- ✅ Movimentação automática de arquivos processados
- ✅ Execução periódica (30 minutos por padrão)
- ✅ Logging estruturado (Serilog)
- ✅ Windows Service integrado
- ✅ Testes unitários inclusos

## 🏗️ Arquitetura

```
DevOpsAutomation.Core
├── Models/              # Modelos de dados
├── Parsers/             # Extração de .docx
├── Clients/             # Cliente Azure DevOps API
├── Services/            # Lógica de negócio
└── Logging/             # Logging estruturado

DevOpsAutomation.Service
├── Windows Service      # Executor principal
├── Program.cs
└── appsettings.json    # Configurações

DevOpsAutomation.Tests
└── Testes unitários
```

## 🛠️ Requisitos

- `.NET 6.0` ou superior
- Windows (para Windows Service)
- Azure DevOps Personal Access Token (PAT)
- Pasta para documentos .docx

## ⚙️ Instalação

### 1. Configurar Variável de Ambiente

```powershell
# PowerShell como Administrador
[Environment]::SetEnvironmentVariable("AZDO_PAT", "seu_token_aqui", "Machine")
```

### 2. Compilar Projeto

```powershell
cd DevOpsAutomation.Service
dotnet build -c Release
```

### 3. Instalar como Windows Service

```powershell
# PowerShell como Administrador
$servicePath = "C:\path\to\DevOpsAutomation.Service\bin\Release\net6.0-windows\DevOpsAutomation.Service.exe"

sc create "DevOpsAutomationService" `
  binPath=$servicePath `
  displayName="DevOps Historia Automation Service" `
  start=auto
```

### 4. Iniciar Serviço

```powershell
Start-Service -Name "DevOpsAutomationService"
```

## 📝 Configuração

Edite `appsettings.json`:

```json
{
  "Azure": {
    "Organizacao": "uon",
    "Projeto": "UonSolutions",
    "PersonalAccessToken": "${AZDO_PAT}"  // Variável de ambiente
  },
  "Paths": {
    "Documentos": "C:\\path\\to\\documentos",
    "Logs": "C:\\path\\to\\logs"
  },
  "Service": {
    "IntervalMs": 1800000,              // 30 minutos
    "DelayEntreArquivosMs": 2000        // 2 segundos entre arquivos
  }
}
```

## 📖 Formato de Documento

Os documentos .docx devem seguir este padrão:

```
[Título Principal] (16pt Bold)

Projeto / Épico Vinculado (14pt Bold)
GIS BR (11pt Normal)

Título (14pt Bold)
... (11pt Normal)

História (14pt Bold)
... (11pt Normal)

Critérios de Aceite (14pt Bold)
Cenário 1 (11pt Bold)
...
Cenário 2 (11pt Bold)
...

Prioridade (14pt Bold)
Média (11pt Normal)
```

Veja `LAYOUT_PADRAO_DOCUMENTOS.md` para documentação completa.

## 🧪 Executar Testes

```powershell
cd DevOpsAutomation.Service
dotnet test
```

## 📊 Monitoramento

### Event Viewer

```powershell
eventvwr.msc
# Aplicativos e Serviços > Windows > Eventos da Aplicação
# Procure por "DevOpsAutomationService"
```

### Log de Arquivo

```
Logs: C:\path\to\logs\service-*.log
```

## 🔍 Troubleshooting

### Serviço não inicia

1. Verificar Event Viewer
2. Verificar permissões na pasta de documentos
3. Testar PAT no Azure DevOps

### Documentos não processados

1. Verificar formato do documento
2. Validar tamanho de fontes (14pt, 16pt, 11pt)
3. Verificar nomes de áreas no mapeamento

### Erro "Area not found"

Verifique `areaMapeamento` em `DocumentParser.cs` e valide contra nomes no Azure DevOps.

## 🛑 Desinstalação

```powershell
# PowerShell como Administrador
Stop-Service -Name "DevOpsAutomationService"
sc delete "DevOpsAutomationService"
```

## 📝 Desenvolvimento

### Estrutura de Pastas

```
src/
├── DevOpsAutomation.Core/
│   ├── Models/
│   ├── Parsers/
│   ├── Clients/
│   ├── Services/
│   └── Logging/
└── DevOpsAutomation.Service/
    ├── DevOpsAutomationService.cs
    ├── Program.cs
    └── appsettings.json

tests/
└── DevOpsAutomation.Tests/
    └── DocumentParserTests.cs
```

### Adicionar Novas Features

1. Criar nova classe em `Core/`
2. Adicionar testes em `Tests/`
3. Integrar ao serviço
4. Atualizar documentação

## 🔐 Segurança

- PAT armazenada em variável de ambiente (nunca em arquivo)
- Autenticação Basic na REST API
- Logging seguro (sem exposição de tokens)
- Validação de entrada de documentos

## 📄 Licença

Proprietary - UON Solutions

## 👥 Contribuição

1. Fazer Fork
2. Criar Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit (`git commit -m 'Add AmazingFeature'`)
4. Push (`git push origin feature/AmazingFeature`)
5. Abrir Pull Request

## 📞 Suporte

- Documentação: `docs/`
- Issues: GitHub Issues
- Email: vitor.ruiz@gmail.com

---

**Versão:** 1.0.0  
**Status:** Production Ready  
**Última Atualização:** 2026-09-29
