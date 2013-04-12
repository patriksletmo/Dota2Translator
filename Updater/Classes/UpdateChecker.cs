/*
Copyright (c) 2013 Patrik Sletmo

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;

namespace Updater
{
    public class UpdateChecker
    {
        // Returns an array containing the files needing an update.
        public static String[] CheckForUpdates()
        {
            // Retrieve the directory of the updater.
            String installationDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            List<String> filesNeedingUpdate = new List<String>();

            // Fetch Versions.xml.
            XPathNavigator navigator = FetchVersionXML();
            if (navigator == null)
            {
                // The server did not return an expected response.
                return new String[0];
            }

            XPathNodeIterator iterator = navigator.Select("/CurrentVersion/File");
            foreach (XPathNavigator item in iterator)
            {
                // Retrieve the name and md5 checksum of the file.
                String name = item.GetAttribute("Name", "");
                String md5 = item.GetAttribute("MD5", "");

                // Retrieve the date of modification of the file.
                String lastModified_string = item.GetAttribute("LastModified", "");

                // Skip files with a missing value.
                if (name == null || md5 == null)
                    continue;

                Boolean isNewerBuild = false;
                if (lastModified_string.Length != 0)
                {
                    // Retrieve the last modified date of the local file.
                    DateTime lastWrite = File.GetLastWriteTimeUtc(Path.Combine(installationDirectory, name));

                    // Parse the remote date.
                    DateTime lastModified = DateTime.Parse(lastModified_string);

                    // Check if the local file was created after than the remote file.
                    if (lastWrite.CompareTo(lastModified) > 0)
                    {
                        // Don't update files that were built after the current server version.
                        isNewerBuild = true;
                    }
                }

                // Calculate the md5 checksum of the installed file.
                String localMd5 = CalculateMD5(Path.Combine(installationDirectory, name));
                if (!isNewerBuild && (localMd5 == null || !md5.Equals(localMd5)))
                {
                    // The md5's does not match - The file needs an update.
                    filesNeedingUpdate.Add(name);
                }
            }

            return filesNeedingUpdate.ToArray();
        }

        // Checks if there is an update available.
        public static Boolean HasUpdate()
        {
            // Check there's any file needing an update.
            return CheckForUpdates().Length > 0;
        }

        // Fetches the Versions.xml from the server.
        private static XPathNavigator FetchVersionXML()
        {
            try
            {
                // Request the file.
                WebRequest request = WebRequest.Create(App.UpdateUrl + "Versions.xml");
                request.Proxy = null; // Don't use a proxy.

                // Retrieve the response stream.
                Stream stream = request.GetResponse().GetResponseStream();

                // Create an XPathNavigator out of the xml.
                XPathDocument document = new XPathDocument(stream);
                XPathNavigator navigator = document.CreateNavigator();

                return navigator;
            }
            catch
            {
            }

            // Unable to fetch the xml.
            return null;
        }

        // Calculates the MD5 checksum for the specified file.
        private static String CalculateMD5(String file)
        {
            // Create a new MD5 instance.
            using (MD5 md5 = MD5.Create())
            {
                // Make sure the file exists.
                if (!File.Exists(file))
                    return null;

                // Read the file.
                using (Stream stream = File.OpenRead(file))
                {
                    // Calculate a lowercase md5 checksum for the file.
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
        }
    }
}
