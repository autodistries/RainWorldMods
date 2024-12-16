## This is a preview mod.  

## Goal
The goal is to allow users to update their mods if they didn't install them from steam (hello, gog players)

## What's done
- Inside the remix menu, by selecting the info button, get count of mods and their status, and a button to update them all
- ability to pull mods from the workshop using raindb.js, pulled from AndrewFM's RainDB

## What remains to be done
- Support for custom sources, based on a url inside `modinfo.json`
- Settings for this mod, including:
    - Frequency of refreshes
    - Forcing a refresh
    - Preferring raindb over custom sources
- Write somewhere when restart is needed

## Broken right now
- The versions of `raindb.js` are frozen to the first time they ever appeared (or at least, are not updating properly)

## Some more info
- `raindb.js` is the file used for getting workshop data. It is updated every two weeks auomatically.

## Thanks
To AndrewFM, whom nicely put a `version` attribute for mods inside `raindb.js`

Here how it would be made:
 On menu open, check internet connection (or check only when trying to fetch blob)
 if connected & update is available, auto-download the updated file

 update mods list and sort them:
 - All
 - Update available
 - Not found

 Update all button will update updateable, then prompt a game restart
