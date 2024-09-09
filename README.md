# Shank and Tran

## Shank

### Description

A simple, interpreted _and_ compiled systems language with ranged types.

This was a group project.

### Quick Start

Run the following commands in PowerShell to automatically run 10 test programs through the interpreter and compiler and see the source programs, their outputs, and the commands used to run them.

```
git clone https://github.com/wharvex/NewShank.git
. .\ShankTestScript.ps1
st
```

### Research Poster

[Link.](https://github.com/wharvex/NewShank/blob/master/shank_poster.pdf)

### Pseudo-EBNF Rules

#### Key

* `this` = exactly 1 this
* `this | that` = this or that
* `this that` = this then that
* `( that )` = 1 or more that(s)
* `{ that }` = 0 or more that(s)
* `[ that ]` = 0 or 1 that(s)
* `< this | that > the_other` = (this or that) then the\_other, as opposed to this or (that then the\_other)
* `this = that` = this is made up of (can be rewritten as) that
* `Terminal` (pascal case, should be a TokenType)
* `non_terminal` (snake case)
* `~SomeTerminal` = any terminal except SomeTerminal
* Non-terminal "first words" in order of increasing granularity:
  * `file`
  * `construct`
  * `line`
  * `segment`
  * `token`

#### Rules

001. `file = construct_outerLevel_top { construct_outerLevel_standard }`
002. `construct_innerLevel_body = token_indent ( line ) token_dedent`
003. `construct_outerLevel_standard = construct_definition_type | line_declaration_variable | line_directive_scope`
004. `construct_outerLevel_top = construct_outerLevel_standard | line_declaration_scope`
005. `segment_declaration_variable_constant = token_constants { segment_declaration_variable_constant_core token_semicolon } segment_declaration_variable_constant_core`
006. `segment_declaration_variable_constant_core = token_identifier token_equals segment_expression`
007. `segment_directive_export = token_export segment_set_identifier`
008. `segment_endOfLine = ~LineBreak EndOfLine`
009. `segment_expression = segment_expression_literal | segment_expression_variable | segment_expression_mathOp`
010. `segment_expression_literal = token_number | token_true | token_false | token_stringLiteral | token_characterLiteral`
011. `segment_expression_mathOp_base = segment_expression_mathOppable { segment_punctuation_mathOperator segment_expression_mathOppable }`
012. `segment_expression_mathOp_paren = token_leftParen segment_expression_mathOp_base token_rightParen`
013. `segment_expression_mathOppable = token_number | token_stringLiteral | segment_expression_variable`
014. `segment_expression_variable = segment_expression_variable_base { token_dot segment_expression_variable_base }`
015. `segment_expression_variable_base = token_identifier { token_leftBracket segment_expression token_rightBracket }`
016. `segment_punctuation_mathOperator = token_plus | token_minus | token_times | token_divide | token_modulo`
017. `segment_set_identifier = { token_identifier token_comma } token_identifier`
018. `segment_statement_assignment = segment_expression_variable token_assignment segment_expression`
019. `segment_type_array = token_array segment_range token_of segment_type`
020. `token_array = Array`
021. `token_assignment = Assignment`
022. `token_characterLiteral = CharacterLiteral`
023. `token_colon = Colon`
024. `token_comma = Comma`
025. `token_constants = Constants`
026. `token_dedent = Dedent`
027. `token_divide = Divide`
028. `token_dot = Dot`
029. `token_equals = Equals`
030. `token_export = Export`
031. `token_false = False`

#### Rules (old scheme)

024. `from_keyword_token = From`
025. `function_definition_form = function_definition_header_line { variable_declaration_line } function_definition_body`
027. `identifier_or_type_keyword_segment = { < identifier_token | type_unit > comma_token } < identifier_token | type_unit >`
028. `identifier_segment_bracket_group = left_bracket_token identifier_segment right_bracket_token`
029. `identifier_token = Identifier`
030. `if_statement = if_token left_paren_token expression right_paren_token body`
031. `import_keyword_token = Import`
032. `import_line = Import < identifier_segment | < identifier_token identifier_segment_bracket_group > >`
033. `indent_token = Indent`
034. `left_bracket_token = LeftBracket`
035. `minus_token = Minus`
036. `module_keyword_token = Module`
037. `modulo_token = Mod`
038. `mutable_variable_declaration_line = variables_keyword_token mutable_variable_declaration_line_segment EndOfLine`
039. `mutable_variable_declaration_line_segment = identifier_segment Colon type_segment`
040. `number_token = Number`
041. `of_keyword_token = Of`
042. `plus_token = Plus`
043. `program = ( file )`
044. `range_segment = from_keyword_token number_token to_keyword_token number_token`
045. `ranged_type_segment = array_type_segment | simple_ranged_type_segment `
046. `record_definition_form = record_definition_header_line record_definition_body`
047. `refers_to_keyword_token = RefersTo`
048. `right_bracket_token = RightBracket`
049. `scope_declaration_line = module_keyword_token identifier_token end_of_line_token`
050. `scope_directive_line = import_line | export_line`
051. `semicolon_token = Semicolon`
052. `simple_rangeable_type_unit = Integer | String | Real | Character`
053. `simple_ranged_type_segment = simple_rangeable_type_unit range_segment`
054. `statement = assignment_statement | if_statement | for_statement | while_statement | repeat_statement | function_call_statement`
055. `statement_line = statement end_of_line_token`
056. `string_literal_token = StringLiteral`
057. `term = factor { < times_token | modulo_token | divide_token > factor }`
058. `times_token = Times`
059. `to_keyword_token = To`
061. `true_token = True`
062. `type_definition_construct = type_definition_form | type_definition_line`
063. `type_definition_form = record_definition_form | function_definition_form`
064. `type_definition_line = enum_definition_line`
065. `type_segment = ranged_type_segment | simple_type_unit | unknown_type_segment`
052. `type_unit = Integer | String | Real | Character | Boolean`
052. `type_or_identifier_unit = Integer | String | Real | Character | Boolean`
066. `unknown_type_segment = [ refers_to_keyword_token ] identifier_token { identifier_or_type_keyword_segment }`
067. `variable_declaration_line = mutable_variable_declaration_line | constant_variable_declaration_line`
070. `variables_keyword_token = Variables`

#### Examples

##### type\_definition\_construct

```
record Student
    name : string
    age : integer
```

OR

```
enum color = [red, green, blue]
```
