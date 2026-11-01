# Targeted repository inspection and command output

Examples only; replace the project, symbols, and PR base with the actual task.

## Discover before reading

```bash
rg --files Hagalaz.Services.GameWorld -g '*.cs' | rg 'ItemContainer|Transaction'
rg -n -g '*.cs' 'TryTransferTo|MutationLock' Hagalaz.Services.GameWorld
git diff --stat main...HEAD
git diff --name-only main...HEAD
git diff -U3 main...HEAD -- path/to/changed-file.cs
```

For PR reviews, resolve the actual base from PR metadata and inventory every changed file. Read each cumulative change and relevant production callers; inspecting batches is an output-management technique, not permission to skip coverage.

## Preserve .NET diagnostics

On Windows PowerShell, capture the full command output before printing a small summary. Record the exit status **immediately after** the command, before any other native command. Example (use the real test project):

```powershell
$log = Join-Path $env:TEMP "hagalaz-test-$PID.log"
dotnet test .\Hagalaz.Services.GameWorld.Tests\Hagalaz.Services.GameWorld.Tests.csproj --verbosity quiet *> $log
$code = $LASTEXITCODE
Write-Output "Exit: $code; full log: $log"
Get-Content -Path $log -Tail 20
if ($code -ne 0) { exit $code }
```

When warnings, failures, or incomplete summaries require investigation, inspect more of the full log, including the complete relevant exception and any newly introduced warnings. Never discard failures or change test/analyzer gate behavior just to reduce model input.

For measurement, compare representative tasks using the same model/effort: total and cached input tokens (when available), output tokens, tool-call count, diagnostic output size, verification results, and rework. Do not claim savings without a baseline.
