param(
    [Parameter(Mandatory=$true)][int]$PlayerId,
    [ValidateSet('Dialog','NoDialog')][string]$Expected='Dialog',
    [string]$SelectPath='',
    [switch]$Cancel,
    [string]$Screenshot=''
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class PhotoPickerProbe {
    public delegate bool Callback(IntPtr window,IntPtr data);
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left,Top,Right,Bottom; }
    [DllImport("user32.dll")] public static extern bool EnumWindows(Callback cb,IntPtr data);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window,out uint owner);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr window,StringBuilder name,int size);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr window,StringBuilder name,int size);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern IntPtr SendMessage(IntPtr window,uint message,IntPtr wParam,string text);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern bool PostMessage(IntPtr window,uint message,IntPtr wParam,IntPtr lParam);
    [DllImport("user32.dll")] public static extern IntPtr GetDlgItem(IntPtr dialog,int control);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window,out Rect rect);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    public static IntPtr Dialog(int player) {
        IntPtr found=IntPtr.Zero;
        EnumWindows((window,data)=> { uint owner; GetWindowThreadProcessId(window,out owner);
            var name=new StringBuilder(256); GetClassName(window,name,256);
            if(owner==player && name.ToString()=="#32770" && IsWindowVisible(window)) { found=window; return false; }
            return true;
        },IntPtr.Zero);
        return found;
    }
}
'@
[PhotoPickerProbe]::SetProcessDPIAware() | Out-Null
if((Get-Process -Id $PlayerId).ProcessName -ne 'BrickDefense') { throw 'Only the test Player can be probed.' }
$dialogHandle=[IntPtr]::Zero
for($dialogAttempt=0;$dialogAttempt -lt 20;$dialogAttempt++) {
    $dialogHandle=[PhotoPickerProbe]::Dialog($PlayerId)
    if($dialogHandle -ne [IntPtr]::Zero) { break }
    Start-Sleep -Milliseconds 100
}
if($Expected -eq 'Dialog' -and $dialogHandle -eq [IntPtr]::Zero) { throw 'EXPECTED_FILE_DIALOG_MISSING: Import clicked, no native file picker opened.' }
if($Expected -eq 'NoDialog' -and $dialogHandle -ne [IntPtr]::Zero) { throw 'UNEXPECTED_FILE_DIALOG_STILL_OPEN' }
if($dialogHandle -eq [IntPtr]::Zero) { 'PHOTO_PICKER_NO_DIALOG_PASSED'; exit }
$dialogTitle=New-Object Text.StringBuilder(256)
[PhotoPickerProbe]::GetWindowText($dialogHandle,$dialogTitle,256) | Out-Null
Write-Output ('PHOTO_PICKER_DIALOG_PASSED: '+$dialogTitle.ToString())
if($Screenshot) {
    [PhotoPickerProbe]::SetWindowPos($dialogHandle,[IntPtr](-1),60,60,0,0,0x41) | Out-Null
    Start-Sleep -Milliseconds 200
    $dialogRect=New-Object PhotoPickerProbe+Rect
    [PhotoPickerProbe]::GetWindowRect($dialogHandle,[ref]$dialogRect) | Out-Null
    $imageWidth=$dialogRect.Right-$dialogRect.Left; $imageHeight=$dialogRect.Bottom-$dialogRect.Top
    $bitmap=New-Object Drawing.Bitmap($imageWidth,$imageHeight)
    $graphics=[Drawing.Graphics]::FromImage($bitmap)
    try { $graphics.CopyFromScreen($dialogRect.Left,$dialogRect.Top,0,0,$bitmap.Size); $bitmap.Save($Screenshot,[Drawing.Imaging.ImageFormat]::Png) }
    finally { $graphics.Dispose(); $bitmap.Dispose(); [PhotoPickerProbe]::SetWindowPos($dialogHandle,[IntPtr](-2),0,0,0,0,0x13) | Out-Null }
}
if($Cancel) { [PhotoPickerProbe]::PostMessage($dialogHandle,0x111,[IntPtr]2,[IntPtr]::Zero) | Out-Null }
elseif($SelectPath) {
    if(-not (Test-Path -LiteralPath $SelectPath -PathType Leaf)) { throw 'Selected test file is missing.' }
    $fileCombo=[PhotoPickerProbe]::GetDlgItem($dialogHandle,0x47C)
    if($fileCombo -eq [IntPtr]::Zero) { throw 'File name field was not found.' }
    [PhotoPickerProbe]::SendMessage($fileCombo,0xC,[IntPtr]::Zero,$SelectPath) | Out-Null
    [PhotoPickerProbe]::PostMessage($dialogHandle,0x111,[IntPtr]1,[IntPtr]::Zero) | Out-Null
}
