using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using OpenFga.Language.Errors;
using OpenFga.Language.Model;

namespace OpenFga.Language;

public static class OpenFgaFromDslTransformer
{
    private static readonly Regex SpacesLinePattern = new(@"^\s*$", RegexOptions.Compiled);
    private static readonly Regex CommentedLinePattern = new(@"^\s*#.*$", RegexOptions.Compiled);

    public static string Transform(string dsl)
        => JsonSerializer.Serialize(ParseAuthorizationModel(dsl));

    public static AuthorizationModel ParseAuthorizationModel(string dsl)
    {
        var result = ParseDsl(dsl);
        if (result.IsFailure)
        {
            throw new DslErrorsException<SyntaxError>(result.Errors);
        }

        return result.AuthorizationModel;
    }

    private static string CleanLine(string line)
    {
        if (SpacesLinePattern.IsMatch(line) || CommentedLinePattern.IsMatch(line))
        {
            return string.Empty;
        }

        var cleanedLine = line.Split([" #"], StringSplitOptions.None)[0];
        return cleanedLine.TrimEnd();
    }

    public static Result ParseDsl(string dsl)
    {
        var cleanedDsl = string.Join("\n", dsl
            .Split('\n')
            .Select(CleanLine));

        var antlrStream = new AntlrInputStream(cleanedDsl);
        var errors = new List<SyntaxError>();
        var lexerErrorListener = new OpenFgaDslErrorListener<int>();
        var parserErrorListener = new OpenFgaDslErrorListener<IToken>();

        var lexer = new OpenFGALexer(antlrStream);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(lexerErrorListener);
        errors.AddRange(lexerErrorListener.Errors);

        var tokenStream = new CommonTokenStream(lexer);
        var parser = new OpenFGAParser(tokenStream);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(parserErrorListener);
        errors.AddRange(parserErrorListener.Errors);

        var listener = new OpenFgaDslListener(parser);
        ParseTreeWalker.Default.Walk(listener, parser.main());

        return new Result(listener.GetAuthorizationModel(), errors);
    }

    public class Result(AuthorizationModel authorizationModel, List<SyntaxError> errors)
    {
        public AuthorizationModel AuthorizationModel { get; } = authorizationModel;
        public List<SyntaxError> Errors { get; } = errors;

        public bool IsSuccess => Errors.Count == 0;
        public bool IsFailure => !IsSuccess;
    }
}