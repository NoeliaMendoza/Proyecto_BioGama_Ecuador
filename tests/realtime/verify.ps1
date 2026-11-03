param(
    [string]$BaseUrl = 'http://127.0.0.1:30080',
    [Parameter(Mandatory = $true)][PSCredential]$AdminCredential
)
$Url = $BaseUrl.Replace('http://', 'ws://').Replace('https://', 'wss://') + '/hubs/stock'
$ErrorActionPreference = 'Stop'
$separator = [char]30
$clients = @()
$entryCompleted = $false
$webSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession

function Get-Token($Html) {
    $match = [regex]::Match($Html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (-not $match.Success) { throw 'No se encontro el token antifalsificacion.' }
    return [Net.WebUtility]::HtmlDecode($match.Groups[1].Value)
}

function Post-Movement($Operation) {
    $form = Invoke-WebRequest "$BaseUrl/Admin/Inventory" -WebSession $webSession -UseBasicParsing
    $null = Invoke-WebRequest "$BaseUrl/Admin/Inventory/$Operation" -WebSession $webSession `
        -UseBasicParsing -Method Post -Body @{
            ProductId = $productId
            Quantity = 1
            Reference = 'Verificacion de stock en tiempo real'
            Notes = 'Entrada y salida compensatoria de una unidad para validar notificaciones.'
            __RequestVerificationToken = (Get-Token $form.Content)
        }
}

function Send-Frame($Client, $Payload) {
    $bytes = [Text.Encoding]::UTF8.GetBytes(($Payload | ConvertTo-Json -Depth 8 -Compress) + $separator)
    $segment = [ArraySegment[byte]]::new($bytes)
    $timeout = [Threading.CancellationTokenSource]::new(10000)
    try {
        $null = $Client.Socket.SendAsync($segment, [Net.WebSockets.WebSocketMessageType]::Text,
            $true, $timeout.Token).GetAwaiter().GetResult()
    } finally { $timeout.Dispose() }
}

function Receive-Frame($Client) {
    $timeout = [Threading.CancellationTokenSource]::new(10000)
    try {
        while (-not $Client.Pending.Contains([string]$separator)) {
            $buffer = New-Object byte[] 8192
            $result = $Client.Socket.ReceiveAsync([ArraySegment[byte]]::new($buffer),
                $timeout.Token).GetAwaiter().GetResult()
            if ($result.MessageType -eq [Net.WebSockets.WebSocketMessageType]::Close) {
                throw 'Conexion cerrada antes de recibir el evento.'
            }
            $Client.Pending += [Text.Encoding]::UTF8.GetString($buffer, 0, $result.Count)
        }
        $end = $Client.Pending.IndexOf($separator)
        $frame = $Client.Pending.Substring(0, $end)
        $Client.Pending = $Client.Pending.Substring($end + 1)
        return ($frame | ConvertFrom-Json)
    } finally { $timeout.Dispose() }
}

function Receive-Event($Client, $Target) {
    for ($i = 0; $i -lt 20; $i++) {
        $frame = Receive-Frame $Client
        if ($frame.type -eq 1 -and $frame.target -eq $Target) { return $frame.arguments[0] }
    }
    throw "No se recibio el evento $Target."
}

try {
    $login = Invoke-WebRequest "$BaseUrl/Identity/Account/Login" -WebSession $webSession -UseBasicParsing
    $null = Invoke-WebRequest "$BaseUrl/Identity/Account/Login" -WebSession $webSession `
        -UseBasicParsing -Method Post -Body @{
            'Input.Email' = $AdminCredential.UserName
            'Input.Password' = $AdminCredential.GetNetworkCredential().Password
            '__RequestVerificationToken' = (Get-Token $login.Content)
        }
    $adminPage = Invoke-WebRequest "$BaseUrl/Admin/Inventory" -WebSession $webSession -UseBasicParsing
    if ($adminPage.BaseResponse.ResponseUri.AbsolutePath -ne '/Admin/Inventory') {
        throw 'Se requiere iniciar sesion como administrador.'
    }
    $before = Invoke-RestMethod "$BaseUrl/Products/Availability"
    $product = $before.products | Where-Object { $_.available -gt 0 } | Select-Object -First 1
    if (-not $product) { throw 'Se necesita un producto disponible para esta prueba.' }
    $productId = $product.id
    $first = $null
    $second = $null
    for ($attempt = 0; $attempt -lt 12; $attempt++) {
        $client = @{ Socket = [Net.WebSockets.ClientWebSocket]::new(); Pending = ''; Pod = '' }
        $clients += $client
        $timeout = [Threading.CancellationTokenSource]::new(10000)
        try { $null = $client.Socket.ConnectAsync([Uri]$Url, $timeout.Token).GetAwaiter().GetResult() }
        finally { $timeout.Dispose() }
        Send-Frame $client @{ protocol = 'json'; version = 1 }
        $handshake = Receive-Frame $client
        if ($handshake.error) { throw $handshake.error }
        $client.Pod = (Receive-Event $client 'Connected').pod
        if (-not $first) { $first = $client }
        elseif ($client.Pod -ne $first.Pod) { $second = $client; break }
        else { $client.Socket.Abort() }
    }
    if (-not $second) { throw 'No se obtuvieron conexiones a dos pods distintos.' }
    Post-Movement 'Entry'
    $entryCompleted = $true
    $eventA = Receive-Event $first 'StockChanged'
    $eventB = Receive-Event $second 'StockChanged'
    $after = Invoke-RestMethod "$BaseUrl/Products/Availability"
    $updatedProduct = $after.products | Where-Object { $_.id -eq $productId }
    if ($eventA.id -ne $eventB.id -or $updatedProduct.available -ne ($product.available + 1)) {
        throw 'El evento o el cambio de stock no coinciden con la entrada registrada.'
    }
    [pscustomobject]@{
        Result = 'PASS'
        ClientA = $first.Pod
        ClientB = $second.Pod
        EventId = $eventA.id
        Emitter = $eventA.pod
        ProductId = $productId
        AvailableBefore = $product.available
        AvailableAfterEntry = $updatedProduct.available
    } | Format-List
} finally {
    if ($entryCompleted) {
        Post-Movement 'Exit'
        $restored = Invoke-RestMethod "$BaseUrl/Products/Availability"
        $restoredProduct = $restored.products | Where-Object { $_.id -eq $productId }
        if ($restoredProduct.available -ne $product.available) {
            Write-Warning 'El stock final no coincide: revisar movimientos concurrentes.'
        } else { Write-Output "Stock restaurado: $($restoredProduct.available) disponibles." }
    }
    foreach ($client in $clients) {
        $client.Socket.Abort()
        $client.Socket.Dispose()
    }
}
