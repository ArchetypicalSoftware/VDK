# VDK - Vega Development Kit

> These are interim notes for internal use. 

## Prerequisites
> This is only while we are in development phase - Once the repo is public it won't be necessary 
Run
```
echo >> ~/.bashrc && echo "export GITHUB_VDK_TOKEN=<YOUR_GHPAT>" >> ~/.bashrc
```

> Temporary: Before running the first time, run this command Working on integrating this into the process
```
echo >> ~/.bashrc && echo "export PATH=\"$PATH:/<PATH_TO_MY_REPO>/.bin\"" >> ~/.bashrc

# ex: echo >> ~/.bashrc && echo "export PATH=\"$PATH:/mnt/d/Code/Archetypical/VDK/.bin\"" >> ~/.bashrc
```

> **_NOTE_**: You can run the commands for your own shell script login (e.g .zshrc, .profile). Remember to source it if you dont exit the terminal:

```
source ~/.<your_shell_script_login>
```

> If you are under Mac OS Sequoia with a corporate security proxy (e.g.: Netskope) you may enconunter the following
> error when devbox is using nix to install dependencies: error: unable to download 'https://github.com/NixOS/nixpkgs/archive/75a52265bda7fd25e06e3a67dee3f0354e73243c.tar.gz': SSL peer certificate or SSH remote key was not OK (60)
> To address that issue follow the steps described [here](https://github.com/NixOS/nix/issues/8081#issuecomment-1962419263)
```
# First you generate a new bundle containing all your custom certificates to be used by nix

security export -t certs -f pemseq -k /Library/Keychains/System.keychain -o /tmp/certs-system.pem
security export -t certs -f pemseq -k /System/Library/Keychains/SystemRootCertificates.keychain -o /tmp/certs-root.pem
cat /tmp/certs-root.pem /tmp/certs-system.pem > /tmp/ca_cert.pem
sudo mv /tmp/ca_cert.pem /etc/nix/

# Update the conf file /etc/nix/nix.conf to reference the bundle

ssl-cert-file = /etc/nix/ca_cert.pem

# Relaunch the daemon

sudo launchctl unload /Library/LaunchDaemons/org.nixos.nix-daemon.plist
sudo launchctl load /Library/LaunchDaemons/org.nixos.nix-daemon.plist

```


## Getting Started

- Install DevBox

```
    curl -fsSL https://get.jetify.com/devbox | bash
```
- Clone the repository `git clone https://github.com/ArchetypicalSoftware/VDK.git`
- Start DevBox session in the cloned repository
```
    devbox shell
```
- Run `vega --help` to see the available commands


