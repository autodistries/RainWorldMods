This is a Rain World mod template.
Works best for gnu/linux systems.
Works on my machine (tm), using a visual studio code fork (vscodium)

## Default features
* preconfigured csproj
    * Mod properties (ID, name, version, desc and author) to be set once for all, applied everywhere
    * References based on the Rain World top folder, every library from Managed/ and BepInEx/*/ is loaded
    * Automatic modinfo.json on build
    * Automatic generation of associated PluginInfo
    * Automatic creation of Mod folder/file structure on build
    * Automatic overwriting of mod in the game files on build
* ModName.cs
    * Main c# file, with basic file structure


## TODO
* Is my nuget necessary here
* test it myself

## Usage
* run `dotnet restore` inside the main folder
* Fill in the mod info inside the first section of the csproj
* `dotnet build`
* run the game, enable the mod, and observe a "Hello World !" in the console