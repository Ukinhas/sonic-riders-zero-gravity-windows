# Sonic Riders: Zero Gravity para Windows

Projeto experimental e não oficial para executar a edição PS2 USA (`SLUS_216.42`) no Windows, baseado em [ps2-web-recomp](https://github.com/snuri00/ps2-web-recomp), de Samir Nuri.

Não há vínculo com a SEGA. A versão completa está disponível no GitHub Releases, pronta para instalar e jogar.

## Baixar e jogar no Windows

### Pacote completo — sem importar ISO

**[Baixar o instalador Windows completo](https://github.com/Ukinhas/sonic-riders-zero-gravity-windows/releases/download/v0.1.2-completo/Instalar-Sonic.exe)**

1. Baixe e abra **`Instalar-Sonic.exe`**.
2. Escolha uma pasta e aguarde o download de **2,66 GB**.
3. Abra **`SonicRidersZeroGravity-Completo.exe`** e aguarde a preparação dos arquivos na primeira abertura.
4. Nas próximas vezes, abra o mesmo EXE para jogar, inclusive offline.

**Não precisa de ISO separada, importação, Python ou compilação.** O instalador baixa as partes automaticamente; não é necessário baixar os arquivos `.part1`, `.part2` ou “Source code” manualmente.

O sistema e o perfil inicial estão em **inglês**, com desbloqueio de conteúdo ativado. Um save existente pode preservar opções anteriores.

### Requisitos

- **Windows 10 x64 (1903+) ou Windows 11 x64**.
- **.NET Framework 4.8** para executar o instalador.
- Placa de vídeo e driver compatíveis com o renderizador wgpu; mantenha o driver atualizado.
- **8 GB livres**, contando o EXE e os dados extraídos. A primeira abertura precisa de aproximadamente **5 GB livres no disco do perfil Windows**, mesmo que o EXE esteja em outro disco.
- Internet durante o download inicial.

Os saves e dados preparados ficam em `%LOCALAPPDATA%\SonicRidersZeroGravity`. Preserve essa pasta para manter seu progresso. O pacote não tem assinatura digital.

Veja os [detalhes da versão completa](RELEASE-v0.1.2-completo.md). Os dados incorporados à distribuição mantêm sua titularidade original; a licença do runtime não se estende a eles.

<details>
<summary>Versão alternativa v0.1.1 — importar a própria ISO</summary>

**[Baixar versão Windows v0.1.1](https://github.com/Ukinhas/sonic-riders-zero-gravity-windows/releases/tag/v0.1.1)** — versão experimental, pronta para executar, sem compilação, Python ou navegador.

1. Baixe `SonicRidersZeroGravity-Windows-v0.1.1.zip` nos arquivos da Release (não o arquivo “Source code”).
2. Extraia o ZIP inteiro e abra `Jogar-Sonic.exe`.
3. Selecione sua ISO da edição **PS2 USA SLUS_216.42** e clique em **Jogar**.
4. Aguarde a importação dos dados para a instalação local (aproximadamente 5 GB livres).
5. Nas próximas vezes, basta abrir `Jogar-Sonic.exe`. A ISO original não precisa mais estar conectada ou no mesmo caminho.

O download inclui o runtime nativo recompilado e suas bibliotecas, mas não a ISO, BIOS, arquivos de recursos do disco ou saves. O importador valida o executável da edição suportada, copia os dados e compara o SHA-256 da cópia com o fluxo original. Isso verifica a cópia, não certifica todos os recursos contra um dump de referência. No primeiro uso, o jogo pode pedir a criação/formatação do cartão virtual. Os dados continuam necessários: eles ficam instalados no PC, não desaparecem por ser um executável nativo.

</details>

## Estado atual

- Aplicação Windows nativa com SDL2 e wgpu-native; execução sem navegador ou Python.
- Recompilação estática do processador Emotion Engine; outros componentes PS2 são implementados pelo runtime.
- Recompilador VU1 e captura local de microprogramas para melhorar desempenho.
- Temporização NTSC de 60 Hz, mantendo a velocidade original.
- Configuração de idioma do sistema em inglês. Um save existente pode preservar opções anteriores do jogo.
- Patch de desbloqueio de conteúdo, com cópia de segurança do cartão existente. Não houve validação exaustiva de cada item desbloqueável.

Python e ferramentas de desenvolvimento são necessários apenas para quem deseja recompilar o projeto. Quem usa o instalador ou o ZIP da Release não precisa dessas ferramentas.

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

## Testes e ajuda

O [teste do instalador publicado](TESTES-v0.1.2-completo.md) confirmou download pelo GitHub, montagem do EXE, extração dos 16 arquivos e duas aberturas em inglês, com a instalação anterior isolada e a ISO original bloqueada. O save pessoal foi preservado.

**Ainda não foi testado em um Windows limpo ou em todas as GPUs.** O ensaio do instalador não revalidou teclado, corridas ou desempenho. A compatibilidade no PC de cada jogador precisa ser confirmada.

Se encontrar um problema, [abra uma issue](https://github.com/Ukinhas/sonic-riders-zero-gravity-windows/issues) com:

- Print e descrição do erro, indicando em qual etapa aconteceu.
- Versão do Windows, processador, memória, placa de vídeo e versão do driver.
- Se o jogo chegou a abrir, o trecho relevante de `%LOCALAPPDATA%\SonicRidersZeroGravity\game.log`, removendo informações pessoais antes de publicar.

## Compilação

Este repositório contém o runtime, os recompiladores e as adaptações de Windows. A árvore de código não inclui ISO, BIOS, executável recompilado, saves ou capturas de microcódigo VU; os pacotes executáveis são distribuídos separadamente nos Releases.

Veja [BUILD-WINDOWS.md](BUILD-WINDOWS.md). Mantenha os arquivos gerados fora da árvore de código versionada. O repositório não contém as 76 capturas VU usadas na build distribuída; elas precisam ser coletadas novamente a partir da sua cópia. Sem essa cobertura, o interpretador VU pode reduzir muito o desempenho.

## Desempenho e limites

A build usada no pacote completo foi medida em 22 combinações, cobrindo trechos de 16 pistas e 18 personagens, em uma RTX 4060. As médias em janelas de cinco segundos ficaram aproximadamente entre 59,5 e 60 FPS, com microtravadas isoladas. Isso **não garante 60 FPS constantes**, nem valida todas as voltas, caminhos ou combinações. As medições de apresentação representam submissão ao renderizador, não captura física do monitor.

O modo online não foi implementado. 120 FPS reais, mantendo a velocidade original, não foi concluído.

## Origem e licenças

- Base: `snuri00/ps2-web-recomp`, commit `c628ae843209f6e4cef34ec04efb6267a1618882`.
- Runtime e ferramentas: [MIT](LICENSE), preservando o crédito de Samir Nuri.
- Tabelas IPU adaptadas de Play!: [aviso e licença](runtime/src/ipu/NOTICE).
- Patch de desbloqueio adaptado do código de Skiller listado em [GameHacking.org](https://gamehacking.org/game/104986).
- Dependências externas conservam suas próprias licenças; inclua-as ao distribuir qualquer build permitida.
- Sonic Riders: Zero Gravity e seus elementos pertencem aos respectivos titulares. A licença deste código não licencia os dados do jogo.

O [README original](README-upstream.md) descreve a base genérica, inclusive outros jogos; suas afirmações não constituem testes desta adaptação.
