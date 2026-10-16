# Windows v0.1.1 — importe uma vez, depois abra o EXE

1. Baixe **SonicRidersZeroGravity-Windows-v0.1.1.zip** e extraia o ZIP inteiro.
2. Abra **Jogar-Sonic.exe**, selecione sua ISO **PS2 USA SLUS_216.42** e clique em Jogar.
3. Aguarde a cópia e verificação dos dados (aproximadamente **5 GB livres**).
4. Nas próximas vezes, abra o mesmo EXE: o jogo inicia automaticamente. A ISO original pode estar desconectada ou em outro caminho.

O download não contém a ISO, BIOS, recursos do disco ou saves. É necessário fornecer a própria cópia na primeira importação. Os dados importados ficam em `%LOCALAPPDATA%/SonicRidersZeroGravity`; preserve essa instalação e mantenha a pasta `engine` junto do EXE.

## Alterações

- Instalação local dos dados, em vez de depender permanentemente do caminho da ISO.
- Progresso e cancelamento; cópias incompletas são descartadas.
- Validação da edição e da estrutura da ISO, verificação SHA-256 da cópia local.
- Reconhecimento de instalação ausente/incompleta e reimportação.
- Mensagem clara ao tentar jogar sem selecionar uma ISO.
- Saves anteriores preservados; perfil isolado disponível para testes.

Requer Windows 10/11 x64, .NET Framework 4.8 e GPU/driver compatível com wgpu/Vulkan. Sem Python, compilação, terminal ou navegador para jogar. Controles WASD, K/L/I/J, U/O e idioma de sistema inglês mantidos. Sem assinatura digital, modo online ou garantia de 60 FPS constantes.

Veja `TESTES-v0.1.1.md` no repositório para o escopo de validação. Os testes de primeiro uso são feitos com perfil vazio neste PC, não em uma reinstalação do Windows ou em todas as GPUs.
