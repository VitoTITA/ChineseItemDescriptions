$ErrorActionPreference = 'Stop'
$gameDir = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../..')).Path
$managed = Join-Path $gameDir 'Probably Stolen_Data/Managed'
$modDir = Join-Path $gameDir 'Mods/ChineseItemDescriptions'

foreach ($name in @('UnityEngine.CoreModule', '0Harmony', 'DseCecil', 'Assembly-CSharp')) {
    [void][Reflection.Assembly]::LoadFrom((Join-Path $managed ($name + '.dll')))
}

# 在独立进程中按游戏启动时的规则登记本机游戏程序集，供原版扫描器使用。
$flags = [Reflection.BindingFlags]'Static, NonPublic'
$gameAssemblyNames = [ModLoader].GetField('gameAssemblyNames', $flags).GetValue($null)
Get-ChildItem -LiteralPath $managed -Filter '*.dll' -Recurse -File | ForEach-Object {
    [void]$gameAssemblyNames.Add($_.BaseName)
}

$result = [ModSecurityDebug]::Scan($modDir)
$result
if ($result -match 'ERROR:|UNDECLARED|HARD FLAGS|unlisted DLLs' -or $result -notmatch 'methods scanned: [1-9]\d*' -or $result -notmatch 'detected: none') {
    throw '官方扫描器未通过。'
}

$modAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modDir 'ChineseItemDescriptions.dll'))
$patch = $modAssembly.GetType('ChineseItemDescriptions.TooltipPatch')
if (!$patch.GetMethod('Prepare', $flags).Invoke($null, $null)) {
    throw '补丁未识别本机游戏的提示框方法。'
}

$target = [HarmonyLib.AccessTools]::DeclaredMethod([GameItemElement], 'GetTooltipBasic', [Type[]]@(), $null)
$getter = [HarmonyLib.AccessTools]::PropertyGetter([GameItem], 'flavorText')
$getOriginal = [HarmonyLib.PatchProcessor].GetMethod('GetOriginalInstructions', [Type[]]@([Reflection.MethodBase], [Reflection.Emit.ILGenerator]))
$original = $getOriginal.Invoke($null, @($target, $null))
$before = @($original | Where-Object { $_.operand -eq $getter })
$originalArgument = [object[]]::new(1)
$originalArgument[0] = $original
$updated = @($patch.GetMethod('Transpiler', $flags).Invoke($null, $originalArgument))
$replacement = $patch.GetMethod('GetDisplayFlavor', $flags)
$after = @($updated | Where-Object { $_.operand -eq $replacement })
if ($before.Count -ne 2 -or $after.Count -ne 2 -or $updated.Count -ne $original.Count) {
    throw '实际游戏方法中的两处描述读取未按预期替换。'
}
'通过：本机游戏方法的两处描述读取均被定向到替换逻辑。'
