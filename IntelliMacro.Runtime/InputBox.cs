using System.Windows.Forms;

namespace IntelliMacro.Runtime
{
    /// <summary>
    /// A dialog that asks for text input, like VB6's InputBox.
    /// </summary>
    public partial class InputBox : Form
    {
        /// <summary>
        /// Create a new input box with a given caption.
        /// </summary>
        public InputBox(string caption)
        {
            InitializeComponent();
            label.Text = caption;
        }

        /// <summary>
        /// Show the input box and wait for a result.
        /// </summary>
        /// <param name="parent">The parent form of this input box.</param>
        /// <param name="message">The message to show.</param>
        /// <param name="initialValue">The initial value.</param>
        /// <returns>The entered text, or <c>null</c> if the dialog has
        /// been cancelled.</returns>
        public static string Show(Form parent, string message, string initialValue)
        {
            InputBox ib = new InputBox(message);
            ib.text.Text = initialValue;
            DialogResult dr = ib.ShowDialog(parent);
            if (dr == DialogResult.Cancel) return null;
            else return ib.text.Text;
        }
    }
}