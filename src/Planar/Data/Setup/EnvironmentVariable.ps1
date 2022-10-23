$env = [System.Environment]::GetEnvironmentVariable('PLANAR_ENVIRONMENT')
if($null -ne $env -and "" -ne $env)
{
    Write-Host "Current value of environment variable PLANAR_ENVIRONMENT is:"
    Write-Host $env -ForegroundColor Green
    $question = 'Are you sure you want to ovveride current value?'
    $choices  = '&Yes', '&No'

    $decision = $Host.UI.PromptForChoice($null, $question, $choices, 1)
    if ($decision -ne 0) {       
        Write-Host 'cancelled' -ForegroundColor Red
        return
    }
}

$confirmation = Read-Host "Set the name of Planar environment"
if($null -ne $confirmation -and "" -ne $confirmation)
{
    [System.Environment]::SetEnvironmentVariable('PLANAR_ENVIRONMENT',$confirmation)
    Write-Host 'Done :)' -ForegroundColor Green
}
else
{
    Write-Host 'cancelled: value is null or empty' -ForegroundColor Red
}
