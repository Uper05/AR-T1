# AR Furniture Placer (AR-T1)

Aplicativo de **Realidade Aumentada** para Android, desenvolvido em Unity, que permite visualizar móveis 3D no ambiente real. O app detecta superfícies horizontais (chão, mesas) pela câmera do celular e deixa o usuário **colocar, mover, girar, redimensionar, recolorir e excluir** móveis virtuais, ajudando a imaginar como um cômodo ficaria decorado.

O projeto também pode ser testado direto no Unity Editor, sem celular, usando o **XR Simulation** do AR Foundation.

---

## Funcionalidades

- **Detecção de planos** — superfícies horizontais são detectadas e exibidas com o `AR Default Plane`.
- **Catálogo de 23 móveis** — camas, estantes, cadeiras, armários, mesas de centro, luminárias, planta, sofás, banqueta, mesa e vasos, escolhidos por uma barra de botões na interface.
- **Colocação** — tocar num plano vazio posiciona o móvel escolhido e já o seleciona.
- **Seleção** — tocar num móvel já colocado o seleciona; um anel azul aparece na base do objeto.
- **Mover** — arrastar o dedo desloca o móvel selecionado sobre o plano.
- **Escalar** — gesto de pinça com dois dedos (limitado a 0,2× – 3× do tamanho original).
- **Girar** — torção com dois dedos gira o móvel no eixo vertical.
- **Recolorir** — paleta de cores no painel de seleção; o botão **Restaurar** volta às cores originais do modelo.
- **Excluir** — remove o móvel selecionado da cena.
- **UI protegida** — toques sobre botões da interface não colocam nem selecionam objetos.
- **Controles de PC** — mouse e teclado funcionam no Editor para testes com XR Simulation.
- **Ferramenta de configuração** — menu `Tools > AutoFix > Run All` no Editor gera os prefabs dos móveis, monta a UI e ajusta cena de build, XR e Application ID.

### Controles

| Ação | Celular (toque) | Editor / PC |
|---|---|---|
| Colocar móvel | Toque num plano vazio | Clique esquerdo num plano |
| Selecionar | Toque no móvel | Clique esquerdo no móvel |
| Mover | Arrastar um dedo | Arrastar com botão esquerdo |
| Escalar | Pinça com dois dedos | Roda do mouse |
| Girar | Torção com dois dedos | Arrastar com botão do meio |
| Navegar na simulação | — | Botão direito + mouse, `W` `A` `S` `D` |
| Desmarcar | Toque fora de planos/móveis | Clique fora de planos/móveis |

---

## Tecnologias

| Item | Versão |
|---|---|
| **Engine** | Unity **6000.3.10f1** (Unity 6.3) |
| **Linguagem** | C# |
| **Plataforma alvo** | Android (ARCore) |
| **Render pipeline** | Universal Render Pipeline (URP) |

### Principais bibliotecas / pacotes

| Pacote | Versão | Uso |
|---|---|---|
| `com.unity.xr.arfoundation` | 6.3.5 | Base de RA: sessão, planos, raycast, XR Simulation |
| `com.unity.xr.arcore` | 6.3.5 | Provedor de RA para Android (Google ARCore) |
| `com.unity.render-pipelines.universal` | 17.3.0 | Renderização (perfis Mobile e PC em `Assets/Settings`) |
| `com.unity.inputsystem` | 1.18.0 | Entrada usada pela navegação do XR Simulation |
| `com.unity.ugui` | 2.0.0 | Interface (Canvas, botões) e TextMeshPro |
| `com.unity.ai.navigation` | 2.0.10 | Incluído pelo template |
| `com.unity.timeline` | 1.8.10 | Incluído pelo template |
| `com.unity.test-framework` | 1.6.0 | Testes |
| `com.unity.visualscripting` | 1.9.9 | Incluído pelo template |
| `com.unity.ide.visualstudio` / `com.unity.ide.rider` | 2.0.26 / 3.0.39 | Integração com IDEs |
| `com.unity.toolchain.linux-x86_64-linux` | 1.1.0 | Toolchain para compilar no Linux |

A lista completa está em [`Packages/manifest.json`](Packages/manifest.json).

---

## Estrutura do projeto

