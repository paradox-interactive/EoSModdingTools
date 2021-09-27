using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using Token = RomeroGames.BRScanner.Token;
using TokenLocation = RomeroGames.BRScanner.TokenLocation;
using TokenType = RomeroGames.BRScanner.TokenType;
using System.Text;
using UnityEditor;
using UnityEngine.Assertions;

namespace RomeroGames
{
    public class BRScript
    {
        static private BRTranspiler _transpiler;

//         private readonly static string Grammar = @"
// script -> start_quote_block quote_block*

// start_quote_block -> EOL? SCRIPT STRING EOL var_statement* rule_statement* START_QUOTE EOL quote_block_statements
// quote_block -> EOL? QUOTE quote_label EOL quote_block_statements
// quote_block_statements -> predialogue_statements? dialog_statements? handle_results_statements?
// quote_label -> NUMBER | IDENTIFIER

// var_statement -> raw_var_statement | string_var_statement
// raw_var_statement -> VAR identifier expr?
// string_var_statement -> VAR STRING identifier STRING

// predialogue_statements -> statement+
// dialogue_statements -> say_statement option_statement+ control_option_statement+
// handle_results_statements -> statement+
// statement -> statement_body if_expression? EOL
// statement_body -> goto_statement
//                 | set_statement
//                 | custom_statement
//                 | trigger_statement
// say_statement -> SAY STRING EOL
// option_statement -> OPTION STRING EOL | TIMEOUT expression EOL
// control_option_statement -> (HIDE_OPTION | DISABLE_OPTION | HOVER_OPTION) if_expression EOL

// lua_statement -> LUA_START EOL
// if_statement -> IF expr EOL statement* elseif_block* else_block? END
// elseif_block -> ELSEIF expr EOL statement*
// else_block -> ELSE EOL statement*
// local_statement -> LOCAL identifier expr?
// call_statement -> CALL quote_label

// goto_statement -> GOTO ENTRY+ quote_label
// set_statement -> SET IDENTIFIER expression
// trigger_statement -> TRIGGER arg_list (IN expression duration_type)
// custom_statement -> IDENTIFIER arg_list
// arg_list -> (LEFT_PAREN (expression (COMMA expression)+)? RIGHT_PAREN)?

// if_expression -> IF expression

// duration_type -> TICKS | DAYS | MONTHS

// expression     -> logic_or
// logic_or   -> logic_and ( "or" logic_and )*
// logic_and  -> equality ( "and" equality )*
// equality       -> comparison ( ( NE | EQ ) comparison )*
// comparison     -> addition ( ( GT | GE | LT | LE ) addition )*
// addition       -> multiplication ( ( MINUS | PLUS ) multiplication )*
// multiplication -> unary ( ( SLASH | STAR ) unary )*
// unary          -> ( NOT | MINUS ) unary
//                | call
// primary        -> NUMBER | STRING | FALSE | TRUE | NIL | IDENTIFIER | TIMEOUT
//                | IDENTIFIER arg_list
//                | LEFT_PAREN expression RIGHT_PAREN
// ";


        public class BRException : System.Exception
        {
            private bool _hasLocation = false;
            private BRScanner.TokenLocation _location;


            public BRException()
                :base()
            {
                _hasLocation = false;
            }

            public BRException(BRScanner.TokenLocation location)
                :base()
            {
                _location = location;
                _hasLocation = true;
            }

            public BRException(string message)
                :base(message)
            {
                _hasLocation = false;
            }


            public BRException(BRScanner.TokenLocation location, string message)
                :base(message)
            {
                _location = location;
                _hasLocation = true;
            }

            public BRException(string message, System.Exception innerException)
                :base(message, innerException)
            {
                _hasLocation = false;
            }

            public BRException(BRScanner.TokenLocation location, string message, System.Exception innerException)
                :base(message, innerException)
            {
                _location = location;
                _hasLocation = true;
            }

            private bool HasLocation { get { return _hasLocation; }}
            private BRScanner.TokenLocation Location { get { return _location; }}
        }

