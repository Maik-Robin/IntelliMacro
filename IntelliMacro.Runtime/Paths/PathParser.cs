using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IntelliMacro.Runtime.Paths
{
    /// <summary>
    /// A class that parses paths of <see cref="IPathNode{T}"/> nodes.
    /// </summary>
    public static class PathParser
    {
        /// <summary>
        /// Parse a path starting from a root and return its only result. If more than one result is found, a
        /// <see cref="MacroErrorException"/> is thrown.
        /// </summary>
        /// <param name="root">The root to start parsing at.</param>
        /// <param name="path">The path</param>
        /// <param name="kind">A name for the kind of elements, used in an error message if more than one result is found.</param>
        public static T ParsePath<T>(IPathRoot<T> root, string path, string kind) where T : class, IPathNode<T>
        {
            IList<T> nodes = ParsePath(path, root);
            if (nodes.Count == 0) return null;
            if (nodes.Count != 1) throw new MacroErrorException("More than one " + kind + " found");
            return nodes[0];
        }

        /// <summary>
        /// Parse a path starting from a list of roots, and return a list of all results.
        /// </summary>
        /// <param name="path">The path</param>
        /// <param name="roots">The list of roots, used as a varargs parameter array.</param>
        /// <returns></returns>
        public static IList<T> ParsePath<T>(string path, params IPathRoot<T>[] roots) where T : IPathNode<T>
        {
            return ParsePath<T, IPathRoot<T>>(roots, path);
        }

        private static IList<T> ParsePath<T, U>(IList<U> roots, string path)
            where U : IPathRoot<T>
            where T : IPathNode<T>
        {
            if (path.Contains("\n") || path.Contains("\r"))
            {
                string[] paths = path.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                List<T> subResults = new List<T>();
                foreach (string p in paths)
                {
                    subResults.AddRange(ParsePath<T, U>(roots, p));
                }
                return subResults;
            }
            if (path.Equals("**") || path.Equals(".."))
            {
                path += "|.";
            }
            if (path.StartsWith("**|"))
            {
                // any number of nodes
                List<IPathRoot<T>> descendants = new List<IPathRoot<T>>();
                AddDescendants(descendants, roots);
                return ParsePath<T, IPathRoot<T>>(descendants, path.Substring(3));
            }
            else if (path.StartsWith("..|"))
            {
                // parent
                List<IPathRoot<T>> parents = new List<IPathRoot<T>>();
                foreach (IPathRoot<T> root in roots)
                {
                    parents.Add(root.Parent);
                }
                return ParsePath<T, IPathRoot<T>>(parents, path.Substring(3));
            }
            else if (path.StartsWith(".#"))
            {
                // numeric select (maybe with range)
                if (!path.Contains("|")) path += "|.";
                Match m = Regex.Match(path, @"^\.#(-?[0-9]+)(\.\.-?[0-9]+)?\|");
                if (m.Success)
                {
                    int minimum = int.Parse(m.Groups[1].Value);
                    int maximum = minimum;
                    if (m.Groups[2].Length > 0)
                    {
                        maximum = int.Parse(m.Groups[2].Value.Substring(2));
                    }
                    if (minimum < 0)
                    {
                        minimum += roots.Count;
                    }
                    else if (minimum > 0)
                    {
                        minimum--;
                    }
                    if (maximum < 0)
                    {
                        maximum += roots.Count;
                    }
                    else if (maximum > 0)
                    {
                        maximum--;
                    }
                    else
                    {
                        maximum = roots.Count - 1;
                    }
                    if (maximum < minimum)
                    {
                        int tmp = maximum; maximum = minimum; minimum = tmp;
                    }
                    if (maximum >= roots.Count) maximum = roots.Count - 1;
                    if (minimum >= roots.Count) minimum = roots.Count - 1;
                    if (maximum < 0) maximum = 0;
                    if (minimum < 0) minimum = 0;
                    IList<U> newRoots = new List<U>();
                    for (int i = minimum; i <= maximum; i++)
                    {
                        newRoots.Add(roots[i]);
                    }
                    return ParsePath<T, U>(newRoots, path.Substring(m.Groups[0].Length));
                }
            }
            else if (path.StartsWith(".!"))
            {
                // sort
                if (!path.Contains("|")) path += "|.";
                int pos = path.IndexOf("|");
                string parameterName = path.Substring(2, pos - 2);
                int reverse = 1;
                if (parameterName.StartsWith("!"))
                {
                    parameterName = parameterName.Substring(1);
                    reverse = -1;
                }
                List<U> newRoots = new List<U>(roots);
                Dictionary<U, string> parameters = new Dictionary<U, string>();
                foreach (U root in newRoots)
                {
                    IPathNode<T> node = root as IPathNode<T>;
                    string parameter = "";
                    if (node != null)
                    {
                        if (parameterName.Length == 0)
                            parameter = node.NodeName;
                        else
                            parameter = node.GetParameter(parameterName);
                    }
                    parameters[root] = parameter;
                }
                newRoots.Sort(delegate(U root1, U root2)
                {
                    return reverse * parameters[root1].CompareTo(parameters[root2]);
                });
                return ParsePath<T, U>(newRoots, path.Substring(pos + 1));
            }
            else if (path.Equals(".") || path.StartsWith(".|") || path.StartsWith(".&"))
            {
                // (filters on) this element
                IList<ParentWrapperRoot<T>> newRoots = new List<ParentWrapperRoot<T>>();
                foreach (U root in roots)
                {
                    newRoots.Add(new ParentWrapperRoot<T>(root));
                }
                return ParsePath<T, ParentWrapperRoot<T>>(newRoots, "*" + path.Substring(1));
            }
            PathPattern currentPattern = null;
            string currentName = "";
            List<KeyValuePair<string, PathPattern>> valuesToTest = new List<KeyValuePair<string, PathPattern>>();
            string nextPart = null;
            for (int i = 0; i < path.Length; i++)
            {
                switch (path[i])
                {
                    case '=':
                        if (currentName == null)
                        {
                            currentName = currentPattern == null ? "" : currentPattern.Pattern;
                            currentPattern = null;
                            break;
                        }
                        else
                        {
                            goto default;
                        }
                    case '&':
                        if (currentName == null) throw new MacroErrorException("Missing equals sign");
                        if (currentPattern == null) currentPattern = new PathPattern("");
                        valuesToTest.Add(new KeyValuePair<string, PathPattern>(currentName, currentPattern));
                        currentName = null;
                        currentPattern = null;
                        break;
                    case '|':
                        nextPart = path.Substring(i + 1);
                        i = path.Length;
                        break;
                    default:
                        currentPattern = new PathPattern(path, i, currentName == null);
                        i = currentPattern.RestOffset - 1;
                        break;
                }
            }
            if (currentName == null) throw new MacroErrorException("Missing equals sign");
            if (currentPattern == null) currentPattern = new PathPattern("");
            valuesToTest.Add(new KeyValuePair<string, PathPattern>(currentName, currentPattern));
            List<T> result = new List<T>();
            foreach (IPathRoot<T> root in roots)
            {
                foreach (T node in root.Children)
                {
                    bool ok = true;
                    foreach (KeyValuePair<string, PathPattern> kvp in valuesToTest)
                    {
                        string value = kvp.Key == "" ? node.NodeName : node.GetParameter(kvp.Key);
                        try
                        {
                            if (!kvp.Value.IsMatch(value))
                            {
                                ok = false;
                                break;
                            }
                        }
                        catch (ArgumentException ex)
                        {
                            string msg = ex.Message;
                            if (msg.Contains(" - ")) msg = msg.Substring(msg.IndexOf(" - ") + 3);
                            throw new MacroErrorException("Regex error: " + msg);
                        }
                    }
                    if (ok)
                    {
                        result.Add(node);
                    }
                }
            }
            if (nextPart != null)
                return ParsePath<T, T>(result, nextPart);
            else
                return result;
        }

        private static void AddDescendants<T, U>(List<IPathRoot<U>> target, IEnumerable<T> nodes)
            where T : IPathRoot<U>
            where U : IPathNode<U>
        {
            foreach (T node in nodes)
            {
                if (!target.Contains(node))
                    target.Add(node);
                AddDescendants(target, node.Children);
            }
        }

        /// <summary>
        /// Find the best path that matches only the given sample relative to a given root.
        /// A <see cref="MacroErrorException"/> is thrown if the given parameters cannot disambiguate
        /// the object.
        /// </summary>
        /// <param name="root">The root.</param>
        /// <param name="nodeSample">The sample.</param>
        /// <param name="paramList">A list of node parameters that should be used.</param>
        /// <returns></returns>
        public static string FindBestExpression<T>(IPathRoot<T> root, T nodeSample, params string[] paramList) where T : class, IPathNode<T>
        {
            T node = null;
            foreach (T n in root.Children)
            {
                if (n.Equals(nodeSample))
                {
                    node = n;
                    break;
                }
            }
            if (node == null) throw new ArgumentException("Node is not a child of the given path root");
            String result = PathPattern.Quote(node.NodeName);
            if (ParsePath(result, root).Count == 1) return result;
            foreach (string param in paramList)
            {
                string value = node.GetParameter(param);
                if (value != "")
                {
                    result = result + "&" + param + "=" + PathPattern.Quote(value);
                    if (ParsePath(result, root).Count == 1) return result;
                }
            }
            throw new MacroErrorException("Ambiguous nodes detected!");
        }

        /// <summary>
        /// Return all the parameters of a given PathNode, including those "hidden" behind
        /// a parameter that ends with an underscore.
        /// </summary>
        /// <seealso cref="IPathNode{T}.ParameterNames"/>
        public static IList<string> GetAllParameterNames<T>(T node) where T : class, IPathNode<T>
        {
            List<string> result = new List<string>();
            IEnumerable<string> names = node.ParameterNames;
            result.AddRange(names);
            foreach (string name in names)
            {
                if (name.EndsWith("_"))
                {
                    string value = node.GetParameter(name);
                    if (value == "") continue;
                    foreach (string subname in value.Split(' '))
                        result.Add(name + subname);
                }
            }
            return result;
        }

        /// <summary>
        /// Quote a string to be used as a value in a path.
        /// </summary>
        public static string QuoteValue(string value)
        {
            return PathPattern.Quote(value);
        }
    }

    class ParentWrapperRoot<T> : IPathRoot<T> where T : IPathNode<T>
    {
        IPathRoot<T> wrapped;
        internal ParentWrapperRoot(IPathRoot<T> wrapped)
        {
            this.wrapped = wrapped;
        }

        public IEnumerable<T> Children
        {
            get
            {
                List<T> result = new List<T>();
                if (wrapped is T)
                    result.Add((T)wrapped);
                return result;
            }
        }

        public IPathRoot<T> Parent { get { throw new NotImplementedException(); } }
    }
}
