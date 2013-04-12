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
using System.IO;
using System.Net;

namespace Updater
{
    class FileDownloader
    {
        // The path to the temporary downloaded file.
        private String TemporaryFile;

        // Whether or not the download succeeded.
        private Boolean Downloaded = false;

        // The name of the file.
        private String _FileName;
        public String FileName
        {
            get
            {
                return _FileName;
            }
        }

        public FileDownloader(String fileName)
        {
            this._FileName = fileName;
        }

        // Converts an array of file names into an array of FileDownloader instances.
        public static FileDownloader[] GetFiles(String[] fileNames)
        {
            FileDownloader[] files = new FileDownloader[fileNames.Length];
            for (int i = 0; i < fileNames.Length; i++)
            {
                files[i] = new FileDownloader(fileNames[i]);
            }

            return files;
        }

        // Downloads the file to a temporary location.
        public Boolean Download()
        {
            try
            {
                // Construct the temporary file path.
                TemporaryFile = Path.Combine(Path.GetTempPath(), "Dota 2 Translator/" + Guid.NewGuid().ToString());
                Directory.CreateDirectory(Path.GetDirectoryName(TemporaryFile));

                // Download the file.
                using (WebClient client = new WebClient())
                {
                    client.Proxy = null; // Don't use a proxy.
                    client.DownloadFile(App.UpdateUrl + "files/" + FileName, TemporaryFile);

                    // Retrieve the last modified date of the remote file.
                    String lastModified_string = client.ResponseHeaders.Get("Last-Modified");
                    if (lastModified_string != null)
                    {
                        try
                        {
                            // Parse the date.
                            DateTime lastModified = DateTime.Parse(lastModified_string);

                            // Set the last modified property of the file to match the remote version.
                            File.SetLastWriteTimeUtc(TemporaryFile, lastModified);
                        }
                        catch
                        {
                        }
                    }
                }

                // Mark the file as succeeded.
                Downloaded = true;

                return true;
            }
            catch
            {
            }

            return false;
        }

        // Applies the update to the specified directory.
        public Boolean Apply(String path)
        {
            // Don't apply a file that has not been downloaded.
            if (!Downloaded)
                return false;

            // Write to tmp_Updater.exe instead of Updater.exe.
            if (FileName.Equals("Updater.exe"))
            {
                _FileName = "tmp_Updater.exe";
            }

            try
            {
                // Copy the temporary file to the correct directory.
                File.Copy(TemporaryFile, Path.Combine(path, FileName), true);
            }
            catch
            {
                return false;
            }

            return true;
        }

        // Deletes the temporary file.
        public void Cleanup()
        {
            try
            {
                File.Delete(TemporaryFile);
            }
            catch
            {
            }
        }

    }
}
