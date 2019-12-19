﻿using ColossalFramework;
using ColossalFramework.Math;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ColossalFramework.Globalization;
using System.Reflection;
using System.IO;
using CSUROffsetPatch.Util;
using ColossalFramework.UI;
using CSUROffsetPatch.NewData;

namespace CSUROffsetPatch
{
    public class Threading : ThreadingExtensionBase
    {
        public static bool isFirstTime = true;
        public static Assembly MoveIt = null;

        public override void OnBeforeSimulationFrame()
        {
            base.OnBeforeSimulationFrame();
            if (Loader.CurrentLoadMode == LoadMode.LoadGame || Loader.CurrentLoadMode == LoadMode.NewGame || Loader.CurrentLoadMode == LoadMode.NewMap || Loader.CurrentLoadMode == LoadMode.LoadMap || Loader.CurrentLoadMode == LoadMode.NewAsset || Loader.CurrentLoadMode == LoadMode.LoadAsset)
            {
                if (CSUROffsetPatch.IsEnabled)
                {
                    CheckDetour();
                }
            }
        }

        public void DetourAfterLoad()
        {
            //This is for Detour RealCity method
            DebugLog.LogToFileOnly("Init DetourAfterLoad");
            bool detourFailed = false;

            if (Loader.isMoveItRunning)
            {
                Assembly MoveIt = Assembly.Load("MoveIt");
                //1
                //private static bool RayCastNode(ushort nodeid, ref NetNode node, Segment3 ray, float snapElevation, out float t, out float priority)
                DebugLog.LogToFileOnly("Detour MoveIt::MoveItTool.RayCastNode calls");
                try
                {
                    Loader.Detours.Add(new Loader.Detour(MoveIt.GetType("MoveIt.MoveItTool").GetMethod("RayCastNode", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, new Type[] { typeof(NetNode).MakeByRefType(), typeof(ColossalFramework.Math.Segment3), typeof(float), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }, null),
                                           typeof(CustomNetNode).GetMethod("MoveItRayCastNode", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, null, new Type[] { typeof(NetNode).MakeByRefType(), typeof(Segment3), typeof(float), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }, null)));
                }
                catch (Exception)
                {
                    DebugLog.LogToFileOnly("Could not detour MoveIt::MoveItTool.RayCastNode");
                    detourFailed = true;
                }

                if (detourFailed)
                {
                    DebugLog.LogToFileOnly("DetourAfterLoad failed");
                }
                else
                {
                    DebugLog.LogToFileOnly("DetourAfterLoad successful");
                }
            }
        }

        public void CheckDetour()
        {
            if (isFirstTime && Loader.DetourInited)
            {
                isFirstTime = false;
                DetourAfterLoad();
                if (Loader.DetourInited)
                {
                    DebugLog.LogToFileOnly("ThreadingExtension.OnBeforeSimulationFrame: First frame detected. Checking detours.");
                    List<string> list = new List<string>();
                    foreach (Loader.Detour current in Loader.Detours)
                    {
                        if (!RedirectionHelper.IsRedirected(current.OriginalMethod, current.CustomMethod))
                        {
                            list.Add(string.Format("{0}.{1} with {2} parameters ({3})", new object[]
                            {
                    current.OriginalMethod.DeclaringType.Name,
                    current.OriginalMethod.Name,
                    current.OriginalMethod.GetParameters().Length,
                    current.OriginalMethod.DeclaringType.AssemblyQualifiedName
                            }));
                        }
                    }
                    DebugLog.LogToFileOnly(string.Format("ThreadingExtension.OnBeforeSimulationFrame: First frame detected. Detours checked. Result: {0} missing detours", list.Count));
                    if (list.Count > 0)
                    {
                        string error = "CSUROffsetPatch detected an incompatibility with another mod! You can continue playing but it's NOT recommended. CSUROffsetPatch will not work as expected. Send CSUROffsetPatch.txt to Author.";
                        DebugLog.LogToFileOnly(error);
                        string text = "The following methods were overriden by another mod:";
                        foreach (string current2 in list)
                        {
                            text += string.Format("\n\t{0}", current2);
                        }
                        DebugLog.LogToFileOnly(text);
                        UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Incompatibility Issue", text, true);
                    }
                }
            }
        }
    }
}
