# 📌 Guia: Vincular ao GitHub

Este guia mostra como vincular o repositório local ao GitHub.

## ✅ Pré-requisitos

- Conta GitHub criada
- Git instalado e configurado
- Permissões para criar repositórios

## 🚀 Passo 1: Criar Repositório no GitHub

1. Acesse https://github.com/new
2. **Repository name:** `DevOpsAutomation`
3. **Description:** `Windows Service em C# para automação de histórias no Azure DevOps`
4. **Visibility:** Private (ou Public se desejar)
5. **NÃO inicialize** com README (já temos um)
6. Clique em **Create repository**

## 🔗 Passo 2: Vincular Repositório Local

Copie e execute EXATAMENTE UM dos comandos abaixo:

### Opção A: HTTPS (Mais fácil, recomendado)

```powershell
cd "C:\Users\vlima\OneDrive - Uon Solutions\devops-automation\DevOpsAutomation.Service"
git remote add origin https://github.com/SEU-USUARIO/DevOpsAutomation.git
git branch -M main
git push -u origin main
```

**Quando pedir senha:**
- Username: seu usuário GitHub
- Password: **Token de acesso pessoal** (não a senha)
  - Gerar em: https://github.com/settings/tokens
  - Permissões necessárias: `repo`, `workflow`

### Opção B: SSH (Mais seguro, requer configuração)

Se já tem SSH configurado:

```powershell
cd "C:\Users\vlima\OneDrive - Uon Solutions\devops-automation\DevOpsAutomation.Service"
git remote add origin git@github.com:SEU-USUARIO/DevOpsAutomation.git
git branch -M main
git push -u origin main
```

## 📋 Passo 3: Gerar Token de Acesso Pessoal (se usar HTTPS)

1. Acesse https://github.com/settings/tokens/new
2. **Nota:** `DevOpsAutomation`
3. **Permissões selecionadas:**
   - ✅ repo (todos os sub-itens)
   - ✅ workflow
4. **Expiração:** 90 dias (ou conforme sua política)
5. Clique em **Generate token**
6. **COPIE** o token (só aparece uma vez!)
7. Use como password no passo 2

## ✨ Passo 4: Verificar Vínculo

```powershell
git remote -v
```

Deve exibir:
```
origin  https://github.com/SEU-USUARIO/DevOpsAutomation.git (fetch)
origin  https://github.com/SEU-USUARIO/DevOpsAutomation.git (push)
```

## 🎉 Pronto!

Seu repositório está vinculado ao GitHub. Agora você pode:

- **Ver no GitHub:** https://github.com/SEU-USUARIO/DevOpsAutomation
- **Fazer push:** `git push`
- **Fazer pull:** `git pull`
- **Criar branches:** `git checkout -b feature/nome`

## 📤 Push de Mudanças Futuras

```powershell
# Fazer mudanças nos arquivos
git add .
git commit -m "Descrição das mudanças"
git push
```

## 🌳 Criar Branch de Desenvolvimento

```powershell
git checkout -b develop
git push -u origin develop
```

## ⚙️ Configurar Branch Padrão (Opcional)

No GitHub:
1. Vá para Settings → Branches
2. **Default branch:** selecione `main` ou `develop`
3. Clique em **Update**

## 🔐 Proteger Branch Principal (Recomendado)

No GitHub:
1. Settings → Branches
2. **Add rule**
3. **Branch name pattern:** `main`
4. Ative:
   - ✅ Require pull request reviews before merging
   - ✅ Require status checks to pass before merging
5. Clique em **Create**

## 🐛 Troubleshooting

### Erro: "fatal: remote origin already exists"

```powershell
git remote remove origin
git remote add origin https://github.com/SEU-USUARIO/DevOpsAutomation.git
```

### Erro: "Authentication failed"

- Verificar se token foi gerado corretamente
- Certificar que token tem permissões `repo` e `workflow`
- Tentar novamente com username/token

### Erro: "fatal: could not read Username"

No Windows, salvar credenciais:

```powershell
git config --global credential.helper manager-core
```

E fazer push novamente. Windows pedirá para salvar credenciais.

## 📚 Referências

- Documentação Git: https://git-scm.com/docs
- GitHub Help: https://docs.github.com
- Tokens GitHub: https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token

---

**Dúvidas?** Verifique o README.md ou abra uma issue no GitHub.
