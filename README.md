# TUO-Launcher

A launcher for [TazUO](https://github.com/PlayTazUO/TazUO). Easily play multiple shards, different accounts, different UO versions, all while staying up to date with TazUO.

## Installation

Download the latest release for your platform:

- **Windows/Linux:** unzip the matching platform ZIP and run the launcher.
- **macOS app:** unzip the `TazUO-Launcher.osx-*.app.zip` asset and open `TazUOLauncher.app`.

The macOS app is not signed or notarized. On first launch, Control-click (or right-click) `TazUOLauncher.app`, choose **Open**, then confirm the prompt. This is only needed the first time.
*If the only option given is to remove the file, you can run this in terminal to remove it from quarantine: `xattr -r -d com.apple.quarantine ./TazUOLauncher.app`*

The app stores profiles, settings, and its downloaded TazUO client in `~/Library/Application Support/TazUO Launcher/`. Use the `.app.zip` for new installs; the flat macOS ZIP is retained for existing portable installs. To migrate data from an older install, copy `launcherdata.json`, `Profiles/`, and `TazUO/` from the old launcher folder into the Application Support folder above. The app also imports these files automatically when it finds the old portable data beside the app bundle.

If the app is installed in a protected location such as `/Applications` and cannot update itself, it opens the release page so the app can be replaced manually.
