#include "awk_runtime.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>
#include <regex.h>

char* FS;
char* CONVFMT;
int NR;
char* SUBSEP;


char* awk_strdup(const char* value)
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

static unsigned long djb2(unsigned char *str)
{
    unsigned long hash = 5381;
    int c;
    while (c = *str++)
        hash = ((hash << 5) + hash) + c;
    return hash;
}

static void array_rehash(Array* array)
{
    // should calculate load factor and rehash if needed
}

Array* array_init()
{
    Array* array = malloc(sizeof(Array));
    if (array == NULL)
    {
        fprintf(stderr, "Memory allocation failed\n");
        exit(1);
    }
    array->capacity = 16;
    array->size = 0;
    void* tmp = malloc(sizeof(ArrayEntry*)*16);
    if (tmp == NULL)
    {
        fprintf(stderr, "Memory allocation failed\n");
        exit(1);
    }
    array->entries = tmp;
    for (int i = 0; i < 16; i++)
        array->entries[i] = NULL;
    return array;
}

AwkValue array_get_value(Array *array, AwkValue key)
{
    char* string_key = awk_to_string(key);
    unsigned long hash_key = djb2(string_key) % array->capacity;
    ArrayEntry* entry = array->entries[hash_key];

    while (entry != NULL)
    {
        if (strcmp(entry->key, string_key) == 0)
        {
            free(string_key);
            return awk_copy(entry->value);
        }
        entry = entry->next;
    }
    free(string_key);
    return awk_undefined();
}

void array_set_value(Array *array, AwkValue key, AwkValue value)
{
    char* string_key = awk_to_string(key);
    unsigned long hash_key = djb2(string_key) % array->capacity;
    ArrayEntry dummy;
    dummy.next = array->entries[hash_key];
    ArrayEntry* entry = &dummy;

    while (entry->next != NULL)
    {
        entry = entry->next;
        if (strcmp(entry->key, string_key) == 0)
        {
            awk_free(&entry->value);
            entry->value = value;
            free(string_key);
            return;
        }
    }

    ArrayEntry* tmp = malloc(sizeof(ArrayEntry));
    if (tmp == NULL)
    {
        fprintf(stderr, "Memory allocation failed\n");
        exit(1);
    }
    entry->next = tmp;
    tmp->value = value;
    tmp->key = string_key;
    tmp->next = NULL;
    array->size++;
    array->entries[hash_key] = dummy.next;
    array_rehash(array);
}

void array_delete_value(Array *array, AwkValue key)
{
    if (array == NULL)
        return;

    char* string_key = awk_to_string(key);
    unsigned long hash_key = djb2(string_key) % array->capacity;

    ArrayEntry* previous = NULL;
    ArrayEntry* current = array->entries[hash_key];

    while (current != NULL)
    {
        if (strcmp(current->key, string_key) == 0)
        {
            if (previous == NULL)
                array->entries[hash_key] = current->next;
            else
                previous->next = current->next;

            awk_free(&current->value);
            free(current->key);
            free(current);
            array->size--;

            free(string_key);
            return;
        }

        previous = current;
        current = current->next;
    }

    free(string_key);
}

void array_delete(Array *array)
{
    if (array == NULL)
        return;

    for (size_t i = 0; i < array->capacity; i++)
    {
        ArrayEntry* entry = array->entries[i];

        while (entry != NULL)
        {
            ArrayEntry* next = entry->next;

            awk_free(&entry->value);
            free(entry->key);
            free(entry);

            entry = next;
        }

        array->entries[i] = NULL;
    }

    array->size = 0;
}

void array_free(Array *array)
{
    if (array == NULL)
        return;

    array_delete(array);
    free(array->entries);
    free(array);
}

ArrayIterator arrayiterator_init(Array *array)
{
    ArrayIterator iter;
    iter.keys = malloc(sizeof(char*) * array->size);
    if (iter.keys == NULL)
    {
        fprintf(stderr, "Memory allocation failed\n");
        exit(1);
    }
    iter.i = 0;
    iter.size = array->size;
    int current = 0;
    
    ArrayEntry* entry;
    for (int i = 0; i < array->capacity; i++)
    {
        if (array->entries[i] != NULL)
        {
            entry = array->entries[i];
            while(entry != NULL)
            {
                iter.keys[current] = malloc(sizeof(char) * (strlen(entry->key) + 1));
                if (iter.keys[current] == NULL)
                {
                    fprintf(stderr, "Memory allocation failed\n");
                    exit(1);
                }
                strcpy(iter.keys[current++], entry->key);
                entry = entry->next;
            }
        }
    }
    return iter;
}

int arrayiterator_is_end(ArrayIterator *iter)
{
    return iter->i >= iter->size;
}

void arrayiterator_next(ArrayIterator *iter)
{
    iter->i++;
}

