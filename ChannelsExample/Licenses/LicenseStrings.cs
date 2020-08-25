using System;
using System.Collections.Generic;

namespace ChannelsExample.Licenses
{
    public static class LicenseStrings
    {
        public static Dictionary<string, string> KnownLicensesMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Apache License 2.0", "Apache-2.0" },
            { "Apache 2.0", "Apache-2.0" },
            { "BSD 3-Clause", "BSD-3-Clause" },
            { "BSD 2-Clause", "BSD-2-Clause" },
            { "GNU General Public License version 2", "GPL-2.0" },
            { "GNU General Public License version 3", "GPL-3.0-only" },
            { "GNU General Public License", "GPL" },
            { "GNU Library General Public License, version 2", "LGPL-2.0" },
            { "GNU Library General Public License, version 2.1", "LGPL-2.1" },
            { "GNU Library General Public License, version 3", "LGPL-2.3" },
            { "GNU Library General Public License", "LGPL" },
            { "GNU Library or Lesser General Public License", "LGPL" },
            { "MIT license", "MIT" }
        };

        public static string TryIdentify(string contents)
        {
            if (!string.IsNullOrEmpty(contents))
            {
                foreach (var (stringToRecognize, identifier) in KnownLicensesMap)
                {
                    if (contents.Contains(stringToRecognize))
                    {
                        return identifier;
                    }
                }

                foreach (var (_, identifier) in KnownLicensesMap)
                {
                    if (contents.Contains(identifier))
                    {
                        return identifier;
                    }
                }
            }

            return "Unknown";
        }
    }
}