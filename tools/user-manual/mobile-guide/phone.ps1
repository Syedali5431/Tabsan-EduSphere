# Drives the EduSphere app on a connected Android phone (adb) and takes the guide's phone screenshots.
# Dot-source it:  . ./phone.ps1   then use Start-EduApp, Tap, Type-Text, Shot …
# Screenshots go to shots/phone/<name>.png with the phone's status bar cut off (no other apps' notifications in the guide).
$ErrorActionPreference = 'Stop'
# adb: $env:ADB, else the Android SDK in ANDROID_HOME, else the usual install places.
$script:Adb = @($env:ADB, "$env:ANDROID_HOME\platform-tools\adb.exe", 'F:\Android\Sdk\platform-tools\adb.exe', 'E:\Android\Sdk\platform-tools\adb.exe',
	"$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe", 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe') |
	Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $script:Adb) { throw 'adb not found: set $env:ADB to adb.exe of the Android SDK.' }
$script:Package = 'com.tabsan.edusphere'
$script:ShotDir = Join-Path $PSScriptRoot 'shots\phone'
New-Item -ItemType Directory -Force $script:ShotDir | Out-Null
Add-Type -AssemblyName System.Drawing

function Adb { & $script:Adb @args }

function Screen-Size {
	$m = [regex]::Match((Adb shell wm size | Out-String), '(\d+)x(\d+)\s*$')
	[pscustomobject]@{ W = [int]$m.Groups[1].Value; H = [int]$m.Groups[2].Value }
}

function Start-EduApp([switch]$Fresh) {
	if ($Fresh) { Adb shell pm clear $script:Package | Out-Null }
	Adb shell am force-stop $script:Package | Out-Null
	Adb shell monkey -p $script:Package -c android.intent.category.LAUNCHER 1 | Out-Null
}

# The screen's elements (uiautomator), with their centre points.
function Ui {
	Adb shell uiautomator dump /sdcard/edu-ui.xml | Out-Null
	[xml]$xml = (Adb exec-out cat /sdcard/edu-ui.xml | Out-String)
	foreach ($n in $xml.SelectNodes('//node')) {
		$m = [regex]::Match($n.bounds, '\[(\d+),(\d+)\]\[(\d+),(\d+)\]')
		[pscustomobject]@{
			Text = $n.text; Desc = $n.'content-desc'; Id = $n.'resource-id'; Class = $n.class; Hint = $n.hint
			X = ([int]$m.Groups[1].Value + [int]$m.Groups[3].Value) / 2; Y = ([int]$m.Groups[2].Value + [int]$m.Groups[4].Value) / 2
			Top = [int]$m.Groups[2].Value; Bottom = [int]$m.Groups[4].Value
		}
	}
}

function Find-El([string]$text, [int]$timeoutSec = 20, [switch]$Contains) {
	$deadline = (Get-Date).AddSeconds($timeoutSec)
	do {
		$hit = Ui | Where-Object {
			if ($Contains) { ($_.Text -and $_.Text.Contains($text)) -or ($_.Desc -and $_.Desc.Contains($text)) -or ($_.Hint -and $_.Hint.Contains($text)) }
			else { $_.Text -eq $text -or $_.Desc -eq $text -or $_.Hint -eq $text }
		} | Select-Object -First 1
		if ($hit) { return $hit }
		Start-Sleep -Milliseconds 700
	} while ((Get-Date) -lt $deadline)
	throw "Not on screen: $text"
}

function Tap([string]$text, [switch]$Contains, [int]$timeoutSec = 20) {
	$el = Find-El $text -Contains:$Contains -timeoutSec $timeoutSec
	Adb shell input tap ([int]$el.X) ([int]$el.Y) | Out-Null
	Start-Sleep -Milliseconds 600
}

# A bottom tab (matched by its accessibility description, so a "Courses" label on the page is not taken instead).
function Tab([string]$name) {
	$el = Ui | Where-Object { $_.Desc -eq $name } | Select-Object -First 1
	if (-not $el) { throw "No tab: $name" }
	Adb shell input tap ([int]$el.X) ([int]$el.Y) | Out-Null
	Start-Sleep -Milliseconds 900
}

function Tap-At([int]$x, [int]$y) { Adb shell input tap $x $y | Out-Null; Start-Sleep -Milliseconds 600 }

