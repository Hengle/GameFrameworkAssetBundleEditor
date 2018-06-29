﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Icarus.GameFramework;
using Icarus.GameFramework.Download;
using Icarus.GameFramework.UpdateAssetBundle;
using Icarus.GameFramework.Version;
using UnityEngine;
using UnityEngine.WSA;
using Application = UnityEngine.Application;

namespace Icarus.UnityGameFramework.Runtime
{
    /// <summary>
    /// 使用UnityWebRequeDownload来更新
    /// </summary>
    [AddComponentMenu("Game Framework/Default Download")]
    public class DefaultUpdateAssetBundle:MonoBehaviour,IUpdateAssetBundle
    {
        public DownloadManager DownloadManager;
        public CoroutineManager Coroutine;
        
        public void UpdateAssetBundle(UpdateInfo updateInfo, IEnumerable<AssetBundleInfo> assetBundleifInfos,
            GameFrameworkAction<AssetBundleInfo> anyCompleteHandle,
            GameFrameworkAction allCompleteHandle, GameFrameworkAction<string> errorHandle)
        {
            int version = -1;

            try
            {
                version = int.Parse(Application.version.Split('.').Last());
            }
            catch (Exception e)
            {
                errorHandle?.Invoke($"请确保 Edit-->Project Settings-->Player --> Other Setting 下的 Version 字段‘.’分割的最后一位是int值，如：0.1.1s.2,‘2’就是我默认的规则");
                return;
            }
            if (version < updateInfo.MinAppVersion)
            {
                Application.OpenURL(updateInfo.AppUpdateUrl);
                return;
            }
            DownloadManager.AllCompleteHandle = allCompleteHandle.Invoke;
            List<DownloadUnitInfo> downloadUnitInfos = new List<DownloadUnitInfo>();
            foreach (var assetBundleInfo in assetBundleifInfos)
            {
                downloadUnitInfos.Add(new DownloadUnitInfo()
                {
                    CompleteHandle = x =>
                    {
                        anyCompleteHandle?.Invoke(assetBundleInfo);
                        //解压
                        Utility.ZipUtil.UnzipZip(x,Application.persistentDataPath);
                        GameFramework.Utility.FileUtil.DeleteFile(x);
                    },
                    ErrorHandle = errorHandle.Invoke,
                    FileName = assetBundleInfo.PackName.Replace("dat", "zip"),
                    SavePath = Path.Combine(Application.persistentDataPath,assetBundleInfo.PackPath),
                    Url = updateInfo.AssetBundleUrl+"/"+ assetBundleInfo.PackFullName.Replace("dat","zip"),
                    IsFindCacheLibrary = false, //ab包更新不找缓存
                    DownloadUtil = new UnityWebRequestDownload(Coroutine)

                });
            }

            DownloadManager.AddRangeDownload(downloadUnitInfos);
        }

        protected virtual void Awake()
        {
            GameEntry.RegisterComponent(this);
        }

        protected virtual void Start()
        {
            DownloadManager = new DownloadManager();
            DownloadManager.Init();
        }

    }
}