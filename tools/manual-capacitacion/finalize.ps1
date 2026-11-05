param(
  # .docx a procesar. Si se omite, procesa todos los .docx de la carpeta out\.
  [string]$Docx,
  # Genera ademas un PNG por pagina en out\pages\ para revisar el resultado sin abrir Word.
  [switch]$Preview
)
# Abre el .docx con Word (COM): actualiza el indice y las referencias "pag. X", guarda y exporta a PDF.
# Requiere Windows con Microsoft Word instalado.
$ErrorActionPreference = 'Stop'
$outDir = Join-Path $PSScriptRoot 'out'
$files = if ($Docx) { @((Resolve-Path $Docx).Path) } else { @(Get-ChildItem $outDir -Filter *.docx | Where-Object { $_.Name -notlike '~$*' } | ForEach-Object { $_.FullName }) }
if (-not $files) { throw "No hay .docx en $outDir. Ejecute primero: node build-manual.js" }

$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
$pdfs = @()
try {
  foreach ($f in $files) {
    $pdf = [IO.Path]::ChangeExtension($f, '.pdf')
    $doc = $word.Documents.Open($f, $false, $false)
    # Dos pasadas: el indice cambia la paginacion y las referencias dependen de ella.
    1..2 | ForEach-Object {
      foreach ($toc in $doc.TablesOfContents) { $toc.Update() }
      $doc.Fields.Update() | Out-Null
      $doc.Repaginate()
    }
    $doc.Save()
    # 17 = PDF; el ultimo argumento crea marcadores del PDF a partir de los titulos.
    $doc.ExportAsFixedFormat($pdf, 17, $false, 0, 0, 1, 1, 0, $true, $true, 1)
    "{0}: {1} paginas -> {2}" -f (Split-Path $f -Leaf), $doc.ComputeStatistics(2), $pdf
    $doc.Close($false)
    $pdfs += $pdf
  }
} finally { $word.Quit() }

if (-not $Preview) { return }

# Vista previa: render de cada pagina del PDF con Windows.Data.Pdf (WinRT).
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$ext = [System.WindowsRuntimeSystemExtensions].GetMethods()
$asTask = ($ext | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
$asAction = ($ext | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncAction' })[0]
function Await($op, [Type]$t) { $task = $asTask.MakeGenericMethod($t).Invoke($null, @($op)); $task.Wait(); $task.Result }
[void][Windows.Storage.StorageFile, Windows.Storage, ContentType = WindowsRuntime]
[void][Windows.Data.Pdf.PdfDocument, Windows.Data.Pdf, ContentType = WindowsRuntime]
[void][Windows.Storage.Streams.InMemoryRandomAccessStream, Windows.Storage.Streams, ContentType = WindowsRuntime]

$pagesDir = Join-Path $outDir 'pages'
New-Item -ItemType Directory -Force $pagesDir | Out-Null
Get-ChildItem $pagesDir -Filter *.png | Remove-Item -Force
foreach ($pdf in $pdfs) {
  $prefix = [IO.Path]::GetFileNameWithoutExtension($pdf)
  $file = Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync($pdf)) ([Windows.Storage.StorageFile])
  $pdoc = Await ([Windows.Data.Pdf.PdfDocument]::LoadFromFileAsync($file)) ([Windows.Data.Pdf.PdfDocument])
  for ($i = 0; $i -lt $pdoc.PageCount; $i++) {
    $stream = New-Object Windows.Storage.Streams.InMemoryRandomAccessStream
    $opts = New-Object Windows.Data.Pdf.PdfPageRenderOptions
    $opts.DestinationWidth = 850
    $t = $asAction.Invoke($null, @($pdoc.GetPage($i).RenderToStreamAsync($stream, $opts))); $t.Wait()
    $net = [System.IO.WindowsRuntimeStreamExtensions]::AsStreamForRead($stream.GetInputStreamAt(0))
    $fs = [IO.File]::Create((Join-Path $pagesDir ('{0}-{1:D2}.png' -f $prefix, ($i + 1))))
    $net.CopyTo($fs); $fs.Close(); $net.Close()
  }
  "Vista previa: $($pdoc.PageCount) paginas en $pagesDir"
}
