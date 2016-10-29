[cmdletbinding(SupportsShouldProcess=$true)]
param($publishProperties=@{}, $packOutput, $pubProfilePath)

# to learn more about this file visit https://go.microsoft.com/fwlink/?LinkId=524327

try{
    if ($publishProperties['ProjectGuid'] -eq $null){
        $publishProperties['ProjectGuid'] = '0e5bc079-0252-43b5-a03b-bd0ba1ba23fa'
    }
	$publishModulePath = Join-Path (Split-Path $MyInvocation.MyCommand.Path) 'publish-module.psm1'
    Import-Module $publishModulePath -DisableNameChecking -Force

	# copy needed unix library to output folder
	cp $home\.nuget\packages\System.Runtime.InteropServices.RuntimeInformation\4.0.0\runtimes\unix\lib\netstandard1.1\System.Runtime.InteropServices.RuntimeInformation.dll $packOutput

    # call Publish-AspNet to perform the publish operation
    Publish-AspNet -publishProperties $publishProperties -packOutput $packOutput -pubProfilePath $pubProfilePath
}
catch{
    "An error occurred during publish.`n{0}" -f $_.Exception.Message | Write-Error
}