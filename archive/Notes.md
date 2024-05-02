# VDK

## Getting Started

```
git clone (REPOSITORY)
cd (REPOSITORY)
build.ps1
add-pack-source.ps1
choco install vdk -s pack -y
```

### Notes

Creating a Chocolatey package involves several steps, from setting up your environment to creating and testing the package itself. Here's a step-by-step guide to get you started:

### 1. Install Chocolatey
First, you need to have Chocolatey installed on your machine. You can install Chocolatey by running the following command in an administrative PowerShell window:

```powershell
Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
```

### 2. Set Up Your Package Development Environment
- **Install Chocolatey Package Builder:** This is a helpful tool for creating and testing Chocolatey packages. You can install it using Chocolatey by running `choco install chocolatey-package-builder`.
- **Create a Package Folder:** This is where you'll store the files related to your package. You can name it after your package for convenience.

### 3. Create a New Package
You can create a basic package structure using the `choco new` command. Run the following command in PowerShell, replacing `your-package-name` with the actual name of your package:

```powershell
choco new your-package-name
```

This command creates a new directory with the name of your package and populates it with a template for your package files.

### 4. Edit the Package Files
Inside your package directory, you'll find several files. The two most important ones are:

- **your-package-name.nuspec:** This is the manifest file for your package. It contains metadata like the package's version, authors, and dependencies. You'll need to edit this file to correctly describe your package.
- **chocolateyinstall.ps1 or chocolateyuninstall.ps1:** These PowerShell scripts are where you define the installation and uninstallation logic for your package. For most packages, you'll at least need to edit the `chocolateyinstall.ps1` script to include the correct installation commands.

### 5. Build Your Package
Once you've edited the package files, you can build your package using the `choco pack` command. Run this command from within your package directory:

```powershell
choco pack
```

This command creates a `.nupkg` file, which is your Chocolatey package.

### 6. Test Your Package
Before publishing, you should test your package to ensure it installs and uninstalls correctly. You can do this by running:

- **To Test Installation:** `choco install your-package-name -dv -s '.'`
- **To Test Uninstallation:** `choco uninstall your-package-name -dv`

The `-dv` flag stands for "debug verbose," and the `-s '.'` specifies the source as the current directory.

### 7. Publish Your Package
After testing, you can publish your package to the Chocolatey community repository. First, you'll need to register for an account on the [Chocolatey website](https://chocolatey.org/) and obtain an API key. Then, you can publish your package using:

```powershell
choco push your-package-name.nupkg --api-key=your-api-key
```

Replace `your-package-name.nupkg` with the name of your package file and `your-api-key` with your actual API key.

### Additional Resources
For more detailed instructions and advanced packaging techniques, refer to the official Chocolatey documentation on their website. There, you'll find a wealth of information on creating packages, including handling dependencies, versioning, and more complex installation scripts.


--- 
# Documentation Template

#### Command

##### Description

##### Syntax
```

```

##### Parameters

##### Example
```
```