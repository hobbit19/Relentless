using Loom.ZombieBattleground.Protobuf;
using System;
using System.Threading.Tasks;
using UnityEngine;
using static Loom.ZombieBattleground.QueueManager;
using Loom.Google.Protobuf;


namespace Loom.ZombieBattleground
{
    public interface IQueueManager
    {
        bool Active { get; set; }
        void AddTask(Func<Task> taskFunc);
        void AddAction(IMessage action);
        void Clear();
    }
}
