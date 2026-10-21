param(
    [string]$DefaultGamePath = "D:\steam\steamapps\common\Slime Rancher"
)

Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, System.Windows.Forms

$scriptDir = $PSScriptRoot
if (-not $scriptDir) { $scriptDir = (Get-Location).Path }

$xaml = @'
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Slime Rancher Co-op Manager" Height="580" Width="680"
        WindowStartupLocation="CenterScreen" Background="#181825" ResizeMode="CanMinimize">
    <Window.Resources>
        <Style TargetType="Button">
            <Setter Property="Foreground" Value="#CDD6F4"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="Padding" Value="10,6"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border Background="{TemplateBinding Background}" CornerRadius="6" Padding="{TemplateBinding Padding}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </Window.Resources>
    
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <Border Grid.Row="0" Background="#1E1E2E" CornerRadius="8" Padding="15,12" Margin="0,0,0,15">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0">
                    <TextBlock Text="Slime Rancher — Co-op Manager" Foreground="#CBA6F7" FontSize="20" FontWeight="Bold"/>
                    <TextBlock Text="Управление мультиплеером, быстрая установка и запуск" Foreground="#A6ADC8" FontSize="12" Margin="0,2,0,0"/>
                </StackPanel>
                <Border Grid.Column="1" x:Name="BadgeStatus" Background="#313244" CornerRadius="12" Padding="12,5" VerticalAlignment="Center">
                    <TextBlock x:Name="TxtStatus" Text="Проверка..." Foreground="#F9E2AF" FontSize="12" FontWeight="Bold"/>
                </Border>
            </Grid>
        </Border>

        <!-- Game Path & Nickname -->
        <Border Grid.Row="1" Background="#1E1E2E" CornerRadius="8" Padding="15" Margin="0,0,0,15">
            <StackPanel>
                <TextBlock Text="Путь к игре:" Foreground="#BAC2DE" FontSize="12" FontWeight="SemiBold" Margin="0,0,0,4"/>
                <Grid Margin="0,0,0,10">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBox x:Name="TxtGamePath" Grid.Column="0" Background="#313244" Foreground="#CDD6F4" BorderThickness="0" Padding="8,6" FontSize="12" VerticalContentAlignment="Center"/>
                    <Button x:Name="BtnBrowse" Grid.Column="1" Content="Обзор..." Background="#45475A" Margin="8,0,0,0" Width="80"/>
                </Grid>

                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" Margin="0,0,8,0">
                        <TextBlock Text="Ник игрока:" Foreground="#BAC2DE" FontSize="12" FontWeight="SemiBold" Margin="0,0,0,4"/>
                        <TextBox x:Name="TxtNickname" Background="#313244" Foreground="#CDD6F4" BorderThickness="0" Padding="8,6" FontSize="12"/>
                    </StackPanel>
                    <StackPanel Grid.Column="1" Margin="8,0,0,0">
                        <TextBlock Text="Локальный IP (для LAN):" Foreground="#BAC2DE" FontSize="12" FontWeight="SemiBold" Margin="0,0,0,4"/>
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="Auto"/>
                            </Grid.ColumnDefinitions>
                            <TextBox x:Name="TxtIp" Grid.Column="0" IsReadOnly="True" Background="#313244" Foreground="#A6ADC8" BorderThickness="0" Padding="8,6" FontSize="12"/>
                            <Button x:Name="BtnCopyIp" Grid.Column="1" Content="Копия" Background="#45475A" Margin="6,0,0,0" Width="60"/>
                        </Grid>
                    </StackPanel>
                </Grid>
            </StackPanel>
        </Border>

        <!-- Action Buttons -->
        <Grid Grid.Row="2" Margin="0,0,0,15">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <Button x:Name="BtnInstall" Grid.Column="0" Content="⚡ Установить кооп" Background="#A6E3A1" Foreground="#11111B" Margin="0,0,5,0" Height="38"/>
            <Button x:Name="BtnUninstall" Grid.Column="1" Content="🗑️ Вернуть оригинал" Background="#F38BA8" Foreground="#11111B" Margin="5,0,5,0" Height="38"/>
            <Button x:Name="BtnLaunch" Grid.Column="2" Content="🚀 Запустить игру" Background="#89B4FA" Foreground="#11111B" Margin="5,0,0,0" Height="38"/>
        </Grid>

        <!-- Extra Tools & Quick Links -->
        <Grid Grid.Row="3" Margin="0,0,0,10">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <Button x:Name="BtnOpenFolder" Grid.Column="0" Content="📂 Папка модов" Background="#313244" Margin="0,0,5,0" FontSize="12"/>
            <Button x:Name="BtnDedicatedServer" Grid.Column="1" Content="🌐 Выделенный сервер" Background="#313244" Margin="5,0,5,0" FontSize="12"/>
            <Button x:Name="BtnHelp" Grid.Column="2" Content="❓ Горячие клавиши" Background="#313244" Margin="5,0,0,0" FontSize="12"/>
        </Grid>

        <!-- Console / Status Output -->
        <Border Grid.Row="4" Background="#11111B" CornerRadius="6" Padding="10">
            <ScrollViewer x:Name="ScrollerLog" VerticalScrollBarVisibility="Auto">
                <TextBox x:Name="TxtLog" Background="Transparent" Foreground="#A6ADC8" BorderThickness="0" FontFamily="Consolas" FontSize="11" IsReadOnly="True" TextWrapping="Wrap"/>
            </ScrollViewer>
        </Border>

        <!-- Footer -->
        <TextBlock Grid.Row="5" Text="Slime Rancher MultiPlayer (SRMP Revival v1652). Меню в игре: F4 | Чат: Enter" Foreground="#585B70" FontSize="11" Margin="0,10,0,0" HorizontalAlignment="Center"/>
    </Grid>
