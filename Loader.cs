using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using CSUROffsetPatch.CustomAI;
using CSUROffsetPatch.Util;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.IO;
using ColossalFramework.Plugins;
using CSUROffsetPatch.NewData;
using ColossalFramework.Math;

namespace CSUROffsetPatch
{
    public class Loader : LoadingExtensionBase
    {
        public static LoadMode CurrentLoadMode;

        public class Detour
        {
            public MethodInfo OriginalMethod;
            public MethodInfo CustomMethod;
            public RedirectCallsState Redirect;

            public Detour(MethodInfo originalMethod, MethodInfo customMethod)
            {
                this.OriginalMethod = originalMethod;
                this.CustomMethod = customMethod;
                this.Redirect = RedirectionHelper.RedirectCalls(originalMethod, customMethod);
            }
        }

        public static List<Detour> Detours { get; set; }
        public static bool DetourInited = false;
        public static bool isMoveItRunning = false;

        public override void OnCreated(ILoading loading)
        {
            Detours = new List<Detour>();
            base.OnCreated(loading);
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            Loader.CurrentLoadMode = mode;
            if (CSUROffsetPatch.IsEnabled)
            {
                if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame || mode == LoadMode.NewMap || mode == LoadMode.LoadMap || mode == LoadMode.NewAsset || mode == LoadMode.LoadAsset)
                {
                    DebugLog.LogToFileOnly("OnLevelLoaded");
                    InitDetour();
                    if (mode == LoadMode.NewGame)
                    {
                        DebugLog.LogToFileOnly("New Game");
                    }
                }
            }
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
            if (CurrentLoadMode == LoadMode.LoadGame || CurrentLoadMode == LoadMode.NewGame || CurrentLoadMode == LoadMode.LoadMap || CurrentLoadMode == LoadMode.NewMap || CurrentLoadMode == LoadMode.LoadAsset || CurrentLoadMode == LoadMode.NewAsset)
            {
                if (CSUROffsetPatch.IsEnabled)
                {
                    RevertDetour();
                    Threading.isFirstTime = true;
                }
            }
        }

        public override void OnReleased()
        {
            base.OnReleased();
        }

        public void InitDetour()
        {
            if (!DetourInited)
            {
                DebugLog.LogToFileOnly("Init detours");
                bool detourFailed = false;

                //1
                DebugLog.LogToFileOnly("Detour NetAI::GetCollisionHalfWidth calls");
                try
                {
                    Detours.Add(new Detour(typeof(NetAI).GetMethod("GetCollisionHalfWidth", BindingFlags.Public | BindingFlags.Instance),
                                           typeof(CustomNetAI).GetMethod("CustomGetCollisionHalfWidth", BindingFlags.Public | BindingFlags.Instance)));
                }
                catch (Exception)
                {
                    DebugLog.LogToFileOnly("Could not detour NetAI::GetCollisionHalfWidth");
                    detourFailed = true;
                }
                //2
                //public static bool RayCast(ref NetSegment mysegment, ushort segmentID, Segment3 ray, float snapElevation, bool nameOnly, out float t, out float priority)
                DebugLog.LogToFileOnly("Detour NetSegment::RayCast calls");
                try
                {
                    Detours.Add(new Detour(typeof(NetSegment).GetMethod("RayCast", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(ushort), typeof(Segment3), typeof(float), typeof(bool), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }, null),
                                           typeof(CustomNetSegment).GetMethod("RayCast", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(NetSegment).MakeByRefType() , typeof(ushort), typeof(Segment3), typeof(float), typeof(bool), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }, null)));
                }
                catch (Exception)
                {
                    DebugLog.LogToFileOnly("Could not detour NetSegment::RayCast");
                    //detourFailed = true;
                }
                //3
                //public static bool RayCast(ref NetNode node, Segment3 ray, float snapElevation, out float t, out float priority)
                DebugLog.LogToFileOnly("Detour NetNode::RayCast calls");
                try
                {
                    Detours.Add(new Detour(typeof(NetNode).GetMethod("RayCast", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(Segment3), typeof(float), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }, null),
                                           typeof(CustomNetNode).GetMethod("RayCast", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(NetNode).MakeByRefType(), typeof(Segment3), typeof(float), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }, null)));
                }
                catch (Exception)
                {
                    DebugLog.LogToFileOnly("Could not detour NetNode::RayCast");
                    //detourFailed = true;
                }

                isMoveItRunning = CheckMoveItIsLoaded();

                if (detourFailed)
                {
                    DebugLog.LogToFileOnly("Detours failed");
                }
                else
                {
                    DebugLog.LogToFileOnly("Detours successful");
                }
                DetourInited = true;
            }
        }

        public void RevertDetour()
        {
            if (DetourInited)
            {
                DebugLog.LogToFileOnly("Revert detours");
                Detours.Reverse();
                foreach (Detour d in Detours)
                {
                    RedirectionHelper.RevertRedirect(d.OriginalMethod, d.Redirect);
                }
                DetourInited = false;
                Detours.Clear();
                DebugLog.LogToFileOnly("Reverting detours finished.");
            }
        }

        private bool Check3rdPartyModLoaded(string namespaceStr, bool printAll = false)
        {
            bool thirdPartyModLoaded = false;

            var loadingWrapperLoadingExtensionsField = typeof(LoadingWrapper).GetField("m_LoadingExtensions", BindingFlags.NonPublic | BindingFlags.Instance);
            List<ILoadingExtension> loadingExtensions = (List<ILoadingExtension>)loadingWrapperLoadingExtensionsField.GetValue(Singleton<LoadingManager>.instance.m_LoadingWrapper);

            if (loadingExtensions != null)
            {
                foreach (ILoadingExtension extension in loadingExtensions)
                {
                    if (printAll)
                        DebugLog.LogToFileOnly($"Detected extension: {extension.GetType().Name} in namespace {extension.GetType().Namespace}");
                    if (extension.GetType().Namespace == null)
                        continue;

                    var nsStr = extension.GetType().Namespace.ToString();
                    if (namespaceStr.Equals(nsStr))
                    {
                        DebugLog.LogToFileOnly($"The mod '{namespaceStr}' has been detected.");
                        thirdPartyModLoaded = true;
                        break;
                    }
                }
            }
            else
            {
                DebugLog.LogToFileOnly("Could not get loading extensions");
            }

            return thirdPartyModLoaded;
        }

        private bool CheckMoveItIsLoaded()
        {
            return this.Check3rdPartyModLoaded("MoveIt", true);
        }
    }
}
