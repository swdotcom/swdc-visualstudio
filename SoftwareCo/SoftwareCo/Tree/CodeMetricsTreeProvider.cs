﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SoftwareCo
{
    class CodeMetricsTreeProvider
    {
        public static TreeViewItem BuildTreeItemParent(CodeMetricsTreeItem treeItem)
        {
            DependencyObject parent = null;
            try
            {
                parent = VisualTreeHelper.GetParent(treeItem);
                while (!(parent is CodeMetricsTreeItem))
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }
            }
            catch (Exception e)
            {
                //
            }
            return parent as CodeMetricsTreeItem;
        }

        public static TreeViewItem BuildContextItemButton(string id, string text, string iconName, MouseButtonEventHandler handler)
        {
            CodeMetricsTreeItem treeItem = new CodeMetricsTreeItem(id);

            // create a stack panel
            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;

            Label label = new Label();
            label.Content = text;
            label.Foreground = Brushes.DarkCyan;

            // add to the stack
            stack.Children.Add(label);

            if (!string.IsNullOrEmpty(iconName))
            {
                Image img = SoftwareCoUtil.CreateImage(iconName);
                img.MouseDown += handler;
                img.Cursor = Cursors.Hand;
                img.HorizontalAlignment = HorizontalAlignment.Right;
                img.Name = id;
                stack.Children.Add(img);
            }

            // assign the stack to the header
            treeItem.Header = stack;
            return treeItem;
        }

        public static TreeViewItem BuildTreeItem(string id, string text, string iconName = null, MouseButtonEventHandler handler = null) {
            CodeMetricsTreeItem treeItem = new CodeMetricsTreeItem(id);

            // create a stack panel
            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;

            if (!string.IsNullOrEmpty(iconName))
            {
                stack.Children.Add(SoftwareCoUtil.CreateImage(iconName));
            }

            Label label = new Label();
            label.Content = text;
            label.Foreground = Brushes.DarkCyan;
            label.Cursor = Cursors.Hand;
            if (handler != null)
            {
                label.MouseDown += handler;
            }

            // add to the stack
            stack.Children.Add(label);

            // assign the stack to the header
            treeItem.Header = stack;
            return treeItem;
        }

    }
}
