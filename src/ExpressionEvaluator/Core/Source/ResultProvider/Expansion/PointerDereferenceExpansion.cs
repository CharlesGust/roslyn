// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Microsoft.VisualStudio.Debugger.Metadata;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class PointerDereferenceExpansion : Expansion
    {
        private readonly TypeAndCustomInfo _elementTypeAndInfo;

        public PointerDereferenceExpansion(TypeAndCustomInfo elementTypeAndInfo)
        {
            Debug.Assert(elementTypeAndInfo.Type != null);
            _elementTypeAndInfo = elementTypeAndInfo;
        }

        internal override void GetRows(
            ResultProvider resultProvider,
            ArrayBuilder<EvalResultDataItem> rows,
            DkmInspectionContext inspectionContext,
            EvalResultDataItem parent,
            DkmClrValue value,
            int startIndex,
            int count,
            bool visitAll,
            ref int index)
        {
            if (InRange(startIndex, count, index))
            {
                rows.Add(GetRow(resultProvider, inspectionContext, value, _elementTypeAndInfo, parent: parent));
            }

            index++;
        }

        private static EvalResultDataItem GetRow(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            DkmClrValue pointer,
            TypeAndCustomInfo elementTypeAndInfo,
            EvalResultDataItem parent)
        {
            var value = pointer.Dereference(inspectionContext);
            var wasExceptionThrown = value.EvalFlags.Includes(DkmEvaluationResultFlags.ExceptionThrown);

            var expansion = wasExceptionThrown ?
                null :
                resultProvider.GetTypeExpansion(inspectionContext, elementTypeAndInfo, value, ExpansionFlags.None);
            var fullName = string.Format("*{0}", parent.ChildFullNamePrefix);
            var editableValue = resultProvider.Formatter.GetEditableValue(value, inspectionContext);

            // NB: Full name is based on the real (i.e. not DebuggerDisplay) name.  This is a change from dev12, 
            // which used the DebuggerDisplay name, causing surprising results in "Add Watch" scenarios.
            return new EvalResultDataItem(
                ExpansionKind.PointerDereference,
                name: fullName,
                typeDeclaringMemberAndInfo: default(TypeAndCustomInfo),
                declaredTypeAndInfo: elementTypeAndInfo,
                parent: null,
                value: value,
                displayValue: wasExceptionThrown ? string.Format(Resources.InvalidPointerDereference, fullName) : null,
                expansion: expansion,
                childShouldParenthesize: true,
                fullName: fullName,
                childFullNamePrefixOpt: fullName,
                formatSpecifiers: Formatter.NoFormatSpecifiers,
                category: DkmEvaluationResultCategory.Other,
                flags: DkmEvaluationResultFlags.None,
                editableValue: editableValue,
                inspectionContext: inspectionContext);
        }
    }
}
