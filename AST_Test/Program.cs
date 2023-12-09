using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

string code = "MI1_Spell_MagicLongSword[0].Description = 'this'+'that'";

code = Regex.Replace(code, @"('.*?')",match => "$" + match.Groups[0].Value);
code = Regex.Replace(code, "'", "\"");

//string code = "MI1_Spell_MagicLongSword.Effects[1].Magnitude = this";
SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();


var assignmentExpression = root.DescendantNodes().OfType<AssignmentExpressionSyntax>().FirstOrDefault();

if (assignmentExpression != null)
{
    // Get the left and right side of the assignment
    var leftSide = assignmentExpression.Left;
    var rightSide = assignmentExpression.Right;

    // Output the left and right side expressions
    Console.WriteLine("Left side: " + JsonConvert.SerializeObject(GetPathElements(leftSide)));
    Console.WriteLine("Right side: " + JsonConvert.SerializeObject(GetPathElements(rightSide)));
}


static List<string> GetPathElements(ExpressionSyntax expression)
{
    var pathElements = new List<string>();

    // Traverse the expression and extract path elements
    ExtractPathElements(expression, pathElements);

    return pathElements;
}

static bool ExtractPathElements(SyntaxNode node, List<string> pathElements)
{
    if(node is ThisExpressionSyntax)
    {
        pathElements.Add("{this}");
        return true;
    }
    if (node is MemberAccessExpressionSyntax memberAccess)
    {
        // If it's a member access expression, recursively extract path elements
        ExtractPathElements(memberAccess.Expression, pathElements);
        pathElements.Add("{"+memberAccess.Name.Identifier.Text+"}");
        pathElements.Add(".");
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
            pathElements.Add(argument.ToString());
            pathElements.Add("[]");
        }
        return true;
    }
    if (node is IdentifierNameSyntax identifierName)
    {
        // If it's an identifier name, add it to the path
        pathElements.Add("{"+identifierName.Identifier.Text+"}");
        return true;
    }
    if (node is LiteralExpressionSyntax identifierLiteral)
    {
        var result = identifierLiteral.Token.Value?.ToString();
        if (result != null) { 
            pathElements.Add(result);
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
                pathElements.Add("'"+text.TextToken.Text+"'");
            }
            else if (content is InterpolationSyntax interpolation)
            {
                // Handle InterpolationSyntax if needed
                // You might want to recursively call ExtractPathElements for the expression in interpolation
                ExtractPathElements(interpolation.Expression, pathElements);
            }
            if(!first)
            {
                pathElements.Add("+");
            }
            else
            {
                first = false;
            }
        }
        return true;
    }
    if (node is BinaryExpressionSyntax binaryExpression)
    {
        ExtractPathElements(binaryExpression.Left, pathElements);

        ExtractPathElements(binaryExpression.Right, pathElements);

        pathElements.Add(binaryExpression.OperatorToken.Text);
        return true;

    }
    return false;
    // Add more cases as needed based on your specific requirements

    // You can handle other types of expressions based on your use case
}