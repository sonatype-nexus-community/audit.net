# PowerShell: Make sure nuget is in your path, THEN:
#	Set-ExecutionPolicy RemoteSigned
#	C:\GitHub\audit.net\audit.net\src\NugetAuditor\NugetAuditor.MSBuild\NugetPublish.ps1 

$target = "C:\GitHub\audit.net\audit.net\src\NugetAuditor\NugetAuditor.MSBuild\NugetAuditor.MSBuild.csproj"
$nugetDir = "C:\NugetStore"

#create a temp dir to store the packages
$outputDir = Join-Path $env:temp ([guid]::NewGuid().ToString())
New-Item $outputDir -type directory

#create nuget packages
Write-Host "creating nuget package" -foregroundcolor Cyan
&nuget pack "$target" -Prop Configuration=Release -OutputDirectory "$outputDir" -Build


#copy packages to nuget folder
foreach ($file in Get-ChildItem $outputDir | Where-Object { ( $_.Name -like "*.nupkg") })
{
    if (!(Test-Path (Join-Path $nugetDir $file)))
    {
        Copy-Item (Join-Path $outputDir $file) $nugetDir
        Write-Host $file.name "copied to $nugetDir." -foregroundcolor Green
    }
    else
    {
        Write-Host $file.name "already exists in nuget folder.  Skipping file." -foregroundcolor Red
    }
}

# remove-item $outputDir -recurse

