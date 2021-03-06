﻿using System.Threading;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public static class UnitySynchronizationContext
    {
        public static SynchronizationContext Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            Instance = SynchronizationContext.Current;
        }
    }
}
