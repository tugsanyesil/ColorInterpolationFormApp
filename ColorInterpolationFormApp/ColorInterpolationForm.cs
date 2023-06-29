using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using ColorInterpolationLib;

namespace ColorInterpolationFormApp
{
    public partial class ColorInterpolationForm : Form
    {
        public ColorInterpolationForm()
        {
            InitializeComponent();
        }

        public static string FileFilterExtension = "txt";
        public static string FileExtension = $".{FileFilterExtension}";
        public static string FileName = $"data{FileExtension}";
        public static string FileFilter = $"{FileFilterExtension} files (*{FileExtension})|*{FileExtension}";
        public ColorInterpolation ColorInt;
        public List<Button> Buttons = new List<Button>();
        public Point GridZeroLocation;
        public Size GridElementOffset;
        public Size GridElementSize;
        public Size GridElementSizeWithOffset;
        public int GridColumn = 9;
        private void ColorInterpolationForm_Load(object sender, EventArgs e)
        {
            GridZeroLocation = AddDataButton.Location;
            GridElementSize = new Size(100, 100);
            GridElementOffset = new Size(10, 10);
            GridElementSizeWithOffset = AddDataButton.Size + GridElementOffset;

            AddDataButton.Size = GridElementSize;

            Form_Refresh(true, "");
        }

        private void Form_Refresh(bool bool_load, string FileName)
        {
            if (FileName != null)
            {
                if (bool_load)
                {
                    if (FileName == "")
                    {
                        FileName = ColorInterpolationForm.FileName;
                    }
                    ColorInt = ColorInterpolation.Load(Path.Combine(GetExecutingDirectoryName(), FileName));
                }

                Buttons.ForEach(button => Controls.Remove(button));
                AddDataButton.Location = GridZeroLocation;
                Buttons.Clear();

                for (int i = 0; i < ColorInt.Doubles.Length; i++)
                {
                    AddData(ColorInt.Colors[i], ColorInt.Doubles[i]);
                }

                Bar.Minimum = (int)(Math.Ceiling(ColorInt.Doubles.ToList().Min()));
                Bar.Maximum = (int)(Math.Floor(ColorInt.Doubles.ToList().Max()));
                Bar.TickFrequency = 10 * Buttons.Count;
            }

            Bar_Scroll(null, EventArgs.Empty);

            double number_of_points = 10;

            Graph.Series[0].Points.Clear();
            Graph.Series[1].Points.Clear();
            Graph.Series[2].Points.Clear();
            Graph.Series[3].Points.Clear();

            double difference = Bar.Maximum - Bar.Minimum;
            if (difference > 0)
            {
                for (double i = Bar.Minimum; i <= Bar.Maximum; i += difference / number_of_points)
                {
                    var color = ColorInt.DoubleToColor(i).Value;

                    Graph.Series[0].Points.AddXY(i, color.A);
                    Graph.Series[1].Points.AddXY(i, color.R);
                    Graph.Series[2].Points.AddXY(i, color.G);
                    Graph.Series[3].Points.AddXY(i, color.B);
                }
            }
        }

        private void Bar_Scroll(object sender, EventArgs e)
        {
            CountLabel.Text = Bar.Value.ToString();
            ResultPanel.BackColor = ColorInt.DoubleToColor(Bar.Value).Value;
        }

        private static DialogResult InputBox(string title, string promptText, out double value)
        {
            Button buttonOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            Button buttonCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };

            Form form = new Form
            {
                Text = title,
                ClientSize = new Size(396, 107),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                MinimizeBox = false,
                MaximizeBox = false,
                AcceptButton = buttonOk,
                CancelButton = buttonCancel
            };

            Label label = new Label
            {
                Text = promptText,
                AutoSize = true
            };
            label.SetBounds(9, 20, 372, 13);

