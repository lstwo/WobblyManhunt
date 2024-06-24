using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WobblyManhunt
{
    public static class Utils
    {
        public static AssetBundle QuickLoadAssetBundle(string assetBundleName)
        {
            string text = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), assetBundleName);
            return AssetBundle.LoadFromFile(text);
        }
    }
}
