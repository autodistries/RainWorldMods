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
    * automatic production of a .zip with the mod files
* ModName.cs
    * Main c# file
    * Enabled accessing private fields
    * Basic method hooking with `On.`
    * Hello world log statement


## TODO
* test it myself

## Usage
* run `dotnet restore` inside the main folder
* Fill in the mod info inside the first section of the csproj
* `dotnet build`
* run the game, enable the mod, and observe a "Hello World !" in the console

## Additional configuration
- If you change the name of the `.csproj`, make sure you also rename the main namespace to that !
    This is because PluginInfo.cs is built based on that.
- You can safely delete the .zip

## About Steam Workshop
- The preview image needs to be at most 976.5625 kilobytes (kB)
- The preview image aspect ratio should be 0.5616 < height/width < 0.5634, which the 16:9 aspect ratio respects