</Window>
'@

$reader = [System.Xml.XmlReader]::Create([System.IO.StringReader]::new($xaml))
$win = [System.Windows.Markup.XamlReader]::Load($reader)

# Controls
$txtGamePath = $win.FindName("TxtGamePath")
$btnBrowse = $win.FindName("BtnBrowse")
$txtNickname = $win.FindName("TxtNickname")
$txtIp = $win.FindName("TxtIp")
$btnCopyIp = $win.FindName("BtnCopyIp")
$btnInstall = $win.FindName("BtnInstall")
$btnUninstall = $win.FindName("BtnUninstall")
$btnLaunch = $win.FindName("BtnLaunch")
$btnOpenFolder = $win.FindName("BtnOpenFolder")
$btnDedicatedServer = $win.FindName("BtnDedicatedServer")
$btnHelp = $win.FindName("BtnHelp")
$txtLog = $win.FindName("TxtLog")
$scrollerLog = $win.FindName("ScrollerLog")
$badgeStatus = $win.FindName("BadgeStatus")
$txtStatus = $win.FindName("TxtStatus")

function Log([string]$msg) {
    $time = (Get-Date).ToString("HH:mm:ss")
    $txtLog.AppendText("[$time] $msg`r`n")
    $scrollerLog.ScrollToEnd()
}

# Auto-detect IP
try {
    $localIp = (Get-NetIPAddress -AddressFamily IPv4 -InterfaceAlias "Wi-Fi*", "Ethernet*" -ErrorAction SilentlyContinue | Where-Object { $_.IPAddress -notmatch "^169\." } | Select-Object -First 1).IPAddress
    if (-not $localIp) { $localIp = "127.0.0.1" }
    $txtIp.Text = $localIp
} catch {
    $txtIp.Text = "127.0.0.1"
}

# Initialize Game Path
if (Test-Path "$DefaultGamePath\SlimeRancher.exe") {
    $txtGamePath.Text = $DefaultGamePath
} else {
    $candidate = "D:\steam\steamapps\common\Slime Rancher"
    if (Test-Path "$candidate\SlimeRancher.exe") {
        $txtGamePath.Text = $candidate
    } else {
        $txtGamePath.Text = "C:\Program Files (x86)\Steam\steamapps\common\Slime Rancher"
    }
}

function UpdateStatus() {
    $game = $txtGamePath.Text
    if (-not (Test-Path "$game\SlimeRancher.exe")) {
        $txtStatus.Text = "Игра не найдена"
        $txtStatus.Foreground = [System.Windows.Media.Brushes]::Red
        $badgeStatus.Background = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#452028")
        return
    }

    $srmpDll = Join-Path $game "SRML\Mods\SRMP.dll"
    if (Test-Path $srmpDll) {
        $txtStatus.Text = "Кооп активен (SRMP v1652)"
        $txtStatus.Foreground = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#A6E3A1")
        $badgeStatus.Background = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#204528")
    } else {
        $txtStatus.Text = "Оригинал (Vanilla)"
        $txtStatus.Foreground = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#BAC2DE")
        $badgeStatus.Background = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#313244")
    }
}

