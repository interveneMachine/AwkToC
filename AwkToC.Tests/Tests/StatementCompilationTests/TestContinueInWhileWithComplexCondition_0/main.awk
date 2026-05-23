BEGIN {
    i = 0
    while (i < 10 && i * 2 < 15)
    {
        if (i == 2)
        {
            i++
            continue
        }
        print i
        i++
    }
}
