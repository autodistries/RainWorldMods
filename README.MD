MoreLogs : logs more things to console

SleepySlugcat : adds cosmetics for sleeping

StepByStep : pause the game and continue one frame at a time

Mods Updater: an off-steam mods updater


.Plans
- (mod) TAS recording and replaying tool
    - Is there a need for this ? External tools probably already exist
    - File format
        - start with a room, or a wildcard for any room
        - each line is for a frame
        - actions are set to jump, throw, grab, up, down, left, right (and NOT keys)
        - jump5 will hold jump for 5 frames, regardless of what happens afterwards (maybe jump- should stop it ?)
        - jump will be understood as jump once
        - multiple actions on a line have to be separated by spaces
    - Record
         - A late and very big goal. Not the most important
         - UI: similar to pause. timeline, edit frame per frame. navigation using keys. jump to add an action
         - a "fast", copy inputs to file recording mode
    - Replay
        - Auto-play from a folder of scriptlets (trigger on rooms)
        - play a single file
        - play current scenario (from editing)
    - Others:
        - reload files easily
        - some kind of watermark that says TAS inputs or something
- (mod) universal fan translation
- (mod) ???
