$files = Get-ChildItem -Path "c:\Users\LENOVO\BowlingAlleyReservationSystem\Views" -Recurse -Filter *.cshtml
foreach ($f in $files) {
    $content = Get-Content $f.FullName -Raw
    $newContent = $content -replace '(?i)#00f2fe', '#8A9A5B'
    $newContent = $newContent -replace '(?i)#0a0b10', '#fdfbf7'
    $newContent = $newContent -replace '(?i)#1a1c2c', '#e9e5d9'
    $newContent = $newContent -replace '(?i)#151821', '#ddead1'
    $newContent = $newContent -replace '(?i)rgba\(0,\s*242,\s*254', 'rgba(138, 154, 91'
    $newContent = $newContent -replace '(?i)rgba\(0,242,254', 'rgba(138,154,91'
    $newContent = $newContent -replace '(?i)#0b192c', '#e9e5d9'
    $newContent = $newContent -replace '(?i)color:\s*#(fff|ffffff)', 'color: #2b3a2a'
    $newContent = $newContent -replace '(?i)color:\s*white', 'color: #2b3a2a'
    $newContent = $newContent -replace '(?i)text-white', 'text-dark'
    
    if ($content -ne $newContent) {
        Set-Content -Path $f.FullName -Value $newContent -NoNewline
    }
}
