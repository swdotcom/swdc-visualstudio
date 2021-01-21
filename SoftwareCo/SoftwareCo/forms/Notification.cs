using System;
using System.Drawing;
using System.Windows.Forms;

namespace SoftwareCo
{
    public class Notification
    {
        public static DialogResult Show(string title, string promptText)
        {
            return Show(title, promptText, null);
        }

        public static DialogResult Show(string title, string promptText,
                                        InputBoxValidation validation)
        {
            Form form = new Form();
            Label label = new Label();
            Button buttonOk = new Button();

            form.Text = title;
            label.Text = promptText;

            buttonOk.Text = "OK";
            buttonOk.DialogResult = DialogResult.OK;

            label.SetBounds(9, 20, 372, 13);
            buttonOk.SetBounds(228, 72, 75, 23);

            label.AutoSize = true;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, buttonOk });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            DialogResult dialogResult = form.ShowDialog();
            return dialogResult;
        }
    }
}
