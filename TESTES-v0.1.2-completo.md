# Teste do instalador público v0.1.2-completo

Executado em 16/10/2026 no PC de desenvolvimento, Windows x64 e NVIDIA RTX 4060. Foi usado o instalador efetivamente baixado da URL pública do GitHub Releases, sem autenticação no download.

## Resultado

- A interface normal permitiu criar uma pasta vazia e iniciar a instalação.
- O próprio instalador baixou as duas partes do GitHub, reuniu 2.663.296.744 bytes e concluiu a verificação de integridade.
- SHA-256 do instalador baixado: `9c6315182b0daffd61a29f9e7c4ee117f9b1b857ba22df35cc7d0824c9c602a9`.
- SHA-256 do EXE instalado: `61d5952de74a77b6501c7564358cbcf4da28fb25de394ed6c5959e3c203c751d`.
- Antes de abrir o jogo, a instalação anterior foi movida temporariamente para um local de preservação. O caminho normal de dados e save estava ausente.
- A ISO original, o EXE privado anterior e o `game.dat` anterior permaneceram abertos com bloqueio exclusivo durante as duas aberturas do teste.
- O EXE baixado foi iniciado diretamente, com PATH limitado ao Windows e sem variáveis de desenvolvimento PS2/Python/Node. Não foi utilizado o botão “Sim” do instalador para essa abertura, permitindo controlar o ambiente do processo.
- O pacote extraiu seus dados, criou o cartão virtual inicial incluído e abriu o aviso de autosave em inglês. Nenhum seletor de ISO foi exibido.
- Os 16 arquivos extraídos foram comparados por tamanho e SHA-256 com o índice embutido no EXE: todos correspondem.
- Uma segunda abertura diretamente pelo EXE reutilizou a instalação recém-criada e voltou à tela inicial em inglês. A ISO e a instalação anterior continuavam bloqueadas.
- As bibliotecas externas carregadas vieram do pacote (`SDL2.dll`, `wgpu_native.dll`, `libc++.dll`, `libunwind.dll`). Os demais módulos observados pertencem ao Windows e ao driver gráfico. Python, Node e compiladores não foram usados pelo jogo.
- O perfil original foi restaurado e o SHA-256 do save pessoal permaneceu idêntico ao anterior ao teste.

Não foi encontrado erro no download, na montagem, na extração ou nas duas aberturas. Não houve alteração no executável publicado.

## Limites

Este é um teste com dados de aplicação vazios no mesmo Windows, **não uma máquina virtual ou instalação limpa do sistema operacional**. Windows Sandbox não estava disponível. O PC já possui .NET Framework 4.8.1 e driver NVIDIA; a compatibilidade com versões mínimas do framework e outras GPUs não foi validada aqui.

O instalador requer .NET Framework 4.8; o jogo depende de Windows compatível e de driver gráfico funcional. Não é possível garantir funcionamento em qualquer PC apenas com este teste. A conexão de rede não foi desligada: a independência da ISO e dos arquivos anteriores foi verificada por bloqueio exclusivo, mas não se trata de um ensaio com a rede desativada.

A entrada K enviada pela automação não avançou de forma confirmável o aviso de autosave. Portanto, este ensaio confirma instalação e abertura, não constitui uma nova validação do teclado, de uma corrida ou dos 60 FPS. As verificações de jogabilidade anteriores têm seu próprio escopo.
