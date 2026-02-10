using System.Collections.Generic;

namespace IntelliMacro.Runtime.Paths
{
    /// <summary>
    /// A generic node on a path that can be navigated using a <see cref="PathParser"/>.
    /// </summary>
    public interface IPathNode<T> : IPathRoot<T> where T : IPathNode<T>
    {

        /// <summary>
        /// Get the name of a node.
        /// </summary>
        string NodeName { get; }

        /// <summary>
        /// Get the parameter names of a node. A parameter name ending with an underscore will have
        /// a value which is a space separated list of suffixes. Each of this suffixes added to the parameter name
        /// will be a valid parameter as well.
        /// </summary>
        IEnumerable<string> ParameterNames { get; }

        /// <summary>
        /// Get a parameter of the node.
        /// </summary>
        string GetParameter(string name);
    }

    /// <summary>
    /// A root of a path that can be navigated using a <see cref="PathParser"/>.
    /// Since this interface is a superinterface of <see cref="IPathNode{T}"/>, every
    /// path node can be used as a path root as well.
    /// </summary>
    public interface IPathRoot<T> where T : IPathNode<T>
    {
        /// <summary>
        /// Return the node's children.
        /// </summary>
        IEnumerable<T> Children { get; }

        /// <summary>
        /// Return the node's parent, or <code>null</code> if the node does
        /// not have any.
        /// </summary>
        IPathRoot<T> Parent { get; }
    }
}
