using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;
using ManagedWinapi.Windows;
using ManagedWinapi.Windows.Contents;

namespace IntelliMacro.Runtime.Paths
{
    /// <summary>
    /// An <see cref="IPathNode{T}"/> that represents a <see cref="SystemWindow"/> on the current desktop.
    /// </summary>
    public class WindowNode : IPathNode<WindowNode>
    {
        readonly SystemWindow sw, parentDialog;
        WindowRelation relation;
        bool includeInvisible = false;

        /// <summary>
        /// Creates a new window node for a given window.
        /// </summary>
        /// <param name="sw">The window</param>
        public WindowNode(SystemWindow sw) : this(sw, false) { }

        /// <summary>
        /// Creates a new window node for a given window.
        /// </summary>
        /// <param name="sw">The window</param>
        /// <param name="includeInvisible">Whether the <see cref="Children"/> property includes hidden windows.</param>
        public WindowNode(SystemWindow sw, bool includeInvisible)
        {
            this.sw = sw;
            this.includeInvisible = includeInvisible;
            this.relation = sw.ParentSymmetric == null ? WindowRelation.TOPLEVEL : WindowRelation.NONE;
        }

        internal WindowNode(SystemWindow sw, WindowRelation relation, bool includeInvisible)
        {
            this.sw = sw;
            this.relation = relation;
            this.includeInvisible = includeInvisible;
            if (relation != WindowRelation.NONE)
                parentDialog = sw.Parent;
        }

        /// <summary>
        /// The window represented by this node.
        /// </summary>
        public SystemWindow Window { get { return sw; } }

        internal SystemWindow ParentDialog { get { return parentDialog; } }

        /// <summary>
        /// The class name of this window.
        /// </summary>
        public string NodeName { get { try { return sw.ClassName; } catch (Win32Exception) { return ""; } } }

        /// <summary>
        /// The parent of this node.
        /// </summary>
        public IPathRoot<WindowNode> Parent
        {
            get
            {
                if (sw.ParentSymmetric == null) return new WindowRoot(null, includeInvisible);
                return new WindowNode(sw.ParentSymmetric, includeInvisible);
            }
        }

        /// <summary>
        /// A list of supported parameters of this node
        /// </summary>
        public IEnumerable<string> ParameterNames
        {
            get
            {
                return PARAMETER_NAMES;
            }
        }

        private static readonly string[] PARAMETER_NAMES = {
            "title", "x", "y", "w", "h", "screenx", "screeny", "r", "b", "index",
            "handle", "enabled", "invisible",

            // toplevel windows
            "relation", "process", "processid", "topmost", "movable", "resizable", "state",

            // child windows
            "checked", "shortcontent", "longcontent", "content_", "id", "mdi", "parenthandle"
        };

