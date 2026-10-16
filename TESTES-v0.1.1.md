# Testes da versão Windows v0.1.1

Data: 16/10/2026. Máquina: PC de desenvolvimento com NVIDIA RTX 4060, driver 610.88.

## Método

- O ZIP v0.1.0 foi baixado do GitHub, seu SHA-256 conferido e seu importador aberto com perfil isolado.
- O ZIP final v0.1.1 foi extraído em outra pasta. O próprio `Jogar-Sonic.exe` desse ZIP realizou a importação completa em outro perfil vazio, sem cartão, instalação ou configuração anteriores.
- O seletor de arquivos apresentou limitações na automação entre monitores. A importação final foi iniciada pela opção `--import`, que chama o mesmo fluxo do botão Jogar; isso não representa validação automatizada completa do diálogo do Windows.
- O usuário confirmou que K avançou o aviso inicial no teclado real. A sequência posterior de menus usou entradas programadas do runtime e capturas de frames. Não foi feita uma nova verificação física de todas as teclas.
- As capturas e os estados intermediários servem para verificar telas e saves, não para medir FPS.

## Resultados

- Importação completa de 4.698.734.592 bytes; SHA-256 da instalação idêntico ao da ISO de origem.
- ZIP final iniciou o runtime com 173 programas VU nativos e apresentou o aviso de autosave em inglês com cartão novo.
- Reabertura automática do ZIP final com caminho de ISO inválido nas configurações e ISO original bloqueada exclusivamente para leitura: o jogo abriu usando `game.dat` da instalação local.
- Criação de nome de jogador, duas confirmações de formatação do cartão virtual, criação dos dados do jogo e acesso ao menu principal e aos modos de jogo.
- Cartão virtual de teste criado com 8.650.752 bytes. Nenhum save de terceiros foi incluído na Release.
- Boot novo, sem carregar estado de execução, usando uma cópia desse cartão: o perfil criado apareceu corretamente na seleção de saves.
- Teste contínuo, sem carregar estados intermediários, com o runtime extraído do ZIP final: carregamento do save, seleção de Sonic/Megalo Station e trecho inicial da corrida com cronômetro e movimento. Não representa uma volta completa nem um levantamento de desempenho.
- O SHA-256 do save pessoal existente permaneceu inalterado durante os testes.
- Dez testes automatizados passaram no executável extraído do ZIP: ELF compatível, rejeição de ISO truncada, cabeçalho inválido, perfil vazio, caminho de instalação malformado, instalação ausente, cancelamento, remoção da cópia cancelada, preservação dos saves e gravação atômica de configuração.
- Na interface do ZIP final, clicar em Jogar sem ISO apresentou a mensagem específica pedindo para selecionar o arquivo.

## Correções e mudança de funcionamento

O primeiro pacote dependia do caminho da ISO a cada execução e exibia uma mensagem genérica ao tentar iniciar sem selecioná-la. A v0.1.1 importa os dados uma vez, mostra progresso, permite cancelar, verifica a cópia com SHA-256 e só registra a instalação quando termina. Depois, o EXE inicia o jogo usando os dados instalados. Instalações incompletas ou com executável inválido são rejeitadas.

## Limites

Perfil vazio neste PC não equivale a uma instalação limpa do Windows, a outra GPU ou ao PC do destinatário. A ISO é necessária na primeira importação; os dados do jogo continuam necessários depois, armazenados localmente. Não foi refeito o levantamento de todas as pistas/personagens nesta rodada, nem demonstrados 60 FPS constantes. Permanecem as limitações de desempenho documentadas no README.
