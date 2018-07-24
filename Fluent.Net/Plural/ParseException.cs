using System;

namespace Fluent.Net.Plural
{
    /*
    rules         = rule (';' rule)*
    rule          = keyword ':' condition samples
                  | 'other' ':' samples
    keyword       = [a-z]+
    condition     = and_condition ('or' and_condition)*
    samples       = ('@integer' sampleList)?
                    ('@decimal' sampleList)?                
    and_condition = relation ('and' relation)*
    relation      = is_relation | in_relation | within_relation 
    is_relation   = expr 'is' ('not')? value
    in_relation   = expr (('not')? 'in' | '=' | '!=') range_list
    within_relation = expr ('not')? 'within' range_list
    expr          = operand (('mod' | '%') value)?
    operand       = 'n' | 'i' | 'f' | 't' | 'v' | 'w'
    range_list    = (range | value) (',' range_list)*
    range         = value'..'value
    sampleList    = sampleRange (',' sampleRange)* (',' ('…'|'...'))?
    sampleRange   = decimalValue ('~' decimalValue)?
    value         = digit+
    decimalValue  = value ('.' value)?
    digit         = 0|1|2|3|4|5|6|7|8|9
    */
    public class ParseException : Exception
    {
        public ParseException(string msg) :
            base(msg)
        {
        }
    }
}
