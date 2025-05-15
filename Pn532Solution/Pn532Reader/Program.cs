using System;
using System.Device.Spi;
using Iot.Device.Pn532;
using Iot.Device.Pn532.ListPassive;
const int SpiBus = 0;
const int spiChipSelectLine = 0;

// Configure SPI connection settings
var connectionSettings = new SpiConnectionSettings(SpiBus, spiChipSelectLine)
{
    ClockFrequency = 1_000_000, // PN532 often supports up to 5MHz, 1MHz is safe
    Mode = SpiMode.Mode0,         // SPI Mode 0 or Mode 3 are typical for PN532
    DataFlow = DataFlow.MsbFirst // Set DataFlow based on typical examples
};

// --- Hardware Initialization ---
Console.WriteLine("Initializing PN532 reader...");
try
{
    // using declaration is correct here
    using var spiDevice = SpiDevice.Create(connectionSettings);
    using var nfcReader = new Pn532(spiDevice, -1, null, true, false);

    // Removed: nfcReader.Wakeup(); as this method doesn't exist in Iot.Device.Pn532.Pn532

    // Get firmware version to confirm communication
    // Access the FirmwareVersion property
    var firmwareVersion = nfcReader.FirmwareVersion;
    Console.WriteLine($"PN532 Firmware Version: {firmwareVersion.Version:X}");

    // Corrected comparison to check if the raw version number is 0
    // using explicit cast to potentially help Rider/compiler resolution
    if ((uint)firmwareVersion.Version.Build == 0)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Failed to communicate with PN532. Check wiring, SPI settings, and permissions.");
        Console.ResetColor();
        return; // Exit if we can't talk to the reader
    }

    Console.WriteLine("PN532 initialized successfully. Waiting for NFC tags...");

    // --- Reading Loop ---
    // Keep track of the last UID read to avoid repeatedly printing the same tag
    string? lastUid = null;
    var lastReadTime = DateTime.MinValue;
    var readDebounceTime = TimeSpan.FromSeconds(2); // Wait 2 seconds before re-reading the same tag

    while (true)
    {
        // Use the ListPassiveTarget method from the Pn532 class
        // This method returns bytes representing the response from the tag
         ReadOnlySpan<byte> response = nfcReader.ListPassiveTarget(
            maxTarget: MaxTarget.One, // Look for one tag
            TargetBaudRate.B106kbpsTypeA, // Target Type A at 106kbps
            // Corrected parameter name:
            [200] // Timeout in milliseconds
         );


        if (response.Length > 6) // Basic check: response should be long enough to contain UID info
        {
             // The UID typically starts at byte 6 and its length is at byte 5 for Type A [based on examples]
             int uidLength = response[5];
             if (response.Length >= 6 + uidLength)
             {
                 var uidSpan = response.Slice(6, uidLength);
                 var uid = uidSpan.ToArray();
                 var currentUid = BitConverter.ToString(uid).Replace("-", "");

                 // Debounce multiple reads of the same tag
                 var now = DateTime.UtcNow;
                 if (currentUid != lastUid || (now - lastReadTime) >= readDebounceTime)
                 {
                     Console.ForegroundColor = ConsoleColor.Green;
                     Console.WriteLine($"\n--- Tag Found! ---");

                     // Display the UID (Unique Identifier)
                     Console.WriteLine($"UID: {currentUid}");

                     // Note: The Iot.Device.Bindings PN532 implementation doesn't directly return
                     // a structured 'NfcTag' object like the older library might have.
                     // You get the raw response bytes, and you'd need further commands
                     // or parsing to get NDEF messages or other detailed tag info.
                     // For just the UID, parsing the ListPassiveTarget response is sufficient.

                     Console.WriteLine("------------------");
                     Console.ResetColor();

                     lastUid = currentUid;
                     lastReadTime = now;
                 }
             }
        }
        // Short delay to prevent busy-waiting when no tag is present
        Thread.Sleep(50);
    }
}
catch (UnauthorizedAccessException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Permission denied to access SPI device.");
    Console.WriteLine("Try running the application with 'sudo' or ensure the user is in the 'spi' and 'gpio' groups.");
    Console.ResetColor();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"An error occurred: {ex.Message}");
    Console.WriteLine(ex.ToString()); // Print stack trace for debugging
    Console.ResetColor();
}

Console.WriteLine("Application finished."); // This line will only be reached if an unhandled exception occurs
