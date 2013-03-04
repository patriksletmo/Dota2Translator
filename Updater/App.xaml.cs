using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Updater
{
    public partial class App : Application
    {
        // The url pointing to where the update files are located.
        public const String UpdateUrl = "http://sletmo.com/dota2translator/updater/";
    }
}
