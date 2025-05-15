# Device Info

## Running on Raspberry Pi

1.  **Publish:** Build for your Pi's architecture (e.g., `linux-arm64`) using `dotnet publish -c Release -r linux-arm64 --self-contained true`. The resulting files are in `bin/Release/netX.Y/linux-arm64/publish/`.
2.  **Transfer:** Copy the *entire contents* of the `publish` folder from your development machine to a directory on your Raspberry Pi (e.g., `/home/pi/Pn532ReaderApp`).
3.  **Run:** Open a terminal on the Raspberry Pi, navigate to the folder, and run the executable file (`Pn532Reader`).
    ```bash
    cd /path/to/Pn532ReaderApp
    ./Pn532Reader
    ```
    *Note: Accessing hardware like SPI typically requires elevated permissions. If you encounter "Permission denied" errors, try running with `sudo`:*
    ```bash
    sudo ./Pn532Reader
    ```

## Builds

Find them here, I'll just manually copy for now, so I can test on my Raspberry Pi

https://drive.google.com/drive/folders/1u1YBYCpNvd-YixzTHfKHhJYKswQ00RuY?usp=drive_link

