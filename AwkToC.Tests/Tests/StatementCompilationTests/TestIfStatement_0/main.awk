BEGIN {
    i = 0
}

{
    i++
    if (i % 3 == 0)
    {
        print i
    }
    if ((i + 1) % 2) print i
}