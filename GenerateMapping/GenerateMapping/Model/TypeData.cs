using System.Collections.Generic;
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
        public bool IsTouple => typeSymbol.IsTupleType;
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


        public TypeData(ITypeSymbol typeSymbol)
        {
            this.typeSymbol = typeSymbol;
        }        
    }
}