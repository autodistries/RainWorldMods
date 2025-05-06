using BepInEx;
using Menu;

using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.IO;



// Access private fields and stuff
using System.Security.Permissions;
using System.Threading.Tasks;
using UnityEngine;
using static Menu.InitializationScreen;
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace BackupTools; //this needs to be changed accordingly, if you rename ModName.csproj ! Explanations on top of ModName.csproj

// These properties are properly set when building, so errors are to be expected in your IDE, but building will occur properly.
// If not, please check the .csproj !
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]


public partial class BackupTools : BaseUnityPlugin
{

    bool doneWithInit = false;
    public static bool noAutoBackups = false;
    public static bool noAutoDelete = false;

    MenuLabel backupsInfoLabel;
    MenuLabel backupsPagesInfoLabel;

    ModOptions options;

   public static int userBackups = 0;
    public static int autoBackups = 0;

    public static bool ILFailed = true;
    private InitializationStep cstep;
    public static  string ILFailedText => $"-------WARNING--------\n\nThe patch from \"{PluginInfo.PLUGIN_NAME}\" that lets you have 50> automatic saves has failed to apply.\n\nThis is either caused by a game code update, or another mod messing with us.\n\nThis means each automatic backup will delete the oldest automatic backup.\nYou should try to update this mod, or manually backup your backups (recommended), or disable automatic backups.\n\nPlease also tell me that happened, so I can fix it c:\nP.S. you had that option {(noAutoDelete?"en":"dis")}abled";

    public DialogBoxNotify ILFailedWarningDialog { get; private set; }

    private bool warningDissmissed=false;
    private bool didGetOurCurrentSettings=false;

    public BackupTools()
    {
    }

    private void OnEnable()
    {
        if (doneWithInit) return;
        options = new ModOptions(Logger);
        Logger.LogInfo($"Backup Tools v" + PluginInfo.PLUGIN_VERSION);

        On.RainWorld.OnModsInit += IMakeARemixInterface;

        On.PlayerProgression.CreateCopyOfSaves_bool += IPreventAutomaticBackups;

        IL.PlayerProgression.CreateCopyOfSaves_bool += IPreventDeletingSaves;

        On.Menu.BackupManager.Singal += IMakeBackupsButtonsCooler;

        On.Menu.BackupManager.ctor += IAddInfoTextToBackupScreen;

        On.Menu.BackupManager.PopulateButtons += IUpdateTheInfoTextWhenButtonsRefresh;

        On.Menu.InitializationScreen.Update += IAmABigWarningSignWhenILFails;
        On.Menu.InitializationScreen.Singal += ICloseTheWarning;

        doneWithInit = true;
    }

    private void ICloseTheWarning(On.Menu.InitializationScreen.orig_Singal orig, InitializationScreen self, MenuObject sender, string message)
    {
        if (message == "ILFAILEDOK") {
            if (ILFailedWarningDialog != null) {
                self.pages[0].subObjects.Remove(ILFailedWarningDialog);
				ILFailedWarningDialog.RemoveSprites();
				ILFailedWarningDialog = null;
                warningDissmissed=true;
            }
        }
        orig(self, sender, message);
    }

    private void IAmABigWarningSignWhenILFails(On.Menu.InitializationScreen.orig_Update orig, InitializationScreen self)
    {
        
        // if (cstep != self.currentStep)
        // {
        //     Logger.LogInfo("Initialization STep: " + self.currentStep);
        //     cstep = self.currentStep;
        //     Logger.LogInfo("Info, cvalues " + options.shouldCancelAutoBackups.Value + "," + options.shouldPreventDelAutoBackups.Value);

        // }
        if (self.currentStep == InitializationStep.WAIT_STARTUP_DIALOGS)
        {
            if (!didGetOurCurrentSettings) {
            // Logger.LogInfo("setting out values " + options.shouldCancelAutoBackups.Value + "," + options.shouldPreventDelAutoBackups.Value);

            noAutoBackups = options.shouldCancelAutoBackups.Value;
            noAutoDelete = options.shouldPreventDelAutoBackups.Value;
            didGetOurCurrentSettings = true;
        }
            if (ILFailed && noAutoDelete && !warningDissmissed)
            {
                if (ILFailedWarningDialog == null)
                {
                    Logger.LogInfo("Adding warning box!!");
                    ILFailedWarningDialog = new DialogBoxNotify(self, self.pages[0], ILFailedText, "ILFAILEDOK", new Vector2(self.manager.rainWorld.options.ScreenSize.x / 2f - 240f + (1366f - self.manager.rainWorld.options.ScreenSize.x) / 2f, 224f), new Vector2(480f, 320f));
                    self.pages[0].subObjects.Add(ILFailedWarningDialog);
                    ILFailedWarningDialog.timeOut = 0f;
                }
                // else
                // {
                //     Logger.LogInfo("Updated warning box");
                //     // ILFailedWarningDialog.Update();
                // }
            }
        }
        if (ILFailedWarningDialog != null)
        {
            self.currentStep = InitializationStep.WAIT_STARTUP_DIALOGS;
        }
        orig(self);
    }

