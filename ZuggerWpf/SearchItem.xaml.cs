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
using System.Windows.Shapes;

namespace ZuggerWpf
{
    /// <summary>
    /// SearchItem.xaml 的交互逻辑
    /// </summary>
    public partial class SearchItem : Window
    {
        ItemBase Item = null;

        internal SearchItem(ItemBase itembase)
        {
            Item = itembase;
            InitializeComponent();
        }
        
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
            else if (Item !=null && e.Key == Key.Enter && !string.IsNullOrEmpty(txtItemId.Text))
            {
                int id;

                if (int.TryParse(txtItemId.Text, out id))
                {
                    Item.ID = id;
                    Util.OpenItem(Item, ItemOperation.View);
                }
                this.Close();
            }
        }

        private void txtItemId_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox txt = sender as TextBox;

             //屏蔽非法按键
            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || e.Key == Key.Decimal)
            {
                if (txt.Text.Contains(".") && e.Key == Key.Decimal)
                {
                    e.Handled = true;
                    return;
                }
                e.Handled = false;
            }
            else if (((e.Key >= Key.D0 && e.Key <= Key.D9) || e.Key == Key.OemPeriod) && e.KeyboardDevice.Modifiers != ModifierKeys.Shift)
            {
                if (txt.Text.Contains(".") && e.Key == Key.OemPeriod)
                {
                    e.Handled = true;
                    return;
                }
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void txtItemId_TextChanged(object sender, TextChangedEventArgs e)
        {
            //屏蔽中文输入和非法字符粘贴输入
            TextBox textBox = sender as TextBox;
            TextChange[] change = new TextChange[e.Changes.Count];
            e.Changes.CopyTo(change, 0);

            int offset = change[0].Offset;
            if (change[0].AddedLength > 0)
            {
                double num = 0;
                if (!Double.TryParse(textBox.Text, out num))
                {
                    textBox.Text = textBox.Text.Remove(offset, change[0].AddedLength);
                    textBox.Select(offset, 0);
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtItemId.Focus();
        }
    }
}
