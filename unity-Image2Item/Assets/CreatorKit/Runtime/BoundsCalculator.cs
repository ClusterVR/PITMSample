using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ClusterVR.CreatorKit
{
    public static class BoundsCalculator
    {
        public static void CalcLocalBounds(GameObject gameObject, out Bounds? rendererBounds, out Bounds? colliderBounds)
        {
            var go = Object.Instantiate(gameObject);
            try
            {
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                rendererBounds = CalcTotalBounds(go.GetComponentsInChildren<Renderer>(true), r => r.bounds);
                colliderBounds = CalcTotalBounds(go.GetComponentsInChildren<Collider>(true), c => c.bounds);
            }
            finally
            {
                Object.Destroy(go);
            }
        }

        static Bounds? CalcTotalBounds<T>(T[] sources, Func<T, Bounds> boundsGetter)
        {
            if (sources == null || sources.Length == 0) return null;
            return sources
                .Select(boundsGetter)
                .Aggregate((result, b) =>
                {
                    result.Encapsulate(b);
                    return result;
                });
        }
    }
}