        public interface IVisitor
        {
            void Visit(Script script);
            void Visit(RemStatement remStatement);
            void Visit(StartQuoteBlock startQuoteBlock);
            void Visit(FunctionBlock functionBlock);
            void Visit(QuoteBlock quoteBlock);
            void Visit(QuoteBlockStatements quoteBlockStatements);
            void Visit(DialogueStatements dialogueStatements);
            void Visit(VarStatement varStatement);
            void Visit(RuleStatement ruleStatement);
            void Visit(TitleStatement titleStatement);
            void Visit(SayStatement sayStatement);
            void Visit(ControlSayStatement controlSayStatement);
            void Visit(LocalStatement localStatement);
            void Visit(CallStatement callStatement);
            void Visit(IfStatement ifStatement);
            void Visit(PostFixIfStatement ifStatement);
            void Visit(OptionStatement optionStatement);
            void Visit(ControlOptionStatement controlOptionStatement);
            void Visit(LuaSection luaStatement);
            void Visit(MakeStringStatement makeStringStatement);
            void Visit(StringCommandExpr stringCommandStatement);
            void Visit(GotoStatement gotoStatement);
            void Visit(SetStatement setStatement);
            void Visit(CustomStatement customStatement);
            void Visit(TriggerStatement triggerStatement);
            void Visit(ArgList argList);
            void Visit(CallLuaExpr callLuaExpr);
            void Visit(UnaryExpr unaryExpression);
            void Visit(BinaryExpr binaryExpression);
            void Visit(LiteralExpr literalExpression);
            void Visit(VariableExpr variableExpression);
            void Visit(OptionExpr optionExpression);
            void Visit(TokenExpr tokenExpression);
            void Visit(CallExpr callExpression);
            void Visit(IfExpression ifExpression);
        }

        public abstract class Visited
        {
            public abstract void Accept(IVisitor visitor);
        }

        public abstract class Expr : Visited
        {
        }

        public abstract class Stmt : Visited
        {
        }

        public class UnaryExpr : Expr
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public Token op;
            public Expr arg;
        }

