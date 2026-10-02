param([Parameter(Mandatory=$true)][string]$SessionDirectory)
$ErrorActionPreference = 'Stop'
$folder = (Resolve-Path -LiteralPath $SessionDirectory).Path
$events = @(Import-Csv -LiteralPath (Join-Path $folder 'events.csv'))
$metadata = Get-Content -Raw -LiteralPath (Join-Path $folder 'session.json') | ConvertFrom-Json
$persistent = ($metadata.config | ConvertFrom-Json).persistentSchool -eq $true
$poses = @(Import-Csv -LiteralPath (Join-Path $folder 'orientation.csv'))
$culture = [Globalization.CultureInfo]::InvariantCulture
function Number([string]$value) { [double]::Parse($value, $culture) }
$counts = @{}
$born = @{}
$responses = [Collections.Generic.List[double]]::new()
foreach ($event in $events) {
    if (-not $counts.ContainsKey($event.event)) { $counts[$event.event] = 0 }
    $counts[$event.event]++
    if ($event.event -eq 'fish_spawn') { $born[$event.fish_id] = Number $event.time_s }
    if ($event.event -eq 'capture' -and $born.ContainsKey($event.fish_id)) {
        $responses.Add((Number $event.time_s) - $born[$event.fish_id])
    }
}
$motion = @{a=0.0; b=0.0}
$validDuration = @{a=0.0; b=0.0}
for ($i=1; $i -lt $poses.Count; $i++) {
    $before=$poses[$i-1]; $after=$poses[$i]
    $dt=(Number $after.time_s)-(Number $before.time_s)
    if ($dt -le 0 -or $dt -gt 0.2) { continue }
    foreach ($player in @('a','b')) {
        $valid='valid_'+$player; $yaw='yaw_'+$player
        if ($before.$valid -ne '1' -or $after.$valid -ne '1') { continue }
        $delta=(Number $after.$yaw)-(Number $before.$yaw)
        $delta=(($delta + 180) % 360 + 360) % 360 - 180
        $motion[$player]+=[Math]::Abs($delta)
        $validDuration[$player]+=$dt
    }
}
$captured=[int]$counts['capture']; $missed=[int]$counts['target_missed']
$rate=$null; if ($captured+$missed -gt 0) { $rate=$captured/($captured+$missed) }
$response=$null; if ($responses.Count -gt 0) { $response=($responses | Measure-Object -Average).Average }
if ($persistent) { $rate=$null; $response=$null }
$summary=[ordered]@{
    status='prototype_descriptive_only'
    persistent_school=$persistent
    capture_count=$captured
    missed_count=$missed
    resolved_target_capture_rate=$rate
    censored_event_count_including_distractors=[int]$counts['target_censored']
    mean_response_time_from_spawn_s=$response
    head_yaw_travel_degrees=$motion
    head_yaw_valid_observed_seconds=$validDuration
    event_counts=$counts
}
$json=$summary | ConvertTo-Json -Depth 5
$json | Set-Content -LiteralPath (Join-Path $folder 'summary.json') -Encoding UTF8
$json
