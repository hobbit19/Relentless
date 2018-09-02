using System;
using UnityEngine;

namespace LoomNetwork.CZB
{
    public class AnimationEventTriggering : MonoBehaviour
    {
        public Action<string> OnAnimationEvent;

        public void AnimationEvent(string animationName)
        {
            if (OnAnimationEvent != null)
            {
                OnAnimationEvent(animationName);
            }
        }
    }
}
