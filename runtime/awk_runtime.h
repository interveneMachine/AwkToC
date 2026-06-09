#ifndef AWK_RUNTIME_H
#define AWK_RUNTIME_H

#include <stddef.h>
#include <stdio.h>

extern char* FS;
extern char* CONVFMT;
extern int NR;
extern char* SUBSEP;

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

typedef struct ArrayEntry
{
    char* key;
    AwkValue value;
    struct ArrayEntry* next;
} ArrayEntry;

typedef struct
{
    ArrayEntry** entries;
    size_t capacity;
    size_t size;
} Array;

typedef struct
{
    char** keys;
    size_t size;
    size_t i;
} ArrayIterator;


Array* array_init();
AwkValue array_get_value(Array* array, AwkValue key);
void array_set_value(Array* array, AwkValue key, AwkValue value);
void array_delete_value(Array* array, AwkValue key);
void array_delete(Array* array);
void array_free(Array* array);
ArrayIterator arrayiterator_init(Array* array);
int arrayiterator_is_end(ArrayIterator* iter);
void arrayiterator_next(ArrayIterator* iter);
void arrayiterator_free(ArrayIterator* iter);

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
AwkValue awk_concat_array_arg(size_t count, AwkValue* values);


void awk_set_default_predefined();
void awk_print_value(AwkValue value, FILE* stream, int type);
void awk_print_values(size_t count, AwkValue* values, FILE* stream, int type);
char* awk_strdup(const char* value);
FILE* awk_output_redirection_write(AwkValue value);
FILE* awk_output_redirection_append(AwkValue value);
FILE* awk_output_redirection_pipe(AwkValue value);

AwkValue awk_atan2(AwkValue x, AwkValue y);
AwkValue awk_cos(AwkValue value);
AwkValue awk_sin(AwkValue value);
AwkValue awk_exp(AwkValue value);
AwkValue awk_log(AwkValue value);
AwkValue awk_sqrt(AwkValue value);
AwkValue awk_int(AwkValue value);

#endif
