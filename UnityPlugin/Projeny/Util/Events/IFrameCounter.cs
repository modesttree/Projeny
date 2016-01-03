using System;
using UnityEngine;

namespace Projeny.Internal
{
    public interface IFrameCounter
    {
        int FrameCount
        {
            get;
        }
    }

    // Necessary since you can't reference things like Time in nunit tests
    public class UnityFrameCounter : IFrameCounter
    {
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
}