# Load Nickname
try {
    $nick = (Get-ItemProperty -Path "HKCU:\Software\Monomi Park\Slime Rancher" -ErrorAction SilentlyContinue).SRMP_Username
    if ($nick) { $txtNickname.Text = $nick }
    else { $txtNickname.Text = $env:USERNAME }
} catch {
    $txtNickname.Text = $env:USERNAME
}

# Handlers
$btnCopyIp.Add_Click({
    [System.Windows.Clipboard]::SetText($txtIp.Text)
    Log "IP адрес скопирован в буфер: $($txtIp.Text)"
})

$btnBrowse.Add_Click({
    $dlg = New-Object System.Windows.Forms.FolderBrowserDialog
    $dlg.Description = "Выберите папку с установленной игрой Slime Rancher"
    if (Test-Path $txtGamePath.Text) { $dlg.SelectedPath = $txtGamePath.Text }
    if ($dlg.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {
        $txtGamePath.Text = $dlg.SelectedPath
        UpdateStatus
    }
})

$txtGamePath.Add_TextChanged({
    UpdateStatus
})

$btnInstall.Add_Click({
    $game = $txtGamePath.Text
    Log "--- Начало установки коопа ---"
    if (-not (Test-Path "$game\SlimeRancher.exe")) {
        Log "ОШИБКА: SlimeRancher.exe не найден по указанному пути!"
        return
    }

    if ($txtNickname.Text) {
        try {
            if (-not (Test-Path "HKCU:\Software\Monomi Park\Slime Rancher")) {
                New-Item -Path "HKCU:\Software\Monomi Park\Slime Rancher" -Force | Out-Null
            }
            Set-ItemProperty -Path "HKCU:\Software\Monomi Park\Slime Rancher" -Name "SRMP_Username" -Value $txtNickname.Text
            Log "Сохранен никнейм: $($txtNickname.Text)"
        } catch { }
    }

    try {
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $scriptDir "Install-Coop.ps1") -GamePath $game
        Log "Кооп успешно установлен! Можно запускать игру."
    } catch {
        Log "Ошибка при установке: $_"
    }
    UpdateStatus
})

$btnUninstall.Add_Click({
    $game = $txtGamePath.Text
    Log "--- Возврат к оригинальной игре ---"
    try {
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $scriptDir "Uninstall-Coop.ps1") -GamePath $game
        Log "Кооп удален. Slime Rancher возвращен к чистому оригиналу."
    } catch {
        Log "Ошибка при удалении: $_"
    }
    UpdateStatus
})

$btnLaunch.Add_Click({
    $game = $txtGamePath.Text
    $exe = Join-Path $game "SlimeRancher.exe"
    if (Test-Path $exe) {
        Log "Запуск игры: $exe"
        Start-Process $exe -WorkingDirectory $game
    } else {
        Log "Файл $exe не найден."
    }
})

$btnOpenFolder.Add_Click({
    $game = $txtGamePath.Text
    $modsDir = Join-Path $game "SRML\Mods"
    if (-not (Test-Path $modsDir)) { $modsDir = $game }
    Start-Process "explorer.exe" $modsDir
})

$btnDedicatedServer.Add_Click({
    $serverDir = "C:\Users\top34\.gemini\antigravity\scratch\srmp-revival\serveur"
    if (Test-Path $serverDir) {
        $runCmd = Join-Path $serverDir "run.cmd"
        if (Test-Path $runCmd) {
            Start-Process "cmd.exe" "/c `"$runCmd`"" -WorkingDirectory $serverDir
            Log "Выделенный сервер запущен в отдельном окне."
        } else {
            Start-Process "explorer.exe" $serverDir
        }
    } else {
        Log "Папка выделенного сервера не найдена."
    }
})

$btnHelp.Add_Click({
    $helpText = "=== Горячие клавиши и управление в коопе ===`n`n[F4] - Открыть/скрыть панель мультиплеера`n[Enter] - Открыть чат`n`nКоманды чата:`n/home - Мгновенная телепортация на базу/ранчо`n/ping - Пинг до хоста`n/tps - Тикрейт сервера`n/list - Список игроков онлайн`n/sleep <часы> - Пропустить время (сон)`n`nПодключение без проброса портов:`nХост жмет 'Host Game' -> передает другу Server Code.`nДруг вставляет Server Code -> жмет 'Connect'."
    [System.Windows.MessageBox]::Show($helpText, "Справка по коопу", [System.Windows.MessageBoxButton]::OK, [System.Windows.MessageBoxImage]::Information)
})

# First load
UpdateStatus
Log "Slime Rancher Co-op Manager готов к работе."
Log "Игра обнаружена: $($txtGamePath.Text)"

$win.ShowDialog() | Out-Null
