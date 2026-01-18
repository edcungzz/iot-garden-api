# Test TTN Webhook with sample data
$payload = @{
    end_device_ids = @{
        device_id = "hetec5-1"
    }
    uplink_message = @{
        decoded_payload = @{
            temp = 28.5
            humi = 65.2
            ec   = 100
            ph   = 6.8
            n    = 45
            p    = 30
            k    = 25
        }
    }
} | ConvertTo-Json -Depth 10

Write-Host "🚀 Sending test webhook to http://localhost:5021/api/uplink" -ForegroundColor Cyan

$response = Invoke-WebRequest `
    -Uri "http://localhost:5021/api/uplink" `
    -Method POST `
    -Body $payload `
    -ContentType "application/json" `
    -UseBasicParsing

Write-Host "✅ Response Status: $($response.StatusCode)" -ForegroundColor Green
Write-Host "Response Body:" -ForegroundColor Yellow
$response.Content
