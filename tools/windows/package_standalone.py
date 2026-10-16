from pathlib import Path
import ctypes as c
from ctypes import wintypes as w
import hashlib
import json
import shutil
import struct
import time
import sys

work = Path(__file__).resolve().parent
stage = work / 'standalone-stage'
destination = work.parents[1] / 'local/packages/SonicRidersZeroGravity-Completo.exe'
destination.parent.mkdir(parents=True, exist_ok=True)
temporary = destination.with_suffix('.exe.part')
if len(sys.argv) != 2:
    raise SystemExit('Usage: python package_standalone.py PATH_TO_YOUR_ISO')
iso = Path(sys.argv[1])
cab = c.WinDLL('cabinet', use_last_error=True)
cab.CreateCompressor.argtypes = [w.DWORD, c.c_void_p, c.POINTER(c.c_void_p)]
cab.CreateCompressor.restype = w.BOOL
cab.Compress.argtypes = [c.c_void_p, c.c_void_p, c.c_size_t, c.c_void_p, c.c_size_t, c.POINTER(c.c_size_t)]
cab.Compress.restype = w.BOOL
cab.CloseCompressor.argtypes = [c.c_void_p]
compressor = c.c_void_p()
if not cab.CreateCompressor(4, None, c.byref(compressor)):
    raise c.WinError(c.get_last_error())
buffer = c.create_string_buffer(2 * 1024 * 1024)
files = [(p.relative_to(stage).as_posix(), p) for p in sorted(stage.rglob('*')) if p.is_file() and p.name not in {'game.dat', 'Diagnostic.exe'}]
files.append(('data/game.dat', iso))
index = bytearray(b'SRZGIDX2' + struct.pack('<I', len(files)))
total = sum(p.stat().st_size for _, p in files)
done = 0
last = time.monotonic()
try:
    with temporary.open('wb') as out:
        with (work / 'standalone-stub.exe').open('rb') as stub:
            shutil.copyfileobj(stub, out)
        for name, source in files:
            offset = out.tell()
            digest = hashlib.sha256()
            size = source.stat().st_size
            with source.open('rb') as data:
                while block := data.read(1024 * 1024):
                    digest.update(block)
                    actual = c.c_size_t()
                    if not cab.Compress(compressor, block, len(block), buffer, len(buffer), c.byref(actual)):
                        raise c.WinError(c.get_last_error())
                    if actual.value < len(block):
                        out.write(struct.pack('<II', len(block), actual.value))
                        out.write(buffer.raw[:actual.value])
                    else:
                        out.write(struct.pack('<II', len(block), 0))
                        out.write(block)
                    done += len(block)
                    if time.monotonic() - last > 10:
                        print(f'{done / total:.0%}: {name}; packed {out.tell() / 1048576:.0f} MiB', flush=True)
                        last = time.monotonic()
            encoded = name.encode('ascii')
            index += struct.pack('<H', len(encoded)) + encoded
            index += struct.pack('<QQQ', size, offset, out.tell() - offset) + digest.digest()
        index_offset = out.tell()
        out.write(index)
        out.write(b'SRZGPK02' + struct.pack('<QQ', index_offset, len(index)) + hashlib.sha256(index).digest())
    temporary.replace(destination)
    print(f'Created {destination}: {destination.stat().st_size / 1024**3:.2f} GiB', flush=True)
finally:
    cab.CloseCompressor(compressor)
