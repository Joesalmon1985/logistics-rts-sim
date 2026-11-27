# Run: powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1
# Unity-free compile check of Assets/God/Game, Editor and all test assemblies (no editor needed).
# Run before any Unity open/test run; it must show 0 errors. Needs the installed Unity editor DLLs
# (override: -UnityManagedDir <Editor>\Data\Managed) and a warm Library from any worktree.
param([string]$UnityManagedDir = "", [string]$UnityLibraryDir = "")
$root = Split-Path -Parent $PSScriptRoot
$args2 = @("build", (Join-Path $root "DotNet/God.GameCompile"), "-c", "Release", "-nologo", "-v", "q", "-clp:NoSummary")
if ($UnityManagedDir) { $args2 += "-p:UnityManagedDir=$UnityManagedDir" }
if ($UnityLibraryDir) { $args2 += "-p:UnityLibraryDir=$UnityLibraryDir" }
$out = & dotnet @args2 2>&1 | Out-String -Stream
$code = $LASTEXITCODE
$errs = $out | Where-Object { $_ -cmatch "\berror [A-Z]+[0-9]+:" } | Sort-Object -Unique
$skip = $out | Where-Object { $_ -match "GameCompile skipped" } | Select-Object -First 1
if ($skip) { Write-Host $skip }
$errs | ForEach-Object { Write-Host $_ }
Write-Host ("{0} error(s)" -f @($errs).Count)
if ($code -ne 0 -and @($errs).Count -eq 0) { $out | ForEach-Object { Write-Host $_ } }
exit $code
