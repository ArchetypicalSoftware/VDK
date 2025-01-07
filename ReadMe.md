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


