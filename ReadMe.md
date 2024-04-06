# VDK

## Installed Tools

- docker-desktop version 4.28.0
- kind version 0.22.0
- kubernetes-cli version 1.29.1
- kubernetes-helm version 3.14.2
- k9s version 0.32.4
- flux version 2.2.3
- VDK-Tools version 0.1.0

## VDK-Tools Module

VDK-Tools PowerShell Module provides a set of PowerShell CmdLets to make it simple to spin up local kubernetes clusters (running on kind) and manage them.

### How to Use 
>NOTE: Some of the documented CmdLts below are intended to be internal, but are currently exposed publicly during initial development to make it easier to test and troublehoot.

---

#### New-VdkCluster

##### Description
Creates a new cluster.

##### Syntax
```
    New-VdkCluster -Name <ClusterName> -WorkerNodes <X> -ControlPlaneNodes <Y> -KubeVersion <Version>
```

##### Parameters

- Name: _(default: vdk)_ The name of the cluster to be created.  (Must be uniqiue)

- WorkerNodes: _(default: 2)_ Number of worker nodes in the cluster

- ControlPlaneNodes: _(default: 1)_ Number of control plane nodes in the cluster

- KubeVersion: _(default: latest)_ The kubernetes version to run.  This can be any version for which there is a published image available for the version of Kind running on the localmachine.  When not specified the system will find the latest version supported by your version of kind.  Format: major.minor (ex.  1.29)

##### Example
```
    New-VdkCluster -Name test-cluster -WorkerNodes 3 -ControlPlaneNodes 2 -KubeVersion 1.28
```

---

#### Get-VdkConfigLocation

##### Description
Gets the location of the VDK configuration directory.  Typically `C:\Users\<UserName>\.vdk` (This is really for internal use)

##### Syntax
```
    Get-VdkConfigLocation
```

##### Parameters

- None

##### Example
```
    Get-VdkConfigLocation    
```

---

#### Get-VdkKindDataLocation

##### Description
Gets the location of the installed kind verion data file.

##### Syntax
```
    Get-VdkKindDataLocation
```

##### Parameters

- None

##### Example
```
    Get-VdkKindDataLocation
```

---

#### Get-VdkKindData 

##### Description
Reads/Returns the Kind version data as an parsded object to be used in mapping kind version to kubenetes images.

##### Syntax
```
    Get-VdkKindData
```

##### Parameters

- None

##### Example
```
    Get-VdkKindData
```

---

#### Get-KindVersion

##### Description
Gets the current version of kind installed on the system.

##### Syntax
```
    Get-KindVersion
```

##### Parameters

- None

##### Example
```
    Get-KindVersion
```

---

#### Get-KindImage

##### Description

Gets the image for a given Kind and Kubernetes version.  If the Kubernetes version is ommitted it will return the image for the latest supported version of Kuberntes for the given version of Kind.  (KubeVersion is the kubernetes api version number ommiting the patch level in the format major.minor.  The returned image will always be the latest patch of the given api version)

##### Syntax
```
    Get-KindImage -KindVersion <VersionString> -KubeVersion <KubernetesVersion>
```

##### Parameters

- KindVerion: _(required)_ The version string of kind in major.minor.patch format.
- KubeVersion _(default: max kubenetes version) The version of kubernetes.

##### Example
```
    Get-KindImage -KindVersion 0.22.0 -KubeVersion 1.29
```

---

#### Initialize-Flux

##### Description
Initializes flux on the current cluster.  _This will be expanded in some way to allow user to specify a repo or at minimum a directory_


##### Syntax
```
    Initialize-Flux
```

##### Parameters

- None

##### Example
```
    Initialize-Flux
```

---

#### Remove-VdkCluster

##### Description

##### Syntax
```
    Remove-VdkCluster -Name <ClusterName>
```

##### Parameters

- Name: _(Required) The name of the cluster to remove/delete.

##### Example
```
    Remove-Cluster -Name vdk
```


## Getting Started - Installation

>Note: This is currently a bit manual, but is intended to use nuget to host the packages and PowerShell modules eventually to make this much simpler.

### Build and Install

```
git clone (REPOSITORY)
cd (REPOSITORY)
build.ps1
add-pack-source.ps1
choco install vdk -s pack -y
```


##### Description

##### Syntax
```

```

##### Parameters

##### Example
```
```