        /// <summary>
        /// Get a parameter of the node.
        /// </summary>
        public string GetParameter(string name)
        {
            switch (name.ToLowerInvariant())
            {
                case "title": return sw.Title;
                case "x": return "" + sw.Position.Left;
                case "y": return "" + sw.Position.Top;
                case "screenx": return "" + sw.Rectangle.Left;
                case "screeny": return "" + sw.Rectangle.Top;
                case "w": return "" + sw.Position.Width;
                case "h": return "" + sw.Position.Height;
                case "r": return "" + sw.Position.Right;
                case "b": return "" + sw.Position.Bottom;
                case "index":
                    int index = 0;
                    SystemWindow tmp = sw;
                    while (tmp != null)
                    {
                        index++;
                        tmp = tmp.WindowAbove;
                    }
                    return "" + index;
                case "handle":
                    return "" + sw.HWnd;
                case "invisible":
                    return sw.VisibilityFlag ? "" : "1";
                case "enabled":
                    return sw.Enabled ? "1" : "0";

                // toplevel windows
                case "relation":
                    switch (relation)
                    {
                        case WindowRelation.CHILD: return "child";
                        case WindowRelation.CURRENT: return "current";
                        case WindowRelation.PARENT: return "parent";
                        case WindowRelation.SIBLING: return "sibling";
                        default: return "";
                    }
                case "process":
                    if (relation == WindowRelation.NONE) return "";
                    return sw.Process.MainModule.FileName.ToUpperInvariant();
                case "processid":
                    if (relation == WindowRelation.NONE) return "";
                    return "" + sw.Process.Id;
                case "topmost":
                    if (relation == WindowRelation.NONE) return "";
                    return sw.TopMost ? "1" : "";
                case "movable":
                    if (relation == WindowRelation.NONE) return "";
                    return sw.Movable ? "1" : "0";
                case "resizable":
                    if (relation == WindowRelation.NONE) return "";
                    return sw.Resizable ? "1" : "0";
                case "state":
                    if (relation == WindowRelation.NONE) return "";
                    FormWindowState ws = sw.WindowState;
                    return ws == FormWindowState.Minimized ? "minimized" :
                        (ws == FormWindowState.Maximized ? "maximized" : "normal");
                // child windows
                case "checked":
                    if (relation != WindowRelation.NONE) return "";
                    CheckState checkstate = sw.CheckState;
                    return checkstate == CheckState.Checked ? "yes" : checkstate == CheckState.Indeterminate ? "maybe" : "no";
                case "shortcontent":
                    if (relation != WindowRelation.NONE) return "";
                    WindowContent sc = sw.Content;
                    return sc == null ? "" : sc.ShortDescription;
                case "longcontent":
                    if (relation != WindowRelation.NONE) return "";
                    WindowContent lc = sw.Content;
                    return lc == null ? "" : lc.LongDescription;
                case "content_":
                    if (relation != WindowRelation.NONE) return "";
                    StringBuilder contentflags = new StringBuilder();
                    WindowContent cc = sw.Content;
                    if (cc == null) return "";
                    foreach (string key in cc.PropertyList.Keys)
                    {
                        if (contentflags.Length > 0) contentflags.Append(" ");
                        contentflags.Append(key);
                    }
                    return contentflags.ToString();
                case "id":
                    if (relation != WindowRelation.NONE) return "";
                    int id = sw.DialogID;
                    if (id == 0) return "";
                    return "" + id;
                case "mdi":
                    if (relation != WindowRelation.NONE) return "";
                    return ((sw.ExtendedStyle & WindowExStyleFlags.MDICHILD) != 0) ? "1" : "";
                case "parenthandle":
                    if (relation != WindowRelation.NONE) return "";
                    SystemWindow parent = sw.ParentSymmetric;
                    return "" + (parent == null ? IntPtr.Zero : parent.HWnd);
                default:
                    if (name.ToLowerInvariant().StartsWith("content_"))
                    {
                        WindowContent cc2 = sw.Content;
                        if (cc2 != null && cc2.PropertyList.ContainsKey(name.Substring(8)) && cc2.PropertyList[name.Substring(8)] != null)
                            return cc2.PropertyList[name.Substring(8)];
                    }
                    return "";
            }
        }

        /// <summary>
        /// The children of this node.
        /// </summary>
        public IEnumerable<WindowNode> Children
        {
            get
            {
                List<WindowNode> result = new List<WindowNode>();
                foreach (SystemWindow cw in sw.AllChildWindows)
                {
                    if (!includeInvisible && !cw.VisibilityFlag) continue;
                    result.Add(new WindowNode(cw, WindowRelation.NONE, includeInvisible));
                }
                return result;
            }
        }

        /// <summary>
        /// The relative path to this node from a given ancestor node.
        /// </summary>
        public string GetRelativePath(WindowNode ancestor)
        {
            if (!ancestor.IsAncestorOf(sw)) throw new ArgumentException();
            SystemWindow swp = sw.ParentSymmetric;
            WindowNode parent = new WindowNode(swp, true);
            String pathToParent = PathParser.FindBestExpression(parent, this, "title", "id", "w", "h", "x", "y", "index");
            if (parent.Equals(ancestor))
                return pathToParent;
            return parent.GetRelativePath(ancestor) + '|' + pathToParent;
        }

        private bool IsAncestorOf(SystemWindow descendant)
        {
            SystemWindow p = descendant.ParentSymmetric;
            if (p == null) return false;
            if (p.HWnd == sw.HWnd) return true;
            return IsAncestorOf(p);
        }

