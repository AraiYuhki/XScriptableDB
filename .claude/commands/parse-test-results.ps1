# Unity Test Results Parser
# XMLテスト結果ファイルを解析して結果を表示するスクリプト

param(
    [string]$ResultFile
)

if (-not (Test-Path $ResultFile)) {
    Write-Host "Error: Result file not found: $ResultFile" -ForegroundColor Red
    exit 1
}

[xml]$xml = Get-Content $ResultFile

$testRun = $xml.'test-run'
if (-not $testRun) {
    Write-Host "Error: Invalid test result format" -ForegroundColor Red
    exit 1
}

# サマリー情報
$total = [int]$testRun.total
$passed = [int]$testRun.passed
$failed = [int]$testRun.failed
$skipped = [int]$testRun.skipped
$duration = $testRun.duration

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Unity Test Results" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Total:   $total"
Write-Host "Passed:  $passed" -ForegroundColor Green
if ($failed -gt 0) {
    Write-Host "Failed:  $failed" -ForegroundColor Red
} else {
    Write-Host "Failed:  $failed"
}
if ($skipped -gt 0) {
    Write-Host "Skipped: $skipped" -ForegroundColor Yellow
} else {
    Write-Host "Skipped: $skipped"
}
Write-Host "Duration: ${duration}s"
Write-Host ""

# 失敗したテストの詳細
function Get-FailedTests($node) {
    $failedTests = @()

    if ($node.'test-case') {
        foreach ($testCase in $node.'test-case') {
            if ($testCase.result -eq 'Failed') {
                $failedTests += @{
                    Name = $testCase.fullname
                    Message = $testCase.failure.message
                    StackTrace = $testCase.failure.'stack-trace'
                }
            }
        }
    }

    if ($node.'test-suite') {
        foreach ($suite in $node.'test-suite') {
            $failedTests += Get-FailedTests $suite
        }
    }

    return $failedTests
}

if ($failed -gt 0) {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  Failed Tests" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""

    $failedTests = Get-FailedTests $testRun

    foreach ($test in $failedTests) {
        Write-Host "Test: $($test.Name)" -ForegroundColor Red
        if ($test.Message) {
            Write-Host "Message: $($test.Message)" -ForegroundColor Yellow
        }
        if ($test.StackTrace) {
            Write-Host "Stack Trace:" -ForegroundColor Gray
            Write-Host $test.StackTrace -ForegroundColor Gray
        }
        Write-Host ""
        Write-Host "----------------------------------------"
        Write-Host ""
    }
}

# 終了コード
if ($failed -gt 0) {
    exit 1
} else {
    exit 0
}
