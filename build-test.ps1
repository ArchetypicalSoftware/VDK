dotnet build ./cli/src/Vdk/Vdk.csproj -c Release
dotnet publish ./cli/src/Vdk/Vdk.csproj -o ./packages/build/linux-arm64 -r linux-arm64 -c Release -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:PublishReadyToRunShowWarnings=true -p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true -p:selfcontained=true
mkdir ./packages/build/linux-arm64/ConfigMounts -ErrorAction SilentlyContinue
mkdir ./packages/build/linux-arm64/Certs -ErrorAction SilentlyContinue
copy-item ./Certs ./packages/build/linux-arm64/Certs -Recurse -Force
copy-item ./cli/src/Vdk/ConfigMounts/hosts.toml ./packages/build/linux-arm64/ConfigMounts -Recurse -Force
#copy ./packages/build/linux-arm64/* .\.bin\