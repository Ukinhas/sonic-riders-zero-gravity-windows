param(
    [Parameter(Mandatory=$true)][string]$RuntimeDirectory,
    [string]$Version = 'v0.1.0'
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$runtime = (Resolve-Path -LiteralPath $RuntimeDirectory).Path
$out = Join-Path $repo "local/releases/$Version"
if (Test-Path -LiteralPath $out) { throw "Output already exists: $out" }
$package = Join-Path $out 'SonicRidersZeroGravity-Windows'
New-Item -ItemType Directory -Path "$package/engine", "$package/licenses" -Force | Out-Null
$native = Join-Path $runtime 'SonicRidersZeroGravity.exe'
$expected = '173e33b27fb2692f3fbdb98728cd6a3aef26916c7530921e37d2ad7db1e60db3'
if ((Get-FileHash -LiteralPath $native -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expected) { throw 'Native runtime does not match the documented optimized build.' }
foreach ($name in @('SonicRidersZeroGravity.exe','SDL2.dll','wgpu_native.dll','libc++.dll','libunwind.dll','libwinpthread-1.dll')) {
    Copy-Item -LiteralPath (Join-Path $runtime $name) -Destination "$package/engine/$name"
}
Copy-Item -Path "$runtime/licenses/*.txt" -Destination "$package/licenses"
Copy-Item -LiteralPath "$repo/LICENSE" -Destination "$package/LICENSE.txt"
Copy-Item -LiteralPath "$PSScriptRoot/LEIA-ME-Release.txt" -Destination "$package/LEIA-ME.txt"
$csc = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$launcherOutput = [IO.Path]::GetFullPath((Join-Path $package 'Jogar-Sonic.exe'))
$launcherSource = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'IsoLauncher.cs'))
& $csc /nologo /codepage:65001 /target:winexe /platform:x64 /optimize+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/out:$launcherOutput" $launcherSource
if ($LASTEXITCODE -ne 0) { throw 'Launcher compilation failed.' }
Copy-Item -LiteralPath "$PSScriptRoot/IsoLauncher.exe.config" -Destination "$package/Jogar-Sonic.exe.config"
$manifest = Get-ChildItem -LiteralPath $package -Recurse -File | ForEach-Object {
    [pscustomobject]@{ path = $_.FullName.Substring($package.Length + 1).Replace('\','/'); bytes = $_.Length; sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
}
$manifest | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath "$package/files.json" -Encoding utf8
$zip = Join-Path $out "SonicRidersZeroGravity-Windows-$Version.zip"
Compress-Archive -LiteralPath $package -DestinationPath $zip -CompressionLevel Optimal
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($zip))" | Set-Content -LiteralPath (Join-Path $out 'SHA256SUMS.txt') -Encoding ascii
Get-Item -LiteralPath $zip | Select-Object FullName, Length
