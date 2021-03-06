using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace TracerX.Viewer {
    // Represents one row of data in the viewer.  Several contiguous rows can map to
    // a single Record object if the Record contains newlines and has been expanded.
    internal class Row {
        public Row(Record record, int rowIndex, int recordLine) {
            Index = rowIndex;
            Init(record, recordLine);
        }

        public void Init(Record record, int recordLine) {
            Rec = record;
            Line = (ushort)recordLine;
        }

        // The Record object whose data is shown on this row.
        public Record Rec;

        // The index of this object in the _rows array.
        public int Index;

        // If the text in Record contains embedded newlines and it has been split/expanded
        // into multiple lines, this is the index of the line to display on this row.
        public ushort Line = 0;

        public bool IsBookmarked {
            get { return Rec.IsBookmarked[Line]; }
            set { Rec.IsBookmarked[Line] = value; }
        }

        //public void SimulateSelected(bool sim) {
        //    _simulateSelected = sim;
        //    ListViewItem item = MainForm.TheMainForm.TheListView.Items[Index];
        //    SimulateSelectedItem(sim, item);
        //}

        // Used by Find and CallStack.
        // Returns non-indented, non-truncated text from the Text column.
        public override string ToString() {
            return Rec.GetLine(Line, ' ', 0, false);
        }

        // Returns indented, non-truncated text.
        // Used for copying text to the clipboard.
        public string GetFullIndentedText() {
            return Rec.GetLine(Line, Settings1.Default.IndentChar, Settings1.Default.IndentAmount, false);
        }

        public static DateTime ZeroTime = DateTime.MinValue;

        //private void SimulateSelectedItem(bool sim, ListViewItem item) {
        //    if (sim) {
        //        // Remember orginal colors and update the item.
        //        item.Selected = false;
        //        _defaultBackColor = item.BackColor;
        //        _defaultForeColor = item.ForeColor;
        //        item.BackColor = SystemColors.Highlight;
        //        item.ForeColor = Color.White;
        //    } else {
        //        // Restore item to original colors.
        //        item.BackColor = _defaultBackColor;
        //        item.ForeColor = _defaultForeColor;
        //        item.Selected = true;
        //    }
        //}

        //// When true, sets background color of generated ListViewItem to make it look selected.
        //private bool _simulateSelected;

        // Array used to initialize the subitems of each ListViewItem generated by MakeItem.
        private static string[] _fields = new string[8];
        private const int _bookmarkIndex = 0;
        private const int _plusIndex = 1;
        private const int _minusIndex = 3;
        private const int _downIndex = 5;
        private const int _upIndex = 7;
        private const int _sublineIndex = 10;
        private const int _lastSublineIndex = 12;
        private static Color _defaultBackColor;
        private static Color _defaultForeColor;

        // Make a ListViewItem from a Row object.
        public ListViewItem MakeItem(Row previousRow) {
            SetFields(previousRow);

            ListViewItem item = new ListViewItem(_fields, ImageIndex);
            item.Tag = this;

            //if (_simulateSelected) {
            //    SimulateSelectedItem(true, item);
            //}

            return item;
        }

        private void SetFields(Row previousRow) {
            int currentNdx = 0;
            ListView theListView = MainForm.TheMainForm.TheListView;
            MainForm mainForm = MainForm.TheMainForm;
            //Debug.Print("TheListView.Columns.Count = " + theListView.Columns.Count);

            if (_fields.Length != theListView.Columns.Count)
                _fields = new string[theListView.Columns.Count];

            // This logic requires TheListView.Columns to be in the same order, and
            // possibly a subset of, OriginalColumns.

            if (theListView.Columns[currentNdx] == mainForm.headerText) {
                _fields[currentNdx++] = Rec.GetLine(Line, Settings1.Default.IndentChar, Settings1.Default.IndentAmount, true);
                if (currentNdx == MainForm.TheMainForm.TheListView.Columns.Count) return;
            }

            if (theListView.Columns[currentNdx] == mainForm.headerLine) {
                _fields[currentNdx++] = Rec.GetRecordNum(Line);
                if (currentNdx == theListView.Columns.Count) return;
            }

            if (theListView.Columns[currentNdx] == mainForm.headerLevel) {
                _fields[currentNdx++] = Rec.Level.ToString();
                if (currentNdx == theListView.Columns.Count) return;
            }

            if (theListView.Columns[currentNdx] == mainForm.headerLogger) {
                _fields[currentNdx++] = Rec.Logger.Name;
                if (currentNdx == theListView.Columns.Count) return;
            }

            if (theListView.Columns[currentNdx] == mainForm.headerThreadId) {
                _fields[currentNdx++] = Rec.ThreadId.ToString();
                if (currentNdx == theListView.Columns.Count) return;
            }

            if (theListView.Columns[currentNdx] == mainForm.headerThreadName) {
                _fields[currentNdx++] = Rec.ThreadName.Name;
                if (currentNdx == theListView.Columns.Count) return;
            }

            if (theListView.Columns[currentNdx] == mainForm.headerTime) {
                if (previousRow == null || previousRow.Rec.Time != this.Rec.Time || Settings1.Default.DuplicateTimes) {
                    if (Settings1.Default.RelativeTime) {
                        _fields[currentNdx++] = FormatTimeSpan(Rec.Time - ZeroTime);
                    } else {
                        _fields[currentNdx++] = Rec.Time.ToLocalTime().ToString(@"MM/dd/yy HH:mm:ss.fff");
                    }
                } else {
                    _fields[currentNdx++] = string.Empty;
                }
                if (currentNdx == theListView.Columns.Count) return;
            }

            if (theListView.Columns[currentNdx] == mainForm.headerMethod) {
                _fields[currentNdx++] = Rec.MethodName;
            }
        }

        private static string FormatTimeSpan(TimeSpan ts) {
            string raw = ts.ToString();
            int colon = raw.IndexOf(':');
            int period = raw.IndexOf('.', colon);

            if (period == -1) {
                return raw;
            } else if (period + 4 >= raw.Length) {
                return raw;
            } else {
                return raw.Remove(period + 4);
            }
        }

        // Returns the image index to use for this row.
        public int ImageIndex {
            get {
                int ret = -1;

                if (Rec.IsEntry) {
                    if (Rec.IsCollapsed) {
                        ret = _plusIndex;
                    } else {
                        ret = _minusIndex;
                    }
                } else if (Rec.HasNewlines) {
                    if (Index == Rec.FirstRowIndex) {
                        // This is the first visible line.
                        if (Rec.IsCollapsed) {
                            ret = _downIndex;
                        } else {
                            ret = _upIndex;
                        }
                    } else if (Line == Rec.Lines.Length - 1) {
                        ret = _lastSublineIndex;
                    } else {
                        ret = _sublineIndex;
                    }
                }

                if (IsBookmarked) {
                    ret += 1;
                }

                return ret;
            }
        }

        public void ShowFullText() {
            FullText ft = new FullText(Rec.GetTextForWindow(Line));
            ft.Show(MainForm.TheMainForm);
        }
    }
}
