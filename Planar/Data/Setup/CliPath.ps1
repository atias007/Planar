$confirmation = Read-Host "Insert the path for planar.exe cli tool"
if($null -ne $confirmation -and "" -ne $confirmation)
{
    $path = [System.Environment]::GetEnvironmentVariable('path')
    $path = $path + ';' + $confirmation
    [System.Environment]::SetEnvironmentVariable('path',$path)
    Write-host $path
    Write-Host 'Done :)' -ForegroundColor Green
}
else
{
    Write-Host 'cancelled: value is null or empty' -ForegroundColor Red
}
