using System;

namespace Xpressive.Home.Contracts.Variables
{
    public sealed class BinaryVariable : IVariable
    {
        public string Name { get; set; }

        public string ContentType { get; set; }

        public byte[] Value { get; set; }

        public string Unit { get; set; }

        object IVariable.Value
        {
            get
            {
                return new BinaryVariableValue
                {
                    ContentType = ContentType,
                    Data = Value
                }.ToString();
            }
            set
            {
                var v = BinaryVariableValue.FromString(value as string);
                ContentType = v.ContentType;
                Value = v.Data;
            }
        }

        private class BinaryVariableValue
        {
            private static string ToString(BinaryVariableValue value)
            {
                if (value == null)
                {
                    return string.Empty;
                }

                var ct = value.ContentType ?? string.Empty;
                var data = Convert.ToBase64String(value.Data ?? new byte[0]);

                ct = ct.Replace("\n", string.Empty);

                return $"{ct}\n{data}";
            }

            public static BinaryVariableValue FromString(string s)
            {
                if (string.IsNullOrEmpty(s))
                {
                    return new BinaryVariableValue();
                }

                var parts = s.Split(new[] {'\n'}, 2, StringSplitOptions.None);
                var ct = parts[0];
                var data = parts.Length > 1 ? Convert.FromBase64String(parts[1]) : new byte[0];

                return new BinaryVariableValue
                {
                    ContentType = ct,
                    Data = data
                };
            }

            public string ContentType { get; set; } = string.Empty;

            public byte[] Data { get; set; } = new byte[0];

            public override string ToString()
            {
                return ToString(this);
            }
        }
    }
}
