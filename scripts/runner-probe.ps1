# Runs the runner form probe (and any other tests) in a verify copy, with graphics.
# Usage: probe_run.ps1 -Etiket before|after -Sahip T-xxx [-Filtre regex] [-MetaYollar a,b]
param([string]$Kaynak = 'D:\Pundun\speed-starts-ik',[string]$Etiket = 'before', [string]$Sahip = 'T-107', [string]$Filtre = 'RunnerFormProbeTests', [string]$MetaYollar = '')

$root = 'D:\Pundun\speed-starts'
$src = $Kaynak
$out = "D:\Pundun\form-probe\$Etiket"
$unity = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f2-x86_64\Editor\Unity.exe'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Get-ChildItem $out -File | Remove-Item -Force -Confirm:$false

$al = & powershell -NoProfile -File "$root\scripts\kopya.ps1" -Al -Sahip $Sahip -Not "runner probe $Etiket"
if ($LASTEXITCODE -ne 0) { Write-Output "AL HATASI $LASTEXITCODE"; Write-Output $al; exit 30 }
$kopya = @($al)[-1]
Write-Output "KOPYA $kopya"

try {
    & powershell -NoProfile -File "$root\scripts\kopya.ps1" -Senkron -Ad $kopya -Kaynak $src -HeadOnly -Sahip $Sahip
    if ($LASTEXITCODE -ne 0) { Write-Output "SENKRON HATASI $LASTEXITCODE"; exit 31 }
    # A prefab's import bakes the script's field defaults; a default changed with no field added or removed does not re-import it,
    # and the run measures the old number. A changed hash forces the import (the next Senkron puts the file back). The added line differs
    # every run: the same blank line each time gave the same hash as the last run, and the numbers stayed old.
    Add-Content -LiteralPath "$kopya\Assets\Speed-Staes\Prefabs\Character\HumanCharacterDummy_F.prefab" -Value (' ' * [int]((Get-Date).Ticks / 10000000 % 997 + 1))

    $unityArgs = @('-batchmode', '-projectPath', "`"$kopya`"", '-runTests', '-testPlatform', 'PlayMode',
        '-testFilter', "`"$Filtre`"", '-testResults', "`"$out\result.xml`"", '-logFile', "`"$out\unity.log`"",
        '-shotDir', "`"$out`"")
    $p = Start-Process -Wait -PassThru -FilePath $unity -ArgumentList $unityArgs
    Write-Output "UNITY CIKIS $($p.ExitCode)"
    if (Test-Path "$out\result.xml") {
        Select-String -Path "$out\result.xml" -Pattern '<test-run ' | Select-Object -First 1 | ForEach-Object { $_.Line.Substring(0, [Math]::Min(150, $_.Line.Length)) }
        Select-String -Path "$out\result.xml" -Pattern 'result="Failed"' | Where-Object { $_.Line -match '<test-case' } | ForEach-Object { if ($_.Line -match ' name="([^"]+)"') { "KIRMIZI: $($Matches[1])" } }
    }
    if (Test-Path "$out\unity.log") { Select-String -Path "$out\unity.log" -Pattern 'error CS' | Select-Object -First 12 | ForEach-Object { $_.Line } }
    if ($MetaYollar) {
        & powershell -NoProfile -File "$root\scripts\kopya.ps1" -GeriAl -Ad $kopya -Hedef $src -Yollar $MetaYollar -Onayla
        Write-Output "GERIAL CIKIS $LASTEXITCODE"
    }
}
finally {
    & powershell -NoProfile -File "$root\scripts\kopya.ps1" -Birak -Ad $kopya -Sahip $Sahip
}
