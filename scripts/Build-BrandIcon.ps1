# Package the approved transparent artwork into standard Windows icon sizes.
# This does not redraw the mark. Run on Windows after replacing the source PNG.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskSource=Join-Path $PSScriptRoot '../src/EmuVora.App/Assets/emuvora-mark.png'
$taskOutput=Join-Path $PSScriptRoot '../installer/EmuVora.ico'
$taskImage=[Drawing.Bitmap]::new([IO.Path]::GetFullPath($taskSource))
$taskFrames=[Collections.Generic.List[byte[]]]::new()
$taskSizes=@(16,24,32,48,64,128,256)
try {
    foreach($taskSize in $taskSizes){
        $taskBitmap=[Drawing.Bitmap]::new($taskSize,$taskSize,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $taskGraphics=[Drawing.Graphics]::FromImage($taskBitmap)
        $taskStream=[IO.MemoryStream]::new()
        try {
            $taskGraphics.Clear([Drawing.Color]::Transparent)
            $taskGraphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $taskGraphics.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $taskGraphics.DrawImage($taskImage,0,0,$taskSize,$taskSize)
            $taskBitmap.Save($taskStream,[Drawing.Imaging.ImageFormat]::Png)
            $taskFrames.Add($taskStream.ToArray())
        } finally {$taskGraphics.Dispose();$taskBitmap.Dispose();$taskStream.Dispose()}
    }
    $taskFile=[IO.File]::Create([IO.Path]::GetFullPath($taskOutput))
    $taskWriter=[IO.BinaryWriter]::new($taskFile)
    try {
        $taskWriter.Write([uint16]0);$taskWriter.Write([uint16]1);$taskWriter.Write([uint16]$taskSizes.Count)
        $taskOffset=6+16*$taskSizes.Count
        for($taskIndex=0;$taskIndex -lt $taskSizes.Count;$taskIndex++){
            $taskDimension=if($taskSizes[$taskIndex] -eq 256){0}else{$taskSizes[$taskIndex]}
            $taskWriter.Write([byte]$taskDimension);$taskWriter.Write([byte]$taskDimension);$taskWriter.Write([byte]0);$taskWriter.Write([byte]0)
            $taskWriter.Write([uint16]1);$taskWriter.Write([uint16]32);$taskWriter.Write([uint32]$taskFrames[$taskIndex].Length);$taskWriter.Write([uint32]$taskOffset)
            $taskOffset+=$taskFrames[$taskIndex].Length
        }
        foreach($taskFrame in $taskFrames){$taskWriter.Write($taskFrame)}
    } finally {$taskWriter.Dispose();$taskFile.Dispose()}
} finally {$taskImage.Dispose()}
Write-Output 'Packaged transparent EmuVora icon at 16, 24, 32, 48, 64, 128 and 256 pixels.'
