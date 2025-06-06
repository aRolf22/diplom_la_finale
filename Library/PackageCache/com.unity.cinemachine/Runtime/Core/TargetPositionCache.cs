using UnityEngine;
using System.Collections.Generic;

namespace Unity.Cinemachine
{
    /// <summary>
    /// Use this class to support caching of Cinemachine target positions and rotations for the 
    /// purposes of timeline scrubbing in the Editor.  At runtime, the public methods in this class
    /// simply return the uncached values from the target's transform.
    /// 
    /// Cinemachine behaviours and extensions that support timeline scrubbing should use the 
    /// GetTargetPosition and GetTargetRotation static methods when accessing the target's position or rotation.
    /// </summary>
    public class TargetPositionCache
    {
        internal static bool UseCache;
        internal const float CacheStepSize = 1 / 60.0f;
        internal enum Mode { Disabled, Record, Playback }
       
        static Mode m_CacheMode = Mode.Disabled;

#if UNITY_EDITOR
        static TargetPositionCache() { UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded; }
        static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) { ClearCache(); }
#endif

        internal static Mode CacheMode
        {
            get => m_CacheMode;
            set
            {
                if (value == m_CacheMode)
                    return;
                m_CacheMode = value;
                switch (value)
                {
                    default: case Mode.Disabled: ClearCache(); break;
                    case Mode.Record: ClearCache(); break;
                    case Mode.Playback: CreatePlaybackCurves(); break;
                }
            }
        }

        internal static bool IsRecording => UseCache && m_CacheMode == Mode.Record;
        internal static bool CurrentPlaybackTimeValid => UseCache && m_CacheMode == Mode.Playback && HasCurrentTime;
        internal static bool IsEmpty => CacheTimeRange.IsEmpty;

        internal static float CurrentTime;

        // These are used during recording to manage camera cuts
        internal static int CurrentFrame;
        internal static bool IsCameraCut;

        class CacheCurve
        {
            public struct Item
            {
                public Vector3 Pos;
                public Quaternion Rot;

                public static Item Lerp(Item a, Item b, float t)
                {
                    return new Item
                    {
                        Pos = Vector3.LerpUnclamped(a.Pos, b.Pos, t),
                        Rot = Quaternion.SlerpUnclamped(a.Rot, b.Rot, t)
                    };
                }
                public static Item Empty => new Item { Rot = Quaternion.identity };
            }

            public float StartTime;
            public float StepSize;
            public int Count => m_Cache.Count;
 
            List<Item> m_Cache;

            public CacheCurve(float startTime, float endTime, float stepSize)
            {
                StepSize = stepSize;
                StartTime = startTime;
                m_Cache = new List<Item>(Mathf.CeilToInt((StepSize * 0.5f + endTime - startTime) / StepSize));
            }

            public void Add(Item item) => m_Cache.Add(item);

            public void AddUntil(Item item, float time, bool isCut)
            {
                var prevIndex = m_Cache.Count - 1;
                var prevTime = prevIndex * StepSize;
                var timeRange = time - StartTime - prevTime;

                // If this sample is the first after a camera cut, we don't want to lerp the positions
                // in the event that some frames got skipped by timeline and the targets were
                // warped at the cut
                if (isCut)
                    for (float t = StepSize; t <= timeRange; t += StepSize)
                        Add(item);
                else
                {
                    var prev = m_Cache[prevIndex];
                    for (float t = StepSize; t <= timeRange; t += StepSize)
                        Add(Item.Lerp(prev, item, t / timeRange));
                }
            }

            public Item Evaluate(float time)
            {
                var numItems = m_Cache.Count;
                if (numItems == 0)
                    return Item.Empty;
                var s = time - StartTime;
                var index = Mathf.Clamp(Mathf.FloorToInt(s / StepSize), 0, numItems - 1);
                var v = m_Cache[index];
                if (index == numItems - 1)
                    return v;
                return Item.Lerp(v, m_Cache[index + 1], (s - index * StepSize) / StepSize);
            }
        }

        class CacheEntry
        {
            public CacheCurve Curve;

            struct RecordingItem 
            {
                public float Time;
                public bool IsCut;
                public CacheCurve.Item Item;
            }
            List<RecordingItem> RawItems = new List<RecordingItem>();

            public void AddRawItem(float time, bool isCut, Transform target)
            {
                // Preserve monotonic ordering
                var endTime = time - CacheStepSize;
                var maxItem = RawItems.Count - 1;
                var lastToKeep = maxItem;
                while (lastToKeep >= 0 && RawItems[lastToKeep].Time > endTime)
                    --lastToKeep;
                if (lastToKeep == maxItem)
                {
                    // Append only, nothing to remove
                    RawItems.Add(new RecordingItem
                    {
                        Time = time,
                        IsCut = isCut,
                        Item = new CacheCurve.Item { Pos = target.position, Rot = target.rotation }
                    });
                }
                else 
                {
                    // Trim off excess, overwrite the one after lastToKeep
                    var trimStart = lastToKeep + 2;
                    if (trimStart <= maxItem)
                        RawItems.RemoveRange(trimStart, RawItems.Count - trimStart);
                    RawItems[lastToKeep + 1] = new RecordingItem
                    {
                        Time = time,
                        IsCut = isCut,
                        Item = new CacheCurve.Item { Pos = target.position, Rot = target.rotation }
                    };
                }
            }

