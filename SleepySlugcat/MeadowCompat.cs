using System;
using System.Linq;
using System.Runtime.InteropServices;
using RainMeadow;


namespace SleepySlugcat;



public class SleepyMeadowCompat : OnlineEntity.EntityData
{
    public SleepyMeadowCompat()
    {
    }

    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
    {
        return new State(entity);
    }

    public class State : EntityDataState
    {
        [OnlineField]
        public int onlineForceSleepCounter;

        [OnlineField]
        public string onlineRandomValue;


        public State()
        {
        }
        public State(OnlineEntity onlineEntity)
        {
            if (!ModManager.ActiveMods.Any((el) => el.id == "nope.sleepyslugcat"))  return;
            if ((onlineEntity as OnlinePhysicalObject)?.apo.realizedObject is not Player player)
            {
                return;
            }

            onlineForceSleepCounter = player.forceSleepCounter;

            var cwtdata = player.abstractCreature.GetCWTData();
            onlineRandomValue = cwtdata.testSyncedValue;
        }

        public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
        {
            if (!ModManager.ActiveMods.Any((el) => el.id == "nope.sleepyslugcat"))  return;

            if (data is not SleepyMeadowCompat sleepyMeadowCompat)
            {
                return;
            }

            var playerOpo = onlineEntity as OnlinePhysicalObject;

            if (playerOpo?.apo.realizedObject is not Player player)
            {
                return;
            }

            player.forceSleepCounter = onlineForceSleepCounter;

            player.abstractCreature.GetCWTData().testSyncedValue = onlineRandomValue;

        
        }
        public override Type GetDataType()
        {
            return typeof(SleepyMeadowCompat);
        }

    }
}
