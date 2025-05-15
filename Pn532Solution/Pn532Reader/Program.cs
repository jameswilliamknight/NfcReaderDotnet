using System;
using System.Device.Spi;
using System.Threading;
using Iot.Device.Nfc.PN532; // Library for PN532
using Iot.Device.Nfc;    // Core NFC types

// --- SPI Configuration ---
// On Raspberry Pi, Bus 0, Chip Select 0 is common (/dev/spidev0.0)
// The Chip Select line is often configurable on the PN532 module itself
// via jumpers/switches. Ensure your module uses the correct CS pin
// (default CS0 on the Pi is GPIO8).
const int SpiBus = 0;
const int SpiChipSelectLine = 0; // Corresponds to CE0 on the Pi header (GPIO8)

// Configure SPI connection settings
SpiConnectionSettings connectionSettings = new SpiConnectionSettings(SpiBus, SpiChipSelectLine)
{
    ClockFrequency = 1_000_000, // PN532 often supports up to 5MHz, 1MHz is safe
    Mode = SpiMode.Mode0         // SPI Mode 0 or Mode 3 are typical for PN532
};

// --- Hardware Initialization ---
Console.WriteLine("Initializing PN532 reader...");
try
{
    using (SpiDevice spiDevice = SpiDevice.Create(connectionSettings))
    using (PN532 nfcReader = new PN532(spiDevice))
    {
        // Wake up the PN532
        nfcReader.Wakeup();
        Thread.Sleep(50); // Give it a moment after waking up

        // Get firmware version to confirm communication
        uint firmwareVersion = nfcReader.GetFirmwareVersion();
        Console.WriteLine($"PN532 Firmware Version: {firmwareVersion:X}");

        if (firmwareVersion == 0)
        {
             Console.ForegroundColor = ConsoleColor.Red;
             Console.WriteLine("Failed to communicate with PN532. Check wiring, SPI settings, and permissions.");
             Console.ResetColor();
             return; // Exit if we can't talk to the reader
        }

        Console.WriteLine("PN532 initialized successfully. Waiting for NFC tags...");

        // --- Reading Loop ---
        while (true)
        {
            // Try to list a passive target (NFC tag)
            // The timeout specifies how long the reader waits for a tag before returning
            NfcTag? tag = nfcReader.TryListPassiveTarget();

            if (tag != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n--- Tag Found! ---");

                // Display basic tag information
                Console.WriteLine($"Tag Type: {tag.GetType().Name}"); // e.g., NfcTypeA, NfcTypeV

                // Display the UID (Unique Identifier)
                if (tag.Uid != null && tag.Uid.Length > 0)
                {
                    Console.WriteLine($"UID: {BitConverter.ToString(tag.Uid).Replace("-", "")}");
                }
                else
                {
                    Console.WriteLine("Tag has no accessible UID.");
                }

                // You could add more logic here to read NDEF messages or other data
                // For example:
                // if (tag is NfcTypeA typeA)
                // {
                //     NdefMessage? ndefMessage = typeA.TryReadNdefMessage();
                //     if (ndefMessage != null)
                //     {
                //         Console.WriteLine("NDEF Message Found:");
                //         foreach (var record in ndefMessage.Records)
                //         {
                //             Console.WriteLine($"  Record Type: {System.Text.Encoding.UTF8.GetString(record.Type)}");
                //             // Display payload based on record type (e.g., Text, Uri)
                //         }
                //     }
                // }


                Console.WriteLine("------------------");
                Console.ResetColor();

                // Add a delay after reading a tag to avoid rapid re-reads
                Thread.Sleep(2000); // Wait 2 seconds before looking for the next tag
            }
            else
            {
                // No tag found, wait a short while before polling again
                Thread.Sleep(50); // Short delay to not busy-wait
            }
        }
    }
}
catch (UnauthorizedAccessException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Permission denied to access SPI device.");
    Console.WriteLine("Try running the application with 'sudo' or ensure the user is in the 'spi' and 'gpio' groups.");
    Console.WriteLine($"Error: {ex.Message}");
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