void arrayiterator_free(ArrayIterator* iter)
{
    if (iter == NULL || iter->keys == NULL)
        return;
    for (size_t i = 0; i < iter->size; i++)
    {
        free(iter->keys[i]);
    }
    free(iter->keys);
    iter->keys = NULL;
    iter->size = 0;
    iter->i = 0;
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
    for(int i = 0; i < value->size; i++)
    {
        if(value->data[i] != NULL)
            free(value->data[i]);
    }
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
    int toFree = 0;
    char* s;
    if (value.type == AWK_STRING)
        s = value.string;
    else if (value.type == AWK_NUMBER)
    {
        s = awk_to_string(value);
        toFree = 1;
    }
    else
        s = "";
    
    regex_t regex;
    regmatch_t  pmatch[1];
    int result = regcomp(&regex, regex_cstring, REG_EXTENDED);
    if (result != 0)
    {
        if(toFree) free(s);
        fprintf(stderr, "Failed to compile regex\n");
        exit(1);
    }
    size_t n_match = 1;
    int is_match = regexec(&regex, s, n_match, pmatch, 0);
    if(toFree) free(s);
    regfree(&regex);
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
        snprintf(buffer, sizeof(buffer), CONVFMT, value.number);
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

AwkValue awk_concat_array_arg(size_t count, AwkValue *values)
{
    char** strings = malloc(count * sizeof(char*));
    if (strings == NULL)
    {
        fprintf(stderr, "Memory allocation failed\n");
        exit(1);
    }
    size_t len = count * sizeof(char) * strlen(SUBSEP) + 1;
    for (size_t i = 0; i < count; i++)
    {
        strings[i] = awk_to_string(values[i]);
        len += strlen(strings[i]);
    }

    char* result = malloc((len + 1) * sizeof(char));
    if (result == NULL)
    {
        fprintf(stderr, "Memory allocation failed\n");
        exit(1);
    }
    strcpy(result, strings[0]);
    free(strings[0]);
    for (size_t i = 1; i < count; i++)
    {
        strcat(result, SUBSEP);
        strcat(result, strings[i]);
        free(strings[i]);
    }
    free(strings);
    return (AwkValue){AWK_STRING, 0.0, result};
}

void awk_set_default_predefined()
{
    FS = malloc(2 * sizeof(char));
    CONVFMT = malloc(5 * sizeof(char));
    SUBSEP = malloc(2 * sizeof(char));

    if (FS == NULL || CONVFMT == NULL || SUBSEP == NULL)
    {
        fprintf(stderr, "Memory allocation failed\n");
        exit(1);
    }
    
    strcpy(FS, " ");
    strcpy(CONVFMT, "%.6g");
    strcpy(SUBSEP, "@");
    NR = 0;
}

FILE *awk_output_redirection_write(AwkValue value)
{
    char* name = awk_to_string(value);
    FILE* file = fopen(name, "w");
    if (file == NULL)
    {
        fprintf(stderr, "Failed to open file %s\n", name);
        exit(1);
    }
    free(name);
    return file;
}

FILE *awk_output_redirection_append(AwkValue value)
{
    char* name = awk_to_string(value);
    FILE* file = fopen(name, "a");
    if (file == NULL)
    {
        fprintf(stderr, "Failed to open file %s\n", name);
        exit(1);
    }
    free(name);
    return file;
}

FILE *awk_output_redirection_pipe(AwkValue value)
{
    char* name = awk_to_string(value);
    FILE* file = popen(name, "w");
    if (file == NULL)
    {
        fprintf(stderr, "Failed to open pipe %s\n", name);
        exit(1);
    }
    free(name);
    return file;
}

void awk_print_value(AwkValue value, FILE* stream, int type)
{
    char* text = awk_to_string(value);
    fprintf(stream, "%s\n", text);
    free(text);
    if (type == 1)
        fclose(stream);
    if (type == 2)
        pclose(stream);
    
}

void awk_print_values(size_t count, AwkValue* values, FILE* stream, int type)
{
    for (size_t i = 0; i < count; i++)
    {
        char* text = awk_to_string(values[i]);
        fprintf(stream, "%s", text);
        free(text);

        if (i + 1 < count)
        {
            fprintf(stream, " ");
        }
    }

    fprintf(stream, "\n");
    if (type == 1)
        fclose(stream);
    if (type == 2)
        pclose(stream);
}

AwkValue awk_atan2(AwkValue x, AwkValue y)
{
    return awk_number(atan2(awk_to_number(x), awk_to_number(y)));
}

AwkValue awk_cos(AwkValue value)
{
    return awk_number(cos(awk_to_number(value)));
}

AwkValue awk_sin(AwkValue value)
{
    return awk_number(sin(awk_to_number(value)));
}

AwkValue awk_exp(AwkValue value)
{
    return awk_number(exp(awk_to_number(value)));
}

AwkValue awk_log(AwkValue value)
{
    return awk_number(log(awk_to_number(value)));
}

AwkValue awk_sqrt(AwkValue value)
{
    return awk_number(sqrt(awk_to_number(value)));
}

AwkValue awk_int(AwkValue value)
{
    return awk_number((int)awk_to_number(value));
}
