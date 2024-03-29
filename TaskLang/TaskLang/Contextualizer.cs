﻿using System;
using System.Collections.Generic;
using System.Linq;
using TaskLang.Tokens;

namespace TaskLang.Context
{
    public static class Contextualizer
    {
        public static Token[][] SplitTokens(Token[] tokenList)
        {
            List<Token[]> split = new List<Token[]>();
            List<Token> current = new List<Token>();

            foreach (Token t in tokenList)
            {
                current.Add(t);
                switch (t.Type)
                {
                    case TokenType.NewLine:
                    case TokenType.Semicolon:
                    case TokenType.Arrow:
                        split.Add(current.ToArray());
                        current.Clear();
                        break;
                    default: break;
                }
            }
            return split.ToArray();
        }
        public static ExpressionContext[] ClassifyExpressions(Token[][] tokens, Context currentContext)
        {
            List<ExpressionContext> result = new List<ExpressionContext>();
            int lineNum = 0;

            foreach (Token[] line in tokens)
            {
                if (Array.Exists(line, t => t.Type == TokenType.Assign)) //contains =
                {
                    if (Array.Exists(line, t => t.Type == TokenType.Var)) //var declaration
                    {
                        //format [var, name, =, value]
                        if (currentContext.DeclareVariable(line[1].Info))
                        {
                            result.Add(new ExpressionContext(new Expression(ExpressionType.VarDeclaration, line, lineNum), currentContext));
                        }
                        else
                        {
                            result.Add(new ExpressionContext(new ExpressionError($"Variable \"{line[1].Info}\" already exists", lineNum), currentContext));
                        }
                    }
                    else //var assignment
                    {
                        //format [name, =, value]
                        if (currentContext.AssignVariable(line[0].Info, null))
                        {
                            result.Add(new ExpressionContext(new Expression(ExpressionType.VarAssignment, line, lineNum), currentContext));
                        }
                        else
                        {
                            result.Add(new ExpressionContext(new ExpressionError($"Variable \"{line[0].Info}\" does not exist", lineNum), currentContext));
                        }
                    }
                }
                else if (Array.Exists(line, t => t.Type == TokenType.Return)) //return
                {
                    //format [return, expr]
                    result.Add(new ExpressionContext(new Expression(ExpressionType.Return, line, lineNum), currentContext));
                }
                else if (Array.Exists(line, t => t.Type == TokenType.Arrow)) //func definition
                {
                    //format [name, args..., ->]
                    result.Add(new ExpressionContext(new Expression(ExpressionType.FunctionDefinition, line, lineNum), currentContext));
                }
                else if (Array.Exists(line, t => new TokenType[] { TokenType.Elif, TokenType.Else, TokenType.For, TokenType.If, TokenType.While }.Contains(t.Type))) //control
                {
                    result.Add(new ExpressionContext(new Expression(ExpressionType.Control, line, lineNum), currentContext));
                }
                else //func call
                {
                    //format [name, args...]
                    if (Array.Exists(line, t => t.Type != TokenType.NewLine))
                        result.Add(new ExpressionContext(new Expression(ExpressionType.FunctionCall, line, lineNum), currentContext));
                }
                if (Array.Exists(line, t => t.Type == TokenType.NewLine)) lineNum++;
            }
            return result.ToArray();
        }
    }

    public struct ExpressionContext
    {
        public Expression Expression { get; private set; }
        public Context Context { get; private set; }

        public ExpressionContext(Expression Expression, Context Context)
        {
            this.Expression = Expression;
            this.Context = Context;
        }

        public override string ToString()
        {
            return $"#{Expression.LineNumber} [{Expression.Type}] {string.Join(" ", Expression.Tokens.Select(t => t.ToString()))}";
        }
    }
}