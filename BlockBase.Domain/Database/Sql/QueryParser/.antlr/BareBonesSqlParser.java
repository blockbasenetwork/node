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
		T__9=10, T__10=11, T__11=12, T__12=13, T__13=14, K_USE=15, K_CURRENT_DATABASE=16, 
		K_LIST_DATABASES=17, K_GET_STRUCTURE=18, K_NOT_TO_ENCRYPT=19, K_BOOL=20, 
		K_DATETIME=21, K_DURATION=22, K_INT=23, K_DECIMAL=24, K_DOUBLE=25, K_TEXT=26, 
		K_ENCRYPTED=27, K_RANGE=28, K_ADD=29, K_ALL=30, K_ALTER=31, K_AND=32, 
		K_ASC=33, K_BY=34, K_COLUMN=35, K_CONSTRAINT=36, K_CREATE=37, K_CROSS=38, 
		K_DATABASE=39, K_DELETE=40, K_DESC=41, K_DISTINCT=42, K_DROP=43, K_FROM=44, 
		K_INNER=45, K_INSERT=46, K_INTO=47, K_JOIN=48, K_KEY=49, K_LEFT=50, K_LIMIT=51, 
		K_NATURAL=52, K_NO=53, K_NOT=54, K_NULL=55, K_OFFSET=56, K_ON=57, K_OR=58, 
		K_ORDER=59, K_OUTER=60, K_PRIMARY=61, K_REFERENCES=62, K_RENAME=63, K_SELECT=64, 
		K_SET=65, K_TABLE=66, K_TO=67, K_UNIQUE=68, K_UPDATE=69, K_USING=70, K_VALUES=71, 
		K_WHERE=72, IDENTIFIER=73, NUMERIC_LITERAL=74, BIND_PARAMETER=75, STRING_LITERAL=76, 
		UNEXPECTED_CHAR=77;
	public static final int
		RULE_parse = 0, RULE_error = 1, RULE_sql_stmt_list = 2, RULE_sql_stmt = 3, 
		RULE_use_database_stmt = 4, RULE_current_database_stmt = 5, RULE_list_databases_stmt = 6, 
		RULE_get_structure_stmt = 7, RULE_create_database_stmt = 8, RULE_drop_database_stmt = 9, 
		RULE_create_table_stmt = 10, RULE_alter_table_stmt = 11, RULE_drop_table_stmt = 12, 
		RULE_insert_stmt = 13, RULE_update_stmt = 14, RULE_delete_stmt = 15, RULE_simple_select_stmt = 16, 
		RULE_select_core = 17, RULE_ordering_term = 18, RULE_result_column = 19, 
		RULE_table_or_subquery = 20, RULE_join_clause = 21, RULE_join_operator = 22, 
		RULE_join_constraint = 23, RULE_column_def = 24, RULE_data_type = 25, 
		RULE_bucket_size = 26, RULE_bucket_range = 27, RULE_column_constraint = 28, 
		RULE_expr = 29, RULE_foreign_key_clause = 30, RULE_signed_number = 31, 
		RULE_literal_value = 32, RULE_keyword = 33, RULE_name = 34, RULE_table_name = 35, 
		RULE_new_table_name = 36, RULE_column_name = 37, RULE_new_column_name = 38, 
		RULE_database_name = 39, RULE_foreign_table = 40, RULE_table_column_name = 41, 
		RULE_any_name = 42, RULE_complex_name = 43;
	public static final String[] ruleNames = {
		"parse", "error", "sql_stmt_list", "sql_stmt", "use_database_stmt", "current_database_stmt", 
		"list_databases_stmt", "get_structure_stmt", "create_database_stmt", "drop_database_stmt", 
		"create_table_stmt", "alter_table_stmt", "drop_table_stmt", "insert_stmt", 
		"update_stmt", "delete_stmt", "simple_select_stmt", "select_core", "ordering_term", 
		"result_column", "table_or_subquery", "join_clause", "join_operator", 
		"join_constraint", "column_def", "data_type", "bucket_size", "bucket_range", 
		"column_constraint", "expr", "foreign_key_clause", "signed_number", "literal_value", 
		"keyword", "name", "table_name", "new_table_name", "column_name", "new_column_name", 
		"database_name", "foreign_table", "table_column_name", "any_name", "complex_name"
	};

	private static final String[] _LITERAL_NAMES = {
		null, "';'", "'('", "','", "')'", "'='", "'.*'", "'.'", "'<'", "'<='", 
		"'>'", "'>='", "'!='", "'+'", "'-'", null, null, null, null, "'!'"
	};
	private static final String[] _SYMBOLIC_NAMES = {
		null, null, null, null, null, null, null, null, null, null, null, null, 
		null, null, null, "K_USE", "K_CURRENT_DATABASE", "K_LIST_DATABASES", "K_GET_STRUCTURE", 
		"K_NOT_TO_ENCRYPT", "K_BOOL", "K_DATETIME", "K_DURATION", "K_INT", "K_DECIMAL", 
		"K_DOUBLE", "K_TEXT", "K_ENCRYPTED", "K_RANGE", "K_ADD", "K_ALL", "K_ALTER", 
		"K_AND", "K_ASC", "K_BY", "K_COLUMN", "K_CONSTRAINT", "K_CREATE", "K_CROSS", 
		"K_DATABASE", "K_DELETE", "K_DESC", "K_DISTINCT", "K_DROP", "K_FROM", 
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
			setState(92);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << T__0) | (1L << K_USE) | (1L << K_CURRENT_DATABASE) | (1L << K_LIST_DATABASES) | (1L << K_ALTER) | (1L << K_CREATE) | (1L << K_DELETE) | (1L << K_DROP) | (1L << K_INSERT))) != 0) || ((((_la - 64)) & ~0x3f) == 0 && ((1L << (_la - 64)) & ((1L << (K_SELECT - 64)) | (1L << (K_UPDATE - 64)) | (1L << (UNEXPECTED_CHAR - 64)))) != 0)) {
				{
				setState(90);
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
					setState(88);
					sql_stmt_list();
					}
					break;
				case UNEXPECTED_CHAR:
					{
					setState(89);
					error();
					}
					break;
				default:
					throw new NoViableAltException(this);
				}
				}
				setState(94);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(95);
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
			setState(97);
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
			setState(103);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__0) {
				{
				{
				setState(100);
				match(T__0);
				}
				}
				setState(105);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(106);
			sql_stmt();
			setState(115);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,4,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					{
					{
					setState(108); 
					_errHandler.sync(this);
					_la = _input.LA(1);
					do {
						{
						{
						setState(107);
						match(T__0);
						}
						}
						setState(110); 
						_errHandler.sync(this);
						_la = _input.LA(1);
					} while ( _la==T__0 );
					setState(112);
					sql_stmt();
					}
					} 
				}
				setState(117);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,4,_ctx);
			}
			setState(121);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,5,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					{
					{
					setState(118);
					match(T__0);
					}
					} 
				}
				setState(123);
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
			setState(136);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,6,_ctx) ) {
			case 1:
				{
				setState(124);
				create_database_stmt();
				}
				break;
			case 2:
				{
				setState(125);
				drop_database_stmt();
				}
				break;
			case 3:
				{
				setState(126);
				alter_table_stmt();
				}
				break;
			case 4:
				{
				setState(127);
				create_table_stmt();
				}
				break;
			case 5:
				{
				setState(128);
				drop_table_stmt();
				}
				break;
			case 6:
				{
				setState(129);
				insert_stmt();
				}
				break;
			case 7:
				{
				setState(130);
				update_stmt();
				}
				break;
			case 8:
				{
				setState(131);
				delete_stmt();
				}
				break;
			case 9:
				{
				setState(132);
				simple_select_stmt();
				}
				break;
			case 10:
				{
				setState(133);
				use_database_stmt();
				}
				break;
			case 11:
				{
				setState(134);
				current_database_stmt();
				}
				break;
			case 12:
				{
				setState(135);
				list_databases_stmt();
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
			setState(138);
			match(K_USE);
			setState(139);
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
			setState(141);
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
			setState(143);
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
			setState(145);
			match(K_GET_STRUCTURE);
			setState(146);
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
			setState(148);
			match(K_CREATE);
			setState(149);
			match(K_DATABASE);
			setState(150);
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
			setState(152);
			match(K_DROP);
			setState(153);
			match(K_DATABASE);
			setState(154);
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
			setState(156);
			match(K_CREATE);
			setState(157);
			match(K_TABLE);
			setState(158);
			table_name();
			{
			setState(159);
			match(T__1);
			setState(160);
			column_def();
			setState(165);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__2) {
				{
				{
				setState(161);
				match(T__2);
				setState(162);
				column_def();
				}
				}
				setState(167);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(168);
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
			setState(170);
			match(K_ALTER);
			setState(171);
			match(K_TABLE);
			setState(172);
			table_name();
			setState(187);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,8,_ctx) ) {
			case 1:
				{
				setState(173);
				match(K_RENAME);
				setState(174);
				match(K_TO);
				setState(175);
				new_table_name();
				}
				break;
			case 2:
				{
				setState(176);
				match(K_ADD);
				setState(177);
				match(K_COLUMN);
				setState(178);
				column_def();
				}
				break;
			case 3:
				{
				setState(179);
				match(K_DROP);
				setState(180);
				match(K_COLUMN);
				setState(181);
				column_name();
				}
				break;
			case 4:
				{
				setState(182);
				match(K_RENAME);
				setState(183);
				column_name();
				setState(184);
				match(K_TO);
				setState(185);
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
			setState(189);
			match(K_DROP);
			setState(190);
			match(K_TABLE);
			setState(191);
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
			setState(193);
			match(K_INSERT);
			setState(194);
			match(K_INTO);
			setState(195);
			table_name();
			setState(207);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==T__1) {
				{
				setState(196);
				match(T__1);
				setState(197);
				column_name();
				setState(202);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==T__2) {
					{
					{
					setState(198);
					match(T__2);
					setState(199);
					column_name();
					}
					}
					setState(204);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(205);
				match(T__3);
				}
			}

			{
			setState(209);
			match(K_VALUES);
			setState(210);
			match(T__1);
			setState(211);
			literal_value();
			setState(216);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__2) {
				{
				{
				setState(212);
				match(T__2);
				setState(213);
				literal_value();
				}
				}
				setState(218);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(219);
			match(T__3);
			setState(234);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__2) {
				{
				{
				setState(220);
				match(T__2);
				setState(221);
				match(T__1);
				setState(222);
				literal_value();
				setState(227);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==T__2) {
					{
					{
					setState(223);
					match(T__2);
					setState(224);
					literal_value();
					}
					}
					setState(229);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(230);
				match(T__3);
				}
				}
				setState(236);
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
			setState(237);
			match(K_UPDATE);
			setState(238);
			table_name();
			setState(239);
			match(K_SET);
			setState(240);
			column_name();
			setState(241);
			match(T__4);
			setState(242);
			literal_value();
			setState(250);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__2) {
				{
				{
				setState(243);
				match(T__2);
				setState(244);
				column_name();
				setState(245);
				match(T__4);
				setState(246);
				literal_value();
				}
				}
				setState(252);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(255);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_WHERE) {
				{
				setState(253);
				match(K_WHERE);
				setState(254);
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
			setState(257);
			match(K_DELETE);
			setState(258);
			match(K_FROM);
			setState(259);
			table_name();
			setState(262);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_WHERE) {
				{
				setState(260);
				match(K_WHERE);
				setState(261);
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
		enterRule(_localctx, 32, RULE_simple_select_stmt);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(264);
			select_core();
			setState(275);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_ORDER) {
				{
				setState(265);
				match(K_ORDER);
				setState(266);
				match(K_BY);
				setState(267);
				ordering_term();
				setState(272);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==T__2) {
					{
					{
					setState(268);
					match(T__2);
					setState(269);
					ordering_term();
					}
					}
					setState(274);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				}
			}

			setState(283);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_LIMIT) {
				{
				setState(277);
				match(K_LIMIT);
				setState(278);
				literal_value();
				setState(281);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if (_la==K_OFFSET) {
					{
					setState(279);
					match(K_OFFSET);
					setState(280);
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
		enterRule(_localctx, 34, RULE_select_core);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(285);
			match(K_SELECT);
			setState(287);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,21,_ctx) ) {
			case 1:
				{
				setState(286);
				match(K_DISTINCT);
				}
				break;
			}
			setState(289);
			result_column();
			setState(294);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__2) {
				{
				{
				setState(290);
				match(T__2);
				setState(291);
				result_column();
				}
				}
				setState(296);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(297);
			match(K_FROM);
			setState(307);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,24,_ctx) ) {
			case 1:
				{
				setState(298);
				table_or_subquery();
				setState(303);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==T__2) {
					{
					{
					setState(299);
					match(T__2);
					setState(300);
					table_or_subquery();
					}
					}
					setState(305);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				}
				break;
			case 2:
				{
				setState(306);
				join_clause();
				}
				break;
			}
			setState(312);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case K_WHERE:
				{
				setState(309);
				match(K_WHERE);
				setState(310);
				expr(0);
				}
				break;
			case K_ENCRYPTED:
				{
				setState(311);
				match(K_ENCRYPTED);
				}
				break;
			case EOF:
			case T__0:
			case T__3:
			case K_USE:
			case K_CURRENT_DATABASE:
			case K_LIST_DATABASES:
			case K_ALTER:
			case K_CREATE:
			case K_DELETE:
			case K_DROP:
			case K_INSERT:
			case K_LIMIT:
			case K_ORDER:
			case K_SELECT:
			case K_UPDATE:
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
		enterRule(_localctx, 36, RULE_ordering_term);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(314);
			expr(0);
			setState(316);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_ASC || _la==K_DESC) {
				{
				setState(315);
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
		enterRule(_localctx, 38, RULE_result_column);
		try {
			setState(322);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,27,_ctx) ) {
			case 1:
				enterOuterAlt(_localctx, 1);
				{
				setState(318);
				table_name();
				setState(319);
				match(T__5);
				}
				break;
			case 2:
				enterOuterAlt(_localctx, 2);
				{
				setState(321);
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
		enterRule(_localctx, 40, RULE_table_or_subquery);
		int _la;
		try {
			setState(343);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,30,_ctx) ) {
			case 1:
				enterOuterAlt(_localctx, 1);
				{
				setState(324);
				table_name();
				}
				break;
			case 2:
				enterOuterAlt(_localctx, 2);
				{
				setState(325);
				match(T__1);
				setState(335);
				_errHandler.sync(this);
				switch ( getInterpreter().adaptivePredict(_input,29,_ctx) ) {
				case 1:
					{
					setState(326);
					table_or_subquery();
					setState(331);
					_errHandler.sync(this);
					_la = _input.LA(1);
					while (_la==T__2) {
						{
						{
						setState(327);
						match(T__2);
						setState(328);
						table_or_subquery();
						}
						}
						setState(333);
						_errHandler.sync(this);
						_la = _input.LA(1);
					}
					}
					break;
				case 2:
					{
					setState(334);
					join_clause();
					}
					break;
				}
				setState(337);
				match(T__3);
				}
				break;
			case 3:
				enterOuterAlt(_localctx, 3);
				{
				setState(339);
				match(T__1);
				setState(340);
				simple_select_stmt();
				setState(341);
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
		enterRule(_localctx, 42, RULE_join_clause);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(345);
			table_or_subquery();
			setState(352);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << T__2) | (1L << K_CROSS) | (1L << K_INNER) | (1L << K_JOIN) | (1L << K_LEFT) | (1L << K_NATURAL))) != 0)) {
				{
				{
				setState(346);
				join_operator();
				setState(347);
				table_or_subquery();
				setState(348);
				join_constraint();
				}
				}
				setState(354);
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
		enterRule(_localctx, 44, RULE_join_operator);
		int _la;
		try {
			setState(368);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case T__2:
				enterOuterAlt(_localctx, 1);
				{
				setState(355);
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
				setState(357);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if (_la==K_NATURAL) {
					{
					setState(356);
					match(K_NATURAL);
					}
				}

				setState(365);
				_errHandler.sync(this);
				switch (_input.LA(1)) {
				case K_LEFT:
					{
					setState(359);
					match(K_LEFT);
					setState(361);
					_errHandler.sync(this);
					_la = _input.LA(1);
					if (_la==K_OUTER) {
						{
						setState(360);
						match(K_OUTER);
						}
					}

					}
					break;
				case K_INNER:
					{
					setState(363);
					match(K_INNER);
					}
					break;
				case K_CROSS:
					{
					setState(364);
					match(K_CROSS);
					}
					break;
				case K_JOIN:
					break;
				default:
					break;
				}
				setState(367);
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
		enterRule(_localctx, 46, RULE_join_constraint);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(372);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_ON) {
				{
				setState(370);
				match(K_ON);
				setState(371);
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
		enterRule(_localctx, 48, RULE_column_def);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(374);
			column_name();
			setState(375);
			data_type();
			setState(379);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (((((_la - 36)) & ~0x3f) == 0 && ((1L << (_la - 36)) & ((1L << (K_CONSTRAINT - 36)) | (1L << (K_NOT - 36)) | (1L << (K_NULL - 36)) | (1L << (K_PRIMARY - 36)) | (1L << (K_REFERENCES - 36)) | (1L << (K_UNIQUE - 36)))) != 0)) {
				{
				{
				setState(376);
				column_constraint();
				}
				}
				setState(381);
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
		enterRule(_localctx, 50, RULE_data_type);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(397);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case K_BOOL:
				{
				setState(382);
				match(K_BOOL);
				}
				break;
			case K_DATETIME:
				{
				setState(383);
				match(K_DATETIME);
				}
				break;
			case K_DURATION:
				{
				setState(384);
				match(K_DURATION);
				}
				break;
			case K_INT:
				{
				setState(385);
				match(K_INT);
				}
				break;
			case K_DECIMAL:
				{
				setState(386);
				match(K_DECIMAL);
				}
				break;
			case K_DOUBLE:
				{
				setState(387);
				match(K_DOUBLE);
				}
				break;
			case K_TEXT:
				{
				setState(388);
				match(K_TEXT);
				}
				break;
			case K_ENCRYPTED:
				{
				setState(389);
				match(K_ENCRYPTED);
				setState(391);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if (_la==NUMERIC_LITERAL) {
					{
					setState(390);
					bucket_size();
					}
				}

				setState(395);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if (_la==K_RANGE) {
					{
					setState(393);
					match(K_RANGE);
					setState(394);
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
		enterRule(_localctx, 52, RULE_bucket_size);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(399);
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
		enterRule(_localctx, 54, RULE_bucket_range);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(401);
			match(T__1);
			setState(402);
			match(NUMERIC_LITERAL);
			setState(403);
			match(T__2);
			setState(404);
			match(NUMERIC_LITERAL);
			setState(405);
			match(T__2);
			setState(406);
			match(NUMERIC_LITERAL);
			setState(407);
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
		enterRule(_localctx, 56, RULE_column_constraint);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(411);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==K_CONSTRAINT) {
				{
				setState(409);
				match(K_CONSTRAINT);
				setState(410);
				name();
				}
			}

			setState(421);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case K_PRIMARY:
				{
				setState(413);
				match(K_PRIMARY);
				setState(414);
				match(K_KEY);
				}
				break;
			case K_NOT:
			case K_NULL:
				{
				setState(416);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if (_la==K_NOT) {
					{
					setState(415);
					match(K_NOT);
					}
				}

				setState(418);
				match(K_NULL);
				}
				break;
			case K_UNIQUE:
				{
				setState(419);
				match(K_UNIQUE);
				}
				break;
			case K_REFERENCES:
				{
				setState(420);
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
		int _startState = 58;
		enterRecursionRule(_localctx, 58, RULE_expr, _p);
		int _la;
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(438);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,44,_ctx) ) {
			case 1:
				{
				setState(424);
				table_name();
				setState(425);
				match(T__6);
				setState(426);
				column_name();
				setState(427);
				_la = _input.LA(1);
				if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << T__4) | (1L << T__7) | (1L << T__8) | (1L << T__9) | (1L << T__10) | (1L << T__11))) != 0)) ) {
				_errHandler.recoverInline(this);
				}
				else {
					if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
					_errHandler.reportMatch(this);
					consume();
				}
				setState(428);
				literal_value();
				}
				break;
			case 2:
				{
				setState(430);
				table_column_name();
				setState(431);
				_la = _input.LA(1);
				if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << T__4) | (1L << T__7) | (1L << T__8) | (1L << T__9) | (1L << T__10) | (1L << T__11))) != 0)) ) {
				_errHandler.recoverInline(this);
				}
				else {
					if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
					_errHandler.reportMatch(this);
					consume();
				}
				setState(432);
				table_column_name();
				}
				break;
			case 3:
				{
				setState(434);
				match(T__1);
				setState(435);
				expr(0);
				setState(436);
				match(T__3);
				}
				break;
			}
			_ctx.stop = _input.LT(-1);
			setState(445);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,45,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					if ( _parseListeners!=null ) triggerExitRuleEvent();
					_prevctx = _localctx;
					{
					{
					_localctx = new ExprContext(_parentctx, _parentState);
					pushNewRecursionContext(_localctx, _startState, RULE_expr);
					setState(440);
					if (!(precpred(_ctx, 2))) throw new FailedPredicateException(this, "precpred(_ctx, 2)");
					setState(441);
					_la = _input.LA(1);
					if ( !(_la==K_AND || _la==K_OR) ) {
					_errHandler.recoverInline(this);
					}
					else {
						if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
						_errHandler.reportMatch(this);
						consume();
					}
					setState(442);
					expr(3);
					}
					} 
				}
				setState(447);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,45,_ctx);
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
		enterRule(_localctx, 60, RULE_foreign_key_clause);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(448);
			match(K_REFERENCES);
			setState(449);
			foreign_table();
			setState(461);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==T__1) {
				{
				setState(450);
				match(T__1);
				setState(451);
				column_name();
				setState(456);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==T__2) {
					{
					{
					setState(452);
					match(T__2);
					setState(453);
					column_name();
					}
					}
					setState(458);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(459);
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
		enterRule(_localctx, 62, RULE_signed_number);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(464);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==T__12 || _la==T__13) {
				{
				setState(463);
				_la = _input.LA(1);
				if ( !(_la==T__12 || _la==T__13) ) {
				_errHandler.recoverInline(this);
				}
				else {
					if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
					_errHandler.reportMatch(this);
					consume();
				}
				}
			}

			setState(466);
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
		enterRule(_localctx, 64, RULE_literal_value);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(468);
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
		public TerminalNode K_DATABASE() { return getToken(BareBonesSqlParser.K_DATABASE, 0); }
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
		enterRule(_localctx, 66, RULE_keyword);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(470);
			_la = _input.LA(1);
			if ( !(((((_la - 15)) & ~0x3f) == 0 && ((1L << (_la - 15)) & ((1L << (K_USE - 15)) | (1L << (K_CURRENT_DATABASE - 15)) | (1L << (K_LIST_DATABASES - 15)) | (1L << (K_GET_STRUCTURE - 15)) | (1L << (K_NOT_TO_ENCRYPT - 15)) | (1L << (K_BOOL - 15)) | (1L << (K_DATETIME - 15)) | (1L << (K_DURATION - 15)) | (1L << (K_INT - 15)) | (1L << (K_DECIMAL - 15)) | (1L << (K_DOUBLE - 15)) | (1L << (K_TEXT - 15)) | (1L << (K_ENCRYPTED - 15)) | (1L << (K_RANGE - 15)) | (1L << (K_ADD - 15)) | (1L << (K_ALL - 15)) | (1L << (K_ALTER - 15)) | (1L << (K_AND - 15)) | (1L << (K_ASC - 15)) | (1L << (K_BY - 15)) | (1L << (K_COLUMN - 15)) | (1L << (K_CONSTRAINT - 15)) | (1L << (K_CREATE - 15)) | (1L << (K_CROSS - 15)) | (1L << (K_DATABASE - 15)) | (1L << (K_DELETE - 15)) | (1L << (K_DESC - 15)) | (1L << (K_DISTINCT - 15)) | (1L << (K_DROP - 15)) | (1L << (K_FROM - 15)) | (1L << (K_INNER - 15)) | (1L << (K_INSERT - 15)) | (1L << (K_INTO - 15)) | (1L << (K_JOIN - 15)) | (1L << (K_KEY - 15)) | (1L << (K_LEFT - 15)) | (1L << (K_LIMIT - 15)) | (1L << (K_NATURAL - 15)) | (1L << (K_NO - 15)) | (1L << (K_NOT - 15)) | (1L << (K_NULL - 15)) | (1L << (K_OFFSET - 15)) | (1L << (K_ON - 15)) | (1L << (K_OR - 15)) | (1L << (K_ORDER - 15)) | (1L << (K_OUTER - 15)) | (1L << (K_PRIMARY - 15)) | (1L << (K_REFERENCES - 15)) | (1L << (K_RENAME - 15)) | (1L << (K_SELECT - 15)) | (1L << (K_SET - 15)) | (1L << (K_TABLE - 15)) | (1L << (K_TO - 15)) | (1L << (K_UNIQUE - 15)) | (1L << (K_UPDATE - 15)) | (1L << (K_USING - 15)) | (1L << (K_VALUES - 15)) | (1L << (K_WHERE - 15)))) != 0)) ) {
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
		enterRule(_localctx, 68, RULE_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(472);
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
		enterRule(_localctx, 70, RULE_table_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(474);
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
		enterRule(_localctx, 72, RULE_new_table_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(476);
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
		enterRule(_localctx, 74, RULE_column_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(478);
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
		enterRule(_localctx, 76, RULE_new_column_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(480);
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
		enterRule(_localctx, 78, RULE_database_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(482);
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
		enterRule(_localctx, 80, RULE_foreign_table);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(484);
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
		enterRule(_localctx, 82, RULE_table_column_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(486);
			table_name();
			setState(487);
			match(T__6);
			setState(488);
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
		enterRule(_localctx, 84, RULE_any_name);
		try {
			setState(497);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case IDENTIFIER:
				enterOuterAlt(_localctx, 1);
				{
				setState(490);
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
				setState(491);
				keyword();
				}
				break;
			case STRING_LITERAL:
				enterOuterAlt(_localctx, 3);
				{
				setState(492);
				match(STRING_LITERAL);
				}
				break;
			case T__1:
				enterOuterAlt(_localctx, 4);
				{
				setState(493);
				match(T__1);
				setState(494);
				any_name();
				setState(495);
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
		enterRule(_localctx, 86, RULE_complex_name);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(500);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,50,_ctx) ) {
			case 1:
				{
				setState(499);
				match(K_NOT_TO_ENCRYPT);
				}
				break;
			}
			setState(502);
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
		case 29:
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
		"\3\u608b\ua72a\u8133\ub9ed\u417c\u3be7\u7786\u5964\3O\u01fb\4\2\t\2\4"+
		"\3\t\3\4\4\t\4\4\5\t\5\4\6\t\6\4\7\t\7\4\b\t\b\4\t\t\t\4\n\t\n\4\13\t"+
		"\13\4\f\t\f\4\r\t\r\4\16\t\16\4\17\t\17\4\20\t\20\4\21\t\21\4\22\t\22"+
		"\4\23\t\23\4\24\t\24\4\25\t\25\4\26\t\26\4\27\t\27\4\30\t\30\4\31\t\31"+
		"\4\32\t\32\4\33\t\33\4\34\t\34\4\35\t\35\4\36\t\36\4\37\t\37\4 \t \4!"+
		"\t!\4\"\t\"\4#\t#\4$\t$\4%\t%\4&\t&\4\'\t\'\4(\t(\4)\t)\4*\t*\4+\t+\4"+
		",\t,\4-\t-\3\2\3\2\7\2]\n\2\f\2\16\2`\13\2\3\2\3\2\3\3\3\3\3\3\3\4\7\4"+
		"h\n\4\f\4\16\4k\13\4\3\4\3\4\6\4o\n\4\r\4\16\4p\3\4\7\4t\n\4\f\4\16\4"+
		"w\13\4\3\4\7\4z\n\4\f\4\16\4}\13\4\3\5\3\5\3\5\3\5\3\5\3\5\3\5\3\5\3\5"+
		"\3\5\3\5\3\5\5\5\u008b\n\5\3\6\3\6\3\6\3\7\3\7\3\b\3\b\3\t\3\t\3\t\3\n"+
		"\3\n\3\n\3\n\3\13\3\13\3\13\3\13\3\f\3\f\3\f\3\f\3\f\3\f\3\f\7\f\u00a6"+
		"\n\f\f\f\16\f\u00a9\13\f\3\f\3\f\3\r\3\r\3\r\3\r\3\r\3\r\3\r\3\r\3\r\3"+
		"\r\3\r\3\r\3\r\3\r\3\r\3\r\3\r\5\r\u00be\n\r\3\16\3\16\3\16\3\16\3\17"+
		"\3\17\3\17\3\17\3\17\3\17\3\17\7\17\u00cb\n\17\f\17\16\17\u00ce\13\17"+
		"\3\17\3\17\5\17\u00d2\n\17\3\17\3\17\3\17\3\17\3\17\7\17\u00d9\n\17\f"+
		"\17\16\17\u00dc\13\17\3\17\3\17\3\17\3\17\3\17\3\17\7\17\u00e4\n\17\f"+
		"\17\16\17\u00e7\13\17\3\17\3\17\7\17\u00eb\n\17\f\17\16\17\u00ee\13\17"+
		"\3\20\3\20\3\20\3\20\3\20\3\20\3\20\3\20\3\20\3\20\3\20\7\20\u00fb\n\20"+
		"\f\20\16\20\u00fe\13\20\3\20\3\20\5\20\u0102\n\20\3\21\3\21\3\21\3\21"+
		"\3\21\5\21\u0109\n\21\3\22\3\22\3\22\3\22\3\22\3\22\7\22\u0111\n\22\f"+
		"\22\16\22\u0114\13\22\5\22\u0116\n\22\3\22\3\22\3\22\3\22\5\22\u011c\n"+
		"\22\5\22\u011e\n\22\3\23\3\23\5\23\u0122\n\23\3\23\3\23\3\23\7\23\u0127"+
		"\n\23\f\23\16\23\u012a\13\23\3\23\3\23\3\23\3\23\7\23\u0130\n\23\f\23"+
		"\16\23\u0133\13\23\3\23\5\23\u0136\n\23\3\23\3\23\3\23\5\23\u013b\n\23"+
		"\3\24\3\24\5\24\u013f\n\24\3\25\3\25\3\25\3\25\5\25\u0145\n\25\3\26\3"+
		"\26\3\26\3\26\3\26\7\26\u014c\n\26\f\26\16\26\u014f\13\26\3\26\5\26\u0152"+
		"\n\26\3\26\3\26\3\26\3\26\3\26\3\26\5\26\u015a\n\26\3\27\3\27\3\27\3\27"+
		"\3\27\7\27\u0161\n\27\f\27\16\27\u0164\13\27\3\30\3\30\5\30\u0168\n\30"+
		"\3\30\3\30\5\30\u016c\n\30\3\30\3\30\5\30\u0170\n\30\3\30\5\30\u0173\n"+
		"\30\3\31\3\31\5\31\u0177\n\31\3\32\3\32\3\32\7\32\u017c\n\32\f\32\16\32"+
		"\u017f\13\32\3\33\3\33\3\33\3\33\3\33\3\33\3\33\3\33\3\33\5\33\u018a\n"+
		"\33\3\33\3\33\5\33\u018e\n\33\5\33\u0190\n\33\3\34\3\34\3\35\3\35\3\35"+
		"\3\35\3\35\3\35\3\35\3\35\3\36\3\36\5\36\u019e\n\36\3\36\3\36\3\36\5\36"+
		"\u01a3\n\36\3\36\3\36\3\36\5\36\u01a8\n\36\3\37\3\37\3\37\3\37\3\37\3"+
		"\37\3\37\3\37\3\37\3\37\3\37\3\37\3\37\3\37\3\37\5\37\u01b9\n\37\3\37"+
		"\3\37\3\37\7\37\u01be\n\37\f\37\16\37\u01c1\13\37\3 \3 \3 \3 \3 \3 \7"+
		" \u01c9\n \f \16 \u01cc\13 \3 \3 \5 \u01d0\n \3!\5!\u01d3\n!\3!\3!\3\""+
		"\3\"\3#\3#\3$\3$\3%\3%\3&\3&\3\'\3\'\3(\3(\3)\3)\3*\3*\3+\3+\3+\3+\3,"+
		"\3,\3,\3,\3,\3,\3,\5,\u01f4\n,\3-\5-\u01f7\n-\3-\3-\3-\2\3<.\2\4\6\b\n"+
		"\f\16\20\22\24\26\30\32\34\36 \"$&(*,.\60\62\64\668:<>@BDFHJLNPRTVX\2"+
		"\b\4\2##++\4\2\7\7\n\16\4\2\"\"<<\3\2\17\20\5\299LLNN\3\2\21J\2\u021c"+
		"\2^\3\2\2\2\4c\3\2\2\2\6i\3\2\2\2\b\u008a\3\2\2\2\n\u008c\3\2\2\2\f\u008f"+
		"\3\2\2\2\16\u0091\3\2\2\2\20\u0093\3\2\2\2\22\u0096\3\2\2\2\24\u009a\3"+
		"\2\2\2\26\u009e\3\2\2\2\30\u00ac\3\2\2\2\32\u00bf\3\2\2\2\34\u00c3\3\2"+
		"\2\2\36\u00ef\3\2\2\2 \u0103\3\2\2\2\"\u010a\3\2\2\2$\u011f\3\2\2\2&\u013c"+
		"\3\2\2\2(\u0144\3\2\2\2*\u0159\3\2\2\2,\u015b\3\2\2\2.\u0172\3\2\2\2\60"+
		"\u0176\3\2\2\2\62\u0178\3\2\2\2\64\u018f\3\2\2\2\66\u0191\3\2\2\28\u0193"+
		"\3\2\2\2:\u019d\3\2\2\2<\u01b8\3\2\2\2>\u01c2\3\2\2\2@\u01d2\3\2\2\2B"+
		"\u01d6\3\2\2\2D\u01d8\3\2\2\2F\u01da\3\2\2\2H\u01dc\3\2\2\2J\u01de\3\2"+
		"\2\2L\u01e0\3\2\2\2N\u01e2\3\2\2\2P\u01e4\3\2\2\2R\u01e6\3\2\2\2T\u01e8"+
		"\3\2\2\2V\u01f3\3\2\2\2X\u01f6\3\2\2\2Z]\5\6\4\2[]\5\4\3\2\\Z\3\2\2\2"+
		"\\[\3\2\2\2]`\3\2\2\2^\\\3\2\2\2^_\3\2\2\2_a\3\2\2\2`^\3\2\2\2ab\7\2\2"+
		"\3b\3\3\2\2\2cd\7O\2\2de\b\3\1\2e\5\3\2\2\2fh\7\3\2\2gf\3\2\2\2hk\3\2"+
		"\2\2ig\3\2\2\2ij\3\2\2\2jl\3\2\2\2ki\3\2\2\2lu\5\b\5\2mo\7\3\2\2nm\3\2"+
		"\2\2op\3\2\2\2pn\3\2\2\2pq\3\2\2\2qr\3\2\2\2rt\5\b\5\2sn\3\2\2\2tw\3\2"+
		"\2\2us\3\2\2\2uv\3\2\2\2v{\3\2\2\2wu\3\2\2\2xz\7\3\2\2yx\3\2\2\2z}\3\2"+
		"\2\2{y\3\2\2\2{|\3\2\2\2|\7\3\2\2\2}{\3\2\2\2~\u008b\5\22\n\2\177\u008b"+
		"\5\24\13\2\u0080\u008b\5\30\r\2\u0081\u008b\5\26\f\2\u0082\u008b\5\32"+
		"\16\2\u0083\u008b\5\34\17\2\u0084\u008b\5\36\20\2\u0085\u008b\5 \21\2"+
		"\u0086\u008b\5\"\22\2\u0087\u008b\5\n\6\2\u0088\u008b\5\f\7\2\u0089\u008b"+
		"\5\16\b\2\u008a~\3\2\2\2\u008a\177\3\2\2\2\u008a\u0080\3\2\2\2\u008a\u0081"+
		"\3\2\2\2\u008a\u0082\3\2\2\2\u008a\u0083\3\2\2\2\u008a\u0084\3\2\2\2\u008a"+
		"\u0085\3\2\2\2\u008a\u0086\3\2\2\2\u008a\u0087\3\2\2\2\u008a\u0088\3\2"+
		"\2\2\u008a\u0089\3\2\2\2\u008b\t\3\2\2\2\u008c\u008d\7\21\2\2\u008d\u008e"+
		"\5P)\2\u008e\13\3\2\2\2\u008f\u0090\7\22\2\2\u0090\r\3\2\2\2\u0091\u0092"+
		"\7\23\2\2\u0092\17\3\2\2\2\u0093\u0094\7\24\2\2\u0094\u0095\5P)\2\u0095"+
		"\21\3\2\2\2\u0096\u0097\7\'\2\2\u0097\u0098\7)\2\2\u0098\u0099\5P)\2\u0099"+
		"\23\3\2\2\2\u009a\u009b\7-\2\2\u009b\u009c\7)\2\2\u009c\u009d\5P)\2\u009d"+
		"\25\3\2\2\2\u009e\u009f\7\'\2\2\u009f\u00a0\7D\2\2\u00a0\u00a1\5H%\2\u00a1"+
		"\u00a2\7\4\2\2\u00a2\u00a7\5\62\32\2\u00a3\u00a4\7\5\2\2\u00a4\u00a6\5"+
		"\62\32\2\u00a5\u00a3\3\2\2\2\u00a6\u00a9\3\2\2\2\u00a7\u00a5\3\2\2\2\u00a7"+
		"\u00a8\3\2\2\2\u00a8\u00aa\3\2\2\2\u00a9\u00a7\3\2\2\2\u00aa\u00ab\7\6"+
		"\2\2\u00ab\27\3\2\2\2\u00ac\u00ad\7!\2\2\u00ad\u00ae\7D\2\2\u00ae\u00bd"+
		"\5H%\2\u00af\u00b0\7A\2\2\u00b0\u00b1\7E\2\2\u00b1\u00be\5J&\2\u00b2\u00b3"+
		"\7\37\2\2\u00b3\u00b4\7%\2\2\u00b4\u00be\5\62\32\2\u00b5\u00b6\7-\2\2"+
		"\u00b6\u00b7\7%\2\2\u00b7\u00be\5L\'\2\u00b8\u00b9\7A\2\2\u00b9\u00ba"+
		"\5L\'\2\u00ba\u00bb\7E\2\2\u00bb\u00bc\5N(\2\u00bc\u00be\3\2\2\2\u00bd"+
		"\u00af\3\2\2\2\u00bd\u00b2\3\2\2\2\u00bd\u00b5\3\2\2\2\u00bd\u00b8\3\2"+
		"\2\2\u00be\31\3\2\2\2\u00bf\u00c0\7-\2\2\u00c0\u00c1\7D\2\2\u00c1\u00c2"+
		"\5H%\2\u00c2\33\3\2\2\2\u00c3\u00c4\7\60\2\2\u00c4\u00c5\7\61\2\2\u00c5"+
		"\u00d1\5H%\2\u00c6\u00c7\7\4\2\2\u00c7\u00cc\5L\'\2\u00c8\u00c9\7\5\2"+
		"\2\u00c9\u00cb\5L\'\2\u00ca\u00c8\3\2\2\2\u00cb\u00ce\3\2\2\2\u00cc\u00ca"+
		"\3\2\2\2\u00cc\u00cd\3\2\2\2\u00cd\u00cf\3\2\2\2\u00ce\u00cc\3\2\2\2\u00cf"+
		"\u00d0\7\6\2\2\u00d0\u00d2\3\2\2\2\u00d1\u00c6\3\2\2\2\u00d1\u00d2\3\2"+
		"\2\2\u00d2\u00d3\3\2\2\2\u00d3\u00d4\7I\2\2\u00d4\u00d5\7\4\2\2\u00d5"+
		"\u00da\5B\"\2\u00d6\u00d7\7\5\2\2\u00d7\u00d9\5B\"\2\u00d8\u00d6\3\2\2"+
		"\2\u00d9\u00dc\3\2\2\2\u00da\u00d8\3\2\2\2\u00da\u00db\3\2\2\2\u00db\u00dd"+
		"\3\2\2\2\u00dc\u00da\3\2\2\2\u00dd\u00ec\7\6\2\2\u00de\u00df\7\5\2\2\u00df"+
		"\u00e0\7\4\2\2\u00e0\u00e5\5B\"\2\u00e1\u00e2\7\5\2\2\u00e2\u00e4\5B\""+
		"\2\u00e3\u00e1\3\2\2\2\u00e4\u00e7\3\2\2\2\u00e5\u00e3\3\2\2\2\u00e5\u00e6"+
		"\3\2\2\2\u00e6\u00e8\3\2\2\2\u00e7\u00e5\3\2\2\2\u00e8\u00e9\7\6\2\2\u00e9"+
		"\u00eb\3\2\2\2\u00ea\u00de\3\2\2\2\u00eb\u00ee\3\2\2\2\u00ec\u00ea\3\2"+
		"\2\2\u00ec\u00ed\3\2\2\2\u00ed\35\3\2\2\2\u00ee\u00ec\3\2\2\2\u00ef\u00f0"+
		"\7G\2\2\u00f0\u00f1\5H%\2\u00f1\u00f2\7C\2\2\u00f2\u00f3\5L\'\2\u00f3"+
		"\u00f4\7\7\2\2\u00f4\u00fc\5B\"\2\u00f5\u00f6\7\5\2\2\u00f6\u00f7\5L\'"+
		"\2\u00f7\u00f8\7\7\2\2\u00f8\u00f9\5B\"\2\u00f9\u00fb\3\2\2\2\u00fa\u00f5"+
		"\3\2\2\2\u00fb\u00fe\3\2\2\2\u00fc\u00fa\3\2\2\2\u00fc\u00fd\3\2\2\2\u00fd"+
		"\u0101\3\2\2\2\u00fe\u00fc\3\2\2\2\u00ff\u0100\7J\2\2\u0100\u0102\5<\37"+
		"\2\u0101\u00ff\3\2\2\2\u0101\u0102\3\2\2\2\u0102\37\3\2\2\2\u0103\u0104"+
		"\7*\2\2\u0104\u0105\7.\2\2\u0105\u0108\5H%\2\u0106\u0107\7J\2\2\u0107"+
		"\u0109\5<\37\2\u0108\u0106\3\2\2\2\u0108\u0109\3\2\2\2\u0109!\3\2\2\2"+
		"\u010a\u0115\5$\23\2\u010b\u010c\7=\2\2\u010c\u010d\7$\2\2\u010d\u0112"+
		"\5&\24\2\u010e\u010f\7\5\2\2\u010f\u0111\5&\24\2\u0110\u010e\3\2\2\2\u0111"+
		"\u0114\3\2\2\2\u0112\u0110\3\2\2\2\u0112\u0113\3\2\2\2\u0113\u0116\3\2"+
		"\2\2\u0114\u0112\3\2\2\2\u0115\u010b\3\2\2\2\u0115\u0116\3\2\2\2\u0116"+
		"\u011d\3\2\2\2\u0117\u0118\7\65\2\2\u0118\u011b\5B\"\2\u0119\u011a\7:"+
		"\2\2\u011a\u011c\5B\"\2\u011b\u0119\3\2\2\2\u011b\u011c\3\2\2\2\u011c"+
		"\u011e\3\2\2\2\u011d\u0117\3\2\2\2\u011d\u011e\3\2\2\2\u011e#\3\2\2\2"+
		"\u011f\u0121\7B\2\2\u0120\u0122\7,\2\2\u0121\u0120\3\2\2\2\u0121\u0122"+
		"\3\2\2\2\u0122\u0123\3\2\2\2\u0123\u0128\5(\25\2\u0124\u0125\7\5\2\2\u0125"+
		"\u0127\5(\25\2\u0126\u0124\3\2\2\2\u0127\u012a\3\2\2\2\u0128\u0126\3\2"+
		"\2\2\u0128\u0129\3\2\2\2\u0129\u012b\3\2\2\2\u012a\u0128\3\2\2\2\u012b"+
		"\u0135\7.\2\2\u012c\u0131\5*\26\2\u012d\u012e\7\5\2\2\u012e\u0130\5*\26"+
		"\2\u012f\u012d\3\2\2\2\u0130\u0133\3\2\2\2\u0131\u012f\3\2\2\2\u0131\u0132"+
		"\3\2\2\2\u0132\u0136\3\2\2\2\u0133\u0131\3\2\2\2\u0134\u0136\5,\27\2\u0135"+
		"\u012c\3\2\2\2\u0135\u0134\3\2\2\2\u0136\u013a\3\2\2\2\u0137\u0138\7J"+
		"\2\2\u0138\u013b\5<\37\2\u0139\u013b\7\35\2\2\u013a\u0137\3\2\2\2\u013a"+
		"\u0139\3\2\2\2\u013a\u013b\3\2\2\2\u013b%\3\2\2\2\u013c\u013e\5<\37\2"+
		"\u013d\u013f\t\2\2\2\u013e\u013d\3\2\2\2\u013e\u013f\3\2\2\2\u013f\'\3"+
		"\2\2\2\u0140\u0141\5H%\2\u0141\u0142\7\b\2\2\u0142\u0145\3\2\2\2\u0143"+
		"\u0145\5T+\2\u0144\u0140\3\2\2\2\u0144\u0143\3\2\2\2\u0145)\3\2\2\2\u0146"+
		"\u015a\5H%\2\u0147\u0151\7\4\2\2\u0148\u014d\5*\26\2\u0149\u014a\7\5\2"+
		"\2\u014a\u014c\5*\26\2\u014b\u0149\3\2\2\2\u014c\u014f\3\2\2\2\u014d\u014b"+
		"\3\2\2\2\u014d\u014e\3\2\2\2\u014e\u0152\3\2\2\2\u014f\u014d\3\2\2\2\u0150"+
		"\u0152\5,\27\2\u0151\u0148\3\2\2\2\u0151\u0150\3\2\2\2\u0152\u0153\3\2"+
		"\2\2\u0153\u0154\7\6\2\2\u0154\u015a\3\2\2\2\u0155\u0156\7\4\2\2\u0156"+
		"\u0157\5\"\22\2\u0157\u0158\7\6\2\2\u0158\u015a\3\2\2\2\u0159\u0146\3"+
		"\2\2\2\u0159\u0147\3\2\2\2\u0159\u0155\3\2\2\2\u015a+\3\2\2\2\u015b\u0162"+
		"\5*\26\2\u015c\u015d\5.\30\2\u015d\u015e\5*\26\2\u015e\u015f\5\60\31\2"+
		"\u015f\u0161\3\2\2\2\u0160\u015c\3\2\2\2\u0161\u0164\3\2\2\2\u0162\u0160"+
		"\3\2\2\2\u0162\u0163\3\2\2\2\u0163-\3\2\2\2\u0164\u0162\3\2\2\2\u0165"+
		"\u0173\7\5\2\2\u0166\u0168\7\66\2\2\u0167\u0166\3\2\2\2\u0167\u0168\3"+
		"\2\2\2\u0168\u016f\3\2\2\2\u0169\u016b\7\64\2\2\u016a\u016c\7>\2\2\u016b"+
		"\u016a\3\2\2\2\u016b\u016c\3\2\2\2\u016c\u0170\3\2\2\2\u016d\u0170\7/"+
		"\2\2\u016e\u0170\7(\2\2\u016f\u0169\3\2\2\2\u016f\u016d\3\2\2\2\u016f"+
		"\u016e\3\2\2\2\u016f\u0170\3\2\2\2\u0170\u0171\3\2\2\2\u0171\u0173\7\62"+
		"\2\2\u0172\u0165\3\2\2\2\u0172\u0167\3\2\2\2\u0173/\3\2\2\2\u0174\u0175"+
		"\7;\2\2\u0175\u0177\5<\37\2\u0176\u0174\3\2\2\2\u0176\u0177\3\2\2\2\u0177"+
		"\61\3\2\2\2\u0178\u0179\5L\'\2\u0179\u017d\5\64\33\2\u017a\u017c\5:\36"+
		"\2\u017b\u017a\3\2\2\2\u017c\u017f\3\2\2\2\u017d\u017b\3\2\2\2\u017d\u017e"+
		"\3\2\2\2\u017e\63\3\2\2\2\u017f\u017d\3\2\2\2\u0180\u0190\7\26\2\2\u0181"+
		"\u0190\7\27\2\2\u0182\u0190\7\30\2\2\u0183\u0190\7\31\2\2\u0184\u0190"+
		"\7\32\2\2\u0185\u0190\7\33\2\2\u0186\u0190\7\34\2\2\u0187\u0189\7\35\2"+
		"\2\u0188\u018a\5\66\34\2\u0189\u0188\3\2\2\2\u0189\u018a\3\2\2\2\u018a"+
		"\u018d\3\2\2\2\u018b\u018c\7\36\2\2\u018c\u018e\58\35\2\u018d\u018b\3"+
		"\2\2\2\u018d\u018e\3\2\2\2\u018e\u0190\3\2\2\2\u018f\u0180\3\2\2\2\u018f"+
		"\u0181\3\2\2\2\u018f\u0182\3\2\2\2\u018f\u0183\3\2\2\2\u018f\u0184\3\2"+
		"\2\2\u018f\u0185\3\2\2\2\u018f\u0186\3\2\2\2\u018f\u0187\3\2\2\2\u0190"+
		"\65\3\2\2\2\u0191\u0192\7L\2\2\u0192\67\3\2\2\2\u0193\u0194\7\4\2\2\u0194"+
		"\u0195\7L\2\2\u0195\u0196\7\5\2\2\u0196\u0197\7L\2\2\u0197\u0198\7\5\2"+
		"\2\u0198\u0199\7L\2\2\u0199\u019a\7\6\2\2\u019a9\3\2\2\2\u019b\u019c\7"+
		"&\2\2\u019c\u019e\5F$\2\u019d\u019b\3\2\2\2\u019d\u019e\3\2\2\2\u019e"+
		"\u01a7\3\2\2\2\u019f\u01a0\7?\2\2\u01a0\u01a8\7\63\2\2\u01a1\u01a3\78"+
		"\2\2\u01a2\u01a1\3\2\2\2\u01a2\u01a3\3\2\2\2\u01a3\u01a4\3\2\2\2\u01a4"+
		"\u01a8\79\2\2\u01a5\u01a8\7F\2\2\u01a6\u01a8\5> \2\u01a7\u019f\3\2\2\2"+
		"\u01a7\u01a2\3\2\2\2\u01a7\u01a5\3\2\2\2\u01a7\u01a6\3\2\2\2\u01a8;\3"+
		"\2\2\2\u01a9\u01aa\b\37\1\2\u01aa\u01ab\5H%\2\u01ab\u01ac\7\t\2\2\u01ac"+
		"\u01ad\5L\'\2\u01ad\u01ae\t\3\2\2\u01ae\u01af\5B\"\2\u01af\u01b9\3\2\2"+
		"\2\u01b0\u01b1\5T+\2\u01b1\u01b2\t\3\2\2\u01b2\u01b3\5T+\2\u01b3\u01b9"+
		"\3\2\2\2\u01b4\u01b5\7\4\2\2\u01b5\u01b6\5<\37\2\u01b6\u01b7\7\6\2\2\u01b7"+
		"\u01b9\3\2\2\2\u01b8\u01a9\3\2\2\2\u01b8\u01b0\3\2\2\2\u01b8\u01b4\3\2"+
		"\2\2\u01b9\u01bf\3\2\2\2\u01ba\u01bb\f\4\2\2\u01bb\u01bc\t\4\2\2\u01bc"+
		"\u01be\5<\37\5\u01bd\u01ba\3\2\2\2\u01be\u01c1\3\2\2\2\u01bf\u01bd\3\2"+
		"\2\2\u01bf\u01c0\3\2\2\2\u01c0=\3\2\2\2\u01c1\u01bf\3\2\2\2\u01c2\u01c3"+
		"\7@\2\2\u01c3\u01cf\5R*\2\u01c4\u01c5\7\4\2\2\u01c5\u01ca\5L\'\2\u01c6"+
		"\u01c7\7\5\2\2\u01c7\u01c9\5L\'\2\u01c8\u01c6\3\2\2\2\u01c9\u01cc\3\2"+
		"\2\2\u01ca\u01c8\3\2\2\2\u01ca\u01cb\3\2\2\2\u01cb\u01cd\3\2\2\2\u01cc"+
		"\u01ca\3\2\2\2\u01cd\u01ce\7\6\2\2\u01ce\u01d0\3\2\2\2\u01cf\u01c4\3\2"+
		"\2\2\u01cf\u01d0\3\2\2\2\u01d0?\3\2\2\2\u01d1\u01d3\t\5\2\2\u01d2\u01d1"+
		"\3\2\2\2\u01d2\u01d3\3\2\2\2\u01d3\u01d4\3\2\2\2\u01d4\u01d5\7L\2\2\u01d5"+
		"A\3\2\2\2\u01d6\u01d7\t\6\2\2\u01d7C\3\2\2\2\u01d8\u01d9\t\7\2\2\u01d9"+
		"E\3\2\2\2\u01da\u01db\5X-\2\u01dbG\3\2\2\2\u01dc\u01dd\5X-\2\u01ddI\3"+
		"\2\2\2\u01de\u01df\5X-\2\u01dfK\3\2\2\2\u01e0\u01e1\5X-\2\u01e1M\3\2\2"+
		"\2\u01e2\u01e3\5X-\2\u01e3O\3\2\2\2\u01e4\u01e5\5X-\2\u01e5Q\3\2\2\2\u01e6"+
		"\u01e7\5X-\2\u01e7S\3\2\2\2\u01e8\u01e9\5H%\2\u01e9\u01ea\7\t\2\2\u01ea"+
		"\u01eb\5L\'\2\u01ebU\3\2\2\2\u01ec\u01f4\7K\2\2\u01ed\u01f4\5D#\2\u01ee"+
		"\u01f4\7N\2\2\u01ef\u01f0\7\4\2\2\u01f0\u01f1\5V,\2\u01f1\u01f2\7\6\2"+
		"\2\u01f2\u01f4\3\2\2\2\u01f3\u01ec\3\2\2\2\u01f3\u01ed\3\2\2\2\u01f3\u01ee"+
		"\3\2\2\2\u01f3\u01ef\3\2\2\2\u01f4W\3\2\2\2\u01f5\u01f7\7\25\2\2\u01f6"+
		"\u01f5\3\2\2\2\u01f6\u01f7\3\2\2\2\u01f7\u01f8\3\2\2\2\u01f8\u01f9\5V"+
		",\2\u01f9Y\3\2\2\2\65\\^ipu{\u008a\u00a7\u00bd\u00cc\u00d1\u00da\u00e5"+
		"\u00ec\u00fc\u0101\u0108\u0112\u0115\u011b\u011d\u0121\u0128\u0131\u0135"+
		"\u013a\u013e\u0144\u014d\u0151\u0159\u0162\u0167\u016b\u016f\u0172\u0176"+
		"\u017d\u0189\u018d\u018f\u019d\u01a2\u01a7\u01b8\u01bf\u01ca\u01cf\u01d2"+
		"\u01f3\u01f6";
	public static final ATN _ATN =
		new ATNDeserializer().deserialize(_serializedATN.toCharArray());
	static {
		_decisionToDFA = new DFA[_ATN.getNumberOfDecisions()];
		for (int i = 0; i < _ATN.getNumberOfDecisions(); i++) {
			_decisionToDFA[i] = new DFA(_ATN.getDecisionState(i), i);
		}
	}
}