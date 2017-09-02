/*
 * MIT License
 *
 * Copyright (c) 2017 Dr. Christian Benjamin Ries
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;
using Newtonsoft.Json.Linq;
using RailwayEssentialCore;

namespace RailwayEssentialWeb
{
    public class TrackViewerJsCallback : ITrackViewerJsCallback
    {
        public event EventHandler CellEdited;
        public event EditModeChangedDelegator EditModeChanged;
        public event CellClickedDelegator CellClicked;
        public event CellSelectedDelegator CellSelected;

        public ITrackEdit TrackEdit { get; set; }

        public void message(string msg)
        {
           //Trace.WriteLine("Message: " + msg.Trim());
        }

        public void cellClicked(int x, int y)
        {
            if (CellClicked != null)
                CellClicked(this, x, y);
        }

        public void cellEdited(int x, int y, int themeId)
        {
            if (TrackEdit != null)
                TrackEdit.ChangeSymbol(x, y, themeId);

            if (CellEdited != null)
            {
                var o = new JObject
                {
                    ["x"] = x,
                    ["y"] = y,
                    ["themeId"] = themeId
                };

                CellEdited(this, new JsonObjectEventArgs(o)); 
            }
        }

        public void cellRotated(int x, int y, string orientation)
        {
            if (TrackEdit != null)
                TrackEdit.RotateSymbol(x, y, orientation);
        }

        public void cellSelected(int x, int y)
        {
            if (CellSelected != null)
                CellSelected(this, x, y);
        }

        public void editModeChanged(bool state)
        {
            if (EditModeChanged != null)
                EditModeChanged(this, state);
        }
    }
}