    /*
						gameVersionChangedDialog = new DialogBoxNotify(this, pages[0], Translate("remix_game_version"), "VERSIONPROMPT", new Vector2(manager.rainWorld.options.ScreenSize.x / 2f - 240f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, 224f), new Vector2(480f, 320f));
//void InitializationScreen.Singal(MenuObject sender, string message)



else if (currentStep == InitializationStep.WAIT_STARTUP_DIALOGS)
			{
				if (!manager.IsRunningAnyDialog)
				{
					currentStep = InitializationStep.WRAP_UP;
				}
			}

    */

    private void IUpdateTheInfoTextWhenButtonsRefresh(On.Menu.BackupManager.orig_PopulateButtons orig, BackupManager self)
    {
        orig(self);
        if (backupsInfoLabel != null)  UpdateInfoText(self);
    }

    private void IAddInfoTextToBackupScreen(On.Menu.BackupManager.orig_ctor orig, Menu.BackupManager self, ProcessManager manager)
    {
        orig(self, manager);
        backupsInfoLabel = new MenuLabel(self, self.pages[0], "INFOTEXT AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", new Vector2(1366f / 2f, 423f), new Vector2(0f, 480f), bigText: false);
        backupsInfoLabel.label.color = MenuColorEffect.rgbMediumGrey;
        self.pages[0].subObjects.Add(backupsInfoLabel);

        backupsPagesInfoLabel = new MenuLabel(self, self.pages[0], $"Page {self.pageNum + 1}/{self.TotalPages}", new Vector2(500f, 45f), new Vector2(0f, 100f), bigText: false);
        // backupsPagesInfoLabel.toggled = false;
        backupsPagesInfoLabel.label.color = MenuColorEffect.rgbMediumGrey;
        self.pages[0].subObjects.Add(backupsPagesInfoLabel);


        MenuLabel menuTitleInfoText = new MenuLabel(self, self.pages[0], "BACKUPS", new Vector2(290f, 625f), new Vector2(0f, 100f), bigText: true);
        self.pages[0].subObjects.Add(menuTitleInfoText);

        UpdateInfoText(self);

        
    }

void UpdateInfoText(BackupManager self)
        {
            userBackups = 0;
            autoBackups = 0;


            foreach (var bd in self.backupDirectories)
            {
                string filename = System.IO.Path.GetFileName(bd); ;
                if (filename.Contains("_USR")) userBackups++;
                else autoBackups++;
            }


            backupsInfoLabel.label.text = $"Total saves {userBackups + autoBackups} ({userBackups} manual, {autoBackups} automatic) - Total size T.B.D...";

            GetAndWriteTotalFilesSize();
        }

    private async void GetAndWriteTotalFilesSize()
    {
        await Task.Run(() =>
        {
            string path = Application.persistentDataPath + System.IO.Path.DirectorySeparatorChar + "backup";

            string[] a = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            long b = 0;
            foreach (string name in a)
            {
                FileInfo info = new FileInfo(name);
                b += info.Length;
            }
            // 4.
            // Return total size

            backupsInfoLabel.label.text = backupsInfoLabel.label.text.Replace("T.B.D...", b/1024/1024 + "Mb");
            return;
        });

    }


