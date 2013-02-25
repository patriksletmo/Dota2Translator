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
using System.Threading.Tasks;
using System.Net;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Dota2ChatInterface
{
    class Translate
    {
        // The url to request.
        private static String requestURL = "http://translate.google.com/translate_a/t?client=t&sl=auto&multires=1&sc=1"; // Has to be appended with &hl=LANGUAGE, &tl=LANGUAGE, and &text=STRING"

        // Translates the specified string using an un-official Google Translate API.
        public static String TranslateString(String str)
        {
            SettingsHandler settings = SettingsHandler.GetInstance();

            try
            {
                // Encode the message for use in url.
                String encodedMessage = Uri.EscapeDataString(str);
                String encodedLanguage = Uri.EscapeDataString(settings.TranslateTo);

                // Create an url.
                String url = requestURL + "&hl=" + encodedLanguage + "&tl=" + encodedLanguage + "&text=" + encodedMessage;

                // Create a request that looks like it came from a normal Google Translate session.
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Proxy = null; // Disable proxy lookup. Saves us around 7 seconds of waiting for one (which might not even exist).
                request.Host = "translate.google.com";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0"; // Mozilla Firefox 19.0. Windows 7 64-bit. Updated 23/2 - 2013.

                // Get the response.
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                // Read the data.
                Stream stream = response.GetResponseStream();
                String result = "";
                byte[] buf = new byte[8192];
                int count = 0;
                while ((count = stream.Read(buf, 0, buf.Length)) > 0)
                {
                    result += Encoding.UTF8.GetString(buf, 0, count);
                }

                // Parse the received data as a JSON array.
                JArray arr = (JArray)JsonConvert.DeserializeObject(result);
                JValue translatedObject = (JValue)((JArray)((JArray)arr[0])[0])[0]; //Them casts.
                String translatedMessage = (String)translatedObject.Value;

                return translatedMessage;
            }
            catch
            {
                // An invalid message, non-existant translation language or network error could cause the main application to crash.
                // Return the original message if any of these erros occurred.

                return str;
            }
        }
        
    }
}
