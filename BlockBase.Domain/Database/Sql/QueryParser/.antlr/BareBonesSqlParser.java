// Generated from /home/marcia/BlockBaseMaster/node/BlockBase.Domain/Database/Sql/QueryParser/BareBonesSql.g4 by ANTLR 4.7.1
import org.antlr.v4.runtime.atn.*;
import org.antlr.v4.runtime.dfa.DFA;
import org.antlr.v4.runtime.*;
import org.antlr.v4.runtime.misc.*;
import org.antlr.v4.runtime.tree.*;
import java.util.List;
import java.util.Iterator;
import java.util.ArrayList;

@SuppressWarnings({"all", "warnings", "unchecked", "unused", "cast"})
public class BareBonesSqlParser extends Parser {
	static { RuntimeMetaData.checkVersion("4.7.1", RuntimeMetaData.VERSION); }

	protected static final DFA[] _decisionToDFA;
	protected static final PredictionContextCache _sharedContextCache =
		new PredictionContextCache();
	public static final int
		T__0=1, T__1=2, T__2=3, T__3=4, T__4=5, T__5=6, T__6=7, T__7=8, T__8=9, 
		T__9=10, T__10=11, T__11=12, T__12=13, T__13=14, T__14=15, T__15=16, K_USE=17, 
		K_CURRENT_DATABASE=18, K_LIST_DATABASES=19, K_GET_STRUCTURE=20, K_NOT_TO_ENCRYPT=21, 
		K_IF=22, K_EXECUTE=23, K_BOOL=24, K_DATETIME=25, K_DURATION=26, K_INT=27, 
		K_DECIMAL=28, K_DOUBLE=29, K_TEXT=30, K_ENCRYPTED=31, K_RANGE=32, K_ADD=33, 
		K_ALL=34, K_ALTER=35, K_AND=36, K_ASC=37, K_BY=38, K_COLUMN=39, K_CONSTRAINT=40, 
		K_CREATE=41, K_CROSS=42, K_DATABASE=43, K_DELETE=44, K_DESC=45, K_DISTINCT=46, 
		K_DROP=47, K_FROM=48, K_INNER=49, K_INSERT=50, K_INTO=51, K_JOIN=52, K_KEY=53, 
		K_LEFT=54, K_LIMIT=55, K_NATURAL=56, K_NO=57, K_NOT=58, K_NULL=59, K_OFFSET=60, 
		K_ON=61, K_OR=62, K_ORDER=63, K_OUTER=64, K_PRIMARY=65, K_REFERENCES=66, 
		K_RENAME=67, K_SELECT=68, K_SET=69, K_TABLE=70, K_TO=71, K_UNIQUE=72, 
		K_UPDATE=73, K_USING=74, K_VALUES=75, K_WHERE=76, IDENTIFIER=77, NUMERIC_LITERAL=78, 
		BIND_PARAMETER=79, STRING_LITERAL=80, UNEXPECTED_CHAR=81;
	public static final int
		RULE_parse = 0, RULE_error = 1, RULE_sql_stmt_list = 2, RULE_sql_stmt = 3, 
		RULE_use_database_stmt = 4, RULE_current_database_stmt = 5, RULE_list_databases_stmt = 6, 
		RULE_get_structure_stmt = 7, RULE_create_database_stmt = 8, RULE_drop_database_stmt = 9, 
		RULE_create_table_stmt = 10, RULE_alter_table_stmt = 11, RULE_drop_table_stmt = 12, 
		RULE_insert_stmt = 13, RULE_update_stmt = 14, RULE_delete_stmt = 15, RULE_operator = 16, 
		RULE_if_stmt = 17, RULE_simple_select_stmt = 18, RULE_select_core = 19, 
		RULE_ordering_term = 20, RULE_result_column = 21, RULE_table_or_subquery = 22, 
		RULE_join_clause = 23, RULE_join_operator = 24, RULE_join_constraint = 25, 
		RULE_column_def = 26, RULE_data_type = 27, RULE_bucket_number = 28, RULE_bucket_range = 29, 
		RULE_column_constraint = 30, RULE_expr = 31, RULE_foreign_key_clause = 32, 
		RULE_signed_number = 33, RULE_literal_value = 34, RULE_keyword = 35, RULE_name = 36, 
		RULE_table_name = 37, RULE_new_table_name = 38, RULE_column_name = 39, 
		RULE_new_column_name = 40, RULE_database_name = 41, RULE_foreign_table = 42, 
		RULE_table_column_name = 43, RULE_any_name = 44, RULE_complex_name = 45;
	public static final String[] ruleNames = {
		"parse", "error", "sql_stmt_list", "sql_stmt", "use_database_stmt", "current_database_stmt", 
		"list_databases_stmt", "get_structure_stmt", "create_database_stmt", "drop_database_stmt", 
		"create_table_stmt", "alter_table_stmt", "drop_table_stmt", "insert_stmt", 
		"update_stmt", "delete_stmt", "operator", "if_stmt", "simple_select_stmt", 
		"select_core", "ordering_term", "result_column", "table_or_subquery", 
		"join_clause", "join_operator", "join_constraint", "column_def", "data_type", 
		"bucket_number", "bucket_range", "column_constraint", "expr", "foreign_key_clause", 
		"signed_number", "literal_value", "keyword", "name", "table_name", "new_table_name", 
		"column_name", "new_column_name", "database_name", "foreign_table", "table_column_name", 
		"any_name", "complex_name"
	};

	private static final String[] _LITERAL_NAMES = {
		null, "';'", "'('", "','", "')'", "'='", "'<'", "'<='", "'>'", "'>='", 
		"'!='", "'{'", "'}'", "'.*'", "'.'", "'+'", "'-'", null, null, null, null, 
		"'!'"
	};
	private static final String[] _SYMBOLIC_NAMES = {
		null, null, null, null, null, null, null, null, null, null, null, null, 
		null, null, null, null, null, "K_USE", "K_CURRENT_DATABASE", "K_LIST_DATABASES", 
		"K_GET_STRUCTURE", "K_NOT_TO_ENCRYPT", "K_IF", "K_EXECUTE", "K_BOOL", 
		"K_DATETIME", "K_DURATION", "K_INT", "K_DECIMAL", "K_DOUBLE", "K_TEXT", 
		"K_ENCRYPTED", "K_RANGE", "K_ADD", "K_ALL", "K_ALTER", "K_AND", "K_ASC", 
		"K_BY", "K_COLUMN", "K_CONSTRAINT", "K_CREATE", "K_CROSS", "K_DATABASE", 
		"K_DELETE", "K_DESC", "K_DISTINCT", "K_DROP", "K_FROM", "K_INNER", "K_INSERT", 
		"K_INTO", "K_JOIN", "K_KEY", "K_LEFT", "K_LIMIT", "K_NATURAL", "K_NO", 
		"K_NOT", "K_NULL", "K_OFFSET", "K_ON", "K_OR", "K_ORDER", "K_OUTER", "K_PRIMARY", 
		"K_REFERENCES", "K_RENAME", "K_SELECT", "K_SET", "K_TABLE", "K_TO", "K_UNIQUE", 
		"K_UPDATE", "K_USING", "K_VALUES", "K_WHERE", "IDENTIFIER", "NUMERIC_LITERAL", 
		"BIND_PARAMETER", "STRING_LITERAL", "UNEXPECTED_CHAR"
	};
	public static final Vocabulary VOCABULARY = new VocabularyImpl(_LITERAL_NAMES, _SYMBOLIC_NAMES);

	/**
	 * @deprecated Use {@link #VOCABULARY} instead.
	 */
	@Deprecated
	public static final String[] tokenNames;
	static {
		tokenNames = new String[_SYMBOLIC_NAMES.length];
		for (int i = 0; i < tokenNames.length; i++) {
			tokenNames[i] = VOCABULARY.getLiteralName(i);
			if (tokenNames[i] == null) {
				tokenNames[i] = VOCABULARY.getSymbolicName(i);
			}

			if (tokenNames[i] == null) {
				tokenNames[i] = "<INVALID>";
			}
		}
	}

	@Override
	@Deprecated
	public String[] getTokenNames() {
		return tokenNames;
	}

	@Override

	public Vocabulary getVocabulary() {
		return VOCABULARY;
	}

	@Override
	public String getGrammarFileName() { return "BareBonesSql.g4"; }

	@Override
	public String[] getRuleNames() { return ruleNames; }

	@Override
	public String getSerializedATN() { return _serializedATN; }

	@Override
	public ATN getATN() { return _ATN; }