            TextBox textBox = new TextBox { Text = 0.ToString() };
            textBox.Anchor |= AnchorStyles.Right;

            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });

            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);

            DialogResult dialogResult = form.ShowDialog();
            if (dialogResult == DialogResult.OK && double.TryParse(textBox.Text, out value))
            {
                dialogResult = DialogResult.OK;
            }
            else
            {
                value = 0;
                dialogResult = DialogResult.Cancel;
            }

            return dialogResult;
        }

        private void GetGridNumbers(int Count, out int gridRow, out int gridColumn)
        {
            gridRow = Count / GridColumn;
            gridColumn = Count - gridRow * GridColumn;
        }

        private Point GetGridLocation(int Count)
        {
            GetGridNumbers(Count, out int gridRow, out int gridColumn);
            return new Point(GridZeroLocation.X + gridColumn * GridElementSizeWithOffset.Width, GridZeroLocation.Y + gridRow * GridElementSizeWithOffset.Height);
        }

        private void Item_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            int i = Buttons.FindIndex(button => button == (Button)sender);
            if (i != -1)
            {
                bool refresh = false;
                if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Left)
                {
                    if (!(i == 0 && e.KeyCode == Keys.Left) && !(i == Buttons.Count - 1 && e.KeyCode == Keys.Right))
                    {
                        int next_i = i + ((e.KeyCode == Keys.Right) ? 1 : -1);

                        (Buttons[next_i].BackColor, Buttons[i].BackColor) = (Buttons[i].BackColor, Buttons[next_i].BackColor);
                        (Buttons[next_i].ForeColor, Buttons[i].ForeColor) = (Buttons[i].ForeColor, Buttons[next_i].ForeColor);
                        (ColorInt.Colors[next_i], ColorInt.Colors[i]) = (ColorInt.Colors[i], ColorInt.Colors[next_i]);
                        refresh = true;
                    }
                }
                else if (e.KeyCode == Keys.Delete)
                {
                    Controls.Remove(Buttons[i]);
                    Buttons.RemoveAt(i);
                    ColorInt = new ColorInterpolation(Buttons.Select(button => button.BackColor).ToArray(), Buttons.Select(button => double.Parse(button.Text)).ToArray());

                    for (int j = i; j < Buttons.Count; j++)
                    {
                        Buttons[j].Location = GetGridLocation(j);
                    }
                    AddDataButton.Location = GetGridLocation(Buttons.Count);
                    refresh = true;
                }

                if (refresh)
                { 
                    Form_Refresh(false, null); 
                }
            }
        }

        private void AddData(Color color, double value)
        {
            Button Item = new Button
            {
                Size = GridElementSize,
                BackColor = color,
                ForeColor = ((color.R + color.G + color.B) / 3.0) < 128 ? Color.White : Color.Black,
                Location = GetGridLocation(Buttons.Count),
                Text = value.ToString()
            };
            Item.PreviewKeyDown += Item_PreviewKeyDown;
            Item.Font = new Font(Item.Font.FontFamily, 25);
            Buttons.Add(Item);
            Controls.Add(Item);
            Item.Show();

            AddDataButton.Location = GetGridLocation(Buttons.Count);
        }

        private void AddDataButton_Click(object sender, EventArgs e)
        {
            ColorDialog dialog = new ColorDialog();

            if (dialog.ShowDialog() == DialogResult.OK && InputBox("InputBox", "Enter a Number", out double value) == DialogResult.OK)
            {
                List<Color> colors = ColorInt.Colors.ToList();
                colors.Add(dialog.Color);

                List<double> doubles = ColorInt.Doubles.ToList();
                doubles.Add(value);

                ColorInt = new ColorInterpolation(colors.ToArray(), doubles.ToArray());
            }
            Form_Refresh(false, "");
        }

        public static string GetExecutingDirectoryName() => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog() { InitialDirectory = GetExecutingDirectoryName(), Filter = FileFilter };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ColorInt.Save(Path.GetFileNameWithoutExtension(dialog.FileName) + ".txt");
            }
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog() { Filter = FileFilter };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Form_Refresh(false, dialog.FileName);
            }
        }

        private void ColorInterpolationForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ColorInt.Save(Path.Combine(GetExecutingDirectoryName(), FileName));
        }
    }
}