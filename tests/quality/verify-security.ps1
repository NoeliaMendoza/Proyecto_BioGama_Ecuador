param(
    [string]$BaseUrl = 'http://127.0.0.1:30080',
    [Parameter(Mandatory = $true)][PSCredential]$AdminCredential
)
$ErrorActionPreference = 'Stop'
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
function Get-Token($Html) {
    $match = [regex]::Match($Html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (-not $match.Success) { throw 'Token antifalsificacion no encontrado.' }
    return [Net.WebUtility]::HtmlDecode($match.Groups[1].Value)
}

$login = Invoke-WebRequest "$BaseUrl/Identity/Account/Login" -WebSession $session -UseBasicParsing
$null = Invoke-WebRequest "$BaseUrl/Identity/Account/Login" -WebSession $session -UseBasicParsing -Method Post -Body @{
    'Input.Email' = $AdminCredential.UserName
    'Input.Password' = $AdminCredential.GetNetworkCredential().Password
    '__RequestVerificationToken' = (Get-Token $login.Content)
}
$inventory = Invoke-WebRequest "$BaseUrl/Admin/Inventory" -WebSession $session -UseBasicParsing
if ($inventory.BaseResponse.ResponseUri.AbsolutePath -ne '/Admin/Inventory') { throw 'Login de administrador requerido.' }
$token = Get-Token $inventory.Content

$cases = @(
    @{ Action = 'CreatePayPalOrder'; Body = ('{"orderId":"' + [Guid]::NewGuid().ToString() + '"}') },
    @{ Action = 'CapturePayPalOrder'; Body = '{"payPalOrderId":"nonexistent-security-check"}' }
)
foreach ($case in $cases) {
    # Windows PowerShell puede conservar headers de la solicitud anterior.
    $null = $session.Headers.Remove('RequestVerificationToken')
    $withoutToken = 0
    try {
        $result = Invoke-WebRequest "$BaseUrl/Orders/$($case.Action)" -Method Post -WebSession $session `
            -UseBasicParsing -ContentType 'application/json' -Body $case.Body
        $withoutToken = [int]$result.StatusCode
    } catch {
        if ($_.Exception.Response) { $withoutToken = [int]$_.Exception.Response.StatusCode }
        else { throw }
    }
    if ($withoutToken -ne 400) { throw "$($case.Action): se esperaba HTTP 400 sin token, se obtuvo $withoutToken." }
    $validRequest = Invoke-WebRequest "$BaseUrl/Orders/$($case.Action)" -Method Post -WebSession $session `
        -UseBasicParsing -ContentType 'application/json' -Body $case.Body -Headers @{ RequestVerificationToken = $token }
    $payload = $validRequest.Content | ConvertFrom-Json
    if ($validRequest.StatusCode -ne 200 -or $payload.success -ne $false) {
        throw 'El token valido no permitio llegar a la validacion de una orden inexistente.'
    }
    Write-Output "PASS $($case.Action): sin token=400; con token=200 y orden inexistente rechazada."
}
Write-Output 'No se crearon ordenes ni se llamo a las pasarelas externas.'
