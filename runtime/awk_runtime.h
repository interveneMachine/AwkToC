#ifndef AWK_RUNTIME_H
#define AWK_RUNTIME_H

#include <stddef.h>

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


AwkValue awk_undefined(void);
AwkValue awk_number(double value);
AwkValue awk_string(const char* value);
AwkValue awk_copy(AwkValue value);
void awk_free(AwkValue* value);


double awk_to_number(AwkValue value);
char* awk_to_string(AwkValue value);
int awk_is_truthy(AwkValue value);



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
