using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace VkRadio.WebTools.WebGrabber.Utils;

public class XmlAttributeDupesHelper
{
    public static string[] InitCache(string pathToSettingsFile)
    {
        var result = new string[0];

        if (!string.IsNullOrEmpty(pathToSettingsFile) && File.Exists(pathToSettingsFile))
        {
            var xelRoot = XElement.Load(pathToSettingsFile);
            var xelAttributeDuplicates = xelRoot.Element("AttributeDuplicates");

            if (xelAttributeDuplicates != null)
            {
                var attDupes = new List<string>();

                foreach (var xelAtt in xelAttributeDuplicates.Elements("AttributeDuplicate"))
                {
                    attDupes.Add($"{xelAtt.Value}=\"{xelAtt.Value}\"");
                }

                result = new string[attDupes.Count];

                for (var i = 0; i < attDupes.Count; i++)
                {
                    result[i] = attDupes[i];
                }
            }
        }

        return result;
    }

    public static void AddDupe(string attributeName, string pathToSettingsFile, ref string[] inout_cache)
    {
        var xelRoot = XElement.Load(pathToSettingsFile);
        var xelDupes = xelRoot.Element("AttributeDuplicates");

        if (xelDupes == null)
        {
            xelDupes = new XElement("AttributeDuplicates");
            xelRoot.Add(xelDupes);
        }

        xelDupes.Add(new XElement("AttributeDuplicate", attributeName));

        var cacheList = new List<string>(inout_cache);

        cacheList.Add($"{attributeName}=\"{attributeName}\"");
        inout_cache = new string[cacheList.Count];

        for (var i = 0; i < cacheList.Count; i++)
        {
            inout_cache[i] = cacheList[i];
        }

        xelRoot.Save(pathToSettingsFile);
    }
}
