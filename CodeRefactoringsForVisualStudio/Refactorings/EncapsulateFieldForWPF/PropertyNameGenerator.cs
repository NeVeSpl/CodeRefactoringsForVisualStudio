using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;


namespace CodeRefactoringsForVisualStudio.Refactorings.EncapsulateFieldForWPF
{
    public static class PropertyNameGenerator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FromFieldName(string fieldName)
        {
            string propertyName = String.Empty;

            if (!String.IsNullOrWhiteSpace(fieldName))
            {
                if (IsNamePrefixed(fieldName))
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNamePrefixed(string name)
        {
            return (name.Length > 1) 
                   && (char.IsLower(name[0]) || (name[0] =='_'))
                   && (char.IsUpper(name[1]));
        }
    }
}
