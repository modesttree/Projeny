using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Projeny.Internal
{
    public static class CoroutineUtil
    {
        public static IEnumerator WaitSeconds(float seconds)
        {
            float startTime = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - startTime < seconds)
            {
                yield return null;
            }
        }
    }
}
