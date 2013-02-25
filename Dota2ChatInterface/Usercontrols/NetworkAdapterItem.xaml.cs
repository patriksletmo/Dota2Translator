using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Dota2ChatInterface
{
    public partial class NetworkAdapterItem : UserControl
    {
        // The adapter IP address.
        public String IP
        {
            get
            {
                return ItemIP.Content.ToString();
            }
            set
            {
                ItemIP.Content = value;
            }
        }

        // The adapter description.
        public String Description
        {
            get
            {
                return ItemDescription.Content.ToString();
            }
            set
            {
                ItemDescription.Content = value;
            }
        }

        // Used to retrieve the actual adapter.
        public int AdapterIndex = -1;

        // Default constructor.
        public NetworkAdapterItem()
        {
            InitializeComponent();
        }
    }
}
