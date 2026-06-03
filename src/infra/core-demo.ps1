param(
    [switch]$Stop
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$pidFile = Join-Path $root "infra\.core-demo.pids"
$resultFile = Join-Path $root "infra\core-demo-result.json"

function Stop-CoreDemo {
    if (-not (Test-Path $pidFile)) {
        Write-Host "No core demo PID file found."
        return
    }

    Get-Content $pidFile | ForEach-Object {
        $processId = 0
        if ([int]::TryParse($_, [ref]$processId)) {
            $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
            if ($process) {
                Stop-Process -Id $process.Id -Force
            }
        }
    }

    Remove-Item $pidFile -Force
    Write-Host "Core demo services stopped."
}

if ($Stop) {
    Stop-CoreDemo
    exit 0
}

if (Test-Path $pidFile) {
    Stop-CoreDemo
}

if (Test-Path $resultFile) {
    Remove-Item $resultFile -Force
}

Set-Location $root

try {
    docker info *> $null
}
catch {
    throw "Docker is not available. Start Docker Desktop, wait until the Linux engine is running, then rerun infra\core-demo.ps1."
}

docker compose up -d

Get-Content (Join-Path $root "infra\postgres\init\04-privatemessaging.sql") |
    docker compose exec -T postgres psql -U app_user -d identity_db

Get-Content (Join-Path $root "infra\postgres\init\05-media.sql") |
    docker compose exec -T postgres psql -U app_user -d identity_db

Get-Content (Join-Path $root "infra\postgres\init\06-helprequest.sql") |
    docker compose exec -T postgres psql -U app_user -d identity_db

$services = @(
    @{ Name = "IdentityService.Api"; Project = "IdentityService\IdentityService.Api\IdentityService.Api.csproj" },
    @{ Name = "UserProfileService.Api"; Project = "UserProfileService\UserProfileService.Api\UserProfileService.Api.csproj" },
    @{ Name = "PetService.Api"; Project = "PetService\PetService.Api\PetService.Api.csproj" },
    @{ Name = "MediaService.Api"; Project = "MediaService\MediaService.Api\MediaService.Api.csproj" },
    @{ Name = "HelpRequestService.Api"; Project = "HelpRequestService\HelpRequestService.Api\HelpRequestService.Api.csproj" },
    @{ Name = "PrivateMessagingService.Api"; Project = "PrivateMessagingService\PrivateMessagingService.Api\PrivateMessagingService.Api.csproj" },
    @{ Name = "ApiGatewayService.Api"; Project = "ApiGatewayService\ApiGatewayService.Api\ApiGatewayService.Api.csproj" }
)

$pids = @()
foreach ($service in $services) {
    $outLog = Join-Path $root "$($service.Name).out.log"
    $errLog = Join-Path $root "$($service.Name).err.log"
    $process = Start-Process dotnet `
        -ArgumentList @("run", "--project", $service.Project, "--launch-profile", "http") `
        -WorkingDirectory $root `
        -RedirectStandardOutput $outLog `
        -RedirectStandardError $errLog `
        -PassThru `
        -WindowStyle Hidden
    $pids += $process.Id
}

$pids | Set-Content $pidFile

function Wait-Http($url) {
    $deadline = (Get-Date).AddSeconds(45)
    do {
        try {
            Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 2 | Out-Null
            return
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for $url"
}

function Wait-Rest($uri, $headers) {
    $deadline = (Get-Date).AddSeconds(20)
    do {
        try {
            return Invoke-RestMethod -Method Get -Uri $uri -Headers $headers
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for $uri"
}

Wait-Http "http://localhost:5145/swagger/index.html"
Wait-Http "http://localhost:5182/swagger/index.html"
Wait-Http "http://localhost:5035/swagger/index.html"
Wait-Http "http://localhost:5217/swagger/index.html"
Wait-Http "http://localhost:5220/swagger/index.html"
Wait-Http "http://localhost:5196/swagger/index.html"
Wait-Http "http://localhost:5012/health"

$suffix = [Guid]::NewGuid().ToString("N").Substring(0, 8)
$password = "Password123!"
$firstEmail = "demo-$suffix@appanimaux.local"
$secondEmail = "demo-peer-$suffix@appanimaux.local"

$first = Invoke-RestMethod -Method Post -Uri "http://localhost:5012/auth/register" `
    -ContentType "application/json" `
    -Body (@{ email = $firstEmail; password = $password } | ConvertTo-Json)

$second = Invoke-RestMethod -Method Post -Uri "http://localhost:5012/auth/register" `
    -ContentType "application/json" `
    -Body (@{ email = $secondEmail; password = $password } | ConvertTo-Json)

$headers = @{ Authorization = "Bearer $($first.accessToken)" }
$autoProfile = Wait-Rest "http://localhost:5012/profiles/me" $headers

$profile = Invoke-RestMethod -Method Put -Uri "http://localhost:5012/profiles/me" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body (@{ username = "demo_$suffix"; displayName = "Demo Core"; bio = "Core smoke demo"; city = "Paris"; country = "FR" } | ConvertTo-Json)

$avatarPath = Join-Path ([System.IO.Path]::GetTempPath()) "appanimaux-core-demo-avatar.png"
[System.IO.File]::WriteAllBytes(
    $avatarPath,
    [Convert]::FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII="))

$uploadOutput = & curl.exe -sS -X POST "http://localhost:5012/media/images" `
    -H "Authorization: Bearer $($first.accessToken)" `
    -F "file=@$avatarPath;type=image/png" `
    -F "isPublic=true" `
    -F "serviceName=userprofile" `
    -F "entityType=profile" `
    -F "entityId=$($profile.id)" `
    -F "usageType=avatar"

if ($LASTEXITCODE -ne 0) {
    throw "Media upload failed with curl exit code $LASTEXITCODE."
}

$media = $uploadOutput | ConvertFrom-Json
if (-not $media.id) {
    throw "Media upload failed: $uploadOutput"
}

Invoke-RestMethod -Method Put -Uri "http://localhost:5012/profiles/me/avatar" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body (@{ mediaId = $media.id; mediaUrl = $media.publicUrl } | ConvertTo-Json) | Out-Null

$profileWithAvatar = Invoke-RestMethod -Method Get -Uri "http://localhost:5012/profiles/me" -Headers $headers

$pet = Invoke-RestMethod -Method Post -Uri "http://localhost:5012/pets" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body (@{ name = "Milo"; species = "cat"; breed = "European"; sex = "male"; weightKg = 4.2; color = "black" } | ConvertTo-Json)

$helpRequest = Invoke-RestMethod -Method Post -Uri "http://localhost:5012/help-requests" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body (@{
        petId = $pet.pet.id
        title = "Garde courte pour Milo"
        description = "Besoin d'une visite de controle pendant une demi-journee."
        helpType = "visite"
        city = "Paris"
        postalCode = "75011"
        latitude = 48.8566
        longitude = 2.3522
        isPaid = $true
        budgetAmount = 15
        currency = "EUR"
    } | ConvertTo-Json)

Invoke-RestMethod -Method Post -Uri "http://localhost:5012/help-requests/$($helpRequest.helpRequest.id)/publish" `
    -Headers $headers | Out-Null

$searchResults = Invoke-RestMethod -Method Get -Uri "http://localhost:5012/help-requests/search?helpType=visite&latitude=48.8566&longitude=2.3522&radiusKm=5" `
    -Headers $headers

$peerHeaders = @{ Authorization = "Bearer $($second.accessToken)" }
$helpOffer = Invoke-RestMethod -Method Post -Uri "http://localhost:5012/help-requests/$($helpRequest.helpRequest.id)/proposals" `
    -Headers $peerHeaders `
    -ContentType "application/json" `
    -Body (@{ message = "Disponible pour passer voir Milo."; proposedAmount = 15; currency = "EUR" } | ConvertTo-Json)

$helpMatch = Invoke-RestMethod -Method Post -Uri "http://localhost:5012/help-requests/$($helpRequest.helpRequest.id)/proposals/$($helpOffer.id)/accept" `
    -Headers $headers

$conversation = Invoke-RestMethod -Method Post -Uri "http://localhost:5012/conversations" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body (@{ memberUserIds = @($second.userId); title = "Core demo" } | ConvertTo-Json)

$message = Invoke-RestMethod -Method Post -Uri "http://localhost:5012/conversations/$($conversation.conversation.id)/messages" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body (@{ content = "Hello from the core demo"; messageType = "text" } | ConvertTo-Json)

$result = [pscustomobject]@{
    gateway = "http://localhost:5012"
    userId = $first.userId
    peerUserId = $second.userId
    autoProfileId = $autoProfile.id
    profileId = $profile.id
    mediaId = $media.id
    avatarUrl = $profileWithAvatar.avatarUrl
    petId = $pet.pet.id
    helpRequestId = $helpRequest.helpRequest.id
    helpOfferId = $helpOffer.id
    helpMatchId = $helpMatch.id
    helpSearchCount = $searchResults.Count
    conversationId = $conversation.conversation.id
    messageId = $message.id
}

$resultJson = $result | ConvertTo-Json
$resultJson | Set-Content $resultFile
Write-Output $resultJson
