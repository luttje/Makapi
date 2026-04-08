using System;
using System.Collections.Generic;
using System.IO;

namespace ApiMonkey.Models
{
    public class SettingsData
    {
        public List<string> RequestRoots { get; set; } = [];

        public SettingsData() { }

        public SettingsData(List<string> requestRoots)
        {
            RequestRoots = requestRoots;
        }

        /// <summary>
        /// Adds the given path to request roots, but only if it's not already there and if it's not a child of an existing root.
        /// </summary>
        /// <param name="path"></param>
        internal void TryAddExclusiveRoot(string path)
        {
            if (RequestRoots.Contains(path))
                return;

            foreach (var root in RequestRoots)
            {
                if (IsSubdirectory(root, path))
                    return;
            }

            RequestRoots.Add(path);
        }

        private bool IsSubdirectory(string parent, string child)
        {
            var parentUri = new Uri(parent.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? parent
                : parent + Path.DirectorySeparatorChar);
            var childUri = new Uri(child);

            return parentUri.IsBaseOf(childUri);
        }
    }
}
