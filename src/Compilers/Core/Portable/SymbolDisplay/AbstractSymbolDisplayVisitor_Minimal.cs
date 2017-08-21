﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SymbolDisplay
{
    internal abstract partial class AbstractSymbolDisplayVisitor : SymbolVisitor
    {
        protected abstract bool ShouldRestrictMinimallyQualifyLookupToNamespacesAndTypes();

        protected bool IsMinimizing
        {
            get { return this.semanticModelOpt != null; }
        }

        protected bool NameBoundSuccessfullyToSameSymbol(INamedTypeSymbol symbol)
        {
            ImmutableArray<ISymbol> normalSymbols = ShouldRestrictMinimallyQualifyLookupToNamespacesAndTypes()
                ? semanticModelOpt.LookupNamespacesAndTypes(positionOpt, name: symbol.Name)
                : semanticModelOpt.LookupSymbols(positionOpt, name: symbol.Name);
            ISymbol normalSymbol = SingleSymbolWithArity(normalSymbols, symbol.Arity);

            if (normalSymbol == null)
            {
                return false;
            }

            // Binding normally ended up with the right symbol.  We can definitely use the
            // simplified name.
            if (normalSymbol.Equals(symbol.OriginalDefinition))
            {
                return true;
            }

            // Binding normally failed.  We may be in a "Color Color" situation where 'Color'
            // will bind to the field, but we could still allow simplification here.
            ImmutableArray<ISymbol> typeOnlySymbols = semanticModelOpt.LookupNamespacesAndTypes(positionOpt, name: symbol.Name);
            ISymbol typeOnlySymbol = SingleSymbolWithArity(typeOnlySymbols, symbol.Arity);

            return typeOnlySymbol == null ? false : typeOnlySymbol.Equals(symbol.OriginalDefinition);
        }

        private static ISymbol SingleSymbolWithArity(ImmutableArray<ISymbol> candidates, int desiredArity)
        {
            ISymbol singleSymbol = null;
            foreach (ISymbol candidate in candidates)
            {
                int arity;
                switch (candidate.Kind)
                {
                    case SymbolKind.NamedType:
                        arity = ((INamedTypeSymbol)candidate).Arity;
                        break;
                    case SymbolKind.Method:
                        arity = ((IMethodSymbol)candidate).Arity;
                        break;
                    default:
                        arity = 0;
                        break;
                }

                if (arity == desiredArity)
                {
                    if (singleSymbol == null)
                    {
                        singleSymbol = candidate;
                    }
                    else
                    {
                        singleSymbol = null;
                        break;
                    }
                }
            }
            return singleSymbol;
        }
    }
}
