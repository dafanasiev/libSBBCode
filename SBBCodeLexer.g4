lexer grammar SBBCodeLexer;

OPEN                : '[' -> pushMode(BBCODE) ;
TEXT                : ~('[' | ']')+ ;

// Parsing content inside tags
mode BBCODE;

CLOSE               : ']' -> popMode ;
SLASH               : '/' ;
EQUALS              : '=' ;
COMMA               : ',' ;
DQSTRING             : '"' .*? '"' ;
QSTRING             : '\'' .*? '\'' ;
TRUE : T R U E ;
FALSE : F A L S E ;
WS                  : [ \t\r\n] -> skip ;
INTNUMBER           : ('+'|'-')? [0-9]+ ;
FLOATNUMBER         : ('+'|'-')? [0-9]+ '.' ;
ID                  : LETTERS+ ;

fragment LETTERS    : [a-zA-Z] ;


fragment A : [aA];
fragment B : [bB];
fragment C : [cC];
fragment D : [dD];
fragment E : [eE];
fragment F : [fF];
fragment G : [gG];
fragment H : [hH];
fragment I : [iI];
fragment J : [jJ];
fragment K : [kK];
fragment L : [lL];
fragment M : [mM];
fragment N : [nN];
fragment O : [oO];
fragment P : [pP];
fragment Q : [qQ];
fragment R : [rR];
fragment S : [sS];
fragment T : [tT];
fragment U : [uU];
fragment V : [vV];
fragment W : [wW];
fragment X : [xX];
fragment Y : [yY];
fragment Z : [zZ];