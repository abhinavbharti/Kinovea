﻿#region License
/*
Copyright © Joan Charmant 2011.
joan.charmant@gmail.com 
 
This file is part of Kinovea.

Kinovea is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License version 2 
as published by the Free Software Foundation.

Kinovea is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Kinovea. If not, see http://www.gnu.org/licenses/.
*/
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.Video
{
    /// <summary>
    /// Load and provide access to the list of video readers/writers.
    /// </summary>
    public static class VideoTypeManager
    {
        #region Properties
        public static IEnumerable<string> SupportedExtensions
        {
            get { return m_VideoReaders.Keys; }
        }
        #endregion
        
        #region Members
        private static Dictionary<string, Type> m_VideoReaders = new Dictionary<string, Type>();
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion
        
        #region Public methods
        /// <summary>
        /// Find and register types implementing the VideoReader base class.
        /// </summary>
        public static void LoadVideoReaders()
        {
            // Check in this assembly and assemblies in the plugin folder.
            List<Assembly> assemblies = new List<Assembly>();
            assemblies.Add(typeof(VideoTypeManager).Assembly);
            
            string dir = Path.GetDirectoryName(Application.ExecutablePath) + "\\filetypes\\";
            if(Directory.Exists(dir))
            {
                foreach (string fileName in Directory.GetFiles(dir, "*.dll"))
                {
                    Assembly pluginAssembly = null;
                    try
                    {
                        pluginAssembly = Assembly.LoadFrom(fileName);
                        assemblies.Add(pluginAssembly);
                    }
                    catch (Exception)
                    {
                        log.ErrorFormat("Could not load assembly {0} for file types plugin", fileName);
                    }
                }
            }
            
            // Register the VideoReaders implementations with the extensions they support.
            foreach (Assembly a in assemblies)
            {
                foreach(Type t in a.GetTypes())
                {
                    if(t.IsSubclassOf(typeof(VideoReader)) && !t.IsAbstract)
                    {
                        // Retrieve the extension list from the attribute, add each entry to the readers dictionary.
                        object[] attributes = t.GetCustomAttributes(typeof(SupportedExtensionsAttribute), false);
                        if(attributes.Length > 0)
                        {
                           string[] extensions = ((SupportedExtensionsAttribute)attributes[0]).Extensions;
                           RegisterExtensions(extensions, t);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Instanciate a video reader that supports the target extension.
        /// </summary>
        /// <param name="_extension"></param>
        public static VideoReader GetVideoReader(string _extension)
        {
            // The FFMpeg plugin will support the wildcard as a fallback mechanism.
            VideoReader reader = null;
            Type readerType;
            bool found = m_VideoReaders.TryGetValue(_extension, out readerType);
            
            if(!found)
                found = m_VideoReaders.TryGetValue("*", out readerType);
            
            if(found)
            {
                ConstructorInfo ci = readerType.GetConstructor(System.Type.EmptyTypes);
                if(ci != null)
                    reader = (VideoReader)Activator.CreateInstance(readerType, null);
            }
            
            return reader;
        }
        #endregion
        
        #region Private methods
        private static void RegisterExtensions(string[] _extensions, Type _readerType)
        {
            log.DebugFormat("Registering extensions for {0} : {1}", _readerType.Name, string.Join("; ", _extensions));
            
            foreach(string extension in _extensions)
            {
                if(!m_VideoReaders.ContainsKey(extension))
                    m_VideoReaders.Add(extension, _readerType);
            }
        }
        #endregion
    }
}