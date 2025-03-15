using Menu.Remix.MixedUI;

namespace BackgroundPreview;

public class Options : OptionInterface
{
    static public Configurable<bool> showBackgrounds;
    static public Configurable<bool> showIntro;
    static public Configurable<bool> showEndings;
    static public Configurable<bool> showPassages;
    static public Configurable<bool> showAll;
    static public Configurable<bool> showSlugcat;
    static public Configurable<bool> showDreams;
    public Options()
    {

        showAll = config.Bind("showAll", true);
        showBackgrounds = config.Bind("showBackgrounds", true);
        showIntro = config.Bind("showIntro", true);
        showEndings = config.Bind("showEndings", false);
        showPassages = config.Bind("showPassages", false);
        showSlugcat = config.Bind("showSlugcat", false);
        showDreams = config.Bind("showDreams", false);
    }

    public override void Initialize()
    {
         base.Initialize();
        var opTab = new OpTab(this, "BackgroundPreviewOptns");
        Tabs =
        [
            opTab
        ];

        var openPreviewer = new OpSimpleButton(new UnityEngine.Vector2(200, 350), new UnityEngine.Vector2(200f, 30f), "Open Background Preview");

        openPreviewer.OnClick += (_) =>
        {
            RWCustom.Custom.rainWorld.processManager.RequestMainProcessSwitch(BackgroundPreviewMenu.ProcessID.BackgroundPreviewID);
        };

        var UIArrPlayerOptions = new UIelement[]
                {
                    openPreviewer,
                };

        opTab.AddItems(UIArrPlayerOptions);
    }
}


        // new OpCheckBox(showAll, new()),
        // new OpCheckBox(showBackgrounds, new()),
        // new OpCheckBox(showIntro, new()),
        // new OpCheckBox(showEndings, new()),
        // new OpCheckBox(showPassages, new()),
        // new OpCheckBox(showSlugcat, new()),
        // new OpCheckBox(showDreams, new()),



        // new OpCheckBox(showAll, new());
        // new OpCheckBox(showBackgrounds, new());
        // new OpCheckBox(showIntro, new());
        // new OpCheckBox(showEndings, new());
        // new OpCheckBox(showPassages, new());
        // new OpCheckBox(showSlugcat, new());
        // new OpCheckBox(showDreams, new());
