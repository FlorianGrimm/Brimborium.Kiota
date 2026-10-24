using Kiota.Builder.CodeDOM;

using System;
using System.Collections.Generic;
using System.Text;

namespace Kiota.Builder.OrderComparers;

public class CodeElementOrderComparerCSharp : CodeElementOrderComparer {
    private readonly static Comparer<int> _IntComparer = Comparer<int>.Default;

    public override int Compare(CodeElement? x, CodeElement? y) {
        if (ReferenceEquals(x, y)) { return 0; }
        if (y is null) { return -1; }
        if (x is null) { return +1; }
        {
            var typeFactorX = GetTypeFactor(x);
            var typeFactorY = GetTypeFactor(y);
            if (typeFactorX < typeFactorY) { return -1; }
            if (typeFactorX > typeFactorY) { return +1; }
        }
        if (x is CodeProperty codePropertyX
           && y is CodeProperty codePropertyY) {
            if ((0 <= codePropertyX.OrderDefinition) && (0 <= codePropertyY.OrderDefinition)) {
                return _IntComparer.Compare(codePropertyX.OrderDefinition, codePropertyY.OrderDefinition);
            }
        }


        return base.Compare(x, y);
    }
}