# Types into the focused field. Spaces and shell characters are escaped for 'input text'.
function Type-Text([string]$text) {
	$escaped = ($text -replace '([\\"''`$&|;<>()*?!#~\[\]{}^])', '\$1') -replace ' ', '%s'
	Adb shell "input text $escaped" | Out-Null
	Start-Sleep -Milliseconds 400
}

function Type-Into([string]$field, [string]$text) {
	Tap $field
	Adb shell input keyevent KEYCODE_MOVE_END | Out-Null
	for ($i = 0; $i -lt 40; $i++) { Adb shell input keyevent KEYCODE_DEL | Out-Null }
	Type-Text $text
}

# Closes the on-screen keyboard (Back, only while it is shown: Escape does not close every keyboard, e.g. the emulator's Gboard,
# and a tap meant for a button then types a letter instead).
function Keyboard-Shown { (Adb shell dumpsys input_method | Out-String) -match 'mInputShown=true' }
function Hide-Keyboard {
	Start-Sleep -Milliseconds 300
	if (Keyboard-Shown) { Adb shell input keyevent 4 | Out-Null; Start-Sleep -Milliseconds 700 }
}
function Back { Adb shell input keyevent 4 | Out-Null; Start-Sleep -Milliseconds 800 }

function Scroll-Down([double]$fraction = 0.55) {
	$s = Screen-Size
	$x = [int]($s.W / 2); $from = [int]($s.H * 0.78); $to = [int]($s.H * (0.78 - $fraction))
	Adb shell input swipe $x $from $x $to 450 | Out-Null
	Start-Sleep -Milliseconds 900
}

function Scroll-Top { for ($i = 0; $i -lt 4; $i++) { $s = Screen-Size; Adb shell input swipe ([int]($s.W / 2)) ([int]($s.H * 0.3)) ([int]($s.W / 2)) ([int]($s.H * 0.9)) 200 | Out-Null }; Start-Sleep -Milliseconds 600 }

# The system bars to cut off: status bar at the top, navigation bar at the bottom (pixels).
function System-Bars {
	$text = Adb shell dumpsys window | Out-String
	$h = (Screen-Size).H
	$top = [regex]::Match($text, 'statusBars[^\n]*?frame=\[\d+,\d+\]\[\d+,(\d+)\]')
	$bottom = [regex]::Match($text, 'navigationBars[^\n]*?frame=\[\d+,(\d+)\]\[\d+,\d+\]')
	[pscustomobject]@{
		Top = if ($top.Success) { [int]$top.Groups[1].Value } else { [int]($h * 0.04) }
		Bottom = if ($bottom.Success -and [int]$bottom.Groups[1].Value -gt $h / 2) { $h - [int]$bottom.Groups[1].Value } else { 0 }
	}
}

function Shot([string]$name) {
	Start-Sleep -Milliseconds 700
	Adb shell screencap -p /sdcard/edu-shot.png 2>$null | Out-Null
	$raw = Join-Path $env:TEMP "edu-$name-raw.png"
	Adb pull /sdcard/edu-shot.png $raw | Out-Null
	Save-Shot $raw $name
}

# Saves a raw phone screenshot without the system bars.
function Save-Shot([string]$raw, [string]$name) {
	$bars = System-Bars
	$img = [System.Drawing.Image]::FromFile($raw)
	try {
		$rect = New-Object System.Drawing.Rectangle 0, $bars.Top, $img.Width, ($img.Height - $bars.Top - $bars.Bottom)
		$bmp = New-Object System.Drawing.Bitmap $rect.Width, $rect.Height
		$g = [System.Drawing.Graphics]::FromImage($bmp)
		$g.DrawImage($img, (New-Object System.Drawing.Rectangle 0, 0, $rect.Width, $rect.Height), $rect, [System.Drawing.GraphicsUnit]::Pixel)
		$g.Dispose()
		$file = Join-Path $script:ShotDir "$name.png"
		$bmp.Save($file, [System.Drawing.Imaging.ImageFormat]::Png)
		$bmp.Dispose()
		$file
	}
	finally { $img.Dispose(); Remove-Item $raw -ErrorAction SilentlyContinue }
}

function Texts { Ui | Where-Object { $_.Text -or $_.Desc } | ForEach-Object { "{0} | {1} @ {2},{3}" -f $_.Text, $_.Desc, [int]$_.X, [int]$_.Y } }
