using ParadoxPower.CSharpExtensions;
using ParadoxPower.Parser;

namespace Hoi4BlueprintBuilder.Core.Extensions;

public static class ParserExtensions
{
    extension(Types.Value value)
    {
        public bool TryGetIntCast(out int result)
        {
            if (value.TryGetInt(out result))
            {
                return true;
            }

            if (value.TryGetDecimal(out decimal decimalValue))
            {
                result = (int)decimalValue;
                return true;
            }

            result = 0;
            return false;
        }

        public bool TryGetDouble(out double result)
        {
            if (value.TryGetDecimal(out decimal decimalValue))
            {
                result = (double)decimalValue;
                return true;
            }

            if (value.TryGetInt(out int intValue))
            {
                result = intValue;
                return true;
            }

            result = 0.0;
            return false;
        }
    }
}
