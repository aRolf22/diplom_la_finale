﻿using Codice.CM.Common;
using Unity.PlasticSCM.Editor.AssetUtils;

namespace Unity.PlasticSCM.Editor.AssetsOverlays.Cache
{
    internal interface IAssetStatusCache
    {
        AssetStatus GetStatus(string fullPath);
        LockStatusData GetLockStatusData(string fullPath);
        void Clear();
        void ClearLocks();
    }

    internal class AssetStatusCache : IAssetStatusCache
    {
        internal AssetStatusCache(
            WorkspaceInfo wkInfo,
            bool isGluonMode)
        {
            mLocalStatusCache = new LocalStatusCache(wkInfo);

            mRemoteStatusCache = new RemoteStatusCache(
                wkInfo,
                isGluonMode,
                ProjectWindow.Repaint,
                RepaintInspector.All);

            mLockStatusCache = new LockStatusCache(
                wkInfo,
                ProjectWindow.Repaint,
                RepaintInspector.All);
        }

        AssetStatus IAssetStatusCache.GetStatus(string fullPath)
        {
            AssetStatus localStatus = mLocalStatusCache.GetStatus(fullPath);

            if (!ClassifyAssetStatus.IsControlled(localStatus))
                return localStatus;

            AssetStatus remoteStatus = mRemoteStatusCache.GetStatus(fullPath);

            AssetStatus lockStatus = mLockStatusCache.GetStatus(fullPath);

            return localStatus | remoteStatus | lockStatus;
        }

        LockStatusData IAssetStatusCache.GetLockStatusData(string fullPath)
        {
            return mLockStatusCache.GetLockStatusData(fullPath);
        }

        void IAssetStatusCache.Clear()
        {
            mLocalStatusCache.Clear();
            mRemoteStatusCache.Clear();
            mLockStatusCache.Clear();
        }

        void IAssetStatusCache.ClearLocks()
        {
            mLockStatusCache.Clear();
        }

        readonly LocalStatusCache mLocalStatusCache;
        readonly RemoteStatusCache mRemoteStatusCache;
        readonly LockStatusCache mLockStatusCache;
    }
}
