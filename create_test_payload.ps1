@{
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
} | ConvertTo-Json -Depth 10 | Out-File -FilePath "test_payload.json" -Encoding UTF8

Write-Host "Test payload saved to test_payload.json"
Get-Content "test_payload.json"
