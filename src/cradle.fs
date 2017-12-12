VARIABLE look
: LOOK@ look c@ ;

CREATE name
name 32 chars allot

VARIABLE namelen

VARIABLE num
num 32 chars allot

VARIABLE numlen

\ ---------------- BEGIN LICENSE ----------------
\
\ MIT License
\
\ Copyright (c) 2017 Edward Ye
\
\ Permission is hereby granted, free of charge, to any person obtaining a copy
\ of this software and associated documentation files (the "Software"), to deal
\ in the Software without restriction, including without limitation the rights
\ to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
\ copies of the Software, and to permit persons to whom the Software is
\ furnished to do so, subject to the following conditions:

\ The above copyright notice and this permission notice shall be included in all
\ copies or substantial portions of the Software.

\ THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
\ IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
\ FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
\ AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
\ LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
\ OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
\ SOFTWARE.

\ Adapted from https://github.com/hahahahaman/Let-s-Build-A-Compiler/blob/master/forth/1.fs

: ISALPHA LOOK@ TOUPPER 'A' 'Z' 1+ WITHIN ;

: ISDIGIT LOOK@ '0' '9' 1+ WITHIN ;

: ERROR CR ." Error: " ;

\ ----------------- END LICENSE -----------------

: ISALNUM ISALPHA ISDIGIT OR ;

: ISWHITESPACE LOOK@ 32 = LOOK@ 9 = OR ;

: GETCHAR DUP KEY look c! EMIT ;

: SKIPWHITESPACE
    BEGIN
        ISWHITESPACE WHILE
        GETCHAR
    REPEAT ;

: EXPECTED ."  expected" CR ABORT ;

: MATCH
    DUP
    LOOK@ =
    IF GETCHAR SKIPWHITESPACE
    ELSE >R ERROR R> EMIT EXPECTED
    ENDIF ;

: NEWNAME 0 namelen c! ;

: NAMELEN@ namelen c@ ;

: ADDCHAR
    LOOK@ TOUPPER name NAMELEN@ chars + c!
    NAMELEN@ 1+ namelen c! ;

: GETNAME
    ISALPHA
    IF
        NEWNAME
        BEGIN
            ISALNUM WHILE
            ADDCHAR
            GETCHAR
        REPEAT
    ELSE ERROR ." Name" EXPECTED
    THEN SKIPWHITESPACE name ;

: NEWNUM 0 numlen c! ;

: NUMLEN@ numlen c@ ;

: ADDNUM
    LOOK@ num NUMLEN@ chars + c!
    NUMLEN@ 1+ numlen c! ;

: GETNUM
    ISDIGIT
    IF
        NEWNUM
        BEGIN
            ISDIGIT WHILE
            ADDNUM
            GETCHAR
        REPEAT
    ELSE ERROR ." Number" EXPECTED
    THEN SKIPWHITESPACE num ;

: INIT CR GETCHAR SKIPWHITESPACE ;

: IDENT
    GETNAME 2>R
    LOOK@ '(' =
    IF
        '(' MATCH
        ')' MATCH
        ." BSR " 2R> TYPE CR
    ELSE
        ." MOVE " 2R> TYPE ." (PC),D0" CR
    ENDIF ;

DEFER express

: FACTOR
    LOOK@ '(' =
    IF
        '(' MATCH
        express
        ')' MATCH
    ELSE ISALPHA
        IF IDENT
        ELSE GETNUM >R ." MOVE #" R> NAMELEN@ TYPE ." ,D0" CR
        ENDIF
    ENDIF ;

: MUL
    '*' MATCH
    FACTOR
    ." MULS (SP)+,D0" CR ;

: DIV
    '/' MATCH
    FACTOR
    ." MOVE (SP)+,D1" CR
    ." DIVS D1,D0" CR ;

: MULOP
    ." MOVE D0,-(SP)" CR
    LOOK@ CASE
        '*' OF MUL ENDOF
        '/' OF DIV ENDOF
        ERROR ." Mul/Div" EXPECTED ENDOF
    ENDCASE ;

: ISMULOP LOOK@ '*' = LOOK@ '/' = OR ;

: TERM
    FACTOR
    BEGIN
        ISMULOP WHILE
        MULOP
    REPEAT ;

: ADD
    '+' MATCH
    TERM
    ." ADD (SP)+,D0" CR ;

: SUB
    '-' MATCH
    TERM
    ." SUB (SP)+,D0" CR
    ." NEG DO" ;

: ADDOP
    ." MOVE D0,-(SP)" CR
    LOOK@ CASE
        '+' OF ADD ENDOF
        '-' OF SUB ENDOF
        ERROR ." Add/Sub" EXPECTED ENDOF
    ENDCASE ;

: ISADDOP LOOK@ '+' = LOOK@ '-' = OR ;

: EXPRESSION
    ISADDOP
    IF ." CLR D0" CR
    ELSE TERM
    THEN
        BEGIN
            ISADDOP WHILE
            ADDOP
        REPEAT ;

' EXPRESSION IS express

: ASSIGNMENT
    GETNAME >R
    '=' MATCH
    EXPRESSION
    ." LEA " R> NAMELEN@ TYPE ." (PC),A0" CR
    ." MOVE D0,(A0)" CR ;

: CRADLE
    INIT
    ASSIGNMENT
    LOOK@ 13 <>
    IF ERROR ." Newline" EXPECTED
    ENDIF ;
