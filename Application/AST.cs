using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;
using Domain;
namespace Application
{


    public static class AST
    {
        public static List<object> GetPath(List<ASTNode> expression)
        {
            var result = new List<object>();
            foreach (var item in expression)
            {
                if (item.Type == "Identifier")
                {
                    result.Add(item.Value);
                }
                else if(item.Type == "ArrayAccess")
                {
                    result.Add(int.Parse(item.Value));
                }
            }
            return result;
        }
        public static object Resolve(Domain.Group g, Dictionary<string, string> groupData, List<ASTNode> expression, GroupItem self)
        {

            var stack = new Stack<object>();

            foreach (var item in expression)
            {
                if (item.Type == "Identifier")
                {
                    GroupItem? value;

                    if(item.Value == "this")
                    {
                        value = self;
                    }
                    else
                    {
                        value = g.Items.FirstOrDefault(x => x.Id == item.Value);
                    }

                    if (value != null)
                    {
                        if (value.Type == "Int" || value.Type == "Float")
                        {
                            stack.Push(double.Parse(groupData[value.Id]));
                        }
                        else
                        {
                            stack.Push(groupData[value.Id]);
                        }
                    }
                    else
                    {
                        stack.Push("undefined");
                    }
                    continue;
                }
                if (item.Type == "String")
                {
                    stack.Push(item.Value);
                    continue;
                }
                if (item.Type == "Number")
                {
                    stack.Push(double.Parse(item.Value));
                    continue;
                }
                if (item.Type == "Operator")
                {
                    var a = stack.Pop();
                    var b = stack.Pop();

                    if (a is double aa && b is double bb)
                    {
                        switch (item.Value)
                        {
                            case "+":
                                stack.Push(bb + aa);
                                break;
                            case "-":
                                stack.Push(bb - aa);
                                break;
                            case "*":
                                stack.Push(bb * aa);
                                break;
                            case "/":
                                stack.Push(bb / aa);
                                break;
                        }
                    }
                    else
                    {
                        if (item.Value == "+")
                        {
                            stack.Push(b.ToString() + a.ToString());
                        }
                        else
                        {
                            stack.Push("undefined");
                        }
                    }
                    continue;
                }
            }

            return stack.First();
        }
        public static List<ASTNode> Parse(string code)
        {
            code = Regex.Replace(code, "''", "__DOUBLE_QUOTATION_f754e_");
            code = Regex.Replace(code, @"('(.|\n)*?')", match => "$@" + match.Groups[0].Value, RegexOptions.Multiline);
            code = Regex.Replace(code, "'", "\"");
            code = Regex.Replace(code, "__DOUBLE_QUOTATION_f754e_", "'");

            //string code = "MI1_Spell_MagicLongSword.Effects[1].Magnitude = this";
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

            CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();


            var assignmentExpression = root.DescendantNodes().OfType<ExpressionSyntax>().FirstOrDefault();

            return GetPathElements(assignmentExpression);

        }
        static List<ASTNode> GetPathElements(ExpressionSyntax expression)
        {
            var pathElements = new List<ASTNode>();

            // Traverse the expression and extract path elements
            ExtractPathElements(expression, pathElements);

            return pathElements;
        }

        static bool ExtractPathElements(SyntaxNode node, List<ASTNode> pathElements)
        {
            if(node is ThisExpressionSyntax)
            {
                pathElements.Add(new() { Type = "Identifier", Value="this"});
                return true;
            }
            if (node is MemberAccessExpressionSyntax memberAccess)
            {
                // If it's a member access expression, recursively extract path elements
                ExtractPathElements(memberAccess.Expression, pathElements);
                pathElements.Add(new() { Type = "Identifier",  Value = memberAccess.Name.Identifier.Text });
                pathElements.Add(new() { Type = "Operator", Value = "."});
                return true;
            }
            if (node is ElementAccessExpressionSyntax elementAccess)
            {
                // If it's an element access expression, recursively extract path elements
                ExtractPathElements(elementAccess.Expression, pathElements);

                // For simplicity, just include the argument as part of the path
                var argument = elementAccess.ArgumentList.Arguments.FirstOrDefault();
                if (argument != null)
                {
                    pathElements.Add(new() { Type = "ArrayAccess", Value = argument.ToString() });
                }
                return true;
            }
            if (node is IdentifierNameSyntax identifierName)
            {
                // If it's an identifier name, add it to the path
                pathElements.Add(new() { Type="Identifier", Value = identifierName.Identifier.Text });
                return true;
            }
            if (node is LiteralExpressionSyntax identifierLiteral)
            {
                var result = identifierLiteral.Token.Value?.ToString();
                if (result != null) {
                    pathElements.Add(new() { Type = "Number", Value = result});
                }
                return true;
            }
            if (node is InterpolatedStringExpressionSyntax interpolatedStringExpression)
            {
                bool first = true;
                foreach (var content in interpolatedStringExpression.Contents)
                {
                    if (content is InterpolatedStringTextSyntax text)
                    {
                        pathElements.Add(new(){ Type="String", Value = text.TextToken.Text });
                    }
                    else if (content is InterpolationSyntax interpolation)
                    {
                        // Handle InterpolationSyntax if needed
                        // You might want to recursively call ExtractPathElements for the expression in interpolation
                        ExtractPathElements(interpolation.Expression, pathElements);
                    }
                    if(!first)
                    {
                        pathElements.Add(new() { Type="Operator", Value="+"});
                    }
                    else
                    {
                        first = false;
                    }
                }
                return true;
            }
            if(node is ParenthesizedExpressionSyntax parenthesizedExpression)
            {
                ExtractPathElements(parenthesizedExpression.Expression, pathElements);
            }
            if (node is BinaryExpressionSyntax binaryExpression)
            {
                ExtractPathElements(binaryExpression.Left, pathElements);

                ExtractPathElements(binaryExpression.Right, pathElements);

                pathElements.Add(new() { Type = "Operator", Value = binaryExpression.OperatorToken.Text });
                return true;

            }
            return false;
            // Add more cases as needed based on your specific requirements

            // You can handle other types of expressions based on your use case
        }
    }
}
