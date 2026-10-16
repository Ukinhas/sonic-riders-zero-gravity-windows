# Windows v0.1.2 — pacote completo

1. Baixe **Instalar-Sonic.exe** nos arquivos abaixo.
2. Abra o instalador e escolha a pasta do jogo. Ele baixa **2,66 GB**; mantenha a conexão até terminar.
3. Abra **SonicRidersZeroGravity-Completo.exe** para jogar. Não é necessário possuir uma ISO separada, importar arquivos, compilar ou instalar Python.

O instalador baixa duas partes automaticamente e verifica o SHA-256 do executável completo antes de liberá-lo. Não baixe as partes manualmente nem o “Source code” para jogar. Depois da instalação, o jogo funciona offline e o instalador pode ser apagado.

Reserve **8 GB livres**: aproximadamente 2,66 GB para o EXE e 5 GB para os dados preparados na primeira abertura em `%LOCALAPPDATA%/SonicRidersZeroGravity`. Se instalar em outro disco, mantenha também 5 GB livres no disco do perfil Windows. Os dados do jogo estão incorporados ao pacote; a primeira abertura os extrai automaticamente. Saves existentes são preservados.

## Controles e requisitos

- WASD: movimento; setas: menus.
- K: X/confirmar; L: círculo; I: triângulo; J: quadrado.
- U: L1; O: R1; Enter/Esc: Start; F11: tela cheia.
- Idioma de sistema e perfil inicial em inglês; desbloqueio de conteúdo ativo.
- Windows 10 x64 1903+ ou Windows 11 x64; GPU/driver compatível com wgpu. O instalador usa .NET Framework 4.8.

Projeto experimental e não oficial, sem vínculo com a SEGA. A licença MIT do runtime não altera a titularidade dos dados do jogo. Sem assinatura digital ou modo online. O runtime é o mesmo do executável completo anteriormente medido: velocidade original, médias próximas de 60 FPS nos trechos avaliados, com quedas isoladas. Não há garantia de 60 FPS constantes em todos os PCs.

Esta atualização acrescenta a distribuição do pacote completo e o instalador de download. O EXE do jogo não foi recompilado nesta versão. A integridade das partes e do EXE é verificada por SHA-256; não representa um novo teste de todas as pistas.

SHA-256 do EXE completo: `61d5952de74a77b6501c7564358cbcf4da28fb25de394ed6c5959e3c203c751d`.

## Teste do instalador publicado

Download real pelo instalador, montagem do EXE, extração e duas aberturas aprovados com o perfil anterior isolado e a ISO original bloqueada. Os 16 arquivos extraídos passaram na verificação de integridade. Veja [o relatório e seus limites](https://github.com/Ukinhas/sonic-riders-zero-gravity-windows/blob/main/TESTES-v0.1.2-completo.md): teste no mesmo Windows, não em uma instalação limpa; teclado e corridas não foram revalidados neste ensaio.