        public class BinaryExpr : Expr
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public Token op;
            public Expr arg1;
            public Expr arg2;
        }

        public class LiteralExpr : Expr
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public object literal;
        }

        public class VariableExpr : Expr
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public string identifier;
        }

        public class OptionExpr : Expr
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public int option;
        }

        public class TokenExpr : Expr
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public Token token;
        }

        public class CallExpr : Expr
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public string identifier;
            public ArgList argList;
        }

        public class StringCommandExpr : Expr
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public ArgList argList;
        }

        public class Script : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public StartQuoteBlock startQuoteBlock;
            public List<QuoteBlock> quoteBlocks;
            public List<FunctionBlock> functionBlocks;
        }

        public class RemStatement : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public string comment;
        }


        public class StartQuoteBlock : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public string scriptType;
            public string scriptId;
            public string scriptPrefix;
            public List<MakeStringStatement> makeStringStatements;
            public List<RemStatement> remStatements;
            public List<VarStatement> varStatements;
            public List<RuleStatement> ruleStatements;
            public LuaSection luaSection;
            public QuoteBlockStatements quoteBlockStatements;
        }

        public class TitleStatement : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public string text;
        }

        public class FunctionBlock : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public string functionLabel;
            public List<string> functionArguments;
            public List<Stmt> functionStatements;
        }

        public class QuoteBlock : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public string quoteLabel;
            public QuoteBlockStatements quoteBlockStatements;
        }

        public class QuoteBlockStatements : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public List<Stmt> predialogueStatements;
            public DialogueStatements dialogueStatements;
            public List<Stmt> handleResultsStatements;
        }

        public class DialogueStatements : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public SayStatement sayStatement;
            public ControlSayStatement controlSayStatement;
            public List<OptionStatement> optionStatements;
            public List<ControlOptionStatement> controlOptionStatements;
        }

        public class LocalStatement : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public string identifier;
            public Expr initialExpr;
        }

        public class CallStatement : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public string label;
        }

        public class IfStatement : Stmt
        {
            public class ElseIfBlock
            {
                public Expr condition;
                public List<Stmt> statements;
            }

            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public Expr condition;
            public List<Stmt> ifStatements;
            public List<ElseIfBlock> elseIfBlocks;
            public List<Stmt> elseStatements;
        }

        public class PostFixIfStatement : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public Expr condition;
            public Stmt statement;
        }

        public class VarStatement : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public string identifier;
            public Expr initialExpr;
            public string stringText;
        }

        public  class RuleStatement : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public Expr expr;
        }

        public class SayStatement : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public string text;
        }
        public class ControlSayStatement : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public string text;
        }

        public class OptionStatement : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public int option;
            public string text;
            public Expr timeOut;
        }

        public class ControlOptionStatement : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public int option;
            public Token token;
            public ArgList argList;

            public IfExpression ifExpression;
            public string text;
        }

        public class LuaSection : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public string lua;
        }

        public class MakeStringStatement : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public string identifier;
            public string text;
        }

        public class GotoStatement : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public string scriptId;

            public string entryId;

            public string label;
        }

        public class SetStatement : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public string identifier;
            public Expr expression;
        }

        public class CustomStatement : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public string identifier;
            public ArgList argList;
        }

        public class TriggerStatement : Stmt
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public ArgList argList;
            public Expr duration;
            public TokenType durationType;
        }

        public class ArgList : Expr
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public List<Expr> args;
        }

        public class CallLuaExpr : Expr
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public string lua;
        }

        public class IfExpression : Expr
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public Expr expression;
        }

        public static void TranspileFile(string file, bool silent, StringBuilder log)
        {
            //Extract mod - BRScript is our anchor point.
            string[] elems = file.Split('/');
            string mod = string.Empty;
            for (int i = 0; i < elems.Length; ++i)
            {
                if (elems[i] == "BRScript")
                {
                    mod = elems[i - 1];
                    break;
                }
            }

            if (!silent)
            {
                log.AppendLine("Transpiling BRScript file: " + file);
            }
            if (_transpiler == null)
            {
                _transpiler = new BRTranspiler();
            }
            else
            {
                _transpiler.Clear();
            }
            string fileText = System.IO.File.ReadAllText(file);
            bool result = _transpiler.Transpile(fileText);
            string destinationFile = file.Replace($"/{mod}/BRScript/", $"/{mod}/Lua/BR/").Replace(".br", ".lua");
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(destinationFile);
            System.IO.Directory.CreateDirectory(fileInfo.Directory.FullName);
            System.IO.File.WriteAllText(destinationFile, _transpiler.Output);
            Debug.AssertFormat(System.IO.File.Exists(destinationFile), "Failed to write transpiled BR script {0}", destinationFile);
            if (!result)
            {
                Debug.LogErrorFormat("Error transpiling BRScript: {0}\n{1}", destinationFile, _transpiler.Output);
            }
            else if (!silent)
            {
                log.AppendLine("Wrote transpiled file: " + destinationFile);
            }
        }

        public static void TranspileFiles(List<string> files, bool silent)
        {
            StringBuilder log = new StringBuilder();

            foreach(string file in files)
            {
                TranspileFile(file, silent, log);
            }

            if (!silent && log.Length > 0)
            {
                Debug.Log(log);
            }

            #if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.Default);
            #endif
        }

        public static void GenerateScripts(IEnumerable<string> modNames, bool silent)
        {
            double buildTimer = EditorApplication.timeSinceStartup;
            int numFiles = 0;

            //Look for all the mod folders, perform this process for each.
            foreach (string modName in modNames)
            {
                string brScriptPath = $"Assets/Mods/{modName}/BRScript";
                if (!Directory.Exists(brScriptPath))
                {
                    continue;
                }

                List<string> sourcePaths = new List<string>();
                string[] files = Directory.GetFiles(brScriptPath, "*.br", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    string fullPath = EoSPathUtils.CleanPath(file);
                    int index = fullPath.IndexOf(brScriptPath, StringComparison.Ordinal);
                    Assert.IsTrue(index > -1, "Invalid BRScript path");
                    string relativePath = fullPath.Substring(index);
                    numFiles++;
                    sourcePaths.Add(relativePath);
                }
                TranspileFiles(sourcePaths, silent);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.LogFormat("Transpiled {0} BRScript files in {1:F1} seconds.", numFiles, EditorApplication.timeSinceStartup - buildTimer);
        }

        public static void CleanScripts(IEnumerable<string> modNames)
        {
            foreach (string modName in modNames)
            {
                string modPath = $"Assets/Mods/{modName}";
                string luaOutputPath = $"{modPath}/Lua/BR";

                if (AssetDatabase.IsValidFolder(luaOutputPath))
                {
                    AssetDatabase.DeleteAsset(luaOutputPath);
                }

                if (AssetDatabase.IsValidFolder($"{modPath}/BRScript"))
                {
                    AssetDatabase.CreateFolder($"{modPath}/Lua", "BR");
                }
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
