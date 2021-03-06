﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace XJDbQuery.Translate
{
    using Expressions;
    using Common;
    public class Translator : ExpressionVisitor
    {
        public TranslateResult Translate(Expression expression)
        {
            expression = PartialEvaluator.Eval(expression);
            ProjectionExpression project = expression as ProjectionExpression;
            if (project == null)
            {
                expression = new QueryBinder().Bind(expression);
                expression = OrderByRewriter.Rewrite(expression);
                project = expression as ProjectionExpression;
            }
            string commandText = new QueryFormatter().FormatExpression(project.Source);

            LambdaExpression projector = new ProjectionBuilder().Build(project.Projector);

            return new TranslateResult() { CommandText = commandText, Projector = projector };
        }
    }
    public class TranslateResult
    {
        public string CommandText { get; set; }

        public LambdaExpression Projector { get; set; }
    }
    
}
