# Sonic Riders: Zero Gravity — Windows recompilation project

Projeto experimental e não oficial para executar a edição PS2 USA (`SLUS_216.42`) no Windows, baseado em [ps2-web-recomp](https://github.com/snuri00/ps2-web-recomp), de Samir Nuri.

Este repositório contém o runtime, os recompiladores e as adaptações de Windows. A árvore de código não inclui ISO, BIOS, executável recompilado, saves ou capturas de microcódigo VU. Não há vínculo com a SEGA.

## Baixar e jogar no Windows

**[Baixar versão Windows v0.1.1](https://github.com/Ukinhas/sonic-riders-zero-gravity-windows/releases/tag/v0.1.1)** — versão experimental, pronta para executar, sem compilação, Python ou navegador.

1. Baixe `SonicRidersZeroGravity-Windows-v0.1.1.zip` nos arquivos da Release (não o arquivo “Source code”).
2. Extraia o ZIP inteiro e abra `Jogar-Sonic.exe`.
3. Selecione sua ISO da edição **PS2 USA SLUS_216.42** e clique em **Jogar**.
4. Aguarde a importação dos dados para a instalação local (aproximadamente 5 GB livres).
5. Nas próximas vezes, basta abrir `Jogar-Sonic.exe`. A ISO original não precisa mais estar conectada ou no mesmo caminho.

O download inclui o runtime nativo recompilado e suas bibliotecas, mas não a ISO, BIOS, arquivos de recursos do disco ou saves. O importador valida o executável da edição suportada, copia os dados e compara o SHA-256 da cópia com o fluxo original. Isso verifica a cópia, não certifica todos os recursos contra um dump de referência. No primeiro uso, o jogo pode pedir a criação/formatação do cartão virtual. Os dados continuam necessários: eles ficam instalados no PC, não desaparecem por ser um executável nativo.

Requer Windows 10/11 x64, .NET Framework 4.8 e GPU/driver compatível com wgpu/Vulkan. Pacote sem assinatura digital. Instalação, saves e logs ficam em `%LOCALAPPDATA%/SonicRidersZeroGravity`. Os testes usam perfil vazio no PC de desenvolvimento (RTX 4060); isso não equivale a testar todas as dependências em uma reinstalação do Windows ou em outras GPUs.

## Estado atual

- Aplicação Windows nativa com SDL2 e wgpu-native; execução sem navegador ou Python.
- Recompilação estática do processador Emotion Engine; outros componentes PS2 são implementados pelo runtime.
- Recompilador VU1 e captura local de microprogramas para melhorar desempenho.
- Temporização NTSC de 60 Hz, mantendo a velocidade original.
- Configuração de idioma do sistema em inglês. Um save existente pode preservar opções anteriores do jogo.
- Patch de desbloqueio de conteúdo, com cópia de segurança do cartão existente. Não houve validação exaustiva de cada item desbloqueável.

Python e ferramentas de desenvolvimento são necessários apenas para quem deseja recompilar o projeto. Quem usa o ZIP da Release não precisa dessas ferramentas.

## Controles

| Tecla | Controle PS2 |
|---|---|
| W/A/S/D | Analógico esquerdo |
| K | X / confirmar |
| L | Círculo / voltar |
| I | Triângulo |
| J | Quadrado |
| U | L1 |
| O | R1 |
| Setas | Direcional |
| Enter / Esc | Start / pausa |
| Backspace | Select |
| 1 / 2 | L2 / R2 |
| 3 / 4 | L3 / R3 |
| F11 | Tela cheia |

## Compilação

Veja [BUILD-WINDOWS.md](BUILD-WINDOWS.md). Os arquivos gerados a partir do jogo devem permanecer locais. O repositório não contém as 76 capturas VU usadas na medição privada; elas precisam ser coletadas novamente a partir da sua cópia. Sem essa cobertura, o interpretador VU pode reduzir muito o desempenho.

## Desempenho e limites

A build privada foi medida em 22 combinações, cobrindo trechos de 16 pistas e 18 personagens, em uma RTX 4060. As médias em janelas de cinco segundos ficaram aproximadamente entre 59,5 e 60 FPS, com microtravadas isoladas. Isso **não garante 60 FPS constantes**, nem valida todas as voltas, caminhos ou combinações. As medições de apresentação representam submissão ao renderizador, não captura física do monitor.

O modo online não foi implementado. 120 FPS reais, mantendo a velocidade original, não foi concluído.

## Origem e licenças

- Base: `snuri00/ps2-web-recomp`, commit `c628ae843209f6e4cef34ec04efb6267a1618882`.
- Runtime e ferramentas: [MIT](LICENSE), preservando o crédito de Samir Nuri.
- Tabelas IPU adaptadas de Play!: [aviso e licença](runtime/src/ipu/NOTICE).
- Patch de desbloqueio adaptado do código de Skiller listado em [GameHacking.org](https://gamehacking.org/game/104986).
- Dependências externas conservam suas próprias licenças; inclua-as ao distribuir qualquer build permitida.
- Sonic Riders: Zero Gravity e seus elementos pertencem aos respectivos titulares. A licença deste código não licencia os dados do jogo.

O [README original](README-upstream.md) descreve a base genérica, inclusive outros jogos; suas afirmações não constituem testes desta adaptação.
