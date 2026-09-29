# ⚡ Quick Start - 5 Minutos

Comece em 5 minutos! Siga **EXATAMENTE** nesta ordem.

## 1️⃣ Crie Repositório no GitHub (1 min)

1. Vá para https://github.com/new
2. **Repository name:** `DevOpsAutomation`
3. **Visibility:** Private
4. **NÃO inicialize** com README
5. Clique em **Create repository**

**Copie este URL:**
```
https://github.com/SEU-USUARIO/DevOpsAutomation.git
```

## 2️⃣ Execute Este Comando (2 min)

Abra PowerShell **COMO ADMIN** e copie/cole tudo:

```powershell
cd "C:\Users\vlima\OneDrive - Uon Solutions\devops-automation\DevOpsAutomation.Service"
git remote add origin https://github.com/SEU-USUARIO/DevOpsAutomation.git
git branch -M main
git push -u origin main
```

**Quando pedir senha:**
- Username: Seu usuário GitHub
- Password: **TOKEN** (não a senha!)

Se não tem token, gere em: https://github.com/settings/tokens/new
- Permissões: `repo` + `workflow`
- Copy o token (aparece 1 vez só!)

## 3️⃣ Verifique (1 min)

```powershell
git remote -v
```

Deve exibir:
```
origin  https://github.com/SEU-USUARIO/DevOpsAutomation.git (fetch)
origin  https://github.com/SEU-USUARIO/DevOpsAutomation.git (push)
```

## 4️⃣ Acesse seu Repositório (1 min)

Abra no navegador:
```
https://github.com/SEU-USUARIO/DevOpsAutomation
```

Veja seus 2 commits! 🎉

## ✅ Pronto!

Seu repositório está vinculado ao GitHub. Agora:

```powershell
# Compilar
dotnet build -c Release

# Testar
dotnet test

# Próximas mudanças
git add .
git commit -m "Seu descritivo"
git push
```

---

**Tempo total:** ~5 minutos ⏱️

**Problemas?** Veja `GITHUB_SETUP.md` (versão completa com troubleshooting)

**Próximo passo?** Veja `DEPLOYMENT.md` para instalar o Windows Service.
