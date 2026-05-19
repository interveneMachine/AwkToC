#include "awk_runtime.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>
#include <regex.h>

char* FS = " ";


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

void remove_newline(char *value)
{
    int i = 0;
    while(value[i] != '\n' && value[i] != '\0') i++;
    value[i] = '\0';
}

Fields fields_string(char *value)
{
    Fields result;
    result.data = NULL;
    result.size = 0;

    if (value == NULL || value[0] == '\0')
    {
        return result;
    }

    regex_t regex;
    int regcomp_result = regcomp(&regex, FS, REG_EXTENDED);
    
    if (regcomp_result != 0)
    {
        fprintf(stderr, "Failed to compile regex for FS\n");
        exit(1);
    }

    // First pass: count number of fields and collect match positions
    size_t match_count = 0;
    regmatch_t *matches = NULL;
    char *ptr = value;
    size_t offset = 0;
    
    while (offset < strlen(value))
    {
        regmatch_t match;
        if (regexec(&regex, ptr, 1, &match, 0) == 0 && match.rm_so != -1)
        {
            if (match.rm_eo == match.rm_so)
            {
                // Empty match, skip one character to avoid infinite loop
                offset++;
                ptr = value + offset;
                continue;
            }

            // Resize matches array
            regmatch_t *new_matches = (regmatch_t*)realloc(matches, (match_count + 1) * sizeof(regmatch_t));
            if (new_matches == NULL)
            {
                fprintf(stderr, "Memory allocation failed\n");
                free(matches);
                regfree(&regex);
                exit(1);
            }
            matches = new_matches;
            
            // Store match with absolute position
            matches[match_count].rm_so = offset + match.rm_so;
            matches[match_count].rm_eo = offset + match.rm_eo;
            match_count++;
            
            offset += match.rm_eo;
            ptr = value + offset;
        }
        else
        {
            break;
        }
    }

    // Number of fields = number of matches + 1
    size_t field_count = match_count + 1;
    result.data = (char**)malloc(field_count * sizeof(char*));
    if (result.data == NULL)
    {
        fprintf(stderr, "Memory allocation failed\n");
        free(matches);
        regfree(&regex);
        exit(1);
    }

    // Extract fields
    size_t field_start = 0;
    
    for (size_t i = 0; i < match_count; i++)
    {
        size_t field_length = matches[i].rm_so - field_start;
        char* field = (char*)malloc(field_length + 1);
        if (field == NULL)
        {
            fprintf(stderr, "Memory allocation failed\n");
            free(matches);
            regfree(&regex);
            exit(1);
        }

        strncpy(field, value + field_start, field_length);
        field[field_length] = '\0';
        result.data[result.size++] = field;
        
        field_start = matches[i].rm_eo;
    }

    // Add the last field
    size_t remaining_length = strlen(value) - field_start;
    char* last_field = (char*)malloc(remaining_length + 1);
    if (last_field == NULL)
    {
        fprintf(stderr, "Memory allocation failed\n");
        free(matches);
        regfree(&regex);
        exit(1);
    }
    strcpy(last_field, value + field_start);
    result.data[result.size++] = last_field;

    free(matches);
    regfree(&regex);
    return result;
}

AwkValue fields_get(Fields fields, int id)
{
    if (id < 0)
    {
        id = fields.size + id + 1;
        if (id <= 0)
            return awk_undefined();
    }
    if (id > fields.size)
    {
        return awk_undefined();
    }
    if (id == 0)
    {
        // Concatenate all fields with FS separator to reconstruct $0
        size_t total_size = 0;
        for (size_t i = 0; i < fields.size; i++)
        {
            total_size += strlen(fields.data[i]);
            if (i < fields.size - 1)
            {
                total_size += strlen(FS);
            }
        }
        
        char* result = malloc(total_size + 1);
        if (result == NULL)
        {
            fprintf(stderr, "Memory allocation failed\n");
            exit(1);
        }
        
        result[0] = '\0';
        for (size_t i = 0; i < fields.size; i++)
        {
            strcat(result, fields.data[i]);
            if (i < fields.size - 1)
            {
                strcat(result, FS);
            }
        }
        
        AwkValue value = awk_string(result);
        free(result);
        return value;
    }
    return awk_string(fields.data[id-1]);
}

AwkValue fields_assign(Fields* fields, int id, AwkValue value)
{
    // TODO
    return awk_string("text");
}

void fields_free(Fields* value)
{
    if(value == NULL || value->data == NULL)
        return;
    free(value->data);
    value->size = 0;
    value->data = NULL;
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

AwkValue awk_match(AwkValue value, const char* regex_cstring)
{
    char* s;
    if (value.type == AWK_STRING)
        s = value.string;
    else if (value.type == AWK_NUMBER)
        s = awk_to_string(value);
    else
        s = "";
    
    regex_t regex;
    regmatch_t  pmatch[1];
    int result = regcomp(&regex, regex_cstring, REG_EXTENDED);
    if (result != 0)
    {
        fprintf(stderr, "Failed to compile regex\n");
        exit(1);
    }
    size_t n_match = 1;
    int is_match = regexec(&regex, s, n_match, pmatch, 0);
    if (is_match)
        return awk_bool(0);
    return awk_bool(1);
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

int awk_to_int(AwkValue value)
{
    if (value.type == AWK_STRING)
    {
        return atoi(value.string);
    }

    if (value.type == AWK_NUMBER)
    {
        return (int)value.number;
    }

    return -1; // error value
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
