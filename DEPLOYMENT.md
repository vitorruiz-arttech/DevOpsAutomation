# 🚀 Guia de Deployment - DevOps Historia Automation Service

Instruções passo a passo para compilar, instalar e executar o serviço.

## 📋 Checklist de Deployment

- [ ] .NET 6.0 SDK instalado
- [ ] Variável de ambiente `AZDO_PAT` configurada
- [ ] Pasta de documentos criada e permissões configuradas
- [ ] Projeto compilado com sucesso
- [ ] Testes passando
- [ ] Serviço instalado no Windows
- [ ] Serviço iniciado e rodando
- [ ] Logs sendo gerados corretamente

## 🛠️ Fase 1: Preparação

### 1.1 Verificar .NET SDK

```powershell
dotnet --version
# Deve ser 6.0 ou superior
```

Se não tiver, baixe em: https://dotnet.microsoft.com/en-us/download/dotnet/6.0

### 1.2 Configurar PAT (Personal Access Token)

**No Azure DevOps:**
1. Vá para https://dev.azure.com/uon
2. Clique em avatar → Personal access tokens
3. New Token
4. **Name:** DevOpsAutomation
5. **Organization:** All accessible organizations
6. **Expiration:** 1 year (mínimo recomendado)
7. **Scopes:** Work Item (Read & Write)
8. Clique em Create
9. **COPIE** o token (aparece uma vez!)

**No Windows (PowerShell Admin):**
```powershell
[Environment]::SetEnvironmentVariable("AZDO_PAT", "seu_token_aqui", "Machine")
```

Reinicie o PowerShell para a variável surtir efeito.

### 1.3 Criar Pastas

```powershell
# Criar pasta de documentos
New-Item -ItemType Directory -Path "C:\DevOpsAutomation\documentos" -Force
New-Item -ItemType Directory -Path "C:\DevOpsAutomation\logs" -Force
New-Item -ItemType Directory -Path "C:\DevOpsAutomation\documentos\Integrado" -Force

# Atribuir permissões (usuário que rodar o serviço)
# Clique direito → Propriedades → Segurança → Editar
# Adicione o usuário com permissão de Controle Total
```

## 🔨 Fase 2: Compilação

### 2.1 Build Release

```powershell
cd "C:\Users\vlima\OneDrive - Uon Solutions\devops-automation\DevOpsAutomation.Service"
dotnet build -c Release

# Saída esperada:
# Build succeeded. [tempo] (BuildTimeSeconds)
```

### 2.2 Executar Testes

```powershell
dotnet test

# Saída esperada:
# Test Run Successful.
# Total tests: 4
# Passed: 4
```

## 📦 Fase 3: Instalação do Serviço

### 3.1 Preparar Caminho do Executável

```powershell
$servicePath = "C:\Users\vlima\OneDrive - Uon Solutions\devops-automation\DevOpsAutomation.Service\src\DevOpsAutomation.Service\bin\Release\net6.0-windows\DevOpsAutomation.Service.exe"

# Verificar que arquivo existe
Test-Path $servicePath
# Deve retornar: True
```

### 3.2 Instalar Serviço Windows

**PowerShell como Administrador:**

```powershell
sc create "DevOpsAutomationService" `
  binPath=$servicePath `
  displayName="DevOps Historia Automation Service" `
  start=auto `
  obj="Local System"

# Saída esperada:
# [SC] CreateService SUCCESS
```

### 3.3 Verificar Instalação

```powershell
Get-Service -Name "DevOpsAutomationService"

# Deve exibir:
# Status   Name                DisplayName
# ------   ----                -----------
# Stopped  DevOpsAutomationS   DevOps Historia Automation Service
```

## ▶️ Fase 4: Iniciar Serviço

### 4.1 Iniciar Manualmente

```powershell
Start-Service -Name "DevOpsAutomationService"
Start-Sleep -Seconds 3
Get-Service -Name "DevOpsAutomationService"

# Deve exibir Status: Running
```

### 4.2 Verificar Logs