	public BareBonesSqlParser(TokenStream input) {
		super(input);
		_interp = new ParserATNSimulator(this,_ATN,_decisionToDFA,_sharedContextCache);
	}
	public static class ParseContext extends ParserRuleContext {
		public TerminalNode EOF() { return getToken(BareBonesSqlParser.EOF, 0); }
		public List<Sql_stmt_listContext> sql_stmt_list() {
			return getRuleContexts(Sql_stmt_listContext.class);
		}
		public Sql_stmt_listContext sql_stmt_list(int i) {
			return getRuleContext(Sql_stmt_listContext.class,i);
		}
		public List<ErrorContext> error() {
			return getRuleContexts(ErrorContext.class);
		}
		public ErrorContext error(int i) {
			return getRuleContext(ErrorContext.class,i);
		}
		public ParseContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_parse; }
	}

	public final ParseContext parse() throws RecognitionException {
		ParseContext _localctx = new ParseContext(_ctx, getState());
		enterRule(_localctx, 0, RULE_parse);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(96);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << K_USE) | (1L << K_CURRENT_DATABASE) | (1L << K_LIST_DATABASES) | (1L << K_IF) | (1L << K_ALTER) | (1L << K_CREATE) | (1L << K_DELETE) | (1L << K_DROP) | (1L << K_INSERT))) != 0) || ((((_la - 68)) & ~0x3f) == 0 && ((1L << (_la - 68)) & ((1L << (K_SELECT - 68)) | (1L << (K_UPDATE - 68)) | (1L << (UNEXPECTED_CHAR - 68)))) != 0)) {
				{
				setState(94);
				_errHandler.sync(this);
				switch (_input.LA(1)) {
				case K_USE:
				case K_CURRENT_DATABASE:
				case K_LIST_DATABASES:
				case K_IF:
				case K_ALTER:
				case K_CREATE:
				case K_DELETE:
				case K_DROP:
				case K_INSERT:
				case K_SELECT:
				case K_UPDATE:
					{
					setState(92);
					sql_stmt_list();
					}
					break;
				case UNEXPECTED_CHAR:
					{
					setState(93);
					error();
					}
					break;
				default:
					throw new NoViableAltException(this);
				}
				}
				setState(98);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(99);
			match(EOF);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class ErrorContext extends ParserRuleContext {
		public Token UNEXPECTED_CHAR;
		public TerminalNode UNEXPECTED_CHAR() { return getToken(BareBonesSqlParser.UNEXPECTED_CHAR, 0); }
		public ErrorContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_error; }
	}

	public final ErrorContext error() throws RecognitionException {
		ErrorContext _localctx = new ErrorContext(_ctx, getState());
		enterRule(_localctx, 2, RULE_error);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(101);
			((ErrorContext)_localctx).UNEXPECTED_CHAR = match(UNEXPECTED_CHAR);
			 
			     throw new System.Exception("UNEXPECTED_CHAR=" + (((ErrorContext)_localctx).UNEXPECTED_CHAR!=null?((ErrorContext)_localctx).UNEXPECTED_CHAR.getText():null)); 
			   
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Sql_stmt_listContext extends ParserRuleContext {
		public List<Sql_stmtContext> sql_stmt() {
			return getRuleContexts(Sql_stmtContext.class);
		}
		public Sql_stmtContext sql_stmt(int i) {
			return getRuleContext(Sql_stmtContext.class,i);
		}
		public Sql_stmt_listContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_sql_stmt_list; }
	}

	public final Sql_stmt_listContext sql_stmt_list() throws RecognitionException {
		Sql_stmt_listContext _localctx = new Sql_stmt_listContext(_ctx, getState());
		enterRule(_localctx, 4, RULE_sql_stmt_list);
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(104);
			sql_stmt();
			setState(105);
			match(T__0);
			setState(111);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,2,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					{
					{
					setState(106);
					sql_stmt();
					setState(107);
					match(T__0);
					}
					} 
				}
				setState(113);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,2,_ctx);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Sql_stmtContext extends ParserRuleContext {
		public Create_database_stmtContext create_database_stmt() {
			return getRuleContext(Create_database_stmtContext.class,0);
		}
		public Drop_database_stmtContext drop_database_stmt() {
			return getRuleContext(Drop_database_stmtContext.class,0);
		}
		public Alter_table_stmtContext alter_table_stmt() {
			return getRuleContext(Alter_table_stmtContext.class,0);
		}
		public Create_table_stmtContext create_table_stmt() {
			return getRuleContext(Create_table_stmtContext.class,0);
		}
		public Drop_table_stmtContext drop_table_stmt() {
			return getRuleContext(Drop_table_stmtContext.class,0);
		}
		public Insert_stmtContext insert_stmt() {
			return getRuleContext(Insert_stmtContext.class,0);
		}
		public Update_stmtContext update_stmt() {
			return getRuleContext(Update_stmtContext.class,0);
		}
		public Delete_stmtContext delete_stmt() {
			return getRuleContext(Delete_stmtContext.class,0);
		}
		public Simple_select_stmtContext simple_select_stmt() {
			return getRuleContext(Simple_select_stmtContext.class,0);
		}
		public Use_database_stmtContext use_database_stmt() {
			return getRuleContext(Use_database_stmtContext.class,0);
		}
		public Current_database_stmtContext current_database_stmt() {
			return getRuleContext(Current_database_stmtContext.class,0);
		}
		public List_databases_stmtContext list_databases_stmt() {
			return getRuleContext(List_databases_stmtContext.class,0);
		}
		public If_stmtContext if_stmt() {
			return getRuleContext(If_stmtContext.class,0);
		}
		public Sql_stmtContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_sql_stmt; }
	}

	public final Sql_stmtContext sql_stmt() throws RecognitionException {
		Sql_stmtContext _localctx = new Sql_stmtContext(_ctx, getState());
		enterRule(_localctx, 6, RULE_sql_stmt);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(127);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,3,_ctx) ) {
			case 1:
				{
				setState(114);
				create_database_stmt();
				}
				break;
			case 2:
				{
				setState(115);
				drop_database_stmt();
				}
				break;
			case 3:
				{
				setState(116);
				alter_table_stmt();
				}
				break;
			case 4:
				{
				setState(117);
				create_table_stmt();
				}
				break;
			case 5:
				{
				setState(118);
				drop_table_stmt();
				}
				break;
			case 6:
				{
				setState(119);
				insert_stmt();
				}
				break;
			case 7:
				{
				setState(120);
				update_stmt();
				}
				break;
			case 8:
				{
				setState(121);
				delete_stmt();
				}
				break;
			case 9:
				{
				setState(122);
				simple_select_stmt();
				}
				break;
			case 10:
				{
				setState(123);
				use_database_stmt();
				}
				break;
			case 11:
				{
				setState(124);
				current_database_stmt();
				}
				break;
			case 12:
				{
				setState(125);
				list_databases_stmt();
				}
				break;
			case 13:
				{
				setState(126);
				if_stmt();
				}
				break;
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Use_database_stmtContext extends ParserRuleContext {
		public TerminalNode K_USE() { return getToken(BareBonesSqlParser.K_USE, 0); }
		public Database_nameContext database_name() {
			return getRuleContext(Database_nameContext.class,0);
		}
		public Use_database_stmtContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_use_database_stmt; }
	}

	public final Use_database_stmtContext use_database_stmt() throws RecognitionException {
		Use_database_stmtContext _localctx = new Use_database_stmtContext(_ctx, getState());
		enterRule(_localctx, 8, RULE_use_database_stmt);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(129);
			match(K_USE);
			setState(130);
			database_name();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Current_database_stmtContext extends ParserRuleContext {
		public TerminalNode K_CURRENT_DATABASE() { return getToken(BareBonesSqlParser.K_CURRENT_DATABASE, 0); }
		public Current_database_stmtContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_current_database_stmt; }
	}

	public final Current_database_stmtContext current_database_stmt() throws RecognitionException {
		Current_database_stmtContext _localctx = new Current_database_stmtContext(_ctx, getState());
		enterRule(_localctx, 10, RULE_current_database_stmt);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(132);
			match(K_CURRENT_DATABASE);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class List_databases_stmtContext extends ParserRuleContext {
		public TerminalNode K_LIST_DATABASES() { return getToken(BareBonesSqlParser.K_LIST_DATABASES, 0); }
		public List_databases_stmtContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_list_databases_stmt; }
	}

	public final List_databases_stmtContext list_databases_stmt() throws RecognitionException {
		List_databases_stmtContext _localctx = new List_databases_stmtContext(_ctx, getState());
		enterRule(_localctx, 12, RULE_list_databases_stmt);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(134);
			match(K_LIST_DATABASES);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Get_structure_stmtContext extends ParserRuleContext {
		public TerminalNode K_GET_STRUCTURE() { return getToken(BareBonesSqlParser.K_GET_STRUCTURE, 0); }
		public Database_nameContext database_name() {
			return getRuleContext(Database_nameContext.class,0);
		}
		public Get_structure_stmtContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_get_structure_stmt; }
	}

	public final Get_structure_stmtContext get_structure_stmt() throws RecognitionException {
		Get_structure_stmtContext _localctx = new Get_structure_stmtContext(_ctx, getState());
		enterRule(_localctx, 14, RULE_get_structure_stmt);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(136);
			match(K_GET_STRUCTURE);
			setState(137);
			database_name();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Create_database_stmtContext extends ParserRuleContext {
		public TerminalNode K_CREATE() { return getToken(BareBonesSqlParser.K_CREATE, 0); }
		public TerminalNode K_DATABASE() { return getToken(BareBonesSqlParser.K_DATABASE, 0); }
		public Database_nameContext database_name() {
			return getRuleContext(Database_nameContext.class,0);
		}
		public Create_database_stmtContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_create_database_stmt; }
	}

	public final Create_database_stmtContext create_database_stmt() throws RecognitionException {
		Create_database_stmtContext _localctx = new Create_database_stmtContext(_ctx, getState());
		enterRule(_localctx, 16, RULE_create_database_stmt);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(139);
			match(K_CREATE);
			setState(140);
			match(K_DATABASE);
			setState(141);
			database_name();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Drop_database_stmtContext extends ParserRuleContext {
		public TerminalNode K_DROP() { return getToken(BareBonesSqlParser.K_DROP, 0); }
		public TerminalNode K_DATABASE() { return getToken(BareBonesSqlParser.K_DATABASE, 0); }
		public Database_nameContext database_name() {
			return getRuleContext(Database_nameContext.class,0);
		}
		public Drop_database_stmtContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_drop_database_stmt; }
	}

	public final Drop_database_stmtContext drop_database_stmt() throws RecognitionException {
		Drop_database_stmtContext _localctx = new Drop_database_stmtContext(_ctx, getState());
		enterRule(_localctx, 18, RULE_drop_database_stmt);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(143);
			match(K_DROP);
			setState(144);
			match(K_DATABASE);
			setState(145);
			database_name();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Create_table_stmtContext extends ParserRuleContext {
		public TerminalNode K_CREATE() { return getToken(BareBonesSqlParser.K_CREATE, 0); }
		public TerminalNode K_TABLE() { return getToken(BareBonesSqlParser.K_TABLE, 0); }
		public Table_nameContext table_name() {
			return getRuleContext(Table_nameContext.class,0);
		}
		public List<Column_defContext> column_def() {
			return getRuleContexts(Column_defContext.class);
		}
		public Column_defContext column_def(int i) {
			return getRuleContext(Column_defContext.class,i);
		}
		public Create_table_stmtContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_create_table_stmt; }
	}

	public final Create_table_stmtContext create_table_stmt() throws RecognitionException {
		Create_table_stmtContext _localctx = new Create_table_stmtContext(_ctx, getState());
		enterRule(_localctx, 20, RULE_create_table_stmt);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(147);
			match(K_CREATE);
			setState(148);
			match(K_TABLE);
			setState(149);
			table_name();
			{
			setState(150);
			match(T__1);
			setState(151);
			column_def();
			setState(156);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__2) {
				{
				{
				setState(152);
				match(T__2);
				setState(153);
				column_def();
				}
				}
				setState(158);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(159);
			match(T__3);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Alter_table_stmtContext extends ParserRuleContext {
		public TerminalNode K_ALTER() { return getToken(BareBonesSqlParser.K_ALTER, 0); }
		public TerminalNode K_TABLE() { return getToken(BareBonesSqlParser.K_TABLE, 0); }
		public Table_nameContext table_name() {
			return getRuleContext(Table_nameContext.class,0);
		}
		public TerminalNode K_RENAME() { return getToken(BareBonesSqlParser.K_RENAME, 0); }
		public TerminalNode K_TO() { return getToken(BareBonesSqlParser.K_TO, 0); }
		public New_table_nameContext new_table_name() {
			return getRuleContext(New_table_nameContext.class,0);
		}
		public TerminalNode K_ADD() { return getToken(BareBonesSqlParser.K_ADD, 0); }
		public TerminalNode K_COLUMN() { return getToken(BareBonesSqlParser.K_COLUMN, 0); }
		public Column_defContext column_def() {
			return getRuleContext(Column_defContext.class,0);
		}
		public TerminalNode K_DROP() { return getToken(BareBonesSqlParser.K_DROP, 0); }
		public Column_nameContext column_name() {
			return getRuleContext(Column_nameContext.class,0);
		}
		public New_column_nameContext new_column_name() {
			return getRuleContext(New_column_nameContext.class,0);
		}
		public Alter_table_stmtContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_alter_table_stmt; }
	}

	public final Alter_table_stmtContext alter_table_stmt() throws RecognitionException {
		Alter_table_stmtContext _localctx = new Alter_table_stmtContext(_ctx, getState());
		enterRule(_localctx, 22, RULE_alter_table_stmt);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(161);
			match(K_ALTER);
			setState(162);
			match(K_TABLE);
			setState(163);
			table_name();
			setState(178);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,5,_ctx) ) {
			case 1:
				{
				setState(164);
				match(K_RENAME);
				setState(165);
				match(K_TO);
				setState(166);
				new_table_name();
				}
				break;
			case 2:
				{
				setState(167);
				match(K_ADD);
				setState(168);
				match(K_COLUMN);
				setState(169);
				column_def();
				}
				break;
			case 3:
				{
				setState(170);
				match(K_DROP);
				setState(171);
				match(K_COLUMN);
				setState(172);
				column_name();
				}
				break;
			case 4:
				{
				setState(173);
				match(K_RENAME);
				setState(174);
				column_name();
				setState(175);
				match(K_TO);
				setState(176);
				new_column_name();
				}
				break;
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Drop_table_stmtContext extends ParserRuleContext {
		public TerminalNode K_DROP() { return getToken(BareBonesSqlParser.K_DROP, 0); }
		public TerminalNode K_TABLE() { return getToken(BareBonesSqlParser.K_TABLE, 0); }
		public Table_nameContext table_name() {
			return getRuleContext(Table_nameContext.class,0);
		}
		public Drop_table_stmtContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_drop_table_stmt; }
	}

	public final Drop_table_stmtContext drop_table_stmt() throws RecognitionException {
		Drop_table_stmtContext _localctx = new Drop_table_stmtContext(_ctx, getState());
		enterRule(_localctx, 24, RULE_drop_table_stmt);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(180);
			match(K_DROP);
			setState(181);
			match(K_TABLE);
			setState(182);
			table_name();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Insert_stmtContext extends ParserRuleContext {
		public TerminalNode K_INSERT() { return getToken(BareBonesSqlParser.K_INSERT, 0); }
		public TerminalNode K_INTO() { return getToken(BareBonesSqlParser.K_INTO, 0); }
		public Table_nameContext table_name() {
			return getRuleContext(Table_nameContext.class,0);
		}
		public TerminalNode K_VALUES() { return getToken(BareBonesSqlParser.K_VALUES, 0); }
		public List<Literal_valueContext> literal_value() {
			return getRuleContexts(Literal_valueContext.class);
		}
		public Literal_valueContext literal_value(int i) {
			return getRuleContext(Literal_valueContext.class,i);
		}
		public List<Column_nameContext> column_name() {
			return getRuleContexts(Column_nameContext.class);
		}
		public Column_nameContext column_name(int i) {
			return getRuleContext(Column_nameContext.class,i);
		}
		public Insert_stmtContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_insert_stmt; }
	}

	public final Insert_stmtContext insert_stmt() throws RecognitionException {
		Insert_stmtContext _localctx = new Insert_stmtContext(_ctx, getState());
		enterRule(_localctx, 26, RULE_insert_stmt);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(184);
			match(K_INSERT);
			setState(185);
			match(K_INTO);
			setState(186);
			table_name();
			setState(198);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==T__1) {
				{
				setState(187);
				match(T__1);
				setState(188);
				column_name();
				setState(193);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==T__2) {
					{
					{
					setState(189);
					match(T__2);
					setState(190);
					column_name();
					}
					}
					setState(195);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(196);
				match(T__3);
				}
			}

			{
			setState(200);
			match(K_VALUES);
			setState(201);
			match(T__1);
			setState(202);
			literal_value();
			setState(207);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__2) {
				{
				{
				setState(203);
				match(T__2);
				setState(204);
				literal_value();
				}
				}
				setState(209);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(210);
			match(T__3);
			setState(225);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__2) {
				{
				{
				setState(211);
				match(T__2);
				setState(212);
				match(T__1);
				setState(213);
				literal_value();
				setState(218);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==T__2) {
					{
					{
					setState(214);
					match(T__2);
					setState(215);
					literal_value();
					}
					}
					setState(220);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(221);
				match(T__3);
				}
				}
				setState(227);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Update_stmtContext extends ParserRuleContext {
		public TerminalNode K_UPDATE() { return getToken(BareBonesSqlParser.K_UPDATE, 0); }
		public Table_nameContext table_name() {
			return getRuleContext(Table_nameContext.class,0);
		}
		public TerminalNode K_SET() { return getToken(BareBonesSqlParser.K_SET, 0); }
		public List<Column_nameContext> column_name() {
			return getRuleContexts(Column_nameContext.class);
		}
		public Column_nameContext column_name(int i) {
			return getRuleContext(Column_nameContext.class,i);
		}
		public List<Literal_valueContext> literal_value() {
			return getRuleContexts(Literal_valueContext.class);
		}
		public Literal_valueContext literal_value(int i) {
			return getRuleContext(Literal_valueContext.class,i);
		}
		public TerminalNode K_WHERE() { return getToken(BareBonesSqlParser.K_WHERE, 0); }
		public ExprContext expr() {
			return getRuleContext(ExprContext.class,0);
		}
		public Update_stmtContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_update_stmt; }
	}

	public final Update_stmtContext update_stmt() throws RecognitionException {
		Update_stmtContext _localctx = new Update_stmtContext(_ctx, getState());
		enterRule(_localctx, 28, RULE_update_stmt);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(228);
			match(K_UPDATE);
			setState(229);
			table_name();
			setState(230);
			match(K_SET);
			setState(231);
			column_name();
			setState(232);
			match(T__4);
			setState(233);
			literal_value();
			setState(241);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__2) {
				{
				{
				setState(234);
				match(T__2);
				setState(235);
				column_name();
				setState(236);
				match(T__4);
				setState(237);
				literal_value();
				}
				}
				setState(243);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(246);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_WHERE) {
				{
				setState(244);
				match(K_WHERE);
				setState(245);
				expr(0);
				}
			}

			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Delete_stmtContext extends ParserRuleContext {
		public TerminalNode K_DELETE() { return getToken(BareBonesSqlParser.K_DELETE, 0); }
		public TerminalNode K_FROM() { return getToken(BareBonesSqlParser.K_FROM, 0); }
		public Table_nameContext table_name() {
			return getRuleContext(Table_nameContext.class,0);
		}
		public TerminalNode K_WHERE() { return getToken(BareBonesSqlParser.K_WHERE, 0); }
		public ExprContext expr() {
			return getRuleContext(ExprContext.class,0);
		}
		public Delete_stmtContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_delete_stmt; }
	}

	public final Delete_stmtContext delete_stmt() throws RecognitionException {
		Delete_stmtContext _localctx = new Delete_stmtContext(_ctx, getState());
		enterRule(_localctx, 30, RULE_delete_stmt);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(248);
			match(K_DELETE);
			setState(249);
			match(K_FROM);
			setState(250);
			table_name();
			setState(253);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_WHERE) {
				{
				setState(251);
				match(K_WHERE);
				setState(252);
				expr(0);
				}
			}

			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class OperatorContext extends ParserRuleContext {
		public OperatorContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_operator; }
	}

	public final OperatorContext operator() throws RecognitionException {
		OperatorContext _localctx = new OperatorContext(_ctx, getState());
		enterRule(_localctx, 32, RULE_operator);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(255);
			_la = _input.LA(1);
			if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << T__4) | (1L << T__5) | (1L << T__6) | (1L << T__7) | (1L << T__8) | (1L << T__9))) != 0)) ) {
			_errHandler.recoverInline(this);
			}
			else {
				if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
				_errHandler.reportMatch(this);
				consume();
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class If_stmtContext extends ParserRuleContext {
		public TerminalNode K_IF() { return getToken(BareBonesSqlParser.K_IF, 0); }
		public Simple_select_stmtContext simple_select_stmt() {
			return getRuleContext(Simple_select_stmtContext.class,0);
		}
		public TerminalNode K_EXECUTE() { return getToken(BareBonesSqlParser.K_EXECUTE, 0); }
		public List<Sql_stmtContext> sql_stmt() {
			return getRuleContexts(Sql_stmtContext.class);
		}
		public Sql_stmtContext sql_stmt(int i) {
			return getRuleContext(Sql_stmtContext.class,i);
		}
		public If_stmtContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_if_stmt; }
	}

	public final If_stmtContext if_stmt() throws RecognitionException {
		If_stmtContext _localctx = new If_stmtContext(_ctx, getState());
		enterRule(_localctx, 34, RULE_if_stmt);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(257);
			match(K_IF);
			setState(258);
			simple_select_stmt();
			setState(259);
			match(K_EXECUTE);
			setState(260);
			match(T__10);
			setState(261);
			sql_stmt();
			setState(262);
			match(T__0);
			setState(268);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (((((_la - 17)) & ~0x3f) == 0 && ((1L << (_la - 17)) & ((1L << (K_USE - 17)) | (1L << (K_CURRENT_DATABASE - 17)) | (1L << (K_LIST_DATABASES - 17)) | (1L << (K_IF - 17)) | (1L << (K_ALTER - 17)) | (1L << (K_CREATE - 17)) | (1L << (K_DELETE - 17)) | (1L << (K_DROP - 17)) | (1L << (K_INSERT - 17)) | (1L << (K_SELECT - 17)) | (1L << (K_UPDATE - 17)))) != 0)) {
				{
				{
				setState(263);
				sql_stmt();
				setState(264);
				match(T__0);
				}
				}
				setState(270);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(271);
			match(T__11);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Simple_select_stmtContext extends ParserRuleContext {
		public Select_coreContext select_core() {
			return getRuleContext(Select_coreContext.class,0);
		}
		public TerminalNode K_ORDER() { return getToken(BareBonesSqlParser.K_ORDER, 0); }
		public TerminalNode K_BY() { return getToken(BareBonesSqlParser.K_BY, 0); }
		public List<Ordering_termContext> ordering_term() {
			return getRuleContexts(Ordering_termContext.class);
		}
		public Ordering_termContext ordering_term(int i) {
			return getRuleContext(Ordering_termContext.class,i);
		}
		public TerminalNode K_LIMIT() { return getToken(BareBonesSqlParser.K_LIMIT, 0); }
		public List<Literal_valueContext> literal_value() {
			return getRuleContexts(Literal_valueContext.class);
		}
		public Literal_valueContext literal_value(int i) {
			return getRuleContext(Literal_valueContext.class,i);
		}
		public TerminalNode K_OFFSET() { return getToken(BareBonesSqlParser.K_OFFSET, 0); }
		public Simple_select_stmtContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_simple_select_stmt; }
	}

	public final Simple_select_stmtContext simple_select_stmt() throws RecognitionException {
		Simple_select_stmtContext _localctx = new Simple_select_stmtContext(_ctx, getState());
		enterRule(_localctx, 36, RULE_simple_select_stmt);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(273);
			select_core();
			setState(284);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_ORDER) {
				{
				setState(274);
				match(K_ORDER);
				setState(275);
				match(K_BY);
				setState(276);
				ordering_term();
				setState(281);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==T__2) {
					{
					{
					setState(277);
					match(T__2);
					setState(278);
					ordering_term();
					}
					}
					setState(283);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				}
			}

			setState(292);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_LIMIT) {
				{
				setState(286);
				match(K_LIMIT);
				setState(287);
				literal_value();
				setState(290);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if (_la==K_OFFSET) {
					{
					setState(288);
					match(K_OFFSET);
					setState(289);
					literal_value();
					}
				}

				}
			}

			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Select_coreContext extends ParserRuleContext {
		public TerminalNode K_SELECT() { return getToken(BareBonesSqlParser.K_SELECT, 0); }
		public List<Result_columnContext> result_column() {
			return getRuleContexts(Result_columnContext.class);
		}
		public Result_columnContext result_column(int i) {
			return getRuleContext(Result_columnContext.class,i);
		}
		public TerminalNode K_FROM() { return getToken(BareBonesSqlParser.K_FROM, 0); }
		public List<Table_or_subqueryContext> table_or_subquery() {
			return getRuleContexts(Table_or_subqueryContext.class);
		}
		public Table_or_subqueryContext table_or_subquery(int i) {
			return getRuleContext(Table_or_subqueryContext.class,i);
		}
		public Join_clauseContext join_clause() {
			return getRuleContext(Join_clauseContext.class,0);
		}
		public TerminalNode K_DISTINCT() { return getToken(BareBonesSqlParser.K_DISTINCT, 0); }
		public TerminalNode K_WHERE() { return getToken(BareBonesSqlParser.K_WHERE, 0); }
		public ExprContext expr() {
			return getRuleContext(ExprContext.class,0);
		}
		public TerminalNode K_ENCRYPTED() { return getToken(BareBonesSqlParser.K_ENCRYPTED, 0); }
		public Select_coreContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_select_core; }
	}

	public final Select_coreContext select_core() throws RecognitionException {
		Select_coreContext _localctx = new Select_coreContext(_ctx, getState());
		enterRule(_localctx, 38, RULE_select_core);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(294);
			match(K_SELECT);
			setState(296);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,19,_ctx) ) {
			case 1:
				{
				setState(295);
				match(K_DISTINCT);
				}
				break;
			}
			setState(298);
			result_column();
			setState(303);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__2) {
				{
				{
				setState(299);
				match(T__2);
				setState(300);
				result_column();
				}
				}
				setState(305);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(306);
			match(K_FROM);
			setState(316);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,22,_ctx) ) {
			case 1:
				{
				setState(307);
				table_or_subquery();
				setState(312);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==T__2) {
					{
					{
					setState(308);
					match(T__2);
					setState(309);
					table_or_subquery();
					}
					}
					setState(314);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				}
				break;
			case 2:
				{
				setState(315);
				join_clause();
				}
				break;
			}
			setState(321);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case K_WHERE:
				{
				setState(318);
				match(K_WHERE);
				setState(319);
				expr(0);
				}
				break;
			case K_ENCRYPTED:
				{
				setState(320);
				match(K_ENCRYPTED);
				}
				break;
			case T__0:
			case T__3:
			case K_EXECUTE:
			case K_LIMIT:
			case K_ORDER:
				break;
			default:
				break;
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Ordering_termContext extends ParserRuleContext {
		public ExprContext expr() {
			return getRuleContext(ExprContext.class,0);
		}
		public TerminalNode K_ASC() { return getToken(BareBonesSqlParser.K_ASC, 0); }
		public TerminalNode K_DESC() { return getToken(BareBonesSqlParser.K_DESC, 0); }
		public Ordering_termContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_ordering_term; }
	}

	public final Ordering_termContext ordering_term() throws RecognitionException {
		Ordering_termContext _localctx = new Ordering_termContext(_ctx, getState());
		enterRule(_localctx, 40, RULE_ordering_term);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(323);
			expr(0);
			setState(325);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_ASC || _la==K_DESC) {
				{
				setState(324);
				_la = _input.LA(1);
				if ( !(_la==K_ASC || _la==K_DESC) ) {
				_errHandler.recoverInline(this);
				}
				else {
					if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
					_errHandler.reportMatch(this);
					consume();
				}
				}
			}

			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Result_columnContext extends ParserRuleContext {
		public Table_nameContext table_name() {
			return getRuleContext(Table_nameContext.class,0);
		}
		public Table_column_nameContext table_column_name() {
			return getRuleContext(Table_column_nameContext.class,0);
		}
		public Result_columnContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_result_column; }
	}

	public final Result_columnContext result_column() throws RecognitionException {
		Result_columnContext _localctx = new Result_columnContext(_ctx, getState());
		enterRule(_localctx, 42, RULE_result_column);
		try {
			setState(331);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,25,_ctx) ) {
			case 1:
				enterOuterAlt(_localctx, 1);
				{
				setState(327);
				table_name();
				setState(328);
				match(T__12);
				}
				break;
			case 2:
				enterOuterAlt(_localctx, 2);
				{
				setState(330);
				table_column_name();
				}
				break;
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Table_or_subqueryContext extends ParserRuleContext {
		public Table_nameContext table_name() {
			return getRuleContext(Table_nameContext.class,0);
		}
		public List<Table_or_subqueryContext> table_or_subquery() {
			return getRuleContexts(Table_or_subqueryContext.class);
		}
		public Table_or_subqueryContext table_or_subquery(int i) {
			return getRuleContext(Table_or_subqueryContext.class,i);
		}
		public Join_clauseContext join_clause() {
			return getRuleContext(Join_clauseContext.class,0);
		}
		public Simple_select_stmtContext simple_select_stmt() {
			return getRuleContext(Simple_select_stmtContext.class,0);
		}
		public Table_or_subqueryContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_table_or_subquery; }
	}

	public final Table_or_subqueryContext table_or_subquery() throws RecognitionException {
		Table_or_subqueryContext _localctx = new Table_or_subqueryContext(_ctx, getState());
		enterRule(_localctx, 44, RULE_table_or_subquery);
		int _la;
		try {
			setState(352);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,28,_ctx) ) {
			case 1:
				enterOuterAlt(_localctx, 1);
				{
				setState(333);
				table_name();
				}
				break;
			case 2:
				enterOuterAlt(_localctx, 2);
				{
				setState(334);
				match(T__1);
				setState(344);
				_errHandler.sync(this);
				switch ( getInterpreter().adaptivePredict(_input,27,_ctx) ) {
				case 1:
					{
					setState(335);
					table_or_subquery();
					setState(340);
					_errHandler.sync(this);
					_la = _input.LA(1);
					while (_la==T__2) {
						{
						{
						setState(336);
						match(T__2);
						setState(337);
						table_or_subquery();
						}
						}
						setState(342);
						_errHandler.sync(this);
						_la = _input.LA(1);
					}
					}
					break;
				case 2:
					{
					setState(343);
					join_clause();
					}
					break;
				}
				setState(346);
				match(T__3);
				}
				break;
			case 3:
				enterOuterAlt(_localctx, 3);
				{
				setState(348);
				match(T__1);
				setState(349);
				simple_select_stmt();
				setState(350);
				match(T__3);
				}
				break;
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Join_clauseContext extends ParserRuleContext {
		public List<Table_or_subqueryContext> table_or_subquery() {
			return getRuleContexts(Table_or_subqueryContext.class);
		}
		public Table_or_subqueryContext table_or_subquery(int i) {
			return getRuleContext(Table_or_subqueryContext.class,i);
		}
		public List<Join_operatorContext> join_operator() {
			return getRuleContexts(Join_operatorContext.class);
		}
		public Join_operatorContext join_operator(int i) {
			return getRuleContext(Join_operatorContext.class,i);
		}
		public List<Join_constraintContext> join_constraint() {
			return getRuleContexts(Join_constraintContext.class);
		}
		public Join_constraintContext join_constraint(int i) {
			return getRuleContext(Join_constraintContext.class,i);
		}
		public Join_clauseContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_join_clause; }
	}

	public final Join_clauseContext join_clause() throws RecognitionException {
		Join_clauseContext _localctx = new Join_clauseContext(_ctx, getState());
		enterRule(_localctx, 46, RULE_join_clause);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(354);
			table_or_subquery();
			setState(361);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << T__2) | (1L << K_CROSS) | (1L << K_INNER) | (1L << K_JOIN) | (1L << K_LEFT) | (1L << K_NATURAL))) != 0)) {
				{
				{
				setState(355);
				join_operator();
				setState(356);
				table_or_subquery();
				setState(357);
				join_constraint();
				}
				}
				setState(363);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Join_operatorContext extends ParserRuleContext {
		public TerminalNode K_JOIN() { return getToken(BareBonesSqlParser.K_JOIN, 0); }
		public TerminalNode K_NATURAL() { return getToken(BareBonesSqlParser.K_NATURAL, 0); }
		public TerminalNode K_LEFT() { return getToken(BareBonesSqlParser.K_LEFT, 0); }
		public TerminalNode K_INNER() { return getToken(BareBonesSqlParser.K_INNER, 0); }
		public TerminalNode K_CROSS() { return getToken(BareBonesSqlParser.K_CROSS, 0); }
		public TerminalNode K_OUTER() { return getToken(BareBonesSqlParser.K_OUTER, 0); }
		public Join_operatorContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_join_operator; }
	}

	public final Join_operatorContext join_operator() throws RecognitionException {
		Join_operatorContext _localctx = new Join_operatorContext(_ctx, getState());
		enterRule(_localctx, 48, RULE_join_operator);
		int _la;
		try {
			setState(377);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case T__2:
				enterOuterAlt(_localctx, 1);
				{
				setState(364);
				match(T__2);
				}
				break;
			case K_CROSS:
			case K_INNER:
			case K_JOIN:
			case K_LEFT:
			case K_NATURAL:
				enterOuterAlt(_localctx, 2);
				{
				setState(366);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if (_la==K_NATURAL) {
					{
					setState(365);
					match(K_NATURAL);
					}
				}

				setState(374);
				_errHandler.sync(this);
				switch (_input.LA(1)) {
				case K_LEFT:
					{
					setState(368);
					match(K_LEFT);
					setState(370);
					_errHandler.sync(this);
					_la = _input.LA(1);
					if (_la==K_OUTER) {
						{
						setState(369);
						match(K_OUTER);
						}
					}

					}
					break;
				case K_INNER:
					{
					setState(372);
					match(K_INNER);
					}
					break;
				case K_CROSS:
					{
					setState(373);
					match(K_CROSS);
					}
					break;
				case K_JOIN:
					break;
				default:
					break;
				}
				setState(376);
				match(K_JOIN);
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Join_constraintContext extends ParserRuleContext {
		public TerminalNode K_ON() { return getToken(BareBonesSqlParser.K_ON, 0); }
		public ExprContext expr() {
			return getRuleContext(ExprContext.class,0);
		}
		public Join_constraintContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_join_constraint; }
	}

	public final Join_constraintContext join_constraint() throws RecognitionException {
		Join_constraintContext _localctx = new Join_constraintContext(_ctx, getState());
		enterRule(_localctx, 50, RULE_join_constraint);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(381);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_ON) {
				{
				setState(379);
				match(K_ON);
				setState(380);
				expr(0);
				}
			}

			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Column_defContext extends ParserRuleContext {
		public Column_nameContext column_name() {
			return getRuleContext(Column_nameContext.class,0);
		}
		public Data_typeContext data_type() {
			return getRuleContext(Data_typeContext.class,0);
		}
		public List<Column_constraintContext> column_constraint() {
			return getRuleContexts(Column_constraintContext.class);
		}
		public Column_constraintContext column_constraint(int i) {
			return getRuleContext(Column_constraintContext.class,i);
		}
		public Column_defContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_column_def; }
	}

	public final Column_defContext column_def() throws RecognitionException {
		Column_defContext _localctx = new Column_defContext(_ctx, getState());
		enterRule(_localctx, 52, RULE_column_def);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(383);
			column_name();
			setState(384);
			data_type();
			setState(388);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (((((_la - 40)) & ~0x3f) == 0 && ((1L << (_la - 40)) & ((1L << (K_CONSTRAINT - 40)) | (1L << (K_NOT - 40)) | (1L << (K_NULL - 40)) | (1L << (K_PRIMARY - 40)) | (1L << (K_REFERENCES - 40)) | (1L << (K_UNIQUE - 40)))) != 0)) {
				{
				{
				setState(385);
				column_constraint();
				}
				}
				setState(390);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Data_typeContext extends ParserRuleContext {
		public TerminalNode K_BOOL() { return getToken(BareBonesSqlParser.K_BOOL, 0); }
		public TerminalNode K_DATETIME() { return getToken(BareBonesSqlParser.K_DATETIME, 0); }
		public TerminalNode K_DURATION() { return getToken(BareBonesSqlParser.K_DURATION, 0); }
		public TerminalNode K_INT() { return getToken(BareBonesSqlParser.K_INT, 0); }
		public TerminalNode K_DECIMAL() { return getToken(BareBonesSqlParser.K_DECIMAL, 0); }
		public TerminalNode K_DOUBLE() { return getToken(BareBonesSqlParser.K_DOUBLE, 0); }
		public TerminalNode K_TEXT() { return getToken(BareBonesSqlParser.K_TEXT, 0); }
		public TerminalNode K_ENCRYPTED() { return getToken(BareBonesSqlParser.K_ENCRYPTED, 0); }
		public Bucket_numberContext bucket_number() {
			return getRuleContext(Bucket_numberContext.class,0);
		}
		public TerminalNode K_RANGE() { return getToken(BareBonesSqlParser.K_RANGE, 0); }
		public Bucket_rangeContext bucket_range() {
			return getRuleContext(Bucket_rangeContext.class,0);
		}
		public Data_typeContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_data_type; }
	}

	public final Data_typeContext data_type() throws RecognitionException {
		Data_typeContext _localctx = new Data_typeContext(_ctx, getState());
		enterRule(_localctx, 54, RULE_data_type);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(406);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case K_BOOL:
				{
				setState(391);
				match(K_BOOL);
				}
				break;
			case K_DATETIME:
				{
				setState(392);
				match(K_DATETIME);
				}
				break;
			case K_DURATION:
				{
				setState(393);
				match(K_DURATION);
				}
				break;
			case K_INT:
				{
				setState(394);
				match(K_INT);
				}
				break;
			case K_DECIMAL:
				{
				setState(395);
				match(K_DECIMAL);
				}
				break;
			case K_DOUBLE:
				{
				setState(396);
				match(K_DOUBLE);
				}
				break;
			case K_TEXT:
				{
				setState(397);
				match(K_TEXT);
				}
				break;
			case K_ENCRYPTED:
				{
				setState(398);
				match(K_ENCRYPTED);
				setState(400);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if (_la==NUMERIC_LITERAL) {
					{
					setState(399);
					bucket_number();
					}
				}

				setState(404);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if (_la==K_RANGE) {
					{
					setState(402);
					match(K_RANGE);
					setState(403);
					bucket_range();
					}
				}

				}
				break;
			default:
				throw new NoViableAltException(this);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Bucket_numberContext extends ParserRuleContext {
		public TerminalNode NUMERIC_LITERAL() { return getToken(BareBonesSqlParser.NUMERIC_LITERAL, 0); }
		public Bucket_numberContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_bucket_number; }
	}

	public final Bucket_numberContext bucket_number() throws RecognitionException {
		Bucket_numberContext _localctx = new Bucket_numberContext(_ctx, getState());
		enterRule(_localctx, 56, RULE_bucket_number);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(408);
			match(NUMERIC_LITERAL);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Bucket_rangeContext extends ParserRuleContext {
		public List<TerminalNode> NUMERIC_LITERAL() { return getTokens(BareBonesSqlParser.NUMERIC_LITERAL); }
		public TerminalNode NUMERIC_LITERAL(int i) {
			return getToken(BareBonesSqlParser.NUMERIC_LITERAL, i);
		}
		public Bucket_rangeContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_bucket_range; }
	}

	public final Bucket_rangeContext bucket_range() throws RecognitionException {
		Bucket_rangeContext _localctx = new Bucket_rangeContext(_ctx, getState());
		enterRule(_localctx, 58, RULE_bucket_range);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(410);
			match(T__1);
			setState(411);
			match(NUMERIC_LITERAL);
			setState(412);
			match(T__2);
			setState(413);
			match(NUMERIC_LITERAL);
			setState(414);
			match(T__2);
			setState(415);
			match(NUMERIC_LITERAL);
			setState(416);
			match(T__3);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Column_constraintContext extends ParserRuleContext {
		public TerminalNode K_PRIMARY() { return getToken(BareBonesSqlParser.K_PRIMARY, 0); }
		public TerminalNode K_KEY() { return getToken(BareBonesSqlParser.K_KEY, 0); }
		public TerminalNode K_NULL() { return getToken(BareBonesSqlParser.K_NULL, 0); }
		public TerminalNode K_UNIQUE() { return getToken(BareBonesSqlParser.K_UNIQUE, 0); }
		public Foreign_key_clauseContext foreign_key_clause() {
			return getRuleContext(Foreign_key_clauseContext.class,0);
		}
		public TerminalNode K_CONSTRAINT() { return getToken(BareBonesSqlParser.K_CONSTRAINT, 0); }
		public NameContext name() {
			return getRuleContext(NameContext.class,0);
		}
		public TerminalNode K_NOT() { return getToken(BareBonesSqlParser.K_NOT, 0); }
		public Column_constraintContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_column_constraint; }
	}

	public final Column_constraintContext column_constraint() throws RecognitionException {
		Column_constraintContext _localctx = new Column_constraintContext(_ctx, getState());
		enterRule(_localctx, 60, RULE_column_constraint);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(420);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_CONSTRAINT) {
				{
				setState(418);
				match(K_CONSTRAINT);
				setState(419);
				name();
				}
			}

			setState(430);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case K_PRIMARY:
				{
				setState(422);
				match(K_PRIMARY);
				setState(423);
				match(K_KEY);
				}
				break;
			case K_NOT:
			case K_NULL:
				{
				setState(425);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if (_la==K_NOT) {
					{
					setState(424);
					match(K_NOT);
					}
				}

				setState(427);
				match(K_NULL);
				}
				break;
			case K_UNIQUE:
				{
				setState(428);
				match(K_UNIQUE);
				}
				break;
			case K_REFERENCES:
				{
				setState(429);
				foreign_key_clause();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class ExprContext extends ParserRuleContext {
		public Table_nameContext table_name() {
			return getRuleContext(Table_nameContext.class,0);
		}
		public Column_nameContext column_name() {
			return getRuleContext(Column_nameContext.class,0);
		}
		public OperatorContext operator() {
			return getRuleContext(OperatorContext.class,0);
		}
		public Literal_valueContext literal_value() {
			return getRuleContext(Literal_valueContext.class,0);
		}
		public List<Table_column_nameContext> table_column_name() {
			return getRuleContexts(Table_column_nameContext.class);
		}
		public Table_column_nameContext table_column_name(int i) {
			return getRuleContext(Table_column_nameContext.class,i);
		}
		public List<ExprContext> expr() {
			return getRuleContexts(ExprContext.class);
		}
		public ExprContext expr(int i) {
			return getRuleContext(ExprContext.class,i);
		}
		public TerminalNode K_AND() { return getToken(BareBonesSqlParser.K_AND, 0); }
		public TerminalNode K_OR() { return getToken(BareBonesSqlParser.K_OR, 0); }
		public ExprContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_expr; }
	}

	public final ExprContext expr() throws RecognitionException {
		return expr(0);
	}

	private ExprContext expr(int _p) throws RecognitionException {
		ParserRuleContext _parentctx = _ctx;
		int _parentState = getState();
		ExprContext _localctx = new ExprContext(_ctx, _parentState);
		ExprContext _prevctx = _localctx;
		int _startState = 62;
		enterRecursionRule(_localctx, 62, RULE_expr, _p);
		int _la;
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(447);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,42,_ctx) ) {
			case 1:
				{
				setState(433);
				table_name();
				setState(434);
				match(T__13);
				setState(435);
				column_name();
				setState(436);
				operator();
				setState(437);
				literal_value();
				}
				break;
			case 2:
				{
				setState(439);
				table_column_name();
				setState(440);
				operator();
				setState(441);
				table_column_name();
				}
				break;
			case 3:
				{
				setState(443);
				match(T__1);
				setState(444);
				expr(0);
				setState(445);
				match(T__3);
				}
				break;
			}
			_ctx.stop = _input.LT(-1);
			setState(454);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,43,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					if ( _parseListeners!=null ) triggerExitRuleEvent();
					_prevctx = _localctx;
					{
					{
					_localctx = new ExprContext(_parentctx, _parentState);
					pushNewRecursionContext(_localctx, _startState, RULE_expr);
					setState(449);
					if (!(precpred(_ctx, 2))) throw new FailedPredicateException(this, "precpred(_ctx, 2)");
					setState(450);
					_la = _input.LA(1);
					if ( !(_la==K_AND || _la==K_OR) ) {
					_errHandler.recoverInline(this);
					}
					else {
						if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
						_errHandler.reportMatch(this);
						consume();
					}
					setState(451);
					expr(3);
					}
					} 
				}
				setState(456);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,43,_ctx);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			unrollRecursionContexts(_parentctx);
		}
		return _localctx;
	}

	public static class Foreign_key_clauseContext extends ParserRuleContext {
		public TerminalNode K_REFERENCES() { return getToken(BareBonesSqlParser.K_REFERENCES, 0); }
		public Foreign_tableContext foreign_table() {
			return getRuleContext(Foreign_tableContext.class,0);
		}
		public List<Column_nameContext> column_name() {
			return getRuleContexts(Column_nameContext.class);
		}
		public Column_nameContext column_name(int i) {
			return getRuleContext(Column_nameContext.class,i);
		}
		public Foreign_key_clauseContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_foreign_key_clause; }
	}

	public final Foreign_key_clauseContext foreign_key_clause() throws RecognitionException {
		Foreign_key_clauseContext _localctx = new Foreign_key_clauseContext(_ctx, getState());
		enterRule(_localctx, 64, RULE_foreign_key_clause);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(457);
			match(K_REFERENCES);
			setState(458);
			foreign_table();
			setState(470);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==T__1) {
				{
				setState(459);
				match(T__1);
				setState(460);
				column_name();
				setState(465);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==T__2) {
					{
					{
					setState(461);
					match(T__2);
					setState(462);
					column_name();
					}
					}
					setState(467);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(468);
				match(T__3);
				}
			}

			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Signed_numberContext extends ParserRuleContext {
		public TerminalNode NUMERIC_LITERAL() { return getToken(BareBonesSqlParser.NUMERIC_LITERAL, 0); }
		public Signed_numberContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_signed_number; }
	}

	public final Signed_numberContext signed_number() throws RecognitionException {
		Signed_numberContext _localctx = new Signed_numberContext(_ctx, getState());
		enterRule(_localctx, 66, RULE_signed_number);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(473);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==T__14 || _la==T__15) {
				{
				setState(472);
				_la = _input.LA(1);
				if ( !(_la==T__14 || _la==T__15) ) {
				_errHandler.recoverInline(this);
				}
				else {
					if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
					_errHandler.reportMatch(this);
					consume();
				}
				}
			}

			setState(475);
			match(NUMERIC_LITERAL);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Literal_valueContext extends ParserRuleContext {
		public TerminalNode NUMERIC_LITERAL() { return getToken(BareBonesSqlParser.NUMERIC_LITERAL, 0); }
		public TerminalNode STRING_LITERAL() { return getToken(BareBonesSqlParser.STRING_LITERAL, 0); }
		public TerminalNode K_NULL() { return getToken(BareBonesSqlParser.K_NULL, 0); }
		public Literal_valueContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_literal_value; }
	}

	public final Literal_valueContext literal_value() throws RecognitionException {
		Literal_valueContext _localctx = new Literal_valueContext(_ctx, getState());
		enterRule(_localctx, 68, RULE_literal_value);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(477);
			_la = _input.LA(1);
			if ( !(((((_la - 59)) & ~0x3f) == 0 && ((1L << (_la - 59)) & ((1L << (K_NULL - 59)) | (1L << (NUMERIC_LITERAL - 59)) | (1L << (STRING_LITERAL - 59)))) != 0)) ) {
			_errHandler.recoverInline(this);
			}
			else {
				if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
				_errHandler.reportMatch(this);
				consume();
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class KeywordContext extends ParserRuleContext {
		public TerminalNode K_USE() { return getToken(BareBonesSqlParser.K_USE, 0); }
		public TerminalNode K_CURRENT_DATABASE() { return getToken(BareBonesSqlParser.K_CURRENT_DATABASE, 0); }
		public TerminalNode K_LIST_DATABASES() { return getToken(BareBonesSqlParser.K_LIST_DATABASES, 0); }
		public TerminalNode K_GET_STRUCTURE() { return getToken(BareBonesSqlParser.K_GET_STRUCTURE, 0); }
		public TerminalNode K_NOT_TO_ENCRYPT() { return getToken(BareBonesSqlParser.K_NOT_TO_ENCRYPT, 0); }
		public TerminalNode K_BOOL() { return getToken(BareBonesSqlParser.K_BOOL, 0); }
		public TerminalNode K_DATETIME() { return getToken(BareBonesSqlParser.K_DATETIME, 0); }
		public TerminalNode K_DURATION() { return getToken(BareBonesSqlParser.K_DURATION, 0); }
		public TerminalNode K_INT() { return getToken(BareBonesSqlParser.K_INT, 0); }
		public TerminalNode K_DECIMAL() { return getToken(BareBonesSqlParser.K_DECIMAL, 0); }
		public TerminalNode K_DOUBLE() { return getToken(BareBonesSqlParser.K_DOUBLE, 0); }
		public TerminalNode K_TEXT() { return getToken(BareBonesSqlParser.K_TEXT, 0); }
		public TerminalNode K_ENCRYPTED() { return getToken(BareBonesSqlParser.K_ENCRYPTED, 0); }
		public TerminalNode K_RANGE() { return getToken(BareBonesSqlParser.K_RANGE, 0); }
		public TerminalNode K_ADD() { return getToken(BareBonesSqlParser.K_ADD, 0); }
		public TerminalNode K_ALL() { return getToken(BareBonesSqlParser.K_ALL, 0); }
		public TerminalNode K_ALTER() { return getToken(BareBonesSqlParser.K_ALTER, 0); }
		public TerminalNode K_AND() { return getToken(BareBonesSqlParser.K_AND, 0); }
		public TerminalNode K_ASC() { return getToken(BareBonesSqlParser.K_ASC, 0); }
		public TerminalNode K_BY() { return getToken(BareBonesSqlParser.K_BY, 0); }
		public TerminalNode K_COLUMN() { return getToken(BareBonesSqlParser.K_COLUMN, 0); }
		public TerminalNode K_CONSTRAINT() { return getToken(BareBonesSqlParser.K_CONSTRAINT, 0); }
		public TerminalNode K_CREATE() { return getToken(BareBonesSqlParser.K_CREATE, 0); }
		public TerminalNode K_CROSS() { return getToken(BareBonesSqlParser.K_CROSS, 0); }
		public TerminalNode K_DATABASE() { return getToken(BareBonesSqlParser.K_DATABASE, 0); }
		public TerminalNode K_DELETE() { return getToken(BareBonesSqlParser.K_DELETE, 0); }
		public TerminalNode K_DESC() { return getToken(BareBonesSqlParser.K_DESC, 0); }
		public TerminalNode K_DISTINCT() { return getToken(BareBonesSqlParser.K_DISTINCT, 0); }
		public TerminalNode K_DROP() { return getToken(BareBonesSqlParser.K_DROP, 0); }
		public TerminalNode K_EXECUTE() { return getToken(BareBonesSqlParser.K_EXECUTE, 0); }
		public TerminalNode K_FROM() { return getToken(BareBonesSqlParser.K_FROM, 0); }
		public TerminalNode K_IF() { return getToken(BareBonesSqlParser.K_IF, 0); }
		public TerminalNode K_INNER() { return getToken(BareBonesSqlParser.K_INNER, 0); }
		public TerminalNode K_INSERT() { return getToken(BareBonesSqlParser.K_INSERT, 0); }
		public TerminalNode K_INTO() { return getToken(BareBonesSqlParser.K_INTO, 0); }
		public TerminalNode K_JOIN() { return getToken(BareBonesSqlParser.K_JOIN, 0); }
		public TerminalNode K_KEY() { return getToken(BareBonesSqlParser.K_KEY, 0); }
		public TerminalNode K_LEFT() { return getToken(BareBonesSqlParser.K_LEFT, 0); }
		public TerminalNode K_LIMIT() { return getToken(BareBonesSqlParser.K_LIMIT, 0); }
		public TerminalNode K_NATURAL() { return getToken(BareBonesSqlParser.K_NATURAL, 0); }
		public TerminalNode K_NO() { return getToken(BareBonesSqlParser.K_NO, 0); }
		public TerminalNode K_NOT() { return getToken(BareBonesSqlParser.K_NOT, 0); }
		public TerminalNode K_NULL() { return getToken(BareBonesSqlParser.K_NULL, 0); }
		public TerminalNode K_OFFSET() { return getToken(BareBonesSqlParser.K_OFFSET, 0); }
		public TerminalNode K_ON() { return getToken(BareBonesSqlParser.K_ON, 0); }
		public TerminalNode K_OR() { return getToken(BareBonesSqlParser.K_OR, 0); }
		public TerminalNode K_ORDER() { return getToken(BareBonesSqlParser.K_ORDER, 0); }
		public TerminalNode K_OUTER() { return getToken(BareBonesSqlParser.K_OUTER, 0); }
		public TerminalNode K_PRIMARY() { return getToken(BareBonesSqlParser.K_PRIMARY, 0); }
		public TerminalNode K_REFERENCES() { return getToken(BareBonesSqlParser.K_REFERENCES, 0); }
		public TerminalNode K_RENAME() { return getToken(BareBonesSqlParser.K_RENAME, 0); }
		public TerminalNode K_SELECT() { return getToken(BareBonesSqlParser.K_SELECT, 0); }
		public TerminalNode K_SET() { return getToken(BareBonesSqlParser.K_SET, 0); }
		public TerminalNode K_TABLE() { return getToken(BareBonesSqlParser.K_TABLE, 0); }
		public TerminalNode K_TO() { return getToken(BareBonesSqlParser.K_TO, 0); }
		public TerminalNode K_UNIQUE() { return getToken(BareBonesSqlParser.K_UNIQUE, 0); }
		public TerminalNode K_UPDATE() { return getToken(BareBonesSqlParser.K_UPDATE, 0); }
		public TerminalNode K_USING() { return getToken(BareBonesSqlParser.K_USING, 0); }
		public TerminalNode K_VALUES() { return getToken(BareBonesSqlParser.K_VALUES, 0); }
		public TerminalNode K_WHERE() { return getToken(BareBonesSqlParser.K_WHERE, 0); }
		public KeywordContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_keyword; }
	}

	public final KeywordContext keyword() throws RecognitionException {
		KeywordContext _localctx = new KeywordContext(_ctx, getState());
		enterRule(_localctx, 70, RULE_keyword);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(479);
			_la = _input.LA(1);
			if ( !(((((_la - 17)) & ~0x3f) == 0 && ((1L << (_la - 17)) & ((1L << (K_USE - 17)) | (1L << (K_CURRENT_DATABASE - 17)) | (1L << (K_LIST_DATABASES - 17)) | (1L << (K_GET_STRUCTURE - 17)) | (1L << (K_NOT_TO_ENCRYPT - 17)) | (1L << (K_IF - 17)) | (1L << (K_EXECUTE - 17)) | (1L << (K_BOOL - 17)) | (1L << (K_DATETIME - 17)) | (1L << (K_DURATION - 17)) | (1L << (K_INT - 17)) | (1L << (K_DECIMAL - 17)) | (1L << (K_DOUBLE - 17)) | (1L << (K_TEXT - 17)) | (1L << (K_ENCRYPTED - 17)) | (1L << (K_RANGE - 17)) | (1L << (K_ADD - 17)) | (1L << (K_ALL - 17)) | (1L << (K_ALTER - 17)) | (1L << (K_AND - 17)) | (1L << (K_ASC - 17)) | (1L << (K_BY - 17)) | (1L << (K_COLUMN - 17)) | (1L << (K_CONSTRAINT - 17)) | (1L << (K_CREATE - 17)) | (1L << (K_CROSS - 17)) | (1L << (K_DATABASE - 17)) | (1L << (K_DELETE - 17)) | (1L << (K_DESC - 17)) | (1L << (K_DISTINCT - 17)) | (1L << (K_DROP - 17)) | (1L << (K_FROM - 17)) | (1L << (K_INNER - 17)) | (1L << (K_INSERT - 17)) | (1L << (K_INTO - 17)) | (1L << (K_JOIN - 17)) | (1L << (K_KEY - 17)) | (1L << (K_LEFT - 17)) | (1L << (K_LIMIT - 17)) | (1L << (K_NATURAL - 17)) | (1L << (K_NO - 17)) | (1L << (K_NOT - 17)) | (1L << (K_NULL - 17)) | (1L << (K_OFFSET - 17)) | (1L << (K_ON - 17)) | (1L << (K_OR - 17)) | (1L << (K_ORDER - 17)) | (1L << (K_OUTER - 17)) | (1L << (K_PRIMARY - 17)) | (1L << (K_REFERENCES - 17)) | (1L << (K_RENAME - 17)) | (1L << (K_SELECT - 17)) | (1L << (K_SET - 17)) | (1L << (K_TABLE - 17)) | (1L << (K_TO - 17)) | (1L << (K_UNIQUE - 17)) | (1L << (K_UPDATE - 17)) | (1L << (K_USING - 17)) | (1L << (K_VALUES - 17)) | (1L << (K_WHERE - 17)))) != 0)) ) {
			_errHandler.recoverInline(this);
			}
			else {
				if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
				_errHandler.reportMatch(this);
				consume();
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class NameContext extends ParserRuleContext {
		public Complex_nameContext complex_name() {
			return getRuleContext(Complex_nameContext.class,0);
		}
		public NameContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_name; }
	}

	public final NameContext name() throws RecognitionException {
		NameContext _localctx = new NameContext(_ctx, getState());
		enterRule(_localctx, 72, RULE_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(481);
			complex_name();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Table_nameContext extends ParserRuleContext {
		public Complex_nameContext complex_name() {
			return getRuleContext(Complex_nameContext.class,0);
		}
		public Table_nameContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_table_name; }
	}

	public final Table_nameContext table_name() throws RecognitionException {
		Table_nameContext _localctx = new Table_nameContext(_ctx, getState());
		enterRule(_localctx, 74, RULE_table_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(483);
			complex_name();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class New_table_nameContext extends ParserRuleContext {
		public Complex_nameContext complex_name() {
			return getRuleContext(Complex_nameContext.class,0);
		}
		public New_table_nameContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_new_table_name; }
	}

	public final New_table_nameContext new_table_name() throws RecognitionException {
		New_table_nameContext _localctx = new New_table_nameContext(_ctx, getState());
		enterRule(_localctx, 76, RULE_new_table_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(485);
			complex_name();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Column_nameContext extends ParserRuleContext {
		public Complex_nameContext complex_name() {
			return getRuleContext(Complex_nameContext.class,0);
		}
		public Column_nameContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_column_name; }
	}

	public final Column_nameContext column_name() throws RecognitionException {
		Column_nameContext _localctx = new Column_nameContext(_ctx, getState());
		enterRule(_localctx, 78, RULE_column_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(487);
			complex_name();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class New_column_nameContext extends ParserRuleContext {
		public Complex_nameContext complex_name() {
			return getRuleContext(Complex_nameContext.class,0);
		}
		public New_column_nameContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_new_column_name; }
	}

	public final New_column_nameContext new_column_name() throws RecognitionException {
		New_column_nameContext _localctx = new New_column_nameContext(_ctx, getState());
		enterRule(_localctx, 80, RULE_new_column_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(489);
			complex_name();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Database_nameContext extends ParserRuleContext {
		public Complex_nameContext complex_name() {
			return getRuleContext(Complex_nameContext.class,0);
		}
		public Database_nameContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_database_name; }
	}

	public final Database_nameContext database_name() throws RecognitionException {
		Database_nameContext _localctx = new Database_nameContext(_ctx, getState());
		enterRule(_localctx, 82, RULE_database_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(491);
			complex_name();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Foreign_tableContext extends ParserRuleContext {
		public Complex_nameContext complex_name() {
			return getRuleContext(Complex_nameContext.class,0);
		}
		public Foreign_tableContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_foreign_table; }
	}

	public final Foreign_tableContext foreign_table() throws RecognitionException {
		Foreign_tableContext _localctx = new Foreign_tableContext(_ctx, getState());
		enterRule(_localctx, 84, RULE_foreign_table);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(493);
			complex_name();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Table_column_nameContext extends ParserRuleContext {
		public Table_nameContext table_name() {
			return getRuleContext(Table_nameContext.class,0);
		}
		public Column_nameContext column_name() {
			return getRuleContext(Column_nameContext.class,0);
		}
		public Table_column_nameContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_table_column_name; }
	}

	public final Table_column_nameContext table_column_name() throws RecognitionException {
		Table_column_nameContext _localctx = new Table_column_nameContext(_ctx, getState());
		enterRule(_localctx, 86, RULE_table_column_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(495);
			table_name();
			setState(496);
			match(T__13);
			setState(497);
			column_name();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Any_nameContext extends ParserRuleContext {
		public TerminalNode IDENTIFIER() { return getToken(BareBonesSqlParser.IDENTIFIER, 0); }
		public KeywordContext keyword() {
			return getRuleContext(KeywordContext.class,0);
		}
		public TerminalNode STRING_LITERAL() { return getToken(BareBonesSqlParser.STRING_LITERAL, 0); }
		public Any_nameContext any_name() {
			return getRuleContext(Any_nameContext.class,0);
		}
		public Any_nameContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_any_name; }
	}

	public final Any_nameContext any_name() throws RecognitionException {
		Any_nameContext _localctx = new Any_nameContext(_ctx, getState());
		enterRule(_localctx, 88, RULE_any_name);
		try {
			setState(506);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case IDENTIFIER:
				enterOuterAlt(_localctx, 1);
				{
				setState(499);
				match(IDENTIFIER);
				}
				break;
			case K_USE:
			case K_CURRENT_DATABASE:
			case K_LIST_DATABASES:
			case K_GET_STRUCTURE:
			case K_NOT_TO_ENCRYPT:
			case K_IF:
			case K_EXECUTE:
			case K_BOOL:
			case K_DATETIME:
			case K_DURATION:
			case K_INT:
			case K_DECIMAL:
			case K_DOUBLE:
			case K_TEXT:
			case K_ENCRYPTED:
			case K_RANGE:
			case K_ADD:
			case K_ALL:
			case K_ALTER:
			case K_AND:
			case K_ASC:
			case K_BY:
			case K_COLUMN:
			case K_CONSTRAINT:
			case K_CREATE:
			case K_CROSS:
			case K_DATABASE:
			case K_DELETE:
			case K_DESC:
			case K_DISTINCT:
			case K_DROP:
			case K_FROM:
			case K_INNER:
			case K_INSERT:
			case K_INTO:
			case K_JOIN:
			case K_KEY:
			case K_LEFT:
			case K_LIMIT:
			case K_NATURAL:
			case K_NO:
			case K_NOT:
			case K_NULL:
			case K_OFFSET:
			case K_ON:
			case K_OR:
			case K_ORDER:
			case K_OUTER:
			case K_PRIMARY:
			case K_REFERENCES:
			case K_RENAME:
			case K_SELECT:
			case K_SET:
			case K_TABLE:
			case K_TO:
			case K_UNIQUE:
			case K_UPDATE:
			case K_USING:
			case K_VALUES:
			case K_WHERE:
				enterOuterAlt(_localctx, 2);
				{
				setState(500);
				keyword();
				}
				break;
			case STRING_LITERAL:
				enterOuterAlt(_localctx, 3);
				{
				setState(501);
				match(STRING_LITERAL);
				}
				break;
			case T__1:
				enterOuterAlt(_localctx, 4);
				{
				setState(502);
				match(T__1);
				setState(503);
				any_name();
				setState(504);
				match(T__3);
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class Complex_nameContext extends ParserRuleContext {
		public Any_nameContext any_name() {
			return getRuleContext(Any_nameContext.class,0);
		}
		public TerminalNode K_NOT_TO_ENCRYPT() { return getToken(BareBonesSqlParser.K_NOT_TO_ENCRYPT, 0); }
		public Complex_nameContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_complex_name; }
	}

	public final Complex_nameContext complex_name() throws RecognitionException {
		Complex_nameContext _localctx = new Complex_nameContext(_ctx, getState());
		enterRule(_localctx, 90, RULE_complex_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(509);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,48,_ctx) ) {
			case 1:
				{
				setState(508);
				match(K_NOT_TO_ENCRYPT);
				}
				break;
			}
			setState(511);
			any_name();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public boolean sempred(RuleContext _localctx, int ruleIndex, int predIndex) {
		switch (ruleIndex) {
		case 31:
			return expr_sempred((ExprContext)_localctx, predIndex);
		}
		return true;
	}
	private boolean expr_sempred(ExprContext _localctx, int predIndex) {
		switch (predIndex) {
		case 0:
			return precpred(_ctx, 2);
		}
		return true;
	}

	public static final String _serializedATN =
		"\3\u608b\ua72a\u8133\ub9ed\u417c\u3be7\u7786\u5964\3S\u0204\4\2\t\2\4"+
		"\3\t\3\4\4\t\4\4\5\t\5\4\6\t\6\4\7\t\7\4\b\t\b\4\t\t\t\4\n\t\n\4\13\t"+
		"\13\4\f\t\f\4\r\t\r\4\16\t\16\4\17\t\17\4\20\t\20\4\21\t\21\4\22\t\22"+
		"\4\23\t\23\4\24\t\24\4\25\t\25\4\26\t\26\4\27\t\27\4\30\t\30\4\31\t\31"+
		"\4\32\t\32\4\33\t\33\4\34\t\34\4\35\t\35\4\36\t\36\4\37\t\37\4 \t \4!"+
		"\t!\4\"\t\"\4#\t#\4$\t$\4%\t%\4&\t&\4\'\t\'\4(\t(\4)\t)\4*\t*\4+\t+\4"+
		",\t,\4-\t-\4.\t.\4/\t/\3\2\3\2\7\2a\n\2\f\2\16\2d\13\2\3\2\3\2\3\3\3\3"+
		"\3\3\3\4\3\4\3\4\3\4\3\4\7\4p\n\4\f\4\16\4s\13\4\3\5\3\5\3\5\3\5\3\5\3"+
		"\5\3\5\3\5\3\5\3\5\3\5\3\5\3\5\5\5\u0082\n\5\3\6\3\6\3\6\3\7\3\7\3\b\3"+
		"\b\3\t\3\t\3\t\3\n\3\n\3\n\3\n\3\13\3\13\3\13\3\13\3\f\3\f\3\f\3\f\3\f"+
		"\3\f\3\f\7\f\u009d\n\f\f\f\16\f\u00a0\13\f\3\f\3\f\3\r\3\r\3\r\3\r\3\r"+
		"\3\r\3\r\3\r\3\r\3\r\3\r\3\r\3\r\3\r\3\r\3\r\3\r\5\r\u00b5\n\r\3\16\3"+
		"\16\3\16\3\16\3\17\3\17\3\17\3\17\3\17\3\17\3\17\7\17\u00c2\n\17\f\17"+
		"\16\17\u00c5\13\17\3\17\3\17\5\17\u00c9\n\17\3\17\3\17\3\17\3\17\3\17"+
		"\7\17\u00d0\n\17\f\17\16\17\u00d3\13\17\3\17\3\17\3\17\3\17\3\17\3\17"+
		"\7\17\u00db\n\17\f\17\16\17\u00de\13\17\3\17\3\17\7\17\u00e2\n\17\f\17"+
		"\16\17\u00e5\13\17\3\20\3\20\3\20\3\20\3\20\3\20\3\20\3\20\3\20\3\20\3"+
		"\20\7\20\u00f2\n\20\f\20\16\20\u00f5\13\20\3\20\3\20\5\20\u00f9\n\20\3"+
		"\21\3\21\3\21\3\21\3\21\5\21\u0100\n\21\3\22\3\22\3\23\3\23\3\23\3\23"+
		"\3\23\3\23\3\23\3\23\3\23\7\23\u010d\n\23\f\23\16\23\u0110\13\23\3\23"+
		"\3\23\3\24\3\24\3\24\3\24\3\24\3\24\7\24\u011a\n\24\f\24\16\24\u011d\13"+
		"\24\5\24\u011f\n\24\3\24\3\24\3\24\3\24\5\24\u0125\n\24\5\24\u0127\n\24"+
		"\3\25\3\25\5\25\u012b\n\25\3\25\3\25\3\25\7\25\u0130\n\25\f\25\16\25\u0133"+
		"\13\25\3\25\3\25\3\25\3\25\7\25\u0139\n\25\f\25\16\25\u013c\13\25\3\25"+
		"\5\25\u013f\n\25\3\25\3\25\3\25\5\25\u0144\n\25\3\26\3\26\5\26\u0148\n"+
		"\26\3\27\3\27\3\27\3\27\5\27\u014e\n\27\3\30\3\30\3\30\3\30\3\30\7\30"+
		"\u0155\n\30\f\30\16\30\u0158\13\30\3\30\5\30\u015b\n\30\3\30\3\30\3\30"+
		"\3\30\3\30\3\30\5\30\u0163\n\30\3\31\3\31\3\31\3\31\3\31\7\31\u016a\n"+
		"\31\f\31\16\31\u016d\13\31\3\32\3\32\5\32\u0171\n\32\3\32\3\32\5\32\u0175"+
		"\n\32\3\32\3\32\5\32\u0179\n\32\3\32\5\32\u017c\n\32\3\33\3\33\5\33\u0180"+
		"\n\33\3\34\3\34\3\34\7\34\u0185\n\34\f\34\16\34\u0188\13\34\3\35\3\35"+
		"\3\35\3\35\3\35\3\35\3\35\3\35\3\35\5\35\u0193\n\35\3\35\3\35\5\35\u0197"+
		"\n\35\5\35\u0199\n\35\3\36\3\36\3\37\3\37\3\37\3\37\3\37\3\37\3\37\3\37"+
		"\3 \3 \5 \u01a7\n \3 \3 \3 \5 \u01ac\n \3 \3 \3 \5 \u01b1\n \3!\3!\3!"+
		"\3!\3!\3!\3!\3!\3!\3!\3!\3!\3!\3!\3!\5!\u01c2\n!\3!\3!\3!\7!\u01c7\n!"+
		"\f!\16!\u01ca\13!\3\"\3\"\3\"\3\"\3\"\3\"\7\"\u01d2\n\"\f\"\16\"\u01d5"+
		"\13\"\3\"\3\"\5\"\u01d9\n\"\3#\5#\u01dc\n#\3#\3#\3$\3$\3%\3%\3&\3&\3\'"+
		"\3\'\3(\3(\3)\3)\3*\3*\3+\3+\3,\3,\3-\3-\3-\3-\3.\3.\3.\3.\3.\3.\3.\5"+
		".\u01fd\n.\3/\5/\u0200\n/\3/\3/\3/\2\3@\60\2\4\6\b\n\f\16\20\22\24\26"+
		"\30\32\34\36 \"$&(*,.\60\62\64\668:<>@BDFHJLNPRTVXZ\\\2\b\3\2\7\f\4\2"+
		"\'\'//\4\2&&@@\3\2\21\22\5\2==PPRR\3\2\23N\2\u0222\2b\3\2\2\2\4g\3\2\2"+
		"\2\6j\3\2\2\2\b\u0081\3\2\2\2\n\u0083\3\2\2\2\f\u0086\3\2\2\2\16\u0088"+
		"\3\2\2\2\20\u008a\3\2\2\2\22\u008d\3\2\2\2\24\u0091\3\2\2\2\26\u0095\3"+
		"\2\2\2\30\u00a3\3\2\2\2\32\u00b6\3\2\2\2\34\u00ba\3\2\2\2\36\u00e6\3\2"+
		"\2\2 \u00fa\3\2\2\2\"\u0101\3\2\2\2$\u0103\3\2\2\2&\u0113\3\2\2\2(\u0128"+
		"\3\2\2\2*\u0145\3\2\2\2,\u014d\3\2\2\2.\u0162\3\2\2\2\60\u0164\3\2\2\2"+
		"\62\u017b\3\2\2\2\64\u017f\3\2\2\2\66\u0181\3\2\2\28\u0198\3\2\2\2:\u019a"+
		"\3\2\2\2<\u019c\3\2\2\2>\u01a6\3\2\2\2@\u01c1\3\2\2\2B\u01cb\3\2\2\2D"+
		"\u01db\3\2\2\2F\u01df\3\2\2\2H\u01e1\3\2\2\2J\u01e3\3\2\2\2L\u01e5\3\2"+
		"\2\2N\u01e7\3\2\2\2P\u01e9\3\2\2\2R\u01eb\3\2\2\2T\u01ed\3\2\2\2V\u01ef"+
		"\3\2\2\2X\u01f1\3\2\2\2Z\u01fc\3\2\2\2\\\u01ff\3\2\2\2^a\5\6\4\2_a\5\4"+
		"\3\2`^\3\2\2\2`_\3\2\2\2ad\3\2\2\2b`\3\2\2\2bc\3\2\2\2ce\3\2\2\2db\3\2"+
		"\2\2ef\7\2\2\3f\3\3\2\2\2gh\7S\2\2hi\b\3\1\2i\5\3\2\2\2jk\5\b\5\2kq\7"+
		"\3\2\2lm\5\b\5\2mn\7\3\2\2np\3\2\2\2ol\3\2\2\2ps\3\2\2\2qo\3\2\2\2qr\3"+
		"\2\2\2r\7\3\2\2\2sq\3\2\2\2t\u0082\5\22\n\2u\u0082\5\24\13\2v\u0082\5"+
		"\30\r\2w\u0082\5\26\f\2x\u0082\5\32\16\2y\u0082\5\34\17\2z\u0082\5\36"+
		"\20\2{\u0082\5 \21\2|\u0082\5&\24\2}\u0082\5\n\6\2~\u0082\5\f\7\2\177"+
		"\u0082\5\16\b\2\u0080\u0082\5$\23\2\u0081t\3\2\2\2\u0081u\3\2\2\2\u0081"+
		"v\3\2\2\2\u0081w\3\2\2\2\u0081x\3\2\2\2\u0081y\3\2\2\2\u0081z\3\2\2\2"+
		"\u0081{\3\2\2\2\u0081|\3\2\2\2\u0081}\3\2\2\2\u0081~\3\2\2\2\u0081\177"+
		"\3\2\2\2\u0081\u0080\3\2\2\2\u0082\t\3\2\2\2\u0083\u0084\7\23\2\2\u0084"+
		"\u0085\5T+\2\u0085\13\3\2\2\2\u0086\u0087\7\24\2\2\u0087\r\3\2\2\2\u0088"+
		"\u0089\7\25\2\2\u0089\17\3\2\2\2\u008a\u008b\7\26\2\2\u008b\u008c\5T+"+
		"\2\u008c\21\3\2\2\2\u008d\u008e\7+\2\2\u008e\u008f\7-\2\2\u008f\u0090"+
		"\5T+\2\u0090\23\3\2\2\2\u0091\u0092\7\61\2\2\u0092\u0093\7-\2\2\u0093"+
		"\u0094\5T+\2\u0094\25\3\2\2\2\u0095\u0096\7+\2\2\u0096\u0097\7H\2\2\u0097"+
		"\u0098\5L\'\2\u0098\u0099\7\4\2\2\u0099\u009e\5\66\34\2\u009a\u009b\7"+
		"\5\2\2\u009b\u009d\5\66\34\2\u009c\u009a\3\2\2\2\u009d\u00a0\3\2\2\2\u009e"+
		"\u009c\3\2\2\2\u009e\u009f\3\2\2\2\u009f\u00a1\3\2\2\2\u00a0\u009e\3\2"+
		"\2\2\u00a1\u00a2\7\6\2\2\u00a2\27\3\2\2\2\u00a3\u00a4\7%\2\2\u00a4\u00a5"+
		"\7H\2\2\u00a5\u00b4\5L\'\2\u00a6\u00a7\7E\2\2\u00a7\u00a8\7I\2\2\u00a8"+
		"\u00b5\5N(\2\u00a9\u00aa\7#\2\2\u00aa\u00ab\7)\2\2\u00ab\u00b5\5\66\34"+
		"\2\u00ac\u00ad\7\61\2\2\u00ad\u00ae\7)\2\2\u00ae\u00b5\5P)\2\u00af\u00b0"+
		"\7E\2\2\u00b0\u00b1\5P)\2\u00b1\u00b2\7I\2\2\u00b2\u00b3\5R*\2\u00b3\u00b5"+
		"\3\2\2\2\u00b4\u00a6\3\2\2\2\u00b4\u00a9\3\2\2\2\u00b4\u00ac\3\2\2\2\u00b4"+
		"\u00af\3\2\2\2\u00b5\31\3\2\2\2\u00b6\u00b7\7\61\2\2\u00b7\u00b8\7H\2"+
		"\2\u00b8\u00b9\5L\'\2\u00b9\33\3\2\2\2\u00ba\u00bb\7\64\2\2\u00bb\u00bc"+
		"\7\65\2\2\u00bc\u00c8\5L\'\2\u00bd\u00be\7\4\2\2\u00be\u00c3\5P)\2\u00bf"+
		"\u00c0\7\5\2\2\u00c0\u00c2\5P)\2\u00c1\u00bf\3\2\2\2\u00c2\u00c5\3\2\2"+
		"\2\u00c3\u00c1\3\2\2\2\u00c3\u00c4\3\2\2\2\u00c4\u00c6\3\2\2\2\u00c5\u00c3"+
		"\3\2\2\2\u00c6\u00c7\7\6\2\2\u00c7\u00c9\3\2\2\2\u00c8\u00bd\3\2\2\2\u00c8"+
		"\u00c9\3\2\2\2\u00c9\u00ca\3\2\2\2\u00ca\u00cb\7M\2\2\u00cb\u00cc\7\4"+
		"\2\2\u00cc\u00d1\5F$\2\u00cd\u00ce\7\5\2\2\u00ce\u00d0\5F$\2\u00cf\u00cd"+
		"\3\2\2\2\u00d0\u00d3\3\2\2\2\u00d1\u00cf\3\2\2\2\u00d1\u00d2\3\2\2\2\u00d2"+
		"\u00d4\3\2\2\2\u00d3\u00d1\3\2\2\2\u00d4\u00e3\7\6\2\2\u00d5\u00d6\7\5"+
		"\2\2\u00d6\u00d7\7\4\2\2\u00d7\u00dc\5F$\2\u00d8\u00d9\7\5\2\2\u00d9\u00db"+
		"\5F$\2\u00da\u00d8\3\2\2\2\u00db\u00de\3\2\2\2\u00dc\u00da\3\2\2\2\u00dc"+
		"\u00dd\3\2\2\2\u00dd\u00df\3\2\2\2\u00de\u00dc\3\2\2\2\u00df\u00e0\7\6"+
		"\2\2\u00e0\u00e2\3\2\2\2\u00e1\u00d5\3\2\2\2\u00e2\u00e5\3\2\2\2\u00e3"+
		"\u00e1\3\2\2\2\u00e3\u00e4\3\2\2\2\u00e4\35\3\2\2\2\u00e5\u00e3\3\2\2"+
		"\2\u00e6\u00e7\7K\2\2\u00e7\u00e8\5L\'\2\u00e8\u00e9\7G\2\2\u00e9\u00ea"+
		"\5P)\2\u00ea\u00eb\7\7\2\2\u00eb\u00f3\5F$\2\u00ec\u00ed\7\5\2\2\u00ed"+
		"\u00ee\5P)\2\u00ee\u00ef\7\7\2\2\u00ef\u00f0\5F$\2\u00f0\u00f2\3\2\2\2"+
		"\u00f1\u00ec\3\2\2\2\u00f2\u00f5\3\2\2\2\u00f3\u00f1\3\2\2\2\u00f3\u00f4"+
		"\3\2\2\2\u00f4\u00f8\3\2\2\2\u00f5\u00f3\3\2\2\2\u00f6\u00f7\7N\2\2\u00f7"+
		"\u00f9\5@!\2\u00f8\u00f6\3\2\2\2\u00f8\u00f9\3\2\2\2\u00f9\37\3\2\2\2"+
		"\u00fa\u00fb\7.\2\2\u00fb\u00fc\7\62\2\2\u00fc\u00ff\5L\'\2\u00fd\u00fe"+
		"\7N\2\2\u00fe\u0100\5@!\2\u00ff\u00fd\3\2\2\2\u00ff\u0100\3\2\2\2\u0100"+
		"!\3\2\2\2\u0101\u0102\t\2\2\2\u0102#\3\2\2\2\u0103\u0104\7\30\2\2\u0104"+
		"\u0105\5&\24\2\u0105\u0106\7\31\2\2\u0106\u0107\7\r\2\2\u0107\u0108\5"+
		"\b\5\2\u0108\u010e\7\3\2\2\u0109\u010a\5\b\5\2\u010a\u010b\7\3\2\2\u010b"+
		"\u010d\3\2\2\2\u010c\u0109\3\2\2\2\u010d\u0110\3\2\2\2\u010e\u010c\3\2"+
		"\2\2\u010e\u010f\3\2\2\2\u010f\u0111\3\2\2\2\u0110\u010e\3\2\2\2\u0111"+
		"\u0112\7\16\2\2\u0112%\3\2\2\2\u0113\u011e\5(\25\2\u0114\u0115\7A\2\2"+
		"\u0115\u0116\7(\2\2\u0116\u011b\5*\26\2\u0117\u0118\7\5\2\2\u0118\u011a"+
		"\5*\26\2\u0119\u0117\3\2\2\2\u011a\u011d\3\2\2\2\u011b\u0119\3\2\2\2\u011b"+
		"\u011c\3\2\2\2\u011c\u011f\3\2\2\2\u011d\u011b\3\2\2\2\u011e\u0114\3\2"+
		"\2\2\u011e\u011f\3\2\2\2\u011f\u0126\3\2\2\2\u0120\u0121\79\2\2\u0121"+
		"\u0124\5F$\2\u0122\u0123\7>\2\2\u0123\u0125\5F$\2\u0124\u0122\3\2\2\2"+
		"\u0124\u0125\3\2\2\2\u0125\u0127\3\2\2\2\u0126\u0120\3\2\2\2\u0126\u0127"+
		"\3\2\2\2\u0127\'\3\2\2\2\u0128\u012a\7F\2\2\u0129\u012b\7\60\2\2\u012a"+
		"\u0129\3\2\2\2\u012a\u012b\3\2\2\2\u012b\u012c\3\2\2\2\u012c\u0131\5,"+
		"\27\2\u012d\u012e\7\5\2\2\u012e\u0130\5,\27\2\u012f\u012d\3\2\2\2\u0130"+
		"\u0133\3\2\2\2\u0131\u012f\3\2\2\2\u0131\u0132\3\2\2\2\u0132\u0134\3\2"+
		"\2\2\u0133\u0131\3\2\2\2\u0134\u013e\7\62\2\2\u0135\u013a\5.\30\2\u0136"+
		"\u0137\7\5\2\2\u0137\u0139\5.\30\2\u0138\u0136\3\2\2\2\u0139\u013c\3\2"+
		"\2\2\u013a\u0138\3\2\2\2\u013a\u013b\3\2\2\2\u013b\u013f\3\2\2\2\u013c"+
		"\u013a\3\2\2\2\u013d\u013f\5\60\31\2\u013e\u0135\3\2\2\2\u013e\u013d\3"+
		"\2\2\2\u013f\u0143\3\2\2\2\u0140\u0141\7N\2\2\u0141\u0144\5@!\2\u0142"+
		"\u0144\7!\2\2\u0143\u0140\3\2\2\2\u0143\u0142\3\2\2\2\u0143\u0144\3\2"+
		"\2\2\u0144)\3\2\2\2\u0145\u0147\5@!\2\u0146\u0148\t\3\2\2\u0147\u0146"+
		"\3\2\2\2\u0147\u0148\3\2\2\2\u0148+\3\2\2\2\u0149\u014a\5L\'\2\u014a\u014b"+
		"\7\17\2\2\u014b\u014e\3\2\2\2\u014c\u014e\5X-\2\u014d\u0149\3\2\2\2\u014d"+
		"\u014c\3\2\2\2\u014e-\3\2\2\2\u014f\u0163\5L\'\2\u0150\u015a\7\4\2\2\u0151"+
		"\u0156\5.\30\2\u0152\u0153\7\5\2\2\u0153\u0155\5.\30\2\u0154\u0152\3\2"+
		"\2\2\u0155\u0158\3\2\2\2\u0156\u0154\3\2\2\2\u0156\u0157\3\2\2\2\u0157"+
		"\u015b\3\2\2\2\u0158\u0156\3\2\2\2\u0159\u015b\5\60\31\2\u015a\u0151\3"+
		"\2\2\2\u015a\u0159\3\2\2\2\u015b\u015c\3\2\2\2\u015c\u015d\7\6\2\2\u015d"+
		"\u0163\3\2\2\2\u015e\u015f\7\4\2\2\u015f\u0160\5&\24\2\u0160\u0161\7\6"+
		"\2\2\u0161\u0163\3\2\2\2\u0162\u014f\3\2\2\2\u0162\u0150\3\2\2\2\u0162"+
		"\u015e\3\2\2\2\u0163/\3\2\2\2\u0164\u016b\5.\30\2\u0165\u0166\5\62\32"+
		"\2\u0166\u0167\5.\30\2\u0167\u0168\5\64\33\2\u0168\u016a\3\2\2\2\u0169"+
		"\u0165\3\2\2\2\u016a\u016d\3\2\2\2\u016b\u0169\3\2\2\2\u016b\u016c\3\2"+
		"\2\2\u016c\61\3\2\2\2\u016d\u016b\3\2\2\2\u016e\u017c\7\5\2\2\u016f\u0171"+
		"\7:\2\2\u0170\u016f\3\2\2\2\u0170\u0171\3\2\2\2\u0171\u0178\3\2\2\2\u0172"+
		"\u0174\78\2\2\u0173\u0175\7B\2\2\u0174\u0173\3\2\2\2\u0174\u0175\3\2\2"+
		"\2\u0175\u0179\3\2\2\2\u0176\u0179\7\63\2\2\u0177\u0179\7,\2\2\u0178\u0172"+
		"\3\2\2\2\u0178\u0176\3\2\2\2\u0178\u0177\3\2\2\2\u0178\u0179\3\2\2\2\u0179"+
		"\u017a\3\2\2\2\u017a\u017c\7\66\2\2\u017b\u016e\3\2\2\2\u017b\u0170\3"+
		"\2\2\2\u017c\63\3\2\2\2\u017d\u017e\7?\2\2\u017e\u0180\5@!\2\u017f\u017d"+
		"\3\2\2\2\u017f\u0180\3\2\2\2\u0180\65\3\2\2\2\u0181\u0182\5P)\2\u0182"+
		"\u0186\58\35\2\u0183\u0185\5> \2\u0184\u0183\3\2\2\2\u0185\u0188\3\2\2"+
		"\2\u0186\u0184\3\2\2\2\u0186\u0187\3\2\2\2\u0187\67\3\2\2\2\u0188\u0186"+
		"\3\2\2\2\u0189\u0199\7\32\2\2\u018a\u0199\7\33\2\2\u018b\u0199\7\34\2"+
		"\2\u018c\u0199\7\35\2\2\u018d\u0199\7\36\2\2\u018e\u0199\7\37\2\2\u018f"+
		"\u0199\7 \2\2\u0190\u0192\7!\2\2\u0191\u0193\5:\36\2\u0192\u0191\3\2\2"+
		"\2\u0192\u0193\3\2\2\2\u0193\u0196\3\2\2\2\u0194\u0195\7\"\2\2\u0195\u0197"+
		"\5<\37\2\u0196\u0194\3\2\2\2\u0196\u0197\3\2\2\2\u0197\u0199\3\2\2\2\u0198"+
		"\u0189\3\2\2\2\u0198\u018a\3\2\2\2\u0198\u018b\3\2\2\2\u0198\u018c\3\2"+
		"\2\2\u0198\u018d\3\2\2\2\u0198\u018e\3\2\2\2\u0198\u018f\3\2\2\2\u0198"+
		"\u0190\3\2\2\2\u01999\3\2\2\2\u019a\u019b\7P\2\2\u019b;\3\2\2\2\u019c"+
		"\u019d\7\4\2\2\u019d\u019e\7P\2\2\u019e\u019f\7\5\2\2\u019f\u01a0\7P\2"+
		"\2\u01a0\u01a1\7\5\2\2\u01a1\u01a2\7P\2\2\u01a2\u01a3\7\6\2\2\u01a3=\3"+
		"\2\2\2\u01a4\u01a5\7*\2\2\u01a5\u01a7\5J&\2\u01a6\u01a4\3\2\2\2\u01a6"+
		"\u01a7\3\2\2\2\u01a7\u01b0\3\2\2\2\u01a8\u01a9\7C\2\2\u01a9\u01b1\7\67"+
		"\2\2\u01aa\u01ac\7<\2\2\u01ab\u01aa\3\2\2\2\u01ab\u01ac\3\2\2\2\u01ac"+
		"\u01ad\3\2\2\2\u01ad\u01b1\7=\2\2\u01ae\u01b1\7J\2\2\u01af\u01b1\5B\""+
		"\2\u01b0\u01a8\3\2\2\2\u01b0\u01ab\3\2\2\2\u01b0\u01ae\3\2\2\2\u01b0\u01af"+
		"\3\2\2\2\u01b1?\3\2\2\2\u01b2\u01b3\b!\1\2\u01b3\u01b4\5L\'\2\u01b4\u01b5"+
		"\7\20\2\2\u01b5\u01b6\5P)\2\u01b6\u01b7\5\"\22\2\u01b7\u01b8\5F$\2\u01b8"+
		"\u01c2\3\2\2\2\u01b9\u01ba\5X-\2\u01ba\u01bb\5\"\22\2\u01bb\u01bc\5X-"+
		"\2\u01bc\u01c2\3\2\2\2\u01bd\u01be\7\4\2\2\u01be\u01bf\5@!\2\u01bf\u01c0"+
		"\7\6\2\2\u01c0\u01c2\3\2\2\2\u01c1\u01b2\3\2\2\2\u01c1\u01b9\3\2\2\2\u01c1"+
		"\u01bd\3\2\2\2\u01c2\u01c8\3\2\2\2\u01c3\u01c4\f\4\2\2\u01c4\u01c5\t\4"+
		"\2\2\u01c5\u01c7\5@!\5\u01c6\u01c3\3\2\2\2\u01c7\u01ca\3\2\2\2\u01c8\u01c6"+
		"\3\2\2\2\u01c8\u01c9\3\2\2\2\u01c9A\3\2\2\2\u01ca\u01c8\3\2\2\2\u01cb"+
		"\u01cc\7D\2\2\u01cc\u01d8\5V,\2\u01cd\u01ce\7\4\2\2\u01ce\u01d3\5P)\2"+
		"\u01cf\u01d0\7\5\2\2\u01d0\u01d2\5P)\2\u01d1\u01cf\3\2\2\2\u01d2\u01d5"+
		"\3\2\2\2\u01d3\u01d1\3\2\2\2\u01d3\u01d4\3\2\2\2\u01d4\u01d6\3\2\2\2\u01d5"+
		"\u01d3\3\2\2\2\u01d6\u01d7\7\6\2\2\u01d7\u01d9\3\2\2\2\u01d8\u01cd\3\2"+
		"\2\2\u01d8\u01d9\3\2\2\2\u01d9C\3\2\2\2\u01da\u01dc\t\5\2\2\u01db\u01da"+
		"\3\2\2\2\u01db\u01dc\3\2\2\2\u01dc\u01dd\3\2\2\2\u01dd\u01de\7P\2\2\u01de"+
		"E\3\2\2\2\u01df\u01e0\t\6\2\2\u01e0G\3\2\2\2\u01e1\u01e2\t\7\2\2\u01e2"+
		"I\3\2\2\2\u01e3\u01e4\5\\/\2\u01e4K\3\2\2\2\u01e5\u01e6\5\\/\2\u01e6M"+
		"\3\2\2\2\u01e7\u01e8\5\\/\2\u01e8O\3\2\2\2\u01e9\u01ea\5\\/\2\u01eaQ\3"+
		"\2\2\2\u01eb\u01ec\5\\/\2\u01ecS\3\2\2\2\u01ed\u01ee\5\\/\2\u01eeU\3\2"+
		"\2\2\u01ef\u01f0\5\\/\2\u01f0W\3\2\2\2\u01f1\u01f2\5L\'\2\u01f2\u01f3"+
		"\7\20\2\2\u01f3\u01f4\5P)\2\u01f4Y\3\2\2\2\u01f5\u01fd\7O\2\2\u01f6\u01fd"+
		"\5H%\2\u01f7\u01fd\7R\2\2\u01f8\u01f9\7\4\2\2\u01f9\u01fa\5Z.\2\u01fa"+
		"\u01fb\7\6\2\2\u01fb\u01fd\3\2\2\2\u01fc\u01f5\3\2\2\2\u01fc\u01f6\3\2"+
		"\2\2\u01fc\u01f7\3\2\2\2\u01fc\u01f8\3\2\2\2\u01fd[\3\2\2\2\u01fe\u0200"+
		"\7\27\2\2\u01ff\u01fe\3\2\2\2\u01ff\u0200\3\2\2\2\u0200\u0201\3\2\2\2"+
		"\u0201\u0202\5Z.\2\u0202]\3\2\2\2\63`bq\u0081\u009e\u00b4\u00c3\u00c8"+
		"\u00d1\u00dc\u00e3\u00f3\u00f8\u00ff\u010e\u011b\u011e\u0124\u0126\u012a"+
		"\u0131\u013a\u013e\u0143\u0147\u014d\u0156\u015a\u0162\u016b\u0170\u0174"+
		"\u0178\u017b\u017f\u0186\u0192\u0196\u0198\u01a6\u01ab\u01b0\u01c1\u01c8"+
		"\u01d3\u01d8\u01db\u01fc\u01ff";
	public static final ATN _ATN =
		new ATNDeserializer().deserialize(_serializedATN.toCharArray());
	static {
		_decisionToDFA = new DFA[_ATN.getNumberOfDecisions()];
		for (int i = 0; i < _ATN.getNumberOfDecisions(); i++) {
			_decisionToDFA[i] = new DFA(_ATN.getDecisionState(i), i);
		}
	}
}