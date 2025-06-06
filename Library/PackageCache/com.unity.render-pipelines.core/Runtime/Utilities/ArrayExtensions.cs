using System;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;
using UnityEngine.Jobs;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Array utilities functions
    /// </summary>
    public static class ArrayExtensions
    {
        /// <summary>
        /// Resizes a native array. If an empty native array is passed, it will create a new one.
        /// </summary>
        /// <typeparam name="T">The type of the array</typeparam>
        /// <param name="array">Target array to resize</param>
        /// <param name="capacity">New size of native array to resize</param>
        public static void ResizeArray<T>(this ref NativeArray<T> array, int capacity) where T : struct
        {
            var newArray = new NativeArray<T>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            if (array.IsCreated)
            {
                NativeArray<T>.Copy(array, newArray, array.Length);
                array.Dispose();
            }
            array = newArray;
        }

        /// <summary>
        /// Resizes a transform access array.
        /// </summary>
        /// <param name="array">Target array to resize</param>
        /// <param name="capacity">New size of transform access array to resize</param>
        public static void ResizeArray(this ref TransformAccessArray array, int capacity)
        {
            var newArray = new TransformAccessArray(capacity);
            if (array.isCreated)
            {
                for (int i = 0; i < array.length; ++i)
                    newArray.Add(array[i]);

                array.Dispose();
            }
            array = newArray;
        }

        /// <summary>
        /// Resizes an array. If a null reference is passed, it will allocate the desired array.
        /// </summary>
        /// <typeparam name="T">The type of the array</typeparam>
        /// <param name="array">Target array to resize</param>
        /// <param name="capacity">New size of array to resize</param>
        public static void ResizeArray<T>(ref T[] array, int capacity)
        {
            if (array == null)
            {
                array = new T[capacity];
                return;
            }

            Array.Resize<T>(ref array, capacity);
        }

        /// <summary>
        /// Fills an array with the same value.
        /// </summary>
        /// <typeparam name="T">The type of the array</typeparam>
        /// <param name="array">Target array to fill</param>
        /// <param name="value">Value to fill</param>
        /// <param name="startIndex">Start index to fill</param>
        /// <param name="length">The number of entries to write, or -1 to fill until the end of the array</param>
        public static void FillArray<T>(this ref NativeArray<T> array, in T value, int startIndex = 0, int length = -1) where T : unmanaged
        {
            Assert.IsTrue(startIndex >= 0);

            unsafe
            {
                T* ptr = (T*)array.GetUnsafePtr<T>();

                int endIndex = length == -1 ? array.Length : startIndex + length;

                for (int i = startIndex; i < endIndex; ++i)
                    ptr[i] = value;
            }
        }
    }
}
