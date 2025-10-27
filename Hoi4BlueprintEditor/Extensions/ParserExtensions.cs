using ParadoxPower.CSharpExtensions;
using ParadoxPower.Parser;

namespace Hoi4BlueprintEditor.Extensions;

public static class ParserExtensions
{
    public static bool TryGetIntCast(this Types.Value value, out int result)
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
}
