using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;


namespace EncapsulateFieldForWPF
{
    public static class PropertyNameGenerator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FromFieldName(string fieldName)
        {
            string propertyName = String.Empty;

            if (!String.IsNullOrWhiteSpace(fieldName))
            {
                if (fieldName.HasPrefix())
                {
                    propertyName = fieldName.Remove(0, 1);
                }
                else
                {
                    propertyName = char.ToUpper(fieldName[0]) + fieldName.Substring(1);
                }
            }

            return propertyName;
        }
    }
}