            public void CreateCurves()
            {
                int maxItem = RawItems.Count - 1;
                float startTime = maxItem < 0 ? 0 : RawItems[0].Time;
                float endTime = maxItem < 0 ? 0 : RawItems[maxItem].Time;
                Curve = new CacheCurve(startTime, endTime, CacheStepSize);
                Curve.Add(maxItem < 0 ? CacheCurve.Item.Empty : RawItems[0].Item);
                for (int i = 1; i <= maxItem; ++i)
                    Curve.AddUntil(RawItems[i].Item, RawItems[i].Time, RawItems[i].IsCut);
                RawItems.Clear();
            }
        }

        static Dictionary<Transform, CacheEntry> m_Cache;

        internal struct TimeRange
        {
            public float Start;
            public float End;

            public bool IsEmpty => End < Start;
            public bool Contains(float time) => time >= Start && time <= End;
            public static TimeRange Empty 
                { get => new TimeRange { Start = float.MaxValue, End = float.MinValue }; }

            public void Include(float time)
            {
                Start = Mathf.Min(Start, time);
                End = Mathf.Max(End, time);
            }
        }
        static TimeRange m_CacheTimeRange;
        internal static TimeRange CacheTimeRange { get => m_CacheTimeRange; }
        internal static bool HasCurrentTime { get => m_CacheTimeRange.Contains(CurrentTime); }

        internal static void ClearCache()
        {
            m_Cache = CacheMode == Mode.Disabled ? null : new Dictionary<Transform, CacheEntry>();
            m_CacheTimeRange = TimeRange.Empty;
            CurrentTime = 0;
            CurrentFrame = 0;
            IsCameraCut = false;
        }

        static void CreatePlaybackCurves()
        {
            m_Cache ??= new Dictionary<Transform, CacheEntry>();
            var iter = m_Cache.GetEnumerator();
            while (iter.MoveNext())
                iter.Current.Value.CreateCurves();
            iter.Dispose();
        }

        const float kWraparoundSlush = 0.1f;

        /// <summary>
        /// When using Timeline in Edit mode:
        ///  - If you're Recording, the method logs the target position at the CurrentTime.
        ///  - Otherwise, it fetches the cached position at CurrentTime.
        ///  
        /// When using Timeline in Play mode, and when you're not scrubbing it:
        ///  - The method returns the position directly from the Transform.
        /// </summary>
        /// <param name="target">Target whose transform is tracked</param>
        /// <returns>The effective position of the target.</returns>
        public static Vector3 GetTargetPosition(Transform target)
        {
            if (!UseCache || CacheMode == Mode.Disabled)
                return target.position;

            // Wrap around during record?
            if (CacheMode == Mode.Record 
                && !m_CacheTimeRange.IsEmpty 
                && CurrentTime < m_CacheTimeRange.Start - kWraparoundSlush)
            {
                ClearCache();
            }

            if (CacheMode == Mode.Playback && !HasCurrentTime)
                return target.position;

            if (!m_Cache.TryGetValue(target, out var entry))
            {
                if (CacheMode != Mode.Record)
                    return target.position;

                entry = new CacheEntry();
                m_Cache.Add(target, entry);
            }
            if (CacheMode == Mode.Record)
            {
                entry.AddRawItem(CurrentTime, IsCameraCut, target);
                m_CacheTimeRange.Include(CurrentTime);
                return target.position;
            }
            if (entry.Curve == null)
                return target.position;
            return entry.Curve.Evaluate(CurrentTime).Pos;
        }

        /// <summary>
        /// When using Timeline in Edit mode:
        ///  - If you're Recording, the method logs the target position at the CurrentTime.
        ///  - Otherwise, it fetches the cached position at CurrentTime.
        ///  
        /// When using Timeline in Play mode, and when you're not scrubbing it:
        ///  - The method returns the position directly from the Transform.
        /// </summary>
        /// <param name="target">Target whose transform is tracked</param>
        /// <returns>The effective position of the target.</returns>
        public static Quaternion GetTargetRotation(Transform target)
        {
            if (CacheMode == Mode.Disabled)
                return target.rotation;

            // Wrap around during record?
            if (CacheMode == Mode.Record 
                && !m_CacheTimeRange.IsEmpty 
                && CurrentTime < m_CacheTimeRange.Start - kWraparoundSlush)
            {
                ClearCache();
            }

            if (CacheMode == Mode.Playback && !HasCurrentTime)
                return target.rotation;

            if (!m_Cache.TryGetValue(target, out var entry))
            {
                if (CacheMode != Mode.Record)
                    return target.rotation;

                entry = new CacheEntry();
                m_Cache.Add(target, entry);
            }
            if (CacheMode == Mode.Record)
            {
                if (m_CacheTimeRange.End <= CurrentTime)
                {
                    entry.AddRawItem(CurrentTime, IsCameraCut, target);
                    m_CacheTimeRange.Include(CurrentTime);
                }
                return target.rotation;
            }
            return entry.Curve.Evaluate(CurrentTime).Rot;
        }
    }
}
