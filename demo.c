#include <stdio.h>
#include <stdlib.h>
#include "awk_runtime.h"

int isBegin;
Fields fields;
Array* salaries;
Array* visits;
Array* departmentTotals;
Array* departmentCounts;
AwkValue highSalaryCount;
AwkValue department0;
AwkValue avg;
AwkValue key;
AwkValue remaining;
AwkValue i;
AwkValue registerEmployee(AwkValue department, AwkValue employee, AwkValue salary)
{
    AwkValue tmp0 = awk_copy(department);
    AwkValue tmp1 = awk_copy(employee);
    AwkValue tmp2[] = { tmp0, tmp1 };
    AwkValue tmp3 = awk_concat_array_arg(2, tmp2);
    AwkValue tmp4 = awk_copy(salary);
    AwkValue tmp5 = array_get_value(salaries, tmp3);
    array_set_value(salaries, tmp3, awk_copy(awk_add(tmp5, tmp4)));
    AwkValue tmp6 = array_get_value(salaries, tmp3);
    AwkValue tmp7 = awk_copy(department);
    AwkValue tmp8 = awk_copy(employee);
    AwkValue tmp9[] = { tmp7, tmp8 };
    AwkValue tmp10 = awk_concat_array_arg(2, tmp9);
    AwkValue tmp11 = array_get_value(visits, tmp10);
    array_set_value(visits, tmp10, awk_copy(awk_add(tmp11, awk_number(1))));
    AwkValue tmp12 = awk_copy(department);
    AwkValue tmp13[] = { tmp12 };
    AwkValue tmp14 = awk_concat_array_arg(1, tmp13);
    AwkValue tmp15 = awk_copy(salary);
    AwkValue tmp16 = array_get_value(departmentTotals, tmp14);
    array_set_value(departmentTotals, tmp14, awk_copy(awk_add(tmp16, tmp15)));
    AwkValue tmp17 = array_get_value(departmentTotals, tmp14);
    AwkValue tmp18 = awk_copy(department);
    AwkValue tmp19[] = { tmp18 };
    AwkValue tmp20 = awk_concat_array_arg(1, tmp19);
    AwkValue tmp21 = array_get_value(departmentCounts, tmp20);
    array_set_value(departmentCounts, tmp20, awk_copy(awk_add(tmp21, awk_number(1))));
    awk_free(&tmp0);
    awk_free(&tmp1);
    awk_free(&tmp3);
    awk_free(&tmp4);
    awk_free(&tmp5);
    awk_free(&tmp6);
    awk_free(&tmp7);
    awk_free(&tmp8);
    awk_free(&tmp10);
    awk_free(&tmp11);
    awk_free(&tmp12);
    awk_free(&tmp14);
    awk_free(&tmp15);
    awk_free(&tmp16);
    awk_free(&tmp17);
    awk_free(&tmp18);
    awk_free(&tmp20);
    awk_free(&tmp21);
    return awk_undefined();
}
AwkValue average(AwkValue total, AwkValue count)
{
    AwkValue tmp22 = awk_copy(total);
    AwkValue tmp23 = awk_copy(count);
    AwkValue tmp24 = awk_div(tmp22, tmp23);
    AwkValue tmp25 = awk_copy(tmp24);
    awk_free(&tmp22);
    awk_free(&tmp23);
    return tmp25;
    awk_free(&tmp22);
    awk_free(&tmp23);
}
void item0()
{
    AwkValue tmp26 = awk_string("=== ANALIZA PRACOWNIKOW ===");
    awk_print_value(tmp26, stdout, 0);
    AwkValue tmp27 = awk_string("Wczytywanie danych...");
    awk_print_value(tmp27, stdout, 0);
    awk_free(&tmp26);
    awk_free(&tmp27);
}
void item1()
{
    AwkValue tmp28 = awk_number(2);
    AwkValue tmp29 = fields_get(fields, awk_to_int(tmp28));
    AwkValue tmp30 = awk_number(1);
    AwkValue tmp31 = fields_get(fields, awk_to_int(tmp30));
    AwkValue tmp32 = awk_number(3);
    AwkValue tmp33 = fields_get(fields, awk_to_int(tmp32));
    AwkValue tmp34 = registerEmployee(tmp29, tmp31, tmp33);
    AwkValue tmp35 = awk_number(3);
    AwkValue tmp36 = fields_get(fields, awk_to_int(tmp35));
    AwkValue tmp37 = awk_number(0);
    AwkValue tmp38 = awk_add(tmp36, tmp37);
    AwkValue tmp39 = awk_number(12000);
    AwkValue tmp40 = awk_ge(tmp38, tmp39);
    if(awk_is_truthy(tmp40))
    {
        AwkValue tmp41 = awk_copy(highSalaryCount);
        awk_free(&highSalaryCount);
        highSalaryCount = awk_copy(awk_add(tmp41, awk_number(1)));
        awk_free(&tmp41);
    }
    awk_free(&tmp29);
    awk_free(&tmp31);
    awk_free(&tmp33);
    awk_free(&tmp34);
    awk_free(&tmp36);
}
void item2()
{
    AwkValue tmp42 = awk_string("");
    awk_print_value(tmp42, stdout, 0);
    AwkValue tmp43 = awk_string("=== PODSUMOWANIE DZIALOW ===");
    awk_print_value(tmp43, stdout, 0);
    ArrayIterator tmp44 = arrayiterator_init(departmentTotals);
    while(1)
    {
        if(arrayiterator_is_end(&tmp44))
        {
            break;
        }
        awk_free(&department0);
        department0 = awk_string(tmp44.entry->key);
        AwkValue tmp45 = awk_copy(department0);
        AwkValue tmp46[] = { tmp45 };
        AwkValue tmp47 = awk_concat_array_arg(1, tmp46);
        AwkValue tmp48 = array_get_value(departmentTotals, tmp47);
        AwkValue tmp49 = awk_copy(department0);
        AwkValue tmp50[] = { tmp49 };
        AwkValue tmp51 = awk_concat_array_arg(1, tmp50);
        AwkValue tmp52 = array_get_value(departmentCounts, tmp51);
        AwkValue tmp53 = average(tmp48, tmp52);
        awk_free(&avg);
        avg = awk_copy(tmp53);
        AwkValue tmp54 = awk_copy(department0);
        AwkValue tmp55 = awk_string("suma:");
        AwkValue tmp56 = awk_copy(department0);
        AwkValue tmp57[] = { tmp56 };
        AwkValue tmp58 = awk_concat_array_arg(1, tmp57);
        AwkValue tmp59 = array_get_value(departmentTotals, tmp58);
        AwkValue tmp60 = awk_string("liczba:");
        AwkValue tmp61 = awk_copy(department0);
        AwkValue tmp62[] = { tmp61 };
        AwkValue tmp63 = awk_concat_array_arg(1, tmp62);
        AwkValue tmp64 = array_get_value(departmentCounts, tmp63);
        AwkValue tmp65 = awk_string("srednia:");
        AwkValue tmp66 = awk_copy(avg);
        AwkValue tmp67[] = { tmp54, tmp55, tmp59, tmp60, tmp64, tmp65, tmp66 };
        awk_print_values(7, tmp67, stdout, 0);
        awk_free(&tmp45);
        awk_free(&tmp47);
        awk_free(&tmp48);
        awk_free(&tmp49);
        awk_free(&tmp51);
        awk_free(&tmp52);
        awk_free(&tmp53);
        awk_free(&tmp54);
        awk_free(&tmp55);
        awk_free(&tmp56);
        awk_free(&tmp58);
        awk_free(&tmp59);
        awk_free(&tmp60);
        awk_free(&tmp61);
        awk_free(&tmp63);
        awk_free(&tmp64);
        awk_free(&tmp65);
        awk_free(&tmp66);
        _incr_0:
        arrayiterator_next(&tmp44);
    }
    AwkValue tmp68 = awk_string("");
    awk_print_value(tmp68, stdout, 0);
    AwkValue tmp69 = awk_string("=== WSZYSCY PRACOWNICY ===");
    awk_print_value(tmp69, stdout, 0);
    ArrayIterator tmp70 = arrayiterator_init(salaries);
    while(1)
    {
        if(arrayiterator_is_end(&tmp70))
        {
            break;
        }
        awk_free(&key);
        key = awk_string(tmp70.entry->key);
        AwkValue tmp71 = awk_copy(key);
        AwkValue tmp72 = awk_string("suma:");
        AwkValue tmp73 = awk_copy(key);
        AwkValue tmp74[] = { tmp73 };
        AwkValue tmp75 = awk_concat_array_arg(1, tmp74);
        AwkValue tmp76 = array_get_value(salaries, tmp75);
        AwkValue tmp77 = awk_string("wystapienia:");
        AwkValue tmp78 = awk_copy(key);
        AwkValue tmp79[] = { tmp78 };
        AwkValue tmp80 = awk_concat_array_arg(1, tmp79);
        AwkValue tmp81 = array_get_value(visits, tmp80);
        AwkValue tmp82[] = { tmp71, tmp72, tmp76, tmp77, tmp81 };
        awk_print_values(5, tmp82, stdout, 0);
        awk_free(&tmp71);
        awk_free(&tmp72);
        awk_free(&tmp73);
        awk_free(&tmp75);
        awk_free(&tmp76);
        awk_free(&tmp77);
        awk_free(&tmp78);
        awk_free(&tmp80);
        awk_free(&tmp81);
        _incr_1:
        arrayiterator_next(&tmp70);
    }
    AwkValue tmp83 = awk_string("");
    awk_print_value(tmp83, stdout, 0);
    AwkValue tmp84 = awk_string("=== DELETE POJEDYNCZEGO ELEMENTU ===");
    awk_print_value(tmp84, stdout, 0);
    AwkValue tmp85 = awk_string("HR");
    AwkValue tmp86 = awk_string("Anna");
    AwkValue tmp87[] = { tmp85, tmp86 };
    AwkValue tmp88 = awk_concat_array_arg(2, tmp87);
    array_delete_value(salaries, tmp88);
    AwkValue tmp89 = awk_string("HR");
    AwkValue tmp90 = awk_string("Anna");
    AwkValue tmp91[] = { tmp89, tmp90 };
    AwkValue tmp92 = awk_concat_array_arg(2, tmp91);
    array_delete_value(visits, tmp92);
    ArrayIterator tmp93 = arrayiterator_init(salaries);
    while(1)
    {
        if(arrayiterator_is_end(&tmp93))
        {
            break;
        }
        awk_free(&key);
        key = awk_string(tmp93.entry->key);
        AwkValue tmp94 = awk_copy(key);
        AwkValue tmp95 = awk_copy(key);
        AwkValue tmp96[] = { tmp95 };
        AwkValue tmp97 = awk_concat_array_arg(1, tmp96);
        AwkValue tmp98 = array_get_value(salaries, tmp97);
        AwkValue tmp99[] = { tmp94, tmp98 };
        awk_print_values(2, tmp99, stdout, 0);
        awk_free(&tmp94);
        awk_free(&tmp95);
        awk_free(&tmp97);
        awk_free(&tmp98);
        _incr_2:
        arrayiterator_next(&tmp93);
    }
    AwkValue tmp100 = awk_string("");
    awk_print_value(tmp100, stdout, 0);
    AwkValue tmp101 = awk_string("=== DELETE CALEJ TABLICY ===");
    awk_print_value(tmp101, stdout, 0);
    array_delete(salaries);
    array_delete(visits);
    AwkValue tmp102 = awk_number(0);
    awk_free(&remaining);
    remaining = awk_copy(tmp102);
    ArrayIterator tmp103 = arrayiterator_init(salaries);
    while(1)
    {
        if(arrayiterator_is_end(&tmp103))
        {
            break;
        }
        awk_free(&key);
        key = awk_string(tmp103.entry->key);
        AwkValue tmp104 = awk_copy(remaining);
        awk_free(&remaining);
        remaining = awk_copy(awk_add(tmp104, awk_number(1)));
        awk_free(&tmp104);
        _incr_3:
        arrayiterator_next(&tmp103);
    }
    AwkValue tmp105 = awk_copy(remaining);
    AwkValue tmp106 = awk_number(0);
    AwkValue tmp107 = awk_eq(tmp105, tmp106);
    if(awk_is_truthy(tmp107))
    {
        AwkValue tmp108 = awk_string("Tablice salaries i visits zostaly wyczyszczone");
        awk_print_value(tmp108, stdout, 0);
        awk_free(&tmp108);
    }
    else
    {
        AwkValue tmp109 = awk_string("Blad: pozostalo elementow:");
        AwkValue tmp110 = awk_copy(remaining);
        AwkValue tmp111[] = { tmp109, tmp110 };
        awk_print_values(2, tmp111, stdout, 0);
        awk_free(&tmp109);
        awk_free(&tmp110);
    }
    AwkValue tmp112 = awk_string("");
    awk_print_value(tmp112, stdout, 0);
    AwkValue tmp113 = awk_string("=== PONOWNE UZYCIE TABLICY ===");
    awk_print_value(tmp113, stdout, 0);
    AwkValue tmp114 = awk_number(1);
    awk_free(&i);
    i = awk_copy(tmp114);
    while(1)
    {
        AwkValue tmp115 = awk_copy(i);
        AwkValue tmp116 = awk_number(5);
        AwkValue tmp117 = awk_le(tmp115, tmp116);
        if(!awk_is_truthy(tmp117))
        {
            awk_free(&tmp115);
            break;
        }
        AwkValue tmp118 = awk_string("result");
        AwkValue tmp119 = awk_copy(i);
        AwkValue tmp120[] = { tmp118, tmp119 };
        AwkValue tmp121 = awk_concat_array_arg(2, tmp120);
        AwkValue tmp122 = awk_copy(i);
        AwkValue tmp123 = awk_copy(i);
        AwkValue tmp124 = awk_mul(tmp122, tmp123);
        array_set_value(salaries, tmp121, awk_copy(tmp124));
        awk_free(&tmp115);
        awk_free(&tmp118);
        awk_free(&tmp119);
        awk_free(&tmp121);
        awk_free(&tmp122);
        awk_free(&tmp123);
        _incr_4:
        AwkValue tmp125 = awk_copy(i);
        awk_free(&i);
        i = awk_copy(awk_add(tmp125, awk_number(1)));
        awk_free(&tmp125);
    }
    AwkValue tmp126 = awk_string("result");
    AwkValue tmp127 = awk_number(3);
    AwkValue tmp128[] = { tmp126, tmp127 };
    AwkValue tmp129 = awk_concat_array_arg(2, tmp128);
    array_delete_value(salaries, tmp129);
    ArrayIterator tmp130 = arrayiterator_init(salaries);
    while(1)
    {
        if(arrayiterator_is_end(&tmp130))
        {
            break;
        }
        awk_free(&key);
        key = awk_string(tmp130.entry->key);
        AwkValue tmp131 = awk_copy(key);
        AwkValue tmp132 = awk_copy(key);
        AwkValue tmp133[] = { tmp132 };
        AwkValue tmp134 = awk_concat_array_arg(1, tmp133);
        AwkValue tmp135 = array_get_value(salaries, tmp134);
        AwkValue tmp136[] = { tmp131, tmp135 };
        awk_print_values(2, tmp136, stdout, 0);
        awk_free(&tmp131);
        awk_free(&tmp132);
        awk_free(&tmp134);
        awk_free(&tmp135);
        _incr_5:
        arrayiterator_next(&tmp130);
    }
    AwkValue tmp137 = awk_string("");
    awk_print_value(tmp137, stdout, 0);
    AwkValue tmp138 = awk_string("Liczba rekordow:");
    AwkValue tmp139 = awk_number((double)NR);
    AwkValue tmp140[] = { tmp138, tmp139 };
    awk_print_values(2, tmp140, stdout, 0);
    AwkValue tmp141 = awk_string("Pensje co najmniej 12000:");
    AwkValue tmp142 = awk_copy(highSalaryCount);
    AwkValue tmp143[] = { tmp141, tmp142 };
    awk_print_values(2, tmp143, stdout, 0);
    array_delete(salaries);
    array_delete(departmentTotals);
    array_delete(departmentCounts);
    awk_free(&tmp42);
    awk_free(&tmp43);
    awk_free(&tmp68);
    awk_free(&tmp69);
    awk_free(&tmp83);
    awk_free(&tmp84);
    awk_free(&tmp85);
    awk_free(&tmp86);
    awk_free(&tmp88);
    awk_free(&tmp89);
    awk_free(&tmp90);
    awk_free(&tmp92);
    awk_free(&tmp100);
    awk_free(&tmp101);
    awk_free(&tmp105);
    awk_free(&tmp112);
    awk_free(&tmp113);
    awk_free(&tmp126);
    awk_free(&tmp129);
    awk_free(&tmp137);
    awk_free(&tmp138);
    awk_free(&tmp141);
    awk_free(&tmp142);
}
int main(int argc, char* argv[])
{
    if (argc != 2)
    {
        fprintf(stderr, "Expected 1 argument <filename>, got %d arguments\n", argc-1);
        return 1;
    }
    salaries = array_init();
    visits = array_init();
    departmentTotals = array_init();
    departmentCounts = array_init();
    awk_set_default_predefined();
    isBegin = 1;
    item0();
    FILE* file = fopen(argv[1], "r");
    if (file == NULL)
    {
        fprintf(stderr, "Failed to open file %s\n", argv[1]);
        return 1;
    }
    char buffer[1024];
    while(fgets(buffer, sizeof(buffer), file))
    {
        remove_newline(buffer);
        fields = fields_string(buffer);
        NR++;
        item1();
        fields_free(&fields);
        isBegin = 0;
    }
    item2();
    fclose(file);
    array_free(salaries);
    array_free(visits);
    array_free(departmentTotals);
    array_free(departmentCounts);
    return 0;
}
