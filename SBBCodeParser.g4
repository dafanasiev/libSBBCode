parser grammar SBBCodeParser;
options {
    tokenVocab = SBBCodeLexer;
}

parse        : element* ;

element
    : content
    | tag
    ;

content
    : value=TEXT
    ;

tag
    : tag_open element* tag_close
    ;

tag_open
    : '[' name=ID (attribute)* ']'
    ;

tag_close
    : '[' '/' name=ID ']'
    ;

attribute
    : name=ID '=' value=(DQSTRING|QSTRING|INTNUMBER|FLOATNUMBER|TRUE|FALSE)
    ;
