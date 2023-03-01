using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace GenerateMapping.Model
{
    internal class TypeData
    {
        private readonly ITypeSymbol typeSymbol;


        public string Name => typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        public bool IsArray => typeSymbol.Kind == SymbolKind.ArrayType;
        public bool IsCollection => typeSymbol.IsCollection() || typeSymbol.IsEnumerable();
        public bool IsGeneric => (typeSymbol is INamedTypeSymbol x) && x.IsGenericType;
        public bool IsImmutable
        {
            get
            {
                if (typeSymbol.IsReferenceType && typeSymbol.SpecialType != SpecialType.System_String)
                {
                    return false;
                }
                return true;
            }
        }
        public bool IsInterface => typeSymbol.TypeKind == TypeKind.Interface;
        public bool IsTuple => typeSymbol.IsTupleType;
        public bool IsRecord => typeSymbol.IsRecord;
        public bool IsPositionalRecord => IsRecord && (((typeSymbol is INamedTypeSymbol namedType) && namedType.Constructors.Any(x => x.Parameters.Length == 0)) == false);
        public IEnumerable<TypeData> Arguments
        {
            get
            {
                if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
                {
                    foreach (var arg in namedTypeSymbol.TypeArguments)
                    {
                        yield return new TypeData(arg);
                    }
                }
                if (typeSymbol is IArrayTypeSymbol arraySymbol)
                {
                    yield return new TypeData(arraySymbol.ElementType);
                }
            }
        }
        public IEnumerable<string> TupleElementNames
        {
            get
            {
                if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
                {
                    foreach (var element in namedTypeSymbol.TupleElements)
                    {
                        yield return element.Name;
                    }
                }
            }
        }


        public IEnumerable<string> GetNamesFromMostSpecificConstructor()
        {
            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                var max = namedTypeSymbol.Constructors.Max(x => x.Parameters.Length);
                var mostSpecificConstructor = namedTypeSymbol.Constructors.Where(x => x.Parameters.Length == max).FirstOrDefault();

                foreach (var param in mostSpecificConstructor.Parameters)
                {
                    yield return param.Name;
                }
            }
        }


        public TypeData(ITypeSymbol typeSymbol)
        {
            this.typeSymbol = typeSymbol;
        }        
    }
}