param([Parameter(Mandatory = $true)][uri] $BaseUri)

$ErrorActionPreference = 'Stop'
if (-not $env:FOCUS_DEMO_PASSWORD -or $env:FOCUS_DEMO_PASSWORD.Length -lt 12) {
    throw 'Set FOCUS_DEMO_PASSWORD to a synthetic password of at least 12 characters.'
}

$root = $BaseUri.AbsoluteUri.TrimEnd('/')
$email = 'demo-' + [guid]::NewGuid().ToString('N') + '@example.invalid'
$headers = @{}
function Invoke-FocusApi([string] $Method, [string] $Path, $Payload = $null) {
    $request = @{ Method = $Method; Uri = "$root/api/v1$Path"; Headers = $headers }
    if ($null -ne $Payload) {
        $request.ContentType = 'application/json'
        $request.Body = $Payload | ConvertTo-Json -Depth 8 -Compress
    }
    Invoke-RestMethod @request
}

Invoke-FocusApi POST '/auth/register' @{ email = $email; password = $env:FOCUS_DEMO_PASSWORD; displayName = 'Demo' } | Out-Null
$tokens = Invoke-FocusApi POST '/auth/login' @{ email = $email; password = $env:FOCUS_DEMO_PASSWORD }
$headers.Authorization = "Bearer $($tokens.accessToken)"
Invoke-FocusApi PATCH '/settings/privacy' @{ telemetryEnabled = $true } | Out-Null
$session = Invoke-FocusApi POST '/sessions' @{ plannedDurationSeconds = 1200 }
$path = "/sessions/$($session.id)"
$goal = Invoke-FocusApi POST "$path/goals" @{ description = 'Outline the methods section' }
$session = Invoke-FocusApi POST "$path/start"
$start = [datetimeoffset]::Parse($session.startedAt.ToString())
$item = @{
    clientEventId = [guid]::NewGuid().ToString()
    startedAt = $start.AddMilliseconds(200).ToString('o')
    endedAt = $start.AddSeconds(2).ToString('o')
    activeProcessName = 'editor'
    keyboardEventCount = 3
    mouseClickCount = 1
    windowSwitchCount = 0
    idleSeconds = 0
}
Start-Sleep -Seconds 3
$accepted = Invoke-FocusApi POST "$path/activity/batch" @{ items = @($item) }
$duplicate = Invoke-FocusApi POST "$path/activity/batch" @{ items = @($item) }
if ($accepted.created -ne 1 -or $duplicate.duplicates -ne 1) {
    throw 'Expected one accepted record and one duplicate response.'
}

$completed = Invoke-FocusApi POST "$path/complete" @{
    selfReportedConcentration = 4
    selfReportedProductivity = 3
    goalOutcomes = @(@{ goalId = $goal.id; status = 'Completed' })
}
$history = Invoke-FocusApi GET '/sessions'
$timeline = Invoke-FocusApi GET "$path/timeline"
$states = Invoke-FocusApi GET "$path/focus-states"
$summary = Invoke-FocusApi GET '/analytics/summary'
Invoke-FocusApi POST '/auth/logout' | Out-Null

[pscustomobject]@{
    SessionId = $completed.id
    SessionStatus = $completed.status
    HistoryCount = $history.totalCount
    TimelineEvents = $timeline.events.totalCount
    EstimatedWindows = $states.totalCount
    AnalyticsSessions = $summary.metrics.sessions
}
