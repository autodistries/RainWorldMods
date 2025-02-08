using System;
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


        public State()
        {
        }
        public State(OnlineEntity onlineEntity)
        {
            if ((onlineEntity as OnlinePhysicalObject)?.apo.realizedObject is not Player player)
            {
                return;
            }

            onlineForceSleepCounter = player.forceSleepCounter;
        }

        public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
        {

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

        
        }
        public override Type GetDataType()
        {
            return typeof(SleepyMeadowCompat);
        }



       

    }
}
