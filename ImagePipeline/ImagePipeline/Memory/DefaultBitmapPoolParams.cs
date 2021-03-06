﻿using FBCore.Common.Util;
using System;
using System.Collections.Generic;
using Windows.System;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Provides pool parameters for <see cref="BitmapPool"/>.
    /// </summary>
    public static class DefaultBitmapPoolParams
    {
        /// <summary>
        /// We are not reusing SoftwareBitmap and want to free them
        /// as soon as possible.
        /// </summary>
        private const int MAX_SIZE_SOFT_CAP = 0;

        /// <summary>
        /// Our SoftwareBitmap live in the native memory. Therefore, we
        /// are not constrained by the max size of the managed memory, but
        /// we want to make sure we don't use too much memory on low end
        /// devices, so that we don't force other background process to
        /// be evicted.
        /// </summary>
        private static int GetMaxSizeHardCap()
        {
            ulong maxMemory = Math.Min(MemoryManager.AppMemoryUsageLimit, int.MaxValue);
            if (maxMemory > 16 * ByteConstants.MB)
            {
                return (int)(maxMemory / 4 * 3);
            }
            else
            {
                return (int)(maxMemory / 2);
            }
        }

        /// <summary>
        /// This will cause all get/release calls to behave like
        /// alloc/free calls i.e. no pooling.
        /// </summary>
        private static readonly Dictionary<int, int> DEFAULT_BUCKETS = new Dictionary<int, int>(0);

        /// <summary>
        /// Get the pool params.
        /// </summary>
        /// <returns>Pool params.</returns>
        public static PoolParams Get()
        {
            return new PoolParams(
                MAX_SIZE_SOFT_CAP,
                GetMaxSizeHardCap(),
                DEFAULT_BUCKETS
            );
        }
    }
}
