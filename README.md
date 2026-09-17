# Approach R10 Windows

A native Windows desktop shot tracker for the Garmin Approach R10.

## Current features

- Windows desktop UI
- Direct Bluetooth connection to a paired Approach R10
- R10 BLE handshake and protobuf transport
- R10 model, firmware and battery display
- Live shot reception
- Club speed, ball speed, launch angle, launch direction, spin and attack angle display
- Duplicate shot filtering
- Self-contained Windows x64 build from GitHub Actions

The Bluetooth/protocol layer is based on the public reverse-engineered work in `mholow/gsp-r10-adapter`, which documents direct Windows Bluetooth operation with a paired R10. The R10 protocol is unofficial and reverse-engineered.

## Using it

1. Turn on the R10 and put it into Bluetooth pairing mode.
2. In Windows 11, open **Settings → Bluetooth & devices → Add device → Bluetooth** and pair **Approach R10**.
3. Download the `ApproachR10-Windows-x64` artifact from the successful GitHub Actions run.
4. Run `ApproachR10.exe`.
5. Click **Connect R10**.
6. Hit shots. They should appear in the table as the R10 sends them.

## Development

Requires .NET 8 SDK and Windows. The project is built automatically on every push with a Windows GitHub Actions runner. A successful build produces a self-contained x64 artifact.

## Roadmap

- Proper carry/total distance calculation from R10 data
- Club selection and session management
- CSV/JSON export
- Camera recording and shot/video linking
- Practice-swing analysis
- Charts and dispersion plots
- Better reconnect handling
- Installer

## Credits / licensing

The R10 BLE transport and protocol are based on public reverse-engineered work by mholow/gsp-r10-adapter. See that project's MIT license and the source attribution in this repository.
