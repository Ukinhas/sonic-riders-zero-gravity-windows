# Windows v0.1.0 — importação de ISO (experimental)

## Como jogar

1. Baixe **SonicRidersZeroGravity-Windows-v0.1.0.zip** abaixo. Não é necessário baixar “Source code”.
2. Extraia o ZIP inteiro e abra **Jogar-Sonic.exe**.
3. Selecione sua ISO **Sonic Riders: Zero Gravity, PS2 USA, SLUS_216.42** e clique em **Jogar**.

Sem compilar, instalar Python, abrir terminal ou usar navegador. A ISO permanece no seu computador e precisa continuar no caminho selecionado. Não há upload da ISO. O download inclui runtime recompilado e bibliotecas; não inclui ISO, BIOS, arquivos de recursos do disco ou saves.

## Requisitos e controles

- Windows 10/11 x64, .NET Framework 4.8, GPU/driver compatível com wgpu/Vulkan.
- WASD: movimento; setas: menus; K: X; L: bola; I: triângulo; J: quadrado; U: L1; O: R1.
- Enter/Esc: Start; Backspace: Select; F11: tela cheia.
- Configuração de sistema em inglês; opções em saves antigos podem prevalecer.
- Sem save anterior, confirme no jogo a criação/formatação do cartão virtual.
- Saves e `game.log`: `%LOCALAPPDATA%/SonicRidersZeroGravity`.

## Estado da versão

O runtime otimizado anterior foi preservado, incluindo 173 rotinas VU e temporização NTSC de 60 Hz. Há microtravadas conhecidas; não é uma garantia de 60 FPS constantes. Não possui modo online.

O importador C# foi compilado e o pacote montado com as DLLs e licenças. **O novo fluxo completo de importação e primeiro uso não foi testado em um PC limpo.** Por isso esta publicação é uma pré-versão experimental. O executável não tem assinatura digital.

`SHA256SUMS.txt` fornece o hash do ZIP. Leia também `LEIA-ME.txt` dentro do pacote.
