grammar BareBonesSql;

@parser::header {#pragma warning disable 3021}
@lexer::header {#pragma warning disable 3021}

parse: ( sql_stmt_list | error)* EOF;

error:
	UNEXPECTED_CHAR { 
     throw new System.Exception("UNEXPECTED_CHAR=" + $UNEXPECTED_CHAR.text); 
   };

sql_stmt_list: sql_stmt ';' ( sql_stmt ';' )* ;

sql_stmt: (
		create_database_stmt
		| drop_database_stmt
		| alter_table_stmt
		| create_table_stmt
		| drop_table_stmt
		| insert_stmt
		| update_stmt
		| delete_stmt
		| simple_select_stmt
		| use_database_stmt
		| current_database_stmt
		| list_databases_stmt
		| if_stmt
		| transaction_sql_stmt
	);

operation_sql_stmt: (
		| insert_stmt
		| update_stmt
		| delete_stmt
		| if_stmt
	);	

transaction_sql_stmt: begin_stmt operation_sql_stmt ';' ( operation_sql_stmt ';' )* commit_stmt;

use_database_stmt: K_USE database_name;

current_database_stmt: K_CURRENT_DATABASE;

begin_stmt: K_BEGIN;

commit_stmt: K_COMMIT;

rollback_stmt: K_ROLLBACK;

list_databases_stmt: K_LIST_DATABASES;

get_structure_stmt: K_GET_STRUCTURE database_name;

create_database_stmt: K_CREATE K_DATABASE database_name;

drop_database_stmt: K_DROP K_DATABASE database_name;

create_table_stmt:
	K_CREATE K_TABLE table_name (
		'(' column_def ( ',' column_def)* ')'
	);

alter_table_stmt:
	K_ALTER K_TABLE table_name (
		K_RENAME K_TO new_table_name
		| K_ADD K_COLUMN column_def
		| K_DROP K_COLUMN column_name
		| K_RENAME column_name K_TO new_column_name
	);

drop_table_stmt: K_DROP K_TABLE table_name;

insert_stmt:
	K_INSERT K_INTO table_name (
		'(' column_name ( ',' column_name)* ')'
	)? (
		K_VALUES '(' literal_value (',' literal_value)* ')' (
			',' '(' literal_value ( ',' literal_value)* ')'
		)*
	);

update_stmt:
	K_UPDATE table_name K_SET column_name '=' ( expr | case_expr ) (',' column_name '=' ( expr | case_expr ))* (K_WHERE expr)?;

delete_stmt: K_DELETE K_FROM table_name ( K_WHERE expr)?;

operator:
( '<' 
| '<=' 
| '>' 
| '>=' 
| '=' 
| '!=' );

if_stmt: K_IF simple_select_stmt K_EXECUTE '{' sql_stmt  ';' ( sql_stmt ';')* '}';

simple_select_stmt:
	select_core (K_ORDER K_BY ordering_term ( ',' ordering_term)*)? (
		K_LIMIT literal_value ( K_OFFSET literal_value )?
	)?;

select_core:
	K_SELECT ( K_DISTINCT )? (result_column | case_expr) (',' (result_column | case_expr))* 
     K_FROM (table_or_subquery (',' table_or_subquery)* | join_clause)
	 (K_WHERE expr | K_ENCRYPTED)?;

ordering_term: expr ( K_ASC | K_DESC)?;

result_column: table_name '.*' | table_column_name;

table_or_subquery:
	table_name
	| '(' (
		table_or_subquery ( ',' table_or_subquery)*
		| join_clause
	) ')'
	| '(' simple_select_stmt ')';

join_clause:
	table_or_subquery (
		join_operator table_or_subquery join_constraint
	)*;

join_operator:
	','
	| K_NATURAL? (K_LEFT K_OUTER? | K_INNER | K_CROSS)? K_JOIN;

join_constraint: (
		K_ON expr
//		| K_USING '(' column_name ( ',' column_name)* ')'
	)?;

column_def: column_name data_type column_constraint*;

data_type: (
		K_BOOL
		| K_DATETIME
		| K_DURATION
		| K_INT
		| K_DECIMAL
		| K_DOUBLE
		| K_TEXT
		| K_ENCRYPTED bucket_number? (K_RANGE bucket_range)?
	);

bucket_number: NUMERIC_LITERAL;

bucket_range: '(' NUMERIC_LITERAL ',' NUMERIC_LITERAL ',' NUMERIC_LITERAL ')';

column_constraint: (K_CONSTRAINT name)? (
		K_PRIMARY K_KEY
		| K_NOT? K_NULL
		| K_UNIQUE
		| foreign_key_clause
	);

expr:  
	literal_value
	| table_name '.' column_name operator literal_value
	| table_column_name operator table_column_name
	| expr (K_AND | K_OR) expr
	| '(' expr ')'
	| expr K_NOT? K_IN ( '(' ( expr ( ',' expr )*
                          )? 
                      ')'
                    | ( database_name '.' )? table_name );

case_expr:
	K_CASE table_column_name? ( K_WHEN expr K_THEN expr )+ ( K_ELSE expr )? K_END result_column?;

foreign_key_clause:
	K_REFERENCES foreign_table (
		'(' column_name ( ',' column_name)* ')'
	)?;

signed_number: ( '+' | '-')? NUMERIC_LITERAL;

literal_value: NUMERIC_LITERAL | STRING_LITERAL | K_NULL;

keyword:
	K_USE
	| K_CURRENT_DATABASE
	| K_LIST_DATABASES
	| K_GET_STRUCTURE
	| K_NOT_TO_ENCRYPT
	| K_BOOL
	| K_DATETIME
	| K_DURATION
	| K_INT
	| K_DECIMAL
	| K_DOUBLE
	| K_TEXT
	| K_ENCRYPTED
	| K_RANGE
	| K_ADD
	| K_ALL
	| K_ALTER
	| K_AND
	| K_ASC
	| K_BY
	| K_COLUMN
	| K_CONSTRAINT
	| K_CREATE
	| K_CROSS
	| K_DATABASE
	| K_DELETE
	| K_DESC
	| K_DISTINCT
	| K_DROP
	| K_EXECUTE
	| K_FROM
	| K_IF
	| K_INNER
	| K_INSERT
	| K_INTO
	| K_JOIN
	| K_KEY
	| K_LEFT
	| K_LIMIT
	| K_NATURAL
	| K_NO
	| K_NOT
	| K_NULL
	| K_OFFSET
	| K_ON
	| K_OR
	| K_ORDER
	| K_OUTER
	| K_PRIMARY
	| K_REFERENCES
	| K_RENAME
	| K_SELECT
	| K_SET
	| K_TABLE
	| K_TO
	| K_UNIQUE
	| K_UPDATE
	| K_USING
	| K_VALUES
	| K_WHERE
	| K_CASE
	| K_WHEN
	| K_THEN
	| K_ELSE
	| K_END
	| K_BEGIN
	| K_COMMIT
	| K_ROLLBACK;

name: complex_name;

table_name: complex_name;

new_table_name: complex_name;

column_name: complex_name;

new_column_name: complex_name;

database_name: complex_name;

foreign_table: complex_name;

table_column_name: table_name '.' column_name;

any_name:
	IDENTIFIER
	| keyword
	| STRING_LITERAL
	| '(' any_name ')';

complex_name: K_NOT_TO_ENCRYPT? any_name;

fragment DIGIT: [0-9];

fragment A: [aA];
fragment B: [bB];
fragment C: [cC];
fragment D: [dD];
fragment E: [eE];
fragment F: [fF];
fragment G: [gG];
fragment H: [hH];
fragment I: [iI];
fragment J: [jJ];
fragment K: [kK];
fragment L: [lL];
fragment M: [mM];
fragment N: [nN];
fragment O: [oO];
fragment P: [pP];
fragment Q: [qQ];
fragment R: [rR];
fragment S: [sS];
fragment T: [tT];
fragment U: [uU];
fragment V: [vV];
fragment W: [wW];
fragment X: [xX];
fragment Y: [yY];
fragment Z: [zZ];

// http://www.sqlite.org/lang_keywords.html

K_USE: U S E;
K_CURRENT_DATABASE: C U R R E N T '_' D A T A B A S E;
K_LIST_DATABASES: L I S T;
K_GET_STRUCTURE: G E T '_' S T R U C T U R E;
K_NOT_TO_ENCRYPT: '!';
K_IF: I F;
K_EXECUTE: E X E C U T E;

K_BOOL: B O O L;
K_DATETIME: D A T E T I M E;
K_DURATION: D U R A T I O N;
K_INT: I N T;
K_DECIMAL: D E C I M A L;
K_DOUBLE: D O U B L E;
K_TEXT: T E X T;

K_ENCRYPTED: E N C R Y P T E D;
K_RANGE: R A N G E;

K_ADD: A D D;
K_ALL: A L L;
K_ALTER: A L T E R;
K_AND: A N D;
K_ASC: A S C;
K_BY: B Y;
K_COLUMN: C O L U M N;
K_CONSTRAINT: C O N S T R A I N T;
K_CREATE: C R E A T E;
K_CROSS: C R O S S;
K_DATABASE: D A T A B A S E;
K_DELETE: D E L E T E;
K_DESC: D E S C;
K_DISTINCT: D I S T I N C T;
K_DROP: D R O P;
K_FROM: F R O M;
K_INNER: I N N E R;
K_INSERT: I N S E R T;
K_INTO: I N T O;
K_JOIN: J O I N;
K_KEY: K E Y;
K_LEFT: L E F T;
K_LIMIT: L I M I T;
K_NATURAL: N A T U R A L;
K_NO: N O;
K_NOT: N O T;
K_NULL: N U L L;
K_OFFSET: O F F S E T;
K_ON: O N;
K_OR: O R;
K_ORDER: O R D E R;
K_OUTER: O U T E R;
K_PRIMARY: P R I M A R Y;
K_REFERENCES: R E F E R E N C E S;
K_RENAME: R E N A M E;
K_SELECT: S E L E C T;
K_SET: S E T;
K_TABLE: T A B L E;
K_TO: T O;
K_UNIQUE: U N I Q U E;
K_UPDATE: U P D A T E;
K_USING: U S I N G;
K_VALUES: V A L U E S;
K_WHERE: W H E R E;

K_CASE: C A S E;
K_WHEN: W H E N;
K_THEN: T H E N;
K_END: E N D;
K_ELSE: E L S E;

K_BEGIN: B E G I N;
K_COMMIT: C O M M I T;
K_ROLLBACK: R O L L B A C K;

IDENTIFIER:
	'"' (~'"' | '""')* '"'
	| '`' (~'`' | '``')* '`'
	| '[' ~']'* ']'
	| [a-zA-Z_] [a-zA-Z_0-9]* ; // TODO check: needs more chars in set

NUMERIC_LITERAL:
	DIGIT+ ('.' DIGIT*)? (E [-+]? DIGIT+)?
	| '.' DIGIT+ ( E [-+]? DIGIT+)?;

BIND_PARAMETER: '?' DIGIT* | [:@$] IDENTIFIER;

STRING_LITERAL: '\'' ( ~'\'' | '\'\'')* '\'';