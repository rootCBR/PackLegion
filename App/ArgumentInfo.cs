using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackLegion
{
    public struct ArgumentInfo
    {
        public readonly string Name;
        public readonly string Value;

        public bool HasName
        {
            get { return !string.IsNullOrEmpty(Name); }
        }

        public bool HasValue
        {
            get { return !string.IsNullOrEmpty(Value); }
        }

        public bool IsEmpty
        {
            get { return !HasName && !HasValue; }
        }

        public bool IsSwitch
        {
            get { return HasName && !HasValue; }
        }

        public bool IsVariable
        {
            get { return HasName && HasValue; }
        }

        public bool IsValue
        {
            get { return HasValue && !HasName; }
        }

        public override string ToString()
        {
            if (this.HasName && this.HasValue)
            {
                return $"{this.Name}={this.Value}";
            }

            if (this.HasName && !this.HasValue)
            {
                return this.Name;
            }

            if (!this.HasName && this.HasValue)
            {
                return this.Value;
            }

            return base.ToString();
        }

        public ArgumentInfo(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public ArgumentInfo(string argument)
        {
            if (argument == null)
            {
                throw new ArgumentNullException("Can not create argument info from null string");
            }

            string arg = argument.TrimStart('-');

            if (arg != argument)
            {
                if (arg.Length > 0)
                {
                    var separator = arg.IndexOf("=");

                    if (separator != -1)
                    {
                        this.Name = arg.Substring(0, separator).ToLower();
                        this.Value = arg.Substring(separator + 1);
                    }
                    else
                    {
                        this.Name = arg.ToLower();
                        this.Value = string.Empty;
                    }
                }
                else
                {
                    this.Name = string.Empty;
                    this.Value = string.Empty;
                }
            }
            else
            {
                this.Name = string.Empty;
                this.Value = arg;
            }
        }
    }
}
