# Test the streaming API directly to see if it now sends real-time chunks
# Run this while the LombdaAgentAPI is running

$apiUrl = "https://localhost:5001"
Write-Host "?? Testing LombdaAgent API Streaming (Enhanced Debug Version)..." -ForegroundColor Green

try {
    # First, create a test agent
    Write-Host "?? Creating test agent..." -ForegroundColor Yellow
    $createAgentBody = @{
        name = "StreamTestAgent_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    } | ConvertTo-Json

    # Use Invoke-WebRequest instead of Invoke-RestMethod for better SSL handling
    try {
        $agentResponse = Invoke-RestMethod -Uri "$apiUrl/v1/agents" -Method POST -Body $createAgentBody -ContentType "application/json"
    }
    catch {
        # Try HTTP fallback if HTTPS fails
        Write-Host "?? HTTPS failed, trying HTTP..." -ForegroundColor Yellow
        $apiUrl = "http://localhost:5000"
        $agentResponse = Invoke-RestMethod -Uri "$apiUrl/v1/agents" -Method POST -Body $createAgentBody -ContentType "application/json"
    }
    
    $agentId = $agentResponse.id
    Write-Host "? Created agent: $($agentResponse.name) (ID: $agentId)" -ForegroundColor Green

    # Test 1: Test streaming infrastructure with the manual test endpoint
    Write-Host "`n?? Testing streaming infrastructure..." -ForegroundColor Cyan
    
    try {
        $testResponse = $httpClient.UploadString("$apiUrl/v1/agents/$agentId/test-stream", "POST", "")
        Write-Host "?? Test streaming response received" -ForegroundColor Green
        
        $testLines = $testResponse -split "`n"
        $testMessageChunks = @()
        $currentEvent = ""
        
        Write-Host "?? Test streaming data:" -ForegroundColor Yellow
        foreach ($line in $testLines) {
            if ($line.StartsWith("event: ")) {
                $currentEvent = $line.Substring(7).Trim()
                Write-Host "  ?? Event: $currentEvent" -ForegroundColor Magenta
            }
            elseif ($line.StartsWith("data: ")) {
                $data = $line.Substring(6)
                if ($currentEvent -eq "message") {
                    $testMessageChunks += $data
                    Write-Host "  ?? Test chunk: '$data'" -ForegroundColor Green
                }
            }
        }
        
        if ($testMessageChunks.Count -gt 0) {
            Write-Host "? Streaming infrastructure working! Received $($testMessageChunks.Count) test chunks." -ForegroundColor Green
        } else {
            Write-Host "? Streaming infrastructure not working properly." -ForegroundColor Red
        }
    }
    catch {
        Write-Host "? Test streaming endpoint error: $($_.Exception.Message)" -ForegroundColor Red
    }

    # Test 2: Real agent streaming
    Write-Host "`n?? Testing real agent streaming..." -ForegroundColor Yellow
    
    $messageBody = @{
        text = "Please count from 1 to 5, putting each number on a separate line"
        threadId = $null
    } | ConvertTo-Json

    Write-Host "?? Sending real streaming request..." -ForegroundColor Cyan
    
    # Create HTTP client for streaming (compatible with older PowerShell)
    $httpClient = New-Object System.Net.WebClient
    $httpClient.Headers.Add("Content-Type", "application/json")
    $httpClient.Headers.Add("Accept", "text/event-stream")
    $httpClient.Headers.Add("Cache-Control", "no-cache")
    
    # Ignore SSL certificate errors for localhost testing
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
    
    Write-Host "?? Starting streaming response..." -ForegroundColor Magenta
    $startTime = Get-Date
    
    try {
        # Send request and get response as string
        $responseText = $httpClient.UploadString("$apiUrl/v1/agents/$agentId/messages/stream", "POST", $messageBody)
        
        Write-Host "?? Response received successfully" -ForegroundColor Cyan
        
        $lines = $responseText -split "`n"
        $messageChunks = @()
        $currentEvent = ""
        $lineCount = 0
        
        Write-Host "?? Parsing streaming data..." -ForegroundColor Yellow
        
        foreach ($line in $lines) {
            $lineCount++
            $currentTime = Get-Date
            $timeSinceStart = ($currentTime - $startTime).TotalSeconds
            
            Write-Host "[$([Math]::Round($timeSinceStart, 2))s] Line $lineCount`: '$line'" -ForegroundColor White
            
            if ($line.StartsWith("event: ")) {
                $currentEvent = $line.Substring(7).Trim()
                Write-Host "  ?? Event: $currentEvent" -ForegroundColor Magenta
            }
            elseif ($line.StartsWith("data: ")) {
                $data = $line.Substring(6)
                if ($currentEvent -eq "message") {
                    $messageChunks += $data
                    Write-Host "  ?? Message chunk: '$data'" -ForegroundColor Green
                }
                elseif ($currentEvent -eq "complete") {
                    Write-Host "  ? Complete data: $data" -ForegroundColor Blue
                }
                elseif ($currentEvent -eq "error") {
                    Write-Host "  ? Error data: $data" -ForegroundColor Red
                }
                else {
                    Write-Host "  ? Unknown event data: $data" -ForegroundColor Gray
                }
            }
            elseif ($line.StartsWith(":")) {
                Write-Host "  ?? Heartbeat comment" -ForegroundColor DarkGray
            }
            elseif ([string]::IsNullOrEmpty($line.Trim())) {
                Write-Host "  ? Empty line (event separator)" -ForegroundColor DarkGray
            }
            else {
                Write-Host "  ? Unhandled line: '$line'" -ForegroundColor Gray
            }
        }
        
        $endTime = Get-Date
        $totalTime = ($endTime - $startTime).TotalSeconds
        
        Write-Host "`n?? Final Results:" -ForegroundColor Cyan
        Write-Host "  ?? Total time: $([Math]::Round($totalTime, 2)) seconds" -ForegroundColor White
        Write-Host "  ?? Total lines received: $lineCount" -ForegroundColor White
        Write-Host "  ?? Total message chunks: $($messageChunks.Count)" -ForegroundColor White
        Write-Host "  ?? Combined message: '$($messageChunks -join '')'" -ForegroundColor White
        
        if ($messageChunks.Count -gt 0) {
            Write-Host "?? SUCCESS: Real-time streaming is working! Received $($messageChunks.Count) chunks over $([Math]::Round($totalTime, 2)) seconds." -ForegroundColor Green
        } else {
            Write-Host "?? WARNING: No message chunks received during streaming." -ForegroundColor Yellow
            Write-Host "?? This suggests the agent is not sending real-time chunks but the streaming infrastructure works." -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "? HTTP Error during streaming request: $($_.Exception.Message)" -ForegroundColor Red
    }
    finally {
        $httpClient.Dispose()
    }

} catch {
    Write-Host "? Error testing streaming: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "?? Make sure the LombdaAgentAPI is running on $apiUrl" -ForegroundColor Yellow
    if ($_.Exception.InnerException) {
        Write-Host "Inner exception: $($_.Exception.InnerException.Message)" -ForegroundColor DarkRed
    }
}

Write-Host "`n?? Diagnosis:" -ForegroundColor Cyan
Write-Host "1. If test streaming works but real streaming doesn't, the issue is with agent configuration" -ForegroundColor White
Write-Host "2. If neither works, the issue is with the streaming infrastructure" -ForegroundColor White
Write-Host "3. Check the API server console logs for [STREAMING DEBUG] messages" -ForegroundColor White