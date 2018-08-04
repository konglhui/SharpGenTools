﻿using System;
using System.Collections.Generic;
using System.Text;
using SharpGen.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpGen.Generator.Marshallers
{
    class ArrayLengthRelationMarshaller : MarshallerBase, IRelationMarshaller
    {
        public ArrayLengthRelationMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public StatementSyntax GenerateManagedToNative(CsMarshalBase publicElement, CsMarshalBase relatedElement)
        {
            return ExpressionStatement(
                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                GetMarshalStorageLocation(relatedElement),
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(publicElement.Name),
                    IdentifierName("Length"))));
        }

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase publicElement, CsMarshalBase relatedElement)
        {
            return ExpressionStatement(
                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                IdentifierName(publicElement.Name),
                ObjectCreationExpression(
                    ArrayType(
                        ParseTypeName(publicElement.PublicType.QualifiedName),
                        SingletonList(
                            ArrayRankSpecifier(
                                SingletonSeparatedList(
                                    GetMarshalStorageLocation(relatedElement))))))));
        }
    }
}
