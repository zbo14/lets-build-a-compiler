VARIABLE idx
: IDX@ idx @ ;
: IDX! idx ! ;
: IDX0 0 IDX! ;
: IDX+ IDX@ 1+ IDX! ;

VARIABLE look
: LOOK@ look c@ ;

VARIABLE namelen
0 namelen !
: NAMELEN@ namelen @ ;
: NAMELEN! namelen ! ;
: NAMELEN+ NAMELEN@ 1+ NAMELEN! ;
: RESETNAMELEN 0 NAMELEN! ;

VARIABLE num
: NUM@ num @ ;
: NUM? num ? ;
: NUM! num ! ;
: RESETNUM 0 NUM! ;

VARIABLE nxt
0 nxt !
: NXT@ nxt @ ;
: NXT! nxt ! ;
: NXT+ NXT@ 1+ NXT! ;

CREATE name
name 32 chars allot
: NAME@ name NAMELEN@ ;

CREATE names
names 64 cells allot
: NAMES@ names IDX@ CELLS + 2@ ;

CREATE nums
nums 64 cells allot
: NUMS@ nums IDX@ CELLS + @ ;

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

: GETCHAR KEY DUP look c! EMIT ;

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

: ADDCHAR
    LOOK@ TOUPPER name NAMELEN@ chars + c!
    NAMELEN+ ;

: GETNAME
    SKIPWHITESPACE
    ISALPHA
    IF
        RESETNAMELEN
        BEGIN
            ISALNUM WHILE
            ADDCHAR
            GETCHAR
            SKIPWHITESPACE
        REPEAT
    ELSE ERROR ." Name" EXPECTED
    THEN SKIPWHITESPACE ;

: GETNUM
    ISDIGIT
    IF
        RESETNUM
        BEGIN
            ISDIGIT WHILE
            10 NUM@ * LOOK@ '0' - + NUM!
            GETCHAR
        REPEAT
    ELSE ERROR ." Number" EXPECTED
    ENDIF ;

: INIT CR GETCHAR SKIPWHITESPACE ;

\ Table

: LOOKUPCOND NAME@ NAMES@ COMPARE 0 <> ;

: LOOKUP
    IDX0
    BEGIN
        LOOKUPCOND WHILE
        IDX+
    REPEAT
    LOOKUPCOND
    IF ERROR ." Variable name" EXPECTED
    ELSE NUMS@ NUM!
    ENDIF ;

: INSERT
    IDX0
    BEGIN
        IDX@ NXT@ < IF LOOKUPCOND ELSE 0 ENDIF WHILE
        IDX+
    REPEAT
    IDX@ NXT@ <
    IF
        NAME@ names IDX@ CELLS + 2!
        NUM@ nums IDX@ CELLS + !
    ELSE
        NAME@ names NXT@ CELLS + 2!
        NUM@ nums NXT@ CELLS + !
        NXT+
    ENDIF ;

\ Factor

DEFER express

: FACTOR
    LOOK@ '(' =
    IF
        '(' MATCH
        express
        ')' MATCH
    ELSE
        ISALPHA
        IF GETNAME LOOKUP
        ELSE GETNUM
        ENDIF
    ENDIF ;

: MUL
    '*' MATCH
    NUM@ >R
    FACTOR
    R> NUM@ * NUM! ;

: DIV
    '/' MATCH
    NUM@ >R
    FACTOR
    R> NUM@ / NUM! ;

: MULOP
    LOOK@ CASE
        '*' OF MUL ENDOF
        '/' OF DIV ENDOF
        ERROR ." Mul/Div" EXPECTED ENDOF
    ENDCASE ;

: ISMULOP LOOK@ '*' = LOOK@ '/' = OR ;

\ Term

: TERM
    FACTOR
    BEGIN
        ISMULOP WHILE
        MULOP
    REPEAT ;

: ADD
    '+' MATCH
    NUM@ >R
    TERM
    R> NUM@ + NUM! ;

: SUB
    '-' MATCH
    NUM@ >R
    TERM
    R> NUM@ - NUM! ;

: ADDOP
    LOOK@ CASE
        '+' OF ADD ENDOF
        '-' OF SUB ENDOF
        ERROR ." Add/Sub" EXPECTED ENDOF
    ENDCASE ;

: ISADDOP LOOK@ '+' = LOOK@ '-' = OR ;

: EXPRESSION
    ISADDOP
    IF RESETNUM
    ELSE TERM
    THEN
        BEGIN
            ISADDOP WHILE
            ADDOP
        REPEAT ;

' EXPRESSION IS express

: NEWLINE
    13 LOOK@ =
    IF
        GETCHAR
        10 LOOK@ =
        IF GETCHAR
        ENDIF
    ENDIF ;

: ASSIGNMENT
    GETNAME
    '=' MATCH
    EXPRESSION
    NUM@ EMIT
    INSERT ;

: INPUT
    '?' MATCH
    GETNAME
    INSERT ;

: OUTPUT
    '!' MATCH
    GETNAME
    LOOKUP
    NUM? ;

: INTERPRETER
    INIT
    BEGIN
        LOOK@ CASE
            '?' OF INPUT ENDOF
            '!' OF OUTPUT ENDOF
            ASSIGNMENT ENDOF
        ENDCASE
        NEWLINE
    '.' LOOK@ = UNTIL ;
