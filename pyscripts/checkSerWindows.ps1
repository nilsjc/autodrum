# Kontrollera om FTDI-enheten är ansluten till Windows
Write-Host "Söker efter FTDI FT232RL-enheter..." -ForegroundColor Cyan
$devices = Get-CimInstance Win32_PnPEntity | Where-Object { $_.Caption -like "*FTDI*" -or $_.Caption -like "*USB Serial Port*" }

if ($devices) {
    foreach ($dev in $devices) {
        Write-Host "Hittade enhet: $($dev.Caption)" -ForegroundColor Green
        Write-Host "Status: $($dev.Status)" -ForegroundColor Green
    }
} else {
    Write-Host "Ingen FTDI-enhet hittades. Kontrollera drivrutiner och USB-anslutning." -ForegroundColor Red
}
