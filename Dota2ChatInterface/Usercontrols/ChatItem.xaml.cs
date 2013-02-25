using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public partial class ChatItem : UserControl
    {
        public String Scope
        {
            get
            {
                // Strip away the brackets.
                String value = _Scope.Content.ToString();
                return value.Substring(1, value.Length - 2);
            }
            set
            {
                // Add brackets to the scope.
                _Scope.Content = "[" + value + "]";
            }
        }

        public String Sender
        {
            get
            {
                // Strip away the colon.
                String value = _Sender.Content.ToString();
                return value.Substring(0, value.Length - 1);
            }
            set
            {
                // Add a colon after the sender.
                _Sender.Content = value + ":";
            }
        }

        public String Message
        {
            get
            {
                // Return the text unmodified.
                String value = _Message.Text;
                return value;
            }
            set
            {
                // Leave the message as it is.
                _Message.Text = value;
            }
        }

        // Default constructor.
        public ChatItem()
        {
            InitializeComponent();
        }
    }
}
