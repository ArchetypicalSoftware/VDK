# /bin/bash
if groups $USER | grep -q "\bdocker\b"; then
    echo "Welcome to Vega VDK"
    # Variables
    REPO="ArchetypicalSoftware/VDK" # Replace with your GitHub repository (e.g., username/repo)
    TOKEN=$GITHUB_VDK_TOKEN # Replace with your GitHub Personal Access Token
    ASSET_NAME="vega-linux-x64.tar.gz" # Replace with the asset name you're looking for
    DOWNLOAD_DIR="./.bin" # Specify the download directory

    # Get the latest release information
    echo "Fetching the latest release information..."
    LATEST_RELEASE=$(curl -s -H "Authorization: token $TOKEN" "https://api.github.com/repos/$REPO/releases/latest")
    VERSION=$(echo "$LATEST_RELEASE" | jq -r ".tag_name")
    CURRENT=$(cat ./.bin/vdk.version)
    if [ "$VERSION" != "$CURRENT" ]; then
        # Extract the asset ID for the desired asset
        ASSET_ID=$(echo "$LATEST_RELEASE" | jq -r ".assets[] | select(.name == \"$ASSET_NAME\") | .id")

        if [ -z "$ASSET_ID" ]; then
            echo "Error: Asset \"$ASSET_NAME\" not found in the latest release."
            exit 1
        fi

        # Create the download directory if it doesn't exist
        mkdir -p "$DOWNLOAD_DIR"

        # Download the asset
        echo "Downloading asset \"$ASSET_NAME\"..."
        curl -L -H "Authorization: token $TOKEN" \
        -H "Accept: application/octet-stream" \
        "https://api.github.com/repos/$REPO/releases/assets/$ASSET_ID" \
        -o "$DOWNLOAD_DIR/$ASSET_NAME"

        echo "Download complete! File saved to \"$DOWNLOAD_DIR/$ASSET_NAME\""  
        echo "Extracting Vega CLI..."
        tar --overwrite -xvf "$DOWNLOAD_DIR/$ASSET_NAME" -C "$DOWNLOAD_DIR"
        mv -f "$DOWNLOAD_DIR/packages/build/linux-x64/vega" ./.bin/vega
        rm -rf "$DOWNLOAD_DIR/packages"
        rm "$DOWNLOAD_DIR/$ASSET_NAME"
        echo "$VERSION" > "$DOWNLOAD_DIR/vdk.version"
        echo "Version: $VERSION" 
    else
        echo "Version: $CURRENT"
    fi
    cd ./.bin
    BIN_PATH=$(pwd)
    cd ..
    # echo "$PATH" | grep -q $BIN_PATH
    # if [ $? -ne 0 ]; then
    #     echo "Updating Path"
    #     echo >> ~/.bashrc && echo "export PATH='$PATH:$BIN_PATH'" >> ~/.bashrc && source ~/.bashrc
    # fi
else
    echo "Adding user $USER to docker group"
    echo " (This will require sudo access)"
    sudo usermod -aG docker "$USER"
    echo "Please exit and restart your shell to pick up group membership changes."
fi

# sudo gpasswd -d username groupname
# sudo gpasswd -d $USER docker