```
Assets/
├── a.unity                    # Cena principal (única cena de build)
├── ARFurniturePlacer.cs       # Entrada (toque/mouse), colocação, mover, escalar e girar
├── ObjectColorSelector.cs     # Seleção atual, painel de seleção, cor e exclusão
├── ColorableObject.cs         # Cor via MaterialPropertyBlock e anel de seleção
├── ColorButton.cs             # Botão da paleta de cores
├── ARDiagnostics.cs           # Logs de diagnóstico do XR Simulation (só no Editor)
├── Editor/
│   └── ProjectAutoFix.cs      # Menu Tools > AutoFix > Run All
├── FBX/                       # Modelos 3D dos móveis
├── Prefabs/
│   ├── AR Default Plane.prefab
│   └── Furniture/             # Prefabs dos móveis (com BoxCollider)
├── Settings/                  # Assets do URP (Mobile/PC)
└── XR/                        # Configurações de XR (ARCore e XR Simulation)
```

---

## Requisitos e dependências

### Para desenvolver
- **Unity Hub** e **Unity Editor 6000.3.10f1**.
- Módulo **Android Build Support** instalado no Editor, incluindo **OpenJDK** e **Android SDK & NDK Tools** (selecione ao instalar o Editor pelo Unity Hub).
- (Opcional) Visual Studio, VS Code ou JetBrains Rider para editar os scripts.

Os pacotes Unity listados acima são baixados automaticamente pelo Package Manager na primeira abertura do projeto — não é necessário instalar nada manualmente.

### Para executar no celular
- Aparelho Android **compatível com ARCore** ([lista oficial](https://developers.google.com/ar/devices)).
- Android **7.1 (API 25)** ou superior.
- **Google Play Services for AR** instalado (a Play Store instala automaticamente quando necessário).
- Arquitetura **ARM64**.

---

## Configurações do projeto

| Configuração | Valor |
|---|---|
| Cena de build | `Assets/a.unity` |
| Application ID (Android) | `com.DefaultCompany.ARFurniturePlacer` |
| Versão | 0.1.0 |
| Minimum API Level | 25 (Android 7.1) |
| Target API Level | Automático (maior instalado) |
| Scripting Backend | IL2CPP |
| Arquitetura | ARM64 |
| Active Input Handling | Both (Input Manager + Input System) |
| XR Plug-in (Android) | ARCore |
| XR Plug-in (Editor/Standalone) | XR Simulation |

Essas opções ficam em **Edit > Project Settings > Player** e **Edit > Project Settings > XR Plug-in Management**. Se algo estiver desconfigurado, rode **Tools > AutoFix > Run All**.

> **Observação:** a cor dos móveis é aplicada na propriedade `_BaseColor` (shader URP/Lit). Se trocar para o Built-in Render Pipeline, altere a constante em `ColorableObject.cs` para `_Color`.

---

## Como executar

### 1. Abrir o projeto
```bash
git clone <url-do-repositorio>
```
No Unity Hub: **Add > Add project from disk**, selecione a pasta `AR-T1` e abra com a versão **6000.3.10f1**. Aguarde a importação dos assets e pacotes.

### 2. Testar no Editor (XR Simulation)
1. Abra a cena `Assets/a.unity`.
2. Confira em **Edit > Project Settings > XR Plug-in Management** (aba PC/Standalone) que **XR Simulation** está marcado.
3. (Opcional) Escolha um ambiente simulado em **Window > XR > AR Foundation > XR Environment**.
4. Clique em **Play**. Use o botão direito + mouse e `WASD` para andar pelo ambiente até os planos serem detectados, depois clique para colocar móveis.

### 3. Build para Android
1. **File > Build Profiles** → selecione **Android** → **Switch Platform**.
2. Verifique que `Assets/a.unity` está na lista de cenas.
3. No celular, ative **Opções do desenvolvedor** e **Depuração USB**, e conecte-o ao computador.
4. Clique em **Build And Run** e salve o `.apk` (por exemplo, na pasta `Build/`).
   Para só gerar o arquivo, use **Build** e instale depois com:
   ```bash
   adb install -r Build/AR-T1.apk
   ```
5. Abra o app, aponte a câmera para o chão e movimente o celular devagar até os planos aparecerem.

## Funcionamento

![Demonstração do projeto](gifs/Gif_funcionamento.mp4)