```powershell
# Event Viewer
eventvwr.msc
# Procure em: Aplicativos e Serviços > Windows > Eventos da Aplicação

# Ou verifique arquivo de log
$logFile = "C:\Users\vlima\OneDrive - Uon Solutions\devops-automation\logs\service-*.log"
Get-Content -Path $logFile -Tail 20
```

## 🔄 Fase 5: Testar Funcionalidade

### 5.1 Colocar Documento de Teste

```powershell
# Copie um documento .docx para a pasta de documentos
Copy-Item -Path "Historia_DeployAutomatico_RC_GISBR.docx" `
  -Destination "C:\DevOpsAutomation\documentos\"
```

### 5.2 Aguardar Execução

O serviço executa a cada 30 minutos. Para testar imediatamente:

```powershell
# Parar o serviço
Stop-Service -Name "DevOpsAutomationService"

# Reiniciar (vai executar ao iniciar)
Start-Service -Name "DevOpsAutomationService"

# Verificar logs em 5-10 segundos
Get-Content -Path "C:\Users\vlima\OneDrive - Uon Solutions\devops-automation\logs\service-*.log" -Tail 30
```

### 5.3 Verificar no Azure DevOps

1. Acesse https://dev.azure.com/uon/UonSolutions
2. Vá para Backlogs
3. Procure pela PBI recém-criada
4. Verifique se título, descrição e critérios de aceite foram preenchidos corretamente

## ⚙️ Fase 6: Configuração Avançada

### 6.1 Alterar Intervalo de Execução

Edite `appsettings.json`:
```json
"Service": {
  "IntervalMs": 600000  // 10 minutos (em ms)
}
```

Depois reinicie o serviço:
```powershell
Restart-Service -Name "DevOpsAutomationService"
```

### 6.2 Alterar Pasta de Documentos

Edite `appsettings.json`:
```json
"Paths": {
  "Documentos": "C:\\Nova\\Pasta\\Documentos"
}
```

### 6.3 Alterar Verbosidade de Logs

Edite `appsettings.json`:
```json
"Logging": {
  "LogLevel": {
    "Default": "Debug"  // Mais detalhes (Debug, Information, Warning, Error)
  }
}
```

## 🛑 Desinstalação

Se precisar remover o serviço:

```powershell
# PowerShell como Administrador
Stop-Service -Name "DevOpsAutomationService"
sc delete "DevOpsAutomationService"

# Saída esperada:
# [SC] DeleteService SUCCESS
```

## 🔍 Monitoramento

### Verificação Diária

```powershell
# Status do serviço
Get-Service -Name "DevOpsAutomationService"

# Últimos 50 logs
Get-Content -Path "C:\Users\vlima\OneDrive - Uon Solutions\devops-automation\logs\service-*.log" -Tail 50 -Wait
```

### Alertas Importantes

Monitorar no Event Viewer por:
- `❌ Erro` - Falha no processamento
- `⚠️ Aviso` - Problemas não críticos
- `✅ Informação` - Operações normais

## 📊 Métricas Esperadas

**Após 1 dia:**
- 48 execuções (a cada 30 min)
- 0+ PBIs criadas
- 0+ erros de processamento

**Arquivo de log:**
- Tamanho: ~5-10 MB/dia
- Linhas: ~500-1000/dia

## 🆘 Troubleshooting

| Problema | Solução |
|----------|---------|
| Serviço não inicia | Verificar Event Viewer, revisar appsettings.json |
| Documentos não processados | Verificar nome de arquivo (.docx), formato do documento |
| Erro "Area not found" | Validar nome da área em DocumentParser.cs |
| Erro "Authentication failed" | Verificar PAT, regenerar se necessário |
| Arquivo não move para Integrado | Verificar permissões na pasta Integrado/ |

## 📞 Suporte

- Documentação: `README.md`
- GitHub Issues: https://github.com/seu-usuario/DevOpsAutomation/issues
- Email: vitor.ruiz@gmail.com

---

**Versão:** 1.0.0  
**Última Atualização:** 2026-09-29
