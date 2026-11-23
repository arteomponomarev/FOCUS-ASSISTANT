param(
    [Parameter(Mandatory = $true)][Guid]$SessionId,
    [string]$Player = 'artifacts/avatar-win64/FocusAvatar.exe',
    [string]$Screenshot = 'artifacts/avatar-window.png',
    [ValidateRange(100, 150)][int]$ClientPercent = 100,
    [string]$SequenceDirectory
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class AvatarWindowSmoke
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    public struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)]
    public struct Monitor { public int Size; public Rect Bounds, Work; public uint Flags; }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    public static extern IntPtr ExStyle(IntPtr window, int index);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] public static extern bool GetLayeredWindowAttributes(IntPtr window, out uint color, out byte alpha, out uint flags);
    [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] public static extern bool GetMonitorInfo(IntPtr monitor, ref Monitor info);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(Point point);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string className, string name);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr window, IntPtr target, uint flags);
}
'@

function Wait-Until([scriptblock]$Condition, [string]$Label) {
    $deadline = [Datetime]::UtcNow.AddSeconds(12)
    do {
        if (& $Condition) { return }
        Start-Sleep -Milliseconds 100
    } while ([Datetime]::UtcNow -lt $deadline)
    throw "Timed out: $Label"
}

function Send-WindowMessage([uint32]$Message, [int]$WParam = 0, [int]$LParam = 0) {
    $target = $window
    if ($Message -eq 0x0312) {
        $target = [AvatarWindowSmoke]::FindWindowEx([IntPtr](-3), [IntPtr]::Zero, 'Static', "Focus Assistant Hotkey $($process.Id)")
    }
    if ($target -eq [IntPtr]::Zero -or -not [AvatarWindowSmoke]::PostMessage($target, $Message, [IntPtr]$WParam, [IntPtr]$LParam)) {
        throw "Could not send test message $Message"
    }
}

function Save-WindowImage($Bounds, [string]$Path = $Screenshot) {
    # PrintWindow targets only the test Player. The desktop and other windows are never captured.
    $bitmap = [Drawing.Bitmap]::new($Bounds.Right - $Bounds.Left, $Bounds.Bottom - $Bounds.Top)
    try {
        $graphics = [Drawing.Graphics]::FromImage($bitmap)
        try {
            $dc = $graphics.GetHdc()
            try {
                if (-not [AvatarWindowSmoke]::PrintWindow($window, $dc, 2)) {
                    throw 'Screenshot unavailable: PrintWindow failed.'
                }
            } finally { $graphics.ReleaseHdc($dc) }
        } finally { $graphics.Dispose() }

        $palette = @{}
        for ($y = 0; $y -lt $bitmap.Height; $y += 10) {
            for ($x = 0; $x -lt $bitmap.Width; $x += 10) {
                $palette[$bitmap.GetPixel($x, $y).ToArgb()] = $true
            }
        }
        if ($palette.Count -lt 12) {
            throw 'Screenshot unavailable: Windows did not render the Player into PrintWindow.'
        }
        $bitmap.Save([IO.Path]::GetFullPath($Path), [Drawing.Imaging.ImageFormat]::Png)
        Write-Output "Window screenshot saved ($($bitmap.Width)x$($bitmap.Height))."
    } finally { $bitmap.Dispose() }
}

