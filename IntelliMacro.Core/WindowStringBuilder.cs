using System;
using System.Text;
using System.Windows.Forms;
using IntelliMacro.Runtime;
using IntelliMacro.Runtime.Paths;
using ManagedWinapi.Accessibility;
using ManagedWinapi.Windows;

namespace IntelliMacro.Core
{
    public partial class WindowStringBuilder : Form
    {
        bool updating;
        int lastSelectedIndex = 0;
        private string resultString = null;

        public WindowStringBuilder()
        {
            InitializeComponent();
            SelectWindow(SystemWindow.ForegroundWindow);
        }

        public string ResultString { get { return resultString; } }

        private void SelectWindow(SystemWindow window)
        {
            ok.Enabled = true;
            updating = true;
            itemList.Items.Clear();
            FillItemList(new WindowNode(window), true);
            itemList.SelectedIndex = itemList.Items.Count - 1;
            updating = false;
            itemList_SelectedIndexChanged(itemList, EventArgs.Empty);
        }

        private void SelectAccObj(SystemAccessibleObject accObj)
        {
            SystemWindow w = accObj.Window;
            SelectWindow(w);
            itemList.Items.Add(new SeparatorElement());
            SystemAccessibleObject wo = SystemAccessibleObject.FromWindow(w, AccessibleObjectID.OBJID_WINDOW);
            FillAccObjList(new AccessibleObjectNode(accObj, false), new AccessibleObjectNode(wo, false));
        }

        private void FillAccObjList(AccessibleObjectNode aoNode, AccessibleObjectNode lastNode)
        {
            if (aoNode.Equals(lastNode)) return;
            AccessibleObjectNode parent = (AccessibleObjectNode)aoNode.Parent;
            FillAccObjList(parent, lastNode);
            itemList.Items.Add(new WindowStringElement<AccessibleObjectNode>(aoNode, parent, PathParser.FindBestExpression(parent, aoNode, "name", "value", "state", "keyboardshortcut", "w", "h", "x", "y", "childid", "window")));
        }

        private void FillItemList(WindowNode windowNode, bool first)
        {
            SystemWindow parent = windowNode.Window.ParentSymmetric;
            if (parent != null)
            {
                WindowNode parentNode = new WindowNode(parent);
                FillItemList(parentNode, false);
                itemList.Items.Add(new WindowStringElement<WindowNode>(windowNode, parentNode, windowNode.GetRelativePath(parentNode)));
            }
            else if (!relative.Checked || first)
            {
                WindowRoot root = new WindowRoot(null);
                itemList.Items.Add(new WindowStringElement<WindowNode>(windowNode, root, windowNode.GetAbsolutePath(root)));
            }
        }

        private void itemList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (updating) return;
            if (!ok.Enabled && itemList.SelectedIndex == lastSelectedIndex)
            {
                itemList.SelectedIndex = lastSelectedIndex;
                return;
            }
            if (itemList.SelectedItem is SeparatorElement)
            {
                itemList.SelectedIndex--;
            }
            updating = true;
            lastSelectedIndex = itemList.SelectedIndex;
            itemValues.Items.Clear();
            if (itemList.SelectedItem is WindowStringElement<AccessibleObjectNode>)
                Update((WindowStringElement<AccessibleObjectNode>)itemList.SelectedItem);
            else
                Update((WindowStringElement<WindowNode>)itemList.SelectedItem);
            updating = false;
            editBox_TextChanged(editBox, EventArgs.Empty);
        }

        private void Update<T>(WindowStringElement<T> item) where T : class, IPathNode<T>
        {
            editBox.Text = item.Value;
            T node = item.Node;
            foreach (string name in PathParser.GetAllParameterNames(node))
            {
                string value = node.GetParameter(name);
                if (value.Length > 0)
                {
                    itemValues.Items.Add(new ListViewItem(new string[] { name, value }));
                }
            }
        }

        private void editBox_TextChanged(object sender, EventArgs e)
        {
            if (updating) return;
            IWindowStringElement current = ((IWindowStringElement)itemList.SelectedItem);
            current.Value = editBox.Text;
            updating = true;
            itemList.Items[itemList.SelectedIndex] = current; // force redraw
            updating = false;
            string error = current.Check();
            if (error != null)
            {
                ok.Enabled = false;
                errorLabel.Text = error;
            }
            else
            {
                ok.Enabled = true;
                errorLabel.Text = "";
                StringBuilder result = new StringBuilder();
                foreach (IWindowStringElement item in itemList.Items)
                {
                    if (result.Length > 0) result.Append("|");
                    result.Append(item.Value);
                }
                resultBox.Text = result.ToString();
            }
        }

        private void ok_Click(object sender, EventArgs e)
        {
            resultString = resultBox.Text;
        }

        private void itemList_Format(object sender, ListControlConvertEventArgs e)
        {
            e.Value = ((IWindowStringElement)e.ListItem).Value;
        }

        private void crosshair_CrosshairDragged(object sender, EventArgs e)
        {
            if (accobj.Checked)
            {
                SelectAccObj(SystemAccessibleObject.FromPoint(MousePosition.X, MousePosition.Y));
            }
            else
            {
                SelectWindow(SystemWindow.FromPointEx(MousePosition.X, MousePosition.Y, false, false));
            }
        }

        private void itemValues_DoubleClick(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in itemValues.SelectedItems)
            {
                editBox.Text += "&" + lvi.Text + "=" + PathParser.QuoteValue(lvi.SubItems[1].Text);
            }
        }
    }

    interface IWindowStringElement
    {
        string Value { get; set; }
        string Check();
    }

    class SeparatorElement : IWindowStringElement
    {
        public string Value
        {
            get { return "-AccObj-"; }
            set { }
        }

        public string Check() { return null; }
    }

    class WindowStringElement<T> : IWindowStringElement where T : IPathNode<T>
    {
        string value;
        readonly T node;
        readonly IPathRoot<T> parent;

        public WindowStringElement(T node, IPathRoot<T> parent, string value)
        {
            this.node = node;
            this.parent = parent;
            this.value = value;
        }

        public string Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public T Node { get { return node; } }

        public string Check()
        {
            try
            {
                foreach (T n in PathParser.ParsePath(value, parent))
                {
                    if (n.Equals(node)) return null;
                }
                return "Control not found";
            }
            catch (MacroErrorException ex)
            {
                return ex.Message;
            }
        }
    }
}