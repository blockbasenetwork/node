// Generated from /home/marcia/BlockBaseVS/Agap2 - RD - BlockBase/BlockBase.Domain/Database/Sql/QueryParser/BareBonesSql.g4 by ANTLR 4.7.1
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
		T__9=10, T__10=11, T__11=12, T__12=13, T__13=14, T__14=15, K_USE=16, K_CURRENT_DATABASE=17, 
		K_LIST_DATABASES=18, K_GET_STRUCTURE=19, K_NOT_TO_ENCRYPT=20, K_BOOL=21, 
		K_DATETIME=22, K_DURATION=23, K_INT=24, K_DECIMAL=25, K_DOUBLE=26, K_TEXT=27, 
		K_ENCRYPTED=28, K_RANGE=29, K_ADD=30, K_ALL=31, K_ALTER=32, K_AND=33, 
		K_ASC=34, K_BY=35, K_COLUMN=36, K_CONSTRAINT=37, K_CREATE=38, K_CROSS=39, 
		K_DELETE=40, K_DESC=41, K_DISTINCT=42, K_DROP=43, K_FROM=44, K_INNER=45, 
		K_INSERT=46, K_INTO=47, K_JOIN=48, K_KEY=49, K_LEFT=50, K_LIMIT=51, K_NATURAL=52, 
		K_NO=53, K_NOT=54, K_NULL=55, K_OFFSET=56, K_ON=57, K_OR=58, K_ORDER=59, 
		K_OUTER=60, K_PRIMARY=61, K_REFERENCES=62, K_RENAME=63, K_SELECT=64, K_SET=65, 
		K_TABLE=66, K_TO=67, K_UNIQUE=68, K_UPDATE=69, K_USING=70, K_VALUES=71, 
		K_WHERE=72, IDENTIFIER=73, NUMERIC_LITERAL=74, BIND_PARAMETER=75, STRING_LITERAL=76, 
		UNEXPECTED_CHAR=77;
	public static final int
		RULE_parse = 0, RULE_error = 1, RULE_sql_stmt_list = 2, RULE_sql_stmt = 3, 
		RULE_use_database_stmt = 4, RULE_current_database_stmt = 5, RULE_list_databases_stmt = 6, 
		RULE_get_structure_stmt = 7, RULE_create_table_stmt = 8, RULE_alter_table_stmt = 9, 
		RULE_drop_table_stmt = 10, RULE_insert_stmt = 11, RULE_update_stmt = 12, 
		RULE_delete_stmt = 13, RULE_simple_select_stmt = 14, RULE_select_core = 15, 
		RULE_ordering_term = 16, RULE_result_column = 17, RULE_table_or_subquery = 18, 
		RULE_join_clause = 19, RULE_join_operator = 20, RULE_join_constraint = 21, 
		RULE_column_def = 22, RULE_data_type = 23, RULE_bucket_size = 24, RULE_bucket_range = 25, 
		RULE_column_constraint = 26, RULE_expr = 27, RULE_foreign_key_clause = 28, 
		RULE_signed_number = 29, RULE_literal_value = 30, RULE_keyword = 31, RULE_name = 32, 
		RULE_table_name = 33, RULE_new_table_name = 34, RULE_column_name = 35, 
		RULE_new_column_name = 36, RULE_database_name = 37, RULE_foreign_table = 38, 
		RULE_any_name = 39, RULE_complex_name = 40;
	public static final String[] ruleNames = {
		"parse", "error", "sql_stmt_list", "sql_stmt", "use_database_stmt", "current_database_stmt", 
		"list_databases_stmt", "get_structure_stmt", "create_table_stmt", "alter_table_stmt", 
		"drop_table_stmt", "insert_stmt", "update_stmt", "delete_stmt", "simple_select_stmt", 
		"select_core", "ordering_term", "result_column", "table_or_subquery", 
		"join_clause", "join_operator", "join_constraint", "column_def", "data_type", 
		"bucket_size", "bucket_range", "column_constraint", "expr", "foreign_key_clause", 
		"signed_number", "literal_value", "keyword", "name", "table_name", "new_table_name", 
		"column_name", "new_column_name", "database_name", "foreign_table", "any_name", 
		"complex_name"
	};

	private static final String[] _LITERAL_NAMES = {
		null, "';'", "'('", "','", "')'", "'='", "'*'", "'.'", "'<'", "'<='", 
		"'>'", "'>='", "'=='", "'!='", "'+'", "'-'", null, null, null, null, "'!'"
	};
	private static final String[] _SYMBOLIC_NAMES = {
		null, null, null, null, null, null, null, null, null, null, null, null, 
		null, null, null, null, "K_USE", "K_CURRENT_DATABASE", "K_LIST_DATABASES", 
		"K_GET_STRUCTURE", "K_NOT_TO_ENCRYPT", "K_BOOL", "K_DATETIME", "K_DURATION", 
		"K_INT", "K_DECIMAL", "K_DOUBLE", "K_TEXT", "K_ENCRYPTED", "K_RANGE", 
		"K_ADD", "K_ALL", "K_ALTER", "K_AND", "K_ASC", "K_BY", "K_COLUMN", "K_CONSTRAINT", 
		"K_CREATE", "K_CROSS", "K_DELETE", "K_DESC", "K_DISTINCT", "K_DROP", "K_FROM", 
		"K_INNER", "K_INSERT", "K_INTO", "K_JOIN", "K_KEY", "K_LEFT", "K_LIMIT", 
		"K_NATURAL", "K_NO", "K_NOT", "K_NULL", "K_OFFSET", "K_ON", "K_OR", "K_ORDER", 
		"K_OUTER", "K_PRIMARY", "K_REFERENCES", "K_RENAME", "K_SELECT", "K_SET", 
		"K_TABLE", "K_TO", "K_UNIQUE", "K_UPDATE", "K_USING", "K_VALUES", "K_WHERE", 
		"IDENTIFIER", "NUMERIC_LITERAL", "BIND_PARAMETER", "STRING_LITERAL", "UNEXPECTED_CHAR"
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
			setState(86);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << T__0) | (1L << K_USE) | (1L << K_CURRENT_DATABASE) | (1L << K_LIST_DATABASES) | (1L << K_ALTER) | (1L << K_CREATE) | (1L << K_DELETE) | (1L << K_DROP) | (1L << K_INSERT))) != 0) || ((((_la - 64)) & ~0x3f) == 0 && ((1L << (_la - 64)) & ((1L << (K_SELECT - 64)) | (1L << (K_UPDATE - 64)) | (1L << (UNEXPECTED_CHAR - 64)))) != 0)) {
				{
				setState(84);
				_errHandler.sync(this);
				switch (_input.LA(1)) {
				case T__0:
				case K_USE:
				case K_CURRENT_DATABASE:
				case K_LIST_DATABASES:
				case K_ALTER:
				case K_CREATE:
				case K_DELETE:
				case K_DROP:
				case K_INSERT:
				case K_SELECT:
				case K_UPDATE:
					{
					setState(82);
					sql_stmt_list();
					}
					break;
				case UNEXPECTED_CHAR:
					{
					setState(83);
					error();
					}
					break;
				default:
					throw new NoViableAltException(this);
				}
				}
				setState(88);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(89);
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
			setState(91);
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
		int _la;
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(97);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__0) {
				{
				{
				setState(94);
				match(T__0);
				}
				}
				setState(99);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(100);
			sql_stmt();
			setState(109);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,4,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					{
					{
					setState(102); 
					_errHandler.sync(this);
					_la = _input.LA(1);
					do {
						{
						{
						setState(101);
						match(T__0);
						}
						}
						setState(104); 
						_errHandler.sync(this);
						_la = _input.LA(1);
					} while ( _la==T__0 );
					setState(106);
					sql_stmt();
					}
					} 
				}
				setState(111);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,4,_ctx);
			}
			setState(115);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,5,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					{
					{
					setState(112);
					match(T__0);
					}
					} 
				}
				setState(117);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,5,_ctx);
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
			setState(128);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case K_ALTER:
				{
				setState(118);
				alter_table_stmt();
				}
				break;
			case K_CREATE:
				{
				setState(119);
				create_table_stmt();
				}
				break;
			case K_DROP:
				{
				setState(120);
				drop_table_stmt();
				}
				break;
			case K_INSERT:
				{
				setState(121);
				insert_stmt();
				}
				break;
			case K_UPDATE:
				{
				setState(122);
				update_stmt();
				}
				break;
			case K_DELETE:
				{
				setState(123);
				delete_stmt();
				}
				break;
			case K_SELECT:
				{
				setState(124);
				simple_select_stmt();
				}
				break;
			case K_USE:
				{
				setState(125);
				use_database_stmt();
				}
				break;
			case K_CURRENT_DATABASE:
				{
				setState(126);
				current_database_stmt();
				}
				break;
			case K_LIST_DATABASES:
				{
				setState(127);
				list_databases_stmt();
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
			setState(130);
			match(K_USE);
			setState(131);
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
			setState(133);
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
			setState(135);
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
			setState(137);
			match(K_GET_STRUCTURE);
			setState(138);
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
		enterRule(_localctx, 16, RULE_create_table_stmt);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(140);
			match(K_CREATE);
			setState(141);
			match(K_TABLE);
			setState(142);
			table_name();
			{
			setState(143);
			match(T__1);
			setState(144);
			column_def();
			setState(149);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__2) {
				{
				{
				setState(145);
				match(T__2);
				setState(146);
				column_def();
				}
				}
				setState(151);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(152);
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
		enterRule(_localctx, 18, RULE_alter_table_stmt);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(154);
			match(K_ALTER);
			setState(155);
			match(K_TABLE);
			setState(156);
			table_name();
			setState(171);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,8,_ctx) ) {
			case 1:
				{
				setState(157);
				match(K_RENAME);
				setState(158);
				match(K_TO);
				setState(159);
				new_table_name();
				}
				break;
			case 2:
				{
				setState(160);
				match(K_ADD);
				setState(161);
				match(K_COLUMN);
				setState(162);
				column_def();
				}
				break;
			case 3:
				{
				setState(163);
				match(K_DROP);
				setState(164);
				match(K_COLUMN);
				setState(165);
				column_name();
				}
				break;
			case 4:
				{
				setState(166);
				match(K_RENAME);
				setState(167);
				column_name();
				setState(168);
				match(K_TO);
				setState(169);
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
		enterRule(_localctx, 20, RULE_drop_table_stmt);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(173);
			match(K_DROP);
			setState(174);
			match(K_TABLE);
			setState(175);
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
		public List<ExprContext> expr() {
			return getRuleContexts(ExprContext.class);
		}
		public ExprContext expr(int i) {
			return getRuleContext(ExprContext.class,i);
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
		enterRule(_localctx, 22, RULE_insert_stmt);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(177);
			match(K_INSERT);
			setState(178);
			match(K_INTO);
			setState(179);
			table_name();
			setState(191);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==T__1) {
				{
				setState(180);
				match(T__1);
				setState(181);
				column_name();
				setState(186);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==T__2) {
					{
					{
					setState(182);
					match(T__2);
					setState(183);
					column_name();
					}
					}
					setState(188);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(189);
				match(T__3);
				}
			}

			{
			setState(193);
			match(K_VALUES);
			setState(194);
			match(T__1);
			setState(195);
			expr(0);
			setState(200);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__2) {
				{
				{
				setState(196);
				match(T__2);
				setState(197);
				expr(0);
				}
				}
				setState(202);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(203);
			match(T__3);
			setState(218);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__2) {
				{
				{
				setState(204);
				match(T__2);
				setState(205);
				match(T__1);
				setState(206);
				expr(0);
				setState(211);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==T__2) {
					{
					{
					setState(207);
					match(T__2);
					setState(208);
					expr(0);
					}
					}
					setState(213);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(214);
				match(T__3);
				}
				}
				setState(220);
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
		public List<ExprContext> expr() {
			return getRuleContexts(ExprContext.class);
		}
		public ExprContext expr(int i) {
			return getRuleContext(ExprContext.class,i);
		}
		public TerminalNode K_WHERE() { return getToken(BareBonesSqlParser.K_WHERE, 0); }
		public Update_stmtContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_update_stmt; }
	}

	public final Update_stmtContext update_stmt() throws RecognitionException {
		Update_stmtContext _localctx = new Update_stmtContext(_ctx, getState());
		enterRule(_localctx, 24, RULE_update_stmt);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(221);
			match(K_UPDATE);
			setState(222);
			table_name();
			setState(223);
			match(K_SET);
			setState(224);
			column_name();
			setState(225);
			match(T__4);
			setState(226);
			expr(0);
			setState(234);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__2) {
				{
				{
				setState(227);
				match(T__2);
				setState(228);
				column_name();
				setState(229);
				match(T__4);
				setState(230);
				expr(0);
				}
				}
				setState(236);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(239);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_WHERE) {
				{
				setState(237);
				match(K_WHERE);
				setState(238);
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
		enterRule(_localctx, 26, RULE_delete_stmt);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(241);
			match(K_DELETE);
			setState(242);
			match(K_FROM);
			setState(243);
			table_name();
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
		public List<ExprContext> expr() {
			return getRuleContexts(ExprContext.class);
		}
		public ExprContext expr(int i) {
			return getRuleContext(ExprContext.class,i);
		}
		public TerminalNode K_OFFSET() { return getToken(BareBonesSqlParser.K_OFFSET, 0); }
		public Simple_select_stmtContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_simple_select_stmt; }
	}

	public final Simple_select_stmtContext simple_select_stmt() throws RecognitionException {
		Simple_select_stmtContext _localctx = new Simple_select_stmtContext(_ctx, getState());
		enterRule(_localctx, 28, RULE_simple_select_stmt);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(248);
			select_core();
			setState(259);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_ORDER) {
				{
				setState(249);
				match(K_ORDER);
				setState(250);
				match(K_BY);
				setState(251);
				ordering_term();
				setState(256);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==T__2) {
					{
					{
					setState(252);
					match(T__2);
					setState(253);
					ordering_term();
					}
					}
					setState(258);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				}
			}

			setState(267);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_LIMIT) {
				{
				setState(261);
				match(K_LIMIT);
				setState(262);
				expr(0);
				setState(265);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if (_la==T__2 || _la==K_OFFSET) {
					{
					setState(263);
					_la = _input.LA(1);
					if ( !(_la==T__2 || _la==K_OFFSET) ) {
					_errHandler.recoverInline(this);
					}
					else {
						if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
						_errHandler.reportMatch(this);
						consume();
					}
					setState(264);
					expr(0);
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
		public TerminalNode K_WHERE() { return getToken(BareBonesSqlParser.K_WHERE, 0); }
		public ExprContext expr() {
			return getRuleContext(ExprContext.class,0);
		}
		public TerminalNode K_DISTINCT() { return getToken(BareBonesSqlParser.K_DISTINCT, 0); }
		public TerminalNode K_ALL() { return getToken(BareBonesSqlParser.K_ALL, 0); }
		public List<Table_or_subqueryContext> table_or_subquery() {
			return getRuleContexts(Table_or_subqueryContext.class);
		}
		public Table_or_subqueryContext table_or_subquery(int i) {
			return getRuleContext(Table_or_subqueryContext.class,i);
		}
		public Join_clauseContext join_clause() {
			return getRuleContext(Join_clauseContext.class,0);
		}
		public Select_coreContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_select_core; }
	}

	public final Select_coreContext select_core() throws RecognitionException {
		Select_coreContext _localctx = new Select_coreContext(_ctx, getState());
		enterRule(_localctx, 30, RULE_select_core);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(269);
			match(K_SELECT);
			setState(271);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,21,_ctx) ) {
			case 1:
				{
				setState(270);
				_la = _input.LA(1);
				if ( !(_la==K_ALL || _la==K_DISTINCT) ) {
				_errHandler.recoverInline(this);
				}
				else {
					if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
					_errHandler.reportMatch(this);
					consume();
				}
				}
				break;
			}
			setState(273);
			result_column();
			setState(278);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__2) {
				{
				{
				setState(274);
				match(T__2);
				setState(275);
				result_column();
				}
				}
				setState(280);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(293);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_FROM) {
				{
				setState(281);
				match(K_FROM);
				setState(291);
				_errHandler.sync(this);
				switch ( getInterpreter().adaptivePredict(_input,24,_ctx) ) {
				case 1:
					{
					setState(282);
					table_or_subquery();
					setState(287);
					_errHandler.sync(this);
					_la = _input.LA(1);
					while (_la==T__2) {
						{
						{
						setState(283);
						match(T__2);
						setState(284);
						table_or_subquery();
						}
						}
						setState(289);
						_errHandler.sync(this);
						_la = _input.LA(1);
					}
					}
					break;
				case 2:
					{
					setState(290);
					join_clause();
					}
					break;
				}
				}
			}

			setState(297);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_WHERE) {
				{
				setState(295);
				match(K_WHERE);
				setState(296);
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
		enterRule(_localctx, 32, RULE_ordering_term);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(299);
			expr(0);
			setState(301);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_ASC || _la==K_DESC) {
				{
				setState(300);
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
		public Column_nameContext column_name() {
			return getRuleContext(Column_nameContext.class,0);
		}
		public Result_columnContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_result_column; }
	}

	public final Result_columnContext result_column() throws RecognitionException {
		Result_columnContext _localctx = new Result_columnContext(_ctx, getState());
		enterRule(_localctx, 34, RULE_result_column);
		try {
			setState(313);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,28,_ctx) ) {
			case 1:
				enterOuterAlt(_localctx, 1);
				{
				setState(303);
				match(T__5);
				}
				break;
			case 2:
				enterOuterAlt(_localctx, 2);
				{
				setState(304);
				table_name();
				setState(305);
				match(T__6);
				setState(306);
				match(T__5);
				}
				break;
			case 3:
				enterOuterAlt(_localctx, 3);
				{
				setState(308);
				column_name();
				}
				break;
			case 4:
				enterOuterAlt(_localctx, 4);
				{
				setState(309);
				table_name();
				setState(310);
				match(T__6);
				setState(311);
				column_name();
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
		enterRule(_localctx, 36, RULE_table_or_subquery);
		int _la;
		try {
			setState(334);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,31,_ctx) ) {
			case 1:
				enterOuterAlt(_localctx, 1);
				{
				setState(315);
				table_name();
				}
				break;
			case 2:
				enterOuterAlt(_localctx, 2);
				{
				setState(316);
				match(T__1);
				setState(326);
				_errHandler.sync(this);
				switch ( getInterpreter().adaptivePredict(_input,30,_ctx) ) {
				case 1:
					{
					setState(317);
					table_or_subquery();
					setState(322);
					_errHandler.sync(this);
					_la = _input.LA(1);
					while (_la==T__2) {
						{
						{
						setState(318);
						match(T__2);
						setState(319);
						table_or_subquery();
						}
						}
						setState(324);
						_errHandler.sync(this);
						_la = _input.LA(1);
					}
					}
					break;
				case 2:
					{
					setState(325);
					join_clause();
					}
					break;
				}
				setState(328);
				match(T__3);
				}
				break;
			case 3:
				enterOuterAlt(_localctx, 3);
				{
				setState(330);
				match(T__1);
				setState(331);
				simple_select_stmt();
				setState(332);
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
		enterRule(_localctx, 38, RULE_join_clause);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(336);
			table_or_subquery();
			setState(343);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << T__2) | (1L << K_CROSS) | (1L << K_INNER) | (1L << K_JOIN) | (1L << K_LEFT) | (1L << K_NATURAL))) != 0)) {
				{
				{
				setState(337);
				join_operator();
				setState(338);
				table_or_subquery();
				setState(339);
				join_constraint();
				}
				}
				setState(345);
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
		enterRule(_localctx, 40, RULE_join_operator);
		int _la;
		try {
			setState(359);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case T__2:
				enterOuterAlt(_localctx, 1);
				{
				setState(346);
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
				setState(348);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if (_la==K_NATURAL) {
					{
					setState(347);
					match(K_NATURAL);
					}
				}

				setState(356);
				_errHandler.sync(this);
				switch (_input.LA(1)) {
				case K_LEFT:
					{
					setState(350);
					match(K_LEFT);
					setState(352);
					_errHandler.sync(this);
					_la = _input.LA(1);
					if (_la==K_OUTER) {
						{
						setState(351);
						match(K_OUTER);
						}
					}

					}
					break;
				case K_INNER:
					{
					setState(354);
					match(K_INNER);
					}
					break;
				case K_CROSS:
					{
					setState(355);
					match(K_CROSS);
					}
					break;
				case K_JOIN:
					break;
				default:
					break;
				}
				setState(358);
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
		public TerminalNode K_USING() { return getToken(BareBonesSqlParser.K_USING, 0); }
		public List<Column_nameContext> column_name() {
			return getRuleContexts(Column_nameContext.class);
		}
		public Column_nameContext column_name(int i) {
			return getRuleContext(Column_nameContext.class,i);
		}
		public Join_constraintContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_join_constraint; }
	}

	public final Join_constraintContext join_constraint() throws RecognitionException {
		Join_constraintContext _localctx = new Join_constraintContext(_ctx, getState());
		enterRule(_localctx, 42, RULE_join_constraint);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(375);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case K_ON:
				{
				setState(361);
				match(K_ON);
				setState(362);
				expr(0);
				}
				break;
			case K_USING:
				{
				setState(363);
				match(K_USING);
				setState(364);
				match(T__1);
				setState(365);
				column_name();
				setState(370);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==T__2) {
					{
					{
					setState(366);
					match(T__2);
					setState(367);
					column_name();
					}
					}
					setState(372);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(373);
				match(T__3);
				}
				break;
			case EOF:
			case T__0:
			case T__2:
			case T__3:
			case K_USE:
			case K_CURRENT_DATABASE:
			case K_LIST_DATABASES:
			case K_ALTER:
			case K_CREATE:
			case K_CROSS:
			case K_DELETE:
			case K_DROP:
			case K_INNER:
			case K_INSERT:
			case K_JOIN:
			case K_LEFT:
			case K_LIMIT:
			case K_NATURAL:
			case K_ORDER:
			case K_SELECT:
			case K_UPDATE:
			case K_WHERE:
			case UNEXPECTED_CHAR:
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
		enterRule(_localctx, 44, RULE_column_def);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(377);
			column_name();
			setState(378);
			data_type();
			setState(382);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (((((_la - 37)) & ~0x3f) == 0 && ((1L << (_la - 37)) & ((1L << (K_CONSTRAINT - 37)) | (1L << (K_NOT - 37)) | (1L << (K_NULL - 37)) | (1L << (K_PRIMARY - 37)) | (1L << (K_REFERENCES - 37)) | (1L << (K_UNIQUE - 37)))) != 0)) {
				{
				{
				setState(379);
				column_constraint();
				}
				}
				setState(384);
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
		public Bucket_sizeContext bucket_size() {
			return getRuleContext(Bucket_sizeContext.class,0);
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
		enterRule(_localctx, 46, RULE_data_type);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(398);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case K_BOOL:
				{
				setState(385);
				match(K_BOOL);
				}
				break;
			case K_DATETIME:
				{
				setState(386);
				match(K_DATETIME);
				}
				break;
			case K_DURATION:
				{
				setState(387);
				match(K_DURATION);
				}
				break;
			case K_INT:
				{
				setState(388);
				match(K_INT);
				}
				break;
			case K_DECIMAL:
				{
				setState(389);
				match(K_DECIMAL);
				}
				break;
			case K_DOUBLE:
				{
				setState(390);
				match(K_DOUBLE);
				}
				break;
			case K_TEXT:
				{
				setState(391);
				match(K_TEXT);
				}
				break;
			case K_ENCRYPTED:
				{
				setState(392);
				match(K_ENCRYPTED);
				setState(393);
				bucket_size();
				setState(396);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if (_la==K_RANGE) {
					{
					setState(394);
					match(K_RANGE);
					setState(395);
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

	public static class Bucket_sizeContext extends ParserRuleContext {
		public TerminalNode NUMERIC_LITERAL() { return getToken(BareBonesSqlParser.NUMERIC_LITERAL, 0); }
		public Bucket_sizeContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_bucket_size; }
	}

	public final Bucket_sizeContext bucket_size() throws RecognitionException {
		Bucket_sizeContext _localctx = new Bucket_sizeContext(_ctx, getState());
		enterRule(_localctx, 48, RULE_bucket_size);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(400);
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
		enterRule(_localctx, 50, RULE_bucket_range);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(402);
			match(T__1);
			setState(403);
			match(NUMERIC_LITERAL);
			setState(404);
			match(T__2);
			setState(405);
			match(NUMERIC_LITERAL);
			setState(406);
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
		enterRule(_localctx, 52, RULE_column_constraint);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(410);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_CONSTRAINT) {
				{
				setState(408);
				match(K_CONSTRAINT);
				setState(409);
				name();
				}
			}

			setState(420);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case K_PRIMARY:
				{
				setState(412);
				match(K_PRIMARY);
				setState(413);
				match(K_KEY);
				}
				break;
			case K_NOT:
			case K_NULL:
				{
				setState(415);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if (_la==K_NOT) {
					{
					setState(414);
					match(K_NOT);
					}
				}

				setState(417);
				match(K_NULL);
				}
				break;
			case K_UNIQUE:
				{
				setState(418);
				match(K_UNIQUE);
				}
				break;
			case K_REFERENCES:
				{
				setState(419);
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
		public Literal_valueContext literal_value() {
			return getRuleContext(Literal_valueContext.class,0);
		}
		public Column_nameContext column_name() {
			return getRuleContext(Column_nameContext.class,0);
		}
		public Table_nameContext table_name() {
			return getRuleContext(Table_nameContext.class,0);
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
		int _startState = 54;
		enterRecursionRule(_localctx, 54, RULE_expr, _p);
		int _la;
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(434);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,46,_ctx) ) {
			case 1:
				{
				setState(423);
				literal_value();
				}
				break;
			case 2:
				{
				setState(427);
				_errHandler.sync(this);
				switch ( getInterpreter().adaptivePredict(_input,45,_ctx) ) {
				case 1:
					{
					setState(424);
					table_name();
					setState(425);
					match(T__6);
					}
					break;
				}
				setState(429);
				column_name();
				}
				break;
			case 3:
				{
				setState(430);
				match(T__1);
				setState(431);
				expr(0);
				setState(432);
				match(T__3);
				}
				break;
			}
			_ctx.stop = _input.LT(-1);
			setState(444);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,48,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					if ( _parseListeners!=null ) triggerExitRuleEvent();
					_prevctx = _localctx;
					{
					setState(442);
					_errHandler.sync(this);
					switch ( getInterpreter().adaptivePredict(_input,47,_ctx) ) {
					case 1:
						{
						_localctx = new ExprContext(_parentctx, _parentState);
						pushNewRecursionContext(_localctx, _startState, RULE_expr);
						setState(436);
						if (!(precpred(_ctx, 3))) throw new FailedPredicateException(this, "precpred(_ctx, 3)");
						setState(437);
						_la = _input.LA(1);
						if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << T__7) | (1L << T__8) | (1L << T__9) | (1L << T__10) | (1L << T__11) | (1L << T__12))) != 0)) ) {
						_errHandler.recoverInline(this);
						}
						else {
							if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
							_errHandler.reportMatch(this);
							consume();
						}
						setState(438);
						expr(4);
						}
						break;
					case 2:
						{
						_localctx = new ExprContext(_parentctx, _parentState);
						pushNewRecursionContext(_localctx, _startState, RULE_expr);
						setState(439);
						if (!(precpred(_ctx, 2))) throw new FailedPredicateException(this, "precpred(_ctx, 2)");
						setState(440);
						_la = _input.LA(1);
						if ( !(_la==K_AND || _la==K_OR) ) {
						_errHandler.recoverInline(this);
						}
						else {
							if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
							_errHandler.reportMatch(this);
							consume();
						}
						setState(441);
						expr(3);
						}
						break;
					}
					} 
				}
				setState(446);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,48,_ctx);
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
		enterRule(_localctx, 56, RULE_foreign_key_clause);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(447);
			match(K_REFERENCES);
			setState(448);
			foreign_table();
			setState(460);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==T__1) {
				{
				setState(449);
				match(T__1);
				setState(450);
				column_name();
				setState(455);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==T__2) {
					{
					{
					setState(451);
					match(T__2);
					setState(452);
					column_name();
					}
					}
					setState(457);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(458);
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
		enterRule(_localctx, 58, RULE_signed_number);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(463);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==T__13 || _la==T__14) {
				{
				setState(462);
				_la = _input.LA(1);
				if ( !(_la==T__13 || _la==T__14) ) {
				_errHandler.recoverInline(this);
				}
				else {
					if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
					_errHandler.reportMatch(this);
					consume();
				}
				}
			}

			setState(465);
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
		enterRule(_localctx, 60, RULE_literal_value);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(467);
			_la = _input.LA(1);
			if ( !(((((_la - 55)) & ~0x3f) == 0 && ((1L << (_la - 55)) & ((1L << (K_NULL - 55)) | (1L << (NUMERIC_LITERAL - 55)) | (1L << (STRING_LITERAL - 55)))) != 0)) ) {
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
		public TerminalNode K_DELETE() { return getToken(BareBonesSqlParser.K_DELETE, 0); }
		public TerminalNode K_DESC() { return getToken(BareBonesSqlParser.K_DESC, 0); }
		public TerminalNode K_DISTINCT() { return getToken(BareBonesSqlParser.K_DISTINCT, 0); }
		public TerminalNode K_DROP() { return getToken(BareBonesSqlParser.K_DROP, 0); }
		public TerminalNode K_FROM() { return getToken(BareBonesSqlParser.K_FROM, 0); }
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
		enterRule(_localctx, 62, RULE_keyword);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(469);
			_la = _input.LA(1);
			if ( !(((((_la - 16)) & ~0x3f) == 0 && ((1L << (_la - 16)) & ((1L << (K_USE - 16)) | (1L << (K_CURRENT_DATABASE - 16)) | (1L << (K_LIST_DATABASES - 16)) | (1L << (K_GET_STRUCTURE - 16)) | (1L << (K_NOT_TO_ENCRYPT - 16)) | (1L << (K_BOOL - 16)) | (1L << (K_DATETIME - 16)) | (1L << (K_DURATION - 16)) | (1L << (K_INT - 16)) | (1L << (K_DECIMAL - 16)) | (1L << (K_DOUBLE - 16)) | (1L << (K_TEXT - 16)) | (1L << (K_ENCRYPTED - 16)) | (1L << (K_RANGE - 16)) | (1L << (K_ADD - 16)) | (1L << (K_ALL - 16)) | (1L << (K_ALTER - 16)) | (1L << (K_AND - 16)) | (1L << (K_ASC - 16)) | (1L << (K_BY - 16)) | (1L << (K_COLUMN - 16)) | (1L << (K_CONSTRAINT - 16)) | (1L << (K_CREATE - 16)) | (1L << (K_CROSS - 16)) | (1L << (K_DELETE - 16)) | (1L << (K_DESC - 16)) | (1L << (K_DISTINCT - 16)) | (1L << (K_DROP - 16)) | (1L << (K_FROM - 16)) | (1L << (K_INNER - 16)) | (1L << (K_INSERT - 16)) | (1L << (K_INTO - 16)) | (1L << (K_JOIN - 16)) | (1L << (K_KEY - 16)) | (1L << (K_LEFT - 16)) | (1L << (K_LIMIT - 16)) | (1L << (K_NATURAL - 16)) | (1L << (K_NO - 16)) | (1L << (K_NOT - 16)) | (1L << (K_NULL - 16)) | (1L << (K_OFFSET - 16)) | (1L << (K_ON - 16)) | (1L << (K_OR - 16)) | (1L << (K_ORDER - 16)) | (1L << (K_OUTER - 16)) | (1L << (K_PRIMARY - 16)) | (1L << (K_REFERENCES - 16)) | (1L << (K_RENAME - 16)) | (1L << (K_SELECT - 16)) | (1L << (K_SET - 16)) | (1L << (K_TABLE - 16)) | (1L << (K_TO - 16)) | (1L << (K_UNIQUE - 16)) | (1L << (K_UPDATE - 16)) | (1L << (K_USING - 16)) | (1L << (K_VALUES - 16)) | (1L << (K_WHERE - 16)))) != 0)) ) {
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
		enterRule(_localctx, 64, RULE_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(471);
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
		enterRule(_localctx, 66, RULE_table_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(473);
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
		enterRule(_localctx, 68, RULE_new_table_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(475);
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
		enterRule(_localctx, 70, RULE_column_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(477);
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
		enterRule(_localctx, 72, RULE_new_column_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(479);
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
		enterRule(_localctx, 74, RULE_database_name);
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
		enterRule(_localctx, 76, RULE_foreign_table);
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
		enterRule(_localctx, 78, RULE_any_name);
		try {
			setState(492);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case IDENTIFIER:
				enterOuterAlt(_localctx, 1);
				{
				setState(485);
				match(IDENTIFIER);
				}
				break;
			case K_USE:
			case K_CURRENT_DATABASE:
			case K_LIST_DATABASES:
			case K_GET_STRUCTURE:
			case K_NOT_TO_ENCRYPT:
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
				setState(486);
				keyword();
				}
				break;
			case STRING_LITERAL:
				enterOuterAlt(_localctx, 3);
				{
				setState(487);
				match(STRING_LITERAL);
				}
				break;
			case T__1:
				enterOuterAlt(_localctx, 4);
				{
				setState(488);
				match(T__1);
				setState(489);
				any_name();
				setState(490);
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
		enterRule(_localctx, 80, RULE_complex_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(495);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,53,_ctx) ) {
			case 1:
				{
				setState(494);
				match(K_NOT_TO_ENCRYPT);
				}
				break;
			}
			setState(497);
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
		case 27:
			return expr_sempred((ExprContext)_localctx, predIndex);
		}
		return true;
	}
	private boolean expr_sempred(ExprContext _localctx, int predIndex) {
		switch (predIndex) {
		case 0:
			return precpred(_ctx, 3);
		case 1:
			return precpred(_ctx, 2);
		}
		return true;
	}

	public static final String _serializedATN =
		"\3\u608b\ua72a\u8133\ub9ed\u417c\u3be7\u7786\u5964\3O\u01f6\4\2\t\2\4"+
		"\3\t\3\4\4\t\4\4\5\t\5\4\6\t\6\4\7\t\7\4\b\t\b\4\t\t\t\4\n\t\n\4\13\t"+
		"\13\4\f\t\f\4\r\t\r\4\16\t\16\4\17\t\17\4\20\t\20\4\21\t\21\4\22\t\22"+
		"\4\23\t\23\4\24\t\24\4\25\t\25\4\26\t\26\4\27\t\27\4\30\t\30\4\31\t\31"+
		"\4\32\t\32\4\33\t\33\4\34\t\34\4\35\t\35\4\36\t\36\4\37\t\37\4 \t \4!"+
		"\t!\4\"\t\"\4#\t#\4$\t$\4%\t%\4&\t&\4\'\t\'\4(\t(\4)\t)\4*\t*\3\2\3\2"+
		"\7\2W\n\2\f\2\16\2Z\13\2\3\2\3\2\3\3\3\3\3\3\3\4\7\4b\n\4\f\4\16\4e\13"+
		"\4\3\4\3\4\6\4i\n\4\r\4\16\4j\3\4\7\4n\n\4\f\4\16\4q\13\4\3\4\7\4t\n\4"+
		"\f\4\16\4w\13\4\3\5\3\5\3\5\3\5\3\5\3\5\3\5\3\5\3\5\3\5\5\5\u0083\n\5"+
		"\3\6\3\6\3\6\3\7\3\7\3\b\3\b\3\t\3\t\3\t\3\n\3\n\3\n\3\n\3\n\3\n\3\n\7"+
		"\n\u0096\n\n\f\n\16\n\u0099\13\n\3\n\3\n\3\13\3\13\3\13\3\13\3\13\3\13"+
		"\3\13\3\13\3\13\3\13\3\13\3\13\3\13\3\13\3\13\3\13\3\13\5\13\u00ae\n\13"+
		"\3\f\3\f\3\f\3\f\3\r\3\r\3\r\3\r\3\r\3\r\3\r\7\r\u00bb\n\r\f\r\16\r\u00be"+
		"\13\r\3\r\3\r\5\r\u00c2\n\r\3\r\3\r\3\r\3\r\3\r\7\r\u00c9\n\r\f\r\16\r"+
		"\u00cc\13\r\3\r\3\r\3\r\3\r\3\r\3\r\7\r\u00d4\n\r\f\r\16\r\u00d7\13\r"+
		"\3\r\3\r\7\r\u00db\n\r\f\r\16\r\u00de\13\r\3\16\3\16\3\16\3\16\3\16\3"+
		"\16\3\16\3\16\3\16\3\16\3\16\7\16\u00eb\n\16\f\16\16\16\u00ee\13\16\3"+
		"\16\3\16\5\16\u00f2\n\16\3\17\3\17\3\17\3\17\3\17\5\17\u00f9\n\17\3\20"+
		"\3\20\3\20\3\20\3\20\3\20\7\20\u0101\n\20\f\20\16\20\u0104\13\20\5\20"+
		"\u0106\n\20\3\20\3\20\3\20\3\20\5\20\u010c\n\20\5\20\u010e\n\20\3\21\3"+
		"\21\5\21\u0112\n\21\3\21\3\21\3\21\7\21\u0117\n\21\f\21\16\21\u011a\13"+
		"\21\3\21\3\21\3\21\3\21\7\21\u0120\n\21\f\21\16\21\u0123\13\21\3\21\5"+
		"\21\u0126\n\21\5\21\u0128\n\21\3\21\3\21\5\21\u012c\n\21\3\22\3\22\5\22"+
		"\u0130\n\22\3\23\3\23\3\23\3\23\3\23\3\23\3\23\3\23\3\23\3\23\5\23\u013c"+
		"\n\23\3\24\3\24\3\24\3\24\3\24\7\24\u0143\n\24\f\24\16\24\u0146\13\24"+
		"\3\24\5\24\u0149\n\24\3\24\3\24\3\24\3\24\3\24\3\24\5\24\u0151\n\24\3"+
		"\25\3\25\3\25\3\25\3\25\7\25\u0158\n\25\f\25\16\25\u015b\13\25\3\26\3"+
		"\26\5\26\u015f\n\26\3\26\3\26\5\26\u0163\n\26\3\26\3\26\5\26\u0167\n\26"+
		"\3\26\5\26\u016a\n\26\3\27\3\27\3\27\3\27\3\27\3\27\3\27\7\27\u0173\n"+
		"\27\f\27\16\27\u0176\13\27\3\27\3\27\5\27\u017a\n\27\3\30\3\30\3\30\7"+
		"\30\u017f\n\30\f\30\16\30\u0182\13\30\3\31\3\31\3\31\3\31\3\31\3\31\3"+
		"\31\3\31\3\31\3\31\3\31\5\31\u018f\n\31\5\31\u0191\n\31\3\32\3\32\3\33"+
		"\3\33\3\33\3\33\3\33\3\33\3\34\3\34\5\34\u019d\n\34\3\34\3\34\3\34\5\34"+
		"\u01a2\n\34\3\34\3\34\3\34\5\34\u01a7\n\34\3\35\3\35\3\35\3\35\3\35\5"+
		"\35\u01ae\n\35\3\35\3\35\3\35\3\35\3\35\5\35\u01b5\n\35\3\35\3\35\3\35"+
		"\3\35\3\35\3\35\7\35\u01bd\n\35\f\35\16\35\u01c0\13\35\3\36\3\36\3\36"+
		"\3\36\3\36\3\36\7\36\u01c8\n\36\f\36\16\36\u01cb\13\36\3\36\3\36\5\36"+
		"\u01cf\n\36\3\37\5\37\u01d2\n\37\3\37\3\37\3 \3 \3!\3!\3\"\3\"\3#\3#\3"+
		"$\3$\3%\3%\3&\3&\3\'\3\'\3(\3(\3)\3)\3)\3)\3)\3)\3)\5)\u01ef\n)\3*\5*"+
		"\u01f2\n*\3*\3*\3*\2\38+\2\4\6\b\n\f\16\20\22\24\26\30\32\34\36 \"$&("+
		"*,.\60\62\64\668:<>@BDFHJLNPR\2\n\4\2\5\5::\4\2!!,,\4\2$$++\3\2\n\17\4"+
		"\2##<<\3\2\20\21\5\299LLNN\3\2\22J\2\u021d\2X\3\2\2\2\4]\3\2\2\2\6c\3"+
		"\2\2\2\b\u0082\3\2\2\2\n\u0084\3\2\2\2\f\u0087\3\2\2\2\16\u0089\3\2\2"+
		"\2\20\u008b\3\2\2\2\22\u008e\3\2\2\2\24\u009c\3\2\2\2\26\u00af\3\2\2\2"+
		"\30\u00b3\3\2\2\2\32\u00df\3\2\2\2\34\u00f3\3\2\2\2\36\u00fa\3\2\2\2 "+
		"\u010f\3\2\2\2\"\u012d\3\2\2\2$\u013b\3\2\2\2&\u0150\3\2\2\2(\u0152\3"+
		"\2\2\2*\u0169\3\2\2\2,\u0179\3\2\2\2.\u017b\3\2\2\2\60\u0190\3\2\2\2\62"+
		"\u0192\3\2\2\2\64\u0194\3\2\2\2\66\u019c\3\2\2\28\u01b4\3\2\2\2:\u01c1"+
		"\3\2\2\2<\u01d1\3\2\2\2>\u01d5\3\2\2\2@\u01d7\3\2\2\2B\u01d9\3\2\2\2D"+
		"\u01db\3\2\2\2F\u01dd\3\2\2\2H\u01df\3\2\2\2J\u01e1\3\2\2\2L\u01e3\3\2"+
		"\2\2N\u01e5\3\2\2\2P\u01ee\3\2\2\2R\u01f1\3\2\2\2TW\5\6\4\2UW\5\4\3\2"+
		"VT\3\2\2\2VU\3\2\2\2WZ\3\2\2\2XV\3\2\2\2XY\3\2\2\2Y[\3\2\2\2ZX\3\2\2\2"+
		"[\\\7\2\2\3\\\3\3\2\2\2]^\7O\2\2^_\b\3\1\2_\5\3\2\2\2`b\7\3\2\2a`\3\2"+
		"\2\2be\3\2\2\2ca\3\2\2\2cd\3\2\2\2df\3\2\2\2ec\3\2\2\2fo\5\b\5\2gi\7\3"+
		"\2\2hg\3\2\2\2ij\3\2\2\2jh\3\2\2\2jk\3\2\2\2kl\3\2\2\2ln\5\b\5\2mh\3\2"+
		"\2\2nq\3\2\2\2om\3\2\2\2op\3\2\2\2pu\3\2\2\2qo\3\2\2\2rt\7\3\2\2sr\3\2"+
		"\2\2tw\3\2\2\2us\3\2\2\2uv\3\2\2\2v\7\3\2\2\2wu\3\2\2\2x\u0083\5\24\13"+
		"\2y\u0083\5\22\n\2z\u0083\5\26\f\2{\u0083\5\30\r\2|\u0083\5\32\16\2}\u0083"+
		"\5\34\17\2~\u0083\5\36\20\2\177\u0083\5\n\6\2\u0080\u0083\5\f\7\2\u0081"+
		"\u0083\5\16\b\2\u0082x\3\2\2\2\u0082y\3\2\2\2\u0082z\3\2\2\2\u0082{\3"+
		"\2\2\2\u0082|\3\2\2\2\u0082}\3\2\2\2\u0082~\3\2\2\2\u0082\177\3\2\2\2"+
		"\u0082\u0080\3\2\2\2\u0082\u0081\3\2\2\2\u0083\t\3\2\2\2\u0084\u0085\7"+
		"\22\2\2\u0085\u0086\5L\'\2\u0086\13\3\2\2\2\u0087\u0088\7\23\2\2\u0088"+
		"\r\3\2\2\2\u0089\u008a\7\24\2\2\u008a\17\3\2\2\2\u008b\u008c\7\25\2\2"+
		"\u008c\u008d\5L\'\2\u008d\21\3\2\2\2\u008e\u008f\7(\2\2\u008f\u0090\7"+
		"D\2\2\u0090\u0091\5D#\2\u0091\u0092\7\4\2\2\u0092\u0097\5.\30\2\u0093"+
		"\u0094\7\5\2\2\u0094\u0096\5.\30\2\u0095\u0093\3\2\2\2\u0096\u0099\3\2"+
		"\2\2\u0097\u0095\3\2\2\2\u0097\u0098\3\2\2\2\u0098\u009a\3\2\2\2\u0099"+
		"\u0097\3\2\2\2\u009a\u009b\7\6\2\2\u009b\23\3\2\2\2\u009c\u009d\7\"\2"+
		"\2\u009d\u009e\7D\2\2\u009e\u00ad\5D#\2\u009f\u00a0\7A\2\2\u00a0\u00a1"+
		"\7E\2\2\u00a1\u00ae\5F$\2\u00a2\u00a3\7 \2\2\u00a3\u00a4\7&\2\2\u00a4"+
		"\u00ae\5.\30\2\u00a5\u00a6\7-\2\2\u00a6\u00a7\7&\2\2\u00a7\u00ae\5H%\2"+
		"\u00a8\u00a9\7A\2\2\u00a9\u00aa\5H%\2\u00aa\u00ab\7E\2\2\u00ab\u00ac\5"+
		"J&\2\u00ac\u00ae\3\2\2\2\u00ad\u009f\3\2\2\2\u00ad\u00a2\3\2\2\2\u00ad"+
		"\u00a5\3\2\2\2\u00ad\u00a8\3\2\2\2\u00ae\25\3\2\2\2\u00af\u00b0\7-\2\2"+
		"\u00b0\u00b1\7D\2\2\u00b1\u00b2\5D#\2\u00b2\27\3\2\2\2\u00b3\u00b4\7\60"+
		"\2\2\u00b4\u00b5\7\61\2\2\u00b5\u00c1\5D#\2\u00b6\u00b7\7\4\2\2\u00b7"+
		"\u00bc\5H%\2\u00b8\u00b9\7\5\2\2\u00b9\u00bb\5H%\2\u00ba\u00b8\3\2\2\2"+
		"\u00bb\u00be\3\2\2\2\u00bc\u00ba\3\2\2\2\u00bc\u00bd\3\2\2\2\u00bd\u00bf"+
		"\3\2\2\2\u00be\u00bc\3\2\2\2\u00bf\u00c0\7\6\2\2\u00c0\u00c2\3\2\2\2\u00c1"+
		"\u00b6\3\2\2\2\u00c1\u00c2\3\2\2\2\u00c2\u00c3\3\2\2\2\u00c3\u00c4\7I"+
		"\2\2\u00c4\u00c5\7\4\2\2\u00c5\u00ca\58\35\2\u00c6\u00c7\7\5\2\2\u00c7"+
		"\u00c9\58\35\2\u00c8\u00c6\3\2\2\2\u00c9\u00cc\3\2\2\2\u00ca\u00c8\3\2"+
		"\2\2\u00ca\u00cb\3\2\2\2\u00cb\u00cd\3\2\2\2\u00cc\u00ca\3\2\2\2\u00cd"+
		"\u00dc\7\6\2\2\u00ce\u00cf\7\5\2\2\u00cf\u00d0\7\4\2\2\u00d0\u00d5\58"+
		"\35\2\u00d1\u00d2\7\5\2\2\u00d2\u00d4\58\35\2\u00d3\u00d1\3\2\2\2\u00d4"+
		"\u00d7\3\2\2\2\u00d5\u00d3\3\2\2\2\u00d5\u00d6\3\2\2\2\u00d6\u00d8\3\2"+
		"\2\2\u00d7\u00d5\3\2\2\2\u00d8\u00d9\7\6\2\2\u00d9\u00db\3\2\2\2\u00da"+
		"\u00ce\3\2\2\2\u00db\u00de\3\2\2\2\u00dc\u00da\3\2\2\2\u00dc\u00dd\3\2"+
		"\2\2\u00dd\31\3\2\2\2\u00de\u00dc\3\2\2\2\u00df\u00e0\7G\2\2\u00e0\u00e1"+
		"\5D#\2\u00e1\u00e2\7C\2\2\u00e2\u00e3\5H%\2\u00e3\u00e4\7\7\2\2\u00e4"+
		"\u00ec\58\35\2\u00e5\u00e6\7\5\2\2\u00e6\u00e7\5H%\2\u00e7\u00e8\7\7\2"+
		"\2\u00e8\u00e9\58\35\2\u00e9\u00eb\3\2\2\2\u00ea\u00e5\3\2\2\2\u00eb\u00ee"+
		"\3\2\2\2\u00ec\u00ea\3\2\2\2\u00ec\u00ed\3\2\2\2\u00ed\u00f1\3\2\2\2\u00ee"+
		"\u00ec\3\2\2\2\u00ef\u00f0\7J\2\2\u00f0\u00f2\58\35\2\u00f1\u00ef\3\2"+
		"\2\2\u00f1\u00f2\3\2\2\2\u00f2\33\3\2\2\2\u00f3\u00f4\7*\2\2\u00f4\u00f5"+
		"\7.\2\2\u00f5\u00f8\5D#\2\u00f6\u00f7\7J\2\2\u00f7\u00f9\58\35\2\u00f8"+
		"\u00f6\3\2\2\2\u00f8\u00f9\3\2\2\2\u00f9\35\3\2\2\2\u00fa\u0105\5 \21"+
		"\2\u00fb\u00fc\7=\2\2\u00fc\u00fd\7%\2\2\u00fd\u0102\5\"\22\2\u00fe\u00ff"+
		"\7\5\2\2\u00ff\u0101\5\"\22\2\u0100\u00fe\3\2\2\2\u0101\u0104\3\2\2\2"+
		"\u0102\u0100\3\2\2\2\u0102\u0103\3\2\2\2\u0103\u0106\3\2\2\2\u0104\u0102"+
		"\3\2\2\2\u0105\u00fb\3\2\2\2\u0105\u0106\3\2\2\2\u0106\u010d\3\2\2\2\u0107"+
		"\u0108\7\65\2\2\u0108\u010b\58\35\2\u0109\u010a\t\2\2\2\u010a\u010c\5"+
		"8\35\2\u010b\u0109\3\2\2\2\u010b\u010c\3\2\2\2\u010c\u010e\3\2\2\2\u010d"+
		"\u0107\3\2\2\2\u010d\u010e\3\2\2\2\u010e\37\3\2\2\2\u010f\u0111\7B\2\2"+
		"\u0110\u0112\t\3\2\2\u0111\u0110\3\2\2\2\u0111\u0112\3\2\2\2\u0112\u0113"+
		"\3\2\2\2\u0113\u0118\5$\23\2\u0114\u0115\7\5\2\2\u0115\u0117\5$\23\2\u0116"+
		"\u0114\3\2\2\2\u0117\u011a\3\2\2\2\u0118\u0116\3\2\2\2\u0118\u0119\3\2"+
		"\2\2\u0119\u0127\3\2\2\2\u011a\u0118\3\2\2\2\u011b\u0125\7.\2\2\u011c"+
		"\u0121\5&\24\2\u011d\u011e\7\5\2\2\u011e\u0120\5&\24\2\u011f\u011d\3\2"+
		"\2\2\u0120\u0123\3\2\2\2\u0121\u011f\3\2\2\2\u0121\u0122\3\2\2\2\u0122"+
		"\u0126\3\2\2\2\u0123\u0121\3\2\2\2\u0124\u0126\5(\25\2\u0125\u011c\3\2"+
		"\2\2\u0125\u0124\3\2\2\2\u0126\u0128\3\2\2\2\u0127\u011b\3\2\2\2\u0127"+
		"\u0128\3\2\2\2\u0128\u012b\3\2\2\2\u0129\u012a\7J\2\2\u012a\u012c\58\35"+
		"\2\u012b\u0129\3\2\2\2\u012b\u012c\3\2\2\2\u012c!\3\2\2\2\u012d\u012f"+
		"\58\35\2\u012e\u0130\t\4\2\2\u012f\u012e\3\2\2\2\u012f\u0130\3\2\2\2\u0130"+
		"#\3\2\2\2\u0131\u013c\7\b\2\2\u0132\u0133\5D#\2\u0133\u0134\7\t\2\2\u0134"+
		"\u0135\7\b\2\2\u0135\u013c\3\2\2\2\u0136\u013c\5H%\2\u0137\u0138\5D#\2"+
		"\u0138\u0139\7\t\2\2\u0139\u013a\5H%\2\u013a\u013c\3\2\2\2\u013b\u0131"+
		"\3\2\2\2\u013b\u0132\3\2\2\2\u013b\u0136\3\2\2\2\u013b\u0137\3\2\2\2\u013c"+
		"%\3\2\2\2\u013d\u0151\5D#\2\u013e\u0148\7\4\2\2\u013f\u0144\5&\24\2\u0140"+
		"\u0141\7\5\2\2\u0141\u0143\5&\24\2\u0142\u0140\3\2\2\2\u0143\u0146\3\2"+
		"\2\2\u0144\u0142\3\2\2\2\u0144\u0145\3\2\2\2\u0145\u0149\3\2\2\2\u0146"+
		"\u0144\3\2\2\2\u0147\u0149\5(\25\2\u0148\u013f\3\2\2\2\u0148\u0147\3\2"+
		"\2\2\u0149\u014a\3\2\2\2\u014a\u014b\7\6\2\2\u014b\u0151\3\2\2\2\u014c"+
		"\u014d\7\4\2\2\u014d\u014e\5\36\20\2\u014e\u014f\7\6\2\2\u014f\u0151\3"+
		"\2\2\2\u0150\u013d\3\2\2\2\u0150\u013e\3\2\2\2\u0150\u014c\3\2\2\2\u0151"+
		"\'\3\2\2\2\u0152\u0159\5&\24\2\u0153\u0154\5*\26\2\u0154\u0155\5&\24\2"+
		"\u0155\u0156\5,\27\2\u0156\u0158\3\2\2\2\u0157\u0153\3\2\2\2\u0158\u015b"+
		"\3\2\2\2\u0159\u0157\3\2\2\2\u0159\u015a\3\2\2\2\u015a)\3\2\2\2\u015b"+
		"\u0159\3\2\2\2\u015c\u016a\7\5\2\2\u015d\u015f\7\66\2\2\u015e\u015d\3"+
		"\2\2\2\u015e\u015f\3\2\2\2\u015f\u0166\3\2\2\2\u0160\u0162\7\64\2\2\u0161"+
		"\u0163\7>\2\2\u0162\u0161\3\2\2\2\u0162\u0163\3\2\2\2\u0163\u0167\3\2"+
		"\2\2\u0164\u0167\7/\2\2\u0165\u0167\7)\2\2\u0166\u0160\3\2\2\2\u0166\u0164"+
		"\3\2\2\2\u0166\u0165\3\2\2\2\u0166\u0167\3\2\2\2\u0167\u0168\3\2\2\2\u0168"+
		"\u016a\7\62\2\2\u0169\u015c\3\2\2\2\u0169\u015e\3\2\2\2\u016a+\3\2\2\2"+
		"\u016b\u016c\7;\2\2\u016c\u017a\58\35\2\u016d\u016e\7H\2\2\u016e\u016f"+
		"\7\4\2\2\u016f\u0174\5H%\2\u0170\u0171\7\5\2\2\u0171\u0173\5H%\2\u0172"+
		"\u0170\3\2\2\2\u0173\u0176\3\2\2\2\u0174\u0172\3\2\2\2\u0174\u0175\3\2"+
		"\2\2\u0175\u0177\3\2\2\2\u0176\u0174\3\2\2\2\u0177\u0178\7\6\2\2\u0178"+
		"\u017a\3\2\2\2\u0179\u016b\3\2\2\2\u0179\u016d\3\2\2\2\u0179\u017a\3\2"+
		"\2\2\u017a-\3\2\2\2\u017b\u017c\5H%\2\u017c\u0180\5\60\31\2\u017d\u017f"+
		"\5\66\34\2\u017e\u017d\3\2\2\2\u017f\u0182\3\2\2\2\u0180\u017e\3\2\2\2"+
		"\u0180\u0181\3\2\2\2\u0181/\3\2\2\2\u0182\u0180\3\2\2\2\u0183\u0191\7"+
		"\27\2\2\u0184\u0191\7\30\2\2\u0185\u0191\7\31\2\2\u0186\u0191\7\32\2\2"+
		"\u0187\u0191\7\33\2\2\u0188\u0191\7\34\2\2\u0189\u0191\7\35\2\2\u018a"+
		"\u018b\7\36\2\2\u018b\u018e\5\62\32\2\u018c\u018d\7\37\2\2\u018d\u018f"+
		"\5\64\33\2\u018e\u018c\3\2\2\2\u018e\u018f\3\2\2\2\u018f\u0191\3\2\2\2"+
		"\u0190\u0183\3\2\2\2\u0190\u0184\3\2\2\2\u0190\u0185\3\2\2\2\u0190\u0186"+
		"\3\2\2\2\u0190\u0187\3\2\2\2\u0190\u0188\3\2\2\2\u0190\u0189\3\2\2\2\u0190"+
		"\u018a\3\2\2\2\u0191\61\3\2\2\2\u0192\u0193\7L\2\2\u0193\63\3\2\2\2\u0194"+
		"\u0195\7\4\2\2\u0195\u0196\7L\2\2\u0196\u0197\7\5\2\2\u0197\u0198\7L\2"+
		"\2\u0198\u0199\7\6\2\2\u0199\65\3\2\2\2\u019a\u019b\7\'\2\2\u019b\u019d"+
		"\5B\"\2\u019c\u019a\3\2\2\2\u019c\u019d\3\2\2\2\u019d\u01a6\3\2\2\2\u019e"+
		"\u019f\7?\2\2\u019f\u01a7\7\63\2\2\u01a0\u01a2\78\2\2\u01a1\u01a0\3\2"+
		"\2\2\u01a1\u01a2\3\2\2\2\u01a2\u01a3\3\2\2\2\u01a3\u01a7\79\2\2\u01a4"+
		"\u01a7\7F\2\2\u01a5\u01a7\5:\36\2\u01a6\u019e\3\2\2\2\u01a6\u01a1\3\2"+
		"\2\2\u01a6\u01a4\3\2\2\2\u01a6\u01a5\3\2\2\2\u01a7\67\3\2\2\2\u01a8\u01a9"+
		"\b\35\1\2\u01a9\u01b5\5> \2\u01aa\u01ab\5D#\2\u01ab\u01ac\7\t\2\2\u01ac"+
		"\u01ae\3\2\2\2\u01ad\u01aa\3\2\2\2\u01ad\u01ae\3\2\2\2\u01ae\u01af\3\2"+
		"\2\2\u01af\u01b5\5H%\2\u01b0\u01b1\7\4\2\2\u01b1\u01b2\58\35\2\u01b2\u01b3"+
		"\7\6\2\2\u01b3\u01b5\3\2\2\2\u01b4\u01a8\3\2\2\2\u01b4\u01ad\3\2\2\2\u01b4"+
		"\u01b0\3\2\2\2\u01b5\u01be\3\2\2\2\u01b6\u01b7\f\5\2\2\u01b7\u01b8\t\5"+
		"\2\2\u01b8\u01bd\58\35\6\u01b9\u01ba\f\4\2\2\u01ba\u01bb\t\6\2\2\u01bb"+
		"\u01bd\58\35\5\u01bc\u01b6\3\2\2\2\u01bc\u01b9\3\2\2\2\u01bd\u01c0\3\2"+
		"\2\2\u01be\u01bc\3\2\2\2\u01be\u01bf\3\2\2\2\u01bf9\3\2\2\2\u01c0\u01be"+
		"\3\2\2\2\u01c1\u01c2\7@\2\2\u01c2\u01ce\5N(\2\u01c3\u01c4\7\4\2\2\u01c4"+
		"\u01c9\5H%\2\u01c5\u01c6\7\5\2\2\u01c6\u01c8\5H%\2\u01c7\u01c5\3\2\2\2"+
		"\u01c8\u01cb\3\2\2\2\u01c9\u01c7\3\2\2\2\u01c9\u01ca\3\2\2\2\u01ca\u01cc"+
		"\3\2\2\2\u01cb\u01c9\3\2\2\2\u01cc\u01cd\7\6\2\2\u01cd\u01cf\3\2\2\2\u01ce"+
		"\u01c3\3\2\2\2\u01ce\u01cf\3\2\2\2\u01cf;\3\2\2\2\u01d0\u01d2\t\7\2\2"+
		"\u01d1\u01d0\3\2\2\2\u01d1\u01d2\3\2\2\2\u01d2\u01d3\3\2\2\2\u01d3\u01d4"+
		"\7L\2\2\u01d4=\3\2\2\2\u01d5\u01d6\t\b\2\2\u01d6?\3\2\2\2\u01d7\u01d8"+
		"\t\t\2\2\u01d8A\3\2\2\2\u01d9\u01da\5R*\2\u01daC\3\2\2\2\u01db\u01dc\5"+
		"R*\2\u01dcE\3\2\2\2\u01dd\u01de\5R*\2\u01deG\3\2\2\2\u01df\u01e0\5R*\2"+
		"\u01e0I\3\2\2\2\u01e1\u01e2\5R*\2\u01e2K\3\2\2\2\u01e3\u01e4\5R*\2\u01e4"+
		"M\3\2\2\2\u01e5\u01e6\5R*\2\u01e6O\3\2\2\2\u01e7\u01ef\7K\2\2\u01e8\u01ef"+
		"\5@!\2\u01e9\u01ef\7N\2\2\u01ea\u01eb\7\4\2\2\u01eb\u01ec\5P)\2\u01ec"+
		"\u01ed\7\6\2\2\u01ed\u01ef\3\2\2\2\u01ee\u01e7\3\2\2\2\u01ee\u01e8\3\2"+
		"\2\2\u01ee\u01e9\3\2\2\2\u01ee\u01ea\3\2\2\2\u01efQ\3\2\2\2\u01f0\u01f2"+
		"\7\26\2\2\u01f1\u01f0\3\2\2\2\u01f1\u01f2\3\2\2\2\u01f2\u01f3\3\2\2\2"+
		"\u01f3\u01f4\5P)\2\u01f4S\3\2\2\28VXcjou\u0082\u0097\u00ad\u00bc\u00c1"+
		"\u00ca\u00d5\u00dc\u00ec\u00f1\u00f8\u0102\u0105\u010b\u010d\u0111\u0118"+
		"\u0121\u0125\u0127\u012b\u012f\u013b\u0144\u0148\u0150\u0159\u015e\u0162"+
		"\u0166\u0169\u0174\u0179\u0180\u018e\u0190\u019c\u01a1\u01a6\u01ad\u01b4"+
		"\u01bc\u01be\u01c9\u01ce\u01d1\u01ee\u01f1";
	public static final ATN _ATN =
		new ATNDeserializer().deserialize(_serializedATN.toCharArray());
	static {
		_decisionToDFA = new DFA[_ATN.getNumberOfDecisions()];
		for (int i = 0; i < _ATN.getNumberOfDecisions(); i++) {
			_decisionToDFA[i] = new DFA(_ATN.getDecisionState(i), i);
		}
	}
}