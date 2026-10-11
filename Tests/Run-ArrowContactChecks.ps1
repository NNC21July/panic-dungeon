param(
    [string]$UnityEditor = 'D:\Unity Hub\Editor\6000.5.3f1\Editor'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root 'Temp/ArrowFeatureChecks'
New-Item -ItemType Directory -Path $output -Force | Out-Null

$sdk = (& dotnet --list-sdks | Select-Object -Last 1)
if ($sdk -notmatch '^(\S+) \[(.+)\]$') {
    throw 'A .NET 8 or newer SDK is required.'
}
$compiler = Join-Path $Matches[2] ($Matches[1] + '/Roslyn/bincore/csc.dll')
$engine = Join-Path $UnityEditor 'Data/Managed/UnityEngine'
$reference = Join-Path $UnityEditor 'Data/NetStandard/ref/2.1.0/netstandard.dll'
$core = Join-Path $engine 'UnityEngine.CoreModule.dll'
$fusion = Join-Path $root 'Assets/Photon/Fusion/Assemblies'
$assembly = Join-Path $output 'ArrowContactChecks.dll'

& dotnet $compiler -nologo -target:exe -langversion:9.0 -nostdlib `
    "-reference:$reference" "-reference:$core" "-reference:$fusion/Fusion.Runtime.dll" "-reference:$fusion/Fusion.Common.dll" "-out:$assembly" `
    (Join-Path $root 'Assets/Scripts/Combat/PolygonContactDetector.cs') `
    (Join-Path $root 'Assets/Scripts/Combat/ContactHistory.cs') `
    (Join-Path $root 'Assets/Scripts/Combat/DamageInfo.cs') `
    (Join-Path $root 'Assets/Scripts/Combat/IDamageable.cs') `
    (Join-Path $root 'Assets/Scripts/Enemies/ZombieRules.cs') `
    (Join-Path $root 'Assets/Scripts/Enemies/EnemySpawnRules.cs') `
    (Join-Path $root 'Assets/Scripts/Multiplayer/CharacterColors.cs') `
    (Join-Path $root 'Assets/Scripts/Traps/StatusEffects/StatusEffect.cs') `
    (Join-Path $root 'Assets/Scripts/Traps/StatusEffects/PoisonEffect.cs') `
    (Join-Path $root 'Assets/Scripts/Traps/StatusEffects/StatusEffectTimer.cs') `
    (Join-Path $root 'Assets/Scripts/Multiplayer/ContactRenderSample.cs') `
    (Join-Path $root 'Assets/Scripts/Multiplayer/PlayerContactSample.cs') `
    (Join-Path $root 'Assets/Scripts/Multiplayer/NetworkSpikeState.cs') `
    (Join-Path $root 'Assets/Scripts/Multiplayer/TrapContactPreview.cs') `
    (Join-Path $root 'Assets/Scripts/Multiplayer/SpikeObserverHold.cs') `
    (Join-Path $PSScriptRoot 'SpikeContactChecks.cs') `
    (Join-Path $PSScriptRoot 'TrapContactPreviewChecks.cs') `
    (Join-Path $PSScriptRoot 'PoisonEffectChecks.cs') `
    (Join-Path $PSScriptRoot 'EnemyRulesChecks.cs') `
    (Join-Path $PSScriptRoot 'CharacterColorChecks.cs') `
    (Join-Path $PSScriptRoot 'ArrowContactChecks.cs')
if ($LASTEXITCODE -ne 0) {
    throw 'Contact checks failed to compile.'
}

& dotnet exec --runtimeconfig (Join-Path $PSScriptRoot 'ArrowContactChecks.runtimeconfig.json') $assembly $engine $fusion
if ($LASTEXITCODE -ne 0) {
    throw 'Contact checks failed.'
}
