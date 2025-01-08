The goal is to allow users to update their mods if they didn't install them from steam

Things are done from the remix interface

- Mods installed via Wrekshop are NOT taken into account and will not be updated
- On the Remix Stats menu, some stats are added about currently installed mods + an update all button
- On each mod, a lil infobox to explain current status + an update button if updatable
- Updates are pulled from raindb (syncs with workshop every two weeks) (local file is updated whenever an update is available)
- Updates are downloaded when the game is alive
- Updates are applied when the game starts
- temporary files are stored in this mods' folder and deleted after being used
- updates will empty the target mod folder, and fill it with the previously downloaded zip
- enabled mods will stay enabled after update
- Mods with preloader patches will fail to update as their dlls are busy at the time updates are applied (so you can't update this mod, or MergeFix that way (yet?), sorry)



## ConsciousModsUpdater
 This is the part that handles everything the user can see.
- When clicking an update btn, the updates are downloaded and stored to be applied on next game start

## PreStartUpdater
- This part handles deleting previous mod folder, and unpacking previously downloaded zips


## Building

Run dotnet build from ConsciousModsUpdater