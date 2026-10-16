#!/bin/bash
# Builds the web version of a recompiled game into build/web-<game>.
# usage: scripts/build_web.sh <game folder under games/> [emsdk dir]
set -e
here=$(cd "$(dirname "$0")/.." && pwd)
game=${1:-gt4}
emsdk=${2:-${EMSDK:-$here/tools/emsdk}}
out="$here/build/web-$game"
boot=$(sed -n 's/^BOOT2 *= *//p' "$here/games/$game/disc/SYSTEM.CNF" | tr -d '\r')
elf="$here/games/$game/core.elf"
if [ ! -f "$elf" ]; then
    name=${boot#*\\}
    elf="$here/games/$game/disc/${name%;*}"
fi
mkdir -p "$out"
printf '%s\n' "$boot" > "$out/boot.txt"
source "$emsdk/emsdk_env.sh" > /dev/null 2>&1
emcmake cmake -S "$here" -B "$out" -G Ninja -DCMAKE_BUILD_TYPE=Release \
    -DPS2_GENERATED="$here/generated/$game" -DPS2_WEB_ELF="$elf" -DPS2_WEB_BOOT_FILE="$out/boot.txt"
ninja -j8 -C "$out" ps2web
gzip -kf9 "$out/ps2web.wasm" "$out/ps2web.data" "$out/ps2web.js"
printf 'built %s/ps2web.html; serve it with scripts/serve.py %q %q\n' "$out" "build/web-$game" "games/$game/disc.iso"
