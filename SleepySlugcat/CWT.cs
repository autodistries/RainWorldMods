using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace SleepySlugcat;



public static class CWT
{

    public static ConditionalWeakTable<AbstractCreature, SlugSleepData> playersDataTable = new();
    public static SlugSleepData GetCWTData(this AbstractCreature ac) => playersDataTable.GetValue(ac, _ => new SlugSleepData(ac));
    public static bool DoesCWTDataExist(this AbstractCreature ac) => playersDataTable.GetValue(ac, null) != null;

    public static List<FLabel> fLabels = new();


    public class SlugSleepData
    {
        public bool sleeping = false;
        public bool forceWakeUp = false;

        public ThreatDetermination? threatDetermination = null;
        // PlayerThreatTracker ? 281567 REMOTE PLAYERS DON'T NEED ONE, their logic is ahndled on their side !!
        // also see float ThreatPulser.Threat line 300678 for possible ways to get threat
        public int threatDeterminationCounter = 0;
        public int timeSinceLastZPopped = 0;

        public bool inVoidSea = false; // this is not a necessary element. Or is it ?
        public bool graspsForbidden = false;


        // Player-specific options would allow remote people to have their own color and stuff. It is not necessary tho.
        public bool singleZs = true;

        public SleepOptions sleepOptions;

        #if DEBUGON
        public string debugString = "";
        public FLabel? debugLabel;
        #endif
        public string testSyncedValue = "undef";
        private static System.Random random = new System.Random();

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }


        public SlugSleepData()
        {
            Console.WriteLine("Skeepy SLugcat Created BASIC entry inside cwt");

        }
        public SlugSleepData(AbstractCreature ac)
        {

            if (ac.realizedCreature is Player p)
            {

                if (ac.world.game.Players.FindIndex((el) => el == ac) is int playerNumber && playerNumber != -1)
                {
                    testSyncedValue = RandomString(10);
                    // if here, player is local !
                    threatDetermination = new ThreatDetermination(playerNumber);
                    sleepOptions = new SleepOptions(p, SleepySlugcatto.modOptions);
                }
                else
                {
                    // remote player
                }
                #if DEBUGON
                createLabel();
                #endif
                Console.WriteLine("Skeepy SLugcat Created a new entry inside cwt");
            }
            else
            {
                Console.WriteLine("Sleepy Slugcat: created an entry for a non realized player, probably");
            }
        }
        #if DEBUGON
        public string updateDebugString(AbstractCreature ac)
        {
            Player? p = ac.realizedCreature as Player;
            debugString =
$@"Threat deter found: {threatDetermination != null}
current threat: {threatDetermination?.currentThreat}
sleeping: {sleeping}
timesincelastZ: {timeSinceLastZPopped}
forceWakeUp: {forceWakeUp}
testSyncedValue: {testSyncedValue}
";
            if (p is not null)
            {
                debugLabel.x = -10 + p.bodyChunks[0].pos.x - p.room.game.cameras[0].pos.x;
                debugLabel.y = 70 + p.bodyChunks[0].pos.y - p.room.game.cameras[0].pos.y;
                debugString +=
@$"forceSLeepCounter: {p.forceSleepCounter}
pid:{p.abstractCreature.world.game.Players.FindIndex((el) => el == p.abstractCreature)}
";



            }


            if (debugLabel is not null)
            {
                debugLabel.text = debugString;
            if (Futile.stage.GetChildIndex(debugLabel) == -1 && SleepySlugcatto.showDebug)
                Futile.stage.AddChild(debugLabel);

            if (Futile.stage.GetChildIndex(debugLabel) != -1 && !SleepySlugcatto.showDebug)
                Futile.stage.RemoveChild(debugLabel);
            }

            return debugString;
        }

        public void createLabel()
        {
            debugLabel = new FLabel(RWCustom.Custom.GetFont(), "hellow")
            {
                alignment = FLabelAlignment.Left,
                x = RWCustom.Custom.rainWorld.options.ScreenSize.x - 50.2f,
                y = RWCustom.Custom.rainWorld.options.ScreenSize.y - 50.2f
            };
            fLabels.Add(debugLabel);
            Futile.stage.AddChild(debugLabel);
        }
        #endif


    }


    public class SleepOptions
    {
        public string textContent = ":3";
        public bool isMusician = false;
        public string colorType = "random";
        public Color customColor = Color.red;
        public Color slugcatColor = Color.cyan;
        public bool rainbowEnabled = false;
        public string rainbowType = "unified";
        public bool isDecayOpacityFadeOn = true;

        public float colorVariance = 0.35f;
        public float sizeVariance = 0.4f;
        public float qtyVariance = 0.4f;

        public SleepOptions()
        {
        }

        public SleepOptions(Player p, ModOptions modOptions) : this(p)
        {
            textContent = modOptions.ZsTextContentConfigurable.Value;
            isMusician = modOptions.ZsIsSlugcatMusicianOnConfigurable.Value;
            colorType = modOptions.ZsColorTypeConfigurable.Value;
            customColor = modOptions.ZsColorPickConfigurable.Value;

            rainbowEnabled = modOptions.ZsColorIsRainbowConfigurable.Value;
            rainbowType = modOptions.ZsColorRainbowTypeConfigurable.Value;
            isDecayOpacityFadeOn = modOptions.ZsColorIsDecayOnConfigurable.Value;
            colorVariance = modOptions.ZsColorVarianceConfigurable.Value;
            sizeVariance = modOptions.ZsSizeVarianceConfigurable.Value;
            qtyVariance = modOptions.ZsQtyVarianceConfigurable.Value;

        }
        public SleepOptions(Player p)
        {
            slugcatColor = p.ShortCutColor();
        }



        /// <summary>
        /// Gives the target Zs color in accordance to the config option
        /// </summary>
        /// <returns></returns>
        public Color getZsColor()
        {
            switch (colorType)
            {
                case "random":
                    return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1f);
                case "slugcat" or "custom":
                    return slightColorVariation(customColor);
                default:
                    throw new Exception($"Wring colorType value: {colorType}");
            }
        }

        /// <summary>
        /// gives room for a color to variate slightly based on wheight config
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        internal Color slightColorVariation(Color c)
        {
            float weight = 1f - colorVariance;
            Color d = c;

            d.r = (float)Math.Sqrt((1 - weight) * Math.Pow(UnityEngine.Random.value, 2) + weight * Math.Pow(d.r, 2));
            d.g = (float)Math.Sqrt((1 - weight) * Math.Pow(UnityEngine.Random.value, 2) + weight * Math.Pow(d.g, 2));
            d.b = (float)Math.Sqrt((1 - weight) * Math.Pow(UnityEngine.Random.value, 2) + weight * Math.Pow(d.b, 2));
            return d;

        }
    }


}


