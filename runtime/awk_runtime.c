#include "awk_runtime.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>


static char* awk_strdup(const char* value)
{
    if (value == NULL)
    {
        char* empty = malloc(1);
        if (empty == NULL)
        {
            fprintf(stderr, "Memory allocation failed\n");
            exit(1);
        }

        empty[0] = '\0';
        return empty;
    }

    size_t length = strlen(value);
    char* copy = malloc(length + 1);

    if (copy == NULL)
    {
        fprintf(stderr, "Memory allocation failed\n");
        exit(1);
    }

    memcpy(copy, value, length + 1);
    return copy;
}

static AwkValue awk_bool(int value)
{
    return awk_number(value ? 1.0 : 0.0);
}


AwkValue awk_undefined(void)
{
    AwkValue value;
    value.type = AWK_UNDEFINED;
    value.number = 0.0;
    value.string = NULL;
    return value;
}

AwkValue awk_number(double number)
{
    AwkValue value;
    value.type = AWK_NUMBER;
    value.number = number;
    value.string = NULL;
    return value;
}

AwkValue awk_string(const char* string)
{
    AwkValue value;
    value.type = AWK_STRING;
    value.number = 0.0;
    value.string = awk_strdup(string);
    return value;
}

AwkValue awk_copy(AwkValue value)
{
    if (value.type == AWK_STRING)
    {
        return awk_string(value.string);
    }

    if (value.type == AWK_NUMBER)
    {
        return awk_number(value.number);
    }

    return awk_undefined();
}

void awk_free(AwkValue* value)
{
    if (value == NULL)
    {
        return;
    }

    if (value->type == AWK_STRING && value->string != NULL)
    {
        free(value->string);
    }

    value->type = AWK_UNDEFINED;
    value->number = 0.0;
    value->string = NULL;
}


double awk_to_number(AwkValue value)
{
    if (value.type == AWK_NUMBER)
    {
        return value.number;
    }

    if (value.type == AWK_STRING)
    {
        if (value.string == NULL)
        {
            return 0.0;
        }

        return strtod(value.string, NULL);
    }

    return 0.0;
}

char* awk_to_string(AwkValue value)
{
    if (value.type == AWK_STRING)
    {
        return awk_strdup(value.string);
    }

    if (value.type == AWK_NUMBER)
    {
        char buffer[64];
        snprintf(buffer, sizeof(buffer), "%.15g", value.number);
        return awk_strdup(buffer);
    }

    return awk_strdup("");
}

int awk_is_truthy(AwkValue value)
{
    if (value.type == AWK_NUMBER)
    {
        return value.number != 0.0;
    }

    if (value.type == AWK_STRING)
    {
        return value.string != NULL && value.string[0] != '\0';
    }

    return 0;
}


AwkValue awk_add(AwkValue left, AwkValue right)
{
    return awk_number(
        awk_to_number(left) + awk_to_number(right)
    );
}

AwkValue awk_sub(AwkValue left, AwkValue right)
{
    return awk_number(
        awk_to_number(left) - awk_to_number(right)
    );
}

AwkValue awk_mul(AwkValue left, AwkValue right)
{
    return awk_number(
        awk_to_number(left) * awk_to_number(right)
    );
}

AwkValue awk_div(AwkValue left, AwkValue right)
{
    return awk_number(
        awk_to_number(left) / awk_to_number(right)
    );
}

AwkValue awk_mod(AwkValue left, AwkValue right)
{
    return awk_number(
        fmod(awk_to_number(left), awk_to_number(right))
    );
}

AwkValue awk_pow(AwkValue left, AwkValue right)
{
    return awk_number(
        pow(awk_to_number(left), awk_to_number(right))
    );
}

AwkValue awk_unary_plus(AwkValue value)
{
    return awk_number(
        awk_to_number(value)
    );
}

AwkValue awk_unary_minus(AwkValue value)
{
    return awk_number(
        -awk_to_number(value)
    );
}


static int awk_should_compare_as_numbers(AwkValue left, AwkValue right)
{
    return left.type == AWK_NUMBER && right.type == AWK_NUMBER;
}

AwkValue awk_eq(AwkValue left, AwkValue right)
{
    if (awk_should_compare_as_numbers(left, right))
    {
        return awk_bool(left.number == right.number);
    }

    char* leftString = awk_to_string(left);
    char* rightString = awk_to_string(right);

    int result = strcmp(leftString, rightString) == 0;

    free(leftString);
    free(rightString);

    return awk_bool(result);
}

AwkValue awk_ne(AwkValue left, AwkValue right)
{
    AwkValue eq = awk_eq(left, right);
    int result = !awk_is_truthy(eq);
    awk_free(&eq);
    return awk_bool(result);
}

AwkValue awk_lt(AwkValue left, AwkValue right)
{
    if (awk_should_compare_as_numbers(left, right))
    {
        return awk_bool(left.number < right.number);
    }

    char* leftString = awk_to_string(left);
    char* rightString = awk_to_string(right);

    int result = strcmp(leftString, rightString) < 0;

    free(leftString);
    free(rightString);

    return awk_bool(result);
}

AwkValue awk_le(AwkValue left, AwkValue right)
{
    if (awk_should_compare_as_numbers(left, right))
    {
        return awk_bool(left.number <= right.number);
    }

    char* leftString = awk_to_string(left);
    char* rightString = awk_to_string(right);

    int result = strcmp(leftString, rightString) <= 0;

    free(leftString);
    free(rightString);

    return awk_bool(result);
}

AwkValue awk_gt(AwkValue left, AwkValue right)
{
    if (awk_should_compare_as_numbers(left, right))
    {
        return awk_bool(left.number > right.number);
    }

    char* leftString = awk_to_string(left);
    char* rightString = awk_to_string(right);

    int result = strcmp(leftString, rightString) > 0;

    free(leftString);
    free(rightString);

    return awk_bool(result);
}

AwkValue awk_ge(AwkValue left, AwkValue right)
{
    if (awk_should_compare_as_numbers(left, right))
    {
        return awk_bool(left.number >= right.number);
    }

    char* leftString = awk_to_string(left);
    char* rightString = awk_to_string(right);

    int result = strcmp(leftString, rightString) >= 0;

    free(leftString);
    free(rightString);

    return awk_bool(result);
}


AwkValue awk_not(AwkValue value)
{
    return awk_bool(!awk_is_truthy(value));
}


AwkValue awk_concat(AwkValue left, AwkValue right)
{
    char* leftString = awk_to_string(left);
    char* rightString = awk_to_string(right);

    size_t leftLength = strlen(leftString);
    size_t rightLength = strlen(rightString);

    char* result = malloc(leftLength + rightLength + 1);

    if (result == NULL)
    {
        fprintf(stderr, "Memory allocation failed\n");
        exit(1);
    }

    memcpy(result, leftString, leftLength);
    memcpy(result + leftLength, rightString, rightLength + 1);

    AwkValue finalValue = awk_string(result);

    free(leftString);
    free(rightString);
    free(result);

    return finalValue;
}



void awk_print_value(AwkValue value)
{
    char* text = awk_to_string(value);
    printf("%s\n", text);
    free(text);
}

void awk_print_values(size_t count, AwkValue* values)
{
    for (size_t i = 0; i < count; i++)
    {
        char* text = awk_to_string(values[i]);
        printf("%s", text);
        free(text);

        if (i + 1 < count)
        {
            printf(" ");
        }
    }

    printf("\n");
}
