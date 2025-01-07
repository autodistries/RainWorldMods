using Menu.Remix;
using Menu.Remix.MixedUI;

namespace ModsUpdater;


# nullable enable




public static class Graphics {

    #region remixinfo
    private static InternalOI_Stats? localInternalOiStats; // the REMIX STATS page
    public static InternalOI_Stats? LocalInternalOiStats { get => localInternalOiStats; set => localInternalOiStats = value; }
    
    private static OpSimpleImageButton? btnUpdateAll;
    public static OpSimpleImageButton? BtnUpdateAll { get => btnUpdateAll; set => btnUpdateAll = value; }
    
    static OpLabel? lblUpdatableMods; // On the REMIX STATS page, the updatable label
    public static OpLabel? LblUpdatableMods { get => lblUpdatableMods; set => lblUpdatableMods = value; }

    private static OpLabel? lblOrphanedMods;
    public static OpLabel? LblOrphanedMods { get => lblOrphanedMods; set => lblOrphanedMods = value; }

    private static OpLabel? lblUpToDateMods;
    public static OpLabel? LblUpToDateMods { get => lblUpToDateMods; set => lblUpToDateMods = value; }

    private static OpLabel? lblModUpdaterStatus; // the info text
    public static OpLabel? LblModUpdaterStatus { get => lblModUpdaterStatus; set  {lblModUpdaterStatus = value; updateModUpdaterStatus();} }

    private static string modUpdaterStatus = "Welcome !"; // Because text needs to be kept across
    public static string ModUpdaterStatus { 
        get => modUpdaterStatus; 
        set { 
            modUpdaterStatus = value; 
            if (lblModUpdaterStatus is not null && lblModUpdaterStatus.text != modUpdaterStatus)
                    lblModUpdaterStatus.text = modUpdaterStatus;  
        }             
    }




    #endregion remixinfo
    private static OpLabel? lblPreviewUpdateStatus;
    public static OpLabel? LblPreviewUpdateStatus { get => lblPreviewUpdateStatus; set => lblPreviewUpdateStatus = value; }
    private static OpSimpleImageButton? lblPreviewUpdateButton;
    public static OpSimpleImageButton? LblPreviewUpdateButton { get => lblPreviewUpdateButton; set => lblPreviewUpdateButton = value; }

    public static void updateModUpdaterStatus() {
        if (lblModUpdaterStatus is not null && lblModUpdaterStatus.text != modUpdaterStatus)
                lblModUpdaterStatus.text = modUpdaterStatus;  
    }       
    

}