function registerEmployee(department, employee, salary) {
    salaries[department, employee] += salary
    visits[department, employee]++
    departmentTotals[department] += salary
    departmentCounts[department]++
}

function average(total, count) {
    return total / count
}

BEGIN {
    print "=== ANALIZA PRACOWNIKOW ==="
    print "Wczytywanie danych..."
}

{
    registerEmployee($2, $1, $3)

    if ($3 >= 12000) {
        highSalaryCount++
    }
}

END {
    print ""
    print "=== PODSUMOWANIE DZIALOW ==="

    for (department in departmentTotals) {
        avg = average(departmentTotals[department],departmentCounts[department])

        print department,
              "suma:", departmentTotals[department],
              "liczba:", departmentCounts[department],
              "srednia:", avg
    }

    print ""
    print "=== WSZYSCY PRACOWNICY ==="

    for (key in salaries) {
        print key,
              "suma:", salaries[key],
              "wystapienia:", visits[key]
    }

    print ""
    print "=== DELETE POJEDYNCZEGO ELEMENTU ==="

    delete salaries["HR", "Anna"]
    delete visits["HR", "Anna"]

    for (key in salaries) {
        print key, salaries[key]
    }

    print ""
    print "=== DELETE CALEJ TABLICY ==="

    delete salaries
    delete visits

    remaining = 0

    for (key in salaries) {
        remaining++
    }

    if (remaining == 0) {
        print "Tablice salaries i visits zostaly wyczyszczone"
    } else {
        print "Blad: pozostalo elementow:", remaining
    }

    print ""
    print "=== PONOWNE UZYCIE TABLICY ==="

    for (i = 1; i <= 5; i++) {
        salaries["result", i] = i * i
    }

    delete salaries["result", 3]

    for (key in salaries) {
        print key, salaries[key]
    }

    print ""
    print "Liczba rekordow:", NR
    print "Pensje co najmniej 12000:", highSalaryCount

    delete salaries
    delete departmentTotals
    delete departmentCounts
}