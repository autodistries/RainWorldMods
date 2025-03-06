The goal is to allow users to update their mods if they didn't install them from steam

From a user perspective, things are done from the remix interface :
- The mod version labels are repurposed to show info on the mod's status
- On each mod's remix preview, the mod status is plainly written and if applicable an "update" button is shown
- The remix stats menu offers data about the quantity of up-to-date/updatable/orphaned mods, and an "update all" button
- Once an update button has been clicked, the zip is put into ModsUpdater's folder
- Updates are applied during the next game start

On the background, 
- A mods database is pulled from RainDB near when the main menu appears.
    The same data is available with a front-end on [the dedicated website](https://raindb.net). 
    Thanks to Andrew for making the version info available !
    RainDB updates every two weeks. The file is only pulled if it has been updated on the server.
- Mods IDs are matched between local and remote mods, and their version is compared
- When the Remix menu is opened, each mod is hooked on constructor to include the update data
- All visuals are cleared when exiting the remix menu
- Updates are applied using a preloader
- Mods Updater uses its own mod folder to store related files. After being applied, zips are deleted from that folder
- Updates will delete and re-create the target mod folder


Here are a few common mod statuses :
- **Dev**: local mod version is greater than remote
- **Updatable**: remote mod version is greater than local !
- **Up to date**: local mod version is up-to-date with remote
- **Updated, needs restart**: an update is waiting for the game to restart to be applied
- **Orphan**: local mod has no matching remote mod
- **Managed by Steam**: local mod has been installed by steam, and therefore won't be updated by the Mods Updater
- **Has a preloader can't update**: Preloaders let code run very early. Once a dll is used, a c# app can not usually delete it from within. MergeFix is one of them. Mods Updater also suffers from this, sadly !
- **Uses sync can't update**: usually, zip updates are available by direct download. But with big mods, they are instead hosted on [sync.com](https://sync.com), a hosting service with no api support at all :c


## Plans
add custom url support (for mods published out-of workshop, or that are hosted on sync.com)


## ConsciousModsUpdater
Downloads, compares, shows UI


## PreStartUpdater
Contains the code for the preloader (unpacks mods on game start)


## Building
Run dotnet build from the ConsciousModsUpdater folder