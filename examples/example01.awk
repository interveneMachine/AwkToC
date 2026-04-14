BEGIN {
    printf "%-10s %-10s %-10s\n", "Name", "Rate", "Total Pay"
    print "------------------------------------------"
}
# test comments
{ print }

{
    total_pay = $2 * $3
    grand_total += total_pay

    printf "%-10s $%-9.2f $%-9.2f\n", $1, $2, total_pay
}



END {
    print "------------------------------------------"
    printf "Grand Total: $%.2f\n", grand_total
}