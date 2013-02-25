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
