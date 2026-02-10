
namespace IntelliMacro.Runtime
{
    /// <summary>
    /// A description for a macro command parameter. Used by plugins to describe
    /// parameters.
    /// </summary>
    public class ParameterDescription
    {
        private readonly string name;
        private readonly bool optional;

        /// <summary>
        /// Create a new parameter description.
        /// </summary>
        /// <param name="optional">Whether the parameter is optional</param>
        /// <param name="name">The name of the parameter</param>
        public ParameterDescription(bool optional, string name)
        {
            this.optional = optional;
            this.name = name;
        }

        /// <summary>
        /// Whether the parameter is optional.
        /// </summary>
        public bool Optional
        {
            get { return optional; }
        }

        /// <summary>
        /// The name of the parameter.
        /// </summary>
        public string Name
        {
            get { return name; }
        }
    }
}
