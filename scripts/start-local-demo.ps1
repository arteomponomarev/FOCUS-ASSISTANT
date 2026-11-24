[CmdletBinding()]
param(
    [ValidateRange(1024, 65535)][int] $ApiPort = 8080,
    [ValidateRange(1024, 65535)][int] $WebPort = 5173,
    [ValidateRange(1, 65535)][int] $DatabasePort = 5432,
    [ValidatePattern('^focus_demo_[a-z0-9_]{1,49}$')][string] $DatabaseName,
    [System.Management.Automation.PSCredential] $DatabaseCredential,
    [switch] $Check
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$apiOrigin = "http://127.0.0.1:$ApiPort"
$webOrigin = "http://127.0.0.1:$WebPort"
if (@($ApiPort, $WebPort, $DatabasePort | Select-Object -Unique).Count -ne 3) {
    throw 'The API, web, and database ports must be distinct.'
}

function Assert-FreePort([int] $Port) {
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, $Port)
    $listener.ExclusiveAddressUse = $true
    try { $listener.Start() }
    catch { throw "Port $Port on 127.0.0.1 is unavailable. Use another port or stop the existing service yourself." }
    finally { $listener.Stop() }
}

function With-Environment([hashtable] $Variables, [scriptblock] $Action) {
    $previous = @{}
    try {
        foreach ($name in $Variables.Keys) {
            $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
            [Environment]::SetEnvironmentVariable($name, $Variables[$name], 'Process')
        }
        & $Action
    } finally {
        foreach ($name in $previous.Keys) {
            [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process')
        }
    }
}

function Wait-Ready([Diagnostics.Process] $Process, [string] $Url, [string] $Log, [int] $Status = 200) {
    $deadline = [DateTime]::UtcNow.AddSeconds(25)
    do {
        if ($Process.HasExited) { throw "Process exited with code $($Process.ExitCode). See $Log" }
        try {
            if ((Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2).StatusCode -eq $Status) { return }
        } catch {
            if ([int]$_.Exception.Response.StatusCode -eq $Status) { return }
        }
        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)
    throw "Startup check failed for $Url. See $Log"
}

Assert-FreePort $ApiPort
Assert-FreePort $WebPort
$socket = [Net.Sockets.TcpClient]::new()
try { $socket.Connect('127.0.0.1', $DatabasePort) }
catch { throw "PostgreSQL is not reachable on 127.0.0.1:$DatabasePort. Start the local service and retry." }
finally { $socket.Dispose() }

$dotnet = (Get-Command dotnet.exe).Source
$node = (Get-Command node.exe).Source
$npm = (Get-Command npm.cmd).Source
if (!$DatabaseName) { $DatabaseName = 'focus_demo_' + [Guid]::NewGuid().ToString('N') }
if (!$DatabaseCredential) {
    $user = [string](Read-Host 'Local PostgreSQL user with permission to create the demo database')
    if ([string]::IsNullOrWhiteSpace($user)) { throw 'A PostgreSQL user is required.' }
    $password = Read-Host 'Local PostgreSQL password' -AsSecureString
    $DatabaseCredential = [System.Management.Automation.PSCredential]::new($user, $password)
}

$output = Join-Path $repo 'artifacts/local-demo'
$build = Join-Path $output 'dotnet'
$logs = (New-Item -ItemType Directory -Force (Join-Path $output ('run-' + [Guid]::NewGuid().ToString('N')))).FullName
$webRoot = Join-Path $repo 'Focus.Web'
$apiRoot = Join-Path $repo 'Focus.Api'
$apiDll = Join-Path $build 'bin/Focus.Api/release/Focus.Api.dll'
$vite = Join-Path $webRoot 'node_modules/vite/bin/vite.js'
$apiProcess = $null
$webProcess = $null

Push-Location $repo
try {
    & $dotnet build 'FOCUS ASSISTANT.slnx' -c Release --artifacts-path $build -p:RestoreLockedMode=true
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
    if (!(Test-Path -LiteralPath $vite)) {
        & $npm --prefix $webRoot ci
        if ($LASTEXITCODE -ne 0) { throw 'Web dependencies could not be installed.' }
    }

    $connection = [System.Data.Common.DbConnectionStringBuilder]::new()
    $connection['Host'] = '127.0.0.1'
    $connection['Port'] = $DatabasePort
    $connection['Database'] = $DatabaseName
    $connection['Username'] = [string]$DatabaseCredential.UserName
    $connection['Password'] = $DatabaseCredential.GetNetworkCredential().Password
    $apiEnvironment = @{
        ASPNETCORE_ENVIRONMENT = 'Development'
        DOTNET_ENVIRONMENT = 'Development'
        ConnectionStrings__PostgreSQL = $connection.ConnectionString
        Observability__ExportOtlp = 'false'
    }
    $connection.Clear()
    Write-Host "Migrating disposable database $DatabaseName."
    try {
        $apiProcess = With-Environment $apiEnvironment {
            & $dotnet $apiDll --migrate *> (Join-Path $logs 'migration.log')
            if ($LASTEXITCODE -ne 0) { throw "Demo migration failed. See $logs\migration.log" }
            $arguments = '"{0}" --urls {1} --Authentication:KeyDirectory=' -f $apiDll, $apiOrigin
            Start-Process -FilePath $dotnet -ArgumentList $arguments -WorkingDirectory $apiRoot `
                -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logs 'api.log') `
                -RedirectStandardError (Join-Path $logs 'api-error.log')
        }
    } finally { $apiEnvironment.Clear() }
    Wait-Ready $apiProcess "$apiOrigin/health/ready" (Join-Path $logs 'api.log')

    $webProcess = With-Environment @{ FOCUS_API_ORIGIN = $apiOrigin; VITE_API_BASE_URL = '/' } {
        $arguments = '"{0}" --host 127.0.0.1 --port {1} --strictPort' -f $vite, $WebPort
        Start-Process -FilePath $node -ArgumentList $arguments -WorkingDirectory $webRoot `
            -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logs 'web.log') `
            -RedirectStandardError (Join-Path $logs 'web-error.log')
    }
    Wait-Ready $webProcess $webOrigin (Join-Path $logs 'web.log')
    Wait-Ready $webProcess "$webOrigin/api/v1/auth/me" (Join-Path $logs 'web.log') 401
    Write-Host "Web: $webOrigin | API: $apiOrigin | Swagger UI: $apiOrigin/api/docs"
    Write-Host "Disposable database: $DatabaseName | Logs: $logs"
    Write-Host 'Pair the Agent after creating a session. The launcher never starts capture or the Avatar.'
    if ($Check) { return }

    Write-Host 'Press Ctrl+C here to stop the API and web processes started by this script.'
    while (!$apiProcess.HasExited -and !$webProcess.HasExited) { Start-Sleep -Milliseconds 500 }
    throw "A demo process stopped unexpectedly. See $logs"
} finally {
    foreach ($process in $webProcess, $apiProcess) {
        if ($null -ne $process) {
            if (!$process.HasExited) { $process.Kill(); $process.WaitForExit() }
            $process.Dispose()
        }
    }
    Pop-Location
}
