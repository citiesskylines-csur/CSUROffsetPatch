using CSUROffsetPatch.Util;
using ColossalFramework.UI;
using ICities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CSUROffsetPatch
{
    public class CSUROffsetPatch : IUserMod
    {
        public static bool IsEnabled = false;

        public string Name
        {
            get { return "CSUR Offset Patch"; }
        }

        public string Description
        {
            get { return "Add offset in Raycast to select CSUR road"; }
        }

        public void OnEnabled()
        {
            IsEnabled = true;
            FileStream fs = File.Create("CSUROffsetPatch.txt");
            fs.Close();
        }

        public void OnDisabled()
        {
            IsEnabled = false;
        }
    }
}
