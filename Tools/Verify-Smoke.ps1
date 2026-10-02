param([Parameter(Mandatory=$true)][string]$SessionDirectory)
$ErrorActionPreference='Stop'
$events=@(Import-Csv -LiteralPath (Join-Path $SessionDirectory 'events.csv'))
foreach ($eventType in @('capture','wrong_owner','wrong_distractor','shot_blocked','occlusion_enter','fish_reappeared','route_pause','route_resume','handoff_start','handoff_end','target_missed','target_censored','block_end')) {
    if (-not ($events | Where-Object event -EQ $eventType)) { throw "Missing expected event: $eventType" }
}
$captures=@($events | Where-Object event -EQ 'capture')
if ($captures.Count -ne 2 -or @($captures | Select-Object -ExpandProperty fish_id -Unique).Count -ne 2) { throw 'Expected two distinct captured targets' }
if (-not ($captures | Where-Object { $_.owner -eq '0' -and $_.actor -eq '1' -and $_.handoff_player -eq '1' })) { throw 'Teammate did not capture under handoff permission' }
$expiry=$events | Where-Object event -EQ 'handoff_end' | Select-Object -First 1
$grant=$events | Where-Object { $_.event -eq 'handoff_start' -and $_.fish_id -eq $expiry.fish_id } | Select-Object -First 1
$culture=[Globalization.CultureInfo]::InvariantCulture
$duration=[double]::Parse($expiry.time_s,$culture)-[double]::Parse($grant.time_s,$culture)
if ($duration -lt 3.95 -or $duration -gt 4.3) { throw "Unexpected handoff lifetime: $duration" }
foreach ($name in @('orientation.csv','samples.csv','session.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $SessionDirectory $name))) { throw "Missing $name" }
}
"PASS: capture permissions, distractor rejection, reef occlusion, visibility transitions, route pause, handoff capture and expiry, censored termination, pose and target logs"
