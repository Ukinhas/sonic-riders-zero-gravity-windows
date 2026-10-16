# Sonic Riders: Zero Gravity — Windows recompilation project

Projeto experimental e não oficial para executar a edição PS2 USA (`SLUS_216.42`) no Windows, baseado em [ps2-web-recomp](https://github.com/snuri00/ps2-web-recomp), de Samir Nuri.

Este repositório contém o runtime, os recompiladores e as adaptações de Windows. **Não inclui o jogo, ISO, BIOS, executável recompilado, saves ou capturas de microcódigo VU.** Para compilar e jogar, forneça seus próprios arquivos locais. Não há vínculo com a SEGA.

## Estado atual

- Aplicação Windows nativa com SDL2 e wgpu-native; execução sem navegador ou Python.
- Recompilação estática do processador Emotion Engine; outros componentes PS2 são implementados pelo runtime.
- Recompilador VU1 e captura local de microprogramas para melhorar desempenho.
- Temporização NTSC de 60 Hz, mantendo a velocidade original.
- Configuração de idioma do sistema em inglês. Um save existente pode preservar opções anteriores do jogo.
- Patch de desbloqueio de conteúdo, com cópia de segurança do cartão existente. Não houve validação exaustiva de cada item desbloqueável.

Python e ferramentas de desenvolvimento são necessários apenas na preparação/compilação. Este é um projeto de desenvolvimento, não um download pronto do jogo.

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
