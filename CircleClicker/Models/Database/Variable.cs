namespace CircleClicker.Models.Database
{
    /// <summary>
    /// A game variable that can be stored in the database.<br />
    /// Should not be accessed directly; use <see cref="VariableReference"/> for that instead.
    /// </summary>
    public class Variable
    {
        public int Id { get; set; }

        /// <summary>
        /// The name of the variable.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// The value of the variable.
        /// </summary>
        public double Value { get; set; }
    }

    /// <summary>
    /// Holds a reference to a <see cref="Variable"/>.
    /// </summary>
    public class VariableReference
    {
        /// <summary>
        /// A list of all <see cref="VariableReference"/> instances.
        /// </summary>
        public static readonly List<VariableReference> Instances = [];

        /// <param name="name">The name of the variable.</param>
        /// <param name="defaultValue">The value to return when the variable does not exist.</param>
        public VariableReference(string name, double defaultValue)
        {
            Name = name;
            DefaultValue = defaultValue;
            Instances.Add(this);
        }

        /// <summary>
        /// The name of the variable.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The default value of the variable.
        /// </summary>
        public double DefaultValue { get; }

        /// <summary>
        /// The value of the variable.
        /// </summary>
        public double Value
        {
            get =>
                Main.Instance.Variables.FirstOrDefault(v => v.Name == Name)?.Value ?? DefaultValue;
            set
            {
                Variable? variable = Main.Instance.Variables.FirstOrDefault(v => v.Name == Name);
                if (variable != null)
                {
                    variable.Value = value;
                }
                else
                {
                    variable = new Variable { Name = Name, Value = value };
                    Main.Instance.Variables.Add(variable);
                }
            }
        }

        public static implicit operator double(VariableReference instance)
        {
            return instance.Value;
        }
    }
}
