using Menu.Remix.MixedUI;
using UnityEngine;
using static SleepySlugcat.Utils;

namespace SleepySlugcat;
public   class ModOptions : OptionInterface
{
    SleepySlugcatto modInstance;
    bool init = false;
    private float decalagey = 492f;
    private float decalagex = 10f;

    internal Configurable<string> ZsColorTypeConfigurable;
    private readonly ConfigAcceptableList<string> ZsColorTypeAcceptable = new("random", "slugcat", "custom");
    OpComboBox usableColorTypeOpComboBox;

    internal Configurable<Color> ZsColorPickConfigurable;
    OpColorPicker usableOpColorPicker;

    internal Configurable<float> ZsColorVarianceConfigurable;
    OpFloatSlider usableOpColorVarianceSlider;

    internal Configurable<float> ZsSizeVarianceConfigurable;
    OpFloatSlider usableOpSizeVarianceSlider;

    internal Configurable<float> ZsQtyVarianceConfigurable;
    OpFloatSlider usableOpQtyVarianceSlider;

    internal Configurable<bool> ZsColorIsRainbowConfigurable;
    OpCheckBox usableOpIsRainbowCheckBox;

    internal Configurable<string> ZsColorRainbowTypeConfigurable;
    private readonly ConfigAcceptableList<string> ZsColorRainbowTypeAcceptable = new("unified", "individual");
    OpComboBox usableRainbowTypeOpComboBox;

    internal Configurable<bool> ZsColorIsDecayOnConfigurable;
    OpCheckBox usableOpIsDecayOnCheckBox;

    internal Configurable<string> ZsTextContentConfigurable;
    OpTextBox usableOpTextContentTextBox;

    internal Configurable<bool> ZsIsSlugcatMusicianOnConfigurable;
    OpCheckBox usableOpIsSlugcatMusicianCheckBox;



    private Menu.Remix.MixedUI.UIelement[] UIArrPlayerOptions;

    public float DecalageY(bool stay = false)
    {
        if (stay) return decalagey;
        else
        {

            if (decalagey > 28f)
            {
                decalagey -= 27f;
                return decalagey;
            }
            else
            {
                decalagey = 492f;
                decalagex += 100f;
                return decalagey;
            };
        }
    }



