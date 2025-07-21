using UnityEngine;
using System.Collections.Generic;
using Token = RomeroGames.BRScanner.Token;
using TokenLocation = RomeroGames.BRScanner.TokenLocation;
using TokenType = RomeroGames.BRScanner.TokenType;

namespace RomeroGames
{
    public class BRParser
    {
        private List<Token> _tokens;
        private int _current = 0;

        public BRParser()
        {
        }

        public void Clear()
        {
            _tokens = null;
            _current = 0;
        }

        public BRScript.Script Parse(List<Token> tokens)
        {
            _tokens = tokens;
            _current = 0;
            return ParseScript();
        }

        private bool IsAtEnd() {
            return Peek().type == TokenType.EOF;
        }

        private Token Peek() {
            return _tokens[_current];
        }

        private Token Advance() {
            if (!IsAtEnd())
            {
                _current++;
                return Previous();
            }
            return Peek();
        }

        private Token Previous() {
            if (_current == 0)
            {
                return new Token(TokenType.EOL, "", null, new TokenLocation(1, 1));
            }
            return _tokens[_current - 1];
        }

        private bool Check(TokenType tokenType) {
            if (IsAtEnd())
            {
                return false;
            }
            return Peek().type == tokenType;
        }

        private bool CheckOrAtEnd(TokenType tokenType) {
            if (IsAtEnd())
            {
                return true;
            }
            return Peek().type == tokenType;
        }

