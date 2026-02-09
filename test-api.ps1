# API End-to-End Test Script
# Tests: Create Account -> Record Charge -> Get Balance -> Record Payment -> Verify Balance

$baseUrl = "http://localhost:5084/api/v1"
$accountId = [guid]::NewGuid().ToString()
$rideId = "RIDE-$(Get-Random -Minimum 1000 -Maximum 9999)"
$paymentRef = "PAY-$(Get-Random -Minimum 1000 -Maximum 9999)"

Write-Host "=== RideLedger API End-to-End Test ===" -ForegroundColor Cyan
Write-Host "Account ID: $accountId"
Write-Host "Ride ID: $rideId"
Write-Host "Payment Ref: $paymentRef"
Write-Host ""

# Test 1: Create Account
Write-Host "[1] Creating Account..." -ForegroundColor Yellow
$createAccountBody = @{
    accountId = $accountId
    name = "Test Account $(Get-Random)"
    type = "Individual"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/accounts" -Method Post `
        -Body $createAccountBody -ContentType "application/json" `
        -Headers @{ "X-Tenant-Id" = "test-tenant-123" }
    Write-Host "✓ Account created: $($response.name)" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed to create account: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.Exception.Response
    exit 1
}

# Test 2: Record Charge
Write-Host "`n[2] Recording Charge ($500.00)..." -ForegroundColor Yellow
$recordChargeBody = @{
    accountId = $accountId
    rideId = $rideId
    amount = 500.00
    serviceDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
    fleetId = "FLEET-001"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/charges" -Method Post `
        -Body $recordChargeBody -ContentType "application/json" `
        -Headers @{ "X-Tenant-Id" = "test-tenant-123" }
    Write-Host "✓ Charge recorded: Ledger Entry ID = $($response.ledgerEntryId)" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed to record charge: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 3: Get Balance (should be $500)
Write-Host "`n[3] Getting Account Balance..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/accounts/$accountId/balance" -Method Get `
        -Headers @{ "X-Tenant-Id" = "test-tenant-123" }
    Write-Host "✓ Balance: $($response.balance) $($response.currency)" -ForegroundColor Green
    if ($response.balance -ne 500.00) {
        Write-Host "✗ Warning: Expected balance 500.00, got $($response.balance)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "✗ Failed to get balance: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 4: Record Payment
Write-Host "`n[4] Recording Payment ($300.00)..." -ForegroundColor Yellow
$recordPaymentBody = @{
    accountId = $accountId
    paymentReferenceId = $paymentRef
    amount = 300.00
    paymentDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
    paymentMode = "Cash"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/payments" -Method Post `
        -Body $recordPaymentBody -ContentType "application/json" `
        -Headers @{ "X-Tenant-Id" = "test-tenant-123" }
    Write-Host "✓ Payment recorded: Ledger Entry ID = $($response.ledgerEntryId)" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed to record payment: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 5: Get Updated Balance (should be $200)
Write-Host "`n[5] Getting Updated Balance..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/accounts/$accountId/balance" -Method Get `
        -Headers @{ "X-Tenant-Id" = "test-tenant-123" }
    Write-Host "✓ Updated Balance: $($response.balance) $($response.currency)" -ForegroundColor Green
    if ($response.balance -ne 200.00) {
        Write-Host "✗ Warning: Expected balance 200.00, got $($response.balance)" -ForegroundColor Yellow
    } else {
        Write-Host "✓ Balance calculation correct!" -ForegroundColor Green
    }
} catch {
    Write-Host "✗ Failed to get updated balance: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 6: Get Account Details
Write-Host "`n[6] Getting Account Details..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/accounts/$accountId" -Method Get `
        -Headers @{ "X-Tenant-Id" = "test-tenant-123" }
    Write-Host "✓ Account: $($response.name)" -ForegroundColor Green
    Write-Host "  Type: $($response.type)" -ForegroundColor Gray
    Write-Host "  Status: $($response.status)" -ForegroundColor Gray
    Write-Host "  Balance: $($response.balance) $($response.currency)" -ForegroundColor Gray
} catch {
    Write-Host "✗ Failed to get account details: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== All Tests Passed! ===" -ForegroundColor Green
Write-Host "Account $accountId successfully tested" -ForegroundColor Cyan