    public ModOptions(SleepySlugcatto ss)
    {
        modInstance = ss;
        ZsColorTypeConfigurable = this.config.Bind<string>("ZsColorConfigurable", "random", ZsColorTypeAcceptable);
        ZsColorTypeConfigurable.OnChange += () =>
            {
                Debug.Log("Changed ZsColorConfigurable value to " + ZsColorTypeConfigurable.Value);
                // Zs. = ZsColorTypeConfigurable.Value switch 
                // {
                //     "random" => ColorMode.RANDOM,
                //     "slugcat" => ColorMode.SLUGCAT,
                //     "custom" => ColorMode.CUSTOM,

                    
                //     default:
                // };
                ss.colorMode = ZsColorTypeConfigurable.Value;
            };

        ZsColorPickConfigurable = config.Bind<Color>("ZsColorPickConfigurable", new Color(0.5f, 0.5f, 0.5f));

        ZsColorVarianceConfigurable = config.Bind<float>("ZsColorVarianceConfigurable", 0.35f, new ConfigAcceptableRange<float>(0f, 1f));

        ZsColorIsRainbowConfigurable = config.Bind("ZsColorIsRainbowConfigurable", false);
        ZsColorRainbowTypeConfigurable = config.Bind("ZsColorRainbowTypeConfigurable", "individual", ZsColorRainbowTypeAcceptable);
        ZsColorIsDecayOnConfigurable = config.Bind("ZsColorIsDecayOnConfigurable", true);
        ZsTextContentConfigurable = config.Bind("ZsTextContentConfigurable", "Z");
        ZsSizeVarianceConfigurable = config.Bind<float>("ZsSizeVarianceConfigurable", 0.40f, new ConfigAcceptableRange<float>(0f, 2f));
        ZsQtyVarianceConfigurable = config.Bind<float>("ZsQtyVarianceConfigurable", 0.40f, new ConfigAcceptableRange<float>(0f, 1f));
        ZsIsSlugcatMusicianOnConfigurable = config.Bind<bool>("ZsIsSlugcatMusicianOnConfigurable", false);

    }
    public override void Initialize()
    {
        TODO("make Zs POP on wakeup");
        TODO("Zs size slider");


        decalagey = 492f;
        decalagex = 10f;

        var opTab = new Menu.Remix.MixedUI.OpTab(this, "SleepySlugcat Options");
        this.Tabs = new[]
        {
            opTab
        };
        OpLabel colorTypeOpLabel = new OpLabel(decalagex, DecalageY(), "Color of the Zs", false);
        usableColorTypeOpComboBox = new OpComboBox(ZsColorTypeConfigurable, new UnityEngine.Vector2(105f, DecalageY(true) - 0.5f), 80f, ZsColorTypeAcceptable.AcceptableValues)
        {
            description = "- Random is random\n- Slugcat bases off the slugcat's color\n- Custom lets you pick a base color"
        };

        OpLabel colorPickOpLabel = new OpLabel(227f, DecalageY(true) - 0.5f, "Custom color ->", false);
        usableOpColorPicker = new OpColorPicker(ZsColorPickConfigurable, new Vector2(327f, DecalageY(true) - 17f));

        OpLabel colorVarianceOpLabel = new OpLabel(decalagex, DecalageY(), "Variance of the Zs", false);
        usableOpColorVarianceSlider = new(ZsColorVarianceConfigurable, new Vector2(120f, DecalageY(true) - 3f), 200)
        {
            description = "Determines how much Zs' color can vary from its base color"
        };

        OpLabel colorRainbowOpLabel = new OpLabel(decalagex, DecalageY(), "Rainbow mode", false);
        usableOpIsRainbowCheckBox = new(ZsColorIsRainbowConfigurable, new Vector2(120f, DecalageY(true)))
        {
            description = "Makes Zs RGB"
        };

        OpLabel colorRainbowTypeOpLabel = new(170f, DecalageY(true), "Rainbow type :");
        usableRainbowTypeOpComboBox = new OpComboBox(ZsColorRainbowTypeConfigurable, new Vector2(250f, DecalageY(true)), 100f)
        {
            description = "- Unified : all Zs' colors are synced\n- Individual : each Z does rainbow individually"
        };

        OpLabel colorDecayOnOpLabel = new OpLabel(decalagex, DecalageY(), "Enable Zs decay", false);
        usableOpIsDecayOnCheckBox = new(ZsColorIsDecayOnConfigurable, new Vector2(120f, DecalageY(true)))
        {
            description = "Enable decay of Zs over time instead of sudden disappearance"
        };

        OpLabel TextContentOpLabel = new OpLabel(decalagex, DecalageY(), "Custom z text", false);
        usableOpTextContentTextBox = new OpTextBox(ZsTextContentConfigurable, new Vector2(120f, DecalageY(true)), 150f)
        {
            allowSpace = true,
            accept = OpTextBox.Accept.StringASCII,
        };

        OpLabel isSlugcatMusicianLabel = new OpLabel(decalagex + 120f + usableOpTextContentTextBox.size.x, DecalageY(true), "Is slugcat a musician");
        usableOpIsSlugcatMusicianCheckBox = new OpCheckBox(ZsIsSlugcatMusicianOnConfigurable, new Vector2(330f + isSlugcatMusicianLabel.label.textRect.xMax, DecalageY(true)));

        OpLabel sizeVarianceOpLabel = new OpLabel(decalagex, DecalageY(), "Size diff of the Zs", false);
        usableOpSizeVarianceSlider = new(ZsSizeVarianceConfigurable, new Vector2(120f, DecalageY(true) - 3f), 200)
        {
            description = "Determines how big Zs' are"
        };


        OpLabel qtyVarianceOpLabel = new OpLabel(decalagex, DecalageY(), "Quantity of Zzs", false);
        usableOpQtyVarianceSlider = new(ZsQtyVarianceConfigurable, new Vector2(120f, DecalageY(true) - 3f), 200)
        {
            description = "Determines how much Zs' there are"
        };
        usableOpIsSlugcatMusicianCheckBox.OnValueUpdate += slugcatMusicianUpdate;

        void slugcatMusicianUpdate(UIconfig config, string value, string oldValue)
        {
            if (value.ToLower() == "true")
            {
                usableOpTextContentTextBox.greyedOut = true;
            }
            else usableOpTextContentTextBox.greyedOut = false;
        }


        usableColorTypeOpComboBox.OnValueUpdate += colorTypeUpdate;
        void colorTypeUpdate(UIconfig _a, string newvalue, string _oldval)
        {
            // Debug.Log("triggered "+newvalue +" "+oldval+ ""+a.Hidden);
            if (newvalue == "custom" && usableOpColorPicker.Hidden)
            {
                // Debug.Log("doing it on");
                colorPickOpLabel.Show();
                usableOpColorPicker.Show();
                //usableOpColorPicker.GetType().GetMethod("Reactivate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(usableOpColorPicker, null);
            }
            else if (!usableOpColorPicker.Hidden)
            {
                // Debug.Log("doing it off");
                colorPickOpLabel.Hide();
                usableOpColorPicker.Hide();

                //usableOpColorPicker.GetType().GetMethod("Deactivate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(usableOpColorPicker, null);
            }
            if (newvalue == "random" && !usableOpColorVarianceSlider.Hidden)
            {
                colorVarianceOpLabel.Hide();
                usableOpColorVarianceSlider.Hide();
            }
            else if (usableOpColorVarianceSlider.Hidden)
            {
                colorVarianceOpLabel.Show();
                usableOpColorVarianceSlider.Show();
            }
        };

        usableOpIsRainbowCheckBox.OnValueUpdate += isRainbowUpdate;

        void isRainbowUpdate(UIconfig _el, string newval, string _oldval)
        {
            //Debug.Log(el + " " + oldval + " " + newval);
            if (newval == "true")
            {
                colorRainbowTypeOpLabel.Show();
                usableRainbowTypeOpComboBox.Show();
            }
            else
            {
                colorRainbowTypeOpLabel.Hide();
                usableRainbowTypeOpComboBox.Hide();
            }
        };






        UIArrPlayerOptions = new UIelement[]
        {
            new OpLabel(10f, 550f, "SleepySlugcat Options", true),
            usableColorTypeOpComboBox,
            colorTypeOpLabel,

            usableOpColorPicker,
            colorPickOpLabel,

            usableOpColorVarianceSlider,
            colorVarianceOpLabel,

            colorRainbowOpLabel,
            usableOpIsRainbowCheckBox,

            colorRainbowTypeOpLabel,
            usableRainbowTypeOpComboBox,

            colorDecayOnOpLabel,
            usableOpIsDecayOnCheckBox,

            TextContentOpLabel,
            usableOpTextContentTextBox,

            isSlugcatMusicianLabel,
            usableOpIsSlugcatMusicianCheckBox,

            sizeVarianceOpLabel,
            usableOpSizeVarianceSlider,

            qtyVarianceOpLabel,
            usableOpQtyVarianceSlider

                   };

        colorTypeUpdate(null, ZsColorTypeConfigurable.Value, "");
        isRainbowUpdate(null, ZsColorIsRainbowConfigurable.Value.ToString(), "");







        opTab.AddItems(UIArrPlayerOptions);
        //  var methods = usableOpComboBox.GetType().GetMethods();
        // foreach (MethodInfo mi in methods)
        // {
        //     OpSimpleButton testbtn = new OpSimpleButton(new Vector2(decalagex, DecalageY()), new Vector2(100f, 10f), mi.Name);
        //     testbtn.OnClick += (arg) =>
        //     {
        //         try {
        //         if (mi == null) { Debug.Log("null!"); return; }
        //         //     usableOpListBox.GetType().GetMethod("_OpenList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.pro).Invoke(usableOpListBox, null);
        //         if (mi.GetParameters().Length == 0) Debug.Log(""+mi.Invoke(usableOpComboBox, null));
        //         else
        //         {
        //             string s = "";
        //             foreach (var param in mi.GetParameters())
        //             {
        //                 s += "--" + param.Name + ":" + param.GetType().Name;
        //             }

        //         } } catch (Exception e) {
        //             Debug.Log("Nope !" +mi.Name + mi.GetMethodImplementationFlags() +e);
        //         }
        //     };

        //     UIelement[] newUiElement = new UIelement[]{
        //         testbtn
        //     };


        //     this.Tabs[0].AddItems(newUiElement);

        // }
    }


}
