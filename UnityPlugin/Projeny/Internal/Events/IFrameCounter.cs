using System;

#if UNITY3D
using UnityEngine;
#endif

namespace Projeny.Internal
{
    public interface IFrameCounter
    {
        int FrameCount
        {
            get;
        }
    }
    
#if UNITY3D
    // Necessary since you can't reference things like Time in nunit tests
    public class UnityFrameCounter : IFrameCounter
    {
        [Inject]
        public UnityFrameCounter()
        {
        }
        public int FrameCount
        {
            get
            {
                return Time.frameCount;
            }
        }
    }
#endif
}
