using BepInEx;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace SleepySlugcat;



public static class CWT
{

    public static ConditionalWeakTable<AbstractCreature, SlugSleepData> playersDataTable = new();
    public static SlugSleepData GetCWTData(this AbstractCreature ac) => playersDataTable.GetValue(ac, _ => new SlugSleepData(ac));
    public static SlugSleepData? TryGetCWTData(this AbstractCreature ac) => playersDataTable.GetValue(ac, null);

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

        public bool inVoidSea = false; // this is not a necessary element
        public bool graspsForbidden = false;


        // Player-specific options would allow remote people to have their own color and stuff. It is not necessary tho.
        public bool singleZs = true;

        public string debugString = "";
        public FLabel? debugLabel;

        public SlugSleepData() {
            Console.WriteLine("Skeepy SLugcat Created BASIC entry inside cwt");

        }
        public SlugSleepData(AbstractCreature ac)
        {
            if (ac.realizedCreature is Player)
            {
                if (ac.world.game.Players.FindIndex((el) => el == ac) is int playerNumber && playerNumber != -1)
                {
                    threatDetermination = new ThreatDetermination(playerNumber);
                }
                createLabel();
                Console.WriteLine("Skeepy SLugcat Created a new entry inside cwt");
            }
            else
            {
                Console.WriteLine("Sleepy Slugcat: created an entry for a non realized player, probably");
            }
        }

        public string updateDebugString(AbstractCreature ac)
        {
            Player? p = ac.realizedCreature as Player;
            debugString =
$@"Threat deter found: {threatDetermination != null}
current threat: {threatDetermination?.currentThreat}
sleeping: {sleeping}
timesincelastZ: {timeSinceLastZPopped}
";
                if (p is not null) {
                    debugLabel.x = -10 + p.bodyChunks[0].pos.x - p.room.game.cameras[0].pos.x;
                    debugLabel.y = -30 + p.bodyChunks[0].pos.y - p.room.game.cameras[0].pos.y;
                    debugString +=
@$"forceSLeepCounter: {p.forceSleepCounter}
pid:{p.abstractCreature.world.game.Players.FindIndex((el) => el == p.abstractCreature)}
";
                }
            if (debugLabel is not null) {
                debugLabel.text = debugString;
            }
            return debugString;
        }

        public void createLabel() {
            debugLabel = new FLabel(RWCustom.Custom.GetFont(), "hellow")
            {
                alignment = FLabelAlignment.Left,
                x = RWCustom.Custom.rainWorld.options.ScreenSize.x - 50.2f,
                y = RWCustom.Custom.rainWorld.options.ScreenSize.y - 50.2f
            };
            fLabels.Add(debugLabel);
            Futile.stage.AddChild(debugLabel);
        }

         
    }

    
}


