function New-VdkCluster  {
    [CmdletBinding()]
    param (        
        [Parameter(Mandatory=$false, Position=0)]
        [string] $Name = "vdk",
        [Parameter(Mandatory=$false, Position=1)]
        [int] $WorkerNodes = 2,
        [Parameter(Mandatory=$false, Position=2)]
        [int] $ControlPlaneNodes = 1,
        [Parameter(Mandatory=$false, Position=3)]
        [string] $KubeVersion = $null
    )
    process {
        $tmp = [System.IO.Path]::GetTempPath()
        $config = Join-Path $tmp "$Name.yaml"
        New-KindManifest -KubeVersion $KubeVersion -WorkerNodes $WorkerNodes -ControlPlaneNodes $ControlPlaneNodes | Out-File $config
        kind create cluster -n $Name --config $config        
        Initialize-Flux
    }
}

function Get-KindVersion {
    process{
        $v = kind --version       
        return $v.Trim().Substring(12).Trim()
    }
}

function Get-VdkConfigLocation {
    process {
        return Join-Path $env:USERPROFILE .vdk\config
    }
}

function Get-VdkKindDataLocation {
    process {
        $path = Get-VdkConfigLocation
        return Join-Path $path kind-version-data.json
    }
}

function Get-VdkKindData {
    process {
        $path = Get-VdkKindDataLocation
        return Get-Content $path | ConvertFrom-Json
    }
}

function Get-KindImage {
    param (
        [Parameter(Mandatory=$true, Position=0)]
        [string]$KindVersion,
        [Parameter(Mandatory=$false, Position=0)]
        [string]$KubeVersion = $null
    )
    process {
        $map = Get-VdkKindData
        $k = $map | Where-Object {$_.Name -eq $KindVersion}
        if($null -eq $k){
            return $null
        }
        $images = $k.Images
        if($null -eq $KubeVersion){
            $image = $images | Sort-Object -Property Version -Descending | Select-Object -First 1
            return $image.Image
        }
        $image = $images | Where-Object {$_.Version.StartsWith($KubeVersion)} | Sort-Object -Property Version -Descending | Select-Object -First 1
        return $image.Image
    }
}

function New-KindManifest {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$false, Position=0)]
        [string] $KubeVersion = $null,
        [Parameter(Mandatory=$false, Position=1)]
        [int] $WorkerNodes = 2,
        [Parameter(Mandatory=$false, Position=2)]
        [int] $ControlPlaneNodes = 1
    )
    process {
        $kindVersion = Get-KindVersion
        $kindImage = Get-KindImage -KindVersion $kindVersion -KubeVersion $KubeVersion
        $manifest = @'
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
nodes:

'@
        for($index=0; $index -lt $ControlPlaneNodes; $index++){
            $manifest = $manifest + @"  
- role: control-plane
  image: $kindImage
  kubeadmConfigPatches:
  - |
      kind: InitConfiguration
      nodeRegistration:
      kubeletExtraArgs:
          node-labels: "ingress-ready=true"

"@
        }    
        for($index=0; $index -lt $WorkerNodes; $index++){
            $manifest = $manifest + @"
- role: worker
  image: $kindImage

"@
        }
        return $manifest
    }
}

function Remove-VdkCluster {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true, Position=0)]
        [string] $ClusterName
    )
    process {
        kind delete cluster --name $ClusterName
    }
}

function Initialize-Flux {
    process{
        flux bootstrap github --owner=$env:GITHUB_USER --repository=kind-flux --branch=main --path=./clusters/default --personal
    }
}

Export-ModuleMember -Function New-VdkCluster
Export-ModuleMember -Function Get-VdkConfigLocation
Export-ModuleMember -Function Get-VdkKindDataLocation
Export-ModuleMember -Function Get-VdkKindData 
Export-ModuleMember -Function Get-KindVersion
Export-ModuleMember -Function Get-KindImage
Export-ModuleMember -Function Initialize-Flux
Export-ModuleMember -Function Remove-VdkCluster
