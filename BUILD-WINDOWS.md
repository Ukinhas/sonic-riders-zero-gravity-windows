# Compilar para Windows

Este roteiro adapta os comandos da build local para caminhos configuráveis. O roteiro público ainda não foi executado do início ao fim em uma máquina limpa.

## Requisitos

Ambiente Linux/WSL com CMake, Ninja, Python 3 e Clang 19 com biblioteca C++ que suporte `std::format`. Para cross-compilation, use llvm-mingw com destino `x86_64-w64-mingw32`, SDL2 para MinGW e wgpu-native para Windows GNU.

Versões usadas na build privada: llvm-mingw 20261006 UCRT; SDL2 2.32.10; wgpu-native 29.0.1.1. Obtenha-as dos projetos oficiais:

- https://github.com/mstorsjo/llvm-mingw
- https://github.com/libsdl-org/SDL
- https://github.com/gfx-rs/wgpu-native

Extraia as dependências localmente. Os caminhos abaixo são exemplos a substituir.

## Extrair e recompilar o executável local

Na raiz deste repositório, em Bash:

```bash
export SONIC_ISO='/caminho/para/sua-copia.iso'
python3 scripts/disc.py extract "$SONIC_ISO" local/disc SLUS_216.42
sha256sum local/disc/SLUS_216.42
```

O ELF suportado tem SHA-256 `43ff74931c70111c7332875653aa47a3db62a0c513e71c8cf48d1d0649de16ee`. Outras versões não foram validadas.

```bash
cmake -S . -B build/host -G Ninja -DCMAKE_BUILD_TYPE=Release -DCMAKE_CXX_COMPILER=clang++-19
cmake --build build/host --target eerecomp vurecomp --parallel 8
build/host/eerecomp local/disc/SLUS_216.42 --entries games/sonic-zero-gravity/entries.txt --out generated/sonic-zero-gravity
mkdir -p local/vu
build/host/vurecomp local/vu generated/sonic-zero-gravity
```

A primeira execução de `vurecomp` cria uma tabela vazia; microprogramas ainda não capturados usam o interpretador.

## Compilar o runtime Windows

```bash
export LLVM_MINGW_ROOT='/caminho/llvm-mingw'
export WGPU_ROOT='/caminho/wgpu-windows-gnu'
export SDL_CMAKE_DIR='/caminho/SDL2/x86_64-w64-mingw32/lib/cmake/SDL2'
cmake -S . -B build/windows -G Ninja \
  -DCMAKE_TOOLCHAIN_FILE="$PWD/tools/windows/toolchain.cmake" \
  -DCMAKE_BUILD_TYPE=Release -DPS2_STANDALONE=OFF \
  -DPS2_GENERATED="$PWD/generated/sonic-zero-gravity" \
  -DPS2_WGPU_DIR="$WGPU_ROOT" -DSDL2_DIR="$SDL_CMAKE_DIR"
cmake --build build/windows --target ps2run --parallel 8
```

O diretório wgpu precisa conter `include/`, `lib/libwgpu_native.a` e `lib/libwgpu_native.dll.a`, conforme a detecção atual do CMake. Não crie arquivos vazios para contornar dependências ausentes.

Prepare uma pasta local `local/app/` com:

- `SonicRidersZeroGravity.exe`, produzido em `build/windows/`;
- `SDL2.dll`, a DLL de wgpu-native e DLLs de runtime exigidas pela toolchain;
- `data/SLUS_216.42`, extraído da sua ISO;
- licenças das dependências usadas.

Ao abrir o aplicativo no Windows, selecione sua ISO. O caminho fica lembrado. Saves e logs ficam em `%LOCALAPPDATA%/SonicRidersZeroGravity/`; progresso existente não é substituído. O cartão inicial é opcional e não é distribuído aqui.

## Capturar microprogramas VU e melhorar desempenho

Em PowerShell, configure um diretório absoluto local e inicie sua build:

```powershell
$env:PS2_VU_RECORD = 'C:\caminho\repositorio\local\vu'
& 'C:\caminho\repositorio\local\app\SonicRidersZeroGravity.exe'
```

Jogue as pistas/personagens desejados e feche o jogo. No Bash/WSL, execute novamente `build/host/vurecomp local/vu generated/sonic-zero-gravity` e recompile o alvo `ps2run`. Copie o novo EXE para a pasta local do aplicativo. As capturas são dados extraídos do jogo; não as envie ao repositório. Essa coleta não garante cobertura de todas as cenas.

## Empacotamento local em um único EXE

`tools/windows/standalone.cpp` e `package_standalone.py` preservam o empacotador da versão privada. Para usá-lo, compile o runtime com `PS2_STANDALONE=ON`, prepare `tools/windows/standalone-stage/` com a estrutura do aplicativo descrita acima e compile o stub, a partir da raiz, no WSL:

```bash
"$LLVM_MINGW_ROOT/bin/x86_64-w64-mingw32-windres" -I host host/windows.rc -O coff -o local/windows.res
"$LLVM_MINGW_ROOT/bin/x86_64-w64-mingw32-clang++" tools/windows/standalone.cpp local/windows.res \
  -O2 -static -mwindows -municode -fno-exceptions -fno-rtti \
  -lcabinet -lbcrypt -lshell32 -lole32 -luuid -o tools/windows/standalone-stub.exe
```

No Windows, execute `python tools/windows/package_standalone.py "C:\caminho\sua-copia.iso"`. O pacote resultante fica em `local/packages/`. O EXE incorpora os dados fornecidos e os extrai na primeira abertura. Não o trate como um binário sem conteúdo do jogo nem o publique automaticamente.
