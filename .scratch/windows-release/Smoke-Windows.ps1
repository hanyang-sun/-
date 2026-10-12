param(
    [Parameter(Mandatory=$true)][int]$PlayerId,
    [ValidateSet('Capture','Click','Text','Clear','Wheel','Close','Info')][string]$Action='Info',
    [int]$X=0,
    [int]$Y=0,
    [string]$Value='',
    [string]$OutputPath=''
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class ReleaseWindow {
    public delegate bool EnumCallback(IntPtr window,IntPtr data);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumCallback callback,IntPtr data);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
    public static IntPtr ForProcess(int process) {
        IntPtr result=IntPtr.Zero;
        EnumWindows((window,data)=> { uint owner; GetWindowThreadProcessId(window,out owner);
            if(owner==process) { var name=new System.Text.StringBuilder(256); GetClassName(window,name,256); if(name.ToString()=="UnityWndClass") { result=window; return false; } }
            return true;
        },IntPtr.Zero);
        return result;
    }
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left,Top,Right,Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct Point { public int X,Y; }
    [StructLayout(LayoutKind.Sequential)] public struct Keyboard { public ushort Vk,Scan; public uint Flags,Time; public IntPtr Extra; }
    [StructLayout(LayoutKind.Explicit,Size=32)] public struct Union { [FieldOffset(0)] public Keyboard Keyboard; }
    [StructLayout(LayoutKind.Sequential)] public struct Input { public uint Type; public Union Data; }
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr window,int command);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int w,int h,uint flags);
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr window,System.Text.StringBuilder text,int size);
    [DllImport("user32.dll")] public static extern int GetClassName(IntPtr window,System.Text.StringBuilder text,int size);
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint from,uint to,bool attach);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] public static extern IntPtr GetThreadDesktop(uint thread);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern bool GetUserObjectInformation(IntPtr handle,int index,System.Text.StringBuilder text,int length,out int needed);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr window,out Rect rect);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr window,ref Point point);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags,uint x,uint y,int data,UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte key,byte scan,uint flags,UIntPtr extra);
    [DllImport("user32.dll")] public static extern uint SendInput(uint count,Input[] inputs,int size);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern bool PostMessage(IntPtr window,uint message,IntPtr wParam,IntPtr lParam);
    public static void Text(string text) {
        keybd_event(0x11,0,0,UIntPtr.Zero); keybd_event(0x41,0,0,UIntPtr.Zero);
        keybd_event(0x41,0,2,UIntPtr.Zero); keybd_event(0x11,0,2,UIntPtr.Zero);
        System.Threading.Thread.Sleep(100);
        foreach(char value in text) {
            var down=new Input {Type=1,Data=new Union {Keyboard=new Keyboard {Scan=value,Flags=4}}};
            var up=new Input {Type=1,Data=new Union {Keyboard=new Keyboard {Scan=value,Flags=6}}};
            if(SendInput(2,new[] {down,up},Marshal.SizeOf(typeof(Input)))!=2) throw new Exception("SendInput failed");
        }
    }
}
'@
[ReleaseWindow]::SetProcessDPIAware() | Out-Null
$playerProcess=Get-Process -Id $PlayerId
if($playerProcess.ProcessName -ne 'BrickDefense') { throw 'Only the BrickDefense smoke-test process is allowed.' }
$playerWindow=[ReleaseWindow]::ForProcess($PlayerId)
if($playerWindow -eq [IntPtr]::Zero) { throw 'Player has no window.' }
if($Action -eq 'Close') { [ReleaseWindow]::PostMessage($playerWindow,0x10,[IntPtr]::Zero,[IntPtr]::Zero) | Out-Null; exit }
if($Action -eq 'Info') {
    $owner=[uint32]0
    $ownerThread=[ReleaseWindow]::GetWindowThreadProcessId($playerWindow,[ref]$owner)
    $windowTitle=New-Object Text.StringBuilder(256); $windowClass=New-Object Text.StringBuilder(256)
    [ReleaseWindow]::GetWindowText($playerWindow,$windowTitle,256) | Out-Null
    [ReleaseWindow]::GetClassName($playerWindow,$windowClass,256) | Out-Null
    $ownerDesktop=New-Object Text.StringBuilder(256); $helperDesktop=New-Object Text.StringBuilder(256); $needed=0
    [ReleaseWindow]::GetUserObjectInformation([ReleaseWindow]::GetThreadDesktop($ownerThread),2,$ownerDesktop,512,[ref]$needed) | Out-Null
    [ReleaseWindow]::GetUserObjectInformation([ReleaseWindow]::GetThreadDesktop([ReleaseWindow]::GetCurrentThreadId()),2,$helperDesktop,512,[ref]$needed) | Out-Null
    [PSCustomObject]@{Window=$playerWindow;Title=$windowTitle.ToString();Class=$windowClass.ToString();Visible=[ReleaseWindow]::IsWindowVisible($playerWindow);OwnerDesktop=$ownerDesktop.ToString();HelperDesktop=$helperDesktop.ToString()}
    exit
}
[ReleaseWindow]::ShowWindow($playerWindow,9) | Out-Null
[ReleaseWindow]::SetWindowPos($playerWindow,[IntPtr](-1),30,30,0,0,0x41) | Out-Null
$foregroundOwner=[uint32]0
$foregroundThread=[ReleaseWindow]::GetWindowThreadProcessId([ReleaseWindow]::GetForegroundWindow(),[ref]$foregroundOwner)
$currentThread=[ReleaseWindow]::GetCurrentThreadId()
if($foregroundThread -ne $currentThread) { [ReleaseWindow]::AttachThreadInput($currentThread,$foregroundThread,$true) | Out-Null }
[ReleaseWindow]::SetForegroundWindow($playerWindow) | Out-Null
if($foregroundThread -ne $currentThread) { [ReleaseWindow]::AttachThreadInput($currentThread,$foregroundThread,$false) | Out-Null }
Start-Sleep -Milliseconds 400
$focusedOwner=[uint32]0
[ReleaseWindow]::GetWindowThreadProcessId([ReleaseWindow]::GetForegroundWindow(),[ref]$focusedOwner) | Out-Null
if(-not [ReleaseWindow]::IsWindowVisible($playerWindow)) { throw 'Player window is not visible.' }
$clientRect=New-Object ReleaseWindow+Rect
$clientPoint=New-Object ReleaseWindow+Point
[ReleaseWindow]::GetClientRect($playerWindow,[ref]$clientRect) | Out-Null
for($windowAttempt=0;($clientRect.Right -eq 0 -or $clientRect.Bottom -eq 0) -and $windowAttempt -lt 5;$windowAttempt++) {
    [ReleaseWindow]::ShowWindow($playerWindow,9) | Out-Null
    Start-Sleep -Milliseconds 200
    [ReleaseWindow]::GetClientRect($playerWindow,[ref]$clientRect) | Out-Null
}
if($clientRect.Right -eq 0 -or $clientRect.Bottom -eq 0) { throw 'Player has an empty client area.' }
[ReleaseWindow]::ClientToScreen($playerWindow,[ref]$clientPoint) | Out-Null
if($Action -eq 'Click' -or $Action -eq 'Wheel') {
    $pointerBits=[IntPtr](($Y -shl 16) -bor ($X -band 0xffff))
    [ReleaseWindow]::PostMessage($playerWindow,0x200,[IntPtr]::Zero,$pointerBits) | Out-Null
    if($Action -eq 'Click') {
        [ReleaseWindow]::PostMessage($playerWindow,0x201,[IntPtr]1,$pointerBits) | Out-Null
        Start-Sleep -Milliseconds 100
        [ReleaseWindow]::PostMessage($playerWindow,0x202,[IntPtr]::Zero,$pointerBits) | Out-Null
    } else { [ReleaseWindow]::PostMessage($playerWindow,0x20A,[IntPtr](([int]$Value) -shl 16),$pointerBits) | Out-Null }
} elseif($Action -eq 'Text') {
    foreach($textCharacter in $Value.ToCharArray()) { [ReleaseWindow]::PostMessage($playerWindow,0x102,[IntPtr]([int]$textCharacter),[IntPtr]::Zero) | Out-Null }
} elseif($Action -eq 'Clear') {
    foreach($textIndex in 1..256) { [ReleaseWindow]::PostMessage($playerWindow,0x100,[IntPtr]8,[IntPtr]1) | Out-Null; [ReleaseWindow]::PostMessage($playerWindow,0x101,[IntPtr]8,[IntPtr]1) | Out-Null }
}
elseif($Action -eq 'Capture') {
    if([string]::IsNullOrWhiteSpace($OutputPath)) { throw 'Capture needs OutputPath.' }
    $bitmap=New-Object Drawing.Bitmap($clientRect.Right,$clientRect.Bottom)
    $graphics=[Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen($clientPoint.X,$clientPoint.Y,0,0,$bitmap.Size)
        $bitmap.Save($OutputPath,[Drawing.Imaging.ImageFormat]::Png)
    } finally { $graphics.Dispose(); $bitmap.Dispose() }
}
[ReleaseWindow]::SetWindowPos($playerWindow,[IntPtr](-2),0,0,0,0,0x13) | Out-Null
[PSCustomObject]@{PlayerId=$PlayerId;Width=$clientRect.Right;Height=$clientRect.Bottom;Action=$Action;Visible=[ReleaseWindow]::IsWindowVisible($playerWindow)}
