using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace XJDbQuery.Common
{
    using Expressions;
    public static  class PartialEvaluator
    {
        public static Expression Eval(Expression expression,Func<Expression,bool> fnCanBeEvaluated)
        {
            return Eval(expression, fnCanBeEvaluated, null);
        }
        public static Expression Eval(Expression expression ,Func<Expression,bool> fnCanBeEvaluated,Func<ConstantExpression,Expression> fnPostEval)
        {
            fnCanBeEvaluated = fnCanBeEvaluated ?? PartialEvaluator.CanBeEvaluatedLocally;
            return SubtreeEvaluator.Eval(Nominator.Nominate(fnCanBeEvaluated, expression), fnPostEval, expression);
        }
        private static bool CanBeEvaluatedLocally(Expression expression)
        {
            return expression.NodeType != ExpressionType.Parameter;
        }
        class SubtreeEvaluator : ExpressionVisitor
        {
            HashSet<Expression> candidates;
            Func<ConstantExpression, Expression> onEval;

            private SubtreeEvaluator(HashSet<Expression> candidates,Func<ConstantExpression,Expression> onEval)
            {
                this.candidates = candidates;
                this.onEval = onEval;
            }
            internal static Expression Eval(HashSet<Expression> candidates, Func<ConstantExpression,Expression> onEval,Expression exp)
            {
                return new SubtreeEvaluator(candidates, onEval).Visit(exp);
            }
            protected override Expression Visit(Expression exp)
            {
                if (exp == null) return null;

                if (this.candidates.Contains(exp))
                    return this.Evaluate(exp);
                return base.Visit(exp);
            }
            private Expression PostEval(ConstantExpression expression)
            {
                return this.onEval == null ? this.onEval(expression) : expression;
            }
            private Expression Evaluate(Expression expression)
            {
                Type type = expression.Type;
                if(expression.NodeType == ExpressionType.Convert)
                {
                    var uexpr = (UnaryExpression)expression;
                    if(TypeHelper.GetNonNullableType(uexpr.Operand.Type)==
                        TypeHelper.GetNonNullableType(type))
                    {
                        expression = ((UnaryExpression)expression).Operand;
                    }
                }
                if (expression.NodeType == ExpressionType.Constant)
                {
                    if (expression.Type == type)
                    {
                        return expression;
                    }
                    else if (TypeHelper.GetNonNullableType(expression.Type) ==
                        TypeHelper.GetNonNullableType(type))
                    {
                        return Expression.Constant(((ConstantExpression)expression).Value, type);
                    }
                }
                var me = expression as MemberExpression;

                if (me != null)
                {
                    var ce = me.Expression as ConstantExpression;
                    if(ce!= null)
                    {
                        return this.PostEval(Expression.Constant(me.Member.GetValue(ce.Value), type));
                    }
                }

                if (type.IsValueType)
                {
                    expression = Expression.Convert(expression, typeof(object));
                }
                Expression<Func<object>> lambda = Expression.Lambda<Func<object>>(expression);

                Func<object> fn = lambda.Compile();
                return this.PostEval(Expression.Constant(fn(), type));
            }


            


        }
        class Nominator : ExpressionVisitor
        {
            Func<Expression, bool> fnCanBeEvaluated;
            HashSet<Expression> candidates;
            bool cannotBeEvaluated;

            private Nominator(Func<Expression,bool> fnCanBeEvaluated)
            {
                this.candidates = new HashSet<Expression>();
                this.fnCanBeEvaluated = fnCanBeEvaluated;
            }

            internal static HashSet<Expression> Nominate(Func<Expression,bool> fnCanBeEvaluated,Expression expression)
            {
                Nominator nominator = new Nominator(fnCanBeEvaluated);
                nominator.Visit(expression);
                return nominator.candidates;
            }
            protected override Expression VisitConstant(ConstantExpression c)
            {
                return base.VisitConstant(c);
            }

            protected override Expression Visit(Expression exp)
            {
                if (exp == null) return null;
                bool saveCannotBeEvaluated = this.cannotBeEvaluated;
                base.Visit(exp);
                if (!this.cannotBeEvaluated)
                {
                    if (this.fnCanBeEvaluated(exp))
                    {
                        this.candidates.Add(exp);
                    }else
                    {
                        this.cannotBeEvaluated = true;
                    }
                }
                this.cannotBeEvaluated |= saveCannotBeEvaluated;
                return exp;
            }
        }
    }

    /// <summary>
    /// Reflection 扩展方法
    /// 利用反射获取或设置值
    /// </summary>
    public static class ReflectionExtensions
    {
        public static object GetValue(this MemberInfo member, object instance)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Property:
                    return ((PropertyInfo)member).GetValue(instance, null);
                case MemberTypes.Field:
                    return ((FieldInfo)member).GetValue(instance);
                default:
                    throw new InvalidOperationException();
            }
        }

        public static void SetValue(this MemberInfo member, object instance, object value)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Property:
                    var pi = (PropertyInfo)member;
                    pi.SetValue(instance, value, null);
                    break;
                case MemberTypes.Field:
                    var fi = (FieldInfo)member;
                    fi.SetValue(instance, value);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