    /// <summary>
    /// Triggers the "INFO" signal when selecting a save you've already selected (instead of doing nothing)
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    private void IMakeBackupsButtonsCooler(On.Menu.BackupManager.orig_Singal orig, Menu.BackupManager self, Menu.MenuObject sender, string message)
    {
        if (message.Contains("BACKUP") && self.selectedBackup == int.Parse(message.Substring("BACKUP".Length)))
        {
            self.Singal(self.infoButton, "INFO");
            return;
        }

        orig(self, sender, message);
        if (message is "NEXT" or "PREV") {
            backupsPagesInfoLabel.label.text = $"Page {self.pageNum+1}/{self.TotalPages}";
        }
        
    }



    /// <summary>
    /// ILhook the check that triggers if backup is automatic.
    /// This if only contains code destinned to find out how many backups already exist
    /// and delete some of them if >50
    /// we IL-force in a check for the value of noAUtoDelete so that this hook can be enabled/disabled at runtime
    /// </summary>
    /// <param name="ctx"></param>
    private void IPreventDeletingSaves(ILContext ctx)
    {
        // Logger.LogDebug(ctx);
        try
        {
            ILCursor c = new ILCursor(ctx);
            c.GotoNext(
                MoveType.After,
                // x => x.OpCode == OpCodes.Brtrue, // this is a force-crash line
                x => x.OpCode == OpCodes.Brtrue_S
            // IL_0024: brtrue.s IL_002d
            );
            c.GotoNext(
                MoveType.Before,
                x => x.OpCode == OpCodes.Brtrue
            // IL_002e: brtrue IL_0166
            );

            c.MoveAfterLabels();

            ILLabel skipLabel = c.DefineLabel();

// Load the static boolean field
            c.Emit(OpCodes.Ldsfld, typeof(BackupTools).GetField("noAutoDelete")); 

            // if noAutoDelete is false (disabled) skip the custom instructions
            c.Emit(OpCodes.Brfalse_S, skipLabel);

            // bypass the brtrue
            c.Emit(OpCodes.Pop); // Cancel IL_002d: ldarg.1
            // Logger.LogInfo("Moved after folder init, next Operation is" + c.Next.OpCode.ToString() + "/" + (c.Next.Operand as MonoMod.Cil.ILLabel).ToString());
            c.Emit(OpCodes.Br, c.Next.Operand); // Emit an unconditional branch to IL_0166 instead

            // Mark the skip label for when noAutoDelete is false
            c.MarkLabel(skipLabel);



            // Logger.LogDebug(ctx);
            ILFailed=false;
        }
        catch (Exception ex)
        {
            Logger.LogError("Could not ILpatch saves deletion!!");
            Logger.LogError(ex);
        }

        /*
[Debug  :Backup tools] // ILContext: System.Void DMD<PlayerProgression::CreateCopyOfSaves>?-1482225152::
PlayerProgression::CreateCopyOfSaves(PlayerProgression,System.Boolean)
IL_0000: call System.String UnityEngine.Application::get_persistentDataPath()
IL_0005: ldsfld System.Char System.IO.Path::DirectorySeparatorChar
IL_000a: stloc.s V_4
IL_000c: ldloca.s V_4
IL_000e: call System.String System.Char::ToString()
IL_0013: ldstr "backup"
IL_0018: call System.String System.String::Concat(System.String,System.String,System.String)
IL_001d: stloc.0

------- if (!Directory.Exists(text))------------
IL_001e: ldloc.0
IL_001f: call System.Boolean System.IO.Directory::Exists(System.String)
IL_0024: brtrue.s IL_002d
------------------
IL_0026: ldloc.0
IL_0027: call System.IO.DirectoryInfo System.IO.Directory::CreateDirectory(System.String)
IL_002c: pop

-------- if (!userCreated)-------------
IL_002d: ldarg.1
IL_002e: brtrue IL_0166

------------------------
        */
    }

    private void IPreventAutomaticBackups(On.PlayerProgression.orig_CreateCopyOfSaves_bool orig, PlayerProgression self, bool userCreated)
    {
        if (noAutoBackups && !userCreated) return;
        orig(self, userCreated);
    }


    private void IMakeARemixInterface(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        if (MachineConnector.SetRegisteredOI(PluginInfo.PLUGIN_GUID, options))
        {
            Logger.LogInfo("Registered Mod Interface");
            Logger.LogInfo("after registering " + options.shouldCancelAutoBackups.Value + "," + options.shouldPreventDelAutoBackups.Value);


        }
        else
        {
            Logger.LogError("Could not register Mod Interface");
        }
    }
}
