#ifndef AWK_RUNTIME_H
#define AWK_RUNTIME_H

#include <stddef.h>

extern char* FS;

typedef enum
{
    AWK_UNDEFINED,
    AWK_NUMBER,
    AWK_STRING
} AwkValueType;

typedef struct
{
    AwkValueType type;
    double number;
    char* string;
} AwkValue;

typedef struct
{
    char** data;
    size_t size;
} Fields;

void remove_newline(char* value);
Fields fields_string(char* value);
AwkValue fields_get(Fields fields, int id);
AwkValue fields_assign(Fields* fields, int id, AwkValue value);
void fields_free(Fields* value);


AwkValue awk_undefined(void);
AwkValue awk_number(double value);
AwkValue awk_string(const char* value);
AwkValue awk_copy(AwkValue value);
AwkValue awk_match(AwkValue value, const char* regex_cstring);
void awk_free(AwkValue* value);


double awk_to_number(AwkValue value);
char* awk_to_string(AwkValue value);
int awk_is_truthy(AwkValue value);
int awk_to_int(AwkValue value);



AwkValue awk_add(AwkValue left, AwkValue right);
AwkValue awk_sub(AwkValue left, AwkValue right);
AwkValue awk_mul(AwkValue left, AwkValue right);
AwkValue awk_div(AwkValue left, AwkValue right);
AwkValue awk_mod(AwkValue left, AwkValue right);
AwkValue awk_pow(AwkValue left, AwkValue right);

AwkValue awk_unary_plus(AwkValue value);
AwkValue awk_unary_minus(AwkValue value);



AwkValue awk_eq(AwkValue left, AwkValue right);
AwkValue awk_ne(AwkValue left, AwkValue right);
AwkValue awk_lt(AwkValue left, AwkValue right);
AwkValue awk_le(AwkValue left, AwkValue right);
AwkValue awk_gt(AwkValue left, AwkValue right);
AwkValue awk_ge(AwkValue left, AwkValue right);



AwkValue awk_not(AwkValue value);


AwkValue awk_concat(AwkValue left, AwkValue right);



void awk_print_value(AwkValue value);
void awk_print_values(size_t count, AwkValue* values);

#endif
