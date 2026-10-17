# Serpentina Racer

Corrida arcade 3D low-poly por uma estrada sinuosa de montanha, feita do zero em **Godot 4 (C#/.NET)**. Todo o carro, a pista, o cenário, a interface e os sons são gerados por código — não há nenhum modelo, textura ou arquivo de áudio importado.

## Tecnologias utilizadas

- **Godot Engine 4.7.2 (build Mono/.NET)**
- **C# / .NET 8**
- Geometria procedural via `SurfaceTool`, `MultiMesh` e meshes primitivas (sem assets externos)
- Áudio sintetizado em tempo real (PCM gerado por código, sem arquivos `.wav`/`.mp3`)

## Funcionalidades implementadas

- Carro controlável com física arcade (`VehicleBody3D`): aceleração progressiva, ré, frenagem, freio de mão, perda de aderência em curva e reposicionamento manual.
- Carro modelado por código com carroceria, capô, teto, para-brisa, janelas, para-choques, faróis, lanternas, retrovisores, aerofólio e rodas com pneu/aro diferenciados e giro visual real.
- Pista fechada gerada proceduralmente (spline Catmull-Rom) com curvas, subidas/descidas, faixas de pista, acostamento, guarda-corpos com colisão e linha de largada/chegada com pórtico.
- Cenário proceduralmente distribuído: terreno com relevo, montanhas no horizonte, árvores, pedras e arbustos, posicionados sem bloquear a pista.
- Câmera de perseguição em terceira pessoa com suavização.
- Sistema de corrida com contagem regressiva, cronômetro, contagem de voltas e checkpoints ordenados (impede voltas "fantasma" feitas na contramão).
- Recorde de melhor volta e melhor tempo total salvos localmente (`user://savegame.json`).
- Menu principal, HUD de corrida, tela de pausa e tela de resultados com opção de jogar novamente ou voltar ao menu.
- Efeitos sonoros e motor sintetizados por código (sem arquivos de áudio).

## Como instalar e executar

### Opção 1 — Executável pronto (Windows)

1. Baixe/clique em `builds/windows/SerpentinaRacer.exe` (gerado localmente — veja "Compilar e exportar" abaixo caso a pasta não exista no seu clone, pois builds não são versionados).
2. Execute o `.exe` diretamente. Não é necessário instalar nada.

### Opção 2 — Rodar pelo editor Godot

1. Instale o **Godot 4.7.2 (.NET/Mono)**: https://godotengine.org/download
2. Instale o **.NET SDK 8.0**: https://dotnet.microsoft.com/download
3. Abra a pasta `SerpentinaRacer/` no Godot (`project.godot`).
4. Pressione **F5** para rodar o jogo.

## Controles

| Tecla | Ação |
|---|---|
| `W` / seta cima | Acelerar |
| `S` / seta baixo | Frear / Ré |
| `A` `D` / setas esquerda-direita | Virar |
| `Espaço` | Freio de mão |
| `R` | Reposicionar o carro na pista |
| `Esc` | Pausar / retomar |

## Estrutura do projeto

```
SerpentinaRacer/
├── project.godot              # Configuração do projeto Godot
├── SerpentinaRacer.csproj/.sln # Projeto C#/.NET
├── Scenes/                    # Cenas (.tscn) — raízes mínimas; tudo é montado por código
│   ├── MainMenu.tscn
│   └── Race.tscn
├── Scripts/
│   ├── Core/                  # GameManager, SaveSystem, utilitários (texturas, formatação de tempo)
│   ├── Car/                   # Modelo visual e controlador físico do carro
│   ├── Track/                 # Geração da pista, spline, checkpoints
│   ├── Environment/           # Terreno, montanhas, vegetação, céu/luz
│   ├── Race/                  # Orquestração da corrida, câmera, lógica de voltas
│   ├── UI/                    # Menu, HUD, pausa, resultados
│   └── Audio/                 # Síntese e gerenciamento de som
└── icon.svg
```

## Compilar e exportar um executável

```powershell
# Compilar o C# (verifica erros de compilação)
dotnet build

# Exportar um executável Windows (requer export templates do Godot 4.7.2 instalados)
godot --headless --export-release "Windows Desktop" "builds/windows/SerpentinaRacer.exe"
```

As configurações de exportação usadas ficam em `export_presets.cfg`.

## Limitações conhecidas

- Testado via automação headless (compilação, carregamento de cenas, simulação de física e execução do executável exportado); não há captura de vídeo/tela do gameplay interativo neste ambiente, pois ele não possui sessão gráfica interativa disponível para o agente.
- A pasta `builds/` não é versionada no Git (binários grandes); gere o executável localmente com o comando acima quando necessário.