$null = [AvatarWindowSmoke]::SetThreadDpiAwarenessContext([IntPtr](-4))
$playerPath = (Resolve-Path -LiteralPath $Player).Path
$logPath = [IO.Path]::ChangeExtension([IO.Path]::GetFullPath($Screenshot), '.log')
$width = [int](400 * $ClientPercent / 100)
$height = [int](478 * $ClientPercent / 100)
$arguments = '-focus-session {0} -logFile "{1}" -screen-width {2} -screen-height {3}' -f $SessionId.ToString('D'), $logPath, $width, $height
$process = Start-Process -FilePath $playerPath -ArgumentList $arguments -PassThru
try {
    # Cache a process handle so Windows PowerShell can read the exit code after the window closes.
    $null = $process.Handle
    Wait-Until {
        $process.Refresh()
        $process.MainWindowHandle -ne [IntPtr]::Zero
    } 'Player window'
    $window = $process.MainWindowHandle
    Wait-Until { ([AvatarWindowSmoke]::ExStyle($window, -20).ToInt64() -band 0x00080008) -eq 0x00080008 } 'layered topmost window'
    Write-Output "Window DPI: $([AvatarWindowSmoke]::GetDpiForWindow($window)); requested client scale: $ClientPercent%."

    [uint32]$color = 0
    [byte]$alpha = 0
    [uint32]$flags = 0
    if (-not [AvatarWindowSmoke]::GetLayeredWindowAttributes($window, [ref]$color, [ref]$alpha, [ref]$flags) -or
        $color -ne 0x00ff00ff -or ($flags -band 1) -ne 1) { throw 'Player transparency color key is unavailable' }

    $monitor = New-Object AvatarWindowSmoke+Monitor
    $monitor.Size = [Runtime.InteropServices.Marshal]::SizeOf($monitor)
    if (-not [AvatarWindowSmoke]::GetMonitorInfo([AvatarWindowSmoke]::MonitorFromWindow($window, 2), [ref]$monitor)) {
        throw 'Could not retrieve Windows work area'
    }
    $bounds = New-Object AvatarWindowSmoke+Rect
    $positionDeadline = [Datetime]::UtcNow.AddSeconds(4)
    do {
        if (-not [AvatarWindowSmoke]::GetWindowRect($window, [ref]$bounds)) { throw 'Could not get Player bounds' }
        if ($bounds.Right -eq ($monitor.Work.Right - 24) -and $bounds.Bottom -eq ($monitor.Work.Bottom - 24)) { break }
        Start-Sleep -Milliseconds 100
    } while ([Datetime]::UtcNow -lt $positionDeadline)
    if ($bounds.Right -ne ($monitor.Work.Right - 24) -or $bounds.Bottom -ne ($monitor.Work.Bottom - 24)) {
        throw 'Player was not positioned within the monitor work area'
    }
    Start-Sleep -Milliseconds 1800
    Save-WindowImage $bounds
    if ($SequenceDirectory) {
        $cue = Join-Path $SequenceDirectory 'cue.txt'
        $seen = Join-Path $SequenceDirectory 'seen.txt'
        foreach ($state in @('Neutral', 'Focused', 'Distracted', 'Break', 'Absent', 'Idle', 'Tired', 'Celebrating', 'Disconnected')) {
            [IO.File]::WriteAllText($cue, $state)
            Wait-Until { (Test-Path -LiteralPath $seen) -and [IO.File]::ReadAllText($seen) -eq $state } "visual cue: $state"
            if ($state -eq 'Tired') {
                # The application only shows Tired after sustained Idle snapshots.
                Start-Sleep -Seconds 30
            }
            Start-Sleep -Milliseconds 580
            Save-WindowImage $bounds (Join-Path $SequenceDirectory "$state.png")
            if ($state -eq 'Celebrating') {
                for ($frame = 0; $frame -lt 6; $frame++) {
                    Start-Sleep -Milliseconds 120
                    Save-WindowImage $bounds (Join-Path $SequenceDirectory ('celebration-{0:00}.png' -f $frame))
                }
            }
        }
        Send-WindowMessage 0x0010
        if (-not $process.WaitForExit(7000) -or $process.ExitCode -ne 0) { throw 'Player did not exit cleanly after state captures' }
        Write-Output 'PASS: captured all nine animation states.'
        return
    }

    $inside = New-Object AvatarWindowSmoke+Point
    $inside.X = $bounds.Left + [int](($bounds.Right - $bounds.Left) * .25)
    $inside.Y = $bounds.Top + [int](($bounds.Bottom - $bounds.Top) * .23)
    Wait-Until { [AvatarWindowSmoke]::WindowFromPoint($inside) -eq $window } 'interactive panel hit test'
    $outside = New-Object AvatarWindowSmoke+Point
    $outside.X = $bounds.Left + 2
    $outside.Y = $bounds.Top + 2
    if ([AvatarWindowSmoke]::WindowFromPoint($outside) -eq $window) { throw 'Transparent pixels block desktop hits' }

    Send-WindowMessage 0x0312 9021
    Wait-Until { ([AvatarWindowSmoke]::ExStyle($window, -20).ToInt64() -band 0x08000020) -eq 0x08000020 } 'passive click-through'
    if (-not [AvatarWindowSmoke]::IsWindowVisible($window)) { throw 'The paired visible companion did not stay on screen' }
    $buddy = New-Object AvatarWindowSmoke+Point
    $buddy.X = $bounds.Left + [int](($bounds.Right - $bounds.Left) * .79)
    $buddy.Y = $bounds.Top + [int](($bounds.Bottom - $bounds.Top) * .79)
    if ([AvatarWindowSmoke]::WindowFromPoint($buddy) -eq $window) { throw 'Passive companion intercepts mouse hit tests' }

    Send-WindowMessage 0x0312 9021
    Wait-Until { ([AvatarWindowSmoke]::ExStyle($window, -20).ToInt64() -band 0x08000020) -eq 0 } 'return to interactive mode'
    Wait-Until { [AvatarWindowSmoke]::WindowFromPoint($inside) -eq $window } 'interactive panel hit test after redraw'

    Send-WindowMessage 0x0010
    if (-not $process.WaitForExit(7000)) { throw 'Player did not exit after WM_CLOSE' }
    if ($process.ExitCode -ne 0) { throw "Player exited with code [$($process.ExitCode)]" }
    Write-Output 'PASS: layered transparency, work-area positioning, click-through, toggle recovery, and clean exit.'
} finally {
    if (-not $process.HasExited) {
        $null = $process.CloseMainWindow()
        if (-not $process.WaitForExit(3000)) { $process.Kill() }
    }
    $process.Dispose()
}
