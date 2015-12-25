﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace JSI
{
    public class TextMenu : List<TextMenu.Item>
    {
        public int currentSelection;
        public string labelColor = JUtil.ColorToColorTag(Color.white);
        public string rightTextColor = JUtil.ColorToColorTag(Color.cyan);
        public string selectedColor = JUtil.ColorToColorTag(Color.green);
        public string disabledColor = JUtil.ColorToColorTag(Color.gray);
        public string menuTitle = string.Empty;
        public int rightColumnWidth;

        public string ShowMenu(int width, int height)
        {
            var menuString = new StringBuilder();

            if (!string.IsNullOrEmpty(menuTitle))
            {
                menuString.AppendLine(menuTitle);
                --height;
            }

            // figure out which entries are visible.
            int numEntries = Count;
            // Sanity check: clamp the current selection
            currentSelection = Math.Min(currentSelection, numEntries - 1);

            // Pick the half-way point of the list
            int midPoint = height >> 1;

            int firstPoint;
            if (midPoint > currentSelection)
            {
                // Menu entry is near the top of the list
                firstPoint = 0;
            }
            else if ((currentSelection + height - midPoint) >= numEntries)
            {
                // Menu entry is near the end of the list.  Account for short
                // lists by clamping to zero.
                firstPoint = Math.Max(0, numEntries - height);
            }
            else
            {
                // Long list, current selection is not near the middle
                firstPoint = currentSelection - midPoint;
            }

            int endPoint = Math.Min(firstPoint + height, numEntries);
            // -2 to account for the first column '  ' or '> ' characters
            int textWidth = width - rightColumnWidth - 2;

            var textItem = new StringBuilder();
            for (int index = firstPoint; index < endPoint; ++index)
            {
                // Clear the string builder
                int strLen = textItem.Length;
                textItem.Remove(0, strLen);

                // Add color strings
                textItem.Append(labelColor);
                if (index == currentSelection)
                {
                    textItem.Append("> ");
                }
                else
                {
                    textItem.Append("  ");
                }
                if (this[index].isDisabled)
                {
                    textItem.Append(disabledColor);
                }
                else if (this[index].isSelected)
                {
                    textItem.Append(selectedColor);
                }

                if (!string.IsNullOrEmpty(this[index].labelText))
                {
                    textItem.Append(this[index].labelText.PadRight(textWidth).Substring(0, textWidth));

                    // Only allow a 'right text' to be added if we already have text.
                    if (!string.IsNullOrEmpty(this[index].rightText) && rightColumnWidth > 0)
                    {
                        if (!this[index].isDisabled && !this[index].isSelected)
                        {
                            textItem.Append(rightTextColor);
                        }

                        textItem.Append(this[index].rightText.PadLeft(rightColumnWidth).Substring(0, rightColumnWidth));
                    }
                }

                menuString.AppendLine(textItem.ToString());
            }

            return menuString.ToString();
        }

        public void NextItem()
        {
            currentSelection = Math.Min(currentSelection + 1, Count - 1);
        }

        public void PreviousItem()
        {
            currentSelection = Math.Max(currentSelection - 1, 0);
        }

        public void SelectItem()
        {
            // Do callback
            if (!this[currentSelection].isDisabled && this[currentSelection].action != null)
            {
                this[currentSelection].action(currentSelection, this[currentSelection]);
            }
        }

        public Item GetCurrentItem()
        {
            return this[currentSelection];
        }

        public int GetCurrentIndex()
        {
            return currentSelection;
        }

        // Set the isSelected flag for the index menu item.  If "exclusive"
        // is set, all other isSelected flags are cleared.
        public void SetSelected(int index, bool exclusive)
        {
            if (exclusive)
            {
                for (int i = 0; i < Count; ++i )
                {
                    this[i].isSelected = false;
                }
            }

            if (index >= 0 && index < Count)
            {
                this[index].isSelected = true;
            }
        }

        public class Item
        {
            public string labelText = string.Empty;
            public string rightText = string.Empty;
            public int id;
            public bool isDisabled;
            public bool isSelected;
            public Action<int, Item> action;
            // Mihara: This can be much more terse to use if there is a constructor with optional parameters.
            // Even if it's finicky about "" rather than string.Empty.
            public Item(string labelText = "", Action<int, Item> action = null, bool isSelected = false, string rightText = "", bool isDisabled = false)
            {
                this.labelText = labelText;
                this.rightText = rightText;
                this.action = action;
                this.isDisabled = isDisabled;
                this.isSelected = isSelected;
                this.id = -1;
            }
            // Consolidated/simple constructor - set most things to their
            // defaults, and require three fields to be supplied.
            public Item(string labelText, Action<int, Item> action, int id)
            {
                this.labelText = labelText;
                this.rightText = "";
                this.action = action;
                this.isDisabled = false;
                this.isSelected = false;
                this.id = id;
            }
        }
    }
}
