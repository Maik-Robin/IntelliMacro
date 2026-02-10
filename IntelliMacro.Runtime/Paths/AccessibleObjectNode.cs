using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ManagedWinapi.Accessibility;

namespace IntelliMacro.Runtime.Paths
{
    /// <summary>
    /// An <see cref="IPathNode{T}"/> of an accessible object.
    /// </summary>
    public class AccessibleObjectNode : IPathNode<AccessibleObjectNode>
    {
        readonly SystemAccessibleObject accobj;
        readonly bool nameAsPrimary;

        /// <summary>
        /// Creates a new AccessibleObjectNode.
        /// </summary>
        /// <param name="accobj">The accessible object.</param>
        /// <param name="nameAsPrimary">Whether the primary value of this node should be its name (and not its role).</param>
        public AccessibleObjectNode(SystemAccessibleObject accobj, bool nameAsPrimary)
        {
            this.accobj = accobj;
            this.nameAsPrimary = nameAsPrimary;
        }

        /// <summary>
        /// The accessible object.
        /// </summary>
        public SystemAccessibleObject AccessibleObject { get { return accobj; } }

        /// <summary>
        /// The parent of this object.
        /// </summary>
        public IPathRoot<AccessibleObjectNode> Parent
        {
            get
            {
                return new AccessibleObjectNode(accobj.Parent, nameAsPrimary);
            }
        }

        /// <summary>
        /// The name of this node. This is either the name or the role of
        /// this accessible object.
        /// </summary>
        public string NodeName
        {
            get
            {
                string name;
                try
                {
                    if (nameAsPrimary)
                        name = Name;
                    else
                        name = InvariantRoleName;
                    if (name == null) name = "";
                    return name;
                }
                catch (COMException)
                {
                    return "?";
                }
            }
        }

        /// <summary>
        /// The name of this accessible object.
        /// </summary>
        public string Name
        {
            get
            {
                return accobj.Name;
            }
        }

        /// <summary>
        /// The role of this accessible object, not localized.
        /// </summary>
        public string InvariantRoleName
        {
            get
            {
                int roleindex = accobj.RoleIndex;
                if (roleindex >= -1 && roleindex <= 64)
                {
                    return Enum.GetName(typeof(AccessibleRole), (AccessibleRole)roleindex);
                }
                else
                {
                    return "Role" + roleindex;
                }
            }
        }

        /// <summary>
        /// Parameter names for accessible objects.
        /// </summary>
        public IEnumerable<string> ParameterNames
        {
            get
            {
                return new string[] {
                    "ChildID", "DefaultAction", "Description",
                    "KeyboardShortcut", "X", "Y", "W", "H", "R", "B",
                    "Name", "Role", "RoleIndex", "RoleString", "State_", "State", 
                    "StateString", "Value", "Visible", "Window", 
                };
            }
        }

        /// <summary>
        /// Gets a parameter of this accessible object.
        /// </summary>
        public string GetParameter(string name)
        {
            try
            {
                switch (name.ToLowerInvariant())
                {
                    case "childid": return "" + accobj.ChildID;
                    case "defaultaction": return accobj.DefaultAction ?? "";
                    case "description": return accobj.Description ?? "";
                    case "keyboardshortcut": return accobj.KeyboardShortcut ?? "";
                    case "x": return "" + accobj.Location.X;
                    case "y": return "" + accobj.Location.Y;
                    case "w": return "" + accobj.Location.Width;
                    case "h": return "" + accobj.Location.Height;
                    case "r": return "" + accobj.Location.Right;
                    case "b": return "" + accobj.Location.Bottom;
                    case "name": return accobj.Name ?? "";
                    case "role": return InvariantRoleName ?? "";
                    case "roleindex": return "" + accobj.RoleIndex;
                    case "rolestring": return accobj.RoleString ?? "";
                    case "state_": return GetStateOptions(accobj.State);
                    case "state": return "" + accobj.State;
                    case "statestring": return accobj.StateString ?? "";
                    case "value": return accobj.Value ?? "";
                    case "visible": return accobj.Visible ? "1" : "0";
                    case "window": return "" + accobj.Window.HWnd;
                    default:
                        if (name.ToLowerInvariant().StartsWith("state_"))
                        {
                            try
                            {
                                int bit = int.Parse(name.Substring(6));
                                if (bit != 0 && (bit & (bit - 1)) == 0 && (accobj.State & bit) != 0)
                                {
                                    return SystemAccessibleObject.StateBitToString(bit);
                                }
                            }
                            catch (FormatException) { }
                        }
                        return "";
                }
            }
            catch (COMException)
            {
                return "?";
            }
        }

        private string GetStateOptions(int stateNumber)
        {
            StringBuilder result = new StringBuilder();
            while (stateNumber != 0)
            {
                int lowBit = stateNumber & -stateNumber;
                result.Append(lowBit + " ");
                stateNumber = stateNumber - lowBit;
            }
            return result.ToString();
        }

        /// <summary>
        /// Gets all children of this accessible object node.
        /// </summary>
        public IEnumerable<AccessibleObjectNode> Children
        {
            get
            {
                SystemAccessibleObject[] saos = accobj.Children;
                AccessibleObjectNode[] nodes = new AccessibleObjectNode[saos.Length];
                for (int i = 0; i < nodes.Length; i++)
                {
                    nodes[i] = new AccessibleObjectNode(saos[i], nameAsPrimary);
                }
                return nodes;
            }
        }

        #region Equals and HashCode
        ///
        public override bool Equals(object obj)
        {
            AccessibleObjectNode aon = obj as AccessibleObjectNode;
            return aon != null && aon.accobj.Equals(accobj);
        }

        ///
        public override int GetHashCode()
        {
            return accobj.GetHashCode();
        }
        #endregion
    }
}
