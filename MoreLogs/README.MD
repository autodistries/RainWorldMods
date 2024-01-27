This (very simple) Rain World mod makes the game log more things to console.
You then can view logs live as the game runs !

## Why ?
Before Debug.Logging something, the game often checks if it should log things in the first place and only does it if RainWorld.ShowLogs is true.
With this mod, logs viewable with DevTools K shortcut are always being logged.
Running the game under Proton from GNU/Linux in terminal shows logs in terminal by default; this mod makes it so that the game logs more things. I like my game logs verbose :3
I do not know for windows, but I will try to make something in the future !

## Tech
This mod replaces property RainWorld.ShowLogs to make it always return true, instead of being the result of bool ModManager.DevTools (aka did you enable DevTools in-game)
It is using [MonoMod RuntimeDetour](https://rainworldmodding.miraheze.org/wiki/MonoMod_RuntimeDetour) to replace the property.

## Installing
For your convenience it is already built.
Copy the MoreLogs folder from this repo into Rain World's mods folder; restart and activate the mod in Remix menu

## Building
You'll need a well set-up .NET environnment.
Then you can simply build the project (dotnet build for linux).
Output dll is located at ./MoreLogs/plugins/MoreLogs.dll

## Testing
The mod was tested with game version 1.9.07b