        /// <summary>
        /// The absolute path to this node from a given root.
        /// </summary>
        public string GetAbsolutePath(WindowRoot root)
        {
            if (sw.ParentSymmetric != null)
            {
                SystemWindow tmp = sw.ParentSymmetric;
                while (tmp.ParentSymmetric != null) tmp = tmp.ParentSymmetric;
                WindowNode tmpNode = new WindowNode(tmp);
                string absolute = tmpNode.GetAbsolutePath(root);
                if (absolute == null) return null;
                return absolute + "|" + GetRelativePath(tmpNode);
            }
            WindowNode thiz = null;
            foreach (WindowNode n in root.Children)
            {
                if (n.Equals(this))
                {
                    thiz = n;
                    break;
                }
            }
            if (thiz == null)
            {
                try { sw.ClassName.ToString(); }
                catch (Win32Exception) { return null; }
                thiz = new WindowNode(sw);
            }
            string result = PathPattern.Quote(thiz.NodeName);
            foreach (string param in new string[] { "relation", "process", "topmost", "title" })
            {
                string value = thiz.GetParameter(param);
                if (value != "")
                {
                    value = PathPattern.Quote(value);
                    if (value.Contains("\\"))
                        value = "*" + value.Substring(value.LastIndexOf('\\'));
                    result += "&" + param + "=" + value;
                }
            }
            return result;
        }

        #region Equals and HashCode
        ///
        public override bool Equals(object obj)
        {
            WindowNode wn = obj as WindowNode;
            return wn != null && wn.sw.HWnd == sw.HWnd;
        }

        ///
        public override int GetHashCode()
        {
            return sw.GetHashCode();
        }
        #endregion
    }

    enum WindowRelation
    {
        NONE = 0,
        TOPLEVEL, CURRENT, PARENT, CHILD, SIBLING
    }

    /// <summary>
    /// An <see cref="IPathRoot{T}"/> instance for <see cref="WindowNode"/>s, whose children
    /// are all the toplevel windows.
    /// </summary>
    public class WindowRoot : IPathRoot<WindowNode>
    {
        IntPtr currentHandle, parentHandle;
        bool includeInvisible;

        /// <summary>
        /// Create a new root.
        /// </summary>
        /// <param name="currentWindow">The current window, used for relative window parameters. May be <code>null</code>.</param>
        public WindowRoot(WindowNode currentWindow) : this(currentWindow, false) { }

        /// <summary>
        /// Create a new root.
        /// </summary>
        /// <param name="currentWindow">The current window, used for relative window parameters. May be <code>null</code>.</param>
        /// <param name="includeInvisible">Whether the <see cref="Children"/> property includes hidden windows.</param>
        public WindowRoot(WindowNode currentWindow, bool includeInvisible)
        {
            this.includeInvisible = includeInvisible;
            if (currentWindow == null)
            {
                currentHandle = IntPtr.Zero;
                parentHandle = IntPtr.Zero;
            }
            else
            {
                currentHandle = currentWindow.Window.HWnd;
                parentHandle = currentWindow.ParentDialog == null ? IntPtr.Zero : currentWindow.ParentDialog.HWnd;
            }
        }

        /// <summary>
        /// Always <code>null</code>.
        /// </summary>
        public IPathRoot<WindowNode> Parent { get { return null; } }

        /// <summary>
        /// The childen of this window.
        /// </summary>
        public IEnumerable<WindowNode> Children
        {
            get
            {
                List<WindowNode> result = new List<WindowNode>();
                foreach (SystemWindow sw in SystemWindow.AllToplevelWindows)
                {
                    if (!includeInvisible && !sw.VisibilityFlag) continue;
                    WindowRelation relation;
                    if (sw.HWnd == currentHandle)
                    {
                        relation = WindowRelation.CURRENT;
                    }
                    else if (sw.HWnd == parentHandle)
                    {
                        relation = WindowRelation.PARENT;
                    }
                    else
                    {
                        IntPtr parent = sw.Parent.HWnd;
                        if (parent == currentHandle && currentHandle != IntPtr.Zero)
                        {
                            relation = WindowRelation.CHILD;
                        }
                        else if (parent == parentHandle && parent != IntPtr.Zero)
                        {
                            relation = WindowRelation.SIBLING;
                        }
                        else
                        {
                            relation = WindowRelation.TOPLEVEL;
                        }
                    }
                    result.Add(new WindowNode(sw, relation, includeInvisible));
                }
                return result;
            }
        }
    }
}