        private bool Match(params TokenType[] types) {
            foreach (TokenType type in types)
            {
                if (Check(type)) {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        private BRScript.BRException NewBRException(string message)
        {
            string lexeme = Peek().lexeme;
            if (lexeme == null)
            {
                lexeme = "";
            }
            TokenLocation location = Peek().location;
            throw new BRScript.BRException(location, string.Format("{0}  ... {1} at ({2},{3}) - CurTokenType = {4}",
                    message, lexeme, location.line, location.offset, Peek().type.ToString()));
        }

        private Token Consume(TokenType type, string message)
        {
            if (Check(type))
            {
                 return Advance();
            }

            throw NewBRException(message);
        }

        private Token ConsumeEndOfLineOrEndOfFile(string message)
        {
            if(Check(TokenType.EOL) || IsAtEnd())
            {
                return Advance();
            }
            throw NewBRException(message);
        }

        private BRScript.Script ParseScript()
        {
            BRScript.Script result = new BRScript.Script();
            result.startQuoteBlock = ParseStartQuoteBlock();

            while(true) // Just to terrify any programmer who sees this.
            {
                while (Match(TokenType.EOL))
                {
                };
                if (Match(TokenType.QUOTE))
                {
                    if (result.quoteBlocks == null)
                    {
                        result.quoteBlocks = new List<BRScript.QuoteBlock>();
                    }
                    BRScript.QuoteBlock quoteBlock = ParseQuoteBlock();
                    result.quoteBlocks.Add(quoteBlock);
                }
                else if (Match(TokenType.FUNCTION))
                {
                    if (result.functionBlocks == null)
                    {
                        result.functionBlocks = new List<BRScript.FunctionBlock>();
                    }
                    BRScript.FunctionBlock functionBlock = ParseFunctionBlock();
                    result.functionBlocks.Add(functionBlock);
                }
                else
                {
                    break;
                }
            }
            return result;
        }

        private void ParseRemStatements(BRScript.StartQuoteBlock result, ref List<BRScript.RemStatement> remStatements)
        {
            BRScript.RemStatement remStatement = ParseRemStatement();
            if (remStatement != null)
            {
                if (remStatements == null)
                {
                    remStatements = new List<BRScript.RemStatement>();
                }
                while (remStatement != null)
                {
                    remStatements.Add(remStatement);
                    remStatement = ParseRemStatement();
                }
                result.remStatements = remStatements;
            }
            while (Match(TokenType.EOL))
            {
            };
        }

        private void ParseVarStatements(BRScript.StartQuoteBlock result, ref List<BRScript.VarStatement> varStatements)
        {
            BRScript.VarStatement varStatement = ParseVarStatement();
            if (varStatement != null)
            {
                if (varStatements == null)
                {
                    varStatements = new List<BRScript.VarStatement>();
                }
                while (varStatement != null)
                {
                    varStatements.Add(varStatement);
                    varStatement = ParseVarStatement();
                }
                result.varStatements = varStatements;
            }
            while (Match(TokenType.EOL))
            {
            };
        }

        private BRScript.StartQuoteBlock ParseStartQuoteBlock()
        {
            while (Match(TokenType.EOL))
            {
            };
            if (IsAtEnd())
            {
                return null;
            }

            BRScript.StartQuoteBlock result = new BRScript.StartQuoteBlock();

            List<BRScript.RemStatement> remStatements = null;
            ParseRemStatements(result, ref remStatements);

            Consume(TokenType.SCRIPT, "Expected SCRIPT command at beginning of script!");
            if (!Match(TokenType.IDENTIFIER))
            {
                throw NewBRException("Missing script type after SCRIPT command!");
            }
            result.scriptType = Previous().literal.ToString();
            if (!result.scriptType.Equals("SITDOWN") && !result.scriptType.Equals("INTERACTION"))
            {
                throw NewBRException("Invalid script type " + result.scriptType);
            }
            if (!Match(TokenType.STRING))
            {
                throw NewBRException("Missing id after SCRIPT command!");
            }
            result.scriptId = Previous().literal.ToString();
            if (Match(TokenType.STRING))
            {
                result.scriptPrefix = Previous().literal.ToString();
            }
            ConsumeEndOfLineOrEndOfFile("Expected new line after SCRIPT command!");

            List<BRScript.VarStatement> varStatements = null;

            ParseVarStatements(result, ref varStatements);

            List<BRScript.MakeStringStatement> makeStringStatements = null;
            while (Match(TokenType.MAKE_STRING))
            {
                if (makeStringStatements == null)
                {
                    makeStringStatements = new List<BRScript.MakeStringStatement>();
                }
                BRScript.MakeStringStatement makeStringStmt = new BRScript.MakeStringStatement();
                Consume(TokenType.IDENTIFIER, "Expected identifier after MAKE_STRING command!");
                makeStringStmt.identifier = Previous().literal.ToString();
                Consume(TokenType.STRING, "Expected string after MAKE_STRING identifier!");
                makeStringStmt.text = Previous().literal.ToString();
                makeStringStatements.Add(makeStringStmt);
                while (Match(TokenType.EOL))
                {
                };
            }
            result.makeStringStatements = makeStringStatements;

            ParseVarStatements(result, ref varStatements);

            List<BRScript.RuleStatement> ruleStatements = null;
            BRScript.RuleStatement ruleStatement = ParseRuleStatement();
            if (ruleStatement != null)
            {
                ruleStatements = new List<BRScript.RuleStatement>();
                while (ruleStatement != null)
                {
                    ruleStatements.Add(ruleStatement);
                    ruleStatement = ParseRuleStatement();
                }
                result.ruleStatements = ruleStatements;
            }

            ParseVarStatements(result, ref varStatements);

            while (Match(TokenType.EOL))
            {
            };
            if (Match(TokenType.LUA_SECTION))
            {
                result.luaSection = ParseLuaSection();
            }
            while (Match(TokenType.EOL))
            {
            };
            Consume(TokenType.START_QUOTE, "Expected START_QUOTE command!");
            ConsumeEndOfLineOrEndOfFile("Expected new line after START_QUOTE!");
            result.quoteBlockStatements = ParseQuoteBlockStatements();
            return result;
        }

        private BRScript.FunctionBlock ParseFunctionBlock()
        {
            BRScript.FunctionBlock result = new BRScript.FunctionBlock();
            if (Check(TokenType.IDENTIFIER)) // Cannot use numbers
            {
                result.functionLabel = Peek().literal.ToString();
                Advance();
                while(!Check(TokenType.EOL))
                {
                    if(result.functionArguments == null)
                    {
                        result.functionArguments = new List<string>();
                    }
                    if(Check(TokenType.IDENTIFIER))
                    {
                        result.functionArguments.Add(Peek().literal.ToString());
                        Advance();
                    }
                    else
                    {
                        throw NewBRException("Invalid argument to FUNCTION command!");
                    }
                }
                ConsumeEndOfLineOrEndOfFile("Expected new line after FUNCTION command and arguments!");
                List<BRScript.Stmt> functionStatements = null;
                BRScript.Stmt statement = ParseStatement();
                if (statement != null)
                {
                    functionStatements = new List<BRScript.Stmt>();
                    while (statement != null)
                    {
                        functionStatements.Add(statement);
                        statement = ParseStatement();
                    }
                }
                result.functionStatements = functionStatements;
            }
            else
            {
                throw NewBRException("Invalid token after FUNCTION command!");
            }
            return result;
        }

        private BRScript.QuoteBlock ParseQuoteBlock()
        {
            BRScript.QuoteBlock result = new BRScript.QuoteBlock();
            if (Check(TokenType.NUMBER) || Check(TokenType.IDENTIFIER))
            {
                result.quoteLabel = Peek().literal.ToString();
                Advance();
                ConsumeEndOfLineOrEndOfFile("Expected new line after QUOTE command!");
                result.quoteBlockStatements = ParseQuoteBlockStatements();
            }
            else
            {
                throw NewBRException("Invalid token after QUOTE command!");
            }
            return result;
        }

        private BRScript.QuoteBlockStatements ParseQuoteBlockStatements()
        {
            List<BRScript.Stmt> preDialogueStatements = null;
            BRScript.Stmt statement = ParseStatement();

            if (statement != null)
            {
                preDialogueStatements = new List<BRScript.Stmt>();
                while (statement != null)
                {
                    preDialogueStatements.Add(statement);
                    statement = ParseStatement();
                }
            }

            BRScript.DialogueStatements dialogueStatements = ParseDialogueStatements();

            List<BRScript.Stmt> handleResultsStatements = null;
            if (dialogueStatements != null)
            {
                statement = ParseStatement();
                if (statement != null)
                {
                    handleResultsStatements = new List<BRScript.Stmt>();
                    while (statement != null)
                    {
                        handleResultsStatements.Add(statement);
                        statement = ParseStatement();
                    }
                }
            }
            if (preDialogueStatements == null && dialogueStatements == null && handleResultsStatements == null)
            {
                return null;
            }

            if (dialogueStatements != null && dialogueStatements.optionStatements == null)
            {
                bool foundOptions = false;
                if (handleResultsStatements != null)
                {
                    foreach(BRScript.Stmt stmt in handleResultsStatements)
                    {
                        if (stmt is BRScript.LuaSection)
                        {
                            // Assume we have automatically handled options here
                            foundOptions = true;
                        }
                    }
                }
                if (!foundOptions)
                {
                    throw NewBRException("No options exist in the quote!");
                }
            }

            BRScript.QuoteBlockStatements result = new BRScript.QuoteBlockStatements();
            result.predialogueStatements = preDialogueStatements;
            result.dialogueStatements = dialogueStatements;
            result.handleResultsStatements = handleResultsStatements;
            return result;
        }

        private BRScript.DialogueStatements ParseDialogueStatements()
        {
            BRScript.SayStatement sayStatement = null;
            BRScript.ControlSayStatement controlSayStatement = null;
            List<BRScript.OptionStatement> optionStatements = null;
            List<BRScript.ControlOptionStatement> controlOptionStatements = null;
            bool foundSay = false;
            while (true)
            {
                if (Match(TokenType.SAY))
                {
                    if (foundSay)
                    {
                        throw NewBRException("Only one SAY command can exist per Quote!");
                    }
                    foundSay = true;
                    sayStatement = new BRScript.SayStatement();
                    Token cur = Peek();
                    if (cur.type == TokenType.STRING)
                    {
                        sayStatement.text = cur.literal.ToString();
                        Advance();
                    }
                    else
                    {
                        throw NewBRException("Unexpected token after SAY command - expected a string!");
                    }
                }
                else if (Check(TokenType.OPTION) || Check(TokenType.TIMEOUT))
                {
                    if (optionStatements == null)
                    {
                        optionStatements = new List<BRScript.OptionStatement>();
                    }
                    optionStatements.Add(ParseOptionStatement());
                }
                else if (Check(TokenType.HIDE_OPTION) || Check(TokenType.DISABLE_OPTION) || Check(TokenType.SUCCESS_CHANCE_OPTION) || Check(TokenType.TONE_OPTION) || Check(TokenType.TOOLTIP_OPTION) || Check(TokenType.HOVER_OPTION) || Check(TokenType.REWARD_OPTION) || Check(TokenType.ROLE))
                {
                    if (controlOptionStatements == null)
                    {
                        controlOptionStatements = new List<BRScript.ControlOptionStatement>();
                    }
                    controlOptionStatements.Add(ParseControlOptionStatement());
                }
                else if (Match(TokenType.SAY_EMOTE))
                {
                    if (controlSayStatement == null)
                    {
                        controlSayStatement = new BRScript.ControlSayStatement();
                    }
                    Token cur = Peek();
                    if (cur.type == TokenType.STRING)
                    {
                        controlSayStatement.text = cur.literal.ToString();
                        Advance();
                    }
                    else
                    {
                        throw NewBRException("Unexpected token after SAY_EMOTE command - expected a string!");
                    }
                }
                else
                {
                    break;
                }
                while (Match(TokenType.EOL))
                {
                };
            }
            if (sayStatement == null && optionStatements == null && controlOptionStatements == null)
            {
                return null;
            }
            else if (sayStatement == null)
            {
                throw NewBRException("No SAY command was found to match the options in the quote!");
            }
            BRScript.DialogueStatements result = new BRScript.DialogueStatements();
            result.sayStatement = sayStatement;
            result.controlSayStatement = controlSayStatement;
            result.optionStatements = optionStatements;
            result.controlOptionStatements = controlOptionStatements;
            return result;
        }

        private BRScript.OptionStatement ParseOptionStatement()
        {
            Token cur = Peek();
            BRScript.OptionStatement result = new BRScript.OptionStatement();
            if (cur.type == TokenType.TIMEOUT)
            {
                result.timeOut = ParseExpression();
            }
            else if (cur.type == TokenType.OPTION)
            {
                result.option = (int)(cur.literal);
                Advance();
                cur = Peek();
                if (cur.type == TokenType.STRING)
                {
                    result.text = cur.literal.ToString();
                    Advance();
                }
                else
                {
                    throw NewBRException("Unexpected token after OPTION command - expected text!");
                }
            }
            else
            {
                return null;
            }
            return result;
        }

        private BRScript.ControlOptionStatement ParseControlOptionStatement()
        {
            Token cur = Peek();
            BRScript.ControlOptionStatement result = null;
            if (cur.type == TokenType.HIDE_OPTION)
            {
                result = new BRScript.ControlOptionStatement();
                result.token = cur;
                result.option = (int)cur.literal;
                Advance();
            }
            else if (cur.type == TokenType.DISABLE_OPTION)
            {
                result = new BRScript.ControlOptionStatement();
                result.token = cur;
                result.option = (int)cur.literal;
                Advance();
            }
            else if (cur.type == TokenType.HOVER_OPTION)
            {
                result = new BRScript.ControlOptionStatement();
                result.token = cur;
                result.option = (int)cur.literal;
                Advance();
            }
            else if (cur.type == TokenType.ROLE)
            {
                result = new BRScript.ControlOptionStatement();
                result.token = cur;
                result.option = (int)cur.literal;
                Advance();
            }
            else if (cur.type == TokenType.TONE_OPTION)
            {
                result = new BRScript.ControlOptionStatement();
                result.token = cur;
                result.option = (int)cur.literal;
                Advance();
                result.argList = new BRScript.ArgList();
                result.argList.args = new List<BRScript.Expr>();
                result.argList.args.Add(ParseExpression());
            }
            else if (cur.type == TokenType.SUCCESS_CHANCE_OPTION)
            {
                result = new BRScript.ControlOptionStatement();
                result.token = cur;
                result.option = (int)cur.literal;
                Advance();
                result.argList = new BRScript.ArgList();
                result.argList.args = new List<BRScript.Expr>();
                result.argList.args.Add(ParseExpression());
            }
            else if (cur.type == TokenType.TOOLTIP_OPTION)
            {
                result = new BRScript.ControlOptionStatement();
                result.token = cur;
                result.option = (int)cur.literal;
                Advance();
                result.argList = ParseArgList();
            }
            else if (cur.type == TokenType.REWARD_OPTION)
            {
                result = new BRScript.ControlOptionStatement();
                result.token = cur;
                result.option = (int)cur.literal;
                Advance();
            }
            else
            {
                return null;
            }
            if (Peek().type == TokenType.EOL)
            {
                return result;
            }
            if(result.token.type == TokenType.HOVER_OPTION)
            {
                Token curToken = Peek();
                if (curToken.type == TokenType.STRING)
                {
                    result.text = curToken.literal.ToString();
                    Advance();
                }
                else
                {
                    throw NewBRException("Unexpected token after HOVER_OPTION command - expected a string!");
                }
            }
            else if(result.token.type == TokenType.REWARD_OPTION)
            {
                Token curToken = Peek();
                if (curToken.type == TokenType.STRING)
                {
                    result.text = curToken.literal.ToString();
                    Advance();
                }
                else
                {
                    throw NewBRException("Unexpected token after REWARD_OPTION command - expected a string!");
                }
            }
            if(result.token.type == TokenType.ROLE)
            {
                Token curToken = Peek();
                if (curToken.type == TokenType.STRING)
                {
                    result.text = curToken.literal.ToString();
                    Advance();
                }
                else
                {
                    throw NewBRException("Unexpected token after ROLE command - expected a string!");
                }
            }
            else
            {
                result.ifExpression = ParseIfExpression();
            }
            if (!(IsAtEnd() || Peek().type == TokenType.EOL))
            {
                throw NewBRException("Unexpected token after option modifier command!");
            }
            return result;
        }

        private BRScript.IfExpression ParseIfExpression()
        {
            if (!Match(TokenType.IF))
            {
                return null;
            }
            BRScript.IfExpression result = new BRScript.IfExpression();
            result.expression = ParseExpression();
            return result;
        }

        private BRScript.RemStatement ParseRemStatement()
        {
            if (Check(TokenType.REM))
            {
                BRScript.RemStatement remStatement = new BRScript.RemStatement();
                remStatement.comment = Peek().literal.ToString();
                Advance();
                ConsumeEndOfLineOrEndOfFile("Expected end of line after REM statement!");
                return remStatement;
            }

            return null;
        }

        private BRScript.VarStatement ParseVarStatement()
        {
            BRScript.VarStatement result = null;
            if (Match(TokenType.VAR))
            {
                result = new BRScript.VarStatement();

                if (Match(TokenType.STRING_COMMAND))
                {
                    result.identifier = Consume(TokenType.IDENTIFIER, "Expected identifier after VAR STRING command!").literal.ToString();
                    Consume(TokenType.STRING, "Expected string after VAR STRING command!");
                    result.stringText = Previous().literal.ToString();
                }
                else
                {
                    result.identifier = Consume(TokenType.IDENTIFIER, "Expected identifier after VAR command!").literal.ToString();
                    if (!(Check(TokenType.EOL) || Check(TokenType.EOF)))
                    {
                        result.initialExpr = ParseExpression();
                    }
                }
                ConsumeEndOfLineOrEndOfFile("Expected end of line after VAR statement!");
            }
            return result;
        }

        private BRScript.RuleStatement ParseRuleStatement()
        {
            BRScript.RuleStatement result = null;
            if (Match(TokenType.RULE))
            {
                result = new BRScript.RuleStatement();
                result.expr = ParseExpression();
                ConsumeEndOfLineOrEndOfFile("Expected end of line after RULE statement!");
            }
            return result;
        }

        private BRScript.LuaSection ParseLuaSection()
        {
            BRScript.LuaSection result = new BRScript.LuaSection();
            result.lua = Previous().literal.ToString();
            return result;
        }

        private BRScript.Stmt ParseStatement()
        {
            // Parse non-dialogue statements
            BRScript.Stmt result = null;
            if (Match(TokenType.GOTO))
            {
                BRScript.GotoStatement gotoStmt = new BRScript.GotoStatement();
                if (Match(TokenType.ENTRY))
                {
                    Consume(TokenType.IDENTIFIER, "Expected identifier after GOTO ENTRY command!");
                    gotoStmt.entryId = Previous().literal.ToString();
                    result = gotoStmt;
                }
                else
                {
                    if (Match(TokenType.SCRIPT))
                    {
                        Consume(TokenType.STRING, "Expected string after GOTO SCRIPT command!");
                        gotoStmt.scriptId = Previous().literal.ToString();
                    }
                    if (Check(TokenType.NUMBER) || Check(TokenType.IDENTIFIER))
                    {
                        gotoStmt.label = Peek().literal.ToString();
                        Advance();
                        result = gotoStmt;
                    }
                    else
                    {
                        throw NewBRException("Expected label after GOTO command!");
                    }
                }
            }
            else if (Match(TokenType.LUA_SECTION))
            {
                result = ParseLuaSection();
            }
            else if (Match(TokenType.MAKE_STRING))
            {
                BRScript.MakeStringStatement makeStringStmt = new BRScript.MakeStringStatement();
                Consume(TokenType.IDENTIFIER, "Expected identifier after MAKE_STRING command!");
                makeStringStmt.identifier = Previous().literal.ToString();
                Consume(TokenType.STRING, "Expected string after MAKE_STRING identifier!");
                makeStringStmt.text = Previous().literal.ToString();
                result = makeStringStmt;
            }
            else if (Match(TokenType.CALL))
            {
                BRScript.CallStatement callStmt = new BRScript.CallStatement();
                if (Check(TokenType.NUMBER) || Check(TokenType.IDENTIFIER))
                {
                    callStmt.label = Peek().literal.ToString();
                    Advance();
                    result = callStmt;
                }
                else
                {
                    throw NewBRException("Expected label after CALL command!");
                }
            }
            else if (Match(TokenType.LOCAL))
            {
                BRScript.LocalStatement localStmt = new BRScript.LocalStatement();
                if (!Check(TokenType.IDENTIFIER))
                {
                    throw NewBRException("Expected identifier after LOCAL command!");
                }
                localStmt.identifier = Peek().literal.ToString();
                Advance();
                if (Check(TokenType.EOL) || Check(TokenType.EOF))
                {
                    return localStmt;
                }
                localStmt.initialExpr = ParseExpression();
                result = localStmt;
            }
            else if (Match(TokenType.IF))
            {
                BRScript.IfStatement ifStatement = new BRScript.IfStatement();
                ifStatement.condition = ParseExpression();
                Consume(TokenType.EOL, "Expected end of line after IF expression!");
                {
                    Token cur = Peek();
                    while ( (cur.type != TokenType.EOF) && (cur.type != TokenType.ELSEIF) && (cur.type != TokenType.ELSE) && (cur.type != TokenType.END))
                    {
                        BRScript.Stmt stmt = ParseStatement();
                        if (ifStatement.ifStatements == null)
                        {
                            ifStatement.ifStatements = new List<BRScript.Stmt>();
                        }
                        ifStatement.ifStatements.Add(stmt);
                        while(Match(TokenType.EOL))
                        {
                        }
                        cur = Peek();
                    }
                }
                while (Match(TokenType.ELSEIF))
                {
                    BRScript.IfStatement.ElseIfBlock elseIfBlock = new BRScript.IfStatement.ElseIfBlock();
                    if (ifStatement.elseIfBlocks == null)
                    {
                        ifStatement.elseIfBlocks = new List<BRScript.IfStatement.ElseIfBlock>();
                    }
                    elseIfBlock.condition = ParseExpression();
                    Consume(TokenType.EOL, "Expected end of line after ELSEIF expression!");
                    Token cur = Peek();
                    while ( (cur.type != TokenType.EOF) && (cur.type != TokenType.ELSEIF) && (cur.type != TokenType.ELSE) && (cur.type != TokenType.END))
                    {
                        BRScript.Stmt elseIfStatement = ParseStatement();
                        if (elseIfBlock.statements == null)
                        {
                            elseIfBlock.statements = new List<BRScript.Stmt>();
                        }
                        elseIfBlock.statements.Add(elseIfStatement);
                        while(Match(TokenType.EOL))
                        {
                        }
                        cur = Peek();
                    }
                    ifStatement.elseIfBlocks.Add(elseIfBlock);
                }
                if(IsAtEnd())
                {
                    throw NewBRException("Unexpected end of file in IF statement!");
                }
                if (Match(TokenType.ELSE))
                {
                    Consume(TokenType.EOL, "Expected end of line after ELSE expression!");
                    Token cur = Peek();
                    while ( (cur.type != TokenType.EOF) && (cur.type != TokenType.END))
                    {
                        BRScript.Stmt elseStatement = ParseStatement();
                        if (ifStatement.elseStatements == null)
                        {
                            ifStatement.elseStatements = new List<BRScript.Stmt>();
                        }
                        ifStatement.elseStatements.Add(elseStatement);
                        while(Match(TokenType.EOL))
                        {
                        }
                        cur = Peek();
                    }
                }
                Consume(TokenType.END, "Expected END to match IF!");
                result = ifStatement;
            }
            else if (Match(TokenType.SET))
            {
                BRScript.SetStatement setStmt = new BRScript.SetStatement();
                Token cur = Consume(TokenType.IDENTIFIER, "Expected identifier after SET command!");
                setStmt.identifier = cur.literal.ToString();
                setStmt.expression = ParseExpression();
                result = setStmt;
            }
            else if (Match(TokenType.TRIGGER))
            {
                BRScript.TriggerStatement triggerStmt = new BRScript.TriggerStatement();
                triggerStmt.argList = ParseArgList();
                if (triggerStmt.argList == null)
                {
                    throw NewBRException("Expected parameters for TRIGGER command!");
                }
                if (triggerStmt.argList.args.Count < 1)
                {
                    throw NewBRException("Expected 1 parameter in TRIGGER command!");
                }
                if (Match(TokenType.IN))
                {
                    triggerStmt.duration = ParseExpression();
                    if (triggerStmt.duration == null)
                    {
                        throw NewBRException("Expected duration expression in TRIGGER command!");
                    }
                    if (!Match(TokenType.DAYS, TokenType.TICKS, TokenType.MONTHS))
                    {
                        throw NewBRException("Expected TICKS, MONTHS or DAYS as TRIGGER duration type!");
                    }
                    triggerStmt.durationType = Previous().type;
                }
                result = triggerStmt;
            }
            else if (Match(TokenType.TITLE))
            {
                BRScript.TitleStatement titleStatement = new BRScript.TitleStatement();
                Token cur = Peek();
                if (cur.type == TokenType.STRING)
                {
                    titleStatement.text = cur.literal.ToString();
                    Advance();
                }
                else
                {
                    throw NewBRException("Unexpected token after TITLE command - expected a string!");
                }
                result = titleStatement;
            }
            else if (Check(TokenType.IDENTIFIER))
            {
                BRScript.CustomStatement customStmt = new BRScript.CustomStatement();
                customStmt.identifier = Peek().literal.ToString();
                Advance();
                customStmt.argList = ParseArgList();
                result = customStmt;
            }
            if (result == null)
            {
                return null;
            }
            BRScript.IfExpression ifExpression = ParseIfExpression();
            if (ifExpression != null)
            {
                BRScript.PostFixIfStatement ifStatement = new BRScript.PostFixIfStatement();
                ifStatement.condition = ifExpression.expression;
                ifStatement.statement = result;
                result = ifStatement;
            }
            ConsumeEndOfLineOrEndOfFile("Unexpected token after statement!");
            return result;
        }

        private BRScript.ArgList ParseArgList()
        {
            // Debug.Log("Parse ArgList");
            BRScript.ArgList result = new BRScript.ArgList();
            List<BRScript.Expr> args = null;
            Consume(TokenType.LEFT_PAREN, "Expected opening '(' after identifier!");
            bool first = true;
            while (!(Match(TokenType.RIGHT_PAREN) || IsAtEnd()))
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    Consume(TokenType.COMMA, "Expected ',' !");
                }
                if (args == null)
                {
                    args = new List<BRScript.Expr>();
                }
                args.Add(ParseExpression());
            }
            if (Previous().type != TokenType.RIGHT_PAREN)
            {
                throw NewBRException("Missing closing ')'");
            }
            result.args = args;
            return result;
        }

        private BRScript.Expr ParseExpression()
        {
            return ParseLogicalOr();
        }

        private BRScript.Expr ParseLogicalOr()
        {
            BRScript.Expr expr = ParseLogicalAnd();
            while (Match(TokenType.OR))
            {
                Token op = Previous();
                BRScript.Expr right = ParseLogicalAnd();
                expr = new BRScript.BinaryExpr(){ op = op, arg1 = expr, arg2 = right};
            }
            return expr;
        }

        private BRScript.Expr ParseLogicalAnd()
        {
            BRScript.Expr expr = ParseEquality();
            while (Match(TokenType.AND))
            {
                Token op = Previous();
                BRScript.Expr right = ParseEquality();
                expr = new BRScript.BinaryExpr(){ op = op, arg1 = expr, arg2 = right};
            }
            return expr;
        }

        private BRScript.Expr ParseEquality()
        {
            BRScript.Expr expr = ParseComparison();
            while (Match(TokenType.NE, TokenType.EQ))
            {
                Token op = Previous();
                BRScript.Expr right = ParseComparison();
                expr = new BRScript.BinaryExpr(){ op = op, arg1 = expr, arg2 = right};
            }
            return expr;
        }

        private BRScript.Expr ParseComparison()
        {
            BRScript.Expr expr = ParseAddition();
            while (Match(TokenType.GT, TokenType.GE, TokenType.LT, TokenType.LE))
            {
                Token op = Previous();
                BRScript.Expr right = ParseAddition();
                expr = new BRScript.BinaryExpr(){ op = op, arg1 = expr, arg2 = right};
            }
            return expr;
        }

        private BRScript.Expr ParseAddition()
        {
            BRScript.Expr expr = ParseMultiplication();
            while (Match(TokenType.MINUS, TokenType.PLUS))
            {
                Token op = Previous();
                BRScript.Expr right = ParseMultiplication();
                expr = new BRScript.BinaryExpr(){ op = op, arg1 = expr, arg2 = right };
            }
            return expr;
        }

        private BRScript.Expr ParseMultiplication()
        {
            BRScript.Expr expr = ParseUnary();
            while (Match(TokenType.SLASH, TokenType.STAR))
            {
                Token op = Previous();
                BRScript.Expr right = ParseUnary();
                expr = new BRScript.BinaryExpr(){ op = op, arg1 = expr, arg2 = right };
            }
            return expr;
        }

        private BRScript.Expr ParseUnary()
        {
            if (Match(TokenType.NOT, TokenType.MINUS))
            {
                Token op = Previous();
                BRScript.Expr right = ParseUnary();
                return new BRScript.UnaryExpr() { op = op, arg = right };
            }

            return ParsePrimary();
        }

        private BRScript.Expr ParsePrimary()
        {
            if (Match(TokenType.FALSE))
            {
                return new BRScript.LiteralExpr(){ literal = false };
            }
            if (Match(TokenType.TRUE))
            {
                return new BRScript.LiteralExpr(){ literal = true };
            }
            if (Match(TokenType.NIL))
            {
                 return new BRScript.LiteralExpr(){ literal = null };
            }
            if (Match(TokenType.NUMBER, TokenType.STRING))
            {
                return new BRScript.LiteralExpr(){ literal = Previous().literal };
            }
            if (Match(TokenType.OPTION))
            {
                return new BRScript.OptionExpr() { option = (int)Previous().literal };
            }
            if (Match(TokenType.STRING_COMMAND))
            {
                BRScript.StringCommandExpr stringCommand = new BRScript.StringCommandExpr();
                stringCommand.argList = ParseArgList();
                if (stringCommand.argList == null || stringCommand.argList.args.Count == 0)
                {
                    throw NewBRException("Expected arguments after STRING command!");
                }
                if (stringCommand.argList.args[0].GetType() != typeof(BRScript.VariableExpr))
                {
                    throw NewBRException("Invalid type for first argument in STRING command!");
                }
                return stringCommand;
            }
            if (Match(TokenType.CALL_LUA))
            {
                BRScript.CallLuaExpr callLuaCommand = new BRScript.CallLuaExpr();
                BRScript.ArgList argList = ParseArgList();
                if (argList == null || argList.args.Count == 0)
                {
                    throw NewBRException("Expected arguments after CALL_LUA command!");
                }
                if (argList.args[0].GetType() != typeof(BRScript.LiteralExpr))
                {
                    throw NewBRException("Invalid type for first argument in CALL_LUA command!");
                }
                callLuaCommand.lua = ((BRScript.LiteralExpr)argList.args[0]).literal.ToString();
                return callLuaCommand;
            }
            if (Match(TokenType.IDENTIFIER))
            {
                string identifier = Previous().literal.ToString();
                if (Check(TokenType.LEFT_PAREN))
                {
                    // Debug.Log("Parse CallExpr");
                    BRScript.ArgList argList = ParseArgList();
                    return new BRScript.CallExpr() { identifier = identifier, argList = argList };
                }
                else
                {
                    return new BRScript.VariableExpr() { identifier = identifier };
                }
            }
            if (Match(TokenType.LEFT_PAREN)) {
                BRScript.Expr expr = ParseExpression();
                Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
                return expr;
            }
            if (Match(TokenType.TIMEOUT))
            {
                return new BRScript.TokenExpr() { token =  Previous() };
            }
            throw NewBRException("Invalid token in expression!");
        }
    }
}
