BEGIN {
    a[0] = 1
    a[1] = 5
    a[2] = "test"
    print a[2]
    b = 0
    a[b] = a[2]
    print a[b], a[2]
    a[2] = a[1]
    print a[2]
}